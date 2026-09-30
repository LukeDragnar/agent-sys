---
controller: MobileNotifyConfigController
module: he-thong
service: Core
man_hinh_ui: "nội bộ / app mobile — 2 endpoint đọc cấu hình thông báo mobile (LayTatCa cần login, LayTatCa_Encrypt public mã hóa); việc CRUD cấu hình nằm ở biến thể QuanTriArea (MobileNotifyConfigController_Core)"
muc_dich: "Cung cấp cấu hình thông báo mobile (MobileNotifyConfig) cho app đọc: trả toàn bộ cấu hình (bản thường yêu cầu login, bản _Encrypt mã hóa AES public). Chỉ READ — không CRUD."
alias: ["cấu hình thông báo mobile", "mobile notify config", "notify config", "MobileNotifyConfig"]
auth: mixed
state_machine: []
deprecated: false
entities: [MobileNotifyConfig]
stored_procs: []
depends: ["IMobileNotifyConfigService", "AES_ECB_128 (mã hóa)", "Core.DataContext (SQL)"]
cross_module_in: ["App mobile gọi LayTatCa / LayTatCa_Encrypt để lấy cấu hình thông báo"]
cross_module_out: []
---

# MobileNotifyConfigController — Đọc cấu hình thông báo mobile (bản app)

Bản trong thư mục `Controllers` (route `api/MobileNotifyConfig`) — **chỉ 2 endpoint READ**, không CRUD (CRUD ở biến thể QuanTriArea `MobileNotifyConfigController_Core`). `LayTatCa` có `[SBAuthorize]` (cần login); `LayTatCa_Encrypt` **không có auth** → public, trả chuỗi mã hóa AES-ECB-128 (camelCase JSON).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả cấu hình | (app mobile) | GET `api/MobileNotifyConfig/LayTatCa` | — (chỉ `[SBAuthorize]` method-level, không key) | public (login) | — | `GetAllAsync()` — toàn bộ cấu hình thông báo mobile. | EF `MobileNotifyConfig` | Có `[SBAuthorize]` không kèm key (chỉ yêu cầu login) |
| 2 | Lấy tất cả (mã hóa) | (app mobile) | GET `api/MobileNotifyConfig/LayTatCa_Encrypt` | — | anonymous (không attribute) | — | `GetAllAsync()` → serialize camelCase → `AES_ECB_128.Encrypt_Data`. | EF | **Không gate** — public; chỉ bảo vệ bằng mã hóa AES |

## Quan hệ
- **Gọi sang:** `IMobileNotifyConfigService.GetAllAsync`, `AES_ECB_128` (encrypt).
- **Dữ liệu (entities):** `MobileNotifyConfig`.
- **Ripple / đồng bộ:** app mobile đọc cấu hình để quyết định gửi/hiển thị thông báo; nguồn cấu hình do biến thể QuanTriArea ghi.

## Lỗi đã biết / lưu ý kỹ thuật
- `MobileNotifyConfigController.cs:30` key `MobileNotifyConfig_LayTatCa` đã bị comment; method còn `[SBAuthorize]` trống (chỉ login, không key quyền cụ thể).
- `MobileNotifyConfigController.cs:39-49` `LayTatCa_Encrypt` không có attribute auth → **public**; chỉ dựa vào mã hóa AES-ECB-128 để "bảo vệ" → key AES nếu lộ thì dữ liệu cấu hình lộ.
- `AES_ECB_128` dùng ECB mode (yếu mật mã) — lưu ý nếu siết bảo mật.
