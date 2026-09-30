> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Lịch hẹn gặp khách

**Q: Tôi đặt lịch mà báo "Đơn vị của bạn không cho phép sử dụng tính năng này"?**
A: Hầu hết thao tác lịch hẹn yêu cầu đơn vị bật quyền cho quản lý đơn vị (QLDV) được dùng tính năng POS. Nếu bạn là QLDV, nhờ admin bật cho phép QLDV xem/dùng POS trong cấu hình. Nếu là vai khác mà vẫn lỗi, kiểm tra cấu hình quyền của đơn vị.

**Q: Tôi đặt lịch mà báo "Không tìm thấy cấu hình!"?**
A: Đơn vị của bạn chưa có (hoặc thiếu) cấu hình đặt lịch. Hệ thống cần cấu hình này để tạo lịch. Liên hệ admin / kỹ thuật để khai báo cấu hình đặt lịch cho đơn vị, sau đó tạo lại.

**Q: Tôi đặt lịch hàng loạt 10 khung giờ, một khung bị lỗi — các khung kia có bị mất không?**
A: Không. Khi đặt hàng loạt, mỗi khung giờ được tạo riêng. Nếu khung thứ N bị lỗi và dừng lại, các khung trước đó **đã được tạo thành công** rồi. Bạn nên kiểm tra lại danh sách lịch để biết những khung nào đã tạo và đặt lại khung bị lỗi.

**Q: Sale tôi đặt lịch từ contact rồi nhưng lịch chưa hiện ra?**
A: Nếu đơn vị bật yêu cầu duyệt lịch, lịch đặt từ contact sẽ ở trạng thái **Chờ duyệt**. Sale leader cần vào tab "Chờ duyệt lịch" và bấm Duyệt thì lịch mới tạo chính thức. Nếu leader hủy duyệt, lịch quay lại chờ.

**Q: Vì sao tôi (nhân viên) chỉ thấy lịch của mình mà không thấy lịch người khác?**
A: Nhân viên không có nhiều vai trò thường chỉ được xem lịch do mình phụ trách. Quản lý / admin xem được nhiều hơn. Đây là phân quyền theo vai trò, không phải lỗi.

**Q: Khách tự đặt lịch trên web có cần đăng nhập không?**
A: Không. Trang đặt lịch công khai cho phép khách tự đặt mà không cần tài khoản. Lịch khách đặt từ web được ghi nhận nguồn là "Website" và sẽ vào hệ thống theo đơn vị tương ứng.

**Q: Tôi không xóa được lịch hẹn đặt online?**
A: Một số đơn vị bật cấu hình không cho admin xóa lịch hẹn online (để giữ lịch sử khách tự đặt). Khi đó chỉ admin/CSKH/super-user mới xóa được. Nếu cần xóa, liên hệ người có quyền hoặc kiểm tra cấu hình.

**Q: Hệ thống có tự nhắc lịch cho khách không?**
A: Có. Có job gửi nhắc lịch trước ngày hẹn (theo số ngày cấu hình): hệ thống gửi SMS/Zalo cho khách và đánh dấu đã gửi để không gửi lại. Việc gửi phụ thuộc cấu hình SMS của đơn vị.
