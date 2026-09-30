# Customer — Quản lý Khách Hàng

> Hồ sơ khách hàng, hạng, điểm tích lũy, công nợ, lịch sử chăm sóc.

---

## Vai trò trong hệ thống

Customer là **profile trung tâm của khách hàng** — nhận cập nhật từ nhiều module:
- ← **Order**: cộng điểm, cập nhật hạng, ghi lịch sử mua
- ← **OrderLogistic**: cập nhật `SoTienTichLuyLogistic`, `SoDonHangLogisticThanhToanThanhCong`
- ← **Contact/Facebook/Tiktok**: tạo khách hàng mới từ lead
- ← **Booking**: liên kết lịch hẹn với KH
- → **Accounting**: xử lý công nợ, bù trừ
- → **Notification/SMS**: nhắc chăm sóc, gửi thông báo
- → **Order/OrderLogistic**: cung cấp thông tin hạng, điểm, nhóm KH khi tính giá

---

## Entities chính

### KhachHang
| Field | Ý nghĩa |
|-------|---------|
| `IdHangKhachHang` | Hạng hiện tại (Vàng/Bạc/Đồng...) |
| `SoDiemTichLuy` | Tổng điểm tích lũy lịch sử (chỉ tăng) |
| `SoDiemHienTai` | Điểm hiện có (có thể trừ khi đổi) |
| `SoTienTichLuy` | Chi tiêu từ đơn tại quầy |
| `SoTienTichLuyLogistic` | Chi tiêu từ đơn giao hàng |
| `SoDonHangThanhToanThanhCong` | Số đơn tại quầy hoàn thành |
| `SoDonHangLogisticThanhToanThanhCong` | Số đơn giao hàng hoàn thành |
| `SoTienNoDonHang` | Nợ từ đơn tại quầy |
| `SoTienNoDonHangLogistic` | Nợ từ đơn giao hàng |
| `NgayLenDonGanNhat` | Ngày mua gần nhất |
| `SaleUserId/IdNhomSale` | Nhân viên sale phụ trách |
| `MarketingUserId/IdNhomMarketing` | Nhân viên marketing phụ trách |
| `IdNguoiGioiThieu` | Khách hàng đã giới thiệu |

### DMHangKhachHang (hạng KH)
| Field | Ý nghĩa |
|-------|---------|
| `PriceTichLuy` | Điểm tối thiểu để đạt hạng |
| `PriceDuyTri` | Điểm tối thiểu để duy trì hạng |
| `IsGiuHang` | Giữ hạng dù điểm xuống dưới ngưỡng |
| `PriceDiscount` | Mức chiết khấu khi ở hạng này |
| `Type` | Đơn vị chiết khấu (1=%, 2=VND) |
| `CoCheXepHang` | Cơ chế xếp hạng (mặc định DiemTichLuy) |

### ConfigDoiDiem (cấu hình điểm)
| Field | Ý nghĩa |
|-------|---------|
| `TienThanhDiem` | 1000 VND = N điểm |
| `DiemThanhTien` | 1 điểm = N VND |
| `IsAllowTichDiem` | Bật/tắt tích điểm |
| `IsAllowDoiDiem` | Bật/tắt đổi điểm |
| `IsTichDiem{DichVu/Combo/SanPham/The/ThuKhac}` | Cấu hình từng loại |

### KhachHangGhiNo (công nợ)
| Field | Ý nghĩa |
|-------|---------|
| `KieuLoai` | Loại giao dịch (nợ/trả) |
| `SoTien` | Số tiền giao dịch |
| `MaKhoanNo` | Mã khoản nợ thủ công |
| `IdTransaction` | ID dùng để rollback |
| `TaiKhoanCongNoPhaiTra` | Tài khoản kế toán liên kết |

### KhachHangCareHistory (chăm sóc)
| Field | Ý nghĩa |
|-------|---------|
| `LoaiLichSu` | Loại (Cuộc gọi, SMS, Email, Zalo...) |
| `NgayChamSocTiepTheo` | Ngày nhắc chăm sóc lần sau |
| `IsProcessing` | Đang xử lý |

---

## API Endpoints chính

**Base URL:** `{SERVICE_API_CUSTOMER}api/KhachHang`

| Endpoint | Mô tả |
|----------|-------|
| `TimTheoDieuKien` | Tìm khách theo điều kiện + phân trang |
| `SearchKhachHang360` | Tìm kiếm 360° (đa chiều) |
| `SearchByKeyword` | Tìm theo số điện thoại/tên |
| `ThemMoi_V2` | Tạo khách hàng mới |
| `CreatePrivate` | Tạo KH nội bộ (từ service khác) |
| `CreateAnonymous` | Tạo KH ẩn danh |
| `TimTheoId/{id}` | Chi tiết khách hàng |
| `CapNhatThongTin` | Cập nhật hồ sơ |

