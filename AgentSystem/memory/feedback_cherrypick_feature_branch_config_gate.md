---
name: feedback-cherrypick-feature-branch-config-gate
description: Khi cherry-pick nhánh feature nghiatd (ngôn ngữ/UI) vào release_staging — giữ config staging, gate bằng so tập formControlName
metadata:
  type: feedback
---

Các nhánh feature đa-ngôn-ngữ / UI của nghiatd (vd `nghiatd/configLanguage`, `FixFormConfig`) trên scrm_frontend đều **tách ra từ base CŨ** (dòng `release_product`) và đã phân nhánh rất xa khỏi `release_staging` (hàng nghìn commit lạ). Hệ quả khi pick vào staging:

- **KHÔNG merge cả nhánh** (kéo theo cả nghìn commit rác). Chỉ cherry-pick đúng các commit feature (lọc theo prefix message / tác giả).
- Staging thường **đã có sẵn một phần** việc đó (bản mới hơn) → pick thẳng/lấy bản feature = **regression** (đổi key, mất sửa mới, **mất config staging mới thêm**).

**Why:** FixFormConfig (2026-06) feature chỉ được phép đụng **ngôn ngữ + giao diện** (tooltip/text/scss), **cấm thêm/bớt cấu hình**. Nhưng vì base cũ, lấy nguyên feature làm **mất 14 formControlName** staging có và **thừa 1** feature tự thêm.

**How to apply:**
1. Cherry-pick các commit feature lên nhánh tạm tách từ staging, dùng `-X ours` (conflict → giữ staging) → bảo toàn config + code staging; phần feature ở vùng không đụng (tooltip/text/scss mới) vẫn vào.
2. **Cổng kiểm tra bắt buộc:** so tập `formControlName="..."` (sort+uniq -c) giữa kết quả và `release_staging` — phải GIỐNG HỆT (0 thêm, 0 bớt). Lệch → sửa file đó.
3. Chuyển về staging dạng **uncommitted** cho dev review (`git checkout <tmp> -- .` rồi `git restore --staged .`), KHÔNG commit; xóa nhánh tạm.
4. Trang nào feature rewrite cả khối → `-X ours` sẽ bỏ tooltip feature ở đó (đánh đổi giữ config); thêm tay sau nếu cần.

**Recipe pick feature → staging (task 1046, đã chạy OK) — 1 NHÁNH CHUNG tái dùng, khỏi đẻ nhánh tạm:**
1. **`git fetch origin release_staging`** TRƯỚC (pick lên staging MỚI NHẤT, không base cũ).
2. **`git checkout -B thiennq/staging-pick origin/release_staging`** — `-B` reset lại cùng 1 tên mỗi lần (không tạo nhánh mới). Stash config debug local trước khi checkout.
3. `git cherry-pick <các commit feature>` (không merge cả nhánh).
4. **Chiến lược resolve conflict** (khác nhau theo loại file):
   - **File config / i18n (en/vi/zh.json)** → giữ **bản staging** (`git checkout --ours <file>`; file staging đã xóa → `git rm` theo). i18n text của feature thường chỉ cosmetic (fallback `tsb.t` đã có VN).
   - **File LOGIC/feature (component .ts/.html, service .cs)** → resolve tay giữ **CẢ code staging LẪN feature** (đừng `-X ours` cả cụm — sẽ mất feature). Conflict thường ở vùng staging reformat/đổi field lân cận, không phải chỗ feature.
5. **CỔNG formControlName** (bắt buộc): `git grep -oh 'formControlName="[^"]*"' HEAD -- '*.html' | sort | uniq -c` vs `origin/release_staging` — diff phải **chỉ có** đúng thay đổi cố ý của feature (0 mất config staging khác).
6. Push `thiennq/staging-pick` → tạo MR **target `release_staging`** (không tự merge). Deploy đủ host gồm consumer riêng nếu đụng sync.
> BE ít formControlName nhưng vẫn fetch-first + cherry-pick + để ý auto-merge service .cs (compile mới chắc). Config debug local KHÔNG lọt (staging-pick lấy bản staging).

**⚠️ Staging có thể lệch LOGIC (không chỉ config/i18n):** conflict .cs không phải lúc nào cũng là "reformat/đổi field lân cận" để giữ-cả-hai. Có khi staging & product **đọc field KHÁC NHAU** cho cùng 1 thứ (case 2026-07-24: staging `GiaoHangMotPhanCOD = reqBody.ReturnCod` vs product `= reqBody.CodSignPart`) — 2 bản **loại trừ nhau**, không "giữ cả hai" được. Resolve theo **semantic ĐÚNG end-to-end** (khớp bên gửi), thường = **lấy bản fix đè logic diverge của staging**, KHÔNG mù quáng `--ours`. Xem `../projects/scrm/lessons/cod-giao-1-phan-webhook-ghi-de-0.md`.

Liên quan: [[feedback-multilang-review-key-collision]] · [[feedback-scrm-ngaymuonnhan-vs-ngaygiao]] · [[cod-giao-1-phan-webhook-ghi-de-0]]
