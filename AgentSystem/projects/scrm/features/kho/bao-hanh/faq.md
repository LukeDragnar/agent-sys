# Quản lý bảo hành — Câu hỏi thường gặp

> Draft — cần bổ sung từ ticket khách thật.

> ⚠️ Nhiều câu trả lời dưới đây là **nghiệp vụ kỳ vọng**, vì màn hình quản lý bảo hành **chưa tìm thấy implement trong source** (mới có field hạn/ngày kích hoạt bảo hành trên đơn/sản phẩm). Cần product xác nhận trước khi dùng nuôi chatbot.

> Tóm tắt: [summarize.md](./summarize.md) · Luồng: [flow.md](./flow.md) · Kỹ thuật: [dev.md](./dev.md)

---

**Q: Quản lý bảo hành trong SCRM dùng để làm gì?**
A: Để theo dõi và xử lý các yêu cầu bảo hành cho sản phẩm khách đã mua — tiếp nhận máy lỗi, tra hạn bảo hành, rồi xử lý theo hướng sửa chữa / đổi mới / có phí / miễn phí (FOC) hoặc từ chối. *(Phạm vi màn hình cụ thể cần product xác nhận.)*

**Q: Hạn bảo hành tính từ lúc nào — ngày mua hay ngày kích hoạt?**
A: Hệ thống lưu **ngày kích hoạt bảo hành** (`NgayKichHoatBaoHanh`) và **hạn bảo hành** (`HanBaoHanh`) riêng. Về nguyên tắc hạn được tính theo ngày kích hoạt, không phải ngày mua. *(Cách tính chính xác và việc kích hoạt tự động/thủ công cần product xác nhận.)*

**Q: Tra một sản phẩm còn bảo hành hay không bằng cách nào?**
A: Tra theo **mã IMEI/serial** của máy hoặc theo **đơn hàng đã bán** chứa dòng sản phẩm đó; hệ thống hiển thị hạn bảo hành còn lại trên dòng sản phẩm của đơn. *(Màn hình tra cứu bảo hành riêng chưa xác nhận có hay không.)*

**Q: Còn trong hạn bảo hành thì sửa có mất phí không?**
A: Không phải lúc nào cũng miễn phí. Hệ thống phân biệt **bảo hành không tính phí (FOC)** và **bảo hành có tính phí** (dịch vụ sửa chữa, bán linh kiện). Lỗi thuộc trách nhiệm nhà sản xuất/cửa hàng thường FOC; lỗi do người dùng có thể tính phí. *(Chính sách cụ thể do từng đơn vị cấu hình — cần product xác nhận.)*

**Q: Đổi máy mới cho khách trong bảo hành thì ghi nhận thế nào?**
A: Đây là một nghiệp vụ kho. Hệ thống có các lý do xuất/nhập như **"Xuất đổi mới trong CS bảo hành"** và **"Trả hàng nhận bảo hành"** để ghi nhận máy cũ vào và máy mới ra. Cần lập phiếu kho tương ứng để tồn kho đúng. *(Quy trình lập phiếu cụ thể cần product xác nhận.)*

**Q: Hết hạn bảo hành rồi có nhận sửa không?**
A: Có thể nhận theo diện **bảo hành tính phí** (sửa chữa, bán linh kiện) — đây là một nhóm lý do riêng trong hệ thống, tách khỏi bảo hành miễn phí. *(Có cho tiếp nhận trên màn bảo hành hay không cần product xác nhận.)*

**Q: Sản phẩm có IMEI thì bảo hành theo gì?**
A: Theo **từng mã IMEI** — mỗi máy là một mã riêng. Khi tiếp nhận hoặc đổi máy phải đúng IMEI để không nhầm sang máy khác.

**Q: SCRM đã có màn hình "Danh sách bảo hành" và "Xử lý bảo hành" chưa?**
A: ⚠️ Ở thời điểm tài liệu này, **chưa tìm thấy hai màn hình đó trong source code**. Bảo hành hiện được lưu ở dạng **thuộc tính** (hạn bảo hành, ngày kích hoạt) trên đơn hàng/sản phẩm, cùng vài lý do xuất kho liên quan. Việc có module quản lý bảo hành độc lập hay chưa **cần product xác nhận**.
