using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AgentCore.Embedding;

public class CodeChunk
{
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string ChunkType { get; set; } = string.Empty;
    public string? SymbolName { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
}

/// <summary>
/// Chunk source files cho embedding.
/// - .cs: dùng Roslyn, chunk theo class + method (có ngữ nghĩa)
/// - .ts/.tsx/.js/.html/.scss: line-window 40 dòng, overlap 8
/// </summary>
public class CodeChunker
{
    private const int WindowSize = 40;
    private const int Overlap = 8;
    private const int MaxChunkChars = 4000; // tránh chunk quá to vượt context tokenizer

    private static readonly string[] SupportedExt = { ".cs", ".ts", ".tsx", ".js", ".html", ".scss" };

    public IEnumerable<string> DiscoverFiles(string rootPath)
    {
        if (!Directory.Exists(rootPath)) yield break;

        foreach (var file in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (!SupportedExt.Contains(ext)) continue;
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            if (file.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}")) continue;
            if (file.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}")) continue;
            yield return file;
        }
    }

    public List<CodeChunk> ChunkFile(string filePath, string rootPath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var relPath = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');

        string content;
        try { content = File.ReadAllText(filePath); }
        catch { return new List<CodeChunk>(); }

        if (string.IsNullOrWhiteSpace(content)) return new List<CodeChunk>();

        return ext == ".cs"
            ? ChunkCSharp(relPath, content)
            : ChunkLineWindow(relPath, content);
    }

    // ── .cs: Roslyn-based, chunk theo method/class ──

    private List<CodeChunk> ChunkCSharp(string relPath, string source)
    {
        var chunks = new List<CodeChunk>();
        SyntaxNode root;
        try
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            root = tree.GetRoot();
        }
        catch
        {
            return ChunkLineWindow(relPath, source);
        }

        var lines = source.Split('\n');

        // Method/Constructor — chunk riêng từng cái
        var methods = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>().ToList();
        var methodSpans = new HashSet<int>();

        foreach (var method in methods)
        {
            var span = method.GetLocation().GetLineSpan();
            var startLine = span.StartLinePosition.Line;
            var endLine = span.EndLinePosition.Line;
            var text = method.ToFullString().Trim();
            if (text.Length < 20) continue;
            if (text.Length > MaxChunkChars) text = text.Substring(0, MaxChunkChars);

            var symbolName = GetMethodSymbolName(method);

            chunks.Add(new CodeChunk
            {
                FilePath = relPath,
                StartLine = startLine + 1,
                EndLine = endLine + 1,
                ChunkType = "method",
                SymbolName = symbolName,
                Content = text,
                ContentHash = Sha256(text),
            });

            for (int i = startLine; i <= endLine; i++) methodSpans.Add(i);
        }

        // Class/Interface/Record header + field — chunk phần không phải method
        var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>().ToList();
        foreach (var type in types)
        {
            var span = type.GetLocation().GetLineSpan();
            var startLine = span.StartLinePosition.Line;
            var endLine = span.EndLinePosition.Line;

            // lấy các dòng thuộc class nhưng không nằm trong method nào
            var sb = new StringBuilder();
            for (int i = startLine; i <= endLine && i < lines.Length; i++)
            {
                if (methodSpans.Contains(i)) continue;
                sb.AppendLine(lines[i]);
            }
            var text = sb.ToString().Trim();
            if (text.Length < 20) continue;
            if (text.Length > MaxChunkChars) text = text.Substring(0, MaxChunkChars);

            chunks.Add(new CodeChunk
            {
                FilePath = relPath,
                StartLine = startLine + 1,
                EndLine = endLine + 1,
                ChunkType = "class",
                SymbolName = type.Identifier.Text,
                Content = text,
                ContentHash = Sha256(text),
            });
        }

        // File không có class nào (vd Program.cs top-level) → fallback line-window
        if (chunks.Count == 0) return ChunkLineWindow(relPath, source);

        return chunks;
    }

    private static string GetMethodSymbolName(BaseMethodDeclarationSyntax method)
    {
        var owner = method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "?";
        var name = method switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text,
            ConstructorDeclarationSyntax c => c.Identifier.Text,
            _ => "?"
        };
        return $"{owner}.{name}";
    }

    // ── .ts/.html/etc: line-window ──

    private List<CodeChunk> ChunkLineWindow(string relPath, string source)
    {
        var chunks = new List<CodeChunk>();
        var lines = source.Split('\n');
        if (lines.Length == 0) return chunks;

        var step = Math.Max(1, WindowSize - Overlap);

        for (int start = 0; start < lines.Length; start += step)
        {
            var end = Math.Min(start + WindowSize, lines.Length);
            var content = string.Join("\n", lines, start, end - start).Trim();
            if (content.Length < 20) { if (end >= lines.Length) break; continue; }
            if (content.Length > MaxChunkChars) content = content.Substring(0, MaxChunkChars);

            chunks.Add(new CodeChunk
            {
                FilePath = relPath,
                StartLine = start + 1,
                EndLine = end,
                ChunkType = "block",
                SymbolName = null,
                Content = content,
                ContentHash = Sha256(content),
            });

            if (end >= lines.Length) break;
        }

        return chunks;
    }

    private static string Sha256(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes);
    }
}
