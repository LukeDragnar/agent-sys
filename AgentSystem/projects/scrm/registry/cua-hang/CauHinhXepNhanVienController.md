---
controller: CauHinhXepNhanVienController
module: cua-hang
service: Order
man_hinh_ui: "Cài đặt hệ thống / Cấu hình Order — popup 'Auto gán user' tab cấu hình nhân viên (config-order/popup-auto-gan-user/tab-config-nv, TabConfigNvComponent): list + lưu cấu hình xếp nhân viên phục vụ đơn."
muc_dich: "Cấu hình quy tắc xếp nhân viên phục vụ đơn hàng (auto gán nhân viên): lấy danh sách cấu hình, xem chi tiết, lưu cấu hình — feed bước 'Xếp nhân viên' ở luồng POS."
alias: ["cấu hình xếp nhân viên", "auto gán nhân viên", "xếp nhân viên phục vụ", "config nhân viên order"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinhXepNhanVien]
stored_procs: []
depends: [Order]
cross_module_in: [Order/config-order (popup-auto-gan-user/tab-config-nv) gọi LayDSCauHinhXepNhanVien/GetCauHinhXepNhanVien/SaveCauHinhNhanVien]
cross_module_out: [Feed bước "Xếp nhân viên" ở luồng POS/đơn hàng (ViewXepNv/LuuXepNv) — gán nhân viên phục vụ đơn]
---

# CauHinhXepNhanVienController — Cấu hình xếp nhân viên phục vụ đơn

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy danh sách cấu hình | load list | POST `api/CauHinhXepNhanVien/LayDSCauHinhXepNhanVien` | (rỗng — login) | public | màn cấu hình | `LayDSCauHinhXepNhanVien(reqBody, CurrentUser)` — list cấu hình xếp nhân viên theo điều kiện | EF | - |
| 2 | Xem chi tiết cấu hình | mở chi tiết | POST `api/CauHinhXepNhanVien/GetCauHinhXepNhanVien` | `CauHinhXepNhanVien_GetCauHinhXepNhanVien` | public | mở 1 cấu hình | `GetCauHinhXepNhanVien(reqBody, CurrentUser)` — chi tiết cấu hình | EF | - |
| 3 | Lưu cấu hình | "Lưu" | POST `api/CauHinhXepNhanVien/SaveCauHinhNhanVien` | `CauHinhXepNhanVien_SaveCauHinhNhanVien` | public | nút Lưu | `SaveCauHinhNhanVien(reqBody, CurrentUser)` — lưu cấu hình xếp nhân viên | EF | - |

## Quan hệ
- **Gọi sang:** `ICauHinhXepNhanVienService` (LayDS / Get / Save).
- **Dữ liệu (entities):** `CauHinhXepNhanVien`.
- **Ripple / đồng bộ:** cấu hình này quyết định cách gán nhân viên phục vụ ở bước "Xếp nhân viên" trong luồng tạo/phục vụ đơn POS.

## Lỗi đã biết / lưu ý kỹ thuật
- `LayDSCauHinhXepNhanVien` chỉ `[SBAuthorize]` rỗng (login), trong khi Get/Save có permission key — đọc list không gắn quyền riêng.
- Code thật: BE `Order/Order.API/Controllers/CauHinhXepNhanVienController.cs`; FE service `services/order/cauhinh-xepnhanvien.ts`, page `pages/caidat-hethong/configsystem/config-order/popup-auto-gan-user/tab-config-nv/`.
