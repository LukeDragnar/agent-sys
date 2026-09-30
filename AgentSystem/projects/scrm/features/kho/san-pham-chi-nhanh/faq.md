# Câu hỏi thường gặp — Quản lý sản phẩm chi nhánh

> Draft — cần bổ sung từ ticket khách thật.

> Tóm tắt: [summarize.md](./summarize.md) · Luồng: [flow.md](./flow.md)

---

**1. "Quản lý sản phẩm chi nhánh" khác gì "Quản lý sản phẩm"?**
"Quản lý sản phẩm" là danh mục **cấp đơn vị** (dùng chung toàn hệ thống). "Quản lý sản phẩm chi nhánh" là danh mục **riêng của từng chi nhánh** — mỗi chi nhánh có bộ sản phẩm / dịch vụ / combo / thẻ của riêng mình.

**2. Tại sao tôi không thấy sản phẩm của chi nhánh khác?**
Đúng thiết kế. Mỗi lần đăng nhập bạn đang đứng ở **một chi nhánh**, và màn chi nhánh chỉ hiện món **do chi nhánh đó tạo và được gán cho chi nhánh đó**. Muốn xem món chi nhánh khác thì đăng nhập / chuyển sang chi nhánh đó.

**3. Có những loại danh mục chi nhánh nào?**
Bốn loại: **Sản phẩm chi nhánh**, **Dịch vụ chi nhánh**, **Combo chi nhánh**, **Thẻ (dịch vụ) chi nhánh**. Ngoài ra có mục **Gói mở rộng** *(tên gọi và phạm vi cụ thể cần product xác nhận)*.

**4. Tôi thêm dịch vụ chi nhánh nhưng danh sách trống?**
Kiểm tra đã **chọn nhóm dịch vụ** chưa. Danh sách bắt buộc lọc theo nhóm — chưa chọn nhóm sẽ không ra dữ liệu. Cũng kiểm tra dịch vụ đã được **gán cho chi nhánh** bạn đang đứng chưa.

**5. Thẻ chi nhánh khác thẻ dịch vụ thường ở đâu?**
Thẻ chi nhánh là **thẻ do chính chi nhánh tự tạo** ("thẻ dịch vụ chi nhánh tạo") — cấu hình giá bán, giá trị thẻ, số tiền khuyến mãi, thời hạn, số lần dùng theo nhu cầu chi nhánh đó.

**6. Combo chi nhánh có giới hạn số lượng theo chi nhánh không?**
Có. Combo có thể cấu hình **số lượng được bán giới hạn theo từng chi nhánh** (hoặc đặt không giới hạn). Nên mỗi chi nhánh có thể có hạn mức combo khác nhau.

**7. Tôi tắt "sử dụng" một dịch vụ chi nhánh thì điều gì xảy ra?**
Món đó ngừng được dùng (không cho chọn khi lên đơn / đặt lịch) nhưng **không bị xóa** — bật lại được. *(Hành vi chi tiết khi đang nằm trong đơn cũ: cần product xác nhận.)*

**8. Ai được quyền cấu hình danh mục chi nhánh?**
Theo nhóm quyền kho/dịch vụ của chi nhánh (ví dụ Admin chi nhánh, Quản lý dịch vụ, Quản lý kho). **Danh sách quyền cụ thể cần product xác nhận.**
