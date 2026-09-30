---
controller: QuanLyCauHinhHoaDonController
module: ke-toan
service: Invoice
man_hinh_ui: "Quản lý cấu hình hóa đơn (tài khoản kết nối HĐĐT) — màn CRUD tài khoản kết hóa đơn không phân tab NCC; phần lớn permission key bị comment"
muc_dich: "CRUD tài khoản kết nối hóa đơn (Account) phiên bản gộp — xem chi tiết, thêm/sửa, xóa, tìm theo điều kiện; không tách MISA/Viettel ở controller"
alias: ["quản lý cấu hình hóa đơn", "tài khoản kết hóa đơn", "QuanLyCauHinhHoaDon"]
auth: public   # tất cả [SBAuthorize] trống (key thật đã comment)
state_machine: []
deprecated: false
entities: [Account]
stored_procs: []
depends:
  - IQuanLyCauHinhHoaDonService
cross_module_in: []
cross_module_out: []
---

# QuanLyCauHinhHoaDonController — CRUD tài khoản kết nối hóa đơn (gộp NCC)

> `partial class`, controller-level `[TienIch("AccountController", "Quản lý cấu hình hóa đơn viettel", TI_Invocie_Viettel)]` — **trùng tên tiện ích "AccountController" với ViettelInvoiceController**.
> CRUD `Account` không phân tab NCC; **mọi permission key đã bị comment**, chỉ còn `[SBAuthorize]` trống → chỉ chặn anonymous. Có thể là controller cũ/gộp song song với Me/Viettel controller.
> Như Viettel controller: `InsertOrUpdate` fallback `TypeInvoice = MeInovice` khi null.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết tài khoản | (mở form sửa) | `GET TimTheoId/{id:Guid?}` | không key (key gốc comment) | public | — | `_service.GetByIdAsync(id)` | EF `Account` | key `QuanLyCauHinhHoaDon_TimTheoId` comment (controller:35) |
| 2 | Thêm/Sửa tài khoản | Nút "Lưu" | `POST InsertOrUpdate` | không key | public | — | Validate; gán IdDonVi/IdChiNhanh từ user; TypeInvoice null → MeInovice; `_service.InsertOrUpdate(model, user)` | EF | key comment (controller:43); fallback NCC MISA giống Viettel |
| 3 | Xóa tài khoản | Nút "Xóa" | `POST Xoa/{id:Guid}` | không key | public | — | GetById null → MESS_NotFound; có → `DeleteAsync(id)` → MESS_Delete_Success | EF | key comment (controller:70) |
| 4 | Tìm tài khoản theo điều kiện | Lưới tài khoản | `POST TimTheoDieuKien` | không key | public | — | `_service.TimTheoDieuKienAsync(gridInfo)` | EF | key comment (controller:92) |

## Quan hệ
- **Gọi sang:** `IQuanLyCauHinhHoaDonService` → `IRepository<Account>`. Không gọi NCC/partner trực tiếp (chỉ CRUD bản ghi tài khoản).
- **Dữ liệu (entities):** `Account` (dùng chung MISA/Viettel, `TypeInvoice`).
- **Ripple / đồng bộ:** không có.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `QuanLyCauHinhHoaDonController.cs:35,43,70,92` — toàn bộ permission key bị comment, chỉ còn `[SBAuthorize]` trống → không phân quyền chi tiết.
- `:55-58` (`InsertOrUpdate`) — TypeInvoice null fallback `MeInovice` (giống ViettelInvoiceController) → có thể gán sai NCC.
- **Trùng tiện ích/chức năng:** `[TienIch("AccountController", ..., TI_Invocie_Viettel)]` trùng tên với ViettelInvoiceController; CRUD Account chồng chéo với Me/Viettel controller → cần xác minh FE thực gọi controller nào (rủi ro duplicate / dead controller).
