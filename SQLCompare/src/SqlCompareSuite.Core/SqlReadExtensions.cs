using Microsoft.Data.SqlClient;

namespace SqlCompareSuite.Core;

/// <summary>
/// Helper mở connection cho thao tác ĐỌC/COMPARE với READ UNCOMMITTED (tương đương WITH (NOLOCK)).
///
/// LÝ DO: tool dùng để compare DB production ↔ staging. Mọi truy vấn đọc (schema metadata + data)
/// PHẢI không đặt shared lock để tránh chặn / bị chặn bởi traffic production.
///
/// CHỈ dùng cho đọc. KHÔNG dùng cho luồng execute sync (ghi) — xem SqlScriptExecutor giữ isolation mặc định.
/// </summary>
public static class SqlReadExtensions
{
    /// <summary>Mở connection và đặt isolation level READ UNCOMMITTED cho toàn session.</summary>
    /// <remarks>
    /// CẢNH BÁO: isolation level set ở đây dính theo session và CÓ THỂ theo connection khi trả về
    /// connection pool, rồi bị tái sử dụng bởi luồng khác. Mọi luồng GHI (vd SqlScriptExecutor)
    /// PHẢI set lại isolation level tường minh (READ COMMITTED) ngay sau khi mở connection để
    /// không thừa hưởng READ UNCOMMITTED từ pool. KHÔNG đổi hành vi của OpenRead.
    /// </remarks>
    public static void OpenRead(this SqlConnection conn)
    {
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;";
        cmd.ExecuteNonQuery();
    }
}
