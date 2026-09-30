using SqlCompareSuite.Core.Data;
using SqlCompareSuite.Core.Execution;
using SqlCompareSuite.Core.Models;
using SqlCompareSuite.Core.Reporting;
using SqlCompareSuite.Core.Schema;

// CLI clone Redgate SQL Compare / Data Compare cho CI/CD.
// Exit code: 0 = OK (identical/sync thành công); 1 = có khác biệt mà không /sync; 2 = lỗi.

try
{
    // Lệnh con = arg đầu tiên
    if (args.Length == 0)
    {
        PrintHelp();
        return 0;
    }

    var command = args[0].ToLowerInvariant();
    var opts = ParseArgs(args.Skip(1));

    return command switch
    {
        "schema" => RunSchema(opts),
        "data" => RunData(opts),
        _ => PrintHelpReturn(),
    };
}
catch (Exception ex)
{
    // Bọc try/catch toàn cục -> stderr + exit 2
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 2;
}

// ====================== SCHEMA COMMAND ======================

int RunSchema(Dictionary<string, string> o)
{
    var source = BuildConn(o, "1");
    var target = BuildConn(o, "2");

    Console.WriteLine($"Schema compare: {source} (source) -> {target} (target)");

    var reader = new SchemaReader();
    var srcSchema = reader.Read(source);
    var tgtSchema = reader.Read(target);

    var result = new SchemaComparer().Compare(srcSchema, tgtSchema);

    // In summary counts
    Console.WriteLine("--- Summary ---");
    Console.WriteLine($"Only in source : {result.CountOnlyInSource}");
    Console.WriteLine($"Only in target : {result.CountOnlyInTarget}");
    Console.WriteLine($"Different      : {result.CountDifferent}");
    Console.WriteLine($"Identical      : {result.CountIdentical}");
    Console.WriteLine($"Total objects  : {result.Items.Count}");

    var differs = result.CountOnlyInSource + result.CountOnlyInTarget + result.CountDifferent > 0;

    // Sinh sync script (cần cho /output hoặc /sync)
    var script = new SchemaScriptGenerator().GenerateSyncScript(srcSchema, tgtSchema, result.Items);

    // /output -> ghi script ra file
    if (o.TryGetValue("output", out var outPath))
    {
        WriteFile(outPath, script);
        Console.WriteLine($"Sync script written to: {outPath}");
    }

    // /report -> ghi report theo đuôi file
    if (o.TryGetValue("report", out var reportPath))
    {
        var content = SchemaReportByExt(reportPath, result, source.ToString(), target.ToString());
        WriteFile(reportPath, content);
        Console.WriteLine($"Report written to: {reportPath}");
    }

    // /sync -> thực thi script đồng bộ target <- source
    if (o.ContainsKey("sync"))
    {
        Console.WriteLine("Executing sync script on target...");
        var exec = new SqlScriptExecutor().Execute(target, script);
        Console.WriteLine(exec.Message);
        if (!exec.Success)
            return 2;
        Console.WriteLine($"Batches executed: {exec.BatchesExecuted}");
        return 0; // sync thành công
    }

    // Không /sync: có khác biệt -> exit 1 (CI gate), giống hệt -> 0
    return differs ? 1 : 0;
}

// ====================== DATA COMMAND ======================

int RunData(Dictionary<string, string> o)
{
    var source = BuildConn(o, "1");
    var target = BuildConn(o, "2");

    if (!o.TryGetValue("table", out var table) || string.IsNullOrWhiteSpace(table))
        throw new ArgumentException("Thiếu /table:schema.Table cho lệnh data.");

    Console.WriteLine($"Data compare: {source} (source) -> {target} (target), table {table}");

    var svc = new DataCompareService();
    var result = svc.Compare(source, target, table);

    // In summary counts
    Console.WriteLine("--- Summary ---");
    Console.WriteLine($"Table          : {result.TableKey}");
    Console.WriteLine($"Key columns    : {string.Join(", ", result.KeyColumns)} ({result.KeyKind})");
    Console.WriteLine($"Only in source : {result.CountOnlyInSource}");
    Console.WriteLine($"Only in target : {result.CountOnlyInTarget}");
    Console.WriteLine($"Different      : {result.CountDifferent}");
    Console.WriteLine($"Identical      : {result.CountIdentical}");

    var differs = result.CountOnlyInSource + result.CountOnlyInTarget + result.CountDifferent > 0;

    // Sinh sync script cho mọi row (cần cho /output hoặc /sync)
    var script = svc.GenerateSyncScript(target, result, result.Rows);

    // /output -> ghi script ra file
    if (o.TryGetValue("output", out var outPath))
    {
        WriteFile(outPath, script);
        Console.WriteLine($"Sync script written to: {outPath}");
    }

    // /report -> ghi report theo đuôi file (html / csv)
    if (o.TryGetValue("report", out var reportPath))
    {
        var content = DataReportByExt(reportPath, result);
        WriteFile(reportPath, content);
        Console.WriteLine($"Report written to: {reportPath}");
    }

    // /sync -> thực thi script
    if (o.ContainsKey("sync"))
    {
        Console.WriteLine("Executing sync script on target...");
        var exec = new SqlScriptExecutor().Execute(target, script);
        Console.WriteLine(exec.Message);
        if (!exec.Success)
            return 2;
        Console.WriteLine($"Batches executed: {exec.BatchesExecuted}");
        return 0;
    }

    return differs ? 1 : 0;
}

