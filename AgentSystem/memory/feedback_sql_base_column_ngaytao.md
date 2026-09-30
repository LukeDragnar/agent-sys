---
name: sql-base-column-ngaytao
description: SCRM SQL entity dùng cột base NgayTao/NgayCapNhat/NgayXoa/IsDelete — KHÔNG có CreatedDate/UpdatedDate
metadata:
  type: feedback
---

Mọi entity SQL SCRM kế thừa `SB.Core.BaseEntity` (`Libs/Models.Core/BaseEntity.cs`). Cột audit chuẩn = **tiếng Việt**:

- `NgayTao` (ngày tạo) · `NguoiTaoUserId` · `NguoiTaoUserName`
- `NgayCapNhat` · `NguoiCapNhatUserId` · `NguoiCapNhatUserName`
- `NgayXoa` · `NguoiXoaUserId` · `NguoiXoaUserName`
- `IsDelete` (bool, xóa mềm)

**KHÔNG tồn tại `CreatedDate` / `UpdatedDate` / `CreatedBy...`** trên bảng SQL. Viết `ORDER BY CreatedDate` = lỗi "Invalid column name".

**Why:** đây là lỗi lặp — user đã phải sửa tay `CreatedDate`→`NgayTao` (query log phát hành HĐ) và nhắc "rất hay sai". Verify thực tế qua `INFORMATION_SCHEMA.COLUMNS` bảng `InvoiceInfo`: chỉ có NgayTao/NgayCapNhat/NgayXoa, không CreatedDate.

**How to apply:** mọi câu SELECT/ORDER BY trên bảng SQL SCRM → dùng `NgayTao` cho "gần nhất", `IsDelete = 0` để loại xóa mềm, và **`WITH (NOLOCK)`** cho mọi bảng (khớp `TableWithNolock()` trong code). Mongo (`BaseEntityMongoDB_V2`) cũng dùng `IsDelete` (bool) nhưng KHÔNG có khái niệm NOLOCK. Liên quan [[feedback_sql_count_large_table]].
