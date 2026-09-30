# WarehouseProduct — Kho & Sản Phẩm

> Quản lý toàn bộ danh mục sản phẩm, dịch vụ, kho hàng, phiếu nhập/xuất/chuyển kho, combo, thẻ dịch vụ.
> Đây là domain lớn nhất (~62 service, ~56 entity, ~40 controller).

---

## Vai trò trong hệ thống

- → **Order**: cung cấp thông tin SP/DV/Combo/Thẻ khi tạo đơn, xuất kho khi thanh toán
- → **OrderLogistic**: cung cấp SP + kho xuất khi chốt đơn
- → **Accounting**: đồng bộ phiếu nhập/xuất sang kế toán (`DongBoPhieuSangKeToanService`)
- → **Ecommerce (Shopee/Tiktok)**: đồng bộ sản phẩm, tồn kho
- ← **SanXuat**: nhập kho thành phẩm sau sản xuất
- ← **OrderProcess**: rollback xuất kho khi hủy đơn

---

## Entities chính

### SanPham (sản phẩm)
| Field | Ý nghĩa |
|-------|---------|
| `GiaSanPham / GiaSanPhamSauVat` | Giá bán trước/sau VAT |
| `GiaNhapVao` | Giá nhập (giá vốn) |
| `IsSuaGiaKhiThanhToan` | Cho phép sửa giá lúc thanh toán |
| `IsSanPhamImei` | Bắt buộc nhập IMEI khi bán |
| `IsSanPhamLoDate` | Quản lý theo lô/date |
| `IsSanPhamCauThanh` | Sản phẩm cấu thành (gồm nhiều SP con) |
| `IsSanPhamHaCap` | Sản phẩm hạ cấp (quy đổi đơn vị) |
| `BangQuyDoiJson` | Bảng quy đổi đơn vị (JSON) |
| `ThuocTinh1-3` / `ThuocTinhValue1-3` | Thuộc tính (màu, size...) tối đa 3 |
| `Barcode`, `IdSanPhamCha` | Mã vạch, sản phẩm cha |
| `Weight/Length/Width/Height` | Kích thước giao hàng |
| `IsAdjustAmount20`, `TyLeDoanhThu` | Điều chỉnh doanh thu 20% |

### DichVu (dịch vụ)
| Field | Ý nghĩa |
|-------|---------|
| `PhutPhucVu` | Thời gian mặc định (phút) |
| `GiaDichVu / GiaDichVuSauVat` | Giá trước/sau VAT |
| `GiaVonDichVu` | Giá vốn dịch vụ |
| `IsTinhGio` | Tính tiền theo giờ |
| `IsTinhGioTheoBlock` | Tính theo block thời gian |
| `SoPhutTinhBlock`, `GiaBlock` | Độ dài block + giá/block |
| `IsChoPhepSuaGioKhiThanhToan` | Cho sửa giờ lúc thanh toán |
| `DichVuChiNhanh` | Giá riêng theo từng chi nhánh |

### Kho
| Field | Ý nghĩa |
|-------|---------|
| `IsMacDinh` | Kho mặc định của chi nhánh |
| `IsUseShipping` | Kho dùng cho giao hàng |
| `UserQuanKho` | Danh sách nhân viên quản kho (JSON) |
| `IsChoPhepChotAm` | Cho phép tồn kho âm |
| `IsAnKhoDoiVoiSale` | Ẩn kho với nhân viên sale |
| `GiaoHangDoiTacMa` | Mã đối tác vận chuyển liên kết |

### KhoChiTiet (tồn kho)
Mỗi record = 1 sản phẩm trong 1 kho:
- `SoLuong` — tồn hiện tại
- `GiaNhapTrungBinh` — giá vốn bình quân gia quyền
- `SoLuongChoXuat` — số lượng đang chờ xuất (đơn chưa hoàn thành)

### PhieuNhap / PhieuXuat / PhieuChuyenKho
Mỗi phiếu = 1 giao dịch kho, có workflow trạng thái:

| Phiếu | Trigger chính | Ảnh hưởng kho |
|-------|--------------|---------------|
| PhieuNhap | Mua hàng NCC, nhập hoàn từ KH | Tăng tồn |
| PhieuXuat | Bán hàng, xuất nội bộ | Giảm tồn |
| PhieuChuyenKho | Điều chuyển giữa kho/chi nhánh | Tồn kho A giảm, B tăng |

