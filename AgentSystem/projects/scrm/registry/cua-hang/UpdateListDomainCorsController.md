---
controller: UpdateListDomainCorsController
module: cua-hang
service: PreOrder
man_hinh_ui: "Quản trị hệ thống (QuanTriArea) — nút 'Update domain CORS' (làm mới danh sách domain được phép CORS của service PreOrder)"
muc_dich: "Reset cache danh sách domain CORS (GlobalConfig.ListDomainCors) của service PreOrder để nạp lại từ DB, không cần restart service."
alias: ["update cors", "làm mới domain cors", "reset list domain cors", "cập nhật cors preorder"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [PreOrder, SB.Core (GlobalConfig/CorsHelper), DB CoreConnectStr]
cross_module_in: [admin hệ thống bấm khi đổi danh sách domain CORS]
cross_module_out: [GlobalConfig.ListDomainCors reset → middleware CORS nạp lại từ DB cho request kế tiếp]
---

# UpdateListDomainCorsController — Làm mới danh sách domain CORS

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Update list domain CORS | "Update CORS" | POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` (role `HT-AD`) | internal | admin HT | Set `GlobalConfig.ListDomainCors = null` (xóa cache), lấy `CoreConnectStr`, gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", ctr)` để trigger nạp lại list từ DB; luôn trả `true` | DB CoreConnectStr (qua CorsHelper) | Origin kiểm tra hard-code `http://localhost:4200` chỉ để warm-up cache; kết quả check bị bỏ (luôn trả true) |

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed(origin, connStr)` — đọc DB qua `CoreConnectStr` để dựng lại `GlobalConfig.ListDomainCors`.
- **Dữ liệu (entities):** không có entity riêng (đọc bảng cấu hình domain qua CorsHelper/connection string).
- **Ripple / đồng bộ:** sau khi gọi, request kế tiếp dùng list domain CORS mới mà không cần restart service.

## Lỗi đã biết / lưu ý kỹ thuật
- **`partial class`** — phần còn lại có thể sinh ở file khác (generated).
- **Origin warm-up hard-code** `http://localhost:4200` — chỉ dùng để ép `CorsHelper` nạp lại list; biến `rs` không được dùng cho response.
- **`[TienIch(..., MaTienIchSCRM.TI_HeThong, true)]`** — nhóm Hệ thống, area QuanTriArea, permission key `DomainCors_UpdateCors` (role `HT-AD`).
- Code thật: BE `PreOrder/PreOrder.API/Areas/SandBoxArea/UpdateListDomainCorsController.cs`.
