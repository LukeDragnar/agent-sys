---
controller: ServerController
module: marketing
service: Facebook
man_hinh_ui: "Quản trị > Quản lý server webhook (Area SanboxArea, route api/QuanTriArea/Server). CRUD danh mục Server (cấu hình server webhook FB)."
muc_dich: "CRUD danh mục Server (cấu hình server webhook Facebook) ở Area QuanTri: xem chi tiết, thêm, sửa, xóa, tìm theo điều kiện. Bản Area có endpoint thật (khác bản Controllers comment toàn bộ — node _Facebook)."
alias: ["server webhook FB", "Server QuanTri", "quản lý server webhook"]
auth: public
state_machine: []
deprecated: false
entities: [Server]
stored_procs: []
depends:
  - "IServerService — GetByIdAsync/InsertAsync/UpdateAsync/DeleteAsync/TimTheoDieuKienAsync"
  - "AutoMapperConfiguration — map ServerDto ↔ Server"
cross_module_in:
  - "Web quản trị server webhook — CRUD Server"
cross_module_out: []
---

# ServerController (QuanTriArea) — Quản lý server webhook FB

> Area `SanboxArea`, route `api/QuanTriArea/Server`. Controller-level `[SBAuthorize]` + `[TienIch("ServerController","Quản lý server webhook", TI_HeThong, true)]`. Permission KEY ở từng action đều bị comment → chỉ check đăng nhập controller-level.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết | (mở) | GET `api/QuanTriArea/Server/TimTheoId/{id:Guid?}` | (chỉ check đăng nhập — key comment) | public | — | `GetByIdAsync(id)`. | EF Server | — |
| 2 | Thêm mới | Thêm | POST `api/QuanTriArea/Server/InsertAsync` | (chỉ check đăng nhập) | public | ModelState hợp lệ | Invalid → `MESS_Model_UnValidate`; hợp lệ → map DTO→Server, `InsertAsync`. | EF Server | — |
| 3 | Cập nhật | Lưu | POST `api/QuanTriArea/Server/UpdateOneAsync` | (chỉ check đăng nhập) | public | Tồn tại theo Id | Map DTO; `GetByIdAsync(Id)` null → `MESS_NotFound`; có → `UpdateAsync`. | EF Server | — |
| 4 | Xóa | Xóa | POST `api/QuanTriArea/Server/DeleteOneItemAsync/{id:Guid}` | (chỉ check đăng nhập) | public | Tồn tại theo Id | `GetByIdAsync(id)` null → `MESS_NotFound`; có → `DeleteAsync` + `MESS_Delete_Success`. | EF Server | — |
| 5 | Tìm theo điều kiện | Tìm kiếm | POST `api/QuanTriArea/Server/TimTheoDieuKienAsync` | (chỉ check đăng nhập) | public | — | `TimTheoDieuKienAsync(gridInfo)` → list phân trang. | EF Server | — |

## Quan hệ
- **Gọi sang:** `IServerService` (CRUD bảng `Server`).
- **Dữ liệu (entities):** `Server`.
- **Ripple / đồng bộ:** cấu hình server webhook ảnh hưởng định tuyến/đăng ký webhook FB.

## Lỗi đã biết / lưu ý kỹ thuật
- Mọi action `[SBAuthorize(...)]` key bị comment (`ServerController.cs:33`...) → chỉ bảo vệ bằng `[SBAuthorize]` controller-level (đăng nhập), không có permission key riêng để audit từng thao tác.
- Trùng tên với bản Controllers comment toàn bộ (node `ServerController_Facebook.md`) → node này suffix `_Facebook_Area`.
