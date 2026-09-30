# Luồng — Quản lý chấm công

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Chi nhánh đã khai báo **khung giờ làm việc** (để xếp ca và đối chiếu chấm công).
- Nhân viên đã được **xếp ca** (CaLamViecNhanVien) để có cơ sở chấm công đúng ca.
- Nếu bật chấm công theo wifi/thiết bị: đã có **wifi (BSSID)** và/hoặc **IMEI** được **đăng ký + duyệt** trong chi nhánh.
- Cấu hình đơn vị xác định vai được phép thao tác thiết bị wifi (`RoleDuocPhepThaoTacThietBiWifi`) và thiết bị chấm công (`RoleDuocPhepThaoTacThietBiChamCong`).

## Luồng dùng (nhân viên + quản lý)
1. **Xếp ca (quản lý):** chọn nhân viên + khung giờ + ngày → tạo từng ca (CreateSingle) hoặc tạo hàng loạt cơ bản/nâng cao (CreateBasic/CreateAdvanced). Có thể xem dạng danh sách hoặc lịch.
2. **Check-in (nhân viên):** đến cửa hàng, app gửi vị trí + ảnh (multipart). Nếu bật wifi/IMEI: app gọi CheckBSSID / CheckImei để xác minh wifi/thiết bị đã đăng ký + đã duyệt; pass thì cho check-in, không pass thì chặn.
3. **Check-out (nhân viên):** tương tự, ghi giờ về.
4. **Quản lý xem & duyệt:** vào Giờ làm việc, lọc theo danh sách hoặc lịch; chấm công hộ nếu nhân viên quên; với mỗi buổi đang **Chờ duyệt** bấm **Duyệt** (→ Đã duyệt) hoặc **Từ chối** (kèm lý do → Từ chối).
5. **Báo cáo:** chọn khoảng thời gian + chi nhánh → xem báo cáo chi tiết / theo ca / theo ngày; xuất Excel.

## Luồng cấu hình (admin / HR)
1. **Đăng ký wifi (BSSID):** Chấm công → Wifi BSSID → Thêm mới (nhập Value/Tên wifi, gắn chi nhánh) → IsDuyet mặc định = chưa duyệt → bấm **Phê duyệt** để bật.
2. **Đăng ký IMEI:** Chấm công → IMEI thiết bị → Thêm (hoặc nhân viên tự DangKyIMEI từ app) → quản lý **Phê duyệt**.
3. Việc thêm/sửa/xóa/duyệt wifi & IMEI yêu cầu vai nằm trong cấu hình đơn vị tương ứng; nếu không có quyền → "Bạn không có quyền thực hiện thao tác này".

## Các trường hợp / case biên
- **Check-in thiếu/sai dữ liệu:** model rỗng → "Thiếu dữ liệu check-in"; JSON sai → "Dữ liệu check-in không hợp lệ"; deserialize null → "Dữ liệu check-in không hợp lệ".
- **Phê duyệt wifi/IMEI 2 lần:** là **toggle** `IsDuyet = !IsDuyet` → lần 1 duyệt, lần 2 trở lại chưa duyệt.
- **Wifi/IMEI khác chi nhánh:** mọi thao tác bị ép `IdChiNhanh == user.IdChiNhanh` (nhánh admin xem toàn đơn vị đang tắt) → không thấy/không sửa bản ghi chi nhánh khác.
- **CheckImei/CheckExsit:** CheckExsit lọc theo `UserId == user.Id` (đã đăng ký cho chính nhân viên đó chưa); CheckBSSID match theo Value HOẶC Tên wifi.
- **Đổi khung giờ đang có người dùng:** CheckUserCaLamViecToChangeIsSuDungKhungGio phát hiện có nhân viên gán lịch theo khung giờ → chặn đổi.
- **Duyệt/Từ chối buổi đã xử lý:** chỉ buổi **Chờ duyệt** mới duyệt/từ chối; từ chối bắt buộc lý do.
- **Xuất Excel rỗng/lỗi:** `!rs.Success || rs.Data == null` → trả lỗi (BadRequest), không ra file.
- **GetCaLamViecNhanVienByDay:** đang để [AllowAnonymous] → gọi được không cần đăng nhập (lưu ý audit, xem dev.md).

## Ví dụ cụ thể có số
Cửa hàng B có 5 nhân viên, ca sáng 8:00–12:00, ca chiều 13:00–17:00.
- HR tạo ca hàng loạt cơ bản: gán cả 5 nhân viên vào ca sáng ngày 20/06 → tạo 5 bản ghi ca.
- Cửa hàng bật chấm công wifi: HR thêm wifi `ShopB-5G`, bấm Phê duyệt (IsDuyet=true). Nhân viên dùng máy nối wifi `ShopB-5G` → CheckBSSID trả approved → check-in được. Nhân viên dùng 4G/wifi lạ → không approved → chặn.
- 12:00 nhân viên An quên check-out → quản lý vào Giờ làm việc, chấm công hộ buổi của An, rồi bấm Duyệt → buổi chuyển từ Chờ duyệt sang Đã duyệt.
- HR bấm Phê duyệt wifi `ShopB-5G` lần nữa do nhầm → IsDuyet quay về false → nhân viên lập tức không chấm công wifi được nữa (phải bấm duyệt lại).
- Cuối tháng, quản lý lọc 01/06–30/06 chi nhánh B → xuất Excel `BaoCaoChamCong_01062026_30062026.xlsx`.

## Hậu điều kiện
- Buổi công sau khi **Đã duyệt** trở thành đầu vào cho **tính lương**.
- Thêm/sửa/xóa/duyệt wifi & IMEI đều ghi **lịch sử tác động** (LichSuTacDong) sang service Log.
- Tạo/xóa ca, chấm công, duyệt phiếu yêu cầu → cập nhật lịch ca và bảng công, không phát message ra ngoài (trừ phần báo cáo/Excel).
