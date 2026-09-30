# Dev — Lịch hẹn khách hàng (Đặt lịch)

> Feature gộp 3 controller (service **Booking**): **DatLichController**, **NguonDatLichController**, **BookingReportController** (vô hiệu hóa). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route / vị trí | Ghi chú |
|---|---|---|
| Đặt lịch hẹn (view lịch) | `dat-lich-ad/dat-lich-view` | view theo thời gian/vị trí/nhân viên/ngày |
| Form tạo/sửa lịch | (modal) + popup Payment | - |
| Màn telesale (contact) | gọi các endpoint `ViewSale_*` | - |
| Trang đặt lịch end-user | (public, `TaoMoi`) | AllowAnonymous |
| Quản lý nguồn đặt lịch | màn Booking "Quản lý nguồn đặt lịch" (cấp đơn vị) | - |

## Endpoint / Controller (trace registry)

### DatLichController (service Booking) — vòng trạng thái: ChuaXacNhan/DaXacNhan/ChoPhucVu/DangPhucVu/HoanThanh/Huy/KhongDen/DaDenNhungChuaMua
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Xem lịch theo thời gian/vị trí/NV/ngày | POST `DsDatLichTheoThoiGian` / `...TheoViTri` / `...TheoNhanVien` / `DsDatLichTheoNgay` | `DatLich_DsDatLichTheoThoiGian` / `...TheoViTri` / `...TheoNhanVien` / `DatLich_DsDatLichTheoNgay` |
| Xem lịch (telesale) | POST `ViewSale_*` | `DatLich_ViewSale_*` |
| Đếm số lịch trong ngày | POST `SoDatLichTheoNgay` | `[SBAuthorize()]` (login) |
| Tìm theo điều kiện (grid) | POST `TimTheoDieuKien` | `DatLich_TimTheoDieuKien` |
| Lấy mã lịch | GET `GetMaDatLich` | `[SBAuthorize()]` |
| Tạo lịch (end-user web) | POST `TaoMoi` | (không key — AllowAnonymous) |
| Tạo lịch (nhân viên) | POST `ThemMoi` | `DatLich_ThemMoi` |
| Tạo lịch hàng loạt | POST `DatLichHangLoat` | `DatLich_DatLichHangLoat` |
| Xem chi tiết | GET `TimTheoId/{id}` | `DatLich_TimTheoId` |
| Cập nhật thông tin | POST `CapNhatThongTin` | `DatLich_CapNhatThongTin` |
| Cập nhật từ popup payment | POST `CapNhatThongTinPayment` | `DatLich_CapNhatThongTinPayment` |
| Cập nhật trạng thái | POST `CapNhatTrangThai` | `DatLich_CapNhatTrangThai` |
| Chuyển trạng thái tạo lịch | POST `ChuyenTrangThaiTaoLich` | `DatLich_ChuyenTrangThaiTaoLich` |
| Cập nhật trạng thái khi hủy đơn | POST `CapNhatTrangThaiKhiHuyDon/{id}` | `DatLich_CapNhatTrangThaiKhiHuyDon` |
| Xóa lịch hẹn | POST `XoaLichHen/{id}` | `DatLich_XoaLichHen` |
| Danh sách trạng thái | POST `ListTrangThaiLichHen` | `DatLich_ListTrangThaiLichHen` |
| Gắn nhãn KH cho lịch | POST `CapNhapLichHenNhanKhachHang` | `[SBAuthorize]` (key bị comment) |
| Validate giờ đặt lịch | POST `ValidateGioDatLich` / `ValidateGioDatLichNhanVien` | `[SBAuthorize()]` |
| Cập nhật thời gian phục vụ | POST `CapNhatThoiGianPhucVu` | `[SBAuthorize()]` |
| Import lịch hẹn | POST `DownloadFileTemplateImportLichHen` / `ImportLichHen` / GET `DownloadDataWrongLichHen/{id}` | `[SBAuthorize]` (key cụ thể bị comment) |
- Internal (`[PrivateSBAuthorize]`): `TimTheoIdAsync_Private/{id}`, `GetLichHenById_Private/{id}`, `KhachHangTimTheoDieuKienPrivate`, `KhachHangGetChiTietLichHenPrivate`, `CapNhatTrangThaiPrivate`, `UpdateKhiThanhToanDonHang`, `GetDsDatLichTheoNhanVien`.

