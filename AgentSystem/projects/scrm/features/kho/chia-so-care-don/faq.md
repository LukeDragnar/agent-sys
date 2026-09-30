# FAQ — Chia số care đơn

> Draft — cần bổ sung từ ticket khách thật.

**Q: "Chia số care đơn" là gì, khác gì với chia số cho sale?**
A: Đây là việc giao **đơn hàng** cho nhân viên kho / chăm sóc đơn (care đơn) phụ trách xử lý. Nó nằm ở module Kho và áp dụng cho đơn hàng logistic, khác với việc chia khách/lead cho nhân viên sale.

**Q: Tại sao đơn không tự được chia cho ai?**
A: Thường do một trong các nguyên nhân: (1) đơn vị chưa bật tính năng chia số care đơn; (2) trạng thái hiện tại của đơn không nằm trong các trạng thái được cấu hình chia; (3) chưa tạo cấu hình care đơn cho chi nhánh; (4) đơn này đã có người/nhóm care từ trước nên hệ thống không chia lại; (5) tất cả nhóm/người đã đủ định mức trong ngày. Vào màn cấu hình kiểm tra lần lượt các mục trên.

**Q: Một đơn có thể được chia lại cho người khác không?**
A: Hệ thống **không tự chia lại**. Khi đơn đã có nhóm hoặc người care, chia tự động sẽ bỏ qua nó. Muốn đổi người phụ trách phải thao tác thủ công ở màn chia số care đơn thủ công (hoặc các nghiệp vụ liên quan), không phải đợi hệ thống tự đổi.

**Q: Chia tự động và chia thủ công khác nhau thế nào?**
A: Chia **tự động** chạy ngầm: đơn vừa đạt trạng thái đã cấu hình là hệ thống tự gắn người care theo luật và định mức. Chia **thủ công** là quản lý tự vào lọc các đơn chưa có người care rồi tự tay phân về nhóm hoặc về người. Hai cách bổ trợ nhau — tự động lo phần lớn, thủ công xử lý phần còn sót hoặc trường hợp đặc biệt.

**Q: "Định mức chia số" có tác dụng gì?**
A: Là số đơn tối đa một nhóm/người được nhận trong ngày. Khi đã đủ định mức, đối tượng đó bị loại khỏi lượt chia cho tới khi sang ngày mới. Hệ thống ưu tiên giao đơn cho đối tượng **ít đơn nhất / lâu chưa nhận nhất** để chia đều.

**Q: "Chia sau X phút" nghĩa là gì?**
A: Ở cấu hình trạng thái giao hàng, mỗi trạng thái có thể đặt "chia sau bao nhiêu phút". Nếu để 0 (hoặc không đặt) thì đơn được chia ngay khi đạt trạng thái; nếu đặt X phút thì đơn được xếp hàng chờ, sau X phút mới chia (dùng để tránh chia vội khi trạng thái còn có thể thay đổi).

**Q: Bật/tắt "nhận dữ liệu" cho một tài khoản để làm gì?**
A: Để bật/tắt việc một nhân viên kho có được nhận đơn care hay không — ví dụ nhân viên nghỉ phép thì tắt, hệ thống sẽ ngừng giao đơn mới cho họ. Lưu ý chỉ ảnh hưởng các đơn **chia về sau**, không thu hồi đơn đã giao trước đó.

**Q: Chia theo sản phẩm hoạt động ra sao?**
A: Khi bật "chia theo sản phẩm", mỗi nhóm/người được gán danh sách sản phẩm phụ trách (hoặc chọn tất cả). Đơn chỉ được chia về nhóm/người có sản phẩm khớp với sản phẩm trong đơn — giúp đơn chứa sản phẩm chuyên môn về đúng người am hiểu sản phẩm đó.

**Q: Đơn vị dùng kho tổng thì chia thế nào?**
A: Khi bật kho tổng cho care đơn, đơn của nhiều chi nhánh được gom về **cấu hình của chi nhánh tổng**, và danh sách người nhận lấy từ tất cả chi nhánh. Nếu bật kho tổng nhưng chưa khai báo chi nhánh tổng thì việc chia sẽ dừng và ghi log lỗi.
