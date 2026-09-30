---
controller: ConfigPartnerController
module: marketing
service: Webhook_Ladi
man_hinh_ui: "Khu Quản trị nội bộ (area QuanTriArea/Sanbox) — cấu hình đối tác/landing. Không phải màn end-user thường."
muc_dich: "CRUD cấu hình đối tác (config theo type/key) cho Webhook_Ladi: lấy/lưu/sửa/xóa/chi tiết config + refresh cache config theo key."
alias: ["ConfigPartner Ladi", "cấu hình đối tác landing", "GetConfig/SetConfig", "RefreshConfig", "config Webhook Ladi"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigPartner]
stored_procs: []
depends:
  - "IConfigPartnerService (WebhookLadi.Service.Generated) — GetConfig/SetConfig/UpdateConfig/GetDetail/DeleteConfig/RefreshConfig"
cross_module_in:
  - "Admin nội bộ (area QuanTriArea) — cấu hình đối tác landing"
cross_module_out:
  - "Cache config (RefreshConfig) — ảnh hưởng cách DataReceive xử lý theo config"
---

# ConfigPartnerController (Webhook_Ladi) — Cấu hình đối tác/landing

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config | Xem | GET `api/QuanTriArea/ConfigPartner/GetConfig?type&key` | — (không SBAuthorize) | **internal** | màn quản trị | `GetConfig(type, key)` → trả config. | service | Không có `[SBAuthorize]` riêng — xem Bảo mật. |
| 2 | Tạo config | Lưu | POST `api/QuanTriArea/ConfigPartner/SetConfig` | — | **internal** | màn quản trị | `SetConfig(SetConfigModel)` → tạo mới. | service | Như trên. |
| 3 | Sửa config | Cập nhật | POST `api/QuanTriArea/ConfigPartner/UpdateConfig` | — | **internal** | màn quản trị | `UpdateConfig(SetConfigModel)`. | service | Như trên. |
| 4 | Chi tiết config | Xem | GET `api/QuanTriArea/ConfigPartner/GetDetail?id` | — | **internal** | màn quản trị | `GetDetail(id)`. | service | Như trên. |
| 5 | Xóa config | Xóa | POST `api/QuanTriArea/ConfigPartner/DeleteConfig?id` | — | **internal** | màn quản trị | `DeleteConfig(id)`. | service | Như trên. |
| 6 | Refresh cache config | Refresh | GET `api/QuanTriArea/ConfigPartner/RefreshConfig?key` | — | **internal** | màn quản trị | `RefreshConfig(key)` (sync) → reload cache; catch → `Result.Error`. | service (cache) | Như trên. |

## Quan hệ
- **Gọi sang:** `IConfigPartnerService` (toàn bộ CRUD + refresh).
- **Dữ liệu (entities):** cấu hình đối tác (ConfigPartner) theo `type`/`key`.
- **Ripple / đồng bộ:** `RefreshConfig` cập nhật cache → thay đổi cách `DataReceiveController` map/đẩy contact theo config.

## Bảo mật / lưu ý
- Controller nằm trong area `QuanTriArea/Sanbox` nhưng **các action không có `[SBAuthorize]` key riêng** (chỉ kế thừa `BaseController`). Đây là endpoint cấu hình quản trị → cần xác nhận có gác quyền ở mức area/middleware; nếu không thì sửa config đối tác có thể bị truy cập thiếu kiểm soát.

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigPartnerController.cs:24-71` — không action nào có `[SBAuthorize]` permission key (khác với LogContactReceive cùng service có gác AD-HT) → kiểm tra gác quyền ở tầng area.
- `RefreshConfig` (`:59`) là `IActionResult` đồng bộ (không async) duy nhất — khác pattern, catch trả `Result.Error`.
