# FAQ — Báo cáo & phân tích khách hàng

> Draft — cần bổ sung từ ticket khách thật.

**Q: Báo cáo chân dung khách hàng cho tôi xem được những gì?**
A: Một bức tranh tổng quan về tệp khách: tổng số khách hàng, số khách mới trong tuần, biểu đồ lượng khách theo ngày (tách khách lẻ và khách cũ), và các biểu đồ phân nhóm khách, giới tính, hạng khách, phân bố theo tỉnh/thành.

**Q: "Khách mới", "khách cũ", "khách lẻ" được tính thế nào?**
A: Dựa trên lịch sử mua hàng chứ không phải khách tự khai. Khách lần đầu phát sinh giao dịch là khách mới; khách quay lại những lần sau được tính là khách cũ. Cách phân loại chi tiết cần product xác nhận.

**Q: Vì sao tôi đổi chi nhánh mà số liệu không đổi / chỉ thấy chi nhánh của mình?**
A: Nếu bạn không phải admin, super-user, CSKH hoặc kế toán, và đơn vị chưa bật cấu hình "chi nhánh hiển thị tất cả khách hàng của đơn vị", thì báo cáo chỉ tính khách thuộc chi nhánh của bạn — kể cả khi bạn chọn chi nhánh khác. Hãy nhờ admin kiểm tra phân quyền/cấu hình.

**Q: Tôi chọn khoảng thời gian dài thì báo "Chỉ được xem dữ liệu trong khoảng N ngày"?**
A: Đơn vị có cấu hình giới hạn số ngày được phép truy vấn báo cáo. Hãy thu hẹp khoảng thời gian xuống trong giới hạn cho phép, hoặc nhờ admin điều chỉnh cấu hình.

**Q: Biểu đồ địa chỉ có nhiều khách rơi vào "Không chọn thành phố", có phải lỗi không?**
A: Không phải lỗi báo cáo. Đó là những khách chưa khai tỉnh/thành trong hồ sơ. Báo cáo gộp tất cả khách thiếu địa chỉ vào một nhóm. Muốn biểu đồ chính xác hơn, cần bổ sung tỉnh/thành cho hồ sơ khách hàng.

**Q: Số liệu báo cáo có "chốt" không, hay thay đổi theo thời gian?**
A: Số liệu là ảnh chụp tại đúng thời điểm bạn bấm xem. Nếu sau đó có thêm khách mới hoặc đơn hàng mới phát sinh, chạy lại báo cáo có thể ra con số khác.

**Q: Báo cáo này có làm thay đổi dữ liệu khách hàng của tôi không?**
A: Không. Đây là báo cáo chỉ đọc, chỉ thống kê và hiển thị; không tạo, sửa hay xóa bất kỳ khách hàng nào.

**Q: Tôi có xuất được báo cáo chân dung khách hàng ra Excel không?**
A: Cần product xác nhận — màn hình hiện tại tập trung vào hiển thị biểu đồ.
