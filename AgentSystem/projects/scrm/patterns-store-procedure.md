# SCRM — Rule viết Stored Procedure (báo cáo / report)

Tổng hợp từ các SP reporting SCRM (điển hình: `SCRM_Report_ReportDoanhSoTongQuan`). File này áp dụng cho **SP reporting** (nhiều param filter, nhiều nhánh logic, output phân trang). SP CRUD đơn giản có thể không theo hết.

---

## 1. Header bắt buộc

```sql
USE [<DB_NAME>]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
```

Mở SP bằng `ALTER PROCEDURE [dbo].[<Tên_SP>]`. Không dùng `CREATE OR ALTER`.

## 2. Block test mẫu ở đầu

Trước `ALTER PROCEDURE` đặt block `/* ... */` chứa **ít nhất 1 lệnh `EXEC` mẫu** với đầy đủ param thực tế (GUID thật, ngày thật). Mục đích: debug lại được ngay mà không phải tự dò param.

```sql
/*
EXEC <Tên_SP>
    @IdDonVi = 'be256683-...',
    @IdChiNhanh = NULL,
    @TuNgay = '2025-12-01 00:00:00',
    @DenNgay = '2025-12-23 23:59:59',
    ...
*/
```

## 3. Quy ước đặt tên & kiểu param

| Prefix / Kiểu | Dùng cho | Default |
|---|---|---|
| `@Id<Xxx>` UNIQUEIDENTIFIER | FK đơn lẻ | `NULL` |
| `@Str<Xxx>` VARCHAR(MAX) | List `id1;id2;id3` phân tách bằng `;` | `NULL` |
| `@Is<Xxx>` BIT | Flag 2-state / 3-state (NULL = tất cả) | `NULL` |
| `@Kieu<Xxx>` / `@Loai<Xxx>` INT | Enum phân nhánh | `NULL` |
| `@TuNgay` / `@DenNgay` DATETIME | Khoảng thời gian | `NULL` |
| `@PageIndex` / `@PageSize` INT | Phân trang | `1` / `20` |

- **Mọi param filter đều `= NULL`** (trừ page).
- Comment inline `-- ...` giải thích ý nghĩa, đặc biệt các enum và flag.
- Param text-list luôn tách bằng **`;`** (không phải `,`).

## 4. Khối khởi tạo chuẩn (ngay sau `AS`)

```sql
SET NOCOUNT ON
SET ARITHABORT ON;
DECLARE @Start DATETIME = GETDATE();

BEGIN  -- Khai báo biến, xử lý biến ban đầu
    IF @IsAdminView IS NULL SET @IsAdminView = 0;
    IF @IsChietKhau IS NULL SET @IsChietKhau = 1;
    -- ...tất cả flag/guid fallback ở đây
    DECLARE @NewGuidDefault UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000';
END
```

- Gom **toàn bộ fallback NULL** vào 1 block đầu, không rải rác.
- `@NewGuidDefault` = empty GUID — dùng cho admin view và placeholder.

## 5. KHÔNG dùng transaction

SP reporting **không** dùng `BEGIN TRAN` / `COMMIT` / `ROLLBACK`. Lý do:

- **Why:** Transaction giữ lock / log rất nặng trên SP đọc dữ liệu lớn — gây blocking diện rộng trên hệ thống OLTP đang chạy song song. Reporting không cần atomicity (chỉ đọc, không ghi), nên transaction chỉ làm chậm và kéo blocking.
- **How to apply:**
  - SP reporting chỉ SELECT + INSERT vào `#tmp` → không cần TRAN.
  - Đọc consistency bằng `WITH(NOLOCK)` (xem §10), chấp nhận dirty read theo chuẩn SCRM.
  - Nếu buộc phải ghi table thật (hiếm trong reporting) → cân nhắc kỹ, hỏi user trước khi bọc TRAN.

## 6. Chuẩn hóa khoảng ngày

```sql
-- @TuNgay về đầu phút (giây .000)
SELECT @TuNgay = CAST(CONVERT(NVARCHAR(50), @TuNgay, 120) + '.000' AS VARCHAR(50));

-- @DenNgay nếu phút = 59 thì kéo tới 59.998
IF (DATEPART(MINUTE, @DenNgay) = 59)
    SET @DenNgay = REPLACE(CONVERT(NVARCHAR(50), @DenNgay, 102), '.', '-')
                   + ' ' + CAST(DATEPART(HOUR, @DenNgay) AS VARCHAR(2)) + ':59:59.998';
```

## 7. Convert `@Str...` (list `;`) → `#tb...` table

