---
controller: NotificationConfigV2Controller
module: marketing
service: SMS
man_hinh_ui: "Marketing > Cấu hình thông báo (nâng cao) — dùng khi đơn vị bật cấu hình nâng cao: quản lý cấu hình thông báo theo id, chi tiết + lưu."
muc_dich: "Quản lý cấu hình thông báo tự động bản V2 (nâng cao): lấy theo đơn vị, xem chi tiết theo id, lưu cấu hình. Song hành NotificationConfigController v1; bổ trợ bởi NotificationConfigMauTinV2 (mẫu tin) và NotificationConfigTieuChi (đối tượng)."
alias: ["NotificationConfigV2", "cấu hình thông báo nâng cao", "thông báo v2"]
auth: public
state_machine: []
deprecated: false
entities: [NotificationConfig]
stored_procs: []
depends:
  - "INotificationConfigV2Service (SMS.Service)"
  - "MaTienIchSCRM.TI_SMS — tiện ích gate (lưu ý: TienIch name vẫn ghi 'NotificationConfigController')"
cross_module_in: []
cross_module_out:
  - "Consumer gửi tin tự động (V2) đọc cấu hình thông báo nâng cao"
---

# NotificationConfigV2Controller — Cấu hình thông báo tự động (V2)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình theo đơn vị | (load form) | POST `api/NotificationConfigV2/TimTheoDonViV2` | `NotificationConfigV2_TimTheoDonViV2` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lấy cấu hình thông báo V2 của đơn vị. | EF | — |
| 2 | Chi tiết cấu hình | Xem | POST `api/NotificationConfigV2/GetNotificationConfigById` | `NotificationConfigV2_GetNotificationConfigById` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lấy 1 cấu hình theo id (`ReqGetNoticationConfigById`). | EF | — |
| 3 | Lưu cấu hình | Lưu | POST `api/NotificationConfigV2/SaveNotificationConfig` | `NotificationConfigV2_SaveNotificationConfig` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lưu cấu hình thông báo (`SaveNoticationConfig`). | EF | — |

## Quan hệ
- **Gọi sang:** `INotificationConfigV2Service`.
- **Dữ liệu (entities):** `NotificationConfig` (V2).
- **Ripple / đồng bộ:** đi cùng `NotificationConfigMauTinV2Controller` (mẫu tin theo cấu hình) + `NotificationConfigTieuChiController` (đối tượng/tiêu chí) tạo bộ cấu hình thông báo nâng cao.

## Lỗi đã biết / lưu ý kỹ thuật
- `[TienIch]` name ghi nhầm "NotificationConfigController" thay vì V2 (`NotificationConfigV2Controller.cs:9`) — copy từ v1.
- Cụm V2 gồm 3 controller: `NotificationConfigV2` (cấu hình gốc) + `NotificationConfigMauTinV2` (mẫu tin) + `NotificationConfigTieuChi` (đối tượng áp dụng).
