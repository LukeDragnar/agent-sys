> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý khuyến mãi

**Q: Tôi tạo khuyến mãi rồi nhưng khi tính tiền không thấy áp giảm giá, tại sao?**
A: Kiểm tra: (1) đơn có nằm trong **thời gian áp dụng** và **khung giờ vàng** không; (2) giỏ hàng đã đạt **ngưỡng số lượng/giá trị tối thiểu** chưa; (3) khách có thuộc **đối tượng** (hạng/nhóm) được hưởng không; (4) chương trình còn lượt không; (5) sản phẩm còn tồn / không bị tạm ngừng. Nếu vừa sửa KM mà chưa áp, đợi hệ thống làm mới rồi thử lại.

**Q: Vì sao tôi không thấy chương trình khuyến mãi mà đồng nghiệp thấy?**
A: Nếu bạn không phải admin tổng đơn vị, bạn chỉ thấy KM của **chi nhánh mình**. Admin tổng thấy tất cả. Đây là thiết lập phân quyền, không phải lỗi.

**Q: Tôi đặt "giới hạn số lượt" cho chương trình nhưng KM không bao giờ áp được?**
A: Khi bật giới hạn lượt, bạn **phải nhập số lượt cụ thể**. Nếu để trống, hệ thống hiểu là số lượt bằng 0 nên coi như đã hết lượt ngay từ đầu. Hãy nhập tổng số lượt mong muốn.

**Q: Khách hủy đơn đã áp khuyến mãi thì lượt khuyến mãi có được trả lại không?**
A: Có. Khi hủy/hoàn đơn, hệ thống hoàn lại lượt đã trừ cho chương trình và cho khách. Nếu lượt không được hoàn (rất hiếm, do lỗi), hãy báo bộ phận hỗ trợ kiểm tra.

**Q: Khách vãng lai (chưa có hồ sơ) có được hưởng khuyến mãi không?**
A: Tùy chương trình. Các KM lọc theo hạng/nhóm khách thường không áp cho khách vãng lai. Muốn áp cho mọi người, hãy đặt đối tượng là "tất cả" thay vì theo hạng/nhóm.

**Q: Khuyến mãi sản phẩm và khuyến mãi đơn hàng khác nhau thế nào?**
A: KM sản phẩm áp theo từng mặt hàng đủ điều kiện (số lượng SP, giá SP); KM đơn hàng áp theo tổng giỏ hàng (tổng giá trị đơn). Khi tạo chương trình bạn chọn loại tương ứng, các điều kiện cấu hình sẽ khác nhau theo loại.

**Q: Tôi muốn giới hạn mỗi khách chỉ dùng 1 lần thì làm sao?**
A: Trong cấu hình chương trình có mục "số lần mỗi khách". Đặt = 1 thì mỗi khách chỉ được áp một lần; hệ thống đếm theo lịch sử áp của khách.

**Q: Có thể nhập nhiều chương trình khuyến mãi một lần từ Excel không?**
A: Được. Màn Khuyến mãi có chức năng import: tải file mẫu, điền theo cột, tải lên. Hệ thống báo các dòng lỗi để bạn tải về sửa lại. Chức năng này dành cho admin tổng đơn vị.
