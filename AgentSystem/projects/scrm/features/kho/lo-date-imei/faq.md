# FAQ — Quản lý lô date & Imei

> Draft — cần bổ sung từ ticket khách thật.

---

**Q: Lô date với IMEI khác gì nhau? Khi nào dùng cái nào?**
A: Dùng **IMEI/số seri** khi mỗi sản phẩm là một mã riêng và bạn cần biết *từng cái* ở đâu, bán cho ai (điện thoại, đồ điện tử, hàng bảo hành). Dùng **lô date** khi hàng cùng loại đi theo lô và quan trọng là **hạn sử dụng** (mỹ phẩm, thực phẩm chức năng, thuốc). Một số sản phẩm có thể bật cả hai.

**Q: Tôi muốn quản lý hạn sử dụng / IMEI thì bắt đầu từ đâu?**
A: Vào **Quản lý sản phẩm**, mở sản phẩm cần quản lý và tick cờ **"Sản phẩm quản lý IMEI"** hoặc **"Sản phẩm quản lý lô date"**. Sau đó mỗi lần nhập kho sản phẩm này, hệ thống sẽ yêu cầu nhập IMEI hoặc thông tin lô (lô SX, ngày SX, hạn dùng).

**Q: "Sinh lô date tự động" / "Sinh IMEI tự động" để làm gì?**
A: Khi hàng của bạn **chưa có sẵn mã IMEI/mã lô** in trên bao bì, hệ thống tự tạo dãy mã cho bạn theo **tiền tố** bạn cấu hình, kèm ngày tạo và số thứ tự tự tăng. Tạo xong có thể **in tem** để dán lên hàng.

**Q: Tại sao nhập tiền tố mà báo lỗi?**
A: Tiền tố **chỉ được là số và tối đa 2 ký tự**. Nhập chữ hoặc dài hơn 2 ký tự sẽ bị chặn.

**Q: Tại sao báo "Ngày hết hạn phải lớn hơn ngày sản xuất"?**
A: Hệ thống không cho hạn sử dụng nhỏ hơn hoặc bằng ngày sản xuất. Kiểm tra lại hai ngày này.

**Q: Báo "Lô sản xuất đã tồn tại" thì sao?**
A: Mã lô sản xuất đã được dùng trong đơn vị của bạn. Đặt mã lô khác, hoặc (nếu thực sự cần dùng lại) chọn tùy chọn bỏ qua kiểm tra trùng lô.

**Q: Tôi muốn xóa một lô date nhưng không xóa được?**
A: Nếu lô date đã gắn với **phiếu nhập đã hoàn thành** thì không xóa được — cần hủy hoặc sửa phiếu nhập trước rồi mới xóa.

**Q: Tra một mã IMEI để biết nó đang ở đâu / đã bán chưa thì làm sao?**
A: Vào màn **Danh sách / Quản lý IMEI**, nhập (hoặc quét) mã IMEI để kiểm tra. Hệ thống cho biết IMEI đang ở trạng thái **Chờ nhập kho / Đã nhập kho / Đã xuất kho / Đang chuyển kho**, thuộc kho nào và sản phẩm nào.

**Q: Đến hạn cần ưu tiên bán lô sắp hết hạn thì xem ở đâu?**
A: Vào **Báo cáo sản phẩm lô date**, lọc theo **Ngày hết hạn sản phẩm** để thấy lô nào còn tồn và sắp hết hạn, ưu tiên bán/đẩy hàng trước.

**Q: In tem IMEI / lô date có được không?**
A: Được. Trong màn Sinh IMEI / Sinh lô date, chọn các mã rồi bấm **In** (có thể cấu hình mẫu in). Mã đã in sẽ chuyển trạng thái "Đã in".
