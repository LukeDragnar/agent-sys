# Kiểm kho — Câu hỏi thường gặp

> Draft — cần bổ sung từ ticket khách thật.

> Tóm tắt: [summarize.md](./summarize.md) · Luồng: [flow.md](./flow.md)

---

**Q: Kiểm kho là gì? Có bắt buộc làm không?**
A: Là việc đếm lại hàng thực tế trong kho rồi so với số hệ thống đang ghi, để phát hiện thừa/thiếu. Không bắt buộc, nhưng nên làm định kỳ (cuối tháng/quý) để kiểm soát thất thoát.

**Q: Kiểm kho xong mà số lệch thì xử lý thế nào?**
A: Phiếu kiểm sẽ đánh dấu món đó là **"Kiểm sai"** kèm số đã đếm và số hệ thống. Bản thân phiếu kiểm **không tự sửa tồn kho** — đây chỉ là bước phát hiện chênh lệch. Để nắn tồn về đúng thực tế, bạn cần tạo phiếu nhập (nếu thực tế nhiều hơn) hoặc phiếu xuất điều chỉnh (nếu thực tế ít hơn). *(Quy trình điều chỉnh cụ thể cần product xác nhận.)*

**Q: Tại sao tôi quét mã mà báo "Không trong thời gian cho phép kiểm kho"?**
A: Mỗi phiếu có Ngày bắt đầu và Ngày kết thúc kiểm. Nếu thời điểm hiện tại nằm ngoài khoảng đó, hệ thống chặn quét/nhập. Mở phiếu kiểm để xem lại hoặc chỉnh ngày (nếu phiếu chưa hết hạn).

**Q: "Số lần kiểm lại tối đa" để làm gì?**
A: Cho phép kiểm đi kiểm lại nhiều lần trong cùng một phiếu (ví dụ lần 1 lệch, đếm lại lần 2). Mỗi lần bấm "Kiểm tra kết quả" tính là 1 lượt. Hết số lượt đã đặt thì không kiểm lại được nữa trong phiếu đó.

**Q: Quét IMEI báo "Imei này đã được kiểm trước đó" là sao?**
A: Mỗi mã IMEI chỉ được quét 1 lần trong 1 phiếu. Nếu báo trùng nghĩa là IMEI đó đã quét rồi — không cần quét lại, số lượng đã được cộng.

**Q: Quét mã báo "Sản phẩm này chưa được nhập kho"?**
A: Mã vạch/IMEI bạn quét không tìm thấy trong kho đang kiểm (chưa nhập, hoặc đã xuất hết, hoặc sai kho/chi nhánh). Kiểm tra lại sản phẩm đã được nhập vào đúng kho này chưa.

**Q: Tôi tạo phiếu "sản phẩm cụ thể" nhưng quét trúng sản phẩm khác thì sao?**
A: Hệ thống báo "Sản phẩm này không nằm trong danh sách sản phẩm trên phiếu kiểm" và không ghi nhận. Nếu muốn kiểm mọi thứ trong kho, chọn kiểu "Tất cả sản phẩm" khi tạo phiếu.

**Q: Ai nhìn thấy / kiểm được phiếu?**
A: Người tạo phiếu, người được thêm vào danh sách "người kiểm kho" hoặc "nhân viên phụ trách kho" của phiếu. Admin tổng và bộ phận chăm sóc khách hàng thấy tất cả phiếu trong đơn vị. *(Phân quyền chi tiết cần product xác nhận.)*

**Q: Có quét bằng điện thoại/máy quét mã vạch được không?**
A: Màn kiểm hỗ trợ nhập mã vạch/IMEI nên máy quét mã vạch dùng được. Chi tiết thiết bị hỗ trợ cần product xác nhận.