**Không dùng `fnSplit`** (legacy). Dùng JSON + `OPENJSON`:

```sql
DECLARE @JsonUserIds NVARCHAR(MAX) = '["' + REPLACE(@StrUserId, ';', '","') + '"]';

CREATE TABLE #tbUsers (
    UserId UNIQUEIDENTIFIER PRIMARY KEY,
    UserName VARCHAR(200),
    Ten NVARCHAR(500)
);

INSERT INTO #tbUsers
SELECT CAST(j.[value] AS UNIQUEIDENTIFIER), u.UserName, u.Ten
FROM OPENJSON(@JsonUserIds) j
OUTER APPLY (
    SELECT Id, UserName, Ten
    FROM dbo.[User] WITH(NOLOCK)
    WHERE Id = CAST(j.[value] AS UNIQUEIDENTIFIER)
) u
UNION
SELECT @NewGuidDefault, 'Admin', 'Admin'
WHERE @IsAdminView = 1 AND @IsAdminChonCoCauToChuc = 0;
```

- Bảng temp **luôn có PRIMARY KEY**.
- `UNION` thêm `@NewGuidDefault` cho case admin chốt đơn (`UserIdSale IS NULL`).

## 8. Pagination chuẩn

```sql
IF @PageSize = 0 SET @PageSize = 999999999;
IF @PageIndex < 0 SET @PageIndex = 0;
DECLARE @StartRowIndex INT = @PageSize * (@PageIndex - 1) + 1;
```

## 9. Phân nhánh logic theo enum

```
IF @LoaiNhanVien = 1            -- Sale
    IF @KieuXem = 3  ...        -- Sản phẩm
    ELSE IF @KieuXem = 4 ...    -- Nhân viên / chi nhánh / ...
ELSE IF @LoaiNhanVien = 3       -- Marketing
    ...
```

Mỗi nhánh **tự build CTE + `#tmp` riêng**. Không share CTE giữa các nhánh.

## 10. Truy vấn chính — convention cứng

- **Mọi bảng đọc phải `WITH(NOLOCK)`** — nhất quán toàn bộ SP reporting SCRM.
- Alias 1 chữ: `m` (main), `dt` (detail), `sp` (sản phẩm), `u` (user), `nv` (nhóm NV).
- **Multi-tenant filter bắt buộc** ở mọi bảng gốc:

```sql
WHERE m.IdDonVi = @IdDonVi
  AND (@IdChiNhanh IS NULL OR @IdChiNhanh = m.IdChiNhanh)
  AND m.IsDelete = 0
```

- Dùng `CROSS APPLY` / `OUTER APPLY` thay subquery correlated khi cần group con.

## 11. Pattern filter tùy chọn

| Dạng | Mẫu |
|---|---|
| Optional scalar | `(@Param IS NULL OR col = @Param)` |
| Optional list string (IDs gộp `;`) | `(@StrIds IS NULL OR CHARINDEX(CAST(col AS VARCHAR(36)), @StrIds) > 0)` |
| Flag 3-state (NULL/0/1) | `(@Is IS NULL OR (@Is = 0 AND col <> 1) OR (@Is = 1 AND col = 1))` |
| Field lưu list `;` trong DB | `(CHARINDEX(@StrId, m.ListIds) > 0 OR @Id IS NULL)` |
| Nhiều cột ngày → `@KieuNgay` switch | `CASE WHEN @KieuNgay='NgayTao' AND m.NgayTao BETWEEN ... THEN 1 ... END = 1` |

## 12. CTE — đặt tên & chain

- Prefix `tmp` + mục đích: `tmpContactData`, `tmpDonHangData`, `tmpDonHangData2`.
- Chain 2–3 cấp:
  1. CTE1: lấy raw + filter chính.
  2. CTE2: break list (`STRING_SPLIT`) nếu cần mở row theo từng ID trong list.
  3. CTE3: aggregate.
- Câu cuối: `SELECT ... INTO #tmpXxx FROM (...) m OPTION(...)` để vật chất hóa cho bước kế tiếp / trả về.

## 13. Đếm distinct an toàn khi JOIN detail

Khi JOIN table detail (ví dụ `DonHangLogisticSanPham`) thì 1 đơn có nhiều dòng → đếm bị nhân. Dùng `ROW_NUMBER` để chặn:

```sql
ROW_NUMBER() OVER (PARTITION BY m.Id ORDER BY m.Id) AS RowNum
-- Sau đó:
COUNT(IIF(RowNum = 1 AND <điều kiện>, 1, NULL)) AS SoDonThucTe
```

