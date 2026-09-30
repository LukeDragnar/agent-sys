---
controller: DMNhomKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/dm-nhomkhachhang (Danh mục nhóm khách hàng) · khachhang-nhomkhachhang (gán KH vào nhóm)"
muc_dich: "CRUD danh mục nhóm khách hàng + bật/tắt trạng thái sử dụng và cho phép tự xét nâng nhóm khi thanh toán"
alias: ["Nhóm khách hàng", "DM Nhóm KH", "DMNhomKhachHang"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMNhomKhachHang]
stored_procs: []
depends: [IDMNhomKhachHangService, BaseController]
cross_module_in: []
cross_module_out: []
---

# DMNhomKhachHangController — Quản lý danh mục nhóm khách hàng

> Controller danh mục nhóm KH. Khai báo `[SBAuthorize()]` cấp class (yêu cầu đăng nhập) + mỗi action siết key/role riêng; có 1 action `[PrivateSBAuthorize]` → auth = **mixed**. Khối "SUPER_ADMIN" đã comment toàn bộ.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm nhóm theo điều kiện | Lưới "Nhóm khách hàng" | `POST api/DMNhomKhachHang/TimTheoDieuKien_DV` | DMNhomKhachHang_TimTheoDieuKienSA | public | — | `TimTheoDieuKien_DV(gridInfo)` | EF | Key quyền tên `...TimTheoDieuKienSA` nhưng action là `_DV` (không phải SA) — đặt nhầm tên key |
| 2 | Thêm mới nhóm | Nút "Thêm mới" | `POST api/DMNhomKhachHang/ThemMoi_DV` | DMNhomKhachHang_ThemMoi_DV | public | — | `Create_DV(payload)` | EF | — |
| 3 | Cập nhật nhóm | Nút "Lưu" | `POST api/DMNhomKhachHang/CapNhat_DV` | DMNhomKhachHang_CapNhat_DV | public | — | `Update_DV(model)` | EF | — |
| 4 | Bật/tắt sử dụng | Toggle "Sử dụng" | `GET api/DMNhomKhachHang/ChangeSuDung_DV/{id}` | DMNhomKhachHang_ChangeSuDung_DV | public | — | `ChangeSuDung_DV(id)` đảo trạng thái sử dụng | EF | — |
| 5 | Bật/tắt tự xét nâng nhóm | Toggle "Tự xét khi thanh toán" | `GET api/DMNhomKhachHang/ChangeChoPhepXetNangNhom_DV/{id}` | DMNhomKhachHang_ChangeChoPhepXetNangNhom_DV | public | — | `ChangeChoPhepXetNangNhom_DV(id)` đảo cờ tự động xét nâng nhóm khi thanh toán | EF | — |
| 6 | Khởi tạo nhóm mặc định | Nút "Khởi tạo" | `GET api/DMNhomKhachHang/KhoiTaoMacDinh` | DMNhomKhachHang_KhoiTaoMacDinh | public | — | `KhoiTao_DV()` | EF | — |
| 7 | Lấy chi tiết theo Id | Mở chi tiết | `GET api/DMNhomKhachHang/TimTheoId_DV/{id}` | DMNhomKhachHang_TimTheoId_DV | public | — | `GetById_DV(id)` | EF | — |
| 8 | Dropdown nhóm KH | Combo chọn nhóm | `GET api/DMNhomKhachHang/DropDownDMNhomKhachHang/{isSuDung}` | `[SBAuthorize]` (chỉ đăng nhập) | public | — | `isSuDung==1` → chỉ nhóm đang dùng; gọi `DropDownDMNhomKhachHang(bool)` | EF | Role gốc bị comment, còn `[SBAuthorize]` trống → quyền lỏng |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GET api/DMNhomKhachHang/DropDownDMNhomKhachHangPrivate/{id}` (`[PrivateSBAuthorize]`) — dropdown nhóm KH theo đơn vị cho service khác (`isSuDung=false`).

## Quy tắc nghiệp vụ (gating / tier)
- `ChangeChoPhepXetNangNhom_DV` bật cờ "tự động xét nâng nhóm khi thanh toán" cho 1 nhóm → ảnh hưởng logic nâng hạng/nhóm KH khi có giao dịch (logic xét thực hiện ở luồng thanh toán/điểm, không nằm trong controller này).
- `ChangeSuDung_DV=false` → nhóm không xuất hiện trong dropdown `isSuDung=1`.

## Quan hệ
- **Gọi sang:** `IDMNhomKhachHangService`.
- **Dữ liệu (entities):** `DMNhomKhachHang`.
- **Ripple / đồng bộ:** cờ "cho phép xét nâng nhóm" được luồng thanh toán/điểm thưởng tiêu thụ; dropdown private phục vụ service khác.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMNhomKhachHangController.cs:66` (`TimTheoDieuKien_DV`) — gắn key quyền `DMNhomKhachHang_TimTheoDieuKienSA` (tên gợi ý SUPER_ADMIN) cho action cấp đơn vị → tên key gây nhầm phân quyền.
- `DMNhomKhachHangController.cs:131-132` (`DropDownDMNhomKhachHang`) — role gốc bị comment, còn `[SBAuthorize]` trống → lỏng hơn các action CRUD.
- `DMNhomKhachHangController.cs:90-96` (`Xoa_DV`) — endpoint xóa nhóm đã **comment cứng** → không hỗ trợ xóa nhóm KH.
