> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Hóa đơn mua bán (Phiếu mua / bán / trả lại hàng mua)

**Q: Tôi tạo phiếu mua hàng xong thì hàng đã vào kho chưa?**
A: Tùy trạng thái. Nếu bạn lưu ở trạng thái Nháp thì chưa sinh phiếu nhập kho. Nếu lưu ở trạng thái khác (đang xử lý), hệ thống tạo phiếu nhập kho + (nếu chọn) phiếu chi + công nợ nhà cung cấp. Hàng thực sự cập nhật tồn kho/giá vốn khi bạn bấm **Hoàn thành** và kho xử lý xong.

**Q: Vì sao tôi không bấm Hoàn thành được phiếu mua?**
A: Phiếu đang ở trạng thái Nháp không cho hoàn thành — phải chuyển sang "đang xử lý" trước. Ngoài ra, nếu kho báo lỗi (ví dụ IMEI chưa quét đủ) thì cũng không hoàn thành được; xử lý xong điều kiện kho rồi thử lại.

**Q: Tôi hủy phiếu mua hàng thì phiếu nhập kho và phiếu chi có bị hủy theo không?**
A: Có. Hủy phiếu mua sẽ hủy cả phiếu nhập kho và phiếu chi liên kết, rồi đặt phiếu mua về trạng thái Hủy. Nhưng nếu phiếu nhập đã hoàn thành thì hệ thống chặn để tránh sai lệch tồn kho — khi đó cần xử lý theo nghiệp vụ khác.

**Q: Sao tôi không thấy nút tạo phiếu bán hàng ở phần kế toán?**
A: Phiếu bán hàng kế toán **sinh tự động** khi đơn hàng/giao vận hoàn tất, không nhập tay ở màn kế toán. Màn này để xem, lọc, xem lịch sử, thống kê tình trạng hóa đơn và tạo phiếu bán trả lại khi khách trả hàng.

**Q: "Xuất muộn" trong thống kê tình trạng hóa đơn nghĩa là gì?**
A: Hệ thống so ngày xuất hóa đơn thực tế với ngày tiêu chuẩn phải xuất (tính từ ngày giao hàng). Nếu xuất sau mốc đó thì đánh dấu "Xuất muộn"; xuất đúng hạn là "Đã xuất"; chưa xuất là "Chưa xuất".

**Q: Khách trả hàng đã mua thì làm thế nào?**
A: Trên phiếu bán hàng tương ứng, dùng chức năng **Tạo phiếu bán trả lại**. Tùy chọn, hệ thống tạo phiếu bán giảm trừ công nợ, phiếu chi hoàn tiền và phiếu nhập kho (hàng về kho).

**Q: Trả lại hàng cho nhà cung cấp thì làm ở đâu, và khi nào tiền/tồn kho mới đổi?**
A: Vào **Trả lại hàng mua**, chọn phiếu mua gốc đã hoàn thành, chọn sản phẩm và số lượng còn được trả. Các phiếu liên quan (xuất kho, thu lại tiền/giảm công nợ) chỉ sinh khi phiếu trả lại ở trạng thái **Hoàn thành**.

**Q: Phiếu trả lại hàng mua đã hoàn thành mà tôi nhập sai thì sửa thế nào?**
A: Phiếu trả lại đã Hoàn thành bị đóng băng, không sửa được nữa. Bạn cần xử lý theo nghiệp vụ điều chỉnh/đối chiếu — liên hệ kế toán trưởng hoặc bộ phận hỗ trợ.

**Q: Số liệu phiếu mua/bán đã đẩy sang MISA chưa?**
A: Hệ thống đẩy chứng từ sang MISA qua hàng đợi (job), không tức thời. Nếu đơn vị có cấu hình kết nối kế toán, chứng từ sẽ được đồng bộ; nếu chưa thấy, kiểm tra trạng thái đồng bộ trên lưới hoặc liên hệ hỗ trợ.
