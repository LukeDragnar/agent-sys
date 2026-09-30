> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Tự động hóa đơn & đối soát chạy nền

**Q: Đơn đã giao xong rồi mà sao không thấy hóa đơn điện tử tự ra?**
A: Tự phát hành chạy ngầm và có độ trễ (mặc định khoảng 5 phút sau khi đơn đạt mốc trạng thái cấu hình). Nếu chờ vẫn không ra, kiểm tra: (1) trạng thái giao hàng của đơn có được đánh dấu "cho phép tự phát hành" không; (2) đã bật tiện ích hóa đơn điện tử và cấu hình tài khoản/mẫu chưa; (3) đơn có thể đã có hóa đơn từ trước nên hệ thống bỏ qua. Nếu vẫn không ra, có thể mạch nền gặp lỗi và "nuốt" lỗi im lặng — cần kỹ thuật tra log.

**Q: Hệ thống tự ra hóa đơn hay tôi vẫn phải bấm tay?**
A: Tự — đây là phần chạy ngầm, không cần bấm nút. Khi đơn chuyển sang trạng thái đã cấu hình, hệ thống tự xếp đơn vào hàng đợi và sau độ trễ sẽ tự phát hành (hoặc tự tạo nháp, tùy cấu hình loại hóa đơn tự động).

**Q: Đơn hoàn (khách trả hàng) sau khi đối soát thì phiếu bán trả lại có tự lên không?**
A: Có. Sau khi bạn đối soát đơn hoàn, hệ thống xếp đơn vào một hàng đợi; một job nền chạy đều (khoảng mỗi 10 giây) sẽ đẩy sang bộ phận kế toán để tạo phiếu bán trả lại tương ứng. Bạn không cần tạo phiếu thủ công.

**Q: Tôi đối soát đơn hoàn xong mà phiếu trả lại vẫn chưa thấy?**
A: Phiếu được tạo qua job nền nên có độ trễ ngắn. Nếu lâu vẫn không lên, có thể bản ghi chưa được xếp vào hàng đợi tạo phiếu, hoặc bước tạo phiếu bên kế toán báo lỗi. Cần kỹ thuật kiểm tra bảng hàng đợi (trạng thái SUCCESS/FAILED + thông báo lỗi).

**Q: Đối soát giao dịch ngân hàng MB chạy lúc nào?**
A: Tự động hằng ngày, chỉ chạy trong khung giờ từ 8h sáng trở đi. Job đọc file đối soát ngân hàng gửi về cho ngày hôm trước; nếu chưa có file thì chờ và thử lại mỗi 30 phút. Khi đối soát xong, hệ thống xuất file kết quả (khớp hoặc lệch).

**Q: Đối soát MB báo "thừa MB" / "thừa hệ thống" / "lệch" nghĩa là gì?**
A: "Thừa MB" = giao dịch có ở file ngân hàng nhưng không có trong hệ thống; "thừa hệ thống/Sandbox" = có trong hệ thống nhưng không có ở file ngân hàng; "lệch" = cùng giao dịch nhưng số tiền khác nhau. Các trường hợp này được ghi ra file chênh lệch để kế toán rà soát.

**Q: Có ngân hàng nào khác MB được đối soát tự động không?**
A: Hiện job đối soát tự động chỉ làm cho MB (kênh "0G"). Ngân hàng khác chưa có job đối soát tương ứng.

**Q: Tôi muốn xem nhật ký các việc chạy ngầm này ở đâu?**
A: Hiện chưa có màn nhật ký riêng cho người dùng. Trạng thái nằm ở các bảng hàng đợi/nhật ký kỹ thuật và log hệ thống — cần bộ phận kỹ thuật tra giúp.

**Q: Có tắt/bật từng việc chạy ngầm này từ giao diện được không?**
A: Không có công tắc trên giao diện. Việc bật/tắt các job và consumer nằm trong cấu hình hệ thống (code), do kỹ thuật quản lý.
