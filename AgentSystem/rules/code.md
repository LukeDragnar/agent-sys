# rules/code.md — Quy tắc viết mã CHUNG (mọi mảng)

> Áp cho mọi project. Quy tắc riêng từng mảng → `projects/<mảng>/patterns.md` + `qa-code.md`.

## Trước khi sửa
- **Đọc luồng hiện có + so field API** trước khi viết mới (đừng đoán).
- Task đụng vùng nguy hiểm (tiền/kho/multi-tenant/schema/tích hợp/RBAC) → theo [quy-trinh-theo-level.md](quy-trinh-theo-level.md).

## Khi viết
- **KHÔNG hardcode** URL/IP/magic-number nghiệp vụ — dùng config/constant/enum.
- **Đa tenant:** query bảng có `IdDonVi`/`IdChiNhanh` PHẢI filter — tránh rò chéo tenant.
- Tên rõ nghĩa, KHÔNG viết tắt tối nghĩa, nhất quán 1 ngôn ngữ (xem [../CONVENTIONS.md](../CONVENTIONS.md)).
- Comment **WHY** ở chỗ load-bearing, không comment WHAT đã rõ; refactor phải update/xóa comment stale.
- Xóa code/inject/using không dùng ngay; refactor phải xóa sạch zombie field/assignment.

## Sửa HÀNG LOẠT (sed / regex / replace-all) — luôn rà lại từng call-site

Thay đổi cơ học "chắc chắn đúng" là chỗ dễ để lọt lỗi im lặng nhất, vì mình tin vào **số lượng thay thế** thay vì đọc kết quả.

- 🔴 **Đừng tin `N chỗ đã thay`** — số đó chỉ nói mẫu khớp bao nhiêu lần, KHÔNG nói còn sót bao nhiêu.
- **Bẫy 1 — biến thể INLINE không khớp mẫu.** Mẫu viết theo dạng nhiều dòng thì bản viết gọn 1 dòng
  (`}).catch(() => { this.loading = false; });`) sẽ bị bỏ qua. Nguy hiểm vì thường đó là **nhánh lỗi**,
  chỉ lộ khi API fail — test tay đường thành công không thấy gì.
- **Bẫy 2 — CRLF làm regex đa dòng trượt.** Mẫu chứa `\n` không khớp file `\r\n`: phần thay giá trị chạy,
  phần **chèn định nghĩa/khai báo** không chạy → sinh code **gọi hàm chưa tồn tại**, gãy compile
  (hoặc tệ hơn: chạy được nhưng thiếu nhánh).
- ✅ **Bắt buộc sau khi chạy:** grep lại **toàn bộ ký hiệu bị đụng** và **đếm định nghĩa vs sử dụng**
  (mỗi hàm mới thêm phải có đúng 1 định nghĩa và ≥1 chỗ gọi ở CÙNG file), rồi đọc từng call-site
  của nhánh lỗi/`catch`/`return` sớm.
- Sửa cơ học đụng **tài nguyên có vòng đời cặp** (show/hide, open/close, lock/unlock, begin/commit)
  → phải kiểm **khớp cặp trên MỌI nhánh**, không chỉ nhánh thành công.

> Sự cố 30/07/2026 (591-3): chuyển 8 chỗ loading sang spinner chung bằng replace-all — 1 nhánh `catch`
> viết inline không khớp mẫu (thiếu `hide()` → treo overlay khi lỗi mạng), và regex chèn constructor
> trượt vì CRLF (file gọi `datLoading()` khi hàm chưa được chèn). Cả hai chỉ lộ ra nhờ grep lại, không
> phải nhờ kết quả replace.

## Trước khi báo xong / push
- Grep file đã sửa tìm: `// TODO` debug, `localhost`/IP cứng, `Console.log`/`Debug.WriteLine` sót, code comment-out >5 dòng, magic number.
- Đụng logic → **review → trace → confirm** (xem CLAUDE.md "Definition of DONE").
- Confirm/destructive/outward → hỏi trước nếu chưa được uỷ quyền rõ.

## Phân quyền (bài học từ quét registry)
- Endpoint ghi/nhạy cảm phải có permission key thật — **đừng để `[SBAuthorize]` rỗng / comment key**.
- Webhook nhận tiền/data ngoài phải verify chữ ký/secret — KHÔNG để anonymous trần.
