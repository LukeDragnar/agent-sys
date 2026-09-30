# Quản lý sản phẩm — Câu hỏi thường gặp

> Draft — cần bổ sung từ ticket khách thật.

> Dành cho khách dùng SCRM + nuôi chatbot. Không chứa thuật ngữ kỹ thuật/code.

---

**Q: Tôi không thêm được sản phẩm, hệ thống báo lỗi?**
A: Kiểm tra lại các thông tin bắt buộc: **Tên**, **Ảnh**, **Giá**, **Nhóm sản phẩm** và **Khu vực kho hàng (chi nhánh)**. Thiếu một trong số này sẽ không lưu được. Ngoài ra, **Tên / Mã / Barcode không được trùng** với sản phẩm đã có.

**Q: Tại sao bắt buộc phải chọn nhóm sản phẩm?**
A: Sản phẩm và dịch vụ phải nằm trong một nhóm (danh mục) để phân loại, tìm kiếm và báo cáo. Nếu chưa có nhóm phù hợp, hãy vào **Danh mục sản phẩm** tạo nhóm trước, rồi quay lại thêm sản phẩm.

**Q: Sản phẩm vừa tạo nhưng không thấy khi bán hàng / không hiện trên website?**
A: Kiểm tra: (1) đã bật **"Sử dụng"** chưa, (2) đã chọn **đúng chi nhánh** đang bán chưa, (3) còn hàng trong kho không. Sản phẩm bị tắt sử dụng hoặc không thuộc chi nhánh đó sẽ không hiển thị.

**Q: Tôi muốn ngừng bán một sản phẩm nhưng giữ lại lịch sử, nên làm gì?**
A: **Tắt "Sử dụng"** thay vì xóa. Xóa là xóa vĩnh viễn, không khôi phục được. Tắt sử dụng thì sản phẩm ẩn khỏi bán hàng nhưng dữ liệu cũ vẫn còn.

**Q: Tôi không xóa được sản phẩm, vì sao?**
A: Sản phẩm đang được dùng ở nơi khác sẽ bị chặn xóa — ví dụ đang nằm trong **combo**, trong **sản phẩm cấu thành**, hoặc trong **dịch vụ**. Hãy gỡ sản phẩm khỏi các nơi đó trước rồi mới xóa được.

**Q: Tôi không sửa được đơn vị tính của sản phẩm?**
A: Khi sản phẩm **đã có tồn kho** hoặc **đã bán trong đơn hàng**, hệ thống khóa việc đổi đơn vị tính để tránh sai lệch số liệu kho. Hãy khai báo đơn vị / bảng quy đổi cho đúng ngay khi tạo sản phẩm.

**Q: Combo của tôi không xóa được?**
A: Combo **đã bán cho khách hàng** sẽ không xóa được (để giữ lịch sử mua của khách). Nếu chỉ muốn ngừng bán, hãy **tắt sử dụng** combo đó.

**Q: Sự khác nhau giữa Thẻ số tiền, Thẻ số lần và Thẻ lịch?**
A: **Thẻ số tiền** = thẻ nạp tiền trả trước, dùng dần theo giá trị. **Thẻ số lần** = mua trước số lần dùng dịch vụ (bắt buộc nhập số lần > 0). **Thẻ lịch** = loại đặc biệt, mỗi đơn vị **chỉ tạo được 1 thẻ lịch**, không giới hạn số lần và không cần thời hạn.

**Q: Dịch vụ spa tính tiền theo giờ thì khai báo thế nào?**
A: Khi thêm dịch vụ, bật **"Tính giờ"**. Nếu tính theo block thời gian, bật thêm **"Tính theo block"** rồi nhập **số phút mỗi block** và **giá mỗi block**. Có thể đặt **giá theo khung giờ** khác nhau (theo ngày trong tuần / khoảng giờ) — lưu ý các khung giờ không được đè (chồng) lên nhau.

**Q: Nhãn sản phẩm dùng để làm gì?**
A: Nhãn là tag có màu để gắn lên sản phẩm cho dễ nhận biết/phân loại nhanh (ví dụ "Bán chạy", "Sắp hết"). Mỗi nhân viên thường chỉ sửa/xóa được nhãn do mình tạo, trừ tài khoản quản trị tổng.
