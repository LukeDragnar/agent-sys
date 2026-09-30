using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Execution;

/// <summary>
/// Tiện ích test connection và liệt kê database trên 1 SQL Server.
/// </summary>
public sealed class SqlServerBrowser
{
    /// <summary>Thử mở connection theo conn. Trả (ok, message mô tả kết quả).</summary>
    public (bool ok, string message) TestConnection(DbConnectionInfo conn)
    {
        if (conn == null)
            return (false, "Connection info is null.");
        if (string.IsNullOrWhiteSpace(conn.Server))
            return (false, "Server is required.");

        try
        {
            using var connection = new SqlConnection(conn.BuildConnectionString());
            connection.OpenRead();
            string db = string.IsNullOrWhiteSpace(connection.Database) ? "(default)" : connection.Database;
            return (true, $"Connection OK. Server: {connection.DataSource}, Database: {db}, Version: {connection.ServerVersion}.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Liệt kê database trên server. Kết nối vào 'master' (clone conn nhưng đổi Database),
    /// loại các system db. Trả danh sách tên đã sort.
    /// </summary>
    public List<string> ListDatabases(DbConnectionInfo conn)
    {
        var databases = new List<string>();
        if (conn == null || string.IsNullOrWhiteSpace(conn.Server))
            return databases;

        var masterConn = new DbConnectionInfo
        {
            Server = conn.Server,
            Database = "master",
            IntegratedSecurity = conn.IntegratedSecurity,
            UserId = conn.UserId,
            Password = conn.Password,
            TrustServerCertificate = conn.TrustServerCertificate,
            ConnectTimeoutSeconds = conn.ConnectTimeoutSeconds,
        };

        const string sql =
            "SELECT name FROM sys.databases " +
            "WHERE database_id > 4 OR name NOT IN ('master','tempdb','model','msdb') " +
            "ORDER BY name";

        using var connection = new SqlConnection(masterConn.BuildConnectionString());
        connection.OpenRead();   // BUG-FIX: đường ĐỌC → dùng OpenRead (NOLOCK) cho nhất quán

        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(0))
                databases.Add(reader.GetString(0));
        }

        return databases;
    }
}
