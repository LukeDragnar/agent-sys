---
controller: CauHinhController
module: he-thong
service: OriginalProject
man_hinh_ui: "nội bộ — Cấu hình service khách hàng (OriginalProject): cấu hình chung + cấu hình khởi tạo (init)"
muc_dich: "Quản lý cấu hình của service OriginalProject (cấu hình khách hàng): lấy cấu hình hiện tại, lưu cấu hình khởi tạo, lấy cấu hình init; có endpoint nội bộ service-to-service (PrivateGetConfig)."
alias: ["Cấu hình OriginalProject", "cấu hình khách hàng", "CauHinh service khách hàng"]
auth: mixed
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["ICauHinhService", "CurrentUser (token)"]
cross_module_in: ["FE OriginalProject lấy/lưu cấu hình", "Service nội bộ khác gọi PrivateGetConfig (PrivateSBAuthorize)"]
cross_module_out: ["Cấu hình ảnh hưởng hành vi các tính năng OriginalProject"]
---

# CauHinhController (OriginalProject) — Cấu hình service khách hàng

Quản lý cấu hình OriginalProject qua `ICauHinhService`. Tiện ích `TI_OriginalProject`. Auth `mixed`: phần lớn action chỉ `[SBAuthorize]` (đăng nhập, không key), `SaveCauHinhInit` có key riêng, `PrivateGetConfig` dùng `[PrivateSBAuthorize]` (service-to-service).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình | — | GET `api/CauHinh/Get` | — (chỉ `[SBAuthorize]`) | internal | — | Trả cấu hình theo CurrentUser. | EF (service) | — |
| 2 | Lưu cấu hình khởi tạo | nút Lưu | POST `api/CauHinh/SaveCauHinhInit` | `CauHinh_OriginalProject_LuuCauHinhKhachHang` | internal | — | Validate ModelState → `SaveCauHinhInit(inSetting, CurrentUser)`. Invalid → trả lỗi. | EF | — |
| 3 | Lấy cấu hình init | — | GET `api/CauHinh/GetCauHinhInit` | — (chỉ `[SBAuthorize]`) | internal | — | Trả cấu hình init theo CurrentUser. | EF | — |
| 4 | Lấy config (nội bộ) | — | POST `api/CauHinh/PrivateGetConfig` | — (`[PrivateSBAuthorize]`) | internal (service-to-service) | — | Trả cấu hình theo CurrentUser; dành cho gọi nội bộ giữa service. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] Service khác gọi `POST api/CauHinh/PrivateGetConfig` (`[PrivateSBAuthorize]`) để lấy cấu hình OriginalProject — cùng logic `GetAsync` như `Get` nhưng qua kênh private auth.

## Quan hệ
- **Gọi sang:** `ICauHinhService` (Get / SaveCauHinhInit / GetCauHinhInit).
- **Dữ liệu (entities):** cấu hình khách hàng OriginalProject (qua service, DTO `CauHinhSettingInitDto`).
- **Ripple / đồng bộ:** Lưu cấu hình → các tính năng OriginalProject đọc cấu hình mới.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs:24,44` — `Get` và `GetCauHinhInit` chỉ `[SBAuthorize]` (không permission key) → mọi user đăng nhập đọc được cấu hình, không phân quyền chi tiết.
- Trùng tên controller `CauHinhController` với module ke-toan → node này suffix `_OriginalProject` để phân biệt.