**Base URL:** `{SERVICE_API_CUSTOMER}api/KhachHangGhiNo`

| Endpoint | Mô tả |
|----------|-------|
| `GetAllGhiNoByKhachHang` | Lịch sử công nợ |
| `GetTongChiTieuByKh` | Tổng chi tiêu |
| `ThemMoiAsync` | Ghi nợ mới |
| `InsertNoThuCong` | Ghi nợ thủ công |
| `XoaGhiNo/{id}` | Xóa ghi nợ |
| `GetDiemVaSoNoKh` | Lấy điểm + số nợ |

---

## Luồng nghiệp vụ

### 1. Tích điểm và nâng hạng sau đơn hàng
```
Order.ThanhToan() hoàn tất
  ↓
Customer service nhận event
  ├→ Tính điểm: SoTien / TienThanhDiem = điểm cộng thêm
  ├→ SoDiemTichLuy += điểm mới (chỉ tăng)
  ├→ SoDiemHienTai += điểm mới
  ├→ SoTienTichLuy += TamTinh đơn
  ├→ NgayLenDonGanNhat = now
  └→ Kiểm tra nâng hạng:
       So sánh SoDiemTichLuy với PriceTichLuy của từng hạng
       Nếu đủ điều kiện → cập nhật IdHangKhachHang
```

### 2. Cập nhật chi tiêu từ OrderLogistic
```
OrderLogistic: ChiTieuKhachHangService.XuLyAsync()
  ↓
Gọi Customer API (Private)
  ├─ Tăng (Huong=1): SoTienTichLuyLogistic += SoTien
  └─ Giảm (Huong=-1): SoTienTichLuyLogistic -= SoTien (rollback)
```

### 3. Quy trình công nợ
```
Đơn hàng → KH trả một phần → còn lại ghi nợ
  ↓
KhachHangGhiNoService.ThemMoiAsync()
  ├→ Sinh MaKhoanNo
  ├→ Cập nhật SoTienNoDonHang trên KhachHang
  └→ Gửi sang Accounting (BuTruCongNo)

KH trả nợ sau
  ↓
InsertNoThuCong (KieuLoai = trả)
  ├→ SoTienNoDonHang -= SoTienTra
  └→ Sync Accounting
```

### 4. Chăm sóc khách hàng
```
Nhân viên tạo KhachHangCareHistory
  (ghi nhận cuộc gọi, hẹn gặp, v.v.)
  ↓
Đặt NgayChamSocTiepTheo
  ↓
Background job GuiSmsNhacLichChamSoc()
  └→ SMS/Notification tới nhân viên phụ trách
```

---

## Business logic đặc biệt

### Hạng khách hàng
- `SoDiemTichLuy` dùng để **xét nâng hạng** (không bao giờ giảm)
- `SoDiemHienTai` dùng để **đổi điểm lấy ưu đãi** (có thể giảm)
- Khi đổi điểm: trừ `SoDiemHienTai`, không trừ `SoDiemTichLuy`
- `IsGiuHang = true`: dù điểm hiện tại xuống dưới `PriceDuyTri`, vẫn giữ hạng
- Template hạng mặc định ở SuperAdmin (`IdDonVi = Guid.Empty`) → các đơn vị clone khi `KhoiTao_DV()`

### Hai loại tích lũy song song
- Đơn tại quầy → `SoTienTichLuy`
- Đơn giao hàng → `SoTienTichLuyLogistic`
- Hạng KH dựa trên **tổng cả hai** hay chỉ một tùy cấu hình `CoCheXepHang`

### CreatePrivate vs ThemMoi
- `ThemMoi_V2` — nhân viên tạo thủ công, có validation đầy đủ
- `CreatePrivate` — gọi từ service khác (Contact, Webhook...), ít validation hơn
- `CreateAnonymous` — KH vãng lai, không có số điện thoại

---

## Checklist fix bug

- [ ] Điểm không cộng → kiểm tra `ConfigDoiDiem.IsAllowTichDiem` + `IsTichDiem{LoaiItem}`
- [ ] Hạng không nâng → kiểm tra `SoDiemTichLuy` vs `PriceTichLuy` của hạng tiếp theo
- [ ] Chi tiêu logistic sai → xem `DonHangLogisticChiTieuLog` của OrderLogistic
- [ ] Công nợ không khớp → so sánh `SoTienNoDonHang` với tổng `KhachHangGhiNo`
- [ ] Rollback công nợ lỗi → kiểm tra `IdTransaction` trong `KhachHangGhiNo`
- [ ] SMS chăm sóc không gửi → xem `KhachHangCareHistory.NgayChamSocTiepTheo`, kiểm tra job
