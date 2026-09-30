---
name: feedback-scrm-no-query-isdelete-for-log
description: SCRM — không query data IsDelete=true để lấy info hiển thị/log. Phải snapshot field cần thiết vào row/queue/object lúc tạo.
metadata:
  type: feedback
---

# Không query data đã soft-delete để enrich log/audit

Khi cần hiển thị/log thông tin của 1 record (vd `MaPhieu` trong lịch sử sửa phiếu), **KHÔNG** query lại record gốc — kể cả bằng `TableWithNolockFullData()` (bypass IsDelete filter).

Thay vào đó: **snapshot field cần thiết vào row session / message queue / DTO log ngay tại thời điểm tạo log**. Sau này list/render chỉ đọc từ row đó.

**Why**:
- User feedback (2026-05-28, bug 437.1 list lịch sử sửa phiếu): "tại sao lại where vậy lúc sửa xóa data sao không lấy thông tin đó gửi vào queue hoặc object đó để log chứ sao lại lấy isdelete= false, tôi ko muốn query data isdelete = false như vậy"
- Pattern `TableWithNolockFullData()` (IgnoreQueryFilters) tồn tại trong code (`EfRepository.cs`) nhưng chỉ dành cho job dọn dẹp / sync nội bộ. Dùng để enrich log audit là sai design — kéo theo phiếu đã xóa vào read-path không cần thiết, mất ý nghĩa của soft-delete.

**How to apply**:
- Khi viết feature audit/log/history mới: thêm column snapshot (Ma, Ten, ...) vào row log entity. Migration `ALTER TABLE ADD COLUMN NVARCHAR(N) NULL` idempotent kiểu `IF NOT EXISTS (SELECT 1 FROM sys.columns ...)`.
- Khi tạo log row: pass `entity.Ma`, `entity.Ten`, ... vào method `InsertSession/InsertLog/Publish` — đừng chỉ truyền `entity.Id` rồi join lại sau.
- Khi gửi queue/event: include snapshot field trong message payload (consumer không cần query lại entity gốc).
- Khi list/render log: SELECT thẳng từ row log, không join sang bảng gốc. Bỏ helper kiểu `EnrichMaPhieu` (join sau bằng `IdPhieuEdit`).
- Reference fix: `PhieuHoanThanhChinhSua.MaPhieuEdit` (column snapshot) + `InsertSession(idPhieuEdit, maPhieuEdit, ...)` (truyền `phieu.Ma` lúc tạo session).

Liên quan: [[scrm_no_or_in_linq]] (cùng nguyên tắc: query phải clean, không workaround bằng filter rộng hơn).
