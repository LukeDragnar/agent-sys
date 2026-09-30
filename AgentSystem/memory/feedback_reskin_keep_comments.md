---
name: feedback-reskin-keep-comments
description: Khi reskin/refactor UI, TUYỆT ĐỐI không xóa comment (kể cả code đã comment / dead-code) — giữ verbatim
metadata:
  type: feedback
---

Khi reskin giao diện hoặc refactor template (HTML Angular), **GIỮ NGUYÊN MỌI COMMENT** của bản gốc — bao gồm cả các khối code đã bị comment (`<!-- <nz-form-item>...formControlName="ghiChuChoSale"... --> `), comment giải thích, comment ToDo, comment placeholder (`<!-- {{ status.text }} -->`).

**Why:** dev cố ý để lại các comment đó (dead-code chờ bật lại, ghi chú nghiệp vụ, ToDo). Subagent reskin viết lại HTML từ đầu thường **nuốt mất comment** → mất thông tin/ý đồ của dev. User đã bắt lỗi nhiều comment bị xóa (vd `ghiChuChoSale`, `isBoQuaKiemTraConHangKhiChotDon`, block `listUserChiNhanhTong`).

**Ưu tiên:** quan trọng nhất là comment **CÓ CODE** (dead-code block đã comment, vd `<!-- <nz-form-item>...formControlName=... -->`) — phải giữ/khôi phục trước tiên. Comment label/divider thuần (`<!-- DÀNH CHO SUPER USER -->`, `<!-- DÀNH CHO ĐƠN VỊ -->`) ít quan trọng, không cần cố khôi phục nếu reskin đã có section header tương đương; nhưng cũng KHÔNG xóa nếu đang có sẵn (gốc).

**How to apply:**
1. Mọi prompt reskin cho subagent PHẢI ghi rõ: "GIỮ NGUYÊN VERBATIM tất cả comment `<!-- ... -->`, đặc biệt code đã comment — không được xóa/bỏ comment nào."
2. Sau reskin, verify: `git show <base>:file | grep -oE '<!--' | wc -l` so với bản mới; và so nội dung comment (strip whitespace) 2 chiều để chắc không mất comment nào. Tự thêm comment section của mình thì OK nhưng KHÔNG được xóa comment gốc.
3. Nếu lỡ mất, khôi phục nguyên văn từ baseline (commit trước reskin / MERGE_HEAD / release_product) vào đúng vị trí tương ứng.

Liên quan: [[feedback-no-blind-overwrite-on-merge]]