Tách 2 bộ số song song:
- **Raw**: đếm theo số dòng (nhân theo số SP trong đơn) — phục vụ tính doanh số / sản phẩm.
- **Thực tế**: chỉ khi `RowNum = 1` — phục vụ đếm số đơn distinct.

## 14. Aggregate có điều kiện

- Tổng tiền: `SUM(IIF(<cond>, ISNULL(col, 0), 0))`
- Đếm: `COUNT(IIF(<cond>, 1, NULL))` — **không dùng `SUM` để đếm**.
- Nhóm cột output theo prefix `_`:
  - `DoanhSoTong_ChietKhau`, `DoanhSoTong_SoDon`, `DoanhSoTong_DoanhSo`
  - `DoanhSoXacNhan_...`, `DoanhSoTamTinh_...`, `DoanhSoDaHoan_...`, `DoanhSoDangHoan_...`

## 15. Enum `GiaoHangTrangThaiMa` (hardcode trong report)

| Mã | Ý nghĩa |
|---|---|
| 1, 3, 4, 5 | Chưa gửi / hủy vận đơn |
| 20+ (trừ 22, 31, 32, 35, 41, 50) | Đang chuyển |
| 22 | (trạng thái trung gian — loại khỏi "đang chuyển") |
| 31, 32 | Giao thành công |
| 35 | Giao một phần |
| 40 | Đang hoàn |
| 41 | Đã hoàn |
| 50 | Trạng thái đặc biệt (trace khi dùng) |

Khi viết điều kiện "đang chuyển", liệt kê đầy đủ các mã loại trừ: `>= 20 AND <> 22 AND <> 31 AND <> 32 AND <> 35 AND <> 41 AND <> 50`.

## 16. Index trên `#tmp` — KHÔNG tự tạo

- **Why:** Tạo index sai hoặc dư thừa trong SP làm chậm thêm vì build index cũng tốn thời gian, và lựa chọn cột index phụ thuộc pattern query downstream mà chỉ người review biết.
- **How to apply:**
  - Viết SP xong, **không tự thêm** `CREATE NONCLUSTERED INDEX ON #tmp...`.
  - Chỉ để `PRIMARY KEY` lúc `CREATE TABLE #tb...` (đã có sẵn vì cần cho JOIN).
  - Để user / DBA tự quyết định có thêm index hay không sau khi đo thực tế.

## 17. Query hint ở `SELECT ... INTO #tmp...`

```sql
OPTION(
    RECOMPILE,
    MAXDOP 10,
    MIN_GRANT_PERCENT = 1,
    MAX_GRANT_PERCENT = 10,
    HASH GROUP
)
```

- `RECOMPILE`: tránh plan cache xấu do param sniffing (nhiều nhánh IF/ELSE).
- `MAXDOP 10`: chặn parallelism quá mức trên OLTP.
- `MIN/MAX_GRANT_PERCENT`: giới hạn memory grant.
- `HASH GROUP`: ưu tiên hash aggregate thay vì sort.

## 18. Debug helper

Giữ lại comment ở đầu SP để bật khi cần:

```sql
--SET STATISTICS IO ON
--SET STATISTICS TIME ON
DECLARE @Start DATETIME = GETDATE();
-- Cuối SP (optional):
-- SELECT DATEDIFF(MS, @Start, GETDATE()) AS ElapsedMs;
```

---

## Checklist self-review trước khi commit SP

- [ ] Có block `/* EXEC ... */` mẫu ở đầu, param thực tế.
- [ ] Tất cả param filter default `= NULL`.
- [ ] Block fallback NULL gom vào 1 chỗ.
- [ ] `SET NOCOUNT ON` + `SET ARITHABORT ON`.
- [ ] **Không có `BEGIN TRAN`**.
- [ ] Mọi bảng đọc có `WITH(NOLOCK)`.
- [ ] Filter multi-tenant `@IdDonVi` + `@IdChiNhanh` + `IsDelete = 0` đủ ở mọi bảng gốc.
- [ ] Dùng `OPENJSON` (không phải `fnSplit`) để parse list `;`.
- [ ] Temp table có `PRIMARY KEY`.
- [ ] **Không tự `CREATE INDEX` trên `#tmp`**.
- [ ] Phân trang `@PageSize = 0 → 999999999`, `@PageIndex < 0 → 0`.
- [ ] `COUNT(IIF(..., 1, NULL))`, không dùng `SUM` để đếm.
- [ ] Dùng `ROW_NUMBER` khi JOIN detail để đếm distinct.
- [ ] `OPTION(RECOMPILE, MAXDOP 10, ...)` ở query nặng.
