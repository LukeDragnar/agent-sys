---
controller: DMNhomKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/dm-nhomkhachhang-admin (Danh mục nhóm khách hàng — biến thể admin/SandBox) — route api/SanboxArea/DMNhomKhachHangController; phân biệt với DMNhomKhachHangController non-SA"
muc_dich: "CRUD danh mục Nhóm khách hàng ở cấp ADMIN/SandBox (endpoint suffix _SA): tìm theo điều kiện, thêm/sửa, lấy theo Id, đổi trạng thái sử dụng, đổi cờ cho phép xét nâng nhóm khi thanh toán"
alias: ["DMNhomKhachHang_SA", "Nhóm khách hàng (admin)", "DM Nhóm KH SandBox", "DMNhomKhachHangController (SandBoxArea)"]
auth: public
state_machine: []
deprecated: false
entities: [DMNhomKhachHang]
stored_procs: []
depends: [IDMNhomKhachHangService, BaseController]
cross_module_in: []
cross_module_out: []
---

# DMNhomKhachHangController (SandBoxArea) — Danh mục nhóm khách hàng (biến thể admin _SA)

> **Biến thể SA/admin**, route `api/SanboxArea/DMNhomKhachHangController`. File nằm trong thư mục `Areas/SandBoxArea` nhưng namespace khai báo là `Customer.API.Areas.Controllers` (lưu ý khi tra cứu).
> Cùng class name `DMNhomKhachHangController` với controller non-SA; node này tài liệu hóa bản SandBox (endpoint `*_SA`). Dùng chung service `IDMNhomKhachHangService` với bản non-SA.
> Khác bản DMHang*: controller-level `[SBAuthorize()]` không kèm role và từng action **không** truyền role string → chỉ yêu cầu đăng nhập (mức public), không siết HT-AD trong attribute.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm nhóm KH theo điều kiện | Lưới (dm-nhomkhachhang-admin) | `POST api/SanboxArea/DMNhomKhachHangController/TimTheoDieuKien_SA` | DMNhomKhachHang_TimTheoDieuKienSA | public | — | `TimTheoDieuKien_SA(gridInfo)` trả danh sách phân trang | EF (IDMNhomKhachHangService) | — |
| 2 | Thêm mới nhóm KH | Nút "Thêm mới" | `POST .../ThemMoi_SA` | DMNhomKhachHang_ThemMoi_SA | public | — | `Create_SA(payload)` | EF | — |
| 3 | Đổi trạng thái sử dụng | Toggle "Sử dụng" | `GET .../ChangeSuDung_SA/{id:Guid}` | DMNhomKhachHang_ChangeSuDung_SA | public | — | `ChangeSuDung_SA(id)` đảo cờ sử dụng | EF | — |
| 4 | Đổi cờ cho phép xét nâng nhóm | Toggle "Tự động xét khi thanh toán" | `GET .../ChangeChoPhepXetNangNhom_SA/{id:Guid}` | DMNhomKhachHang_ChangeChoPhepXetNangNhom_SA | public | — | `ChangeChoPhepXetNangNhom_SA(id)` đảo cờ tự động xét nâng nhóm khi thanh toán | EF | — |
| 5 | Lấy nhóm KH theo Id | (mở chi tiết) | `GET .../TimTheoId_SA/{id:Guid}` | DMNhomKhachHang_TimTheoId_SA | public | — | `GetById_SA(id)` trả chi tiết | EF | — |
| 6 | Cập nhật nhóm KH | Nút "Lưu" | `POST .../CapNhat_SA` | DMNhomKhachHang_CapNhat_SA | public | — | `Update_SA(model)` | EF | — |

## Quan hệ
- **Gọi sang:** `IDMNhomKhachHangService` (TimTheoDieuKien_SA, Create_SA, ChangeSuDung_SA, ChangeChoPhepXetNangNhom_SA, GetById_SA, Update_SA).
- **Dữ liệu (entities):** `DMNhomKhachHang` (qua DTO `DMNhomKhachHangFilterDto` / `DMNhomKhachHangCUDto`).
- **Ripple / đồng bộ:** cờ "cho phép xét nâng nhóm" ảnh hưởng logic tự động xét nâng nhóm KH khi thanh toán (cross sang luồng bán hàng/thanh toán).

## Quy tắc nghiệp vụ (gating)
1. `ChangeChoPhepXetNangNhom_SA` bật/tắt cờ → khi cờ TRUE, hệ thống **tự động xét nâng nhóm KH** tại thời điểm thanh toán; FALSE → không tự xét.
2. `ChangeSuDung_SA` bật/tắt cờ sử dụng → nhóm bị tắt không hiển thị/áp dụng cho KH mới.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMNhomKhachHangController.cs:8` — namespace `Customer.API.Areas.Controllers` (không phải `...SandBoxArea`) dù file đặt trong thư mục SandBoxArea → dễ nhầm khi grep theo namespace.
- `DMNhomKhachHangController.cs:72-78` — endpoint `Xoa_SA` đã bị **comment** → không hỗ trợ xóa nhóm qua API này.
- Các action `*_SA` ở đây **không** truyền role string (khác DMHang/KhachHangDanhGia/TrangThai dùng HT-AD) → chỉ kiểm soát bằng permission key, mức public.
