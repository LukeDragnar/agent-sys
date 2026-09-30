> Draft — cần bổ sung từ ticket thật.

# FAQ — RollBack & xử lý đơn

**Q: "Danh sách đơn hàng lỗi" là gì, có phải đơn bị lỗi của khách không?**
A: Không phải lỗi của khách. Đây là danh sách các đơn đang được **hoàn tác (rollback)** sau khi bị hủy, hủy thanh toán hoặc trả khách. Hệ thống cần trả ngược lại tồn kho, công nợ, điểm, khuyến mãi… nên ghi nhận đơn vào danh sách này để theo dõi quá trình hoàn tác.

**Q: Các cột CustomerSuccess, WarehouseProductSuccess… nghĩa là gì?**
A: Mỗi cột là một phân hệ cần hoàn tác. Có dấu ✔ nghĩa là phân hệ đó đã hoàn tác xong (ví dụ đã trả lại tồn kho, đã hoàn công nợ khách). Cột trống nghĩa là phân hệ đó **chưa hoàn tác xong** — đang chờ hệ thống xử lý hoặc đang bị lỗi.

**Q: Tôi muốn tự bấm "rollback lại" một đơn bị kẹt thì làm sao?**
A: Màn này chỉ để **xem/theo dõi**, không có nút thao tác. Việc hoàn tác do **job nền tự động** chạy và tự thử lại. Nếu một đơn kẹt lâu, hãy báo bộ phận kỹ thuật để kiểm tra job và log của phân hệ đang lỗi.

**Q: Đơn đã hủy rồi mà sao kho vẫn chưa thấy cộng lại hàng?**
A: Vì rollback chạy **từng phân hệ một** và có thể trễ. Hãy mở danh sách đơn hàng lỗi, tìm đúng đơn, xem cột WarehouseProductSuccess: nếu chưa có ✔ thì phần hoàn kho chưa chạy xong. Đợi job xử lý; nếu kẹt lâu thì báo kỹ thuật.

**Q: Vì sao một đơn đã hoàn được kho nhưng chưa hoàn được công nợ?**
A: Mỗi phân hệ hoàn tác độc lập, một phân hệ lỗi không chặn phân hệ khác. Đó là lý do bạn có thể thấy đơn hoàn xong phần này nhưng chưa phần kia. Hệ thống sẽ tự thử lại phần còn thiếu.

**Q: "Kiểu lỗi: Hủy đơn" và "Thanh toán đơn" khác nhau gì?**
A: Đó là lý do phát sinh rollback: "Hủy đơn" là đơn bị hủy nên phải hoàn tác toàn bộ; "Thanh toán đơn" liên quan tới hoàn tác phần thanh toán. Bạn có thể lọc theo từng kiểu để khoanh vùng.

**Q: Khách hàng có nhìn thấy màn này không?**
A: Không. Đây là màn quản trị nội bộ (super-admin SaaS), chỉ admin hệ thống truy cập được.
