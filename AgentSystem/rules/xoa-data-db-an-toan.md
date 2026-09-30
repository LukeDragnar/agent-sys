# Kỹ năng: Viết query XÓA data DB an toàn (SQL Server)

> Skill dùng cho **mọi project** khi cần gen script xóa/hủy data theo yêu cầu (xóa phiếu, dọn record lỗi, hủy chứng từ...). Đúc từ ca thật: xóa cụm **Phiếu mua hàng (PMH)** xuyên 3 DB service (Accounting / Warehouse / Major).
>
> Đọc kèm: `rules/db.md` (rule prod-safety gốc) · `memory/feedback_prod_query_safety.md`.

---

## 0. Nguyên tắc gốc (bất di bất dịch)

1. **Không tự chạy** — Claude chỉ gen script, người dùng review rồi tự chạy. **Tuyệt đối không đụng prod** (xem `rules/db.md`).
2. **Xóa MỀM** (`IsDelete=1` + `NgayXoa` + `NguoiXoaUserName`), **không `DELETE` cứng** → còn đường lần vết / khôi phục.
3. **Hiểu domain TRƯỚC khi cascade.** Record là "chứng từ gốc" (vd PMH kéo theo phiếu nhập + phiếu chi + công nợ + bút toán + MISA) thì xóa mềm SQL **KHÔNG đảo** được tồn kho / giá vốn / IMEI / lô-date / ES / công nợ / MISA → ưu tiên **luồng Hủy của app**. Chỉ dùng SQL khi user **chấp nhận** chỉ "ẩn record khỏi list" và chịu lệch các thứ ngoài record.
4. **Báo cáo trung thực**: chỉ ghi "đã xóa" khi thấy `@@ROWCOUNT`. Mới chạy preview = **chưa xóa**.

---

## 1. Template chuẩn (1 DB, cha–con) — copy về sửa

Hai chốt an toàn cốt lõi: **cờ `@Action`** (SELECT/UPDATE) + **guard `@ConfirmTime`** (chống lỡ-save).

```sql
/* ===== SOFT-DELETE an toàn — 1 DB ===== */
DECLARE @Action      VARCHAR(10) = 'SELECT'   -- 🔴 'SELECT'=xem trước | 'UPDATE'=xóa thật (CHỈ đúng chữ mới xóa)
DECLARE @ConfirmTime VARCHAR(12) = ''         -- 🔴 khi UPDATE: nhập ĐÚNG phút hiện tại yyMMddHHmm (script in ra nếu sai)

DECLARE @NguoiYeuCau VARCHAR(30) = 'Ten'      -- ⚠️ chỉ sửa TÊN người yêu cầu; NGÀY tự sinh
DECLARE @pNgayXoa    DATETIME    = GETDATE()  -- 1 MỐC thời gian duy nhất cho cả script
DECLARE @pNguoiXoaUserName VARCHAR(50) = @NguoiYeuCau + 'YeuCauXoa_' + FORMAT(@pNgayXoa,'yyyyMMdd')
DECLARE @Now VARCHAR(12) = FORMAT(@pNgayXoa,'yyMMddHHmm')

IF @Action='UPDATE' AND @ConfirmTime<>@Now
BEGIN
    SELECT N'BỊ CHẶN: mã xác nhận sai/cũ. Copy chuỗi bên phải vào @ConfirmTime rồi chạy lại NGAY trong phút này.' AS CanhBao,
           @Now AS ConfirmTime_CanNhap
    SET @Action='SELECT'    -- ép về preview, KHÔNG cho xóa
END

-- 1) Danh sách MÃ cần xóa (nhiều mã tùy ý)
DECLARE @MaList TABLE (Ma VARCHAR(50) PRIMARY KEY)
INSERT INTO @MaList (Ma) VALUES ('MA_PHIEU_1') /*, ('MA_PHIEU_2')*/

-- 2) RESOLVE: mọi điều kiện lọc VIẾT 1 LẦN ở đây → chốt đúng tập id cần đụng
--    (IdDonVi/Id derive TỪ chính record, không hardcode)
DECLARE @Cha TABLE (Id VARCHAR(50) PRIMARY KEY, IdDonVi VARCHAR(50), Ma VARCHAR(50))
INSERT INTO @Cha (Id, IdDonVi, Ma)
SELECT c.Id, c.IdDonVi, c.Ma
FROM dbo.BangCha c WITH (NOLOCK)
JOIN @MaList l ON l.Ma = c.Ma
WHERE c.IsDelete = 0

-- 3) ĐỐI CHIẾU: mã nào không tìm thấy (gõ sai/đã xóa/khác đơn vị) → Id NULL
SELECT l.Ma AS MaYeuCau, x.Id, x.IdDonVi
FROM @MaList l LEFT JOIN @Cha x ON x.Ma = l.Ma
ORDER BY (CASE WHEN x.Id IS NULL THEN 0 ELSE 1 END), l.Ma

-- 4) Bảng CHA  (SELECT/UPDATE chỉ khác DML+NOLOCK; điều kiện = JOIN @Cha)
IF @Action='UPDATE' BEGIN
    UPDATE m SET m.IsDelete=1, m.NgayXoa=@pNgayXoa, m.NguoiXoaUserName=@pNguoiXoaUserName
    FROM dbo.BangCha m JOIN @Cha x ON x.Id=m.Id
    SELECT @@ROWCOUNT AS BangCha_Xoa END
ELSE SELECT TOP 100 m.* FROM dbo.BangCha m WITH(NOLOCK) JOIN @Cha x ON x.Id=m.Id

-- 5) Bảng CON
IF @Action='UPDATE' BEGIN
    UPDATE m SET m.IsDelete=1, m.NgayXoa=@pNgayXoa, m.NguoiXoaUserName=@pNguoiXoaUserName
    FROM dbo.BangCon m JOIN @Cha x ON x.Id=m.IdCha
    SELECT @@ROWCOUNT AS BangCon_Xoa END
ELSE SELECT TOP 200 m.* FROM dbo.BangCon m WITH(NOLOCK) JOIN @Cha x ON x.Id=m.IdCha WHERE m.IsDelete=0
```