// ====================== HELPERS ======================

// Build DbConnectionInfo từ /serverN /dbN /userN /pwN. Không có userN -> Windows auth.
DbConnectionInfo BuildConn(Dictionary<string, string> o, string n)
{
    var server = Get(o, $"server{n}") ?? throw new ArgumentException($"Thiếu /server{n}:...");
    var db = Get(o, $"db{n}") ?? throw new ArgumentException($"Thiếu /db{n}:...");
    var user = Get(o, $"user{n}");
    var pw = Get(o, $"pw{n}");

    var conn = new DbConnectionInfo
    {
        Server = server,
        Database = db,
        IntegratedSecurity = string.IsNullOrEmpty(user), // không có user -> Windows auth
        UserId = user ?? "",
        Password = pw ?? "",
        TrustServerCertificate = true,
    };
    return conn;
}

// Suy ra report schema theo đuôi file: .xml / .csv / mặc định .html
string SchemaReportByExt(string path, SchemaComparisonResult result, string src, string tgt)
{
    var ext = Path.GetExtension(path).ToLowerInvariant();
    return ext switch
    {
        ".xml" => SchemaReport.ToXml(result),
        ".csv" => SchemaReport.ToCsv(result),
        _ => SchemaReport.ToHtml(result, src, tgt),
    };
}

// Suy ra report data theo đuôi file: .csv / mặc định .html
string DataReportByExt(string path, DataComparisonResult result)
{
    var ext = Path.GetExtension(path).ToLowerInvariant();
    return ext switch
    {
        ".csv" => DataReport.ToCsv(result),
        _ => DataReport.ToHtml(result),
    };
}

// Ghi file, tạo thư mục cha nếu chưa có
void WriteFile(string path, string content)
{
    var dir = Path.GetDirectoryName(Path.GetFullPath(path));
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    File.WriteAllText(path, content);
}

string? Get(Dictionary<string, string> o, string key)
    => o.TryGetValue(key, out var v) ? v : null;

// Parse args dạng /key:value và cờ /key (-> "true"). Chỉ split ở dấu ':' ĐẦU TIÊN.
Dictionary<string, string> ParseArgs(IEnumerable<string> rawArgs)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var a in rawArgs)
    {
        if (string.IsNullOrWhiteSpace(a) || !a.StartsWith('/')) continue;
        var body = a[1..]; // bỏ '/'
        var idx = body.IndexOf(':');
        if (idx < 0)
            dict[body] = "true"; // cờ không value
        else
            dict[body[..idx]] = body[(idx + 1)..]; // value có thể chứa ':' khác
    }
    return dict;
}

int PrintHelpReturn() { PrintHelp(); return 0; }

void PrintHelp()
{
    Console.WriteLine(@"sqlcompare - SQL Schema/Data compare CLI (cho CI/CD)

USAGE:
  sqlcompare schema /server1:A /db1:DbA /server2:B /db2:DbB [options]
  sqlcompare data   /server1:A /db1:DbA /server2:B /db2:DbB /table:dbo.Customer [options]

COMMON OPTIONS:
  /server1 /db1            Source server + database (left)
  /server2 /db2            Target server + database (right)
  /user1 /pw1             SQL auth cho source (không có -> Windows auth)
  /user2 /pw2             SQL auth cho target (không có -> Windows auth)
  /sync                    Thực thi sync script: target <- source
  /output:path.sql         Ghi sync script ra file
  /report:path.html|xml|csv  Ghi report (format suy theo đuôi file)

DATA-ONLY:
  /table:schema.Table      Bảng cần so sánh data (bắt buộc cho lệnh data)

REPORT FORMAT:
  schema: .html (mặc định) | .xml | .csv
  data  : .html (mặc định) | .csv

EXIT CODE:
  0  identical, hoặc /sync thành công
  1  có khác biệt mà không /sync (CI gate)
  2  lỗi

EXAMPLES:
  sqlcompare schema /server1:SRV1 /db1:Shop_Dev /server2:SRV2 /db2:Shop_Prod /report:diff.html
  sqlcompare schema /server1:SRV1 /db1:Shop_Dev /server2:SRV2 /db2:Shop_Prod /output:sync.sql /sync
  sqlcompare data /server1:SRV1 /db1:Shop_Dev /server2:SRV2 /db2:Shop_Prod /table:dbo.Customer /report:rows.csv");
}
