using System.Data;
using AgentInfrastructure.Data;
using AgentInfrastructure.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentCore.CodeMap;

// Raw SQL result DTOs
public class ModuleRow { public string Module { get; set; } = ""; public int Cnt { get; set; } }
public class EdgeRow { public string Source { get; set; } = ""; public string Target { get; set; } = ""; public int Cnt { get; set; } }

public class CodeMapService : ICodeMapService
{
    private readonly AgentDbContext _db;
    private readonly RoslynAnalyzer _analyzer;
    private readonly TypeScriptAnalyzer _tsAnalyzer;
    private readonly ILogger<CodeMapService> _logger;
    private static readonly Dictionary<Guid, ScanProgress> _progressMap = new();
    private static readonly Dictionary<string, (GraphData Data, DateTime CachedAt)> _graphCache = new();

    public CodeMapService(AgentDbContext db, RoslynAnalyzer analyzer, TypeScriptAnalyzer tsAnalyzer, ILogger<CodeMapService> logger)
    {
        _db = db;
        _analyzer = analyzer;
        _tsAnalyzer = tsAnalyzer;
        _logger = logger;
    }

    public ScanProgress? GetScanProgress(Guid projectId)
    {
        lock (_progressMap) { return _progressMap.TryGetValue(projectId, out var p) ? p : null; }
    }

    // ── Scan ──

