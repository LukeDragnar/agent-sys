# Booking — Lịch Hẹn

> Quản lý lịch hẹn (đặt lịch) của khách hàng tại các chi nhánh spa/phòng khám. Hỗ trợ nhiều nguồn đặt lịch, phân công nhân viên, quản lý ghế/phòng, và tích hợp với combo/thẻ dịch vụ.

---

## Vai trò

- ← **Website / AuthenCustomer / Telesale / trực tiếp**: tạo lịch hẹn
- → **Customer**: lấy thông tin, phân loại KH
- → **WarehouseProduct**: lấy dịch vụ, sản phẩm, combo
- → **Promotion**: tính khuyến mãi áp dụng
- → **Order**: cập nhật khi KH thanh toán đơn hàng
- → **SMS**: nhắc lịch trước ngày hẹn

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `DatLich` | Lịch hẹn: `Ma`, `Ngay`, `GioBatDau`, `GioKetThuc`, `ThoiGian`, `TrangThai`, `TrangThaiTruoc` |
| `DatLich` (KH) | `KhachHangId`, `IdContact`, `TenKhachHang`, `SdtKhachHang`, `IsKhachCu`, `IsChamSoc` |
| `DatLich` (dịch vụ) | `ListIdDichVu`, `ListIdComboSpa`, `ListIdSanPham`, `ListIdNhomSanPham`, `IdTheKhachHang` |
| `DatLich` (nhân viên) | `NhanVienId`, `NhanVienTuVanId`, `NhanVienBanHangId`, `SaleId` |
| `DatLich` (UTM) | `UTMSource`, `UTMCampaign`, `UTMMedium`, `UTMTerm`, `UTMContent`, `UTMChannel` |
| `DatLichChiTiet` | Chi tiết lịch: `DichVuId`, `ComboId`, `IdSanPham`, `NhanVienId`, `ChoNgoiId` |
| `KhungGio` | Khung giờ làm việc: `GioBatDau`, `GioKetThuc`, `IsCaDem` |
| `ChoNgoi` | Ghế/phòng dịch vụ |
| `NguonDatLich` | Nguồn đặt lịch: Website, Telesale, Trực tiếp... `MacDinh` |
| `TrangThai` | Danh sách trạng thái lịch hẹn |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /DsDatLichTheoThoiGian` | Danh sách lịch theo khung giờ |
| `POST /DsDatLichTheoViTri` | Danh sách lịch theo ghế/phòng |
| `POST /DsDatLichTheoNhanVien` | Danh sách lịch theo nhân viên |
| `POST /TaoMoi` [AllowAnonymous] | Tạo lịch từ website/app |
| `POST /ThemMoi` [SBAuthorize] | Tạo lịch từ nội bộ |
| `POST /DatLichHangLoat` | Tạo lịch hàng loạt |
| `POST /CapNhatTrangThai` | Cập nhật trạng thái |
| `POST /ValidateGioDatLich` | Kiểm tra trùng giờ |
| `POST /GetListNhanVienOrderByTimeRanh` | Nhân viên rảnh theo giờ |
| `POST /ImportLichHen` | Import lịch từ file Excel |
| `POST /CapNhatThongTinPayment` | Cập nhật khi KH thanh toán |
| `GET /TimTheoIdAsync_Private/{id}` | Lấy lịch hẹn (private API) |
| `POST /KhachHangTimTheoDieuKienPrivate` | KH tìm lịch hẹn của mình |

---

## Luồng

```
[KH đặt lịch online]
KH vào website/app → chọn dịch vụ + ngày giờ
  ↓
POST /TaoMoi (AllowAnonymous)
  ├→ ValidateDatLich() — kiểm tra trùng giờ, nhân viên
  ├→ Tạo DatLich + DatLichChiTiet
  └→ Ghi UTM tracking

[Nhắc lịch trước ngày hẹn]
JobSendSmsNhacLich (scheduler)
  └→ Lọc lịch chuẩn bị đến ngày (IsGuiTruocNgayHen)
       → SMS service gửi nhắc nhở

[Cập nhật khi thanh toán]
Order thanh toán → POST /CapNhatThongTinPayment
  └→ Cập nhật DatLich.TrangThai = Đã thanh toán

[Phân công nhân viên]
GetListNhanVienOrderByTimeRanh(gioBatDau, gioKetThuc)
  └→ Trả về nhân viên rảnh, sắp xếp theo thời gian available
```

---

## Đặc điểm quan trọng

- **Thẻ khách hàng**: `IdTheKhachHang` — buổi dịch vụ trừ vào thẻ của KH
- **TrangThaiTruoc**: lưu trạng thái cũ để trace lịch sử thay đổi
- **DatLichHangLoat**: tạo nhiều lịch lặp lại (`LapLai`, `TanSuat`, `SoBuoi`)
- **ViewSale**: nhân viên Sales/Telesale chỉ thấy lịch hẹn của khách hàng mình quản lý
- **ShortLink**: tạo link ngắn để chia sẻ lịch hẹn

---

## Checklist fix bug

- [ ] Lịch trùng giờ → gọi `ValidateGioDatLich`, kiểm tra `KhungGio`
- [ ] Nhân viên không hiện → kiểm tra `NgayLamViec`, `KhungGioLamViec`
- [ ] SMS nhắc không gửi → kiểm tra `DatLich.IsGuiTruocNgayHen`, `JobSendSmsNhacLich`
- [ ] Thẻ không trừ → kiểm tra `IdTheKhachHang`, liên kết WarehouseProduct
- [ ] Import lịch lỗi → tải file template `/DownloadFileTemplateImportLichHen`, xem `/DownloadDataWrongLichHen`
