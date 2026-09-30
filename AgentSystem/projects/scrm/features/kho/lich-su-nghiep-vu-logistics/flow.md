# Flow — Lịch sử nghiệp vụ logistics

## Tiền điều kiện
- Có đơn trong **OrderLogistic** (đơn giao vận) thuộc đúng **đơn vị + chi nhánh** của user đang đăng nhập.
- Đơn đã hoặc đang **đổi trạng thái giao hàng**, làm hệ thống sinh ra các bản ghi nghiệp vụ (Pending) và/hoặc đã xử lý xong (History).
- User có quyền **"NghiepVuManagement_TimDonHang"** (nhóm HT-CSKH) để xem danh sách.

## Luồng dùng chính (tra đơn → xem lịch sử trạng thái)
1. **Mở màn hình** Lịch sử nghiệp vụ logistics (module Kho), route `lich-su-nghiep-vu-logistic`.
2. Màn hình **tự load** lần đầu: nạp danh mục trạng thái giao hàng (dropdown) + gọi danh sách đơn không filter (trang 1).
3. **User nhập điều kiện tìm:**
   - Mã đơn (chứa, không cần khớp tuyệt đối).
   - Khoảng ngày (lọc theo **ngày tạo đơn**).
   - Trạng thái giao hàng của đơn.
   - Lọc trạng thái nghiệp vụ (chọn nhiều): Đang chờ / Thành công / Thất bại.
4. **Bấm Tìm kiếm** → hệ thống:
   - Lọc ra tập đơn thỏa điều kiện (mã đơn / ngày / trạng thái giao hàng).
   - Đếm số bản ghi **Pending** theo đơn, đếm **Thành công** + **Thất bại** từ History theo đơn, lấy **thời điểm xử lý gần nhất**.
   - Gộp lại mỗi đơn một dòng, áp filter trạng thái nghiệp vụ nếu có, sắp xếp theo thời điểm gần nhất giảm dần, phân trang.
5. **Bảng kết quả:** mỗi dòng = 1 đơn → Mã đơn, Khách hàng, Trạng thái giao hàng, số Pending, số Thành công, số Thất bại, Xử lý gần nhất.
6. **Bấm "Xem"** trên một dòng → mở Drawer chi tiết, gọi đồng thời 2 API:
   - Danh sách **Pending** của đơn (nghiệp vụ, hướng, trigger, số lần thử, thời gian xử lý, ghi chú lỗi).
   - Danh sách **History** của đơn (thêm cột Kết quả: Thành công / Thất bại / Bỏ qua, thời điểm xử lý thực tế).

## Case biên
- **Không nhập filter nào:** trả về toàn bộ đơn có phát sinh nghiệp vụ (Pending hoặc History) của chi nhánh, sắp xếp theo lần xử lý gần nhất.
- **Đơn chỉ có Pending, chưa có History:** số Thành công/Thất bại = 0, "Xử lý gần nhất" để trống.
- **Đơn vừa đổi trạng thái:** xuất hiện Pending là đúng — nghiệp vụ chờ tới giờ hẹn mới chạy.
- **Đơn đổi trạng thái nhiều lần liên tiếp:** bản ghi cũ có thể bị **Bỏ qua** (trang_thai 3) vì có bản ghi mới hơn cùng nghiệp vụ.
- **Hướng tăng và giảm bằng nhau trên cùng đơn:** có thể triệt tiêu, không tạo tác động — vẫn hiển thị trong lịch sử để truy vết.
- **Filter chọn cả 3 trạng thái nghiệp vụ:** tương đương không lọc theo trạng thái (chỉ cần đơn có ít nhất một loại > 0).
- **Phân quyền chi nhánh:** user chỉ thấy đơn thuộc đơn vị + chi nhánh của mình; đơn chi nhánh khác không hiện dù gõ đúng mã.

## Ví dụ có số
Đơn `DH-2026-00045`, giao thành công lúc 10:00 ngày 18/06:
- Hệ thống sinh 1 nghiệp vụ `CHI_TIEU` hướng **Tăng** (cộng chi tiêu khi giao thành công), hẹn chạy 10:00 + delay.
- Lúc 10:05 user tra: dòng đơn hiển thị **Pending = 1**, Thành công = 0, Thất bại = 0.
- Sau khi job chạy xong: dòng đơn hiển thị **Pending = 0, Thành công = 1**, Xử lý gần nhất = thời điểm job chạy.
- Nếu đơn sau đó bị **hoàn** lúc 14:00 → sinh thêm nghiệp vụ hướng **Giảm**. Nếu chạy lỗi 3 lần (ví dụ lỗi gọi service Customer): chuyển sang History với kết quả **Thất bại**, ghi chú lỗi hiển thị trong chi tiết → dòng đơn hiển thị **Thất bại = 1**.

## Hậu điều kiện
- Màn hình **không thay đổi dữ liệu đơn** khi chỉ tra cứu / xem chi tiết.
- Kết quả phản ánh đúng trạng thái hiện thời của các bảng Pending + History tại thời điểm tìm (dữ liệu đọc với NOLOCK, gần real-time).
- Người dùng nắm được: đơn này còn nghiệp vụ nào chờ, đã thành công bao nhiêu, lỗi cái nào và lý do.
