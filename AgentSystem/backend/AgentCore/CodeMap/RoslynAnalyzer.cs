using System.Text.Json;
using AgentInfrastructure.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AgentCore.CodeMap;

public class ScanResult
{
    public List<CodeSymbol> Symbols { get; set; } = new();
    public List<CodeApiEndpoint> Endpoints { get; set; } = new();
    public List<CodeDependency> Dependencies { get; set; } = new();
}

public class RoslynAnalyzer
{
    public ScanResult AnalyzeFile(string filePath, string rootPath, Guid projectId)
    {
        var result = new ScanResult();
        var code = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var relativePath = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
        var now = DateTime.UtcNow;

        // Handle enums separately (EnumDeclarationSyntax does not extend TypeDeclarationSyntax)
        foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            var enumNs = GetNamespace(enumDecl);
            var enumName = enumDecl.Identifier.Text;
            result.Symbols.Add(new CodeSymbol
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = enumName,
                FullName = string.IsNullOrEmpty(enumNs) ? enumName : $"{enumNs}.{enumName}",
                SymbolType = "Enum",
                FilePath = relativePath,
                LineNumber = GetLineNumber(enumDecl),
                Namespace = enumNs,
                Accessibility = GetAccessibility(enumDecl.Modifiers),
                ExtraInfo = ExtractEnumValues(enumDecl),
                ScannedAt = now
            });
        }

        foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var ns = GetNamespace(typeDecl);
            var typeName = typeDecl.Identifier.Text;
            var fullName = string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";

            var typeSymbol = new CodeSymbol
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = typeName,
                FullName = fullName,
                SymbolType = GetSymbolType(typeDecl),
                FilePath = relativePath,
                LineNumber = GetLineNumber(typeDecl),
                Namespace = ns,
                Accessibility = GetAccessibility(typeDecl.Modifiers),
                BaseTypes = GetBaseTypes(typeDecl),
                ExtraInfo = ExtractAttributes(typeDecl.AttributeLists),
                ScannedAt = now
            };

            result.Symbols.Add(typeSymbol);

            // Extract methods
            foreach (var method in typeDecl.Members.OfType<MethodDeclarationSyntax>())
            {
                var methodSymbol = new CodeSymbol
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Name = method.Identifier.Text,
                    FullName = $"{fullName}.{method.Identifier.Text}",
                    SymbolType = "Method",
                    FilePath = relativePath,
                    LineNumber = GetLineNumber(method),
                    Namespace = ns,
                    ParentId = typeSymbol.Id,
                    Accessibility = GetAccessibility(method.Modifiers),
                    ReturnType = method.ReturnType.ToString(),
                    Parameters = GetParameters(method.ParameterList),
                    ExtraInfo = ExtractAttributes(method.AttributeLists),
                    ScannedAt = now
                };
                result.Symbols.Add(methodSymbol);
            }

            // Extract properties
            foreach (var prop in typeDecl.Members.OfType<PropertyDeclarationSyntax>())
            {
                var propSymbol = new CodeSymbol
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Name = prop.Identifier.Text,
                    FullName = $"{fullName}.{prop.Identifier.Text}",
                    SymbolType = "Property",
                    FilePath = relativePath,
                    LineNumber = GetLineNumber(prop),
                    Namespace = ns,
                    ParentId = typeSymbol.Id,
                    Accessibility = GetAccessibility(prop.Modifiers),
                    ReturnType = prop.Type.ToString(),
                    ExtraInfo = ExtractAttributes(prop.AttributeLists),
                    ScannedAt = now
                };
                result.Symbols.Add(propSymbol);
            }

            // Extract const/static fields
            foreach (var field in typeDecl.Members.OfType<FieldDeclarationSyntax>())
            {
                var isConst = field.Modifiers.Any(SyntaxKind.ConstKeyword);
                var isStatic = field.Modifiers.Any(SyntaxKind.StaticKeyword);
                if (!isConst && !isStatic) continue;

                foreach (var variable in field.Declaration.Variables)
                {
                    var value = variable.Initializer?.Value.ToString();
                    result.Symbols.Add(new CodeSymbol
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = variable.Identifier.Text,
                        FullName = $"{fullName}.{variable.Identifier.Text}",
                        SymbolType = "Field",
                        FilePath = relativePath,
                        LineNumber = GetLineNumber(field),
                        Namespace = ns,
                        ParentId = typeSymbol.Id,
                        Accessibility = GetAccessibility(field.Modifiers),
                        ReturnType = field.Declaration.Type.ToString(),
                        ExtraInfo = value != null ? JsonSerializer.Serialize(new { value, isConst, isStatic }) : null,
                        ScannedAt = now
                    });
                }
            }

            // Extract constructor injection → dependencies
            foreach (var ctor in typeDecl.Members.OfType<ConstructorDeclarationSyntax>())
            {
                foreach (var param in ctor.ParameterList.Parameters)
                {
                    if (param.Type == null) continue;
                    result.Dependencies.Add(new CodeDependency
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        FromSymbolId = typeSymbol.Id,
                        ToSymbolName = param.Type.ToString(),
                        DependencyType = "Injection",
                        ScannedAt = now
                    });
                }
            }

            // Extract inheritance / implementation
            if (typeDecl.BaseList != null)
            {
                foreach (var baseType in typeDecl.BaseList.Types)
                {
                    var baseTypeName = baseType.Type.ToString();
                    var depType = baseTypeName.StartsWith("I") && baseTypeName.Length > 1 && char.IsUpper(baseTypeName[1])
                        ? "Implementation"
                        : "Inheritance";

                    result.Dependencies.Add(new CodeDependency
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        FromSymbolId = typeSymbol.Id,
                        ToSymbolName = baseTypeName,
                        DependencyType = depType,
                        ScannedAt = now
                    });
                }
            }

            // Extract API endpoints from controller attributes
            ExtractEndpoints(typeDecl, typeSymbol, relativePath, projectId, now, result);
        }

        return result;
    }

    private void ExtractEndpoints(
        TypeDeclarationSyntax typeDecl, CodeSymbol typeSymbol,
        string filePath, Guid projectId, DateTime now, ScanResult result)
    {
        // Check if this is a controller
        var isController = typeDecl.Identifier.Text.EndsWith("Controller")
            || HasAttribute(typeDecl, "ApiController");
        if (!isController) return;

        // Get base route from [Route] attribute
        var baseRoute = GetAttributeArgument(typeDecl, "Route") ?? "";

        // Replace [controller] placeholder
        var controllerName = typeDecl.Identifier.Text;
        var shortName = controllerName.EndsWith("Controller")
            ? controllerName[..^"Controller".Length]
            : controllerName;
        baseRoute = baseRoute.Replace("[controller]", shortName, StringComparison.OrdinalIgnoreCase);

        foreach (var method in typeDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            var httpMethods = GetHttpMethods(method);
            if (httpMethods.Count == 0) continue;

            var methodSymbol = result.Symbols
                .FirstOrDefault(s => s.ParentId == typeSymbol.Id && s.Name == method.Identifier.Text);

            foreach (var (httpMethod, routeSuffix) in httpMethods)
            {
                var fullRoute = CombineRoute(baseRoute, routeSuffix);

                result.Endpoints.Add(new CodeApiEndpoint
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    HttpMethod = httpMethod,
                    RouteTemplate = fullRoute,
                    ControllerName = controllerName,
                    ActionName = method.Identifier.Text,
                    SymbolId = methodSymbol?.Id,
                    FilePath = filePath,
                    LineNumber = GetLineNumber(method),
                    ScannedAt = now
                });
            }
        }
    }

    private static List<(string Method, string Route)> GetHttpMethods(MethodDeclarationSyntax method)
    {
        var results = new List<(string, string)>();
        var httpAttrs = new[] { "HttpGet", "HttpPost", "HttpPut", "HttpDelete", "HttpPatch" };

        foreach (var attrList in method.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var attrName = attr.Name.ToString();
                foreach (var httpAttr in httpAttrs)
                {
                    if (!attrName.Equals(httpAttr, StringComparison.OrdinalIgnoreCase)
                        && !attrName.Equals(httpAttr + "Attribute", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var route = "";
                    if (attr.ArgumentList?.Arguments.Count > 0)
                    {
                        var arg = attr.ArgumentList.Arguments[0].ToString().Trim('"');
                        route = arg;
                    }
                    results.Add((httpAttr.Replace("Http", "").ToUpper(), route));
                }
            }
        }

        return results;
    }

    private static string CombineRoute(string baseRoute, string suffix)
    {
        baseRoute = baseRoute.Trim('/');
        suffix = suffix.Trim('/');
        if (string.IsNullOrEmpty(suffix)) return baseRoute;
        if (string.IsNullOrEmpty(baseRoute)) return suffix;
        return $"{baseRoute}/{suffix}";
    }

    private static string GetSymbolType(TypeDeclarationSyntax typeDecl) => typeDecl switch
    {
        ClassDeclarationSyntax => "Class",
        InterfaceDeclarationSyntax => "Interface",
        StructDeclarationSyntax => "Struct",
        RecordDeclarationSyntax => "Record",
        _ => "Type"
    };

    private static string GetNamespace(SyntaxNode node)
    {
        // File-scoped namespace
        var fileScopedNs = node.SyntaxTree.GetRoot().DescendantNodes()
            .OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScopedNs != null) return fileScopedNs.Name.ToString();

        // Block-scoped namespace
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is NamespaceDeclarationSyntax ns)
                return ns.Name.ToString();
            parent = parent.Parent;
        }
        return string.Empty;
    }

    private static string GetAccessibility(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword)) return "public";
        if (modifiers.Any(SyntaxKind.PrivateKeyword)) return "private";
        if (modifiers.Any(SyntaxKind.ProtectedKeyword)) return "protected";
        if (modifiers.Any(SyntaxKind.InternalKeyword)) return "internal";
        return "internal"; // C# default
    }

    private static string? GetBaseTypes(TypeDeclarationSyntax typeDecl)
    {
        if (typeDecl.BaseList == null || !typeDecl.BaseList.Types.Any()) return null;
        return string.Join(", ", typeDecl.BaseList.Types.Select(t => t.Type.ToString()));
    }

    private static string? GetParameters(ParameterListSyntax paramList)
    {
        if (!paramList.Parameters.Any()) return null;
        return string.Join(", ", paramList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
    }

    private static int GetLineNumber(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static bool HasAttribute(TypeDeclarationSyntax typeDecl, string attributeName) =>
        typeDecl.AttributeLists.SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Equals(attributeName, StringComparison.OrdinalIgnoreCase)
                    || a.Name.ToString().Equals(attributeName + "Attribute", StringComparison.OrdinalIgnoreCase));

    private static string? GetAttributeArgument(TypeDeclarationSyntax typeDecl, string attributeName)
    {
        var attr = typeDecl.AttributeLists.SelectMany(al => al.Attributes)
            .FirstOrDefault(a => a.Name.ToString().Equals(attributeName, StringComparison.OrdinalIgnoreCase)
                              || a.Name.ToString().Equals(attributeName + "Attribute", StringComparison.OrdinalIgnoreCase));

        if (attr?.ArgumentList?.Arguments.Count > 0)
            return attr.ArgumentList.Arguments[0].ToString().Trim('"');

        return null;
    }

    /// <summary>Extract enum member names, values, and [Description] attributes</summary>
    private static string? ExtractEnumValues(EnumDeclarationSyntax enumDecl)
    {
        var members = enumDecl.Members;
        if (!members.Any()) return null;

        var values = new List<object>();
        foreach (var member in members)
        {
            var name = member.Identifier.Text;
            var value = member.EqualsValue?.Value.ToString();
            var desc = GetDescriptionFromAttributes(member.AttributeLists);

            var entry = new Dictionary<string, object?> { ["name"] = name };
            if (value != null) entry["value"] = value;
            if (desc != null) entry["description"] = desc;
            values.Add(entry);
        }

        return JsonSerializer.Serialize(values);
    }

    /// <summary>Extract all attributes as JSON array: [{"name":"Route","args":"api/[controller]"}, ...]</summary>
    private static string? ExtractAttributes(SyntaxList<AttributeListSyntax> attributeLists)
    {
        var attrs = attributeLists.SelectMany(al => al.Attributes).ToList();
        if (!attrs.Any()) return null;

        var result = new List<object>();
        foreach (var attr in attrs)
        {
            var name = attr.Name.ToString();
            // Skip common noisy attributes
            if (name is "Serializable" or "CompilerGenerated") continue;

            var entry = new Dictionary<string, string> { ["name"] = name };
            if (attr.ArgumentList?.Arguments.Count > 0)
            {
                var args = string.Join(", ", attr.ArgumentList.Arguments.Select(a => a.ToString()));
                entry["args"] = args;
            }
            result.Add(entry);
        }

        return result.Any() ? JsonSerializer.Serialize(result) : null;
    }

    /// <summary>Get [Description("...")] value from attribute lists</summary>
    private static string? GetDescriptionFromAttributes(SyntaxList<AttributeListSyntax> attributeLists)
    {
        var descAttr = attributeLists.SelectMany(al => al.Attributes)
            .FirstOrDefault(a =>
            {
                var n = a.Name.ToString();
                return n is "Description" or "DescriptionAttribute"
                    or "System.ComponentModel.Description"
                    or "System.ComponentModel.DescriptionAttribute";
            });

        if (descAttr?.ArgumentList?.Arguments.Count > 0)
            return descAttr.ArgumentList.Arguments[0].ToString().Trim('"');

        return null;
    }
}
