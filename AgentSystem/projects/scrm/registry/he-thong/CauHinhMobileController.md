---
controller: CauHinhMobileController
module: he-thong
service: Core
man_hinh_ui: "nội bộ / app mobile — cấu hình core cho mobile (cấp đơn vị + cấp user)"
muc_dich: "Lưu/lấy cấu hình core dành cho mobile: cấp đơn vị (CauHinhMobileService) và cấp từng user (ConfigMobileUserService)."
alias: ["CauHinhMobile", "cấu hình mobile", "cấu hình core mobile"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinhMobileSetting]
stored_procs: []
depends: ["ICauHinhMobileService", "IConfigMobileUserService"]
cross_module_in: ["App mobile gọi SaveAsync/GetAsync (cấp đơn vị) và SaveCauHinhUserAsync/GetCauHinhUserAsync (cấp user)"]
cross_module_out: []
---

# CauHinhMobileController — Cấu hình core cho mobile

Lưu/lấy cấu hình mobile ở 2 cấp: **đơn vị** (`_service` = ICauHinhMobileService) và **user** (`_serviceMobileUser` = IConfigMobileUserService). Tất cả endpoint `[SBAuthorize()]` rỗng; permission key gốc bị comment.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình mobile (đơn vị) | Lưu | POST `api/CauHinhMobile/SaveAsync` | `CauHinh_Core_Mobile_LuuCauHinh` (DV-AD-TONG, HT-CSKH) | public | — | Upsert `CauHinhMobileSetting` cấp đơn vị theo `CurrentUser`. | EF | — |
| 2 | Lấy cấu hình mobile (đơn vị) | (load) | POST `api/CauHinhMobile/GetAsync` | — (login; key bị comment) | public | — | Trả cấu hình mobile của đơn vị. | EF | Key `_GetCauHinh` bị comment |
| 3 | Lưu cấu hình mobile (user) | Lưu | POST `api/CauHinhMobile/SaveCauHinhUserAsync` | — (login; key bị comment) | public | — | `_serviceMobileUser.SaveCauHinhUserAsync` lưu cấu hình theo user. | EF | Key `_ByUserId_LuuCauHinh` bị comment |
| 4 | Lấy cấu hình mobile (user) | (load) | POST `api/CauHinhMobile/GetCauHinhUserAsync` | — (login; key bị comment) | public | — | Trả cấu hình theo user hiện tại. | EF | Key `_ByUserId_GetCauHinh` bị comment |

## Quan hệ
- **Gọi sang:** `ICauHinhMobileService` (cấp đơn vị), `IConfigMobileUserService` (cấp user).
- **Dữ liệu (entities):** `CauHinhMobileSetting`.
- **Ripple / đồng bộ:** không có cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhMobileController.cs:33,42,51` — permission key gốc bị comment, chỉ còn `[SBAuthorize()]` rỗng → mọi user login đọc/ghi được cấu hình (kể cả cấp đơn vị Get).
