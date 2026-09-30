---
controller: KhachHangNhomKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/khachhang-nhomkhachhang (Gán khách hàng vào nhóm — danh sách + khachhang-nhomkhachhang-edit) · dm-nhomkhachhang › ds-khachhang (xem KH trong 1 nhóm)"
muc_dich: "Quản lý quan hệ N-N giữa Khách hàng và Nhóm khách hàng: gán KH vào nhóm (single/multi), cập nhật, xóa, tra cứu KH theo nhóm và nhóm theo KH, lấy cấu hình nhóm."
alias: ["Nhóm khách hàng của KH", "Gán nhóm KH", "KH theo nhóm", "Customer group mapping", "KhachHangNhomKhachHang"]
auth: public
state_machine: []
deprecated: false
entities: [KhachHangNhomKhachHang]
stored_procs: []
depends:
  - IKhachHangNhomKhachHangService
  - BaseController                 # CurrentUser, ResponseResult
cross_module_in: []
cross_module_out: []
---

# KhachHangNhomKhachHangController — Gán khách hàng vào nhóm khách hàng

> Controller quản lý **bảng nối** KhachHang ⇄ NhomKhachHang (quan hệ nhiều-nhiều). CRUD + tra cứu 2 chiều. Toàn bộ endpoint `[SBAuthorize]` có key riêng (public, login + role). Không có endpoint private.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách KH theo id nhóm | Lưới "KH trong nhóm" (ds-khachhang) | `POST GetListKhachHangByIdNhom` | KhachHangNhomKhachHang_GetListKhachHangByIdNhom (AD-CN/AD-MCN/AD-TONG/NV/QLDV) | public | Mở 1 nhóm | ModelState invalid → lỗi; `GetListKhachHangByIdNhom(payload)`. | EF | — |
| 2 | Tìm KH-nhóm theo điều kiện | Lưới list/filter chính | `POST TimTheoDieuKien` | KhachHangNhomKhachHang_TimTheoDieuKien (AD-CN/AD-MCN/AD-TONG/QLDV) | public | — | ModelState invalid → lỗi; `TimTheoDieuKien(payload)`. | EF | — |
| 3 | Danh sách nhóm theo id KH | Tab "Nhóm" trong hồ sơ KH | `GET GetListNhomByIdKhachHang/{id:Guid}` | KhachHangNhomKhachHang_GetListNhomByIdKhachHang (AD-CN/AD-MCN/AD-TONG/NV/QLDV) | public | — | `GetListNhomByIdKhachHang(id)`. | EF | — |
| 4 | Gán nhiều KH vào 1 nhóm | Nút "Thêm nhiều KH" / bulk | `POST CreateMulti` | KhachHangNhomKhachHang_CreateMulti (AD-CN/AD-MCN/AD-TONG) | public | — | ModelState invalid → lỗi; `CreateMulti(payload)` — gán list KH theo IdNhom. | EF | — |
| 5 | Gán 1 nhóm cho 1 KH | Nút "Thêm nhóm" (single) | `POST CreateSingle` | KhachHangNhomKhachHang_CreateSingle (AD-CN/AD-MCN/AD-TONG) | public | — | ModelState invalid → lỗi; `CreateSingle(payload)`. | EF | — |
| 6 | Cập nhật nhóm theo KH | Nút "Lưu" (sửa) | `POST Update` | KhachHangNhomKhachHang_Update (AD-CN/AD-MCN/AD-TONG) | public | — | ModelState invalid → lỗi; `Update(payload)` — cập nhật tập nhóm của 1 KH. | EF | — |
| 7 | Xóa quan hệ KH-nhóm | Nút "Xóa" | `GET Delete/{id:Guid}` | KhachHangNhomKhachHang_Delete (AD-CN/AD-MCN/AD-TONG) | public | — | `Delete(id)` xóa 1 bản ghi nối. | EF | Dùng `GET` cho thao tác xóa (không phải POST/DELETE) — không idempotent-safe theo convention HTTP |
| 8 | Lấy cấu hình nhóm theo id KH | (cấu hình hiển thị/auto-gán nhóm) | `GET GetConfigNhomKhachHang/{id:Guid}` | KhachHangNhomKhachHang_GetConfigNhomKhachHang (AD-TONG/AD-CN/AD-MCN/QLDV/SALE/NV) | public | — | `GetConfigNhomKhachHang(id, CurrentUser)`. | EF | — |

## Quan hệ
- **Gọi sang:** `IKhachHangNhomKhachHangService` (toàn bộ CRUD + tra cứu 2 chiều).
- **Dữ liệu (entities):** `KhachHangNhomKhachHang` (bảng nối KhachHang ↔ NhomKhachHang). Danh mục nhóm gốc do `NhomKhachHangController` (ngoài file này) quản lý.
- **Ripple / đồng bộ:** không phát event/cross-service trong controller. Nhóm KH có thể được dùng làm điều kiện ở quy đổi điểm (`ConfigDoiDiemChiTiet.IdNhomKhachHang`) và phân khúc MKT.

## Lỗi đã biết / lưu ý kỹ thuật
- `KhachHangNhomKhachHangController.cs:94` (`Delete`) — thao tác xóa khai báo `[HttpGet("Delete/{id:Guid}")]` → dùng GET để xóa, lệch convention REST (nên POST/DELETE).
- `GetConfigNhomKhachHang` (#8) khai báo `virtual` — có thể bị override ở controller con (cần xác nhận có lớp kế thừa hay không).
