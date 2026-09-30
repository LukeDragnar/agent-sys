# Luồng — Lịch hẹn khách hàng (Đặt lịch)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh (trừ tạo lịch online — công khai).
- Hầu hết thao tác yêu cầu **QLDV được phép xem POS** (`IsAllowAppointmentViewForCashier`); false → "Đơn vị của bạn không cho phép sử dụng tính năng này!".
- Có sẵn: khách hàng (hoặc tạo mới khi đặt), dịch vụ/combo, khu vực-chỗ ngồi, nguồn đặt lịch.
- Có cấu hình Booking — `cauHinh == null` sẽ chặn tạo lịch (xem case biên).

## Luồng dùng (user)

### Tạo lịch
1. Nhân viên mở form tạo lịch → hệ thống sinh **mã lịch** (`GetMaDatLich`).
2. Chọn khách, dịch vụ/combo, thời gian, chỗ ngồi, nhân viên phục vụ → **Lưu** (`ThemMoi`).
3. `InsertAsync`: tách khách đính kèm → mỗi khách 1 bản ghi `DatLich`; tạo/tìm khách (Customer); tạo chi tiết; tạo shortlink; gửi SMS/Zalo + notification; commit transaction.
4. **Tạo lịch online (web):** khách đặt trên website → `TaoMoi` (công khai), nguồn = WEBSITE.
5. **Đặt hàng loạt:** `DatLichHangLoat` lặp từng khung giờ, mỗi khung gọi `InsertAsync` riêng.

### Xem & cập nhật
6. Xem lịch theo thời gian / vị trí / nhân viên / ngày (bản thường + bản telesale). Nhân viên non-multi-role chỉ thấy lịch của mình.
7. Cập nhật thông tin / trạng thái lịch; đồng bộ Contact nếu lịch có IdContact; publish notification.

## Luồng cấu hình (admin — Nguồn đặt lịch)
1. Mở **Quản lý nguồn đặt lịch** → list nguồn theo đơn vị.
2. **Thêm/Sửa**: validate tên bắt buộc, chống trùng tên trong đơn vị; nếu đặt mặc định → bỏ mặc định nguồn cũ.
3. **Xóa**: chặn nếu nguồn lõi (`Ma == TRUCTIEP`/`WEBSITE`); ngược lại xóa cứng.
4. **Đặt mặc định**: trong transaction bỏ mặc định nguồn cũ → set nguồn này mặc định.
5. **Khởi tạo mặc định**: copy nguồn hệ thống (`IdDonVi == Guid.Empty`) chưa trùng tên về đơn vị.

## Các trường hợp / case biên
- **`cauHinh == null`** → `InsertAsync` chặn "Không tìm thấy cấu hình!" → không tạo được lịch (code tự nhận đây là điểm cần xử lý lại).
- **`SoLuongKhach > MaxCustomerInGroup`** (Max>0) → lỗi giới hạn nhóm khách.
- **Đặt hàng loạt lỗi giữa chừng:** lỗi ở khung giờ thứ N → các lịch trước đã commit (không transaction bao batch) → tạo lịch một phần, không rollback toàn cục.
- **Khách đính kèm thiếu Tên/SĐT** (khách thứ 2+) → lỗi; **khách trùng IdKhachHang** → "Các khách hàng không được trùng nhau!".
- **ChuyenTrangThaiTaoLich khi lịch đã HoanThanh** → từ chối; chỉ áp khi trạng thái đích ∈ {DaXacNhan, ChoPhucVu, DangPhucVu, Huy, KhongDen}.
- **Xóa lịch online** khi cấu hình Contact `IsNotAllowedAdminRemoveLichHenOnl` bật → chặn (trừ admin/CSKH/super).
- **Cập nhật thời gian phục vụ cho lịch đã xóa** → vẫn trả `true` (không lỗi).
- **Báo cáo đặt lịch:** toàn bộ endpoint `BookingReportController` bị comment → gọi route báo cáo trả 404 (tính năng tắt).

## Ví dụ cụ thể có số
Spa E, chi nhánh trung tâm, có nguồn mặc định "Trực tiếp":
- Lễ tân tạo lịch cho chị Hoa: dịch vụ "Gội đầu", giờ **14:00 hôm nay**, bàn số 5, nhân viên Lan → Lưu OK; hệ thống gửi SMS nhắc lịch.
- Đặt hàng loạt 3 khung giờ (9h, 10h, 11h) cho 3 khách. Khung 9h và 10h tạo OK, khung 11h thiếu SĐT khách → lỗi: **2 lịch (9h, 10h) đã được tạo**, khung 11h không tạo.
- Admin vào Nguồn đặt lịch, thêm "Fanpage", đặt làm mặc định → nguồn "Trực tiếp" bị bỏ mặc định, mỗi đơn vị chỉ còn 1 nguồn mặc định.
- Admin thử xóa nguồn "Website" → bị chặn (nguồn lõi không xóa được).
- Quản lý mở báo cáo đặt lịch → route trả lỗi không tìm thấy (tính năng đang tắt).

## Hậu điều kiện
- **Tạo lịch:** ghi `DatLich` + chi tiết; tạo/tìm khách; gửi SMS/Zalo; publish thông báo cho thu ngân/QLLH/admin; `SaveLogTuongTacKhachHang`.
- **Đổi trạng thái:** đồng bộ Contact (nếu có IdContact) + publish notification + `UpdateThoiGianDenCuaHang`.
- **Nguồn đặt lịch:** Thêm/Sửa/Xóa/Mặc định ghi DB; mỗi đơn vị tối đa 1 nguồn mặc định.
- **Báo cáo:** không có hậu điều kiện (tính năng vô hiệu hóa).