**Quy trình dùng:** để `@Action='SELECT'` chạy preview → khớp đúng dòng → đổi `@Action='UPDATE'`, chạy 1 lần (bị chặn + in `ConfirmTime_CanNhap`) → copy vào `@ConfirmTime` → chạy lại **trong phút đó** → đối chiếu các cột `_Xoa` với preview.

---

## 2. Cascade xuyên NHIỀU DB service — "bản đồ khóa"

Khi record trải nhiều DB (mỗi service 1 DB, **không** transaction xuyên DB):

1. **Resolve ở DB gốc** → xuất **bản đồ khóa**: id record + các khóa liên kết sang DB khác.
2. **Copy khóa** sang script của từng DB (mỗi DB 1 connection / 1 script riêng, cùng khung §1).
3. Khóa liên kết dạng **chuỗi `"id;id"`** → tách bằng `STRING_SPLIT` + `CROSS APPLY`:

```sql
DECLARE @ChildIds TABLE (Id VARCHAR(50) PRIMARY KEY)
INSERT INTO @ChildIds (Id)
SELECT DISTINCT LTRIM(RTRIM(s.value))
FROM @Cha p CROSS APPLY STRING_SPLIT(ISNULL(p.IdsCon,''), ';') s
WHERE NULLIF(LTRIM(RTRIM(s.value)),'') IS NOT NULL   -- lọc rỗng do dấu ';' cuối
```

4. **Bút toán / định khoản**: mỗi phiếu con (nhập/xuất/thu/chi) thường sinh bút toán RIÊNG keyed theo id của nó → gom `@AllPhieuIds = cha ∪ các id con` rồi soi bút toán theo tập đó (đừng chỉ theo id cha).

**Bài học ripple (SCRM):** phiếu nhập sinh từ PMH **không** đồng bộ sang Accounting (phần nhập thể hiện bằng bút toán `MH`); phiếu chi chỉ đồng bộ + sinh bút toán `PC` **khi đã chi thật**. ⇒ luôn **preview từng DB**, "check không có thì không xóa".

---

## 3. Catalog kỹ thuật (13)

