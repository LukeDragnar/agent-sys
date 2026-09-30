# Luồng — Quản lý voucher

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Đơn vị đã **mua tiện ích Voucher (TI_Voucher)** để áp voucher khi thanh toán.
- Có dữ liệu nền: hạng/nhóm khách, sản phẩm/dịch vụ/thẻ (đối tượng áp dụng).
- Voucher còn hạn, **chưa bị khóa**, còn lượt/mã chưa dùng.

## Luồng cấu hình (admin — tạo & quản lý voucher)
1. Mở **Marketing → Voucher → Thêm mới** → `ThemMoi`/`Create(payload)`: sinh `Voucher` + `VoucherOption` + (mã nếu loại MotMa/NhieuMa).
2. Cấu hình đối tượng áp dụng qua `VoucherDoiTuongApDung` (whitelist) và `VoucherDoiTuongKhongApDung` (blacklist) theo loại dịch vụ/sản phẩm/thẻ; cờ `IsSuDungChiNhanh`/`IsSuDungHangKhachHang`/`IsSuDungNhomKhachHang`/`IsSuDungLoaiLichHen`.
3. Quản lý mã: `TimKiemMaVoucher` (tab mã), `DownloadMaVoucher/{id}` (tải Excel mã).
4. Khóa/Mở khóa (`Khoa`/`MoKhoa`), Gia hạn (`GetDataGiaHanVoucher` → `GiaHanVoucher`), Xử lý thủ công 1 mã (`XuLyThuCongMaVoucher`).
5. Xem **lịch sử sử dụng** (`LichSuSuDungVoucher` → VoucherLichSu).

## Luồng dùng (áp voucher khi thanh toán — xuyên module)
1. Thanh toán đơn → OrderRollBackService → `PromotionUpdateWithBill/ThanhToanDonHang`.
2. Voucher chỉ áp khi vượt qua **chuỗi gate đối tượng** (mỗi cờ bật → khớp đối tượng HOẶC có bản ghi `IdDichVu = Guid.Empty` = "tất cả"):
   - Khách vãng lai → chỉ voucher `IsKhongGioiHan` mới áp.
   - `IsSuDungChiNhanh` → cần ChiNhanh khớp `userLogin.IdChiNhanh` hoặc Guid.Empty.
   - `IsSuDungHangKhachHang` → cần hạng khớp `rankKhachHang` hoặc Guid.Empty.
   - `IsSuDungNhomKhachHang` → cần nhóm khớp hoặc Guid.Empty.
   - `IsSuDungLoaiLichHen` → cần loại lịch hẹn khớp `IdNguonDatLich` hoặc Guid.Empty.
   - Theo khách cụ thể → cần khớp `payload.IdKhachHang` hoặc Guid.Empty.
3. Trong transaction: **NhieuMa** set `IsDaDung=true`; **MotMa** `SoLanSuDung += 1`; option `SoLuongDaDung += n`. Voucher Deposit → publish message Deposit sau commit.

## Luồng sinh mã hàng loạt (job chiến dịch — nội bộ)
- Khi tạo chiến dịch khách hàng → job gọi `GenVoucherCodeForJobMarketingCampaign*` / `GenVoucherCodeByBatch` (private) sinh mã hàng loạt.
- Hủy chiến dịch → `DeleteVoucherCodeByIdNguonTao/{idNguonTao}/{idDonVi}` dọn mã đã sinh.

## Case biên
- **Non-admin xem danh sách / lịch sử** → bị ép `IdChiNhanh = CurrentUser.IdChiNhanh`.
- **Voucher bị khóa (state Khoa)** → không áp được khi thanh toán.
- **Khách vãng lai** → mọi gate hạng/nhóm/khách-cụ-thể fail → chỉ voucher `IsKhongGioiHan` áp.
- **Mã NhieuMa đã dùng (`IsDaDung=true`)** → không dùng lại; **mã MotMa** đã đạt giới hạn lần → loại.
- **Hủy/rollback đơn** → NhieuMa `IsDaDung=false`; MotMa `SoLanSuDung-1`; option `SoLuongDaDung-=n` (rollback-safe trong transaction). Rollback lỗi → mã kẹt "đã dùng" → cần `XuLyThuCongMaVoucher`.
- **`actionId` của xử lý thủ công là magic number** (int raw) — truyền sai số → thao tác sai trạng thái.
- **`GetAllKhuyenMaiGuiSmsZaloDonVi`** qua `_commonService` (inject optional null) → NRE nếu DI thiếu.

## Ví dụ cụ thể có số
Shop D tạo đợt voucher "GIAM10" loại **một mã** giảm 10%, 1–30/6, áp cho khách hạng VIP, không giới hạn số lần.
- Khách VIP nhập GIAM10 khi thanh toán → đủ điều kiện hạng → áp giảm 10%, `SoLanSuDung` 0 → **1**.
- Khách vãng lai nhập GIAM10 → vì có gate hạng VIP và khách không có hồ sơ → **loại**, không áp.
- Shop tạo đợt khác loại **nhiều mã**, sinh 500 mã qua job, mỗi mã 1 lần. Tải Excel 500 mã gửi SMS cho khách. Khách A dùng mã `ABC123` → `IsDaDung=true`; A nhập lại `ABC123` → **đã dùng**, loại.
- Khách A hủy đơn → `ABC123` `IsDaDung=false` → dùng lại được.

## Hậu điều kiện
- Tạo/Sửa/Xóa/Khóa/Gia hạn voucher → ghi DB.
- Áp voucher khi thanh toán → đánh dấu mã đã dùng + tăng `SoLuongDaDung`; ghi VoucherLichSu.
- Hủy đơn → hoàn mã (rollback-safe).
- Job chiến dịch → sinh/dọn mã hàng loạt theo nguồn tạo.
