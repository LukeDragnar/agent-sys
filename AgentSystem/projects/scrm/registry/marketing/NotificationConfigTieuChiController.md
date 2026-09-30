---
controller: NotificationConfigTieuChiController
module: marketing
service: SMS
man_hinh_ui: "Marketing > Cấu hình thông báo (nâng cao) > Đối tượng/tiêu chí áp dụng — chọn sản phẩm/dịch vụ… mà cấu hình thông báo áp dụng."
muc_dich: "Quản lý tiêu chí/đối tượng áp dụng của cấu hình thông báo V2 (NotificationConfigTieuChi): lấy/lưu/xóa danh sách đối tượng (sản phẩm, dịch vụ…) mà một cấu hình thông báo sẽ áp dụng."
alias: ["NotificationConfigTieuChi", "tiêu chí thông báo", "đối tượng áp dụng thông báo", "sản phẩm dịch vụ thông báo"]
auth: public
state_machine: []
deprecated: false
entities: [NotificationConfigTieuChi, NotificationConfig]
stored_procs: []
depends:
  - "INotificationConfigTieuChiService (SMS.Service)"
  - "MaTienIchSCRM.TI_SMS — tiện ích gate controller-level"
cross_module_in: []
cross_module_out:
  - "Consumer gửi tin tự động lọc đối tượng (sản phẩm/dịch vụ) theo tiêu chí này trước khi gửi"
---

# NotificationConfigTieuChiController — Tiêu chí/đối tượng áp dụng cấu hình thông báo (V2)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy danh sách đối tượng | (load) | POST `api/NotificationConfigTieuChi/TimTheoDieuKien` | `NotificationConfigTieuChi_TimTheoDieuKien` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lấy danh sách đối tượng (sản phẩm/dịch vụ…) theo điều kiện. | EF | — |
| 2 | Lưu đối tượng áp dụng | Lưu | POST `api/NotificationConfigTieuChi/SaveNotificationConfigTieuChi` | `NotificationConfigTieuChi_SaveNotificationConfigTieuChi` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lưu danh sách đối tượng cho cấu hình. | EF | — |
| 3 | Xóa tiêu chí | Xóa | POST `api/NotificationConfigTieuChi/XoaNotificationConfigTieuChi` | `NotificationConfigTieuChi_XoaNotificationConfigTieuChi` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Xóa cấu hình tiêu chí (`ReqXoaNotificationConfigTieuChi`). | EF | — |

## Quan hệ
- **Gọi sang:** `INotificationConfigTieuChiService`.
- **Dữ liệu (entities):** `NotificationConfigTieuChi` (đối tượng/tiêu chí áp dụng), `NotificationConfig`.
- **Ripple / đồng bộ:** consumer dùng tiêu chí để lọc đối tượng nhận tin thông báo tự động (V2).

## Lỗi đã biết / lưu ý kỹ thuật
- Là 1 trong 3 controller của cụm cấu hình thông báo V2 (xem `NotificationConfigV2Controller`): cấu hình gốc + mẫu tin + tiêu chí đối tượng.
