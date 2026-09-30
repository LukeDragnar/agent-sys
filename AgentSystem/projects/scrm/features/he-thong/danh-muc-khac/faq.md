> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Danh mục khác (gốc — cấp hệ thống)

**Q: Tôi sửa một nguồn đặt lịch ở cấp hệ thống thì các shop đã dùng nguồn đó có tự cập nhật không?**
A: Không. Nguồn đặt lịch cấp hệ thống chỉ là **template**. Khi đơn vị "Khởi tạo mặc định" thì nó **copy** nguồn về đơn vị. Sửa/xóa ở cấp hệ thống sau đó **không** tự đồng bộ ngược về các đơn vị đã copy.

**Q: Tôi không xóa được nguồn "Trực tiếp" hoặc "Website"?**
A: Đúng. Hai nguồn lõi có Mã `TRUCTIEP` và `WEBSITE` bị **chặn xóa** vì là nguồn hệ thống quan trọng. Bạn chỉ có thể sửa thông tin hiển thị, không xóa.

**Q: "Đặt mặc định" cho nguồn đặt lịch hoạt động thế nào?**
A: Tại một thời điểm chỉ có **một** nguồn đặt lịch hệ thống được đặt mặc định. Khi bạn đặt một nguồn làm mặc định, nguồn mặc định cũ sẽ tự bị bỏ cờ.

**Q: Lĩnh vực và ngành nghề khác nhau thế nào?**
A: Đây là các danh mục phân loại cho đơn vị. Quan trọng nhất: **Lĩnh vực** còn gắn với cấu hình **bộ tiện ích (tính năng)** — quyết định đơn vị thuộc lĩnh vực đó được dùng những tính năng nào. Ngành nghề dùng để khởi tạo dữ liệu mẫu theo ngành.

**Q: Tôi gán/bỏ tiện ích cho một lĩnh vực thì đơn vị thuộc lĩnh vực đó bị ảnh hưởng ngay không?**
A: Cấu hình tiện ích theo lĩnh vực quyết định bộ tính năng khả dụng cho đơn vị thuộc lĩnh vực. Hãy cẩn thận vì thay đổi này tác động tới những gì đơn vị có thể dùng. Cụ thể có hiệu lực tức thì hay cần đồng bộ — *cần product/đội kỹ thuật xác nhận*.

**Q: Tôi cần quyền gì để vào các màn danh mục này?**
A: Chủ yếu cần quyền quản trị hệ thống (HT-AD). Lưu ý một vài thao tác xem dữ liệu (xem chi tiết, tìm theo điều kiện của Lĩnh vực; toàn bộ Nhóm ngành nghề) hiện không siết chặt theo vai trò — đây là điểm cần rà soát về bảo mật.
