> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý hồ sơ nhân viên

**Q: Nhân viên nghỉ việc thì xử lý tài khoản thế nào — xóa hay khóa?**
A: Nên dùng **Khóa tài khoản** thay vì xóa, để giữ lại lịch sử (công, hoa hồng, đơn hàng đã làm). Khi cần có thể mở khóa lại. Xóa tài khoản chỉ dùng khi chắc chắn không cần dữ liệu nữa, và cần quyền admin hệ thống.

**Q: Tôi tạo nhân viên mới nhưng quên chọn đơn vị?**
A: Nếu không gắn đơn vị, hệ thống vẫn tạo được nhưng tài khoản sẽ ở dạng "không thuộc đơn vị" (đơn vị/chi nhánh để trống, domain theo môi trường). Nên gắn đúng đơn vị-chi nhánh ngay từ đầu để nhân viên có đúng quyền và dữ liệu.

**Q: Nhân viên báo đăng nhập được nhưng không thấy dữ liệu / thiếu quyền, dù tài khoản đã có?**
A: Có thể tài khoản bị lệch dữ liệu giữa các phần của hệ thống. Admin có thể dùng **Đồng bộ tài khoản theo đơn vị** để đồng bộ lại, sau đó nhân viên đăng nhập lại.

**Q: Quản lý chi nhánh có tự thêm phòng ban / nhóm cho chi nhánh mình được không?**
A: Được. Có bản thao tác riêng cho cấp chi nhánh (DV-AD-CN) — khi đó hệ thống tự gán chi nhánh của bạn. Bản cấp đơn vị (DV-AD-TONG) dành cho admin tổng quản lý toàn đơn vị.

**Q: Vì sao tôi tạo nhóm sale rồi mà ở màn chia đơn / giao việc chưa thấy nhóm?**
A: Hãy kiểm tra nhóm đã thêm đúng đơn vị/chi nhánh và đã thêm nhân viên vào nhóm chưa. Các màn chia đơn/giao việc/chia chat lấy nhóm theo đơn vị/chi nhánh và theo vai trò của nhân viên trong nhóm.

**Q: Thêm/bớt người trong nhóm có ảnh hưởng gì ngoài việc hiển thị?**
A: Có. Thành viên nhóm và cây phòng ban quyết định quan hệ **lead/quản lý sale** và việc phân công sale/CSKH, chia đơn, chia chat. Đổi thành viên có thể đổi cách phân bổ công việc.

**Q: Khởi tạo "nhóm mẫu" là gì, có nên bấm không?**
A: Đó là chức năng sinh sẵn bộ nhóm nhân viên mẫu theo ngành nghề cho đơn vị mới. Chỉ nên dùng khi thiết lập ban đầu; nếu đã có nhóm thì không cần. (Lưu ý nội bộ: chức năng này hiện chặn quyền chưa chặt — cần product xác nhận trước khi mở rộng.)

**Q: Tôi đổi mật khẩu cho nhân viên, nhân viên vẫn đăng nhập bằng mật khẩu cũ?**
A: Hãy chắc chắn đã lưu thành công và báo nhân viên dùng mật khẩu mới. Nếu vẫn lỗi, thử cho nhân viên đăng xuất rồi đăng nhập lại; nếu còn lỗi thì liên hệ hỗ trợ kỹ thuật.

---

## (Bổ sung Pha 2 — phần Hồ sơ nhân sự HR; product xác nhận có tách feature riêng không)

> Các câu dưới đây cho màn **Hồ sơ nhân sự HR** (xem/sửa nhân thân + nghiệp vụ HR, ảnh, giấy tờ của 1 nhân viên).

**Q: Hồ sơ nhân sự khác gì với tài khoản nhân viên?**
A: Tài khoản nhân viên là phần đăng nhập/phân quyền (mở ở màn Quản lý nhân viên). Hồ sơ nhân sự là phần **thông tin con người + hồ sơ HR**: ngày vào công ty, ngày lên chính thức, ngày nghỉ việc, nguồn tuyển dụng, bí danh, Facebook, quê quán, kèm **ảnh** và **giấy tờ** (Giấy tờ liên quan / Quyết định bổ nhiệm). Mỗi nhân viên có đúng 1 hồ sơ đi kèm.

**Q: Tôi muốn sửa họ tên / email / số điện thoại của nhân viên ngay trong hồ sơ nhân sự?**
A: Không sửa được ở màn hồ sơ nhân sự. Họ tên và các thông tin nhân thân thuộc về **tài khoản nhân viên** — vào màn **Quản lý nhân viên** để sửa. Màn hồ sơ chỉ cho sửa các thông tin nghiệp vụ HR (ngày tháng, nguồn tuyển dụng, bí danh, Facebook, quê quán).

**Q: Tôi nhập ngày nghỉ việc trước ngày vào công ty, hệ thống báo lỗi?**
A: Đúng. Ngày nghỉ việc phải sau ngày vào công ty; tương tự ngày lên chính thức cũng phải sau ngày vào công ty. Hãy kiểm tra lại các mốc ngày cho hợp lệ rồi lưu lại.

**Q: Lúc lưu hồ sơ, một giấy tờ tôi thêm trước đó biến mất?**
A: Khi lưu hồ sơ, hệ thống đồng bộ danh sách giấy tờ theo đúng những gì đang hiển thị trên màn — giấy tờ nào không còn trong danh sách lúc lưu sẽ bị xóa. Hãy giữ nguyên các giấy tờ cần lưu trong danh sách trước khi bấm lưu.

**Q: Tôi chỉ muốn xem hồ sơ nhân sự, có làm thay đổi gì dữ liệu không?**
A: Về phía người dùng thì xem là xem, không cần lo. (Lưu ý nội bộ: lần đầu mở hồ sơ của một nhân viên chưa có hồ sơ, hệ thống sẽ tự tạo sẵn một bản ghi hồ sơ rỗng cho người đó — đây là hành vi kỹ thuật hiện tại, không ảnh hưởng thông tin nhân viên.)

**Q: Ai được mở và sửa hồ sơ nhân sự?**
A: Các vai quản trị/kế toán cấp đơn vị (admin tổng/chi nhánh, kế toán, quản lý đơn vị). Quyền chi tiết cho từng nút (đặc biệt thêm/xóa giấy tờ) hiện chưa hoàn toàn chuẩn — cần product xác nhận (ghi nhận nội bộ).
