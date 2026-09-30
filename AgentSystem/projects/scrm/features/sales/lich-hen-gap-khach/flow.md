# Luồng — Lịch hẹn gặp khách

## Tiền điều kiện
- Người dùng đã đăng nhập (trừ trang đặt lịch web — ẩn danh), thuộc đơn vị + chi nhánh.
- Đơn vị đã có **cấu hình đặt lịch** (thiếu → `InsertAsync` báo "Không tìm thấy cấu hình!").
- QLDV phải được phép xem POS (`IsAllowAppointmentViewForCashier`) cho hầu hết thao tác.
- Có dịch vụ / combo để gắn vào lịch; có cấu hình SMS nếu muốn gửi nhắc lịch.
- Duyệt lịch từ contact: đơn vị bật yêu cầu duyệt; người duyệt là sale leader.

## Luồng dùng (đặt lịch nhân viên)
1. Nhân viên mở màn đặt lịch, chọn xem theo thời gian / vị trí / nhân viên / ngày (`DsDatLichTheo*`).
2. Bấm tạo lịch → form: chọn khách (tìm/tạo `KhachHang`), dịch vụ/combo, thời gian, nhân viên, chỗ ngồi.
3. `ThemMoi` → `InsertAsync` (nguồn = NhanVienDatLich): tách khách đính kèm → N entity `DatLich` + `DatLichChiTiet`, tìm/tạo KH, tạo shortlink, gửi SMS/Zalo + notification, transaction.
4. Lịch hiển thị; gửi thông báo cho thu ngân / QLLH / admin (`GuiThongBaoInfo`).

## Luồng đổi trạng thái
1. Mở lịch → `CapNhatTrangThai`: load DB, lưu `TrangThaiTruoc`, check quyền sale + QLDV POS.
2. Update trạng thái → đồng bộ Contact (nếu `IdContact`) + publish `UpdateThoiGianDenCuaHang` + notification trạng thái.
3. Khi tạo đơn từ lịch: `ChuyenTrangThaiTaoLich` (chặn nếu lịch đã Hoàn thành). Khi hủy đơn: `CapNhatTrangThaiKhiHuyDon` → gán `TrangThai = TrangThaiTruoc`.

## Luồng duyệt lịch từ contact (sale leader)
1. Leader vào tab **Chờ duyệt lịch** → `GetListContactDatLichChoDuyet` (ContactController) trả contact có lịch ở state `DatLich_ChoDuyet`.
2. **Duyệt** → `DuyetContactDeTaoLich` → tạo lịch thật (state `DatLich_DaDuyet`), đồng bộ `SyncContactLichHenToLichHen`.
3. **Hủy duyệt** → `HuyDuyetContactTaoLich` → trả về `DatLich_ChoDuyet`.

## Luồng end-user web (ẩn danh)
1. Khách mở trang đặt lịch public → `TaoMoi` (AllowAnonymous): set nguồn = WEBSITE, Loai = DatLichTruoc, nguồn tạo KH = KhachHangDatLichTuWeb.
2. `InsertAsync` lấy DonVi từ `model.IdDonVi` (vì `_currentUser == null`).

## Các trường hợp / case biên
- **Cấu hình null:** `InsertAsync` trả "Không tìm thấy cấu hình!" (Service:2056) → không tạo được lịch (điểm cứng, có comment tự nhận cần xử lý lại).
- **Vượt giới hạn nhóm khách:** `SoLuongKhach > MaxCustomerInGroup` (Max>0) → lỗi giới hạn (Service:2059).
- **Khách đính kèm thiếu/trùng:** khách thứ 2+ thiếu Tên/SĐT → lỗi; trùng `IdKhachHang` → "Các khách hàng không được trùng nhau!".
- **Đặt hàng loạt lỗi giữa chừng:** `DatLichHangLoat` không transaction bao batch — lỗi ở khung thứ N thì các lịch trước đã commit (tạo lịch một phần, không rollback) — Service:4824-4837.
- **ChuyenTrangThaiTaoLich khi lịch Hoàn thành:** bị chặn; chỉ áp khi `cauHinh.UpdateTrangThaiTaoLich` ∈ {DaXacNhan, ChoPhucVu, DangPhucVu, Huy, KhongDen}.
- **Xóa lịch online:** bị chặn nếu cấu hình Contact `IsNotAllowedAdminRemoveLichHenOnl` (trừ admin/CSKH/super).
- **Quyền sale theo phân cấp:** QL Sale / SaleLead / Sale chỉ thao tác lịch có SaleId thuộc danh sách user được phép.
- **Cập nhật thời gian phục vụ trên lịch đã xóa:** `CapNhatThoiGianPhucVu` vẫn trả `true` (không lỗi) dù lịch đã xóa.
- **QLDV không được phép POS:** mọi thao tác → "Đơn vị của bạn không cho phép sử dụng tính năng này!".

## Ví dụ cụ thể có số
Spa C có cấu hình đặt lịch, MaxCustomerInGroup = 3, bật yêu cầu duyệt lịch contact.
- Lễ tân đặt lịch cho chị Mai + 2 khách đi cùng (3 khách, ≤ 3) ngày 25/06 14:00, gắn combo gội + massage, nhân viên KTV An.
- Hệ thống tạo lịch + gửi SMS nhắc tới chị Mai; thông báo tới thu ngân. Nếu nhập 4 khách → lỗi vượt giới hạn nhóm.
- Telesale Bình đặt lịch cho 1 contact → lịch vào **Chờ duyệt**. Leader Dũng vào tab "Chờ duyệt lịch" thấy 1 contact → **Duyệt** → lịch tạo thật, contact sang `DatLich_DaDuyet`.
- Lễ tân đặt hàng loạt 5 khung giờ; khung thứ 4 trùng cấu hình lỗi → dừng. Kết quả: 3 lịch đầu **đã tạo**, khung 4-5 chưa → lễ tân kiểm tra lại và đặt bù khung 4-5.
- Hôm trước ngày hẹn, job nhắc lịch quét lô 50 lịch có Ngay = hôm nay + numberOfDays → gửi SMS/Zalo, set `NgayGui` để không gửi lại.

## Hậu điều kiện
- Tạo lịch → N bản ghi `DatLich` + `DatLichChiTiet`; tìm/tạo KH; shortlink; SMS/Zalo + notification.
- Đổi trạng thái → cập nhật lịch + đồng bộ Contact + publish notification.
- Duyệt lịch contact → tạo lịch thật + đồng bộ trạng thái lịch ↔ contact.
- Job nhắc lịch → gửi SMS/Zalo + đánh dấu `NgayGui` (không gửi lại).