    public async Task<ScanStats> ScanProjectAsync(Guid projectId, string rootPath)
    {
        _logger.LogInformation("Scanning project {ProjectId} at {Path}", projectId, rootPath);

        var progress = new ScanProgress { IsRunning = true, Phase = "discovering" };
        lock (_progressMap) { _progressMap[projectId] = progress; }

        // Clear old data
        progress.Phase = "clearing";
        await ClearProjectAsync(projectId);

        var allFiles = DiscoverFiles(rootPath);
        var csFiles = allFiles.Where(f => f.EndsWith(".cs")).ToList();
        var tsFiles = allFiles.Where(f => f.EndsWith(".ts")).ToList();

        _logger.LogInformation("Found {CsCount} .cs files, {TsCount} .ts files", csFiles.Count, tsFiles.Count);

        var totalFiles = csFiles.Count + tsFiles.Count;
        progress.TotalFiles = totalFiles;
        progress.Phase = "analyzing";
        var processed = 0;

        var allSymbols = new List<CodeSymbol>();
        var allEndpoints = new List<CodeApiEndpoint>();
        var allDependencies = new List<CodeDependency>();

        // Analyze C# files
        foreach (var file in csFiles)
        {
            try
            {
                var result = _analyzer.AnalyzeFile(file, rootPath, projectId);
                allSymbols.AddRange(result.Symbols);
                allEndpoints.AddRange(result.Endpoints);
                allDependencies.AddRange(result.Dependencies);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze C# {File}", file);
            }
            progress.ProcessedFiles = ++processed;
        }

        // Analyze TypeScript files
        foreach (var file in tsFiles)
        {
            try
            {
                var result = _tsAnalyzer.AnalyzeFile(file, rootPath, projectId);
                allSymbols.AddRange(result.Symbols);
                allDependencies.AddRange(result.Dependencies);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze TS {File}", file);
            }
            progress.ProcessedFiles = ++processed;
        }

        // Resolve ToSymbolId for dependencies where possible
        var symbolLookup = new Dictionary<string, Guid>();
        foreach (var s in allSymbols.Where(s => s.SymbolType is "Class" or "Interface" or "Enum" or "Struct" or "Record"))
            symbolLookup[s.Name] = s.Id; // last wins if duplicate name

        foreach (var dep in allDependencies)
        {
            var cleanName = dep.ToSymbolName.Split('<')[0]; // strip generic: ILogger<T> → ILogger
            if (symbolLookup.TryGetValue(cleanName, out var id))
                dep.ToSymbolId = id;
        }

        // Bulk insert
        progress.Phase = "saving";
        _db.CodeSymbols.AddRange(allSymbols);
        _db.CodeApiEndpoints.AddRange(allEndpoints);
        _db.CodeDependencies.AddRange(allDependencies);
        await _db.SaveChangesAsync();

        // Update project LastScannedAt
        var project = await _db.Projects.FindAsync(projectId);
        if (project != null)
        {
            project.LastScannedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        progress.Phase = "done";
        progress.IsRunning = false;
        _logger.LogInformation("Scan complete: {Symbols} symbols, {Endpoints} endpoints, {Deps} dependencies",
            allSymbols.Count, allEndpoints.Count, allDependencies.Count);

        // Clean up progress after a delay
        _ = Task.Run(async () => { await Task.Delay(30000); lock (_progressMap) { _progressMap.Remove(projectId); } });

        return new ScanStats
        {
            ProjectId = projectId,
            ProjectName = project?.Name ?? "",
            TotalFiles = csFiles.Count,
            Classes = allSymbols.Count(s => s.SymbolType == "Class"),
            Interfaces = allSymbols.Count(s => s.SymbolType == "Interface"),
            Methods = allSymbols.Count(s => s.SymbolType == "Method"),
            Properties = allSymbols.Count(s => s.SymbolType == "Property"),
            Endpoints = allEndpoints.Count,
            Dependencies = allDependencies.Count,
            LastScannedAt = DateTime.UtcNow
        };
    }

    /// <summary>Incremental scan — only re-scan files changed since last scan</summary>
    public async Task<ScanStats> ScanIncrementalAsync(Guid projectId, string rootPath)
    {
        _logger.LogInformation("Incremental scan {ProjectId} at {Path}", projectId, rootPath);

        var progress = new ScanProgress { IsRunning = true, Phase = "discovering" };
        lock (_progressMap) { _progressMap[projectId] = progress; }

        // Get last scan time
        var lastScanned = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId)
            .Select(s => s.ScannedAt)
            .OrderByDescending(d => d)
            .FirstOrDefaultAsync();

        var allFiles = DiscoverFiles(rootPath);
        var changedFiles = allFiles
            .Where(f => lastScanned == default || File.GetLastWriteTimeUtc(f) > lastScanned)
            .ToList();

        _logger.LogInformation("Incremental: {Changed}/{Total} files changed since {LastScan}",
            changedFiles.Count, allFiles.Count, lastScanned);

        if (changedFiles.Count == 0)
        {
            progress.Phase = "done"; progress.IsRunning = false;
            lock (_progressMap) { _progressMap.Remove(projectId); }
            return await GetStatsAsync(projectId);
        }

        progress.TotalFiles = changedFiles.Count;
        progress.Phase = "analyzing";

        // Delete old data for changed files only
        var changedRelPaths = changedFiles
            .Select(f => Path.GetRelativePath(rootPath, f).Replace('\\', '/'))
            .ToHashSet();

        await DeleteByFilePaths(projectId, changedRelPaths);

        // Scan changed files
        var (allSymbols, allEndpoints, allDependencies) = AnalyzeFiles(changedFiles, rootPath, projectId, progress);

        // Resolve dependencies against ALL existing symbols + new ones
        await ResolveDependencies(projectId, allSymbols, allDependencies);

        progress.Phase = "saving";
        _db.CodeSymbols.AddRange(allSymbols);
        _db.CodeApiEndpoints.AddRange(allEndpoints);
        _db.CodeDependencies.AddRange(allDependencies);
        await _db.SaveChangesAsync();

        var project = await _db.Projects.FindAsync(projectId);
        if (project != null) { project.LastScannedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }

        progress.Phase = "done"; progress.IsRunning = false;
        _ = Task.Run(async () => { await Task.Delay(30000); lock (_progressMap) { _progressMap.Remove(projectId); } });

        return await GetStatsAsync(projectId);
    }

