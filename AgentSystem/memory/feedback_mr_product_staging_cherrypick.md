---
name: Workflow MR — product only, staging cherry-pick
description: Feature branch chỉ tạo MR vào release_product; lên release_staging chỉ bằng cherry-pick trực tiếp
type: feedback
---

## Luồng deploy chuẩn SCRM

1. **Feature branch → release_staging:** CHỈ `git cherry-pick` (không MR, không merge). Mục đích: build image staging test nhanh.
2. **Feature branch → release_product:** CHỈ qua Merge Request (MR) trên GitLab UI. User/lead tự review + click merge. Claude KHÔNG tự merge, KHÔNG push thẳng vào product.

## KHÔNG bao giờ

- ❌ Tạo MR target `release_staging` (staging chỉ cherry-pick, không MR)
- ❌ Tạo MR target các protected branch ngoài `release_product`
- ❌ Merge feature vào staging (dù qua MR hay CLI)
- ❌ Cherry-pick feature vào product (không — phải qua MR)

**Why:** User confirm rule 2026-04-15. Staging dùng cherry-pick để pick nhanh + độc lập nhiều feature song song test. Product phải qua MR để có audit + review + CI check đầy đủ.

**How to apply:**
- Khi user nói "đẩy lên staging" → `git cherry-pick` feature commits vào `release_staging` local → `git push origin release_staging`.
- Khi user nói "đưa vào product" → đưa link MR `new?target=release_product` cho user tự tạo/click merge trên UI.
- Nếu push feature → GitLab auto-show MR creation link, đó là link tạo MR target DEFAULT branch (thường `main` hoặc `release_product`) — chỉ share link với user, không tự tạo MR.

## 🔴 THÓI QUEN BẮT BUỘC mỗi thay đổi: PUSH FEATURE **+** PICK STAGING (đủ CẢ HAI)

User nhắc (2026-07-25): **"luôn push feature, pick staging"** — mỗi lần sửa code là làm ĐỦ 2 việc, đừng làm 1 quên 1:
1. **Commit + push FEATURE branch** (`<user>/<task>`) → để lên product qua MR.
2. **Cherry-pick sang `release_staging`** (+ push) → để build/deploy staging.
- Áp cho **CẢ BE lẫn FE** (2 repo riêng: `scrm` + `scrm_frontend`) — nhớ cả hai.
- **VERIFY origin THẬT có code sau push** (`git log origin/<branch>` + `git ls-tree`/`git show` kiểm file/dòng), KHÔNG tin "push OK" suông. **SHOW log ra cho user** (đừng nói suông "đã push rồi").
- 🔴 **VERIFY FOLDER BUILD đã PULL** trước khi build image: SCRM build từ folder riêng `D:\PROJECT\Git\_SourceForBuild\` (`scrm_stg`=BE staging, `scrm_frontend_stg`=FE staging, `scrm_build`=BE product...). Folder build **có thể CŨ dù branch đã push** → `git rev-parse HEAD` phải == `origin/<branch build>`; behind → `git pull --ff-only` TRƯỚC khi build. ⚠️ Bẫy phụ: 1 file có thể "tình cờ đúng giá trị" dù folder cũ (net-zero qua nhiều commit) → **đừng tin 1 file, phải so HEAD==origin**.

**Why:** Vụ 2026-07-24 (build+deploy product mà **không có code**): **root-cause = FOLDER BUILD chưa pull** (branch đã push nhưng `_SourceForBuild/*` còn cũ). Cũng có thể do push feature nhưng quên pick staging (hoặc ngược lại) = 1 môi trường thiếu code. → luôn đủ: push feature + pick staging + **pull folder build** + verify origin, SHOW log.

Liên quan: [[feedback_cherrypick_feature_branch_config_gate]] (giữ config staging khi pick) · [[feedback_no_merge_staging_to_feature]].
