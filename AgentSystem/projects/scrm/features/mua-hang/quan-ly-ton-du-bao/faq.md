> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý tồn (dự báo)

**Q: Tôi muốn hệ thống tự báo khi hàng sắp hết thì cài ở đâu?**
A: Vào **Kho → Giới hạn tồn kho**, khai báo cho từng sản phẩm × chi nhánh mức **tồn tối thiểu** (và tối đa, khẩn cấp nếu cần). Sau đó vào màn **Báo cáo tồn kho đơn vị / Dự báo hết hàng** để xem mặt hàng nào đã chạm ngưỡng. Nếu chưa cài ngưỡng thì cảnh báo sẽ không chính xác.

**Q: Màn dự báo không hiện cảnh báo gì, dù hàng gần hết?**
A: Hãy kiểm tra: (1) bạn đã cài Giới hạn tồn kho cho sản phẩm đó chưa; (2) đơn vị đã bật chế độ "tính giới hạn tồn kho tự động" chưa; (3) bạn đã chọn **kiểu khoảng ngày** khi xem dự báo chưa — thiếu bước này hệ thống sẽ báo lỗi yêu cầu chọn.

**Q: Sao tôi không xuất được Excel báo cáo tồn kho đơn vị?**
A: Chức năng xuất Excel báo cáo này chỉ dành cho tài khoản admin (admin tổng / admin chi nhánh). Ngoài ra mỗi lần xuất giới hạn 10.000 dòng — nếu dữ liệu nhiều hơn, hãy lọc bớt (theo nhóm SP, chi nhánh, khoảng ngày) rồi xuất từng phần.

**Q: Tôi sửa nhanh ô "Tồn tối thiểu" trên lưới Giới hạn tồn kho rồi bấm Cập nhật mà báo lỗi?**
A: Trường hợp này đã được ghi nhận. Cách chắc chắn là mở dòng đó (hoặc dùng Thêm mới / Khởi tạo) và nhập đầy đủ các trường rồi lưu. Nếu vẫn lỗi, báo bộ phận hỗ trợ kèm tên sản phẩm và chi nhánh.

**Q: Tôi đặt mức báo động cho hàng loạt sản phẩm cùng lúc được không?**
A: Được. Dùng nút **Khởi tạo** để áp một bộ cấu hình cho toàn bộ (hoặc theo nhóm) sản phẩm của đơn vị; hoặc dùng **Nhập Excel** (tối đa 2.000 dòng/file) nếu bạn đã có sẵn danh sách. Khi thêm tay từng dòng thì tối đa 100 dòng/lần.

**Q: Từ màn dự báo tôi đặt hàng luôn được không?**
A: Được. Trên màn báo cáo tồn kho đơn vị có nút tạo **đơn đặt hàng** cho những mặt hàng đang thiếu, nối thẳng sang quy trình Đơn đặt hàng để bạn duyệt và mua.

**Q: Tại sao đồng nghiệp ở chi nhánh khác thấy số tồn khác tôi?**
A: Mỗi người chỉ xem/cấu hình trong phạm vi chi nhánh của mình (trừ admin xem toàn đơn vị). Đây là phân quyền theo chi nhánh, không phải sai số liệu.

**Q: Tôi cài nhầm ngưỡng, xóa đi có khôi phục được không?**
A: Không. Xóa dòng giới hạn tồn kho là xóa hẳn. Nếu chỉ nhập sai số, bạn nên sửa lại giá trị thay vì xóa.
