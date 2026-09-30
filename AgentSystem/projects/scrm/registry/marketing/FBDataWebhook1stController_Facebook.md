---
controller: FBDataWebhook1stController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI. Route api/FBDataWebhook1st (Controllers, service Facebook). TOÀN BỘ ENDPOINT BỊ COMMENT."
muc_dich: "Controller danh mục FBDataWebhook1st (log webhook FB lớp 1 — chỉ lưu, chưa xử lý) ở Facebook.API — RỖNG về endpoint: ctor inject IFBDataWebhook1stService nhưng toàn bộ 5 action CRUD bị comment cứng. Scaffold danh mục sinh tự động, chưa kích hoạt."
alias: ["FBDataWebhook1st danh mục", "log webhook 1st rỗng"]
auth: public
state_machine: []
deprecated: true
entities: [FBDataWebhook1st]
stored_procs: []
depends:
  - "IFBDataWebhook1stService (inject, không dùng — action bị comment)"
cross_module_in: []
cross_module_out: []
---

# FBDataWebhook1stController — FBDataWebhook1st (Controllers, COMMENTED OUT)

> Route `api/FBDataWebhook1st`. Gate `[TienIch]` cũng bị comment. **Toàn bộ action CRUD bị comment** — không expose endpoint.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (rỗng) | — | — | — | — | — | Không expose endpoint (tất cả comment). | — | TimTheoId/ThemMoi/CapNhat/Xoa/TimTheoDieuKien đều bị comment (`FBDataWebhook1stController.cs:26-111`). |

## Lỗi đã biết / lưu ý kỹ thuật
- `FBDataWebhook1stController.cs:13,26-111` — `[TienIch]` + 5 action CRUD comment toàn bộ; chỉ còn ctor inject `IFBDataWebhook1stService` (entity `FBDataWebhook1st` = nhánh "chỉ lưu" của webhook cũ).
- `deprecated: true` — scaffold danh mục chưa kích hoạt.
