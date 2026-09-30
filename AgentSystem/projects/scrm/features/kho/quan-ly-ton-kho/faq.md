> Draft — cần bổ sung từ ticket khách thật.

# Hỏi đáp — Quản lý tồn kho

### 1. Tôi mở màn Quản lý tồn kho mà bảng trống, không có sản phẩm nào?
Bạn cần **chọn nhóm sản phẩm** (và kho) trước. Khi chưa chọn nhóm sản phẩm, hệ thống không tải dữ liệu nên bảng trống — đây là bình thường, không phải lỗi. Chọn nhóm xong dữ liệu sẽ hiện.

### 2. "Số lượng tồn" và "Có thể bán" khác nhau ở chỗ nào?
- **Số lượng tồn:** tổng hàng đang nằm trong kho.
- **Đang chờ xuất:** hàng đã được giữ chỗ cho các đơn chưa hoàn thành (chưa xuất kho thật).
- **Có thể bán = Tồn − Đang chờ xuất.**

Khi bán hàng, hệ thống nhìn số "có thể bán", nên đừng chỉ nhìn cột Tồn.

### 3. Tồn kho hiển thị sai so với thực tế, do đâu?
Thường gặp nhất là **còn phiếu chưa xác nhận**: phiếu nhập / xuất / chuyển kho / kiểm kho đang ở trạng thái nháp thì chưa cộng/trừ vào tồn. Hãy kiểm tra các phiếu liên quan đã xác nhận chưa. Nếu vẫn lệch, đối chiếu bằng phiếu **Kiểm kho**.

### 4. Vì sao tôi không thấy cột giá nhập / giá vốn?
Giá nhập trung bình có thể bị **ẩn theo cấu hình** với một số chi nhánh hoặc một số vai trò nhân viên (ví dụ không cùng chi nhánh tổng, hoặc vai trò nằm trong danh sách bị che giá). Đây là thiết lập có chủ đích, không phải lỗi. Nếu cần xem, liên hệ admin để chỉnh cấu hình.

### 5. Tôi muốn xem 1 sản phẩm còn bao nhiêu ở tất cả chi nhánh thì làm sao?
Dùng màn **Kiểm tra tồn kho**: chọn sản phẩm, hệ thống liệt kê tồn theo từng chi nhánh. Nếu đơn vị bật cấu hình giới hạn tồn kho tự động, còn có thêm **dự báo hết hàng** theo khoảng thời gian.

### 6. Nhân viên sale của tôi không nhìn thấy một kho?
Có thể kho đó được cấu hình **"ẩn kho với sale"**. Vào cấu hình kho / kho chi nhánh để bật/tắt tùy chọn này.

### 7. Vì sao có lúc bán được hàng dù tồn đang về 0 (hoặc âm)?
Nếu kho được bật **"cho phép chốt âm"**, hệ thống vẫn cho bán khi tồn không đủ (tồn sẽ xuống âm). Đây là cấu hình từng kho — nếu không muốn, tắt tùy chọn này.

### 8. Tôi xuất được dữ liệu tồn kho ra Excel không?
Có. Trên bảng tồn kho có nút **Xuất Excel**. Với báo cáo tồn kho theo đơn vị (nhiều bản ghi) hệ thống giới hạn mỗi lần xuất tối đa **10.000 bản ghi**, nhập vị trí bắt đầu và số lượng cần lấy.
