# Luồng — Quản lý nhân viên xử lý (Cấu hình xếp nhân viên)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị.
- Có sẵn danh sách nhân viên của đơn vị (đối tượng để gán phục vụ đơn).
- Việc Xem chi tiết / Lưu cấu hình yêu cầu quyền (key `CauHinhXepNhanVien_GetCauHinhXepNhanVien` / `_SaveCauHinhNhanVien` — *cần product xác nhận* vai cụ thể).

## Luồng dùng / cấu hình (admin)
1. Admin mở **Cài đặt hệ thống → Cấu hình Order → popup "Auto gán user" → tab cấu hình nhân viên**.
2. Hệ thống gọi **Lấy danh sách cấu hình** xếp nhân viên theo điều kiện + người dùng hiện tại.
3. Admin mở **Xem chi tiết** một cấu hình.
4. Chỉnh tham số → bấm **Lưu** → hệ thống lưu cấu hình xếp nhân viên.
5. Cấu hình này feed bước "Xếp nhân viên" ở luồng tạo/phục vụ đơn POS (ViewXepNv / LuuXepNv).

## Các trường hợp / case biên
- **Lấy danh sách cấu hình:** chỉ yêu cầu đăng nhập (không gắn quyền riêng) — khác với Xem chi tiết / Lưu (có gắn quyền).
- **Cấu hình trống / chưa lưu:** bước "Xếp nhân viên" ở POS có thể không gán được nhân viên tự động → đơn để trống nhân viên phục vụ (*cần product xác nhận* hành vi mặc định khi không có cấu hình).
- **Không có chức năng xóa:** chỉ Lấy DS / Xem / Lưu — muốn đổi thì sửa rồi lưu đè.

## Ví dụ cụ thể có số
Cửa hàng C có 4 nhân viên phục vụ (A, B, C, D):
- Admin vào tab cấu hình xếp nhân viên, hệ thống tải danh sách cấu hình hiện có (giả sử 1 bản ghi).
- Admin mở chi tiết, thiết lập quy tắc gán → bấm Lưu → lưu thành công.
- Khi thu ngân tạo đơn mới ở POS, bước "Xếp nhân viên" đọc cấu hình vừa lưu và gán nhân viên phục vụ theo quy tắc (vd luân phiên A→B→C→D).
- *Cần product xác nhận:* cơ chế phân bổ cụ thể (luân phiên / theo ca / theo khu vực) — không suy ra được từ controller.

## Hậu điều kiện
- Sau **Lưu**: cấu hình mới được ghi DB, áp cho các đơn tạo/phục vụ về sau.
- Cấu hình là dữ liệu nền — không phát sinh đơn/giao dịch trực tiếp.
