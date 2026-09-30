# Dev — Quản lý Subiz

> Service: **Webhook_Subiz**. Controller `ConfigChatController` — CRUD cấu hình kết nối chat Subiz. Tiện ích `TI_ExternalChat`. Bản ghi được `EventReceiveService` đọc để xác thực webhook + định tuyến contact. Chỉ dành cho dev.

## Màn hình & route (FE)
- Màn "Cấu hình chat nguồn ngoài (Subiz)" trong khu External Chat: list + thêm/sửa/xóa/chi tiết + danh sách domain.
- Route FE cụ thể: *cần trace FE* (chưa xác minh trong session này).

## Endpoint / Controller (trace registry)
**ConfigChatController** — namespace `WebhookSubiz.API.Areas.SanboxArea` nhưng `[Route("api/[controller]")]` → **path thật `api/ConfigChat/...`** (KHÔNG có `/Sanbox`/Area trong path):
| # | Hành động | Endpoint | Quyền (key) |
|---|---|---|---|
| 1 | Tìm / load lưới | POST `api/ConfigChat/TimTheoDieuKienAsync` | `ConfigChat_TimTheoDieuKienAsync` (DV-AD-TONG, DV-AD-CN) |
| 2 | Thêm mới | POST `api/ConfigChat/AddChatConfig` | `ConfigChat_AddChatConfig` |
| 3 | Cập nhật | POST `api/ConfigChat/UpdateChatConfig` | `ConfigChat_UpdateChatConfig` |
| 4 | Xem chi tiết | GET `api/ConfigChat/LayChiTiet/{id:guid}` | `ConfigChat_LayChiTiet` |
| 5 | Xóa | POST `api/ConfigChat/DeleteConfig/{id:guid}` | `ConfigChat_DeleteConfig` |
| 6 | Lấy DS domain | GET `api/ConfigChat/LayDSDomain/{id:guid}` | `[SBAuthorize()]` rỗng (key `ConfigChat_LayDSDomain` đã comment) |

- `DeleteConfig` (và `DeleteAgent`) dùng `[HttpPost]` chứ không `[HttpDelete]`.

## Entities
- **`ConfigChat` (SubizConfig)** — `AccountId`, `Password` (= **secret HMAC** webhook), `DomainWebsite`, `IdLandingDefault`, `IdUserSaleDefault`, `IdNhomUserSaleDefault`, `DonViId`, `ChiNhanhId`. Lưu qua EF `WebhookSubizDataContext`.

## Luồng code
- **CRUD:** `ConfigChatController` → service CRUD EF `ConfigChat` (lọc theo đơn vị/chi nhánh + CurrentUser). Không bắn event.
- **Tiêu thụ cấu hình (webhook):** `EventReceiveService.ReceiveDataAsync` (`DataReceiveService.cs:340-401`) đọc `ConfigChat` theo `account_id` → tính HMAC từ `Password` (`CreateSignature`) để verify chữ ký Subiz → lấy landing/đơn vị/chi nhánh/user mặc định gán cho contact.

## Bug-prone / lưu ý (path:line — từ registry, CHƯA tự sửa)
- **Phân quyền lệch:** `WebhookSubiz.API/Controllers/ConfigChatController.cs:96-98` — `LayDSDomain` có `[SBAuthorize()]` RỖNG (key `ConfigChat_LayDSDomain` comment L96) → chỉ cần đăng nhập, không kiểm permission cụ thể; 5 endpoint còn lại có key đầy đủ.
- **Secret nhạy cảm:** `Password` của ConfigChat là **secret HMAC webhook** (dùng ở `CreateSignature` verify chữ ký Subiz) — lộ trường này = bypass xác thực webhook ở `EventReceiveController`. **Cần đảm bảo không trả `Password` ra response chi tiết** (`LayChiTiet`).
- **Đừng nhầm route:** namespace `Areas.SanboxArea` nhưng route `api/[controller]` → path thật `api/ConfigChat/...`, không có `/Sanbox` hay Area trong URL.
- **HTTP verb:** `DeleteConfig`/`DeleteAgent` dùng `[HttpPost]` không phải `[HttpDelete]` — chú ý khi gọi/test.

## File source (path:line)
- BE controller: `WebhookSubiz.API/Controllers/ConfigChatController.cs` — `:96-98` (LayDSDomain auth rỗng).
- BE consumer/service đọc cấu hình: `EventReceiveService` / `DataReceiveService.cs:340-401`.
- Registry: `projects/scrm/registry/marketing/ConfigChatController.md`.
