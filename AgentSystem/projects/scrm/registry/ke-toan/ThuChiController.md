---
controller: ThuChiController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Quỹ / Thu chi: danh sách phiếu thu - chi (phiếu thu/chi tiền mặt, tiền gửi/chuyển khoản, chuyển tiền nội bộ) + tổng quan tồn quỹ (đầu kỳ/trong kỳ/cuối kỳ). Có các nút Xuất Excel theo từng loại phiếu. Phần 'Số dư ban đầu' (khởi tạo công nợ KH/NCC/tài khoản) cũng nằm trong controller này."
muc_dich: "Đọc/tổng hợp danh sách phiếu thu-chi của đơn vị (tính tồn quỹ, tổng thu/chi/nạp/rút), xem chi tiết phiếu, xuất Excel; quản lý 'số dư ban đầu' (công nợ khởi tạo KH/NCC/tài khoản); và 2 endpoint nội bộ cập nhật/rollback PTTT thu-chi khi Order đổi phương thức thanh toán đơn hàng."
alias: ["thu chi", "phiếu thu", "phiếu chi", "quỹ tiền mặt", "tiền gửi", "chuyển tiền nội bộ", "tồn quỹ", "số dư ban đầu", "khởi tạo công nợ", "thu chi đơn hàng"]
auth: mixed
state_machine: []
deprecated: false
entities: [ThuChi, ThuChiDonHang, SoDuBanDau]
stored_procs: []
depends: [CustomerService/FindCustomersById, CustomerService/KhachHangGhiNo (số dư ban đầu KH), WarehouseProductService/NhaCungCapGhiNo (số dư ban đầu NCC), CommonService/ChiNhanh (enrich tên CN), GemBox.Spreadsheet (export), AutoMapper (clone phiếu khi insert PTTT)]
cross_module_in: [Order/DonHangPtThanhToanService (gọi UpdatePttt_ThuChiByDonHang), Order/DonHangPtttRollback (gọi RollbackPttt_ThuChiByDonHang)]
cross_module_out: [Customer/KhachHangGhiNo (tạo/xóa số dư ban đầu KH), WarehouseProduct/NhaCungCapGhiNo (tạo/xóa số dư ban đầu NCC)]
---

# ThuChiController — Quỹ / Thu chi + Số dư ban đầu

