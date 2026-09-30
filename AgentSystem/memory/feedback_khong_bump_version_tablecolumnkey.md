---
name: feedback_khong_bump_version_tablecolumnkey
description: KHÔNG thêm hậu tố version (_V1/_V2) vào TableColumnKey của app-table-setting — user có chức năng xóa cache layout.
metadata:
  type: feedback
---

Khi khai `TableColumnKey` cho `app-table-setting` (persist layout cột), **đặt key thuần** (vd `DM_TRANG_THAI_TICKET`, `TICKET_DRILL_TK`), **KHÔNG** thêm `_V1`/`_V2`.

**Why:** Team đã có sẵn **chức năng xóa cache layout** trong app → khi đổi cột/thứ tự mặc định, user tự clear cache chứ không cần bump version key. Bump version chỉ làm key rác dài dòng.

**How to apply:** Đặt tên key mô tả bảng, không kèm số version. Nếu đổi cấu trúc cột mặc định → nhắc user "xóa cache layout" thay vì đổi key. (Recipe `projects/scrm/skills/table-list.md` từng ghi "bump `_V2`" — với SCRM này thì KHÔNG cần.) Liên quan [[feedback_config_thay_hardcode_can_gate_deploy]].
