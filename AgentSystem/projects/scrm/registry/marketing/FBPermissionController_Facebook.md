---
controller: FBPermissionController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI (scaffold, toàn bộ action bị comment)"
muc_dich: "Scaffold quản lý FBPermission (quyền/Tasks của user trên fanpage). Toàn bộ endpoint bị comment-out → controller không expose action nào. FBPermission thực tế được upsert ngầm ở luồng SyncPageFB của FBFanpageController_Facebook."
alias: ["quyền fanpage", "FBPermission", "phân quyền page FB"]
auth: internal
state_machine: []
deprecated: true
entities: [FBPermission]
stored_procs: []
depends:
  - "IFBPermissionService (chỉ inject, không endpoint nào active)"
cross_module_in: []
cross_module_out: []
---

# FBPermissionController (Facebook) — Quản lý quyền fanpage (SCAFFOLD)

> **Scaffold rỗng**: ctor inject `IFBPermissionService` nhưng TẤT CẢ action (TimTheoId, ThemMoi, CapNhatThongTin, Xoa, TimTheoDieuKien) đều bị comment-out (FBPermissionController.cs:26-112). Không có endpoint HTTP nào hoạt động. `[TienIch("FBPermissionController","Quản lý danh mục",TI_Facebook)]`.
>
> `FBPermission` (Tasks/TaskStatus quyền page) được TẠO/CẬP NHẬT ngầm trong `FBFanpageController_Facebook.SyncPageFB` và join để áp quyền hàng ở FBPost/FBMessage — KHÔNG qua controller này.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | nội bộ — không có UI | — | (toàn bộ comment-out) | — | internal | Không expose | Không có endpoint active. | — | Scaffold; nếu cần CRUD quyền page phải mở comment + thêm SBAuthorize key. |

## Quan hệ
- **Dữ liệu (entities):** `FBPermission` (Tasks/TaskStatus của user ↔ fanpage) — được seed ở luồng đồng bộ fanpage.
- **Ripple / đồng bộ:** không (controller inert).

## Lỗi đã biết / lưu ý kỹ thuật
- `FBPermissionController.cs:26-112` — toàn bộ method comment-out → controller không hoạt động (deprecated/scaffold).
