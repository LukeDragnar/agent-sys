---
name: feedback-build-repo-commit-thang-product
description: Repo trong _SourceForBuild là repo BUILD — bump version + gitignore commit thẳng nhánh base (product), KHÔNG cần MR, đừng cảnh báo thừa
metadata:
  type: feedback
---

Các repo dưới `_SourceForBuild/` (`build_ecommerce_connect`, `scrm_build`, `build_scrm_frontend`,
`build_delivery_connect`, `scrm_stg`, …) là **repo BUILD/deploy**, không phải nơi review code tính năng.
Ở đó việc **bump version image trong `docker_build_and_push-*.bat`** và **thêm rule `.gitignore`** được
**commit + push THẲNG nhánh base** (`product` / `release_product`) — **đúng lệ, KHÔNG mở MR**.

**Why:** thiennq nói rõ (31/07/2026): *"lên thẳng product, vì vướng mấy cái obj, bin mới tôi mới phải nhờ bạn đó"*.
Lịch sử repo cũng vậy: `product: up version product-1.96 -> product-1.97` là commit thẳng của chính thiennq.
Việc Claude nhắc *"commit này đi thẳng product, không qua review"* ở ngữ cảnh này là **cảnh báo thừa** —
làm loãng những cảnh báo thật (cảnh báo chỉ có giá trị khi hiếm).

**How to apply:**
- User nhờ commit/push ở path `_SourceForBuild/*` → xem diff, nếu chỉ là **version bump / `.gitignore` / config build**
  thì commit + push thẳng nhánh base, **không hỏi MR, không cảnh báo "đi thẳng product"**.
- Message theo mẫu sẵn có của repo: `product: up version product-1.97 -> product-1.98`.
- **Project mới trong solution build** → nhớ thêm `/<Project>/bin` + `/<Project>/obj` vào `.gitignore`
  (mỗi project 1 cặp dòng riêng — repo này không dùng pattern `**/bin`).
- ⚠️ **Bẫy đã dính:** `.gitignore` của `build_ecommerce_connect` **không có newline cuối file** → `>>` append
  làm dòng mới dính vào dòng cuối, **phá luôn rule đang chạy**. Append xong **phải `git status` + đọc `git diff`**
  để chắc không nuốt mất rule cũ.
- Vẫn giữ nguyên các cấm khác: **không tự deploy/đẩy image**, không đụng `PortainerProduction.xml`,
  không gọi endpoint `10.0.63.x`. Xem [[feedback-mr-link-only]] (không tự tạo MR ở repo code tính năng).
