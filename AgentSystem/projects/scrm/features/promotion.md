# Promotion — Khuyến Mãi & Voucher

> Quản lý chương trình khuyến mãi, voucher, hoa hồng giới thiệu. Áp dụng vào đơn hàng tại quầy và đơn logistic.

---

## Vai trò trong hệ thống

- ← **Order/OrderLogistic**: gọi `PromotionUpdateWithBill` khi thanh toán để áp KM/Voucher
- ← **MarketingCampaign**: tạo voucher hàng loạt để gửi KH
- → **Customer**: kiểm tra hạng/nhóm KH để lọc KM phù hợp
- → **Branch**: tính hoa hồng cho nhân viên sale (nếu có cấu hình giới thiệu)
- Rollback: khi Order bị hủy → `PromotionRollback` khôi phục lại số lượt dùng

---

## Entities chính

### KhuyenMai (chương trình KM)
| Field | Ý nghĩa |
|-------|---------|
| `DonViTinh` | 1 = %, 2 = tiền cố định |
| `GiaTri` | Giá trị giảm |
| `GiaTriKhuyenMaiToiDa` | Giới hạn tối đa tiền giảm |
| `LoaiKhuyenMai` | 1 = giảm sản phẩm, 2 = giảm cả đơn |
| `TuNgay/DenNgay` | Thời gian hiệu lực |
| `SoLuotApDung/DaApDung` | Giới hạn tổng số lượt |
| `IsApDungVaoLanMua` | Giảm khác nhau theo lần mua thứ N |
| `IsApDungTichLuy` | Cho phép áp dụng nhiều KM cùng lúc |
| `DoUuTienApDung` | Thứ tự ưu tiên (số nhỏ → áp trước) |
| `IsApDungKhungGioVang` | Chỉ áp trong khung giờ nhất định |
| `IsResetSoLuotByDay` | Reset số lượt mỗi ngày |
| `IsApDungTatCaNhom/Hang/DichVu/SanPham` | Áp tất cả hay chọn lọc |

### KhuyenMaiChiTiet (phạm vi & điều kiện)
Mỗi record định nghĩa 1 trong 3 loại:
- `LoaiCauHinh = 1`: **Phạm vi** — SP/DV nào được hưởng KM
- `LoaiCauHinh = 2`: **Điều kiện** — điều kiện cần để KM có hiệu lực
- `LoaiCauHinh = 3`: **Quà tặng** — SP/DV được tặng kèm

`KieuLoai`: 1=SP, 2=DV, 3=NhómKH, 4=HạngKH, 5=Combo, 6=Thẻ, 7=Voucher, 8=NhómDV, 9=NhómSP...

### KhuyenMaiLanMua (KM theo lần mua thứ N)
Khi `IsApDungVaoLanMua = true`:
- Lần mua 1 → giảm X%
- Lần mua 2 → giảm Y%
- Lần mua N → giảm Z%

### KhuyenMaiDieuKienCapBac (KM theo ngưỡng)
Giảm giá theo khoảng giá trị đơn:
| Ngưỡng | Giảm |
|--------|------|
| 500K - 1M | 5% |
| 1M - 2M | 10% |
| > 2M | 15% |

`LoaiGiaTriDieuKien`: 1=tổng tiền, 2=số lượng, 3=số loại SP

### VoucherOption + VoucherMa
- **VoucherOption**: cấu hình chung cho 1 loại voucher (áp điều kiện, giá trị)
- **VoucherMa**: mỗi mã cụ thể (`MaVoucher`) thuộc 1 VoucherOption
- `SoLuongPhatHanh` → tạo hàng loạt mã với `TienToMa` làm prefix
- `IsCheckSoDienThoai = true` → mã chỉ dùng được cho 1 SĐT cụ thể

### HoaHongGioiThieu
Khi KH mua hàng do người giới thiệu → tạo record hoa hồng:
- `CauHinhHoaHongGioiThieu`: % hoặc tiền cố định theo từng SP/DV
- `GiaTriKhachHangNhanDuoc`: phần KH được nhận (nếu có split)

---

## API Endpoints chính

**Base URL:** `{SERVICE_API_PROMOTION}api/`

