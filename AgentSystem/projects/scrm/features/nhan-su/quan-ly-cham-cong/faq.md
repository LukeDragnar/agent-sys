> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý chấm công

**Q: Nhân viên check-in mà báo lỗi "Thiếu dữ liệu check-in" / "Dữ liệu check-in không hợp lệ"?**
A: Lỗi này xảy ra khi app gửi thiếu thông tin chấm công hoặc dữ liệu bị sai định dạng (ví dụ mất kết nối giữa chừng, app cũ chưa cập nhật). Hãy thử check-in lại; nếu vẫn lỗi, cập nhật app mới nhất hoặc liên hệ hỗ trợ.

**Q: Vì sao nhân viên không chấm công được dù đã đến cửa hàng?**
A: Nếu cửa hàng bật kiểm tra wifi (BSSID) hoặc thiết bị (IMEI) thì máy phải đang dùng đúng wifi cửa hàng / đúng thiết bị **đã đăng ký và đã được duyệt**. Hãy kiểm tra: wifi đã đăng ký + duyệt trong **Wifi BSSID** chưa, hoặc thiết bị nhân viên đã đăng ký IMEI và được quản lý duyệt chưa.

**Q: Tôi bấm "Phê duyệt" wifi/thiết bị 2 lần thì nó lại thành chưa duyệt?**
A: Đúng. Nút phê duyệt là công tắc bật/tắt: bấm lần 1 là duyệt, bấm lần 2 là bỏ duyệt. Nếu muốn giữ ở trạng thái đã duyệt thì chỉ bấm 1 lần.

**Q: Quản lý chi nhánh không thấy wifi/thiết bị của chi nhánh khác?**
A: Đúng thiết kế. Mỗi người chỉ xem và thao tác wifi/IMEI trong chi nhánh của mình. Hiện cả tài khoản admin cũng bị giới hạn theo chi nhánh ở màn này.

**Q: Tôi quên duyệt công, hệ thống có tự duyệt không?**
A: Không. Buổi công ở trạng thái **Chờ duyệt** cần quản lý vào duyệt (hoặc từ chối kèm lý do). Chỉ buổi đang chờ duyệt mới duyệt/từ chối được; buổi đã xử lý rồi thì không đổi qua màn này.

**Q: Nhân viên quên check-in thì sao?**
A: Quản lý có thể **chấm công hộ** trong màn Giờ làm việc, nhập buổi công cho nhân viên đó, rồi duyệt như bình thường.

**Q: Nhân viên có tự xem được công của mình không?**
A: Có. Có báo cáo "chấm công của tôi" — luôn chỉ hiển thị công của chính người đang đăng nhập, không xem được công người khác qua màn này.

**Q: Xuất Excel báo cáo chấm công bị báo lỗi, không ra file?**
A: Thường do khoảng thời gian/bộ lọc đang chọn không có dữ liệu, hoặc dịch vụ xuất gặp lỗi. Hãy chọn lại khoảng thời gian có dữ liệu rồi xuất; có 2 kiểu layout để chọn nếu một kiểu chưa phù hợp.

**Q: Sao xếp được ca nhưng không đổi được khung giờ?**
A: Nếu khung giờ đang có nhân viên được gán lịch theo nó thì hệ thống chặn đổi để tránh hỏng lịch đã xếp. Hãy gỡ/đổi lịch các nhân viên đang dùng khung giờ đó trước.
