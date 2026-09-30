---
name: KHÔNG merge release_staging vào feature branch
description: Feature branch chỉ rebase/merge từ release_product — cấm lấy code từ release_staging
type: feedback
---

Feature branch (`thiennq/*`, `anhdt/*`, ...) **CHỈ** được đồng bộ với `release_product` — KHÔNG bao giờ merge/rebase từ `release_staging` hoặc `release_sprint`.

**Why:** User confirm rule 2026-04-15. `release_staging` chứa code đang test (có thể chưa stable, có cherry-pick từ nhiều feature khác). Merge staging vào feature sẽ kéo code chưa duyệt + gây conflict khi deploy lên product. Dev flow chuẩn: feature ← release_product; feature cherry-pick hoặc MR → release_staging (chỉ chiều này).

**How to apply:**
- Khi feature bị behind → `git rebase origin/release_product` hoặc `git merge origin/release_product` (chọn 1)
- Khi MR feature → release_product show nhiều file → nguyên nhân là feature behind release_product (không phải staging). Rebase từ release_product.
- Không gọi `git pull origin release_staging` trên feature branch, BAO GIỜ.
- Chỉ cherry-pick commit từ feature → release_staging (1 chiều).