### NguonDatLichController (service Booking) — cấp Đơn vị
| Hành động | Endpoint | Quyền |
|---|---|---|
| Lấy chi tiết theo Id | GET `api/NguonDatLich/TimTheoId/{id}` | `NguonDatLich_TimTheoId` |
| Tìm theo điều kiện | POST `api/NguonDatLich/TimTheoDieuKien_DV` | `NguonDatLich_TimTheoDieuKien_DV` |
| Thêm mới | POST `api/NguonDatLich/ThemMoi_DV` | `NguonDatLich_ThemMoi_DV` |
| Cập nhật | POST `api/NguonDatLich/CapNhat_DV` | `NguonDatLich_CapNhat_DV` |
| Xóa | GET `api/NguonDatLich/Xoa_DV/{id}` | `NguonDatLich_Xoa_DV` |
| Đặt mặc định | GET `api/NguonDatLich/SetMacDinh_DV/{id}` | `NguonDatLich_SetMacDinh_DV` |
| Khởi tạo mặc định | POST `api/NguonDatLich/KhoiTaoMacDinh` | `NguonDatLich_KhoiTaoMacDinh` |
| Lấy tất cả theo đơn vị | GET `api/NguonDatLich/GetAllByDonVi_DV` | `NguonDatLich_GetAllByDonVi_DV` |
- Internal: `GetAllByDonVi_DV_Private/{id}` (GET).

### BookingReportController (service Booking) — **DEPRECATED, toàn bộ endpoint bị comment**
| Hành động (đã tắt) | Endpoint (comment) | Quyền (cũ) | Stored proc |
|---|---|---|---|
| Đếm số đặt lịch / ra đơn (dashboard) | ~~POST `api/BookingReport/DashboardGetCountDatLich`~~ | `DashboardGetCountDatLich` | `sp_DashboardGetCountDatLich` |
| Thống kê đặt lịch (nguồn/trạng thái/khách theo giờ) | ~~POST `api/BookingReport/BookingReportBookings`~~ | `BookingReportBookings` | `PRC_SCRM_BookingReport_Bookings` |
- Cả 2 action + body service comment → controller không expose endpoint nào (route gọi sẽ 404).

## Entities
- `DatLich` (1 lịch/khách; cột `KhachHang` lưu JSON `List<KhachHangDatLich>`), `DatLichChiTiet` (DV/Combo × NhanVien × ChoNgoi), `DatLichChiTietSpDvComboDaMua`.
- `NguonDatLich` (Ten, Ma, Icon, SuDung, MacDinh, IdDonVi, NgayCapNhat). Nguồn hệ thống có `IdDonVi == Guid.Empty`.
- BookingReport: không truy cập EF entity — đọc qua stored proc.

## Luồng code
- **Tạo lịch** (`ThemMoi`/`TaoMoi` → `InsertAsync`): check đơn đã có lịch chưa (Order) → tách `LstKhachDinhKem` → N `DatLich` + `DatLichChiTiet` → tìm/tạo KhachHang (Customer) → ShortLink → SMS/Zalo → publish `GuiThongBaoInfo` → transaction insert → `SaveLogTuongTacKhachHang`.
- **Đổi trạng thái:** `CapNhatTrangThai`/`ChuyenTrangThaiTaoLich` → đồng bộ Contact (nếu IdContact) + publish notification + `UpdateThoiGianDenCuaHang`.
- **Order thanh toán:** Order gọi `UpdateKhiThanhToanDonHang` (private) → set trạng thái + đồng bộ Contact.
- **Job nhắc lịch** `SendSmsTruocNgayDatLich`: quét batch 50 lịch/đơn vị có `Ngay = hôm nay + numberOfDays`, gửi SMS+Zalo, set `NgayGui` chống gửi lại.
- **NguonDatLich mặc định duy nhất:** ThemMoi/CapNhat/SetMacDinh_DV bỏ mặc định bản cũ trước khi set bản mới (SetMacDinh trong transaction).