    /// <summary>Add another path to existing project data (e.g. add FE to BE project)</summary>
    public async Task<ScanStats> ScanAddPathAsync(Guid projectId, string rootPath)
    {
        _logger.LogInformation("Add path scan {ProjectId} at {Path}", projectId, rootPath);

        var progress = new ScanProgress { IsRunning = true, Phase = "discovering" };
        lock (_progressMap) { _progressMap[projectId] = progress; }

        var allFiles = DiscoverFiles(rootPath);

        // Get the root folder name to use as path prefix for cleanup
        var rootFolderName = new DirectoryInfo(rootPath).Name;
        progress.Phase = "clearing";
        await DeleteByPathPrefix(projectId, rootFolderName);
        _db.ChangeTracker.Clear();

        progress.TotalFiles = allFiles.Count;
        progress.Phase = "analyzing";

        var (allSymbols, allEndpoints, allDependencies) = AnalyzeFiles(allFiles, rootPath, projectId, progress);

        await ResolveDependencies(projectId, allSymbols, allDependencies);
        _db.ChangeTracker.Clear();

        progress.Phase = "saving";
        _db.CodeSymbols.AddRange(allSymbols);
        _db.CodeApiEndpoints.AddRange(allEndpoints);
        _db.CodeDependencies.AddRange(allDependencies);
        await _db.SaveChangesAsync();

        var project = await _db.Projects.FindAsync(projectId);
        if (project != null) { project.LastScannedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }

        progress.Phase = "done"; progress.IsRunning = false;
        _ = Task.Run(async () => { await Task.Delay(30000); lock (_progressMap) { _progressMap.Remove(projectId); } });

        return await GetStatsAsync(projectId);
    }

    // ── Shared helpers for scan ──

    private List<string> DiscoverFiles(string rootPath)
    {
        bool ShouldExclude(string f)
        {
            var normalized = f.Replace('/', '\\');
            return normalized.Contains("\\obj\\")
                || normalized.Contains("\\bin\\")
                || normalized.Contains("\\.vs\\")
                || normalized.Contains("\\node_modules\\")
                || normalized.Contains("\\dist\\")
                || normalized.Contains("\\.angular\\")
                || normalized.Contains("\\packages\\");
        }

        var csFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !ShouldExclude(f));
        var tsFiles = Directory.GetFiles(rootPath, "*.ts", SearchOption.AllDirectories)
            .Where(f => !ShouldExclude(f) && !f.EndsWith(".spec.ts") && !f.EndsWith(".d.ts"));