> Controller hỗn hợp `auth: mixed`: phần đọc/xuất là `public` (có/không key tùy method), 2 endpoint đồng bộ PTTT là `internal` (`[PrivateSBAuthorize]`). Phần ghi phiếu thu-chi THỰC SỰ (tạo/sửa phiếu thu, phiếu chi) KHÔNG nằm ở controller này — controller này chỉ ĐỌC danh sách + xuất Excel + quản lý 'số dư ban đầu' + nhận lệnh đồng bộ PTTT từ Order. Đọc đi qua EF (`reposThuChi`, `reposThuChiDonHang`), enrich khách hàng bằng cách gọi Customer `FindCustomersById`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách phiếu thu-chi + tổng quan quỹ | Lưới list + panel tồn quỹ/tổng thu chi | `POST api/ThuChi/DanhSach` | `ThuChi_DanhSach` | public | màn Quỹ/Thu chi | Non-admin (không Ad-Tổng, không Ad-nhiều-CN) bị ép `IdChiNhanh = userLogin.IdChiNhanh`. Lọc `IdDonVi + IsDelete=false`, theo CN/loại thu-chi/lý do/khoảng ngày. `CalculateSumOverView` → tính TonQuyDauKy/CuoiKy/HienTai (qua `TinhTonQuy`) + TongThuChi. `CalculateSum` → tổng Thu/Chi/Nạp/Rút. Order DESC `NgayTaoThuChi`. Keyword lọc thêm theo mã hóa đơn (`ThuChiDonHang.MaHoaDon`). Enrich tên KH qua Customer `FindCustomersById`. | **EF** `ThuChi`, `ThuChiDonHang` | Nếu `FindCustomersById` trả null/empty → trả lỗi "Không tìm thấy thông tin sản phẩm" (message sai ngữ cảnh; chặn cả lưới khi không có KH). L192-195. |
| 2 | Xem chi tiết phiếu | (mở chi tiết phiếu) | `GET api/ThuChi/GetDetailPhieuChi?idThuChi=` | `SBAuthorize()` (any login) | public | click 1 phiếu | Lấy phiếu + chi tiết đối tượng theo id, trả `ReqThuChiDoiTuong`. | **EF** `ThuChi`(+ chi tiết) | `[SBAuthorize()]` trống — chỉ cần đăng nhập. |
| 3 | Xuất Excel theo loại phiếu | nút "Xuất Excel" (mỗi tab) | `POST api/ThuChi/ExportPhieuThuTienMat` · `.../ExportPhieuThuTienGui` · `.../ExportPhieuChiTienMat` · `.../ExportPhieuChiTienGui` · `.../ExportPhieuChuyenTienNoiBo` | `SBAuthorize()` (any login) | public | có dữ liệu | Lọc giống #1, build file GemBox, trả `.xlsx` đúng tên loại phiếu. `result is null` → `NotFound("Không có dữ liệu để xuất")`. | **EF** + GemBox.Spreadsheet | 5 export đều `[SBAuthorize()]` trống — không phân quyền theo key, chỉ cần login. |
| 4 | Số dư ban đầu — thêm | Popup "Khởi tạo số dư ban đầu" / Lưu | `POST api/ThuChi/ThemMoiSoDuBanDau` | `SBAuthorize()` (any login) | public | màn Số dư ban đầu | `SoDuBanDauService.ThemMoiSoDuBanDau`. Validate IdChiNhanh/LoaiDoiTuong/KieuLoai(±1)/SoTien≠0/IdDoiTuong. `SoTien` lưu dấu theo KieuLoai (Nợ=+, Có=−). `NhomDoiTuong=KH` → gọi Customer `KhachHangGhiNo/ThemMoiSoDuBanDauKhachHang`; `=NCC` → gọi Warehouse `NhaCungCapGhiNo/ThemMoiSoDuBanDauNCC` (lỗi gọi → trả lỗi, KHÔNG insert). Khác (tài khoản) → chống trùng (DonVi×CN×Nhóm×Loại×ĐốiTượng) rồi insert nội bộ. | **EF** `SoDuBanDau` + call cross-service | Insert local & call cross-service KHÔNG cùng transaction: với KH/NCC nếu insert local fail sau khi đã ghi nợ bên kia → lệch (không thấy rollback). `SoDuBanDauService.cs:108-177`. |
| 5 | Số dư ban đầu — xóa | nút "Xóa" | `POST api/ThuChi/XoaSoDuBanDau?id=&typeView=` | `SBAuthorize()` (any login) | public | mỗi dòng | Tìm theo id+DonVi. `typeView=KH` → gọi Customer xóa số dư; `=NCC` → gọi Warehouse xóa số dư (lỗi → dừng, không xóa local). OK → `DeleteAsync` (hard delete). | **EF** `SoDuBanDau` + call cross-service | Hard delete. Cross-service trước, local sau — không transaction chung. |
| 6 | Số dư ban đầu — danh sách | Lưới Số dư ban đầu | `POST api/ThuChi/GetListSoDuBanDauPaging` | `SBAuthorize()` (any login) | public | màn Số dư ban đầu | Lọc `IdDonVi` + CN + keyword(SoTK/TenTK/MaĐT/TenĐT) + `TypeView`→`NhomDoiTuong`. Map `DuNo=(SoTien>0?SoTien:0)`, `DuCo=(SoTien<0?SoTien:0)`. Paging, order DESC `NgayCapNhat`. | **EF** `SoDuBanDau` | — |

