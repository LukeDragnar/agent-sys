> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Đồng bộ dữ liệu (theo đơn vị)

**Q: "Đồng bộ dữ liệu theo đơn vị" là làm gì?**
A: Là chạy lại việc cập nhật dữ liệu khách hàng của một đơn vị sang hệ Marketing Campaign cho khớp với dữ liệu gốc. Có 3 kiểu: đồng bộ khách hàng, chuẩn hóa địa chỉ (full-text), và đồng bộ chi tiêu khách.

**Q: Ai được dùng chức năng này?**
A: Chỉ **SuperUser** (super-admin nội bộ nhà cung cấp). Người dùng đơn vị/khách hàng không vào được; mọi nút đều bị chặn nếu không phải SuperUser.

**Q: Bấm "Đồng bộ" báo "Số bản ghi tối đa là 2.000!" / "10.000!" là sao?**
A: Mỗi lần đồng bộ có giới hạn batch: khách hàng và chi tiêu tối đa 2.000 bản ghi/lần, địa chỉ full-text tối đa 10.000 bản ghi/lần. Hãy giảm số bản ghi mỗi lần xuống trong giới hạn rồi chạy nhiều đợt.

**Q: Báo "Đang có tiến trình đồng bộ dữ liệu khác đang chạy!" nghĩa là gì?**
A: Đơn vị đó đang có một tiến trình đồng bộ cùng kiểu chạy chưa xong. Hệ thống chặn để tránh chạy chồng. Đợi tiến trình hiện tại xong rồi chạy lại.

**Q: Bấm đồng bộ xong sao chưa thấy dữ liệu thay đổi ngay?**
A: Đồng bộ chạy nền (xếp hàng đợi rồi dịch vụ Marketing Campaign xử lý), không tức thì. Tùy số lượng bản ghi, cần thời gian để hoàn tất.

**Q: Đồng bộ chi tiêu báo "Kiểu loại dữ liệu chi tiêu không đúng"?**
A: Khi đồng bộ chi tiêu, bạn phải chọn các **kiểu loại chi tiêu** hợp lệ. Nếu để trống hoặc chọn kiểu loại không thuộc danh sách cho phép, hệ thống báo lỗi này. Chọn lại đúng kiểu loại rồi chạy.

**Q: Tôi không phải SuperUser, bấm nút thì báo lỗi "dữ liệu không hợp lệ" — có phải lỗi hệ thống?**
A: Không hẳn lỗi — chức năng này chỉ cho SuperUser; hệ thống chặn nhưng hiện thông báo chưa rõ là "lỗi quyền" (đây là điểm cần cải thiện thông báo). Bạn cần tài khoản SuperUser để thực hiện.

**Q: Lỡ chạy đồng bộ nhầm thì có hủy được không?**
A: Có nút **Hủy** để hủy tiến trình đồng bộ của đơn vị. Tuy nhiên phần đã xử lý trước khi hủy có thể đã cập nhật dữ liệu; nên cân nhắc kỹ trước khi chạy batch lớn.
