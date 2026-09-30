# Luồng — Thu chi (Quỹ tiền mặt / tiền gửi)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (IdDonVi) và chi nhánh (IdChiNhanh).
- Để xem danh sách phiếu thu-chi: cần quyền `ThuChi_DanhSach`. Các thao tác còn lại (chi tiết, xuất Excel, số dư ban đầu) chỉ cần đăng nhập.
- Đã có dữ liệu nền: danh mục **Lý do thu chi** (để lọc/khai phiếu), danh sách chi nhánh, và (cho số dư ban đầu) khách hàng / nhà cung cấp đã tồn tại.

## Luồng dùng (xem quỹ & phiếu thu-chi)
1. Người dùng mở **Kế toán → Quỹ / Thu chi**.
2. Hệ thống kiểm vai trò: nếu **không** phải admin tổng và **không** phải admin nhiều chi nhánh → ép `IdChiNhanh = chi nhánh của người dùng` (chỉ xem dòng tiền chi nhánh mình).
3. Người dùng chọn bộ lọc (chi nhánh, loại thu-chi, lý do, khoảng ngày) và/hoặc gõ từ khóa, rồi tìm.
4. Hệ thống truy vấn phiếu của đơn vị (`IsDelete=false`), sắp xếp mới nhất lên đầu theo ngày tạo phiếu. Từ khóa lọc thêm theo mã hóa đơn của phiếu gắn đơn hàng.
5. Tính **tồn quỹ** (đầu kỳ / cuối kỳ / hiện tại) và **tổng Thu / Chi / Nạp / Rút** hiển thị trên panel.
6. Tên khách hàng trên phiếu được làm giàu bằng cách gọi sang module Khách hàng.
7. Người dùng bấm vào một phiếu để xem chi tiết (phiếu + chi tiết đối tượng), hoặc bấm **Xuất Excel** theo loại phiếu.

## Luồng cấu hình (khai số dư ban đầu)
1. Mở phần **Số dư ban đầu** trong màn Thu chi.
2. Bấm **Khởi tạo số dư ban đầu**, chọn: chi nhánh, nhóm đối tượng (Khách hàng / Nhà cung cấp / Tài khoản), đối tượng cụ thể, kiểu dư (Nợ = +, Có = −) và số tiền (phải khác 0).
3. Lưu:
   - Nhóm = **Khách hàng** → gọi module Khách hàng ghi số dư ban đầu KH.
   - Nhóm = **Nhà cung cấp** → gọi module Kho/NCC ghi số dư ban đầu NCC.
   - Nhóm = **Tài khoản** → chống trùng (đơn vị × chi nhánh × nhóm × loại × đối tượng) rồi lưu nội bộ.
4. Có thể **xóa** dòng số dư: hệ thống gọi sang Khách hàng/NCC để xóa số dư bên kia trước, rồi xóa vĩnh viễn bản ghi tại đây.

## Các trường hợp / case biên
- **Người dùng thường (không admin):** luôn bị ép về đúng chi nhánh, bỏ qua lựa chọn chi nhánh khác.
- **Bộ lọc rỗng / không có phiếu:** danh sách trống; xuất Excel báo "Không có dữ liệu để xuất".
- **Phiếu không gắn khách hàng:** khi làm giàu tên KH, nếu module Khách hàng trả về rỗng, hệ thống hiện báo lỗi "Không tìm thấy thông tin sản phẩm" (message sai ngữ cảnh) — có thể chặn cả lưới. Đây là lỗi đã biết (xem dev.md).
- **Số dư ban đầu số tiền = 0:** bị chặn (số tiền phải khác 0).
- **Số dư ban đầu KH/NCC, một bên lỗi:** nếu gọi sang Khách hàng/NCC lỗi thì KHÔNG lưu cục bộ. Nhưng nếu ghi bên kia OK mà lưu cục bộ lỗi (hoặc ngược lại khi xóa) thì có thể **lệch** vì hai bước không cùng một giao dịch (xem dev.md).
- **Đơn hàng đổi phương thức thanh toán:** module Đơn hàng gọi hệ thống khớp lại phiếu thu-chi của đơn — phương thức bị bỏ → xóa phiếu; phương thức mới → thêm phiếu (clone từ phiếu mẫu, sinh mã mới); phương thức còn → cập nhật số tiền. Khi luồng đơn lỗi → gọi rollback để hoàn lại.

## Ví dụ cụ thể có số
Chi nhánh A trong tháng 6: đầu kỳ tồn quỹ tiền mặt 10.000.000đ.
- Có 3 phiếu thu tiền mặt (tổng 5.000.000đ) và 2 phiếu chi tiền mặt (tổng 2.000.000đ) trong tháng.
- Hệ thống tính: trong kỳ Thu = 5.000.000, Chi = 2.000.000 → tồn cuối kỳ = 10.000.000 + 5.000.000 − 2.000.000 = **13.000.000đ**.
- Anh Nam (admin tổng) chọn "tất cả chi nhánh" → thấy thêm phiếu của chi nhánh B; chị Mai (nhân viên CN A) chỉ thấy 5 phiếu của CN A.
- Khai số dư ban đầu cho khách Trần Văn B: kiểu dư **Nợ**, số tiền 1.500.000đ → lưu thành +1.500.000 và đẩy sang công nợ phải thu KH. Khai tiếp với số tiền 0 → bị chặn.

## Hậu điều kiện
- Xem danh sách / chi tiết / xuất Excel **không thay đổi dữ liệu** (read-only).
- Thêm số dư ban đầu KH/NCC → ghi bản ghi `SoDuBanDau` + cập nhật công nợ bên Khách hàng/NCC.
- Xóa số dư ban đầu → xóa vĩnh viễn bản ghi + hoàn công nợ bên kia.
- Cập nhật phiếu thu-chi theo đơn hàng (luồng nội bộ từ Đơn hàng) → phiếu của đơn được khớp lại theo phương thức thanh toán mới.
