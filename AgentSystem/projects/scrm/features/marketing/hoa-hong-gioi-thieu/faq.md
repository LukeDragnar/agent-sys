> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Hoa hồng giới thiệu

**Q: Tôi đã bán đơn có gắn CTV nhưng vào thống kê không thấy hoa hồng?**
A: Thống kê hoa hồng chỉ hiển thị khi đơn đã được **đẩy vào pipeline tính hoa hồng**. Nếu đơn vừa bán mà chưa xử lý xong, hoặc bước đẩy bị lỗi, báo cáo sẽ trống. Đợi đơn được xử lý hoặc kiểm tra lại với bộ phận kỹ thuật nếu mãi không lên số.

**Q: Hoa hồng của CTV được tính theo gì?**
A: Theo **nhóm hoa hồng** mà CTV được gán. Nhóm có thể cấu hình hoa hồng theo sản phẩm (% theo từng SP), theo bậc doanh số (đạt mốc nào hưởng %), hoặc theo chiến dịch. Mỗi CTV thuộc 1 nhóm nên nhóm quyết định cách tính.

**Q: Tôi đổi % hoa hồng của một nhóm thì có ảnh hưởng CTV nào?**
A: Tất cả CTV đang gán nhóm đó đều bị ảnh hưởng theo cấu hình mới. Nếu chỉ muốn đổi cho một số CTV, hãy tạo nhóm riêng và gán lại.

**Q: Tôi đã cấu hình hoa hồng chiến dịch nhưng đơn không phát sinh hoa hồng chiến dịch?**
A: Kiểm tra nhóm hoa hồng của CTV đó có **bật cờ "dùng hoa hồng chiến dịch"** không. Nếu tắt thì dù chiến dịch có cấu hình, đơn cũng không phát sinh hoa hồng chiến dịch.

**Q: Có nhập nhiều CTV / nhóm hoa hồng một lần từ Excel không?**
A: Có. Cả màn Cộng tác viên và Nhóm hoa hồng đều có chức năng import Excel: tải file mẫu, điền, tải lên. Hệ thống báo dòng lỗi để tải về sửa.

**Q: Tôi muốn tạo một nhóm mới giống hệt nhóm cũ?**
A: Dùng chức năng **Sao chép** ở màn Nhóm hoa hồng để nhân bản cấu hình sang nhóm mới, rồi chỉnh lại phần khác biệt. Ngoài ra có chức năng **Áp dụng** để áp cấu hình theo phân loại nhóm.

**Q: Xóa một nhóm hoa hồng đang có CTV thì sao?**
A: Nên gỡ/chuyển CTV sang nhóm khác trước. Xóa nhóm sẽ dọn cấu hình con của nhóm; nếu còn CTV gán nhóm vừa xóa, các CTV đó sẽ mất cấu hình hoa hồng tương ứng.

**Q: CTV có bắt buộc là nhân viên không?**
A: Không bắt buộc. CTV có thể là người ngoài hoặc là nhân viên (kiểu loại "nhân viên" gắn IdUser). Tùy cách khai báo khi thêm CTV.
