---
name: Working folder — sửa trực tiếp, không copy
description: Khi sửa code phải làm trực tiếp trên working folder của dev, không tạo ở nơi khác rồi copy
type: feedback
---

Làm việc TRỰC TIẾP trên **working folder** do user chỉ định (confirm đầu session — placeholder `{WORKING_BE}` / `{WORKING_FE}`), không tạo file ở nơi khác rồi copy.

**Why:** Copy là thao tác thừa, dễ lệch version giữa nơi sửa và nơi commit → gây nhầm lẫn.

**How to apply:**
- BE: thao tác trực tiếp vào `{WORKING_BE}`
- FE: thao tác trực tiếp vào `{WORKING_FE}`
- Nếu user có folder read-only reference khác working folder → chỉ đọc, không sửa, không copy sang working.

**Ghi chú:** Mỗi dev có cách tổ chức folder riêng (1 folder vừa code vừa build, hay tách riêng...) — Claude không áp convention, chỉ làm theo path user confirm.
