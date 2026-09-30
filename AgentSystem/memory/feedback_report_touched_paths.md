---
name: Báo path code đã thao tác khi xong việc
description: Mỗi lần làm xong phải báo rõ path folder + file code đã đụng để user biết chỗ mà commit/build
type: feedback
---

Kết thúc **mọi task đụng code**, trong Confirm block phải báo rõ:
- **Working ROOT path** đã thao tác (vd `D:\WORK\SCRM\GIT_2\BE\scrm`, `D:\WORK\SCRM\GIT_2\FE\scrm_frontend`) — để user biết **cd vào folder nào** mà commit / build check.
- **Danh sách file cụ thể** đã sửa (đường dẫn đầy đủ, nhóm theo BE/FE hoặc theo repo folder nếu nhiều root).

**Why:** User + team mở **nhiều session Claude cùng lúc**, mỗi session code ở **folder khác nhau** → dễ quên session này đang làm ở đâu → không biết folder nào cần commit/build. Báo path rõ giúp user định vị ngay.

**How to apply:**
- Confirm cuối task **luôn có mục "Path đã thao tác"** liệt kê ROOT folder(s) + file. KHÔNG chỉ liệt kê tên file trơ (thiếu root → user không biết folder nào).
- 🔴 **CẤM viết tắt đường dẫn bằng `...\`** (vd `...\tinh-lai-gia-von-modal\.component.ts`). Mỗi file phải ghi **ĐẦY ĐỦ từ ổ đĩa**: `D:\WORK\SCRM\GIT_2\FE\scrm_frontend\src\app\pages\...\file.ts`. Workspace có **nhiều bản gần giống** (`GIT`, `GIT_2`, `GIT_3`, `GIT REPORT`) → thiếu root/viết tắt = user KHÔNG biết code ở bản nào → commit/build nhầm chỗ.
- Nếu 1 task đụng **nhiều root** (BE + FE, hoặc nhiều repo GIT/GIT_2) → tách rõ từng root + file thuộc root đó, và **nói rõ tất cả nằm ở GIT_2 (hay bản nào)**.
- Vẫn giữ nguyên [[feedback_build]] (user tự build) — mục này chỉ thêm việc **BÁO path**, không tự build/commit.
- Path lấy từ path user đã confirm trong session (xem rule confirm path mỗi session), KHÔNG tự đoán.

**Lỗi đã mắc (2026-07):** báo bảng file dạng `...\tinh-lai-gia-von-modal\.component.ts` (tương đối, thiếu root) → user không biết ở `GIT` hay `GIT_2`/`GIT_3`. Phải ghi full `D:\WORK\SCRM\GIT_2\FE\scrm_frontend\src\app\...`.
