# Luồng — Quản lý tài khoản & người dùng

## Tiền điều kiện
- Người thao tác là **admin hệ thống (HT-AD)**; một số tra cứu/chuyển đơn vị cho **HT-CSKH**; đồng bộ user cho **super-admin**.
- Đơn vị (tenant) cần tồn tại nếu tài khoản được gắn đơn vị.
- Hệ thống biết môi trường đang chạy (production/staging/sprint/development) để gán domain cho tài khoản không có đơn vị.

## Luồng dùng (admin — CRUD tài khoản)
1. Mở **Hệ thống → Quản lý nhân viên / tài khoản**.
2. Tải lưới: `User/TimTheoDieuKien` (theo `UserFilterDto`) hoặc `User/LayDanhSachUserDonViTheoDieuKien` / `User/LayTatCaTheoIdDonVi/{idDonVi}`.
3. **Thêm** (`User/ThemMoi`):
   - Nếu `IdDonVi` khác rỗng → `GetOrgByIdAsync` lấy đơn vị; đơn vị null → trả **NotFound**.
   - Nếu không có đơn vị → set IdDonVi/IdChiNhanh/IdNganhNghe = `Guid.Empty`, gán `Domain` theo môi trường.
   - → `ThemMoiTaiKhoanAsync` (service tự đồng bộ sang nơi cần).
4. **Sửa** (`User/CapNhatThongTin`): chỉ resolve ngành nghề/domain khi `IdDonVi.HasValue` và đơn vị tồn tại → `CapNhapTaiKhoanAsync`.
5. **Xóa** (`User/XoaTaiKhoan`) theo `UserNhanVienDeleteReq`.
6. **Đổi mật khẩu** (`User/DoiMatKhau`) theo `ChangePassworDto`.
7. **Khóa/Mở** (`User/KhoaTaiKhoan`) theo `UserLockReq` → đổi trạng thái HoatDong ↔ Khoa.

## Luồng cấu hình (CSKH / super-admin)
1. **CSKH tìm để chuyển đơn vị**: `GetDonViChiNhanhByUser?u=`, `GetDonViByMaDon` — service tự chặn chỉ super-admin & HT-CSKH.
2. **Super-admin đồng bộ**: `DongBoUserByDonVi` (qua `IUserSyncService`) → re-sync toàn bộ user của đơn vị; `GetStatusDongBoUserByDonVi` để theo dõi tiến trình.
3. **Admin tìm theo keyword**: `AdminSearchByKeyword_DV` (`AdminFilterUser`).

## Case biên
- **Thêm user gắn đơn vị không tồn tại** → NotFound, không tạo.
- **Thêm user không đơn vị** → IdDonVi/IdChiNhanh/IdNganhNghe = Guid.Empty; Domain hardcode theo môi trường (prod = quantri.sandbox.com.vn, staging/sprint = tương ứng, dev = localhost).
- **Sửa user mà IdDonVi null** → bỏ qua bước resolve ngành nghề/domain.
- **Khóa tài khoản đang đăng nhập** → phiên hiện tại bị chặn ở lần xác thực kế tiếp.
- **Đồng bộ user theo đơn vị** → tác động nhiều service; theo dõi `GetStatusDongBoUserByDonVi` để biết đã xong.
- **Gọi các endpoint nội bộ (tìm đơn vị/đồng bộ)** mà không đủ quyền → service tự từ chối (gating nằm trong service, không ở controller).

## Ví dụ cụ thể có số
Đơn vị "Spa Hoa Mai" (IdDonVi tồn tại) cần thêm 5 nhân viên.
- Admin thêm tài khoản "le.thi.a" gắn đơn vị Spa Hoa Mai → hệ thống lấy ngành nghề + domain của Spa Hoa Mai → tạo OK, đồng bộ sang các service.
- Admin thêm 1 tài khoản hệ thống "support01" không gắn đơn vị → IdDonVi = Guid.Empty, domain = quantri.sandbox.com.vn (môi trường production).
- Một nhân viên nghỉ việc → admin **Khóa** tài khoản → user đó không đăng nhập được nữa; nếu cần xóa hẳn thì **Xóa tài khoản**.
- CSKH phát hiện user "tran.b" hiển thị sai đơn vị → tìm `GetDonViChiNhanhByUser` → thấy đang gắn nhầm chi nhánh → phối hợp chuyển đúng đơn vị; sau đó super-admin chạy `DongBoUserByDonVi` để đồng bộ lại.

## Hậu điều kiện
- Thêm/Sửa user → cập nhật DB + đồng bộ user sang các service liên quan theo đơn vị.
- Khóa/Mở → đổi trạng thái đăng nhập của user.
- Xóa → gỡ tài khoản.
- Đồng bộ → dữ liệu user của đơn vị nhất quán trở lại giữa các service.
