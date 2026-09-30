# Dev — Lịch hẹn gặp khách

> Service: **Booking** (BE .NET) · FE màn Đặt lịch hẹn. Chỉ dành cho dev.
> Trace registry: `registry/cua-hang/DatLichController.md` (vòng đời lịch); duyệt lịch từ contact ở `registry/sales/ContactController.md` (endpoint `GetListContactDatLichChoDuyet`, `DuyetContactDeTaoLich`, `HuyDuyetContactTaoLich`). Xem thêm `registry/cua-hang/DatLichChiTietController.md`, `DatLichNangCaoController.md`.

## Màn hình & route (FE)
| Màn hình | Route (tham chiếu) | Ghi chú |
|---|---|---|
| Đặt lịch hẹn (admin/view) | `dat-lich-ad` / `dat-lich-view` | lịch theo thời gian/vị trí/nhân viên/ngày + modal tạo/sửa + popup Payment |
| Màn telesale (contact) | — | gọi các endpoint `ViewSale_*` |
| Trang đặt lịch end-user | public | `TaoMoi` (AllowAnonymous) |
| Chờ duyệt lịch (contact) | `sale/danhsach-contact-datlich` | leader duyệt lịch (ContactController) |

## Endpoint / Controller (trace registry)
**DatLichController** (Service `Booking`). Nhóm hành động (bảng đầy đủ + line trong registry node):
| Nhóm | Endpoint | Quyền (key) |
|---|---|---|
| Xem lịch | `DsDatLichTheoThoiGian/ViTri/NhanVien/Ngay` | `DatLich_DsDatLichTheo*` |
| Xem lịch (telesale) | `ViewSale_DsDatLichTheo*` | `DatLich_ViewSale_*` (gọi cùng service, khác key) |
| Đếm / tìm grid | `SoDatLichTheoNgay` (`[SBAuthorize()]`), `TimTheoDieuKien` | `DatLich_TimTheoDieuKien` |
| Tạo lịch | `ThemMoi`, `TaoMoi` (anonymous), `DatLichHangLoat` | `DatLich_ThemMoi`, (web: không key), `DatLich_DatLichHangLoat` |
| Chi tiết / sửa | `TimTheoId/{id}`, `CapNhatThongTin`, `CapNhatThongTinPayment` | `DatLich_*` |
| Trạng thái | `CapNhatTrangThai`, `ChuyenTrangThaiTaoLich`, `CapNhatTrangThaiKhiHuyDon/{id}`, `ListTrangThaiLichHen` | `DatLich_*` |
| Xóa | `XoaLichHen/{id}` | `DatLich_XoaLichHen` |
| Nhãn KH | `CapNhapLichHenNhanKhachHang` | `[SBAuthorize]` (key bị comment `:583`) |
| Validate giờ / NV rảnh | `ValidateGioDatLich(+NhanVien)`, `GetListNhanVienOrderByTimeRanh` | `[SBAuthorize()]` |
| Import | `ImportLichHen`, `DownloadFileTemplateImportLichHen`, `DownloadDataWrongLichHen/{id}` | `[SBAuthorize]` (key bị comment `:521-543`) |

Duyệt lịch contact (**ContactController**): `GetListContactDatLichChoDuyet` (`GetListContactDatLichChoDuyet_TimTheoDieuKienAsync`), `DuyetContactDeTaoLich` (`DuyetContactDeTaoLich_TimTheoDieuKienAsync`), `HuyDuyetContactTaoLich` (`ContactController_HuyDuyetContactTaoLich`) — state lịch `DatLich_ChoDuyet`/`DaDuyet`.

Endpoint private (`[PrivateSBAuthorize]`): `TimTheoIdAsync_Private`, `GetLichHenById_Private`, `KhachHangTimTheoDieuKienPrivate`, `KhachHangGetChiTietLichHenPrivate`, `CapNhatTrangThaiPrivate`, `UpdateKhiThanhToanDonHang` (Order gọi khi thanh toán). Xem registry node.