| Controller | Endpoints quan trọng |
|-----------|---------------------|
| `KhuyenMai` | TimTheoDieuKien, ThemMoi, CapNhat, **GetKhuyenMaiByIdKhachHang_V2** |
| `Voucher` | TimTheoDieuKien, ThemMoi, **GetListWithConditionsCheckCode**, GenVoucherCode, DownloadMaVoucher |
| `PromotionUpdateWithBill` | **ThanhToanDonHang**, **RollBackThanhToanDonHang**, ThanhToanDonHang_Logistic |
| `HoaHongGioiThieu` | TimTheoDieuKien_TheoIdNguonGT |
| `DonHangCauHinhGiamGia` | CRUD cấu hình giảm theo khoảng tiền đơn |

---

## Luồng nghiệp vụ

### 1. Áp KM vào đơn hàng
```
Order.TinhToanLai() → gọi Promotion.GetKhuyenMaiByIdKhachHang_V2()
  ├→ Filter KM theo: thời gian + hạng KH + nhóm KH + loại KH (cũ/mới)
  ├→ Filter theo: phạm vi SP/DV trong đơn
  ├→ Filter theo: khung giờ vàng (nếu có)
  └→ Trả về list KM phù hợp + tính tiền giảm

Order.ThanhToan() → gọi PromotionUpdateWithBill.ThanhToanDonHang()
  ├→ Validate lại điều kiện
  ├→ Tính tiền giảm cuối cùng
  ├→ Cập nhật: KhuyenMai.SoLuotDaApDung++
  ├→ Tạo: KhuyenMaiKhachHang (lịch sử)
  ├→ Tạo: HoaHongGioiThieu (nếu KH được giới thiệu)
  └→ Tạo: VoucherLichSu (nếu dùng voucher)
```

### 2. Kiểm tra và áp voucher
```
FE gọi POST /Voucher/GetListWithConditionsCheckCode
  ├→ Tìm VoucherMa theo MaVoucher
  ├→ Kiểm tra: IsSuDung=true, SoLanSuDung < SoLanDungToiDa
  ├→ Kiểm tra: TuNgay <= Now <= DenNgay
  ├→ Kiểm tra: điều kiện KH (hạng/nhóm/SĐT)
  ├→ Kiểm tra: SP/DV trong đơn có trong danh sách áp dụng không
  ├→ Kiểm tra: khung giờ (nếu cấu hình)
  └→ Trả về giá trị giảm hoặc error message
```

### 3. Rollback khi hủy đơn
```
Order bị hủy → OrderProcess gọi Promotion.RollBackThanhToanDonHang()
  ├→ Tìm KhuyenMaiKhachHang theo IdDonHang
  ├→ KhuyenMai.SoLuotDaApDung--
  ├→ VoucherMa.SoLanSuDung-- (khôi phục)
  └→ Xóa HoaHongGioiThieu records
```

---

## Business logic đặc biệt

**Thứ tự ưu tiên KM (`DoUuTienApDung`):**
- Số nhỏ → áp trước
- Nếu `IsApDungTichLuy = false` → chỉ áp 1 KM ưu tiên cao nhất
- Nếu `IsApDungTichLuy = true` → áp tất cả KM phù hợp (stack)

**Tính giảm giá:**
```
GiaTri = DonViTinh=1 → ThanhTien × GiaTri%
GiaTri = DonViTinh=2 → GiaTri (cố định)
Nếu GioiHanGiaTriKhuyenMaiToiDa → MIN(GiaTri, GiaTriToiDa)
```

**KM theo lần mua:**
- `KhuyenMaiKhachHang.SoLanDaSuDung` đếm KH đã dùng KM này bao nhiêu lần
- Lần tiếp theo tra `KhuyenMaiLanMua[SoLanDaSuDung + 1]` để lấy mức giảm

**Voucher hàng loạt cho Marketing:**
- `GenVoucherCodeByBatch` tạo hàng nghìn mã 1 lúc
- `MarketingCampaign` gọi API này khi chạy campaign SMS/Zalo

---

## Checklist fix bug

- [ ] KM không hiển thị cho KH → kiểm tra hạng KH, nhóm KH, thời gian hiệu lực
- [ ] KM áp sai số tiền → kiểm tra `DonViTinh`, `GiaTriKhuyenMaiToiDa`
- [ ] Voucher không áp được → xem error từ `GetListWithConditionsCheckCode`, kiểm tra `SoLanSuDung`
- [ ] Sau hủy đơn voucher vẫn bị trừ lượt → kiểm tra `PromotionRollback` có được gọi không
- [ ] Hoa hồng giới thiệu không tạo → kiểm tra `CauHinhHoaHongGioiThieu`, `IdNguonGioiThieu` trên đơn
- [ ] KM theo lần mua sai → kiểm tra `KhuyenMaiKhachHang.SoLanDaSuDung` đếm đúng chưa