        return csFiles.Concat(tsFiles).ToList();
    }

    private (List<CodeSymbol>, List<CodeApiEndpoint>, List<CodeDependency>) AnalyzeFiles(
        List<string> files, string rootPath, Guid projectId, ScanProgress progress)
    {
        var allSymbols = new List<CodeSymbol>();
        var allEndpoints = new List<CodeApiEndpoint>();
        var allDependencies = new List<CodeDependency>();
        var processed = 0;

        foreach (var file in files)
        {
            try
            {
                ScanResult result;
                if (file.EndsWith(".cs"))
                    result = _analyzer.AnalyzeFile(file, rootPath, projectId);
                else
                    result = _tsAnalyzer.AnalyzeFile(file, rootPath, projectId);

                allSymbols.AddRange(result.Symbols);
                allEndpoints.AddRange(result.Endpoints);
                allDependencies.AddRange(result.Dependencies);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze {File}", file);
            }
            progress.ProcessedFiles = ++processed;
        }

        return (allSymbols, allEndpoints, allDependencies);
    }

    private async Task ResolveDependencies(Guid projectId, List<CodeSymbol> newSymbols, List<CodeDependency> newDeps)
    {
        // Build lookup from new symbols + existing DB symbols
        var existingList = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && s.SymbolType != "Method" && s.SymbolType != "Property" && s.SymbolType != "Field")
            .Select(s => new { s.Name, s.Id })
            .ToListAsync();

        var existingLookup = new Dictionary<string, Guid>();
        foreach (var s in existingList)
            existingLookup[s.Name] = s.Id; // last wins if duplicate

        foreach (var s in newSymbols.Where(s => s.SymbolType is "Class" or "Interface" or "Enum" or "Struct" or "Record"))
            existingLookup[s.Name] = s.Id;

        foreach (var dep in newDeps)
        {
            var cleanName = dep.ToSymbolName.Split('<')[0];
            if (existingLookup.TryGetValue(cleanName, out var id))
                dep.ToSymbolId = id;
        }
    }

    private async Task DeleteByFilePaths(Guid projectId, HashSet<string> relativePaths)
    {
        // Get symbol IDs for these files
        var symbolIds = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && relativePaths.Contains(s.FilePath))
            .Select(s => s.Id)
            .ToListAsync();

        if (!symbolIds.Any()) return;

        var idSet = symbolIds.ToHashSet();

        // Delete deps referencing these symbols
        var deps = await _db.CodeDependencies
            .Where(d => d.ProjectId == projectId && (idSet.Contains(d.FromSymbolId) || (d.ToSymbolId != null && idSet.Contains(d.ToSymbolId.Value))))
            .ToListAsync();
        _db.CodeDependencies.RemoveRange(deps);

        // Delete endpoints referencing these symbols
        var endpoints = await _db.CodeApiEndpoints
            .Where(e => e.ProjectId == projectId && relativePaths.Contains(e.FilePath))
            .ToListAsync();
        _db.CodeApiEndpoints.RemoveRange(endpoints);

        // Delete symbols
        var symbols = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && relativePaths.Contains(s.FilePath))
            .ToListAsync();
        _db.CodeSymbols.RemoveRange(symbols);

        await _db.SaveChangesAsync();
    }

    private async Task DeleteByPathPrefix(Guid projectId, string pathPrefix)
    {
        // Fast SQL delete by path prefix — no need to load entities
        var like = $"{pathPrefix}%";
        await _db.Database.ExecuteSqlRawAsync(
            @"DELETE d FROM [CodeDependencies] d
              INNER JOIN [CodeSymbols] s ON d.[FromSymbolId] = s.[Id]
              WHERE d.[ProjectId] = {0} AND s.[FilePath] LIKE {1}", projectId, like);
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM [CodeApiEndpoints] WHERE [ProjectId] = {0} AND [FilePath] LIKE {1}", projectId, like);
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM [CodeSymbols] WHERE [ProjectId] = {0} AND [FilePath] LIKE {1}", projectId, like);
    }

    private void InvalidateCache(Guid projectId)
    {
        lock (_graphCache)
        {
            var keysToRemove = _graphCache.Keys.Where(k => k.Contains(projectId.ToString())).ToList();
            foreach (var k in keysToRemove) _graphCache.Remove(k);
        }
    }

    public async Task ClearProjectAsync(Guid projectId)
    {
        InvalidateCache(projectId);
        var pid = projectId.ToString();
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM [CodeDependencies] WHERE [ProjectId] = {0}", projectId);
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM [CodeApiEndpoints] WHERE [ProjectId] = {0}", projectId);
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM [CodeSymbols] WHERE [ProjectId] = {0}", projectId);
    }

    // ── Queries ──

    public async Task<List<SymbolResult>> FindSymbolAsync(Guid projectId, string name, string? symbolType = null)
    {
        var query = _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && s.Name.Contains(name));

        if (!string.IsNullOrEmpty(symbolType))
            query = query.Where(s => s.SymbolType == symbolType);

        // Get matching type-level symbols (not methods/properties)
        var typeSymbols = await query
            .Where(s => s.SymbolType != "Method" && s.SymbolType != "Property" || s.Name == name)
            .OrderBy(s => s.Name)
            .Take(20)
            .ToListAsync();

        var result = new List<SymbolResult>();

        foreach (var sym in typeSymbols)
        {
            var dto = MapSymbol(sym);

            // Load children (methods/properties) for type-level symbols
            if (sym.SymbolType is "Class" or "Interface" or "Struct" or "Record")
            {
                var children = await _db.CodeSymbols
                    .Where(c => c.ParentId == sym.Id)
                    .OrderBy(c => c.SymbolType).ThenBy(c => c.Name)
                    .ToListAsync();
                dto.Children = children.Select(MapSymbol).ToList();

                // Load dependencies
                var deps = await _db.CodeDependencies
                    .Where(d => d.FromSymbolId == sym.Id)
                    .ToListAsync();
                dto.Dependencies = deps.Select(d => new DependencyResult
                {
                    FromSymbol = sym.Name,
                    ToSymbol = d.ToSymbolName,
                    DependencyType = d.DependencyType
                }).ToList();
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<List<EndpointResult>> FindEndpointAsync(Guid projectId, string? route = null, string? httpMethod = null)
    {
        var query = _db.CodeApiEndpoints.Where(e => e.ProjectId == projectId);

        if (!string.IsNullOrEmpty(route))
            query = query.Where(e => e.RouteTemplate.Contains(route));

        if (!string.IsNullOrEmpty(httpMethod))
            query = query.Where(e => e.HttpMethod == httpMethod.ToUpper());

        var endpoints = await query
            .OrderBy(e => e.RouteTemplate)
            .Take(50)
            .ToListAsync();

        var result = new List<EndpointResult>();
        foreach (var ep in endpoints)
        {
            var dto = new EndpointResult
            {
                HttpMethod = ep.HttpMethod,
                RouteTemplate = ep.RouteTemplate,
                ControllerName = ep.ControllerName,
                ActionName = ep.ActionName,
                FilePath = ep.FilePath,
                LineNumber = ep.LineNumber
            };

            // Build service chain: Controller → Service → Repository
            if (ep.SymbolId.HasValue)
            {
                var controllerSymbol = await _db.CodeSymbols
                    .FirstOrDefaultAsync(s => s.Id == ep.SymbolId.Value);
                if (controllerSymbol?.ParentId != null)
                {
                    dto.ServiceChain = await BuildServiceChain(controllerSymbol.ParentId.Value);
                }
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<DependencyTreeResult> GetDependenciesAsync(Guid projectId, string symbolName, int depth = 1)
    {
        var symbol = await _db.CodeSymbols
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Name == symbolName
                && s.SymbolType != "Method" && s.SymbolType != "Property");

        if (symbol == null)
            return new DependencyTreeResult { Symbol = symbolName };

        // What this symbol depends on
        var dependsOn = await _db.CodeDependencies
            .Where(d => d.FromSymbolId == symbol.Id)
            .Select(d => new DependencyResult
            {
                FromSymbol = symbol.Name,
                ToSymbol = d.ToSymbolName,
                DependencyType = d.DependencyType
            })
            .ToListAsync();

        // What depends on this symbol (used by)
        var usedBy = await _db.CodeDependencies
            .Where(d => d.ToSymbolId == symbol.Id || d.ToSymbolName == symbolName)
            .Join(_db.CodeSymbols, d => d.FromSymbolId, s => s.Id, (d, s) => new DependencyResult
            {
                FromSymbol = s.Name,
                ToSymbol = symbolName,
                DependencyType = d.DependencyType
            })
            .ToListAsync();

        return new DependencyTreeResult
        {
            Symbol = symbolName,
            DependsOn = dependsOn,
            UsedBy = usedBy
        };
    }

    public async Task<List<SearchResult>> SearchAsync(Guid projectId, string query, int limit = 20)
    {
        return await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId
                && (s.Name.Contains(query) || s.FullName.Contains(query)))
            .OrderByDescending(s => s.Name == query) // exact match first
            .ThenBy(s => s.SymbolType)
            .ThenBy(s => s.Name)
            .Take(limit)
            .Select(s => new SearchResult
            {
                Name = s.Name,
                FullName = s.FullName,
                SymbolType = s.SymbolType,
                FilePath = s.FilePath,
                LineNumber = s.LineNumber,
                ExtraInfo = s.ExtraInfo
            })
            .ToListAsync();
    }

    // ── Stats & Tree ──

    public async Task<ScanStats> GetStatsAsync(Guid projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        var symbols = _db.CodeSymbols.Where(s => s.ProjectId == projectId);

        return new ScanStats
        {
            ProjectId = projectId,
            ProjectName = project?.Name ?? "",
            TotalFiles = await symbols.Select(s => s.FilePath).Distinct().CountAsync(),
            Classes = await symbols.CountAsync(s => s.SymbolType == "Class"),
            Interfaces = await symbols.CountAsync(s => s.SymbolType == "Interface"),
            Methods = await symbols.CountAsync(s => s.SymbolType == "Method"),
            Properties = await symbols.CountAsync(s => s.SymbolType == "Property"),
            Endpoints = await _db.CodeApiEndpoints.CountAsync(e => e.ProjectId == projectId),
            Dependencies = await _db.CodeDependencies.CountAsync(d => d.ProjectId == projectId),
            LastScannedAt = project?.LastScannedAt
        };
    }

    public async Task<List<TreeNode>> GetTreeAsync(Guid projectId)
    {
        var symbols = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && s.ParentId == null)
            .OrderBy(s => s.FilePath).ThenBy(s => s.Name)
            .ToListAsync();

        var children = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && s.ParentId != null)
            .OrderBy(s => s.SymbolType).ThenBy(s => s.Name)
            .ToListAsync();

        var childLookup = children.GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Group by folder path
        var folderTree = new Dictionary<string, TreeNode>();
        var roots = new List<TreeNode>();

        foreach (var sym in symbols)
        {
            var parts = sym.FilePath.Split('/');
            var folderPath = string.Join("/", parts.Take(parts.Length - 1));
            var fileName = parts.Last();

            // Ensure folder nodes exist
            var currentPath = "";
            TreeNode? parentFolder = null;
            foreach (var part in parts.Take(parts.Length - 1))
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
                if (!folderTree.ContainsKey(currentPath))
                {
                    var folder = new TreeNode { Name = part, Type = "folder" };
                    folderTree[currentPath] = folder;
                    if (parentFolder == null)
                        roots.Add(folder);
                    else
                        parentFolder.Children.Add(folder);
                }
                parentFolder = folderTree[currentPath];
            }

            // File node
            var fileNode = new TreeNode
            {
                Name = fileName,
                Type = "file",
                FilePath = sym.FilePath,
            };

            // Symbol node under file
            var symNode = new TreeNode
            {
                Name = sym.Name,
                Type = sym.SymbolType.ToLower(),
                FilePath = sym.FilePath,
                LineNumber = sym.LineNumber
            };

            // Add children (methods, properties)
            if (childLookup.TryGetValue(sym.Id, out var symChildren))
            {
                symNode.Children = symChildren.Select(c => new TreeNode
                {
                    Name = c.ReturnType != null ? $"{c.ReturnType} {c.Name}" : c.Name,
                    Type = c.SymbolType.ToLower(),
                    FilePath = c.FilePath,
                    LineNumber = c.LineNumber
                }).ToList();
            }

            fileNode.Children.Add(symNode);

            if (parentFolder != null)
                parentFolder.Children.Add(fileNode);
            else
                roots.Add(fileNode);
        }

        return roots;
    }

    public async Task<GraphData> GetGraphAsync(Guid projectId, string? filter = null)
    {
        // Get type-level symbols (classes, interfaces, etc.) — not methods/properties
        var symbolsQuery = _db.CodeSymbols
            .Where(s => s.ProjectId == projectId
                && s.SymbolType != "Method" && s.SymbolType != "Property"
                && s.SymbolType != "Enum");

        if (!string.IsNullOrEmpty(filter))
            symbolsQuery = symbolsQuery.Where(s => s.Name.Contains(filter) || s.Namespace.Contains(filter) || s.FilePath.Contains(filter));

        var symbols = await symbolsQuery.ToListAsync();
        var symbolIds = symbols.Select(s => s.Id).ToHashSet();

        // Get dependencies between these symbols
        var deps = await _db.CodeDependencies
            .Where(d => d.ProjectId == projectId && symbolIds.Contains(d.FromSymbolId))
            .ToListAsync();

        // Count methods per class
        var methodCounts = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && s.SymbolType == "Method" && s.ParentId != null)
            .GroupBy(s => s.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count);

        // Build node type label (Controller, Service, Repository, etc.)
        string ClassifyNode(CodeSymbol s)
        {
            if (s.SymbolType == "Interface") return "Interface";
            if (s.Name.EndsWith("Controller")) return "Controller";
            if (s.Name.EndsWith("Service") || s.Name.EndsWith("Manager")) return "Service";
            if (s.Name.EndsWith("Repository") || s.Name.EndsWith("Repo")) return "Repository";
            if (s.Name.EndsWith("Hub")) return "Hub";
            if (s.Name.EndsWith("Consumer")) return "Consumer";
            if (s.Name.EndsWith("Dto") || s.Name.EndsWith("DTO") || s.Name.EndsWith("Model") || s.Name.EndsWith("Vm")) return "Model";
            if (s.BaseTypes != null && s.BaseTypes.Contains("BaseEntity")) return "Entity";
            return s.SymbolType;
        }

        var depCountPerSymbol = deps.GroupBy(d => d.FromSymbolId).ToDictionary(g => g.Key, g => g.Count());

        var nodes = symbols.Select(s => new GraphNode
        {
            Id = s.Id.ToString(),
            Name = s.Name,
            Type = ClassifyNode(s),
            FilePath = s.FilePath,
            LineNumber = s.LineNumber,
            MethodCount = methodCounts.GetValueOrDefault(s.Id),
            DependencyCount = depCountPerSymbol.GetValueOrDefault(s.Id)
        }).ToList();

        var nodeIds = nodes.Select(n => n.Id).ToHashSet();

        // Only include edges where both source and target are in the graph
        var edges = deps
            .Where(d => d.ToSymbolId.HasValue && nodeIds.Contains(d.ToSymbolId.Value.ToString()))
            .Select(d => new GraphEdge
            {
                Source = d.FromSymbolId.ToString(),
                Target = d.ToSymbolId!.Value.ToString(),
                Type = d.DependencyType
            })
            .ToList();

        return new GraphData { Nodes = nodes, Edges = edges };
    }

    public async Task<GraphData> GetModuleGraphAsync(Guid projectId)
    {
        var cacheKey = $"modules_{projectId}";
        lock (_graphCache)
        {
            if (_graphCache.TryGetValue(cacheKey, out var cached)
                && (DateTime.UtcNow - cached.CachedAt).TotalMinutes < 30)
                return cached.Data;
        }

        var pid = projectId.ToString();

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        // Module nodes
        var moduleRows = new List<ModuleRow>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT LEFT(FilePath, CHARINDEX('/', FilePath) - 1) AS Module, COUNT(*) AS Cnt
                FROM CodeSymbols
                WHERE ProjectId = @pid AND ParentId IS NULL
                  AND SymbolType NOT IN ('Method','Property','Field')
                  AND CHARINDEX('/', FilePath) > 0
                GROUP BY LEFT(FilePath, CHARINDEX('/', FilePath) - 1)
                ORDER BY COUNT(*) DESC";
            cmd.Parameters.Add(new SqlParameter("@pid", projectId));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                moduleRows.Add(new ModuleRow { Module = reader.GetString(0), Cnt = reader.GetInt32(1) });
        }

        var moduleNodes = moduleRows.Select(m => new GraphNode
        {
            Id = m.Module, Name = m.Module, Type = "Module", FilePath = m.Module, MethodCount = m.Cnt
        }).ToList();

        // Cross-module edges
        var edgeRows = new List<EdgeRow>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT
                  LEFT(sf.FilePath, CHARINDEX('/', sf.FilePath) - 1) AS Source,
                  LEFT(st.FilePath, CHARINDEX('/', st.FilePath) - 1) AS Target,
                  COUNT(*) AS Cnt
                FROM CodeDependencies d
                JOIN CodeSymbols sf ON d.FromSymbolId = sf.Id
                JOIN CodeSymbols st ON d.ToSymbolId = st.Id
                WHERE d.ProjectId = @pid
                  AND d.ToSymbolId IS NOT NULL
                  AND CHARINDEX('/', sf.FilePath) > 0
                  AND CHARINDEX('/', st.FilePath) > 0
                  AND LEFT(sf.FilePath, CHARINDEX('/', sf.FilePath) - 1)
                   <> LEFT(st.FilePath, CHARINDEX('/', st.FilePath) - 1)
                GROUP BY LEFT(sf.FilePath, CHARINDEX('/', sf.FilePath) - 1),
                         LEFT(st.FilePath, CHARINDEX('/', st.FilePath) - 1)
                HAVING COUNT(*) >= 2";
            cmd.Parameters.Add(new SqlParameter("@pid", projectId));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                edgeRows.Add(new EdgeRow { Source = reader.GetString(0), Target = reader.GetString(1), Cnt = reader.GetInt32(2) });
        }

        var moduleSet = moduleNodes.Select(n => n.Id).ToHashSet();
        var edges = edgeRows
            .Where(e => moduleSet.Contains(e.Source) && moduleSet.Contains(e.Target))
            .Select(e => new GraphEdge { Source = e.Source, Target = e.Target, Type = e.Cnt.ToString() })
            .ToList();

        foreach (var node in moduleNodes)
            node.DependencyCount = edges.Count(e => (e.Source as string) == node.Id);

        var result = new GraphData { Nodes = moduleNodes, Edges = edges };
        lock (_graphCache) { _graphCache[cacheKey] = (result, DateTime.UtcNow); }
        return result;
    }

    // ── Helpers ──

    private async Task<List<string>> BuildServiceChain(Guid classSymbolId)
    {
        var chain = new List<string>();
        var visited = new HashSet<Guid>();
        var currentId = classSymbolId;

        while (chain.Count < 5) // prevent infinite loops
        {
            if (!visited.Add(currentId)) break;

            var sym = await _db.CodeSymbols.FindAsync(currentId);
            if (sym == null) break;

            chain.Add(sym.Name);

            // Find first injection dependency that is a service/repo
            var dep = await _db.CodeDependencies
                .Where(d => d.FromSymbolId == currentId && d.DependencyType == "Injection" && d.ToSymbolId != null)
                .FirstOrDefaultAsync();

            if (dep?.ToSymbolId == null) break;
            currentId = dep.ToSymbolId.Value;
        }

        return chain;
    }

    private static SymbolResult MapSymbol(CodeSymbol s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        FullName = s.FullName,
        SymbolType = s.SymbolType,
        FilePath = s.FilePath,
        LineNumber = s.LineNumber,
        Namespace = s.Namespace,
        Accessibility = s.Accessibility,
        ReturnType = s.ReturnType,
        Parameters = s.Parameters,
        BaseTypes = s.BaseTypes,
        ExtraInfo = s.ExtraInfo
    };
}
