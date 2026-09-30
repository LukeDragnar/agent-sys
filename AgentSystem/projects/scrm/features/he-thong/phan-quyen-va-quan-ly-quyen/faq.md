> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Phân quyền & quản lý quyền

**Q: Nhân viên báo "không vào được màn X / bị báo không có quyền", xử lý thế nào?**
A: Kiểm tra theo thứ tự: (1) user đang được gán role nào; (2) role đó đã được tích quyền cho chức năng X chưa (bảng phân quyền chức năng); (3) đơn vị đã **mua/bật tiện ích** chứa chức năng X chưa. Thiếu bất kỳ lớp nào trong ba lớp này đều khiến user bị chặn — đây là thiết lập, không phải lỗi.

**Q: "Role" và "quyền" khác nhau thế nào?**
A: Role là **chức danh / nhóm vai trò** (vd admin đơn vị, kế toán, sale). Quyền là **cho phép làm một chức năng cụ thể**. Bạn gán role cho user, rồi tích quyền cho role. User hưởng quyền theo role mình mang.

**Q: Tôi muốn cho một shop mới có quyền giống hệt shop cũ, có phải tích lại từng ô không?**
A: Không cần. Dùng chức năng **Copy quyền**: chọn đơn vị nguồn và đơn vị đích, hệ thống sao chép cấu hình quyền. (Bản copy mới — V2 — chỉ super-admin chạy được.)

**Q: Vì sao tôi không phân quyền mặc định toàn hệ thống được dù là admin?**
A: Phân quyền **mặc định toàn hệ thống** chỉ dành cho **super-admin** của nhà cung cấp. Admin thường (kể cả admin đơn vị) chỉ tinh chỉnh quyền trong phạm vi đơn vị của mình.

**Q: Xóa một role có an toàn không?**
A: Cần thận trọng. Hệ thống hiện **không cảnh báo** nếu role đó còn đang gán cho user. Xóa nhầm sẽ khiến những user đó mất role và có thể mất quyền truy cập. Nên gỡ role khỏi user trước, hoặc kiểm tra kỹ trước khi xóa.

**Q: "Quyền custom" là gì? Tôi tạo mới ở đâu?**
A: Quyền custom là quyền cấp **riêng** cho một user/role ngoài role mặc định (override). Màn quản lý quyền custom chỉ để **xem và thu hồi (xóa)** — bạn không tạo quyền custom trực tiếp ở đây; chúng được sinh từ luồng khác.

**Q: Tôi đổi quyền cho một role thì khi nào có hiệu lực?**
A: Ngay sau khi lưu, mọi user đang mang role đó sẽ chịu ảnh hưởng (được thêm/mất quyền vào màn). Nếu user vẫn thấy như cũ, thử đăng nhập lại để hệ thống nạp lại quyền.

**Q: "Tự động phân quyền" làm gì?**
A: Nó gán nhanh các quyền mặc định cho hệ thống theo mẫu có sẵn, đỡ phải tích tay từng ô. Thường dùng khi khởi tạo hoặc đồng bộ lại quyền chuẩn.
