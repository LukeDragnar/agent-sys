> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý log & nhật ký (Log Delivery / Log Ecommerce)

**Q: Khách báo đơn từ sàn (Shopee/TikTok) không lên hệ thống, tôi kiểm tra ở đâu?**
A: Vào **Hệ thống → Log Ecommerce**, lọc theo thời gian/đơn vị để xem data từ sàn đã tới hệ thống chưa và lỗi ở bước nào. Nếu thấy bản ghi bị lỗi, bấm **Gửi lại** để hệ thống nhận/xử lý lại.

**Q: Trạng thái giao hàng của đơn không cập nhật, làm sao biết do đâu?**
A: Vào **Hệ thống → Log Delivery**, lọc và xem log trao đổi với luồng giao vận. Nếu bản ghi cập nhật trạng thái bị lỗi, có thể **Gửi lại** để đẩy lại vào luồng giao vận xử lý lần nữa.

**Q: Bấm "Gửi lại" là gửi lại cái gì? Có an toàn không?**
A: "Gửi lại" đẩy bản ghi data đang bị lỗi vào lại luồng xử lý (giao vận hoặc sàn) để hệ thống chạy lại. Nó tác động lên dữ liệu nghiệp vụ thật, nên chỉ dùng cho bản ghi thực sự cần xử lý lại; tránh bấm nhiều lần một bản ghi để khỏi xử lý trùng.

**Q: Gửi lại rồi mà vẫn lỗi thì sao?**
A: "Gửi lại" chỉ đưa data vào xử lý lại, không tự sửa nguyên nhân gốc. Nếu data sai định dạng hoặc đơn vị cấu hình thiếu, nó sẽ vẫn lỗi. Cần xử lý nguyên nhân gốc rồi mới gửi lại.

**Q: Tôi muốn sửa hoặc xóa một dòng log thì làm thế nào?**
A: Không sửa/xóa thủ công được. Log do hệ thống tự ghi để phục vụ tra cứu/khắc phục sự cố; bạn chỉ tra cứu và (nếu cần) gửi lại data lỗi.

**Q: Log cũ tìm không thấy, có phải bị mất không?**
A: Log được lưu ở kho riêng và có thể bị dọn theo định kỳ để tiết kiệm dung lượng, nên log quá cũ có thể không còn. Việc tra cứu/khắc phục nên làm sớm khi sự cố còn mới.

**Q: Màn này khách hàng có vào được không?**
A: Không. Đây là công cụ nội bộ của đội kỹ thuật/admin hệ thống (HT-AD), không mở cho khách hàng cuối.

**Q: Log Delivery khác Log Ecommerce thế nào?**
A: Log Delivery ghi data trao đổi với luồng **giao vận** (đơn vị vận chuyển, trạng thái giao hàng). Log Ecommerce ghi data nhận từ **sàn TMĐT** (Shopee, TikTok…). Cả hai đều cho tra cứu và gửi lại data lỗi, nhưng đẩy vào hai luồng xử lý khác nhau.
