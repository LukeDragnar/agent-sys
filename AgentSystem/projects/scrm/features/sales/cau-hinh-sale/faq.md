> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Cấu hình Sale

**Q: "Cấu hình Sale" gồm những gì?**
A: Là bộ cài đặt tổng cho hoạt động bán hàng: cấu hình contact (init, kho số, chia contact xoay vòng, nguồn dữ liệu, sinh bản ghi CSKH…), cấu hình đơn hàng sale (kho tổng, chia số care đơn, nghiệp vụ), và cấu hình giảm giá tự động theo giá trị đơn. Admin cài một lần, áp cho cả đơn vị.

**Q: Tôi cài giảm giá theo đơn nhưng báo "Cấu hình giá trị đã được thiết lập"?**
A: Đã có một dòng cấu hình giảm giá có khoảng giá trị giao với khoảng bạn đang nhập. Mỗi khoảng giá trị chỉ được khai báo một lần, không được chồng nhau. Hãy sửa dòng cũ hoặc chọn khoảng giá khác.

**Q: Cài giảm giá theo % thì tối đa bao nhiêu?**
A: Không quá 100%. Nếu chọn giảm theo tiền thì mức giảm phải nhỏ hơn "giá trị từ" của khoảng (không được giảm nhiều hơn ngưỡng bắt đầu).

**Q: Giảm giá theo đơn được áp lúc nào?**
A: Khi khách thanh toán, hệ thống tự tra tổng giá trị đơn rồi áp dòng cấu hình "đang dùng" có khoảng chứa tổng đơn đó (ưu tiên khoảng có giá trị đến lớn nhất). Nếu tổng đơn nhỏ hơn mọi khoảng thì không giảm.

**Q: Tôi lưu cấu hình kho số cho một chi nhánh, sao cấu hình đơn vị cũng đổi?**
A: Đúng vậy — khi lưu cấu hình kho số theo chi nhánh, hệ thống tự đồng bộ lại cấu hình kho số cấp đơn vị (gom các chi nhánh đang hoạt động). Đây là thiết kế để cấu hình đơn vị luôn khớp các chi nhánh.

**Q: Ai được sửa các cấu hình này?**
A: Phần lớn cấu hình quan trọng cần quyền admin tổng đơn vị (DV-AD-TONG). Cấu hình giảm giá cần role admin hoặc quản lý đơn vị. Cấu hình SuperAdmin chỉ quản trị hệ thống (HT-AD/HT-CSKH) mới vào được.

**Q: "Cấu hình SuperAdmin" khác cấu hình thường thế nào?**
A: Cấu hình SuperAdmin là cấp hệ thống, vượt trên cấu hình của đơn vị, chỉ bộ phận quản trị hệ thống thao tác. Admin đơn vị thông thường không chỉnh được phần này.

**Q: Tôi sửa cấu hình rồi, có ghi lại lịch sử không?**
A: Có. Nhiều cấu hình (ví dụ giảm giá, sinh bản ghi CSKH) ghi lịch sử thay đổi để tra lại ai sửa, sửa gì.

**Q: Cấu hình đơn hàng sale ảnh hưởng gì?**
A: Các cờ như kho tổng, chia số care đơn, nghiệp vụ order sẽ chi phối cách đơn được tạo và phân bổ. Đổi cấu hình là đổi hành vi xử lý đơn cho cả đội sale.

**Q: Làm sao giới hạn ai được sửa từng thông tin của contact?** *(bổ sung Pha 2 — CauHinhSuaSo)*
A: Dùng **cấu hình quyền sửa field contact** (ma trận sửa số). Đây là bảng "field/cột của contact → các vai trò được phép sửa". Admin tích chọn cho mỗi field danh sách role được chỉnh; nhân viên không thuộc danh sách đó sẽ không sửa được field tương ứng. Cấu hình áp theo từng đơn vị.

**Q: Ai được thiết lập ma trận quyền sửa field contact?** *(bổ sung Pha 2 — CauHinhSuaSo)*
A: Cần quyền admin tổng đơn vị (DV-AD-TONG) hoặc quản trị hệ thống (HT-AD / HT-CSKH).

**Q: Đơn vị mới chưa cấu hình ma trận sửa số thì sao?** *(bổ sung Pha 2 — CauHinhSuaSo)*
A: Khi chưa từng lưu, màn lấy cấu hình có thể báo "Lỗi lấy dữ liệu" thay vì hiện danh sách rỗng — đây là hành vi hiện tại của hệ thống. Chỉ cần lưu ma trận lần đầu là về bình thường (lưu ý kỹ thuật đã ghi cho dev).