| # | Kỹ thuật | Vấn đề nó giải |
|---|---|---|
| 1 | **Multi-mã bằng table variable** (`@MaList TABLE(Ma PK)` + JOIN) | Xóa nhiều mã 1 lần thay vì `@pMaPhieu` đơn |
| 2 | **Resolve-once** — dồn TẤT CẢ điều kiện lọc vào bước resolve (`@Ids`); SELECT/UPDATE chỉ `JOIN @Ids` | SELECT & UPDATE là **2 lệnh khác nhau → không share WHERE**; sửa điều kiện 1 chỗ, hết lệch |
| 3 | **Derive khóa từ record** (IdDonVi/Id lấy từ phiếu, không hardcode) | Đúng multi-tenant tự động; khỏi copy id tay |
| 4 | **Cờ `@Action`** (chỉ đúng `'UPDATE'` mới xóa; gõ khác → về SELECT) | Thay comment/uncomment tay; **tự né lỗi NOLOCK-trên-bảng-đích** (2 nhánh tách) |
| 5 | **Guard `@ConfirmTime`=yyMMddHHmm** | Chống **lỡ save file với UPDATE**: phút sau chạy không khớp → tự chặn |
| 6 | **1 mốc `@pNgayXoa=GETDATE()`** cho mọi thứ | `NgayXoa` + token xác nhận + hậu tố ngày trong tên đều khớp, không drift |
| 7 | **Auto-date username** (`@NguoiYeuCau+'YeuCauXoa_'+FORMAT(@pNgayXoa,'yyyyMMdd')`) | Hết quên chỉnh ngày; chỉ còn đổi tên |
| 8 | **Block đối chiếu** (LEFT JOIN mã-yêu-cầu vs resolved) | Lộ mã gõ sai / không tồn tại / khác đơn vị (Id NULL) |
| 9 | **`SELECT @@ROWCOUNT`** sau mỗi UPDATE | Đối chiếu số dòng xóa vs preview |
| 10 | **Cross-DB + "bản đồ khóa"** | Record trải nhiều DB → resolve khóa ở DB gốc, mang sang từng DB |
| 11 | **Tách FK chuỗi** (`"id;id"` → `STRING_SPLIT`+`CROSS APPLY`) | Khóa liên kết bẩn (vd `IdsThuChi`) |
| 12 | **Giữ hết block / no-op** (block ra 0 dòng thì thôi) | Script tái dùng cho nhiều phiếu; "check không có thì không xóa" |
| 13 | **Reverse-check chống mồ côi** (xóa con → soi cha còn sống không) | Xóa lẻ phiếu con khi cha còn sống = cha trỏ vào record chết |

**Reverse-check mẫu (chạy ở DB có cả cha lẫn con):**
```sql
SELECT con.Ma AS MaCon, con.Id, con.IdCha,
       CASE WHEN cha.Id IS NOT NULL AND cha.IsDelete=0
            THEN N'⛔ THUỘC CHA CÒN SỐNG → xóa theo cụm cha, đừng xóa lẻ'
            ELSE N'✅ độc lập → xóa lẻ được' END AS KetLuan
FROM dbo.BangCon con WITH(NOLOCK)
JOIN @MaConList l ON l.Ma=con.Ma
LEFT JOIN dbo.BangCha cha WITH(NOLOCK) ON cha.Id=con.IdCha
WHERE con.IsDelete=0
```

---

## 4. Checklist an toàn PROD (bắt buộc — từ `rules/db.md`)

- ✅ `WITH (NOLOCK)` mọi bảng ở nhánh SELECT · ✅ `TOP n` · ✅ `IsDelete=0` · ✅ `IdDonVi` scope.
- ✅ Điều kiện lọc **sargable** — cột index để **TRẦN** trong `WHERE` (`col > 0`, `NgayTao >= @a AND < @b`); **cấm** `ISNULL(...)` / `CONVERT(date,...)` trong `WHERE`/`JOIN ON`.
- ✅ Lọc **bảng cha trước** (ít dòng) rồi mới join xuống chi tiết; UPDATE bounded bằng `@Ids` (không full-scan).
- ✅ Chạy prod nặng: `OPTION (MAXDOP 1)`, chia nhỏ khoảng ngày, tránh giờ cao điểm, ưu tiên replica.

---

## 5. Cạm bẫy đã gặp

- ❌ `UPDATE ... FROM bảng WITH(NOLOCK)` → lỗi *"Cannot use NOLOCK on target"*. Cờ `@Action` tách 2 nhánh nên nhánh UPDATE không NOLOCK → khỏi dính.
- ❌ Tưởng soft-delete phiếu nhập = trừ tồn kho. **KHÔNG** — tồn/giá vốn/IMEI/ES/MISA/công nợ vẫn nguyên.
- ❌ Xóa child (PN/PC) mà không check parent (PMH) → parent trỏ vào record chết.
- ❌ Ghi confirm "đã xóa" khi mới chạy **preview** — phải có `@@ROWCOUNT` mới tính là xóa.
- ❌ `FORMAT()` cần SQL Server 2012+; `STRING_SPLIT` cần 2016+ (compat ≥130). DB cũ hơn → dùng cách tách khác.
