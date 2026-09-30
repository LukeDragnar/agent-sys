---
name: Build/Deploy Staging — quy trình và folder
description: Trỏ sang nguồn sự thật DUY NHẤT cho build/deploy staging (rules/build.md + agents/deploy-agent.md)
type: feedback
---

Chỉ được build/deploy staging sau khi user **confirm 2 lần**. **TUYỆT ĐỐI KHÔNG** động vào Production / Sprint / Devonline.

**Why:** Build sai folder hoặc sai nhánh có thể deploy code lỗi lên staging, ảnh hưởng toàn team test.

## 🔴 Nguồn sự thật DUY NHẤT — đọc 2 file này, đừng theo file này

- **`rules/build.md`** — luật build/deploy đầy đủ (folder invariant · cổng cứng path+remote+branch · rà-soát-rồi-chốt · verify `HEAD == origin` · quy trình BE/FE · `npm ci --legacy-peer-deps` · tag đè `staging-1.0` phải recreate stack).
- **`agents/deploy-agent.md`** — agent spawn được thực thi luật đó (Pha 0 → 3, 2 cổng confirm, format report).

> ⚠️ Bản cũ của file này dạy 3 điều **nay đã SAI**, đừng làm theo trí nhớ:
> - ~~"có người build ngay trên folder code, không áp convention"~~ → nay **mỗi môi trường một folder riêng**, cấm build trong `_SourceForCode/Group*`.
> - ~~"chạy `docker_build_and_push-Staging.bat`"~~ → nay **cấm gọi thẳng `.bat`** (cuối bat có `pause` → treo agent); chạy 3 lệnh nối `&&`.
> - ~~"`git checkout release_staging && git pull`"~~ → nay **cấm tự checkout**; chỉ `pull --ff-only` trong folder đã đúng nhánh sẵn.

Liên quan: [[feedback_mr_product_staging_cherrypick]] (push feature + pick staging + verify origin) · `rules/git.md` (ngoại lệ git cho deploy-agent).
