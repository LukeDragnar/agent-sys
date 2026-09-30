---
name: Branch naming convention
description: Format tên nhánh git cho project scrm
type: feedback
---

Format: `{username}/{feature_name}_{yyyy_mm_dd}` — snake_case tiếng Việt không dấu, phân cách bằng `_`.

Ví dụ: `thiennq/chitieu_batch_store_2026_04_15`, `thiennq/dong_thoi_ncc_kh_2026_03_20`

**Why:** User feedback underscore dễ tìm/double-click select hơn dash (kebab-case), confirmed 2026-04-15.

**How to apply:** Khi được hỏi đặt tên nhánh, dùng format này với `_`. Username mặc định là `thiennq`. Nếu có tên branch cũ dùng dash, giữ nguyên không rename.
