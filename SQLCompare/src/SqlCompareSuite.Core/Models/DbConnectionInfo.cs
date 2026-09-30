using Microsoft.Data.SqlClient;

namespace SqlCompareSuite.Core.Models;

/// <summary>
/// Thông tin kết nối tới 1 SQL Server database. Build connection string từ các field.
/// </summary>
public sealed class DbConnectionInfo
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";

    /// <summary>True = Windows auth (Integrated Security); False = SQL auth (User/Password).</summary>
    public bool IntegratedSecurity { get; set; } = true;

    public string UserId { get; set; } = "";
    public string Password { get; set; } = "";

    /// <summary>Mặc định bật TrustServerCertificate để tránh lỗi SSL với server nội bộ.</summary>
    public bool TrustServerCertificate { get; set; } = true;

    public int ConnectTimeoutSeconds { get; set; } = 30;

    public string BuildConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = Database,
            TrustServerCertificate = TrustServerCertificate,
            ConnectTimeout = ConnectTimeoutSeconds,
            Encrypt = true,
        };

        if (IntegratedSecurity)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = UserId;
            builder.Password = Password;
        }

        return builder.ConnectionString;
    }

    public override string ToString() => $"{Server}.{Database}";
}
