---
controller: ConfigSystemController
module: he-thong-rollback-don
service: OrderProcess
man_hinh_ui: "nội bộ — không có UI (CRUD cấu hình hệ thống của service OrderProcess, gọi từ màn Quản trị)"
muc_dich: "CRUD bảng ConfigSystem riêng của service OrderProcess (cấu hình tham số chạy job rollback/hoàn tác đơn): tìm theo mã/id, lấy tất cả, gom theo group + sort, thêm/sửa/xóa."
alias: ["cấu hình hệ thống OrderProcess", "ConfigSystem", "tham số rollback"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: ["IConfigSystemService"]
cross_module_in: ["Màn Quản trị (route api/QuanTriArea/ConfigSystem) đọc/ghi cấu hình"]
cross_module_out: ["OrderRollBackService đọc ConfigSystem khi chạy job rollback (kho/khách/booking/KM/thu-chi)"]
---

# ConfigSystemController (OrderProcess) — Cấu hình hệ thống cho service rollback đơn

CRUD bảng `ConfigSystem` cục bộ của service OrderProcess. Đây là kho tham số mà `OrderRollBackService` đọc khi chạy các job hoàn tác (CallRollBackWareHouse/Customer/Booking/Promotion/ThuChi). Controller-level có `[SBAuthorize]` + `[TienIch("ConfigSystemController_Customer", ...)]` → mọi action bảo vệ bằng key (internal).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo mã | (nội bộ) | GET `api/QuanTriArea/ConfigSystem/TimTheoMa?ma=` | `ConfigSystem_Customer_TimTheoMa` | internal | — | Trả 1 config theo `ma` | EF | — |
| 2 | Lấy tất cả | (nội bộ) | GET `.../LayTatCa` | `ConfigSystem_Customer_LayTatCa` | internal | — | Trả toàn bộ config | EF | — |
| 3 | Lấy theo group + sort | (nội bộ) | POST `.../LayTatCaByGroupSort` | `ConfigSystem_Customer_LayTatCaTheoGroupVaSort` | internal | — | `GetLikeTreeSort(filter)` — gom theo group, sắp xếp | EF | — |
| 4 | Lấy theo group | (nội bộ) | GET `.../LayTatCaByGroup` | `ConfigSystem_Customer_LayTatCaTheoGroup` | internal | — | `GetLikeTree()` — gom theo group | EF | — |
| 5 | Thêm mới | (nội bộ) | POST `.../ThemMoi` | `ConfigSystem_Customer_ThemMoi` | internal | ModelState valid | Map DTO→entity, set NgayTao/NguoiTao = CurrentUser, Id=Guid mới, Insert | EF | — |
| 6 | Xóa theo Id | (nội bộ) | POST `.../Xoa/{id}` | `ConfigSystem_Customer_XoaConfigSystem` | internal | tồn tại Id (ONLY_SUPERUSER theo mô tả key) | Check tồn tại → Delete | EF | Mô tả key ghi "ONLY_SUPERUSER" nhưng code không enforce thêm role |
| 7 | Cập nhật theo Id | (nội bộ) | POST `.../CapNhatThongTin` | `ConfigSystem_Customer_CapNhatThongTin` | internal | tồn tại Id | Map DTO→entity, check tồn tại (404 nếu null), set NguoiCapNhat/NgayCapNhat, Update | EF | — |
| 8 | Tìm theo Id | (nội bộ) | GET `.../TimTheoId/{id}` | `ConfigSystem_Customer_TimTheoId` | internal | — | Trả config theo Id (map sang DTO) | EF | — |
| 9 | Lấy các nhóm cấu hình | (nội bộ) | GET `.../LayCacNhomCauHinh` | `ConfigSystem_Customer_LayCacGroupConfigSystem` | internal | — | `GetGroupCode()` — danh sách group | EF | — |
| 10 | Tìm theo điều kiện | (nội bộ) | POST `.../TimTheoDieuKien` | (SBAuthorize rỗng — chỉ cần đăng nhập) | internal | — | `TimTheoDieuKien(filter)` | EF | `[SBAuthorize()]` không key → chỉ check token, không check quyền cụ thể |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (CRUD EF trên bảng ConfigSystem của OrderProcess).
- **Dữ liệu (entities):** `ConfigSystem`.
- **Ripple / đồng bộ:** Không phát event. Giá trị config tại đây được `OrderRollBackService` đọc để điều khiển các job rollback.

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:125-131` — action `TimTheoDieuKien` dùng `[SBAuthorize()]` rỗng (chỉ check đăng nhập, không có permission key cụ thể) → không xuất hiện trong đồng bộ quyền chi tiết.
- TienIch key ở class là `"ConfigSystemController_Customer"` (sao chép từ service Customer) trong khi đây là OrderProcess → nhãn tiện ích hơi lệch context service.
- Đây là 1 trong **nhiều** bản ConfigSystemController trùng tên trải khắp các service (mỗi service 1 bảng cấu hình riêng) — không dùng chung DB.
