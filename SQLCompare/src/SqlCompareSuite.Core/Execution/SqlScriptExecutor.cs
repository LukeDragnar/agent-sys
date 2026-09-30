using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Execution;

/// <summary>Kết quả thực thi 1 script SQL trên DB target.</summary>
public sealed class ScriptExecutionResult
{
    public bool Success;
    public int BatchesExecuted;
    public string Message = "";
}

/// <summary>
/// Thực thi script SQL trên 1 DB target. Tách batch theo dòng "GO" và chạy
/// lần lượt trong 1 transaction. Có lỗi -> rollback toàn bộ.
/// </summary>
public sealed class SqlScriptExecutor
{
    /// <summary>
    /// Tách script theo dòng chỉ chứa "GO" (case-insensitive, đã trim), bỏ batch rỗng,
    /// chạy từng batch trong cùng 1 transaction. Lỗi -> rollback + Success=false.
    /// Thành công -> commit + Success=true.
    /// </summary>
    public ScriptExecutionResult Execute(DbConnectionInfo target, string script)
    {
        var result = new ScriptExecutionResult();

        if (target == null)
        {
            result.Message = "Target connection is null.";
            return result;
        }
        if (string.IsNullOrWhiteSpace(script))
        {
            result.Success = true;
            result.Message = "Script is empty — nothing to execute.";
            return result;
        }

        List<string> batches = SplitBatches(script);
        if (batches.Count == 0)
        {
            result.Success = true;
            result.Message = "No executable batches found.";
            return result;
        }

        try
        {
            using var connection = new SqlConnection(target.BuildConnectionString());
            connection.Open();

            // BUG-FIX: connection pool có thể trả về 1 connection từng được set READ UNCOMMITTED
            // bởi luồng ĐỌC (SqlReadExtensions.OpenRead). Isolation level dính theo session và
            // được tái sử dụng khi connection quay lại pool → luồng GHI này có thể vô tình chạy
            // READ UNCOMMITTED trên DB production. Set tường minh READ COMMITTED ngay sau khi mở,
            // TRƯỚC BeginTransaction, để không thừa hưởng isolation level cũ từ pool.
            using (var resetCmd = connection.CreateCommand())
            {
                resetCmd.CommandText = "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;";
                resetCmd.ExecuteNonQuery();
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (string batch in batches)
                {
                    using var command = new SqlCommand(batch, connection, transaction)
                    {
                        CommandTimeout = 0,
                    };
                    command.ExecuteNonQuery();
                    result.BatchesExecuted++;
                }

                transaction.Commit();
                result.Success = true;
                result.Message = $"Executed successfully. {result.BatchesExecuted} batch(es) committed.";
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); }
                catch { /* rollback best-effort */ }

                result.Success = false;
                result.Message =
                    $"Execution failed at batch #{result.BatchesExecuted + 1}: {ex.Message}. " +
                    "Transaction rolled back — no changes applied.";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Could not execute script: {ex.Message}";
        }

        return result;
    }

    /// <summary>Tách script thành các batch theo dòng "GO" (trim, case-insensitive). Bỏ batch rỗng.</summary>
    private static List<string> SplitBatches(string script)
    {
        var batches = new List<string>();
        var current = new System.Text.StringBuilder();

        string[] lines = script.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        foreach (string line in lines)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                AddIfNotEmpty(batches, current);
                current.Clear();
            }
            else
            {
                current.AppendLine(line);
            }
        }
        AddIfNotEmpty(batches, current);

        return batches;
    }

    private static void AddIfNotEmpty(List<string> batches, System.Text.StringBuilder sb)
    {
        string text = sb.ToString();
        if (!string.IsNullOrWhiteSpace(text))
            batches.Add(text);
    }
}
