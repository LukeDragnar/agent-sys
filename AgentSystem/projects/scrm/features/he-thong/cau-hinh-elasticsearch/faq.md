> Draft — cần bổ sung từ ticket vận hành thật.

# FAQ — Cấu hình Elasticsearch

**Q: Khách báo "tìm khách/đơn không ra" dù dữ liệu chắc chắn có, xử lý sao?**
A: Khả năng cao do dữ liệu tìm kiếm (index) của đơn vị đó bị lệch/chưa cập nhật. Vào Quản lý Elastic search, chọn đúng đơn vị và loại dữ liệu (contact/đơn/sản phẩm…), bấm Reindex để dựng lại. Đợi vài phút cho việc chạy nền xong rồi thử tìm lại.

**Q: Bấm Reindex xong sao tìm vẫn chưa thấy đổi?**
A: Reindex chạy nền qua hàng đợi, không cập nhật tức thì. Tùy lượng dữ liệu có thể mất từ vài giây đến vài phút. Đừng bấm lại liên tục — chờ một lúc rồi kiểm tra lại.

**Q: Nếu để trống danh sách đơn vị khi reindex thì sao?**
A: Sẽ áp cho **tất cả đơn vị**. Hãy cân nhắc, vì reindex các loại dữ liệu lớn (đơn hàng, sản phẩm) cho toàn hệ thống rất tốn tài nguyên. Khi chỉ cần xử lý 1 shop thì hãy chọn đúng đơn vị đó.

**Q: Tôi bấm "Reindex toàn bộ" mà bị từ chối?**
A: Lệnh reindex toàn bộ tất cả đơn vị (full) chỉ tài khoản SuperUser mới chạy được. Tài khoản admin hệ thống thường sẽ bị chặn. Hãy nhờ tài khoản có quyền SuperUser thực hiện.

**Q: Reindex hay xóa index có làm mất dữ liệu khách hàng/đơn hàng không?**
A: Không. Đây chỉ là bản sao phục vụ tìm kiếm nhanh. Reindex là dựng lại bản sao từ dữ liệu gốc; xóa index chỉ xóa bản sao tìm kiếm. Dữ liệu thật (khách, đơn, sản phẩm) trong hệ thống vẫn nguyên.

**Q: Khi nào cần xóa index?**
A: Khi muốn dọn sạch dữ liệu tìm kiếm của một đơn vị, ví dụ đơn vị ngừng dùng ES hoặc muốn dựng lại từ con số 0. Sau khi xóa, nếu vẫn muốn dùng tìm kiếm thì reindex lại.

**Q: Đánh lại "sản phẩm" có cần đánh thêm gì không?**
A: Không cần. Khi reindex sản phẩm, hệ thống tự đánh lại cả phần sản phẩm cha trong cùng một lần, nên bạn chỉ cần chọn loại "sản phẩm" là đủ.

**Q: Khách hàng (chủ shop) có tự vào cấu hình ES được không?**
A: Không. Đây là chức năng nội bộ của đội vận hành SaaS. Khách hàng chỉ dùng kết quả tìm kiếm, không thấy và không cấu hình màn này.
