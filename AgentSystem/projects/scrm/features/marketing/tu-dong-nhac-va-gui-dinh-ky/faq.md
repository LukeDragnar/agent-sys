> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Tác vụ tự động nhắc & gửi tin định kỳ

**Q: Vì sao khách của tôi không nhận được SMS chúc sinh nhật?**
A: Đường gửi SMS sinh nhật tự động hiện **đang tắt** trong hệ thống (phần lập lịch gửi sinh nhật bị vô hiệu hóa), nên hệ thống không tự bắn tin sinh nhật. Nếu bạn cần chúc sinh nhật khách, hãy dùng **Chiến dịch chăm sóc khách hàng** (lọc khách theo tháng/ngày sinh rồi đặt gửi định kỳ) hoặc gửi SMS/Zalo thủ công. Nếu shop bạn từng có gửi sinh nhật tự động, vui lòng liên hệ bộ phận hỗ trợ để xác nhận kênh đang dùng.

**Q: Tôi đã bật chiến dịch/nhắc lịch mà khách vẫn không nhận tin?**
A: Hãy kiểm tra theo thứ tự: (1) Tài khoản SMS/Zalo của đơn vị còn bật và **còn tiền/quota** không; (2) đã chọn **mẫu tin** đúng chưa; (3) số điện thoại khách có đúng/không bị che; (4) với nhắc lịch: lịch hẹn đó có bật "gửi nhắc trước ngày hẹn" và cấu hình nhắc lịch của đơn vị có bật không. Thiếu một trong các điều này là tin sẽ không gửi.

**Q: Tin nhắc/chiến dịch gửi chậm, không tới ngay khi đến giờ?**
A: Bình thường. Hệ thống chạy theo nhịp (vài giây đến vài phút một lần) và mỗi lượt chỉ xử lý một lô khách nhất định, nên khi có nhiều khách tin sẽ gửi rải ra trong khoảng thời gian chứ không bắn cùng lúc.

**Q: Tin Zalo của tôi không gửi vào ban đêm?**
A: Đúng, tin Zalo chỉ được gửi trong khung giờ cho phép. Ngoài khung giờ đó hệ thống sẽ hoãn và gửi lại khi vào giờ hợp lệ; bản ghi được giữ ở trạng thái chờ chứ không mất.

**Q: Khách bị nhận trùng một tin nhắc nhiều lần?**
A: Với nhắc lịch hẹn, mỗi lịch chỉ gửi 1 lần (hệ thống đánh dấu "đã gửi"). Nếu khách báo nhận trùng, có thể do có nhiều lịch/nhiều chiến dịch cùng nhắm tới khách đó, hoặc cùng một khách nằm trong nhiều nhóm điều kiện. Hãy rà lại các chiến dịch định kỳ đang bật.

**Q: Nhắc khách đóng tiền cọc chạy lúc nào?**
A: Tác vụ nhắc thanh toán đơn cọc chỉ chạy trong **khung giờ cố định** (đêm/sáng sớm) và phải bật cấu hình "gửi tin khi chưa thanh toán". Ngoài khung giờ đó tác vụ tạm nghỉ nên tin sẽ gửi vào đợt kế tiếp.

**Q: Tôi muốn dừng toàn bộ tin tự động cho một loại nghiệp vụ?**
A: Vào cấu hình thông báo tương ứng và **tắt** nghiệp vụ đó (hoặc tắt chiến dịch/đợt gửi). Ngoài ra, tắt tài khoản SMS/Zalo của đơn vị sẽ dừng mọi tin tự động.

**Q: Tin tự động có trừ tiền như tin gửi tay không?**
A: Có. Mỗi tin tự động (nhắc lịch, chiến dịch, nhắc nợ…) đều trừ tiền/quota tài khoản SMS/Zalo và ghi vào lịch sử gửi tin như tin gửi thủ công.
