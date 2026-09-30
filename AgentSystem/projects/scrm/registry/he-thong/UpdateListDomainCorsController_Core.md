---
controller: UpdateListDomainCorsController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — admin tool (Quản lý domain cors): nút làm mới danh sách domain CORS được phép"
muc_dich: "Reset/làm mới cache danh sách domain CORS được phép (đọc lại từ DB) — admin gọi sau khi thêm/sửa domain."
alias: ["UpdateListDomainCors", "domain cors", "cập nhật cors", "làm mới cors"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IConfiguration", "BaseController", "CorsHelper", "GlobalConfig"]
cross_module_in: ["FE Quản lý domain cors gọi UpdateCors để làm mới danh sách"]
cross_module_out: ["Reset GlobalConfig.ListDomainCors → ảnh hưởng kiểm tra CORS toàn service Core; trùng tên file với UpdateListDomainCorsController ở MỌI service khác — node Core riêng"]
---

# UpdateListDomainCorsController — Làm mới CORS (Core)

Controller `partial` (route `api/QuanTriArea/UpdateListDomainCors`, Area SanboxArea) service **Core**. Mỗi service trong hệ thống có 1 controller cùng tên (xem các node `UpdateListDomainCorsController*.md` ở cua-hang/ke-toan/khach-hang/marketing/nhan-su/sales, và he-thong `_File`/`_OriginalProject`). Node này là bản service **Core** → suffix `_Core`.

Chỉ 1 hành động: reset cache `GlobalConfig.ListDomainCors = null` rồi gọi `CorsHelper.IsOriginAllowed` (đọc lại từ connection string `CoreConnectStr`) để nạp lại danh sách.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Update CORS | Làm mới | POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` (HT-AD) | internal | — | `GlobalConfig.ListDomainCors = null` → `CorsHelper.IsOriginAllowed("http://localhost:4200", CoreConnectStr)` để nạp lại danh sách domain từ DB. Luôn trả `true`. | EF (CoreConnectStr) | Hardcode origin "http://localhost:4200" để trigger reload; luôn trả true bất kể kết quả |

## Quan hệ
- **Gọi sang:** IConfiguration (CoreConnectStr), CorsHelper, GlobalConfig.
- **Dữ liệu (entities):** không trực tiếp — đọc bảng domain CORS qua CorsHelper.
- **Ripple / đồng bộ:** reset `GlobalConfig.ListDomainCors` (static) → kiểm tra CORS toàn service Core dùng danh sách mới.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs:31` hardcode origin `"http://localhost:4200"` chỉ để trigger reload cache (giá trị kết quả không dùng).
- Luôn `ResultWithData(true)` — không phản ánh lỗi đọc DB nếu có.
- Trùng tên ở mọi service → node này suffix `_Core`.
