using System.Security.Cryptography;
using System.Text;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Project;

/// <summary>
/// Mã hóa chuỗi nhạy cảm (password) bằng Windows DPAPI, scope CurrentUser.
/// Chỉ ĐÚNG user + ĐÚNG máy mới giải mã được → copy file sang máy/user khác sẽ không đọc được password (trả "").
/// Tương thích ngược: chuỗi cũ lưu plaintext (không có prefix) vẫn đọc được.
/// </summary>
public static class SecretProtector
{
    private const string Prefix = "dpapi:";

    // Entropy cố định gắn với app (không phải secret thật, chỉ tăng tính riêng cho chuỗi mã hóa).
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SqlCompareSuite.v1");

    /// <summary>Mã hóa. Rỗng → "". Không mã hóa được (vd non-Windows) → trả plaintext để không mất dữ liệu.</summary>
    public static string Protect(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return "";
        try
        {
            byte[] enc = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(enc);
        }
        catch
        {
            return plain;
        }
    }

    /// <summary>Giải mã. Không có prefix → coi là plaintext cũ. Giải mã lỗi (máy khác/hỏng) → "".</summary>
    public static string Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return "";
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
            return stored; // legacy plaintext
        try
        {
            byte[] cipher = Convert.FromBase64String(stored[Prefix.Length..]);
            byte[] plain = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return "";
        }
    }

    /// <summary>Tạo bản sao DbConnectionInfo với Password thay bằng giá trị cho trước (không đụng bản gốc).</summary>
    public static DbConnectionInfo CloneWithPassword(DbConnectionInfo? c, string password)
    {
        c ??= new DbConnectionInfo();
        return new DbConnectionInfo
        {
            Server = c.Server,
            Database = c.Database,
            IntegratedSecurity = c.IntegratedSecurity,
            UserId = c.UserId,
            Password = password,
            TrustServerCertificate = c.TrustServerCertificate,
            ConnectTimeoutSeconds = c.ConnectTimeoutSeconds,
        };
    }
}
