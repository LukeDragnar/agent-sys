# Luồng — Cấu hình giá & chiết khấu

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị + chi nhánh; cấu hình lọc/lưu theo **đơn vị + chi nhánh** của user.
- Đã khai báo sẵn: sản phẩm/dịch vụ/combo (để gán làm mặc định), khu vực & chỗ ngồi (Booking), và — nếu tính giờ — dịch vụ đã được đánh dấu "dịch vụ tính giờ" + có bảng giá theo khung giờ.
- Có quyền tương ứng (thêm/sửa/xóa cấu hình).

## Luồng cấu hình (admin — Cấu hình mặt hàng mặc định)
1. Mở **Cấu hình mặt hàng mặc định** → danh sách (`TimTheoDieuKien`) lọc theo đơn vị + chi nhánh, enrich tên khu vực/chỗ ngồi (gọi Booking), tên SP (Elastic), tên DV/combo (cache).
2. **Thêm mới** (`ThemMoi`): nhập Tên, chọn SP/DV/combo mặc định (lưu trong `CauHinhJson`), chọn khu vực/chỗ ngồi, (tùy chọn) dịch vụ tính giờ + dịch vụ chuyển bàn → validate → insert; nếu bật `IsMacDinh` → bỏ mặc định các bộ khác cùng chi nhánh → clear cache (transaction).
3. **Sửa** (`CapNhat`): như thêm nhưng update theo Id; serialize lại `CauHinhJson`.
4. **Xóa** (`Xoa`): xóa cứng theo đơn vị + chi nhánh + Id → clear cache.
5. **Bật/tắt sử dụng** (`ThayDoiTrangThaiSuDung`): đảo bit `SuDung` (toggle, không loại trừ nhau).
6. **Bật/tắt mặc định** (`ThayDoiTrangThaiMacDinh`): đảo bit `IsMacDinh`; bật → bỏ mặc định mọi bộ khác cùng chi nhánh.

## Luồng dùng (POS thu ngân tiêu thụ cấu hình)
1. Thu ngân chọn bàn/khu vực ở màn bán hàng.
2. POS gọi `LayCauHinhSuDung` / `LayCauHinhHienTai` (đọc cache theo đơn vị + chi nhánh; cache miss → nạp lại).
3. Tự bơm SP/DV/combo mặc định vào đơn; dịch vụ tính giờ → tính tiền theo khung giờ (`LayTatCaDichVuTinhGioTheoKhuVuc`).

## Quy tắc validate (ThemMoi/CapNhat — dừng ở lỗi đầu tiên)
1. `Ten` rỗng → "Tên cấu hình không được để trống!".
2. Trùng tên (cùng đơn vị + chi nhánh, khác Id) → "Tên cấu hình đã tồn tại!".
3. UPDATE mà thiếu Id → "Không tìm thấy Id bản ghi cấu hình!".
4. Danh sách DV mặc định: đếm DV thuộc đơn vị phải khớp số chọn, lệch → "Không tìm thấy dịch vụ!".
5. Danh sách SP mặc định: tương tự → "Không tìm thấy sản phẩm!".
6. Dịch vụ tính giờ (nếu có): DV phải tồn tại; phải `IsTinhGio=true`; không trùng (cùng khu vực + cùng DV tính giờ) → báo trùng. Khu vực null = áp cho mọi khu vực.
7. Dịch vụ chuyển bàn (nếu có): phải `IsTinhGio=true && IsDichVuBan=true`.
8. Bàn (`IdChoNgoi`): không trùng với bộ cấu hình khác → trả tên bộ đang trùng.
- **Invariant mặc định-duy-nhất:** mỗi đơn vị + chi nhánh tối đa 1 bộ `IsMacDinh=true` tại một thời điểm.

## Các trường hợp / case biên
- **Tên rỗng / trùng tên** → chặn (xem trên).
- **Bàn trùng giữa 2 bộ** → `CheckBanTonTaiTrongCauHinh` báo các bộ đang giữ bàn đó trước khi lưu.
- **Chọn DV chưa được đánh dấu tính giờ làm dịch vụ tính giờ** → "...không được cài đặt là dịch vụ tính giờ".
- **Bật mặc định bộ mới** → các bộ khác cùng chi nhánh tự về `IsMacDinh=false`.
- **Xóa bộ cấu hình → cache không được làm mới đúng** (known-issue clear sai key) → POS có thể còn đọc bộ đã xóa tới khi cache hết hạn (TTL 7 ngày) hoặc có thao tác ghi khác clear đúng.
- **Cấu hình giảm giá đơn theo mức giá (controller cũ)** → toàn bộ vô hiệu, không có endpoint nào hoạt động.

## Ví dụ cụ thể có số
Quán bi-a có khu A (10 bàn) và khu VIP (4 phòng):
- Admin tạo bộ "Khu A": gán dịch vụ tính giờ "Giờ bi-a thường" (40.000đ/giờ), gán 10 bàn khu A, bật **mặc định**.
- Tạo bộ "Khu VIP": dịch vụ tính giờ "Giờ bi-a VIP" (70.000đ/giờ), gán 4 phòng VIP, **không** bật mặc định.
- Thu ngân mở bàn A3 → POS tự áp giờ bi-a thường 40.000đ/giờ; khách chơi 1 giờ 30 phút → tính 60.000đ giờ + món thêm.
- Admin thử gán bàn A3 vào thêm bộ "Khu VIP" → hệ thống báo "bàn A3 đã được thiết lập ở cấu hình Khu A" → chặn lưu.
- Admin bật mặc định cho "Khu VIP" → bộ "Khu A" tự bỏ mặc định (chỉ 1 mặc định/chi nhánh).

## Hậu điều kiện
- Bộ cấu hình được lưu/sửa/xóa trong DB; mỗi thao tác ghi → clear cache cấu hình theo đơn vị (xem known-issue với thao tác Xóa) → POS lần sau lấy cấu hình mới.
- Không phát sinh message/event cross-service; chỉ ảnh hưởng cache + màn POS.
