---
controller: NotificationConfigMauTinV2Controller
module: marketing
service: SMS
man_hinh_ui: "Marketing > Cấu hình thông báo (nâng cao) > Mẫu tin theo cấu hình — gán/lưu mẫu tin cho từng NotificationConfig."
muc_dich: "Quản lý mẫu tin gắn với cấu hình thông báo V2 (NotificationConfigMauTin): lấy danh sách mẫu theo idNotificationConfig và lưu mẫu tin cho cấu hình đó."
alias: ["NotificationConfigMauTinV2", "mẫu tin theo cấu hình thông báo", "mẫu thông báo v2"]
auth: public
state_machine: []
deprecated: false
entities: [NotificationConfigMauTin, NotificationConfig]
stored_procs: []
depends:
  - "INotificationConfigMauTinV2Service (SMS.Service)"
  - "MaTienIchSCRM.TI_SMS — tiện ích gate (TienIch name vẫn ghi 'NotificationConfigMauTinV2Controller'... thực ra ghi 'NotificationConfigController')"
cross_module_in: []
cross_module_out:
  - "Consumer gửi tin tự động dùng mẫu tin gắn theo NotificationConfig khi gửi"
---

# NotificationConfigMauTinV2Controller — Mẫu tin theo cấu hình thông báo (V2)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy mẫu theo cấu hình | (load form) | POST `api/NotificationConfigMauTinV2/LayDanhSach_ByIdNotificationConfig` | `NotificationConfigMauTinV2_LayDanhSach_ByIdNotificationConfig` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lấy mẫu tin gắn với 1 `idNotificationConfig`. | EF | — |
| 2 | Lưu mẫu tin | Lưu | POST `api/NotificationConfigMauTinV2/SaveNotificationConfigMauTin` | `NotificationConfigMauTinV2_SaveNotificationConfigMauTin` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lưu mẫu tin cho cấu hình (`SaveNoticationConfig`). | EF | — |

## Quan hệ
- **Gọi sang:** `INotificationConfigMauTinV2Service`.
- **Dữ liệu (entities):** `NotificationConfigMauTin` (mẫu tin theo cấu hình), `NotificationConfig`.
- **Ripple / đồng bộ:** nội dung mẫu này được consumer dùng khi gửi tin thông báo tự động (V2).

## Lỗi đã biết / lưu ý kỹ thuật
- `[TienIch]` name vẫn ghi "NotificationConfigController" (`NotificationConfigMauTinV2Controller.cs:10`) — copy/paste, không phản ánh đúng controller.
- Là 1 trong 3 controller của cụm cấu hình thông báo V2 (xem `NotificationConfigV2Controller`).