## Entities
- **`DatLich`** — 1 lịch/khách; cột `KhachHang` lưu JSON `List<KhachHangDatLich>`.
- **`DatLichChiTiet`** — DV/Combo/ComboKhachHang × NhanVien × ChoNgoi.
- **`DatLichChiTietSpDvComboDaMua`** — SP/DV trong combo đã mua.
- Khác: `KhachHangDatLich`, `NguonDatLich`, `ChoNgoi`.
- State machine: `ChuaXacNhan, DaXacNhan, ChoPhucVu, DangPhucVu, HoanThanh, Huy, KhongDen, DaDenNhungChuaMua`.

## Luồng code
- **InsertAsync** (chung cho ThemMoi/TaoMoi/DatLichHangLoat): tách `LstKhachDinhKem` → N `DatLich`, tìm/tạo KH (Customer), tạo ShortLink, gửi SMS/Zalo + notification, publish `GuiThongBaoInfo`, transaction insert, `SaveLogTuongTacKhachHang`.
- **Gate QLDV POS:** `IsAllowAppointmentViewForCashier(_currentUser, configSV)` ở hầu hết thao tác.
- **Đồng bộ Contact:** chỉ khi lịch có `IdContact` → `CapNhapTrangThaiLichChoContact` / `CapNhapContactNhanKhachHang`.
- **Job nhắc lịch:** `SendSmsTruocNgayDatLich` quét batch 50 lịch/đơn vị có `Ngay = hôm nay + numberOfDays`, gửi SMS+Zalo, set `NgayGui`.

## Bug-prone / lưu ý (path:line)
> Chỉ ghi nhận — KHÔNG sửa code ở session này. Để session security xử lý.
- **`DatLichHangLoat` không transaction bao batch** (Service:4824-4837) — lỗi khung thứ N → các lịch trước đã commit, tạo lịch một phần, không rollback.
- **`InsertAsync` chặn cứng khi `cauHinh==null`** (Service:2056-2057) — comment trong code tự nhận luồng bị ngắt chỉ vì cấu hình null, cần xử lý lại.
- **Filter grid theo NhanVien/ChoNgoi dùng `KhachHang.Contains(id)`** (Service:1242,1251 và 5724,5733) — quét substring cột JSON `KhachHang` thay vì join `DatLichChiTiet` → có thể khớp nhầm.
- **`GetDatLichById_Private` gọi `.Select` sau `FirstOrDefaultAsync`** (Service:7202) — đối tượng đã materialize; cần soi lại.
- **`KhachHangTimTheoDieuKienPrivate` (PrivateSBAuthorize) vẫn dùng `_currentUser.DonViId`** (Service:5793,5797) — endpoint private có thể `_currentUser == null` → rủi ro NullRef.
- **Nhiều key quyền bị comment** → giảm về chỉ-đăng-nhập: import (`:521-543`), gắn nhãn `CapNhapLichHenNhanKhachHang` (`:583`). Cần xác nhận có chủ ý.
- **`TaoMoi` là `[AllowAnonymous]`** (Controller:165) — end-user web tạo lịch không cần token; cần validate input.
- **`ContactDatLich_CapNhatThongTinPrivate` map đè ~20 field** (Controller:268-309) — field không ghi đè giữ giá trị DB.

## File source
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Booking\Booking.API\Controllers\DatLichController.cs`.
- BE service: `Booking\Booking.Service\Generated\DatLichService.cs` (~7200 dòng); namespace `Booking.Services.Generated`.
- Duyệt lịch contact: `Contact\...\Controllers\ContactController.cs` (`GetListContactDatLichChoDuyet`, `DuyetContactDeTaoLich`, `HuyDuyetContactTaoLich`).
- Registry nodes: `projects/scrm/registry/cua-hang/DatLichController.md`, `projects/scrm/registry/sales/ContactController.md`.
