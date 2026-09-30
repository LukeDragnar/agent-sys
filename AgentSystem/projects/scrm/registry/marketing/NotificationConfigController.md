---
controller: NotificationConfigController
module: marketing
service: SMS
man_hinh_ui: "Marketing > Cấu hình thông báo (đơn vị bật/tắt gửi tin theo sự kiện: tác nghiệp, trạng thái đơn hàng…). Bản v1."
muc_dich: "Quản lý cấu hình thông báo tự động theo đơn vị (NotificationConfig) bản v1: bật/tắt việc gửi SMS/Zalo theo nhóm sự kiện (chung, tác nghiệp, trạng thái đơn hàng); cung cấp API ẩn danh cho consumer hệ thống đọc cấu hình."
alias: ["NotificationConfig", "cấu hình thông báo", "thông báo đơn hàng", "thông báo tác nghiệp", "bật tắt gửi tin sự kiện"]
auth: mixed
state_machine: []
deprecated: false
entities: [NotificationConfig]
stored_procs: []
depends:
  - "INotificationConfigService (SMS.Services.Generated)"
  - "MaTienIchSCRM.TI_SMS — tiện ích gate controller-level"
cross_module_in:
  - "[anonymous] consumer/service hệ thống gọi `TimTheoDonViCustom` / `GetDsNotiConfigTheoDieuKien` để biết đơn vị nào bật thông báo gì"
cross_module_out:
  - "SmsSend *System (gửi tin tự động) chỉ gửi khi NotificationConfig của đơn vị bật sự kiện tương ứng"
---

# NotificationConfigController — Cấu hình thông báo tự động (v1)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình theo đơn vị | (load form) | POST `api/NotificationConfig/TimTheoDonVi` | `NotificationConfig_TimTheoDonVi` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lấy danh sách cấu hình thông báo của đơn vị. | EF | — |
| 2 | Lưu cấu hình (chung) | Lưu | POST `api/NotificationConfig/Save` | `NotificationConfig_Save` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lưu list `NotificationConfig` (nhóm chung). | EF | — |
| 3 | Lưu cấu hình tác nghiệp | Lưu (tab tác nghiệp) | POST `api/NotificationConfig/SaveTacNghiep` | `NotificationConfig_SaveTacNghiep` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lưu cấu hình thông báo nhóm tác nghiệp. | EF | — |
| 4 | Lưu cấu hình trạng thái đơn hàng | Lưu (tab đơn hàng) | POST `api/NotificationConfig/SaveTrangThaiDonHang` | `NotificationConfig_SaveTrangThaiDonHang` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lưu cấu hình gửi tin theo trạng thái đơn hàng. | EF | — |
| 5 | Xóa cấu hình đơn vị | Xóa | GET `api/NotificationConfig/DeleteByIdUnit` | `NotificationConfig_DeleteByIdUnit` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Xóa toàn bộ cấu hình thông báo của đơn vị đăng nhập. | EF | Thao tác xóa qua GET (`cs:74-75`). |

## Endpoint nội bộ (private / service-to-service)
- **[anonymous] `TimTheoDonViCustom`** (GET) — trả cấu hình thông báo (toàn bộ?) cho service khác, không auth.
- **[anonymous] `GetDsNotiConfigTheoDieuKien`** (POST `DsNotiConfigTheoDieuKienReqDto`) — consumer hệ thống hỏi "đơn vị X có bật thông báo sự kiện Y không" trước khi gửi tin tự động.

## Quan hệ
- **Gọi sang:** `INotificationConfigService`.
- **Dữ liệu (entities):** `NotificationConfig` (cấu hình bật/tắt theo nhóm sự kiện/đơn vị).
- **Ripple / đồng bộ:** quyết định luồng `SmsSend.*System` có gửi tin tự động hay không.

## Lỗi đã biết / lưu ý kỹ thuật
- `DeleteByIdUnit` dùng `[HttpGet]` cho thao tác xóa (`NotificationConfigController.cs:74-80`).
- 2 endpoint `[AllowAnonymous]` (`cs:82-95`) là cửa cho consumer đọc cấu hình — không xác thực, cần kiểm soát ở tầng mạng.
- Endpoint `TimTheoDieuKien` v1 đã bị comment (`cs:33-39`); thay bằng `TimTheoDonVi`.
