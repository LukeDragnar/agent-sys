> Draft — cần bổ sung từ ticket khách thật. Module Sản Xuất còn sơ khai (level 1), nhiều câu trả lời cần product xác nhận.

# FAQ — Quản lý sản xuất

**Q: Tôi muốn sản xuất một sản phẩm thì bắt đầu từ đâu?**
A: Bắt đầu từ **Đơn đặt hàng** (Sản xuất → Đơn đặt hàng): nhập sản phẩm và số lượng cần sản xuất. Sau đó tạo **Yêu cầu sản xuất (Production Request)**, rồi **tách thành Lệnh công việc (WO)**, cuối cùng chạy từng công đoạn của lệnh. Lưu ý: sản phẩm phải có **BOM (định mức)** trước thì mới tách được lệnh.

**Q: "Production Request" và "Work Order" khác nhau thế nào?**
A: **Production Request (PR)** là *yêu cầu* sản xuất — tổng hợp số lượng cần làm (đã trừ tồn kho). **Work Order (WO / lệnh công việc)** là *lệnh thực thi* được tách ra từ PR, gắn với một BOM cụ thể và chạy qua các công đoạn. Một PR có thể tách thành nhiều WO.

**Q: Tôi bấm tách lệnh nhưng báo "Đơn đã được tách"?**
A: Mỗi yêu cầu sản xuất chỉ tách thành lệnh **một lần**. Nếu đã tách rồi thì không tách lại được. Bạn xem các lệnh đã sinh ra ở màn **Lệnh công việc** hoặc **Lịch sử**.

**Q: Vì sao đưa lệnh sang Planner lại báo lỗi "đơn WO cũ"?**
A: Lệnh đó **chưa gắn BOM (định mức)**. Quy trình các công đoạn được dựng từ BOM, nên lệnh không có BOM sẽ không dựng được quy trình. Hãy đảm bảo khi tách lệnh đã chọn đúng BOM cho sản phẩm.

**Q: Số "cần sản xuất" được tính ra sao?**
A: Hệ thống lấy số lượng cần (theo đơn) **trừ đi tồn kho hiện có** để ra phần còn thiếu cần sản xuất. Vì vậy nếu tồn kho đã đủ, phần cần sản xuất có thể bằng 0.

**Q: Tôi lỡ xóa một lệnh sản xuất đã chạy, tồn kho có tự trả lại không?**
A: **Không.** Khi xóa lệnh, hệ thống không tự hủy các phiếu xuất/nhập kho đã tạo trước đó, nên tồn kho có thể bị lệch. Nếu đã xóa nhầm, hãy liên hệ bộ phận kho/hỗ trợ để xử lý phiếu thủ công.

**Q: Tôi không hoàn thành được một công đoạn dù đã chạy xong?**
A: Để hoàn thành một bước, **mọi sản phẩm (nguyên liệu đầu vào và thành phẩm đầu ra) của bước đó phải đã được tạo phiếu**. Nếu còn sản phẩm chưa tạo phiếu xuất/nhập, hệ thống sẽ chặn và nhắc "Vui lòng tạo các phiếu".

**Q: "Lock sản phẩm" trên đơn đặt hàng để làm gì?**
A: Đó là **giữ chỗ** số lượng sản phẩm trong kho cho đơn sản xuất, tránh việc số lượng đó bị dùng cho việc khác trước khi bạn sản xuất xong. Lock chỉ ghi nhận nội bộ trong module sản xuất.

**Q: Có "Production Order" và "Yêu cầu sản xuất", cái nào mới đúng?**
A: Luồng đang dùng thực tế là **Yêu cầu sản xuất → Work Order**. Màn "Production Order" là một danh mục riêng, nghi là bản cũ/song song — cần bộ phận sản phẩm xác nhận có còn dùng không.
