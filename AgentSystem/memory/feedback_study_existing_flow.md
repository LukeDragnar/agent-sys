---
name: Study existing flow before coding
description: Phải đọc kỹ luồng hiện có + so sánh field name với API response thực tế trước khi viết code mới
type: feedback
---

Khi implement tính năng mới dựa trên form/luồng đã có (ví dụ: fill data phiếu xuất từ phiếu nhập), PHẢI:

1. **Đọc kỹ luồng hiện có** trước khi code — ví dụ luồng "thêm SP bằng barcode" có toàn bộ logic đơn vị tính, giá vốn, quy đổi. Copy pattern từ đó, không tự viết.
2. **So sánh field name với API response thực tế** — entity field (`TiLeQuyDoi`, `JsonQuyDoi`) khác response field (`tyLeQuyDoiMacDinh`, `bangQuyDoiJson`). Phải xem BE query trả gì.
3. **Hiểu rõ ý nghĩa field** — `giaVon` là đơn giá hay tổng? Xem luồng cũ dùng thế nào, không đoán.
4. **Kiểm tra SP/API có trả field cần dùng trong list data không** trước khi dùng ở HTML template.
5. **Test kỹ trước khi commit** — không commit từng fix nhỏ, gom lại test đủ case rồi mới đẩy 1 lần.

**Why:** Session xuất trả NCC phải fix 5+ lần do không study kỹ luồng barcode add + dùng sai field name + hiểu sai business logic. Mỗi fix lại commit/push/cherry-pick gây rối git history.

**How to apply:** Trước khi viết bất kỳ fill/map data nào giữa 2 form, đọc luồng tạo mới hiện có của form đích, list ra tất cả field cần set, so sánh với API response nguồn.
