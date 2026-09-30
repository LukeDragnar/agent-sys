---
name: feedback-no-blind-overwrite-on-merge
description: Khi merge/cherry-pick 2 nhánh đã sửa cùng file, KHÔNG ghi đè mù một bên — verify pre-change baseline kẻo mất code dev khác
metadata:
  type: feedback
---

Khi cherry-pick/merge mà 2 nhánh **cùng sửa song song** một nhóm file (vd cùng reskin trang config), TUYỆT ĐỐI không "ghi đè" lấy nguyên bản một bên (kiểu `git checkout <branch> -- file` hay `-X theirs`) dù user nói "bên X làm chuẩn".

**Why:** nhánh kia có thể đã reskin + thêm feature MỚI HƠN. Ghi đè bản cũ lên = nuốt mất code của dev khác (vụ thật: đè bản FixFormConfig cũ lên staging làm mất feature MeInvoice/hóa đơn nháp của dotuananhh — 4 control LIVE biến mất khỏi UI). `.ts` vẫn còn control nhưng template mới không render → regression thầm lặng.

**How to apply:**
1. So control LIVE **2 chiều** dựa trên **baseline TRƯỚC thay đổi** (vd `<commit>^`), KHÔNG so bản đã-ghi-đè với chính nguồn (vòng lặp → luôn "giống nhau" sai lệch). Strip comment trước khi grep `formControlName`/`[(ngModel)]`/`(click)`/`<app-`.
2. Phát hiện mỗi file: STG-only vs FF-only control + bên nào đã reskin (`grep cfg-page|settings-section|setting-row`).
3. Quyết theo từng file: bên là **superset/mới hơn** thì GIỮ, chỉ **chèn thêm** control bên kia thiếu. Bên chưa reskin thì mới áp reskin.
4. Cherry-pick merge commit: chọn đúng `-m` (xem `git diff <parent>..<commit> --stat` để biết -m nào ra "phần cần"). Tránh `-X theirs` cho template Angular (trộn hunk → hỏng thẻ, vd thừa `</ng-container>` gây NG5002).
5. Verify cuối: không mất control 2 bên, cân bằng thẻ, binding `formControlName` có trong `.ts`, không marker.

Liên quan: [[feedback-verify-current-branch-before-git-ops]]
