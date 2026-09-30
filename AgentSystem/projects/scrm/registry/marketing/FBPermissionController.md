---
controller: FBPermissionController
module: marketing
service: FacebookChat
man_hinh_ui: "nội bộ — không có UI (toàn bộ action đã comment; permission FB do FBFanpageController quản khi sync)"
muc_dich: "Controller danh mục quyền fanpage (FBPermission). HIỆN TẠI RỖNG — mọi endpoint (xem/thêm/sửa/xóa/tìm) đều bị comment. Logic quyền page thật do FBFanpageService gọi IFBPermissionService.GetByFacebookUserFanpageId trong lúc đồng bộ fanpage, không qua controller này."
alias: ["FBPermission", "quyền fanpage", "phân quyền page FB", "FB Tasks", "permission fanpage"]
auth: internal
state_machine: []
deprecated: true
entities: [FBPermission]
stored_procs: []
depends:
  - "IFBPermissionService (FacebookChat.Services.Generated) — GetByFacebookUserFanpageId + UpdateAsync override (clear cache)"
  - "ICommonService.GetFBPermissionByFanPageId — đọc cache permission theo FanPageId"
  - "ICommonCacheService.ClearCache_FBPermission_FanPageId"
cross_module_in:
  - "FBFanpageService (SyncPageFB / SyncPageFB_V2) — caller thật của IFBPermissionService khi upsert quyền page theo FB Tasks"
cross_module_out: []
---

# FBPermissionController — Danh mục quyền fanpage (rỗng / nội bộ)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | internal | — | Controller chỉ inject `IFBPermissionService`, **không có action active nào** — tất cả method (TimTheoId/ThemMoi/CapNhat/Xoa/TimTheoDieuKien) đều bị comment. Truy cập sẽ không có route. | — | Toàn bộ controller là skeleton chưa dùng. |

## Endpoint nội bộ (private / service-to-service)
- Controller không expose endpoint. Service `IFBPermissionService` được dùng nội bộ bởi **FBFanpageService**:
  - `GetByFacebookUserFanpageId(fbUser, pageId, userLogin)` — đọc cache `GetFBPermissionByFanPageId`, lọc theo `IdFBUser`+`FanPageId`+`IdDonVi`; dùng khi sync để biết user đã có bản ghi quyền page chưa.
  - `UpdateAsync` (override) — MarkDirty các field + `ClearCache_FBPermission_FanPageId` sau update.
  - `InsertAsync` — tạo mới `FBPermission` (gọi từ FBFanpageService khi page chưa có quyền), lưu `Tasks` (JSON FB Tasks) + `TaskStatus` (admin/editor/moderator → true; advertiser/analyst → false).

## Quan hệ
- **Gọi sang:** không (controller). Service: cache permission qua `ICommonService`/`ICommonCacheService`.
- **Dữ liệu (entities):** `FBPermission` (FanPageId, IdFBUser, IdFBFanpage, Tasks, TaskStatus, IdDonVi).
- **Ripple / đồng bộ:** thay đổi quyền page → clear cache `FBPermission_FanPageId`; ảnh hưởng các màn join FBPermission (TimTheoDieuKien fanpage/post lọc page user có quyền).

## Lỗi đã biết / lưu ý kỹ thuật
- `FBPermissionController.cs:26-112` — 100% action bị comment; controller hiện không có chức năng UI nào → đánh `deprecated: true` để AI bỏ qua nhanh. Quyền page thật xử lý trong `FBFanpageService.SyncPageFB_V2` (`FBFanpageService.cs:731-804`).
- `[TienIch(... "Quản lý danh mục" ...)]` ở controller-level nhưng mô tả không khớp mục đích (copy từ template danh mục).
- Logic phân loại quyền (`FormatTaskStatus`) nằm ở `FBFanpageService.cs:1028-1057`, không ở service permission.