Mỗi phiếu có:
- `DmKhoMaTinhTrang` — trạng thái (nháp/đã xác nhận/hủy)
- `RevertPhieu*` — hủy phiếu (rollback tồn kho)
- Liên kết `IdDonHang/IdDonHangLogistic` — kết nối với đơn hàng

### ComBoSpa (combo dịch vụ)
Gói gồm nhiều DV+SP bán kèm nhau với giá ưu đãi:
- `ComBoSpaDichVu` — danh sách dịch vụ trong combo
- `ComBoSpaSanPham` — danh sách sản phẩm kèm theo
- `ComBoSpaKhachHang` — KH đã mua combo (tracking sử dụng)
- `HanSuDung` — thời hạn sử dụng combo
- `IsGioiHanSoLuong` / `SoLuongToiDa` — giới hạn số lượng bán

### TheDichVu (thẻ dịch vụ)
Thẻ nạp tiền hoặc thẻ số lần:
| Field | Ý nghĩa |
|-------|---------|
| `LoaiThe` | Loại thẻ (tiền/số lần) |
| `SoLanSuDung` | Số lần sử dụng |
| `IsKhongGioiHan` | Không giới hạn số lần |
| `ThoiHanThang/Ngay` | Thời hạn tính từ ngày mua |
| `GiaTri` | Giá trị quy tiền của thẻ |
| `TiLeGiamHoaHong` | Tỷ lệ giảm hoa hồng khi dùng thẻ |
| `TheDichVuKhachHang` | KH đang sở hữu thẻ + lịch sử dùng |

---

## API Endpoints chính

**Base URL:** `{SERVICE_API_WAREHOUSE}api/`

| Controller | Endpoints chính |
|-----------|----------------|
| `SanPham` | TimTheoDieuKien, ThemMoi, CapNhatThongTin, Xoa, SuaGiaHangLoat |
| `DichVu` | TimTheoDieuKien, ThemMoi, CapNhatThongTin, GetDichVuByChiNhanh |
| `Kho` | TimTheoDieuKien, ThemMoi, CapNhatThongTin |
| `KhoChiTiet` | Tồn kho theo SP/kho |
| `PhieuNhap` | TimTheoDieuKien, ThemMoi, CapNhatThongTin, **RevertPhieuNhap** |
| `PhieuXuat` | TimTheoDieuKien, ThemMoi, CapNhatThongTin, **RevertPhieuXuat** |
| `PhieuChuyenKho` | TimTheoDieuKien, ThemMoi, CapNhatThongTin |
| `ComBoSpa` | CRUD + quản lý DV/SP/KH trong combo |
| `TheDichVu` | CRUD + TheDichVuKhachHang |
| `SanPhamImei` | Tra cứu, mapping IMEI vào đơn |
| `PhieuKiemKho` | Kiểm kê kho |

---

## Luồng nghiệp vụ

### 1. Xuất kho khi bán hàng
```
Order.ThanhToan() hoặc OrderLogistic.ChotDon()
  ↓
WarehouseProduct tự động tạo PhieuXuat
  ├→ Giảm KhoChiTiet.SoLuong
  ├→ Cập nhật GiaNhapTrungBinh (bình quân gia quyền)
  ├→ (nếu IMEI) ghi log SanPhamImei → trạng thái "đã bán"
  └→ DongBoPhieuSangKeToanService → Accounting
```

### 2. Nhập hàng từ nhà cung cấp
```
Tạo PhieuNhap + PhieuNhapSanPham (line items)
  ↓
Xác nhận phiếu (DmKhoMaTinhTrang = confirmed)
  ├→ Tăng KhoChiTiet.SoLuong
  ├→ Tính lại GiaNhapTrungBinh
  ├→ (nếu IMEI) tạo record SanPhamImei mới
  ├→ (nếu tự động) SinhImeiTuDong
  └→ Sync sang Accounting
```

### 3. Điều chuyển kho
```
Tạo PhieuChuyenKho (IdTuKho → IdDenKho)
  ↓
Xác nhận + (nếu IMEI) quét IMEI tại kho nhận (IsDaQuetImeiNhanKho)
  ↓
KhoChiTiet kho nguồn: SoLuong giảm
KhoChiTiet kho đích: SoLuong tăng
  ↓
(Nếu khác chi nhánh) xem xét DongBo sang Accounting
```

### 4. Đồng bộ Ecommerce (Shopee/Tiktok)
```
Thay đổi SP/tồn kho trong SCRM
  ↓
ProductConnectService / ShopeeConnectService
  └→ Push API sang sàn TMĐT cập nhật tồn kho + thông tin SP
```

---

## Business logic đặc biệt

