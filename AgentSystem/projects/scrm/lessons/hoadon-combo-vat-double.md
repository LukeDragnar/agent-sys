# Bug — HĐ combo "X tặng Y" bị GẤP ĐÔI tổng (VAT header phồng)

> Sự cố production (10–24/07/2026). Combo tặng xuất HĐ GTGT: **tiền hàng đúng, VAT = số bù, tổng ≈ 2×**.

## Hiện tượng
Đơn combo (mua X tặng Y) xuất HĐ ra tổng gấp ~2 lần số khách thực trả. Tiền hàng (net) đúng, nhưng **VAT bị phình** để bù cho tổng gấp đôi. VD combo 4 tặng 2: đúng 2.500.000 → HĐ ra 5.000.000 (VAT thành ~2.685.185 thay vì ~185.185 = 8%).

## Cơ chế (2 tầng — OrderLogistic sinh dòng, Order dựng header, Invoice đọc header)
1. **Gốc — OrderLogistic** `GenDonHangComboChiTietToDhSanPham` (`OrderLogistic.Service/Generated/DonHangService.cs`, ~dòng 9089/9093): `task-suaCombo` (Toàn, 10/07) đổi cách tính → **gán `SoTienVat` cho TỪNG dòng SP con của combo** (`SoTienVat = ThanhTien − ThanhTienSauChietKhauTruocVat`, `ThanhTien = itemComboRoot.ThanhTien × soLuongCombo`). → các dòng SP con combo (`DonHangLogisticSanPham`, `IdCombo` != null) mang VAT.
2. **Header — Order** (`Order.Service/Generated/DonHangService.cs` ~9634/9638): `DonHangTongVatSp = Σ SoTienVat` các dòng chi tiết (gồm cả dòng combo con) → `DonHangSoTienVatTongSauPhanBo = DonHangTongVatSp − DonHangVatPhanBo` bị **phồng**.
3. **Invoice** (`DonHangInvoiceService.cs:2010`): `TongTienDonHang = DonHangTienSanPham − DonHangChietKhauTong + DonHangSoTienVatTongSauPhanBo` — đọc THẲNG header (không re-sum) → invoice trung thực nhưng lấy VAT bẩn → tổng double. MISA + Viettel dùng chung → cùng dính.

## Cách BẮT (detector principled — không ngưỡng cố định)
- **Anchor ĐÚNG (không bị hỏng):** `DonHangTienSanPham` (net) và `DonHangTienThuKhach` (tổng khách trả). Kể cả khi VAT header hỏng, 2 field này vẫn đúng → dùng làm mốc.
- **Detector:** `(Net − CK + VatHeader) ≠ ThuKhach` ⇒ hỏng. **Guard bắt buộc** `ThuKhach ≥ Net − CK` (VAT ngụ ý ≥ 0) để loại nhánh khác (đơn giảm-giá-thêm có `VatHeader=0`, `ThuKhach < Net−CK`).
- Sai lầm detector đã thử & bỏ: ngưỡng VAT>12% (sót ca combo nhỏ), so Σ dòng vs header (cả 2 cùng double), tỷ lệ cố định (đơn mix combo+SP tỷ lệ 1..2).

## Đã vá
- `task-hotFixDonOnline` (Toàn, 07-22) + `task-hotFixDonOn` (Toàn, **07-24** — guard zero VAT cho dòng quà tặng/`DonGia=0`, đúng case "X tặng Y"). **Deploy prod 07-24 13:31** — OrderLogistic.API 4.4146.

## Bài học (quan trọng cho lần sau)
1. 🔴 **Vá code KHÔNG hồi tố.** Fix chỉ chữa đơn MỚI / đơn được recompute lại (re-save chạy code mới). **Đơn tồn cũ vẫn hỏng** — nhất là đơn **đã chốt** (`DonHangTrangThaiChotDon=1`, khó auto-recompute) → phải **ép recompute hoặc fix data** trước khi xuất, nếu không xuất là sai. "Đã vá" ≠ "data đã sạch".
2. 🔴 **Header tự lành bẫy detector.** Đơn đã-xuất-sai rồi được recompute → header về đúng → detector soi-header-hiện-tại **bỏ sót**. Muốn tìm ĐỦ đơn *đã xuất sai* phải soi **nguồn HĐ đã phát hành THẬT** (log tổng HĐ / SCRM_Invoice), không dựa header hiện tại.
3. **Anchor để bắt bug tài chính = field khách-trả-thật**, không phải field bị hỏng.
4. **Sửa 1 field header đọc-bởi-invoice không đủ** — còn dòng SP nhân đôi + ES + cache + phiếu Accounting → ưu tiên **recompute qua app** hơn UPDATE tay. Field mang lỗi: `DonHangSoTienVatTongSauPhanBo` (+ `DonHangTongVatSp`, `DonHangLogisticSanPham.SoTienVat`).

> Liên quan: `sua-orderlogistic-quen-order-song-song.md` (2 `DonHangInvoiceService.cs` Order vs OrderLogistic song song — sửa 1 phải rà cái kia).
