# Luồng — Cấu hình hệ thống

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Để sửa **cấu hình chức năng** (tab Cài đặt chung): user là SuperUser, hoặc CSKH hệ thống, hoặc Admin đơn vị (`isSuperUser || isHeThongCSKH() || isAdmin`).
- Để CRUD **chi nhánh**: có quyền `ChiNhanh_ThemMoi/_CapNhatThongTin/_Xoa`.
- Để CRUD **tài khoản ngân hàng**: có quyền `TkNganHangHeThong_*` tương ứng.
- Để **buộc đổi mật khẩu**: có quyền `UserBaoMat_YeuCauDoiMatKhau` (admin đơn vị).

## Luồng dùng (user / admin đơn vị)
**A. Cấu hình chức năng:**
1. Mở `caidat/he-thong`, chọn tab → đổi thiết lập → Lưu → áp dụng toàn đơn vị.

**B. Chi nhánh:**
1. Mở `caidat/chinhanh`, bấm Thêm mới → nhập thông tin → khi tạo, hệ thống **tự ép `IdDonVi = đơn vị đang đăng nhập`**.
2. Sửa/Xóa chi nhánh → áp dụng cho đơn vị mình.

**C. Tài khoản ngân hàng:**
1. Mở màn quản lý TK ngân hàng → Thêm/Sửa tài khoản.
2. Bấm **Mặc định** trên 1 tài khoản → đặt làm mặc định cho người dùng hiện tại (riêng thu = loại 0, chi = loại 1).
3. (tùy chọn) Kiểm tra kết nối online với terminal.

**D. Bảo mật:**
1. Mở `caidat/cai-dat-bao-mat`, chọn nhân viên → Yêu cầu đổi mật khẩu → đặt cờ buộc đổi trên user.

## Luồng cấu hình (admin)
- Xem mục B/C/D ở trên — toàn bộ là cấu hình do admin đơn vị thực hiện. Phần quản trị toàn hệ thống (HT-AD/HT-KT) nằm ở khu QuanTriArea, không thuộc cẩm nang khách.

## Case biên
- **Xóa tài khoản ngân hàng đã dùng** → service `KiemTraNganHangDaSuDung` trả true → chặn xóa (báo lỗi).
- **Non-admin xem chi tiết tài khoản ngân hàng** (`TimTheoId`) → bị lọc theo chi nhánh: chỉ thấy TK cùng chi nhánh của mình.
- **Lọc chi nhánh khi không phải super/CSKH/kế toán hệ thống** → hệ thống ép `IdDonVi = CurrentUser.DonViId` (không xem được chi nhánh đơn vị khác).
- **Buộc đổi mật khẩu** chỉ đặt cờ, không đăng xuất ngay; lần login kế tiếp mới chặn.
- **Đặt mặc định trên tài khoản không có quyền `_SetMacDinh`** → bị chặn ở tầng phân quyền.
- **Tra TK ngân hàng theo TerminalId** (`TimTheoTerminalId/{terminalId}`) hiện `[AllowAnonymous]` → trả được dữ liệu kể cả khi chưa đăng nhập (lỗ hổng — xem dev.md).

## Ví dụ cụ thể có số
Đơn vị X có 3 chi nhánh (CN1, CN2, CN3) và 4 tài khoản ngân hàng.
- Admin tổng anh Nam mở màn TK ngân hàng → thấy cả 4 tài khoản. Anh đặt TK "Vietcombank - 0123" làm **mặc định thu** và TK "ACB - 0456" làm **mặc định chi**.
- Kế toán chị Hoa (chỉ thuộc CN2, không phải admin) mở `TimTheoId` của 1 tài khoản thuộc CN1 → hệ thống lọc theo chi nhánh → **không trả** tài khoản CN1 cho chị.
- Anh Nam xóa TK "BIDV - 0789" chưa từng dùng → xóa thành công. Anh thử xóa TK "Vietcombank - 0123" (đang là mặc định thu, đã có giao dịch) → `KiemTraNganHangDaSuDung = true` → **bị chặn**, báo lỗi đã sử dụng.
- Anh Nam thêm CN4 → khi lưu, hệ thống tự gán `IdDonVi = X`; CN4 lập tức xuất hiện trong combobox chi nhánh các màn khác (sau khi tải lại).

## Hậu điều kiện
- Cấu hình chức năng được lưu theo đơn vị → các màn liên quan đọc thiết lập mới.
- Tài khoản ngân hàng mới/đặt mặc định → màn thu/chi và tạo QR thu dùng TK mặc định mới.
- Yêu cầu đổi mật khẩu → user mục tiêu bị buộc đổi ở lần đăng nhập kế tiếp.
- Thêm/xóa chi nhánh → thay đổi danh sách chi nhánh dùng cho lọc/báo cáo toàn đơn vị.
