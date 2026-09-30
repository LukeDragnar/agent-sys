> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý đơn đặt hàng

**Q: Tôi tạo đơn đặt hàng nhưng không sửa lại được?**
A: Đơn chỉ sửa được khi đang ở trạng thái **Nháp** hoặc **Trình lại**. Khi đơn đã được gửi duyệt / đã duyệt / đang xử lý thì khóa chỉnh sửa để giữ đúng quy trình. Nếu cần đổi, bạn có thể trình lại (nếu được) hoặc hủy và tạo đơn mới.

**Q: Tôi không xóa được đơn đặt hàng?**
A: Chỉ đơn ở trạng thái **Nháp** mới xóa được, và xóa là xóa hẳn. Đơn đã vào quy trình thì không xóa — bạn có thể **Hủy** đơn thay vì xóa.

**Q: Vì sao tôi duyệt được đơn nhưng không tạo phiếu mua hàng / xử lý được, hoặc ngược lại?**
A: Hệ thống tách hai quyền: **quyền duyệt** (cho bước Chờ duyệt → Đã duyệt) và **quyền tạo/xử lý mua** (cho bước Chờ xử lý → Hoàn thành). Bạn có thể được cấp một trong hai. Liên hệ admin nếu cần thêm quyền.

**Q: Đơn của tôi sau khi duyệt sao không thấy trạng thái "Đã duyệt"?**
A: Đúng quy trình. "Đã duyệt" chỉ là bước trung gian — duyệt xong đơn **tự chuyển sang Chờ xử lý** ngay. Bạn sẽ thấy đơn ở Chờ xử lý chứ không dừng ở Đã duyệt.

**Q: Đơn vị tôi không có bước duyệt thì đơn đi thế nào?**
A: Nếu đơn vị không bật duyệt, đơn bỏ qua bước Chờ duyệt và vào thẳng **Chờ xử lý** để mua hàng. Đây là do cấu hình (CanKiemDuyet).

**Q: Tôi hủy đơn thì các phiếu mua hàng đã tạo có bị hủy theo không?**
A: Nếu bạn chọn tùy chọn hủy kèm phiếu mua hàng liên quan, hệ thống sẽ hủy các phiếu chưa hoàn tất. Lưu ý nếu có nhiều phiếu, trong trường hợp hiếm có thể xảy ra lệch trạng thái — nếu thấy bất thường, báo bộ phận hỗ trợ.

**Q: Khi nào đơn tự chuyển sang Hoàn thành?**
A: Khi tất cả sản phẩm trên đơn đã **nhận đủ số lượng** (qua các phiếu mua hàng), hệ thống tự đặt đơn về **Hoàn thành**.

**Q: Tôi có nhiều đơn cần nhập một lúc, có nhập từ Excel được không?**
A: Được. Có chức năng nhập đơn đặt hàng từ Excel: tải file mẫu, điền theo cột (nhà cung cấp, chi nhánh, kho, sản phẩm, nhân viên...), tải lên; hệ thống kiểm tra và giữ lại dòng lỗi để bạn sửa.
