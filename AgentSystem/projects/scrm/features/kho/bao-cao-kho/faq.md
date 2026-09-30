# FAQ — Báo cáo kho

> Draft — cần bổ sung từ ticket khách thật.

### 1. Có xuất Excel được không?
Có. Các báo cáo nhập–xuất kho, xuất kho, tồn kho, sản phẩm bán theo ngày đều có nút **Xuất Excel**, tải về đúng bộ lọc đang xem. Một vài báo cáo phụ (ví dụ vòng quay) có thể chưa có nút xuất — khi đó dùng báo cáo gần nhất hoặc liên hệ hỗ trợ.

### 2. Số liệu báo cáo bị lệch so với thực tế, do đâu?
Thường gặp:
- **Đơn vị tính khác nhau:** hàng nhập theo thùng, bán theo lon — báo cáo quy đổi về đơn vị hiện tại, nếu bảng quy đổi cấu hình sai thì số sẽ lệch.
- **Sai khoảng thời gian / chi nhánh:** đang lọc nhầm kỳ hoặc nhầm chi nhánh.
- **Phát sinh sau thời điểm xem:** báo cáo là ảnh chụp tại lúc bấm xem; có nhập/xuất/bán mới thì chạy lại sẽ khác.
- **Nghiệp vụ gốc chưa hoàn tất:** phiếu nhập/xuất chưa duyệt, đơn hàng chưa chốt → chưa vào báo cáo.

### 3. Vì sao báo cáo tồn kho không hỏi khoảng thời gian?
Vì tồn kho là số **đang còn tại thời điểm hiện tại**, không tính theo kỳ. Còn nhập/xuất/sản phẩm bán mới cần chọn từ ngày – đến ngày.

### 4. Báo "Chỉ được xem dữ liệu trong khoảng N ngày" là sao?
Đơn vị có cấu hình giới hạn độ dài khoảng thời gian truy vấn (để tránh kéo dữ liệu quá lớn). Hãy thu hẹp khoảng từ ngày – đến ngày cho nằm trong giới hạn rồi xem lại.

### 5. Báo "Chưa đến lượt truy cập" là lỗi gì?
Không phải lỗi. Báo cáo kho nặng giới hạn số người chạy cùng lúc; khi đang đông, bạn được đưa vào hàng chờ. Đợi một lát rồi bấm xem lại.

### 6. Tôi chọn chi nhánh khác nhưng vẫn ra dữ liệu chi nhánh mình, vì sao?
Nếu tài khoản không phải admin/super-user, hệ thống tự giới hạn về chi nhánh bạn được gán, kể cả khi bạn chọn chi nhánh khác. Cần xem chi nhánh khác thì liên hệ quản trị để cấp quyền phù hợp.

### 7. "Báo cáo xuất kho" và "Báo cáo xuất kho theo nhân viên" khác gì nhau?
Cùng dữ liệu xuất kho, nhưng bản "theo nhân viên" **gộp theo người thực hiện xuất** để biết ai xuất hàng nào, bao nhiêu. Đây là cách xem (group) của cùng một báo cáo xuất kho.

### 8. "Quay vòng sản phẩm" cho biết điều gì?
Cho biết hàng luân chuyển nhanh hay chậm: vòng quay, số ngày tồn trung bình, và phân loại sản phẩm **bán chạy / trung bình / bán kém**. Dùng để phát hiện hàng tồn đọng nên ngừng nhập hoặc đẩy bán.
