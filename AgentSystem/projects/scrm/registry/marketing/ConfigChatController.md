---
controller: ConfigChatController
module: marketing
service: Webhook_Subiz
man_hinh_ui: "nội bộ — cấu hình. Màn 'Cấu hình chat nguồn ngoài' (Subiz) trong khu External Chat (TI_ExternalChat): list + thêm/sửa/xóa/chi tiết + danh sách domain."
muc_dich: "CRUD cấu hình kết nối chat Subiz (SubizConfig/ConfigChat) — lưu account_id, secret (Password) để verify HMAC, domain website, landing/đơn vị/chi nhánh/user sale mặc định để định tuyến contact khi nhận webhook."
alias: ["cấu hình chat subiz", "config chat nguồn ngoài", "kết nối subiz", "subiz account config", "domain chat subiz"]
auth: mixed
state_machine: []
deprecated: false
entities: [ConfigChat (SubizConfig)]
stored_procs: []
depends: [EF WebhookSubizDataContext]
cross_module_in: [SCRM UI External Chat (admin đơn vị/chi nhánh cấu hình)]
cross_module_out: [EventReceiveController/EventReceiveService (đọc ConfigChat để xác thực account_id + HMAC + định tuyến contact)]
---

# ConfigChatController — Cấu hình chat nguồn ngoài (Subiz)

> CRUD cấu hình kết nối **Subiz** (TienIch `TI_ExternalChat`). Đây là nơi nhập `account_id` + `Password` (secret dùng tính HMAC chữ ký webhook) + `DomainWebsite` + landing/đơn vị/chi nhánh/user-sale mặc định. `EventReceiveService` đọc lại bản ghi này để xác thực webhook Subiz và định tuyến contact. **Lưu ý namespace controller là `WebhookSubiz.API.Areas.SanboxArea` nhưng `[Route("api/[controller]")]` → path thật vẫn `api/ConfigChat/...`** (route không theo Area).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + lọc | `POST api/ConfigChat/TimTheoDieuKienAsync` | `ConfigChat_TimTheoDieuKienAsync` (DV-AD-TONG, DV-AD-CN) | internal | màn cấu hình chat | Tìm cấu hình theo `ConfigFilter` + `CurrentUser` (lọc đơn vị/chi nhánh). | **EF** `ConfigChat` | — |
| 2 | Thêm mới | "Thêm" | `POST api/ConfigChat/AddChatConfig` | `ConfigChat_AddChatConfig` | internal | nút thêm | Tạo cấu hình từ `SubizConfigDto` (account_id, password/secret, domain, landing, đơn vị). | **EF** `ConfigChat` | — |
| 3 | Cập nhật | "Lưu" | `POST api/ConfigChat/UpdateChatConfig` | `ConfigChat_UpdateChatConfig` | internal | sửa 1 cấu hình | Cập nhật cấu hình. | **EF** `ConfigChat` | — |
| 4 | Xem chi tiết | (mở 1 cấu hình) | `GET api/ConfigChat/LayChiTiet/{id:guid}` | `ConfigChat_LayChiTiet` | internal | click 1 dòng | Trả chi tiết theo id. | **EF** `ConfigChat` | — |
| 5 | Xóa | "Xóa" | `POST api/ConfigChat/DeleteConfig/{id:guid}` | `ConfigChat_DeleteConfig` | internal | nút xóa | Xóa cấu hình theo id. | **EF** `ConfigChat` | — |
| 6 | Lấy danh sách domain | (dropdown/list domain) | `GET api/ConfigChat/LayDSDomain/{id:guid}` | `[SBAuthorize()]` (any login) | mixed | trong form cấu hình | Trả danh sách domain của cấu hình theo id. Permission key bị comment → chỉ cần ĐĂNG NHẬP. | **EF** `ConfigChat` | Endpoint duy nhất không gắn permission key (xem Lỗi đã biết). |

## Quan hệ
- **Gọi sang:** chỉ EF `WebhookSubizDataContext` (CRUD ConfigChat).
- **Dữ liệu (entities):** `ConfigChat`/`SubizConfig` (AccountId, Password=secret HMAC, DomainWebsite, IdLandingDefault, IdUserSaleDefault, IdNhomUserSaleDefault, DonViId, ChiNhanhId).
- **Ripple / đồng bộ:** không bắn event. Bản ghi được `EventReceiveService.ReceiveDataAsync` đọc để xác thực `account_id`, tính HMAC từ `Password`, và lấy landing/đơn vị/chi nhánh/user mặc định gán cho contact (DataReceiveService.cs:340-401).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookSubiz.API/Controllers/ConfigChatController.cs:96-98` — `LayDSDomain` có `[SBAuthorize()]` RỖNG (key `ConfigChat_LayDSDomain` đang comment L96) → chỉ cần đăng nhập, KHÔNG kiểm permission cụ thể; trong khi 5 endpoint còn lại có key đầy đủ. Lệch chuẩn phân quyền.
- `Password` của ConfigChat là **secret HMAC webhook** (dùng ở `CreateSignature` để verify chữ ký Subiz) — lộ trường này = bypass được xác thực webhook ở `EventReceiveController`. Cần đảm bảo không trả `Password` ra response chi tiết.
- Namespace `Areas.SanboxArea` nhưng route `api/[controller]` → đừng nhầm path có `/Sanbox`/Area; path thật là `api/ConfigChat/...`.
- `DeleteConfig`/`DeleteAgent` dùng `[HttpPost]` chứ không phải `[HttpDelete]`.
