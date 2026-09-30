> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Nghiệp vụ tự động theo trạng thái giao hàng

**Q: Sao chi tiêu của khách tự nhiên thay đổi mà không ai sửa?**
A: Đây là hệ thống tự động: khi đơn giao hàng của khách đổi trạng thái (đã giao, đã thanh toán, hủy, hoàn…), chi tiêu và công nợ của khách được tính lại cho khớp. Đơn giao thành công thì chi tiêu tăng; đơn bị hủy/hoàn thì chi tiêu trừ lại. Bạn kiểm tra trạng thái giao của đơn liên quan sẽ thấy con số khớp.

**Q: Tôi cập nhật đơn "đã giao" rồi mà chi tiêu khách chưa thấy tăng?**
A: Hệ thống có **độ trễ cấu hình** (thường để vài chục phút đến vài giờ) trước khi xử lý, để tránh trường hợp giao xong khách trả lại ngay thì khỏi cộng nhầm. Bạn chờ qua khoảng delay này; nếu vẫn không thấy, nhờ CSKH kiểm tra màn quản trị nghiệp vụ xem đơn có bị "Lỗi" không.

**Q: Đơn giao một phần thì tính chi tiêu thế nào?**
A: Chỉ tính phần khách thực nhận: tiền thu hộ (COD) của phần đã giao cộng với tiền cọc. Phần hàng không giao được coi như không phát sinh, không tính vào chi tiêu.

**Q: Đơn bị giao rồi lại hoàn, con số có bị sai/cộng dồn không?**
A: Không. Hệ thống xử lý các lần đổi trạng thái **lần lượt theo đúng thứ tự thời gian**. Kết quả cuối cùng luôn khớp trạng thái mới nhất: nếu đơn cuối cùng là "hoàn" thì chi tiêu được trừ về 0.

**Q: Hệ thống này có tự trừ tồn kho hay tự xuất hóa đơn khi giao hàng không?**
A: Theo cấu hình hiện tại, kênh tự động này **chỉ cập nhật chi tiêu và công nợ khách**. Việc trừ tồn kho / tạo hóa đơn đi theo luồng khác. (Khung mở rộng cho tồn kho/hóa đơn đã được chuẩn bị nhưng chưa bật — cần product xác nhận nếu cần dùng.)

**Q: Một nghiệp vụ bị lỗi thì sao, có mất không?**
A: Không mất. Hệ thống tự thử lại tối đa 3 lần (mỗi lần cách nhau ít phút). Nếu vẫn lỗi, bản ghi được chuyển vào lịch sử với trạng thái "Lỗi" và còn một tác vụ nền quét lại các bản ghi lỗi để thử thêm. CSKH cũng có thể vào bấm "Thử lại" thủ công.

**Q: Tôi muốn xử lý lại một nghiệp vụ đã chạy thành công thì được không?**
A: Không. Bản ghi đã "Thành công" sẽ bị chặn không cho chạy lại (để tránh cộng/trừ trùng). Chỉ những bản ghi "Lỗi" hoặc "Bỏ qua" mới được tạo lại để thử.

**Q: "Bỏ qua" trong lịch sử nghĩa là gì?**
A: Là khi một đơn có nhiều yêu cầu xử lý cùng nghiệp vụ, hệ thống chỉ giữ bản mới nhất; những bản cũ hơn đã bị thay thế sẽ được đánh dấu "Bỏ qua" thay vì xử lý, để tránh tính trùng.

**Q: Tôi muốn tắt tính năng tự cộng/trừ này cho cửa hàng của tôi?**
A: Được — đây là cấu hình theo đơn vị (đặt độ trễ = -1 để tắt). Liên hệ quản trị/CSKH để điều chỉnh. Cần product xác nhận về quyền chỉnh cấu hình này.
