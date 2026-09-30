> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Cấu hình hệ thống cửa hàng

**Q: Vì sao tôi không xóa được đơn hàng?**
A: Có thể đơn vị của bạn đã bật cờ **"không cho phép xóa đơn hàng"** trong Cấu hình Order. Khi đó hệ thống chặn xóa đơn với thông báo "Đơn vị không cho phép xóa đơn hàng!". Đây là thiết lập của shop để kiểm soát; admin có thể tắt cờ này trong Cấu hình Order nếu cần.

**Q: Tôi thêm máy in mới mà khi in đơn không thấy chọn được?**
A: Hãy kiểm tra máy in đã được **bật trạng thái sử dụng** chưa (có toggle "Sử dụng" trong danh mục máy in). Chỉ máy in đang bật mới hiện ra trong danh sách chọn khi in đơn bếp/hóa đơn.

**Q: Tôi muốn xóa một khu vực nhưng báo "Khu vực đang sử dụng không thể xóa!"?**
A: Khu vực đó đang có bàn/chỗ ngồi được phục vụ. Bạn cần để các bàn trong khu vực ở trạng thái trống (không còn đang phục vụ) rồi mới xóa được. Lưu ý khi xóa khu vực, toàn bộ chỗ ngồi thuộc khu vực cũng bị xóa theo.

**Q: Tạo khu vực mới báo trùng tên?**
A: Tên khu vực không được trùng trong cùng đơn vị và chi nhánh. Bạn đổi tên khác hoặc kiểm tra xem khu vực cùng tên đã tồn tại chưa.

**Q: Cấu hình Order và cấu hình SuperAdmin khác nhau thế nào?**
A: Cấu hình Order (init) là cài đặt của đơn vị do admin đơn vị (DV-AD-TONG) lưu. Cấu hình SuperAdmin là cài đặt cấp cao hơn, chỉ tài khoản hệ thống (HT-AD/HT-CSKH) mới chỉnh được, áp cho đơn vị từ phía vận hành SaaS.

**Q: Tôi sửa sơ đồ bàn xong nhưng máy POS chưa cập nhật?**
A: Mỗi lần thêm/sửa/xóa khu vực, hệ thống làm mới dữ liệu khu vực-chỗ ngồi. Nếu màn POS chưa đổi, hãy tải lại màn hình; lần lấy dữ liệu kế tiếp sẽ ra sơ đồ mới.

**Q: Cấu hình hóa đơn lấy ở đâu ra?**
A: Một phần cấu hình hóa đơn được đồng bộ tự động từ phần hóa đơn (Invoice) sang Cấu hình Order. Nếu thấy lệch, kiểm tra lại thiết lập ở phần hóa đơn điện tử.
