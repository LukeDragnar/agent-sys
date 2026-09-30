---
name: KHÔNG động vào release_product (chỉ pull)
description: Cấm mọi thao tác ghi lên release_product — không commit, push, merge, cherry-pick TRỰC TIẾP lên branch này
type: feedback
---

`release_product` là branch **PROTECTED** — đây là source of truth cho code đang chạy production.

**Claude CHỈ được:**
- `git fetch origin release_product` — lấy refs về local
- `git pull origin release_product` khi đang ở local `release_product` branch (update local)
- `git checkout release_product` để tham khảo
- `git rebase origin/release_product` (rebase feature LÊN product, không động product)
- `git log origin/release_product` để tra cứu

**Claude TUYỆT ĐỐI KHÔNG được:**
- `git commit` khi đang ở branch `release_product` local
- `git push origin release_product` (ghi lên shared)
- `git cherry-pick <commit> → release_product`
- `git merge <feature> → release_product`
- Force push release_product dưới bất kỳ hình thức nào
- Đụng PR/MR target release_product mà không có user confirm đa tầng

**Why:** User confirm rule 2026-04-15 tuyệt đối. `release_product` chạy prod → sai 1 cái sập thật, không rollback nhanh được. Flow chuẩn: code vào feature → cherry-pick staging test → PR staging → product (user/lead tự merge).

**How to apply:**
- Khi user nói "đưa code vào product" → hiểu là cần tạo MR target release_product, để user tự merge trên UI GitLab — KHÔNG tự push
- Khi user dùng từ "pick/merge lên product" → hỏi lại để chắc chắn + confirm 2 lần + chỉ tạo commit trên local, user push thủ công
- Rule này áp dụng cho CẢ `release_product` ở TẤT CẢ repo (SCRM chính, và các repo khác nếu có branch tên tương tự)
