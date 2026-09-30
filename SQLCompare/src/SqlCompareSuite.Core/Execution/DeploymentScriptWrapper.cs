using System.Text;

namespace SqlCompareSuite.Core.Execution;

/// <summary>
/// Bọc script đồng bộ thô (các batch tách bằng dòng "GO") thành script deploy
/// đúng phong cách Redgate SQL Compare: header comment + SET options + transaction
/// + mỗi batch có guard "IF @@ERROR &lt;&gt; 0 SET NOEXEC ON" + PRINT tiến trình
/// + footer @Success / ROLLBACK.
/// </summary>
public static class DeploymentScriptWrapper
{
    /// <param name="rawScript">Script thô: các câu lệnh tách bằng dòng GO (KHÔNG tự bọc transaction).</param>
    /// <param name="targetName">DB đích (sẽ bị sửa), vd "10.0.63.68.SCRM_Order_Logistic".</param>
    /// <param name="sourceName">DB nguồn để đồng bộ theo.</param>
    /// <param name="timestamp">Thời điểm tạo (caller truyền, vd DateTime.Now string).</param>
    /// <param name="progressPrints">Có chèn PRINT N'...' trước mỗi batch không.</param>
    public static string Wrap(
        string rawScript, string targetName, string sourceName,
        string timestamp, bool progressPrints = true)
    {
        var sb = new StringBuilder();

        // ----- Header comment -----
        sb.AppendLine("/*");
        sb.AppendLine("Run this script on:");
        sb.AppendLine();
        sb.AppendLine($"        {targetName}    -  This database will be modified");
        sb.AppendLine();
        sb.AppendLine("to synchronize it with:");
        sb.AppendLine();
        sb.AppendLine($"        {sourceName}");
        sb.AppendLine();
        sb.AppendLine("You are recommended to back up your database before running this script");
        sb.AppendLine();
        sb.AppendLine($"Script created by SQL Compare Suite at {timestamp}");
        sb.AppendLine("*/");

        // ----- SET options + mở transaction -----
        sb.AppendLine("SET NUMERIC_ROUNDABORT OFF");
        sb.AppendLine("GO");
        sb.AppendLine("SET ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON");
        sb.AppendLine("GO");
        sb.AppendLine("SET XACT_ABORT ON");
        sb.AppendLine("GO");
        sb.AppendLine("SET TRANSACTION ISOLATION LEVEL Serializable");
        sb.AppendLine("GO");
        sb.AppendLine("BEGIN TRANSACTION");
        sb.AppendLine("GO");
        sb.AppendLine("IF @@ERROR <> 0 SET NOEXEC ON");
        sb.AppendLine("GO");

        // ----- Thân: mỗi batch + guard -----
        foreach (var batch in SplitBatches(rawScript))
        {
            var trimmed = batch.Trim();
            if (trimmed.Length == 0) continue;
            // Bỏ batch chỉ là comment.
            if (IsAllComment(trimmed)) continue;

            if (progressPrints)
            {
                string? msg = FirstMeaningfulLine(trimmed);
                if (msg != null)
                {
                    sb.AppendLine($"PRINT N'{msg.Replace("'", "''")}'");
                    sb.AppendLine("GO");
                }
            }

            sb.AppendLine(trimmed);
            sb.AppendLine("GO");
            sb.AppendLine("IF @@ERROR <> 0 SET NOEXEC ON");
            sb.AppendLine("GO");
        }

        // ----- Footer: commit + báo kết quả / rollback -----
        sb.AppendLine("COMMIT TRANSACTION");
        sb.AppendLine("GO");
        sb.AppendLine("IF @@ERROR <> 0 SET NOEXEC ON");
        sb.AppendLine("GO");
        sb.AppendLine("DECLARE @Success AS BIT");
        sb.AppendLine("SET @Success = 1");
        sb.AppendLine("SET NOEXEC OFF");
        sb.AppendLine("IF (@Success = 1) PRINT 'The database update succeeded'");
        sb.AppendLine("ELSE BEGIN");
        sb.AppendLine("\tIF @@TRANCOUNT > 0 ROLLBACK TRANSACTION");
        sb.AppendLine("\tPRINT 'The database update failed'");
        sb.AppendLine("END");
        sb.AppendLine("GO");

        return sb.ToString();
    }

    /// <summary>Tách script theo dòng chỉ chứa "GO" (case-insensitive).</summary>
    private static IEnumerable<string> SplitBatches(string script)
    {
        var lines = (script ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var cur = new StringBuilder();
        foreach (var line in lines)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return cur.ToString();
                cur.Clear();
            }
            else
            {
                cur.AppendLine(line);
            }
        }
        if (cur.Length > 0) yield return cur.ToString();
    }

    /// <summary>
    /// Nhãn cho PRINT tiến trình: ưu tiên comment tên object "-- [schema].[obj]" (vì 1 batch có thể
    /// gồm NHIỀU lệnh cho cùng object); nếu không có thì lấy dòng lệnh đầu tiên.
    /// </summary>
    private static string? FirstMeaningfulLine(string batch)
    {
        string? objComment = null;
        string? firstStmt = null;
        foreach (var raw in batch.Split('\n'))
        {
            var l = raw.Trim();
            if (l.Length == 0) continue;
            if (l.StartsWith("--"))
            {
                var c = l.TrimStart('-', ' ', '=');
                if (objComment == null && c.StartsWith("[")) objComment = c; // "-- [dbo].[Table]"
                continue;
            }
            firstStmt ??= l;
        }
        var label = objComment ?? firstStmt;
        if (label == null) return null;
        return label.Length > 120 ? label.Substring(0, 120) + "..." : label;
    }

    /// <summary>True nếu batch chỉ gồm comment/trống.</summary>
    private static bool IsAllComment(string batch)
    {
        foreach (var raw in batch.Split('\n'))
        {
            var l = raw.Trim();
            if (l.Length == 0 || l.StartsWith("--")) continue;
            if (l.StartsWith("/*") || l.EndsWith("*/")) continue;
            return false;
        }
        return true;
    }
}