## Bug-prone / lưu ý
- **`InsertAsync` chặn cứng khi `cauHinh==null`** (`DatLichService.cs:2056-2057`) — comment tự nhận "logic này gây luồng bị ngắt việc tạo lịch chỉ vì cấu hình null => cần xử lý lại".
- **`DatLichHangLoat` không transaction bao batch** (`DatLichService.cs:4824-4837`) — lỗi khung giờ thứ N → lịch trước đã commit, tạo lịch một phần.
- **Filter grid theo NhanVien/ChoNgoi dùng `KhachHang.Contains(id)`** (`DatLichService.cs:1242,1251,5724,5733`) — quét substring trong cột JSON thay vì join `DatLichChiTiet` → có thể khớp nhầm.
- **`GetDatLichById_Private` gọi `.Select` sau `FirstOrDefaultAsync`** (`DatLichService.cs:7202`) — đối tượng đã materialize; cần xác nhận compile được.
- **`KhachHangTimTheoDieuKienPrivate` (Private) vẫn dùng `_currentUser.DonViId`** (`DatLichService.cs:5793,5797`) — endpoint private có thể `_currentUser == null` → rủi ro NRE.
- **Nhiều key quyền bị comment → giảm về chỉ-login:** import (`ImportLichHen`... Controller:521-543), gắn nhãn (`CapNhapLichHenNhanKhachHang`, Controller:583) — cần xác nhận có chủ ý.
- **`TaoMoi` `[AllowAnonymous]`** (Controller:165) — end-user tạo lịch không cần token; service nhánh `_currentUser==null` lấy DonVi từ `model.IdDonVi`.
- **NguonDatLich — `TimTheoId` guard cross-tenant bị bỏ qua** (`Controller:30-34`): khi `Data.IdDonVi != CurrentUser.DonViId` chỉ tạo `ResponseResult(error)` mà **không `return`** → vẫn trả data đơn vị khác → **rò rỉ dữ liệu cross-tenant** (cần `return`).
- **NguonDatLich — `CapNhat_DV` không verify chủ sở hữu** (`Service:229`) — update theo `item` client gửi, chỉ chống trùng tên, không khẳng định `item.Id` thuộc `user.DonViId` (Xóa có check, Cập nhật không) → rủi ro sửa nhầm bản ghi đơn vị khác nếu Id giả mạo.
- **NguonDatLich — Xóa dùng HTTP GET** (`Controller:76`) — hành động phá hủy nên dùng POST/DELETE (rủi ro CSRF/prefetch).
- **NguonDatLich — message "Không tìm thấy tài khoản!"** trong `SetMacDinh_DV` (`Service:193`) copy nhầm ngữ cảnh; **`KhoiTaoMacDinh` không bọc transaction** (`Service:326-345`) → race khi chạy đồng thời.
- **BookingReport — controller chết:** cả 2 action comment (`BookingReportController.cs:25-40`), interface rỗng (`BookingReportService.cs:33-36`, `:52-131`) → FE còn gọi 2 route sẽ nhận 404. Nếu khôi phục: non-admin/non-super/non-test ép `IdChiNhanh = userLogin.IdChiNhanh` (`BookingReportService.cs:65-70`, `:103-108`).

## File source (path:line)
- DatLich: BE controller `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Booking\Booking.API\Controllers\DatLichController.cs` (TaoMoi `:165`, import `:521-543`, nhãn `:583`); service `...\Booking\Booking.Service\Generated\DatLichService.cs` (~7200 dòng; `:1242`,`:1251`,`:2056-2057`,`:4824-4837`,`:5724`,`:5733`,`:5793`,`:5797`,`:7202`).
- NguonDatLich: controller `...\Booking\Booking.API\Controllers\NguonDatLichController.cs` (`:30-34`, `:76`); service `...\Booking\Booking.Service\Generated\NguonDatLichService.cs` (region DON_VI `:186-360`; `:193`,`:229`,`:326-345`).
- BookingReport: controller `...\Booking\Booking.API\Controllers\BookingReportController.cs` (`:25-40`); service `...\Booking\Booking.Service\Generated\BookingReportService.cs` (`:33-36`,`:52-131`,`:65-70`,`:103-108`).
