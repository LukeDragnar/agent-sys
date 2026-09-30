# Luồng — Quản lý thông tin khách hàng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Có ít nhất 1 trong các role được phép xem danh sách KH 360 (Sale, MKT, admin đơn vị/chi nhánh, kế toán, CSKH…). Màn Danh mục khách hàng yêu cầu role admin tổng đơn vị (DV-AD-TONG).
- Đơn vị đã có sẵn dữ liệu nền để lọc: nhóm khách hàng, hạng khách hàng, nguồn, danh mục khách hàng (nghề nghiệp, tôn giáo…). Nếu trống thì bộ lọc tương ứng không có lựa chọn.
- Cấu hình đơn vị ảnh hưởng phạm vi xem: cho phép chi nhánh xem khách toàn đơn vị, che số điện thoại, cho phép lead/quản lý xem khách của nhân viên.

## Luồng dùng (Danh sách khách hàng)
1. Người dùng mở **Khách hàng → Danh sách khách hàng**.
2. Hệ thống xác định vai trò người dùng để quyết định "xếp hạng theo Sale (loaiNhanVien=1)" hay "theo MKT (loaiNhanVien=3)"; nếu là user thuần Sale → mặc định 1, thuần MKT → mặc định 3.
3. Người dùng nhập từ khóa (tên/SĐT) và/hoặc mở bộ lọc chi tiết, rồi bấm tìm.
4. Hệ thống gọi cùng lúc: danh sách khách (có phân trang), tổng số bản ghi, và thống kê nhanh (tổng khách, tạo hôm nay, sinh nhật hôm nay/tháng).
5. Trước khi truy vấn, hệ thống lọc theo quyền: lấy danh sách nhóm + user Sale/MKT mà người dùng được phép xem; nếu user thuần Sale/MKT và cấu hình không cho lead xem khách nhân viên → chỉ giới hạn về khách của chính user đó.
6. Kết quả trả về được làm giàu thêm tại tầng service: dịch mã nhóm/hạng/nghề nghiệp/tôn giáo… thành tên hiển thị, ghép tên Sale/MKT, ghép tên địa chỉ (tỉnh/huyện/xã), tính tuổi từ ngày sinh, xử lý che/ẩn số điện thoại theo cấu hình.
7. Bảng hiển thị; người dùng có thể phân trang, sắp xếp, bấm vào dòng để mở hồ sơ chi tiết (drawer), hoặc thực hiện thêm/sửa/nhập Excel/xuất Excel/gửi SMS/phân bổ.

## Luồng cấu hình (admin — Danh mục khách hàng)
1. Admin mở **Khách hàng → Danh mục khách hàng**.
2. Chọn Kiểu loại (0=Tình trạng khách hàng, 1=Hiệu quả sử dụng, 2=Tình trạng sử dụng, 3=Chất lượng data, 4=Lý do từ chối, 5=Nghề nghiệp, 6=Tình trạng hôn nhân, 7=Tôn giáo, 8=Trường, 9=Lớp, 10=Đợt tuyển sinh, 11=Trạng thái, 12=Ngành học quan tâm, 13=Loại cây trồng).
3. Thêm mới: nhập Mã, Tên, STT → hệ thống validate (Mã đúng định dạng, Mã & Tên không trùng trong cùng kiểu loại + cùng đơn vị) → lưu → xóa cache danh mục của đơn vị.
4. Sửa/Xóa tương tự, đều xóa cache để lần lọc khách sau lấy dữ liệu mới.

## Case biên
- **User thuần Sale/MKT + cấu hình `IsLeadManagerViewCustomerOfEmployee = false`** → chỉ thấy khách của chính mình (idUserString = id của user, bỏ qua nhóm).
- **Cấu hình `ChiNhanhHienThiTatCaKhachHangDonVi` tắt** → ép `IsViewAll = false`, chỉ xem trong phạm vi chi nhánh.
- **Admin chi nhánh / QLDV bật "xem tất cả"** → bỏ lọc theo chi nhánh (idChiNhanh = null).
- **Che số điện thoại bật** → SĐT hiển thị bị mask; nếu thêm cấu hình không cho gọi → trường SĐT thật để null.
- **Xuất Excel** → ép PageSize = 5.000, Page = 1; nếu không có dữ liệu trả lỗi; nếu service xuất bận trả mã 503 (FE báo "chưa được xuất lúc này").
- **Danh mục: tạo trùng Mã hoặc trùng Tên** trong cùng kiểu loại → trả lỗi "Mã/Tên danh mục đã tồn tại!".
- **Xóa khách hàng** → luôn trả lỗi "Tính năng xóa khách hàng không còn được sử dụng!".

## Ví dụ cụ thể có số
Shop A có 1.200 khách. Chị Lan (role DV-SALE) đang phụ trách 80 khách.
- Chị Lan mở Danh sách khách hàng: do là Sale thuần và đơn vị không cho lead xem khách nhân viên → hệ thống chỉ trả **80 khách** của chị, thống kê "tổng" = 80.
- Anh Tuấn (DV-AD-TONG) mở cùng màn, bật "xem tất cả" → thấy **1.200 khách**, thống kê hiển thị ví dụ: tổng 1.200, tạo hôm nay 5, sinh nhật hôm nay 2, sinh nhật tháng này 34.
- Anh Tuấn lọc "nhóm = VIP, tháng sinh nhật = 6" còn 34 khách → bấm Xuất Excel → vì < 5.000 nên xuất đủ 34 dòng ra file `DanhSachKhachHang_all_all.xlsx`.
- Admin khai báo danh mục Nghề nghiệp: thêm Mã `GV` – Tên "Giáo viên" – STT 1 → lưu OK. Thêm tiếp Mã `GV` lần nữa → báo "Mã danh mục đã tồn tại!".

## Hậu điều kiện
- Truy vấn danh sách KH 360 **không thay đổi dữ liệu** (read-only); chỉ làm giàu dữ liệu hiển thị.
- Xuất Excel **bắn 1 event qua RabbitMQ** để ghi lịch sử xuất (ai xuất, IP, thời điểm) nhưng không sửa khách.
- Thêm/Sửa/Xóa **danh mục khách hàng** ghi vào DB và **xóa cache danh mục đơn vị** → các màn lọc/nhập khách sau đó lấy danh mục mới.