**Giá vốn bình quân gia quyền:**
- Mỗi lần nhập hàng với giá khác → `GiaNhapTrungBinh` được tính lại
- Formula: `(SoLuongCu × GiaCu + SoLuongNhap × GiaNhap) / TongSoLuong`

**Sản phẩm IMEI:**
- Khi xuất: phải chỉ định IMEI cụ thể
- Khi nhập: tạo record `SanPhamImei` cho mỗi serial
- `SinhImeiTuDong`: tự generate dãy IMEI theo prefix cấu hình

**Sản phẩm cấu thành:**
- `IsSanPhamCauThanh = true`: khi xuất SP này → xuất đồng thời các SP con
- `BangQuyDoiJson`: định nghĩa tỷ lệ quy đổi

**Giá theo chi nhánh:**
- `DichVuChiNhanh` / `SanPhamChiNhanh`: mỗi chi nhánh có thể có giá riêng
- Nếu không có record chi nhánh → dùng giá gốc của SP/DV

**Thẻ dịch vụ KH (`TheDichVuKhachHang`):**
- Mỗi lần dùng thẻ → giảm `SoLanSuDung` hoặc `GiaTri`
- Lịch sử dùng trong `TheDichVuKhachHangGiaoDichLichSu`
- Hết hạn theo `NgayHetHan` (tính từ ngày mua + `ThoiHanThang/Ngay`)

---

## NhaCungCap — Đồng thời là Khách hàng

### Quan hệ NCC ↔ KH
- KH có `IsDongThoiLaNcc` + `MaNhaCungCap`
- NCC có `IsDongThoiLaKhachHang`
- Khi đồng thời: NCC và KH **dùng cùng một mã** (`Ma` trên KH = `Ma` trên NCC)

### Rules khi tick/bỏ tick

| Hành động | BE xử lý | FE hiển thị |
|-----------|----------|-------------|
| KH tick "đồng thời là NCC" | `KhachHang_Insert/Update` → gọi `NhaCungCap_KhachHangDongThoiLaNhaCungCap` (tạo/restore NCC) | — |
| KH bỏ tick "đồng thời là NCC" | Clear `MaNhaCungCap = null` + `MarkDirty`; NCC được `IsDongThoiLaKhachHang = false` (KHÔNG xóa NCC) | Ô Mã NCC trống |
| NCC tick "đồng thời là KH" | `NhaCungCap_Insert/Update` → gọi `KhachHang_NhaCungCapDongThoiLaKhachHang` (tạo KH) | — |
| NCC bỏ tick "đồng thời là KH" | `IsDongThoiLaKhachHang = false` trên NCC (KHÔNG xóa KH) | — |

### FE display Mã NCC trong danh sách KH
```html
<!-- Nếu MaNhaCungCap chưa được lưu vào DB (data mới) nhưng đã tick đồng thời → dùng mã KH -->
{{ data.maNhaCungCap || (data.isDongThoiLaNcc ? data.ma : '') }}
```
- `maNhaCungCap` có giá trị → hiển thị (data cũ)
- `maNhaCungCap` null + `isDongThoiLaNcc = true` → hiển thị `ma` (data mới, cùng mã)
- `isDongThoiLaNcc = false` → hiển thị trống

### Confirmation popup (ConfirmCode)
- `ConfirmCode = "2"`: NCC đã xóa → hỏi có muốn khôi phục không
- `ConfirmCode = "3"`: NCC đang tồn tại → hỏi có muốn dùng NCC này đồng thời không
- `ConfirmStatus = true` → đồng ý; `ConfirmStatus = false` → từ chối (tạo mới riêng)

---

## Checklist fix bug

- [ ] Tồn kho sai → kiểm tra `KhoChiTiet`, xem phiếu nhập/xuất chưa được xác nhận
- [ ] Giá vốn sai → kiểm tra `GiaNhapTrungBinh`, xem thứ tự xác nhận phiếu nhập
- [ ] IMEI không xuất được → kiểm tra trạng thái `SanPhamImei` (đã bán chưa)
- [ ] Combo không hiển thị đúng giá → kiểm tra `ComBoSpaDichVu/SanPham` và hạn sử dụng
- [ ] Thẻ dịch vụ hết số lần nhưng vẫn dùng được → kiểm tra `IsKhongGioiHan`
- [ ] Tồn kho Shopee/Tiktok không đồng bộ → xem `LogProcessController`, kiểm tra ProductConnect
- [ ] Phiếu không sync sang kế toán → kiểm tra `DongBoPhieuSangKeToanService`, xem log