## Endpoint nội bộ (private / service-to-service)
- **[internal] `POST api/ThuChi/UpdatePttt_ThuChiByDonHang`** (`[PrivateSBAuthorize]`) — **Order** (`DonHangPtThanhToanService.cs:368`) gọi khi user đổi phương thức thanh toán của đơn hàng. So khớp phiếu thu-chi đã có (theo `IdTransaction`) với danh sách PTTT mới (theo `IdLoaiQuy`): LoaiQuy có ở DB nhưng không có ở request → **xóa**; có ở request nhưng chưa có DB → **insert** (clone phiếu mẫu bằng AutoMapper, gen lại `MaThuChi="THU"+yyyyMMddHHmmssfff`, set SoTien/TK ngân hàng + tạo `ThuChiDonHang` tương ứng); cả 2 bên có → **update** SoTien (MarkDirty). → ripple: phiếu thu-chi của đơn hàng được tái khớp theo PTTT. `ThuChiService.cs:982`.
- **[internal] `POST api/ThuChi/RollbackPttt_ThuChiByDonHang`** (`[PrivateSBAuthorize]`) — **Order** (`DonHangPtttRollback.cs:168`) gọi để rollback cập nhật PTTT khi luồng đơn hàng lỗi. `ThuChiService.cs:1305`.

## Quan hệ
- **Gọi sang:** Customer `FindCustomersById` (enrich tên KH ở DanhSach), Customer `KhachHangGhiNo` + Warehouse `NhaCungCapGhiNo` (tạo/xóa số dư ban đầu), Common (tên chi nhánh).
- **Dữ liệu (entities):** `ThuChi` (+ `ThuChiDonHang` chi tiết theo đơn hàng), `SoDuBanDau` (công nợ khởi tạo: NhomDoiTuong KH/NCC/TaiKhoan, LoaiDu, SoTien mang dấu ±).
- **Ripple / đồng bộ:** Bút toán phiếu thu/chi do `ButToanController` sinh riêng (xem ButToanController.md). Số dư ban đầu KH/NCC đẩy sang Customer/Warehouse. PTTT đơn hàng được Order điều khiển 2 chiều (update/rollback).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/ThuChiController.cs`
  - L49-155: `GetDetailPhieuChi` + 5 `Export*` + 3 `*SoDuBanDau*` đều `[SBAuthorize()]` trống → chỉ cần login, không kiểm key. Chỉ `DanhSach` (L35) có key `ThuChi_DanhSach`.
- BE `Accounting/Accounting.Service/Generated/ThuChiService.cs`
  - L192-195: `DanhSach` trả "Không tìm thấy thông tin sản phẩm" khi Customer `FindCustomersById` rỗng → message sai ngữ cảnh + có thể chặn lưới dù vẫn có phiếu (rủi ro với phiếu không gắn KH).
  - L1055: insert phiếu mới luôn gán prefix `"THU"` cho `MaThuChi` kể cả khi đây là dòng phiếu CHI (clone từ template) → mã phiếu chi có thể mang prefix THU (cần xác nhận template loại quỹ).
- BE `Accounting/Accounting.Service/Generated/SoDuBanDauService.cs`
  - L108-177 / L182-220: tạo & xóa số dư KH/NCC gọi cross-service và insert/delete local KHÔNG trong cùng transaction → nguy cơ lệch khi 1 bên fail. Xóa là hard delete.
- DTO: `Libs/Models.Core/DTO/Branch.Client/ThuChi/*` (ReqDanhSachThuChi, ReqThemMoiSoDuBanDau...), `Models.Core.DTO.Accounting.Client.SoDuBanDau`.
- Caller cross-module: `Order/Order.Service/Generated/OtherService/BranchService.cs:265,284` build URL `/ThuChi/UpdatePttt_ThuChiByDonHang` + `/RollbackPttt_ThuChiByDonHang`.
