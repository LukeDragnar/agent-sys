# Lesson: Cherry-pick FE lên `release_staging` — i18n JSON là STUB, đừng bê bản product

**Sự cố (2026-07-13, task B161 Lazada):** cherry-pick 13 file FE từ `thiennq/product_hotfix` sang `release_staging` → 3 file i18n (`en/vi/zh.json`) conflict nặng: `en.json` staging chỉ **4 dòng** (`HELLO/SORT/SAVE`) còn bản product **10.083 dòng**; `zh.json` bị **xóa hẳn** trên staging.

## Nguyên nhân (chốt)
Trên `release_staging`/prod, **dịch load từ lang API/DB** (`environment.*.SERVICE_API_LANG → core/api/lang`). File `src/assets/i18n/*.json` chỉ là **placeholder stub**. Bản full 10k dòng chỉ tồn tại ở branch product (tiện dev offline).
→ Nếu lấy bản product đè lên staging = commit nhầm cả file dịch mà staging chủ ý giữ stub (kiểu "revert-mất-config", rất khó phát hiện khi review nhanh).

## Cách xử lý đúng
- Conflict i18n khi pick sang staging → **giữ bản staging**: `git checkout --ours <file.json>` (en/vi), `git rm` file staging đã xóa (zh).
- **10 file code** (logic Lazada) vẫn pick bình thường.
- Key i18n mới (vd `btn-in-don-lazada`, `tlt-them-thuoc-tinh-cho-lazada`) → **seed vào lang DB staging bằng SQL multi-lang**, KHÔNG qua file JSON. Không seed → nút/tooltip hiện key thô.

## Bẫy đi kèm cùng session (đổi branch product↔staging)
- **Deps churn:** `node_modules` không tự đổi theo branch. staging cần `ag-grid-*`(từ `libs/*.tgz`)+`leaflet`+`ng-zorro ^15.1.1`; product gỡ bớt → đổi branch xong **build vỡ** (NG1010 `AgGridAngular` undefined + "Can't resolve ...css"). Fix: `npm install --legacy-peer-deps` (repo vướng ERESOLVE `@agm/core`). Xử triệt để: worktree mỗi branch (xem `memory/local/worktree-setup-todo.md`).
- **Debug-only KHÔNG commit:** `launchSettings.json` (`StagingDebug`) + `environment.stagingdebug.ts`. Khi commit/rebase → stash ra trước, pop lại sau.

## Bẫy #2: Field-order entity lệch giữa 2 nhánh → conflict KÉO field không liên quan (2026-07-20)
Cherry-pick commit thêm 3 cột `SalePhuTrach` vào `DonVi.cs` từ product → staging. `IsIndexNew` bị xếp **khác vị trí** giữa product (cuối class) và staging (giữa) → git báo conflict và **lôi cả `IsIndexNew` vào khối incoming**. Nếu lấy nguyên "theirs" → **`IsIndexNew` bị nhân đôi** → build lỗi "duplicate member".
- **Chẩn:** entity SCRM là generated + no migration → 2 nhánh hay xếp field khác thứ tự → conflict hiển thị field ngoài ý đồ commit.
- **Resolve đúng:** giữ field cũ **1 lần** (bản staging), **chỉ thêm field MỚI** của commit. **ĐỪNG** `git checkout --theirs <file>` cả file (revert luôn field-order + thay đổi khác của staging).
- Verify sau resolve: `grep -c "IsIndexNew"` = 1, số field mới đúng số, `grep -E "^(<<<|===|>>>)"` = rỗng.

## Bẫy #3: Staging có feature SONG SONG đụng cùng file → conflict "cả hai cùng thêm" (2026-07-20)
Cherry-pick FE `ticket-ho-tro.component.ts` sang staging → conflict 2 vùng (import + constructor) vì staging đã có feature **Guideline gán ngành** đụng đúng file đó.
- **Resolve:** **giữ CẢ 2** (2 import, 2 constructor param) — thêm dấu `,` cho đúng cú pháp TS (param cuối vẫn để trailing comma được).
- Verify: cả 2 service có mặt + được DÙNG thật (grep usage), phần auto-merge (vd `ngOnInit` gọi `gl.khoiTao()` của staging) còn nguyên.
- Đây là biểu hiện base-staleness: nhánh feature base cũ, target đã tiến → xem thêm `feedback_cherrypick_feature_branch_config_gate`.

Liên quan: `../skills/stagingdebug-config.md`, `../skills/multi-lang/`, `rules/git.md` (cherry-pick sang staging, tránh merge commit thừa; mang hotfix product→feature).
