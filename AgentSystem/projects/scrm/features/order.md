# Order — Quản lý Đơn Hàng

> Đơn hàng tại quầy (spa/salon/bán lẻ). Trung tâm của toàn hệ thống.

---

## Vai trò trong hệ thống

Order là **hub trung tâm** — hầu hết các module khác đều nhận trigger từ đây:
- → **WarehouseProduct**: xuất kho sản phẩm
- → **Promotion**: áp khuyến mãi, tính voucher
- → **Invoice**: phát hành hóa đơn điện tử
- → **Accounting**: tạo chứng từ kế toán
- → **Customer**: cập nhật điểm, hạng, lịch sử mua, công nợ
- → **Notification**: thông báo cho nhân viên/KH
- ← **Booking**: đơn có thể được tạo từ lịch hẹn (IdLichHen)
- ← **WebhookBank**: xác nhận thanh toán online

---

## Entities chính

### DonHang (đơn hàng)
| Field | Ý nghĩa |
|-------|---------|
| `IdKhachHang` | Khách hàng (nullable — có thể đơn vãng lai) |
| `DmDonHangTrangThai` | Trạng thái đơn (lookup DMDonHang) |
| `TamTinh` | Giá trị thực tế cuối cùng sau tất cả giảm giá |
| `ThanhTien` | Thành tiền cơ bản trước giảm giá |
| `TienNo` | Tiền nợ còn lại |
| `TienCoc` | Tiền đặt cọc |
| `IdLichHen` | Liên kết lịch hẹn (Booking module) |
| `IsPhucVuTaiNha` | Dịch vụ tại nhà (có tính phụ trội) |
| `InvNo/InvCode` | Thông tin hóa đơn điện tử |
| `TrangThaiPhatHanhHoaDon` | Trạng thái phát hành HĐ |
| UTM fields | Tracking nguồn đơn (Source, Campaign...) |

**Hệ thống giảm giá đa tầng (tất cả đều có `Type` + `Value`):**
- `GiamGiaHoaDon` — giảm cả đơn
- `GiamGiaHangKh` — theo hạng KH
- `GiamGiaNhomKh` — theo nhóm KH
- `PhuThu` — phụ thu thêm
- `ThueVat` — thuế VAT

### DonHangSanPham / DonHangDichVu / DonHangCombo
3 loại line item trong đơn:

| | SanPham | DichVu | Combo |
|--|---------|--------|-------|
| Có IMEI | ✓ | — | — |
| Có bảo hành | ✓ | — | — |
| Tính theo giờ/block | — | ✓ | — |
| Có sản phẩm con | ✓ (cấu thành) | — | ✓ |

**DonHangDichVu** có block pricing:
- `IsTinhGio` / `IsTinhGioTheoBlock` — cách tính
- `SoPhutMax`, `GiaBlock` — giới hạn và đơn giá block
- `ThoiGianPhucVu` — thời gian thực phục vụ

### DonHangNhanVien
Mỗi line item có thể assign nhiều nhân viên với vai trò khác nhau:
- `IdNhanVien` — nhân viên phục vụ
- `IdNguoiUpSale` — người upsale
- `IdNguoiTuVan` — người tư vấn
- `TypeHoaHong` + `ValueHoaHong` — hoa hồng (fixed/%)

### DonHangPtThanhToan
Một đơn có thể thanh toán bằng nhiều phương thức cùng lúc:
- Tiền mặt, chuyển khoản, thẻ, điểm, thẻ dịch vụ, trả góp
- `SoTienThanhToanDonHienTai` — phân biệt tiền trả đơn này vs trả nợ cũ

---

## API Endpoints chính

**Base URL:** `{SERVICE_API_ORDER}api/DonHang`

| Endpoint | Mô tả |
|----------|-------|
| `TaoDon` | Tạo đơn mới (trạng thái mở) |
| `CapNhatDon` | Cập nhật thông tin chính đơn |
| `ThemChiTietDon` | Thêm SP/DV/Combo vào đơn |
| `SuaChiTietDon` | Sửa line item |
| `XoaChiTietDon` | Xóa line item |
| `TinhToanLai` | Tính lại giá (gọi trước thanh toán) |
| `ThanhToan` | Đóng đơn + xử lý thanh toán |
| `CloseDon` | Đóng đơn |
| `OpenDon` | Mở lại đơn đã đóng |
| `ViewXepNv` / `LuuXepNv` | Xếp nhân viên phục vụ |
| `ApDungVoucher` / `XoaVoucher` | Quản lý voucher |
| `TimTheoDieuKien` | Tìm kiếm đơn hàng |

---

## Luồng nghiệp vụ

### 1. Tạo và xử lý đơn tại quầy
```
TaoDon (trạng thái mở)
  ↓
ThemChiTietDon (SP/DV/Combo)
  ↓
LuuXepNv (assign nhân viên từng line item)
  ↓
ApDungVoucher (optional)
  ↓
TinhToanLai (tính lại giảm giá, hạng KH, voucher)
  ↓
ThanhToan
  ├→ Promotion: kiểm tra & áp KM
  ├→ WarehouseProduct: xuất kho SP
  ├→ Customer: cộng điểm, cập nhật hạng
  ├→ Accounting: tạo chứng từ
  └→ Invoice: phát hành HĐ (nếu cấu hình)
```

### 2. Xử lý thanh toán online
```
FE gửi yêu cầu thanh toán online
  ↓
DonHangThanhToanOnlineController tạo link/QR
  ↓
WebhookBank nhận callback ngân hàng
  ↓
OrderProcess cập nhật trạng thái
  └→ (nếu lỗi) OrderProcess rollback
```

---

## Business logic đặc biệt

**Tính giảm giá:**
- Mỗi line item tính giảm giá độc lập → phân bổ lại tổng
- `TinhToanLai` phải được gọi trước `ThanhToan` nếu thay đổi KM/hạng KH

**Hoa hồng nhân viên:**
- Tính theo từng line item, không phải tổng đơn
- Có thể có 3 người hưởng hoa hồng cùng 1 dịch vụ (phục vụ, tư vấn, upsale)
- `IsTuDienDoanhSo` — nhân viên tự khai doanh số (không hệ thống tính)

**Data denormalization:**
- Tên SP/DV/Combo, tên nhân viên lưu thẳng vào đơn → query nhanh nhưng không sync nếu đổi tên sau

**Cache:**
- `DonHangCacheService` cache chi tiết đơn đang mở
- `DonHangStoreService` cho query list tối ưu

---

## Checklist fix bug

- [ ] Giảm giá không đúng → kiểm tra `TinhToanLai` có được gọi trước `ThanhToan` không
- [ ] Hoa hồng sai → kiểm tra `DonHangNhanVien`, field `IsTuDienDoanhSo`
- [ ] Xuất kho sai → xem log từ WarehouseProduct (đơn có IMEI hay không)
- [ ] Hóa đơn không phát hành → kiểm tra `TrangThaiPhatHanhHoaDon`, xem `TmpDonHangInvoice`
- [ ] Thanh toán online stuck → xem `DonHangLichSuThanhToanOnline`, kiểm tra OrderProcess rollback
- [ ] Điểm KH không cộng → kiểm tra `ConfigDoiDiem.IsAllowTichDiem`, log từ Customer service
