using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.SourceControl;

/// <summary>
/// Đọc đệ quy tất cả *.sql trong 1 folder (source control) và build DatabaseSchema
/// best-effort để SchemaComparer dùng — tái sử dụng cơ chế snapshot override.
///
/// GIỚI HẠN (parser nhẹ, không phải full T-SQL parser):
///  - Table: chỉ so sánh được column name + type text + nullable + identity + default
///    (PK/FK/index/check constraint KHÔNG được parse → bảng có thể báo Different
///    nếu phía DB live có các thành phần này).
///  - Programmable (View/SP/Function/Trigger): so sánh bằng definition text = nguyên văn file.
///  - Synonym/Sequence: bỏ qua (xem code) vì khó parse an toàn.
///  - File có nhiều câu CREATE: chỉ nhận câu CREATE đầu tiên.
///  - File không nhận diện được → bỏ qua (không ném lỗi).
/// </summary>
public sealed class ScriptFolderReader
{
    /// <summary>Đọc folder → DatabaseSchema. folderPath rỗng/không tồn tại → ném lỗi rõ ràng.</summary>
    public DatabaseSchema Read(string folderPath)
    {
        var schema = new DatabaseSchema();
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new System.ArgumentException("Folder path is empty.", nameof(folderPath));
        if (!System.IO.Directory.Exists(folderPath))
            throw new System.IO.DirectoryNotFoundException($"Folder not found: {folderPath}");

        IEnumerable<string> files = System.IO.Directory.EnumerateFiles(
            folderPath, "*.sql", System.IO.SearchOption.AllDirectories);

        foreach (string file in files)
        {
            string text;
            try { text = System.IO.File.ReadAllText(file); }
            catch { continue; } // file lỗi đọc → bỏ qua, không làm hỏng cả lần đọc

            try { ProcessFile(schema, text); }
            catch { /* parse 1 file lỗi → bỏ qua file đó */ }
        }

        return schema;
    }

    /// <summary>Nhận diện 1 file + đưa object vào schema. Không nhận diện được → bỏ qua.</summary>
    private static void ProcessFile(DatabaseSchema schema, string text)
    {
        ScriptObjectParser.ParsedObject? obj = ScriptObjectParser.Detect(text);
        if (obj == null) return;

        string key = $"{obj.Schema}.{obj.Name}";

        switch (obj.Keyword)
        {
            case "TABLE":
                var table = new TableDef
                {
                    Schema = obj.Schema,
                    Name = obj.Name,
                    Columns = ScriptObjectParser.ParseColumns(text),
                };
                schema.Tables[key] = table; // file sau ghi đè file trùng key (best-effort)
                break;

            case "VIEW":
                AddProgrammable(schema, obj, DbObjectType.View);
                break;

            case "PROCEDURE":
            case "PROC":
                AddProgrammable(schema, obj, DbObjectType.StoredProcedure);
                break;

            case "FUNCTION":
                AddProgrammable(schema, obj, DbObjectType.Function);
                break;

            case "TRIGGER":
                AddProgrammable(schema, obj, DbObjectType.Trigger);
                break;

            // SYNONYM/SEQUENCE: bỏ qua — parser nhẹ không bảo đảm chính xác base object / option.
            case "SYNONYM":
            case "SEQUENCE":
            default:
                break;
        }
    }

    /// <summary>Thêm 1 object dạng code; Definition = nguyên văn file (giữ CREATE OR ALTER).</summary>
    private static void AddProgrammable(DatabaseSchema schema, ScriptObjectParser.ParsedObject obj, DbObjectType type)
    {
        schema.Programmables[$"{obj.Schema}.{obj.Name}"] = new ProgrammableObject
        {
            Schema = obj.Schema,
            Name = obj.Name,
            ObjectType = type,
            Definition = obj.FullText,
        };
    }
}
