using System.Text.RegularExpressions;
using AgentInfrastructure.Entities;

namespace AgentCore.CodeMap;

/// <summary>
/// Regex-based TypeScript/Angular analyzer.
/// Extracts components, services, interfaces, classes, methods from .ts files.
/// </summary>
public class TypeScriptAnalyzer
{
    // Patterns
    private static readonly Regex ClassRegex = new(@"^export\s+(abstract\s+)?class\s+(\w+)(?:\s+extends\s+(\w+))?(?:\s+implements\s+([\w,\s]+))?\s*\{",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex InterfaceRegex = new(@"^export\s+interface\s+(\w+)(?:\s+extends\s+([\w,\s]+))?\s*\{",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex EnumRegex = new(@"^export\s+enum\s+(\w+)\s*\{",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex MethodRegex = new(@"^\s+(?:async\s+)?(public\s+|private\s+|protected\s+)?(\w+)\s*\(([^)]*)\)\s*(?::\s*([^\{]+))?\s*\{",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex PropertyRegex = new(@"^\s+(?:public\s+|private\s+|protected\s+|readonly\s+)*(\w+)(?:\?)?:\s*([^;=]+)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ComponentRegex = new(@"@Component\(\{[^}]*selector:\s*'([^']+)'",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex InjectableRegex = new(@"@Injectable\(",
        RegexOptions.Compiled);

    private static readonly Regex ConstructorRegex = new(@"constructor\(\s*([^)]*)\)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CtorParamRegex = new(@"(?:private|public|protected|readonly)\s+(\w+):\s*(\w+)",
        RegexOptions.Compiled);

    public ScanResult AnalyzeFile(string filePath, string rootPath, Guid projectId)
    {
        var result = new ScanResult();
        var code = File.ReadAllText(filePath);
        var lines = code.Split('\n');
        var relativePath = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
        var now = DateTime.UtcNow;

        // Detect component selector
        var componentMatch = ComponentRegex.Match(code);
        var isComponent = componentMatch.Success;
        var isService = InjectableRegex.IsMatch(code);

        // Extract classes
        foreach (Match m in ClassRegex.Matches(code))
        {
            var className = m.Groups[2].Value;
            var baseClass = m.Groups[3].Success ? m.Groups[3].Value : null;
            var interfaces = m.Groups[4].Success ? m.Groups[4].Value.Trim() : null;
            var lineNum = GetLineNumber(code, m.Index);

            var baseTypes = new List<string>();
            if (baseClass != null) baseTypes.Add(baseClass);
            if (interfaces != null) baseTypes.AddRange(interfaces.Split(',').Select(x => x.Trim()));

            var typeLabel = isComponent ? "Component" : isService ? "Service" : "Class";

            var classSymbol = new CodeSymbol
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = className,
                FullName = className,
                SymbolType = typeLabel,
                FilePath = relativePath,
                LineNumber = lineNum,
                Namespace = ExtractNamespace(relativePath),
                Accessibility = "public",
                BaseTypes = baseTypes.Any() ? string.Join(", ", baseTypes) : null,
                ScannedAt = now
            };
            result.Symbols.Add(classSymbol);

            // Extract methods within this class block
            var classBlock = ExtractBlock(code, m.Index + m.Length - 1);
            ExtractMembers(classBlock, m.Index, code, classSymbol, relativePath, projectId, now, result);

            // Extract constructor injection
            var ctorMatch = ConstructorRegex.Match(classBlock);
            if (ctorMatch.Success)
            {
                foreach (Match cp in CtorParamRegex.Matches(ctorMatch.Groups[1].Value))
                {
                    result.Dependencies.Add(new CodeDependency
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        FromSymbolId = classSymbol.Id,
                        ToSymbolName = cp.Groups[2].Value,
                        DependencyType = "Injection",
                        ScannedAt = now
                    });
                }
            }

            // Inheritance/implementation
            foreach (var bt in baseTypes)
            {
                result.Dependencies.Add(new CodeDependency
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    FromSymbolId = classSymbol.Id,
                    ToSymbolName = bt,
                    DependencyType = bt.StartsWith("I") && bt.Length > 1 && char.IsUpper(bt[1]) ? "Implementation" : "Inheritance",
                    ScannedAt = now
                });
            }
        }

        // Extract interfaces
        foreach (Match m in InterfaceRegex.Matches(code))
        {
            var ifName = m.Groups[1].Value;
            result.Symbols.Add(new CodeSymbol
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = ifName,
                FullName = ifName,
                SymbolType = "Interface",
                FilePath = relativePath,
                LineNumber = GetLineNumber(code, m.Index),
                Namespace = ExtractNamespace(relativePath),
                Accessibility = "public",
                BaseTypes = m.Groups[2].Success ? m.Groups[2].Value.Trim() : null,
                ScannedAt = now
            });
        }

        // Extract enums
        foreach (Match m in EnumRegex.Matches(code))
        {
            result.Symbols.Add(new CodeSymbol
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = m.Groups[1].Value,
                FullName = m.Groups[1].Value,
                SymbolType = "Enum",
                FilePath = relativePath,
                LineNumber = GetLineNumber(code, m.Index),
                Namespace = ExtractNamespace(relativePath),
                Accessibility = "public",
                ScannedAt = now
            });
        }

        return result;
    }

    private void ExtractMembers(string block, int blockStartOffset, string fullCode,
        CodeSymbol parent, string filePath, Guid projectId, DateTime now, ScanResult result)
    {
        // Methods
        foreach (Match m in MethodRegex.Matches(block))
        {
            var name = m.Groups[2].Value;
            if (name is "constructor" or "if" or "for" or "while" or "switch" or "catch" or "return") continue;

            result.Symbols.Add(new CodeSymbol
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = name,
                FullName = $"{parent.Name}.{name}",
                SymbolType = "Method",
                FilePath = filePath,
                LineNumber = GetLineNumber(fullCode, blockStartOffset + m.Index),
                Namespace = parent.Namespace,
                ParentId = parent.Id,
                Accessibility = m.Groups[1].Success ? m.Groups[1].Value.Trim() : "public",
                ReturnType = m.Groups[4].Success ? m.Groups[4].Value.Trim() : "void",
                Parameters = m.Groups[3].Value.Trim().Length > 0 ? m.Groups[3].Value.Trim() : null,
                ScannedAt = now
            });
        }
    }

    private static string ExtractBlock(string code, int openBraceIndex)
    {
        var depth = 0;
        var start = openBraceIndex;
        for (var i = openBraceIndex; i < code.Length; i++)
        {
            if (code[i] == '{') depth++;
            else if (code[i] == '}') { depth--; if (depth == 0) return code[start..(i + 1)]; }
        }
        return code[start..];
    }

    private static int GetLineNumber(string code, int charIndex)
    {
        var line = 1;
        for (var i = 0; i < charIndex && i < code.Length; i++)
            if (code[i] == '\n') line++;
        return line;
    }

    private static string ExtractNamespace(string filePath)
    {
        // Use folder path as namespace: "src/app/services/order.service.ts" → "app/services"
        var parts = filePath.Split('/');
        if (parts.Length <= 2) return "";
        return string.Join("/", parts.Skip(1).Take(parts.Length - 2));
    }
}
