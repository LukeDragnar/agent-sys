# FAQ — Quản lý nhà cung cấp

> Draft — cần bổ sung từ ticket khách thật.

**Q1. Thông tin tối thiểu để thêm một nhà cung cấp là gì?**
Bắt buộc: **Tên nhà cung cấp**, **Mã nhà cung cấp**, **Số điện thoại**. Các thông tin còn lại (địa chỉ, email, ngân hàng, công nợ, nhóm...) là tùy chọn.

**Q2. Báo "Mã nhà cung cấp đã tồn tại" thì làm sao?**
Mã NCC phải là duy nhất trong cửa hàng. Đổi sang mã khác chưa dùng. Nếu mã bị trùng với một **khách hàng** đã có, hệ thống sẽ hỏi xác nhận để dùng chung làm đối tượng "vừa là NCC vừa là KH".

**Q3. Tại sao tôi không xóa được nhà cung cấp?**
Vì NCC đó đã có **phiếu mua hàng** liên quan hoặc đang gắn với một **khách hàng**. Hệ thống chặn xóa để giữ toàn vẹn dữ liệu/công nợ. Nếu không dùng nữa, hãy **tắt "Sử dụng"** thay vì xóa.

**Q4. "Đồng thời là khách hàng" để làm gì?**
Khi một đối tác vừa bán hàng cho bạn vừa mua hàng của bạn. Bật tùy chọn này thì hệ thống tạo/đồng bộ thông tin sang module Khách hàng dùng chung mã, tiện đối trừ công nợ.

**Q5. "Số ngày được nợ" và "Số ngày nợ tối đa" khác nhau thế nào?**
"Số ngày được nợ" là hạn mặc định cho NCC này; "Số ngày nợ tối đa" là trần. Yêu cầu **nợ tối đa ≥ được nợ**, ngược lại sẽ báo lỗi khi lưu.

**Q6. Nhóm nhà cung cấp dùng để làm gì? Tạo ở đâu?**
Để phân loại NCC (ví dụ: nhóm vật tư, nhóm điện máy...) phục vụ lọc và báo cáo. Tạo ở màn hình **Nhóm nhà cung cấp** riêng (chỉ Admin tổng được thêm/sửa/xóa nhóm), sau đó chọn nhóm khi thêm NCC.

**Q7. Có thể nhập danh sách NCC hàng loạt không?**
Có. Tải **file mẫu Excel** từ chức năng Import, điền dữ liệu rồi upload. Dòng sai có thể tải về file lỗi để sửa lại. Ngoài ra có **Xuất Excel** danh sách hiện tại.

**Q8. Vì sao có nhà cung cấp không cho sửa?**
Đó là NCC được **sinh tự động** (ví dụ từ đơn sàn TMĐT). Loại này hệ thống khóa chỉnh sửa để tránh lệch dữ liệu đồng bộ.
