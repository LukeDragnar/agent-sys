---
controller: UserController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Quản lý nhân viên (admin QuanTriArea): lưới nhân viên/tài khoản + Thêm/Sửa/Xóa, đổi mật khẩu, khóa/mở tài khoản, đồng bộ user theo đơn vị; HT-CSKH tìm đơn vị để chuyển"
muc_dich: "Quản trị tài khoản nhân viên cấp hệ thống/đơn vị: CRUD user, đổi/khóa mật khẩu, đồng bộ user theo đơn vị, tra cứu đơn vị-chi nhánh phục vụ CSKH chuyển đổi."
alias: ["User admin", "quản lý nhân viên", "quản lý tài khoản", "user QuanTriArea", "đồng bộ user", "khóa tài khoản"]
auth: mixed
state_machine: [HoatDong, Khoa]
deprecated: false
entities: [User, Role, DonVi]
stored_procs: []
depends: ["IUserService", "IUserSyncService", "IRoleService", "INotificationService", "IPermisionService", "IPageService", "ICommonService", "IRedisCacheService", "IVerificationCodeService", "AppCoreDataContext", "BaseController"]
cross_module_in: ["FE Hệ thống/Quản lý nhân viên gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/XoaTaiKhoan/DoiMatKhau/KhoaTaiKhoan", "HT-CSKH gọi GetDonViChiNhanhByUser/GetDonViByMaDon/LayDanhSachUserDonViTheoDieuKien để tìm & chuyển đơn vị", "Admin super gọi DongBoUserByDonVi/GetStatusDongBoUserByDonVi để re-sync user sai"]
cross_module_out: ["ThemMoi/CapNhat resolve đơn vị qua ICommonService.GetOrgByIdAsync (cross-service); DongBoUserByDonVi đồng bộ user sang các service khác; tạo/sửa user gắn ngành nghề/domain theo đơn vị"]
---

# UserController — Quản lý nhân viên (Core admin)

⚠️ KHÁC với `UserController_AuthenCustomer.md` (service AuthenCustomer — API app khách hàng). Đây là bản service **Core**, Area SanboxArea, route `api/QuanTriArea/User` → admin quản lý tài khoản nhân viên. Node suffix `_Core` vì trùng tên `UserController` trong cùng module he-thong.

Kế thừa `BaseController`. Mỗi method gắn key `User_*` (phần lớn HT-AD); vài endpoint chỉ `[SBAuthorize]` rỗng (check quyền trong service). auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/QuanTriArea/User/TimTheoDieuKien` | `User_TimTheoDieuKien` (HT-AD) | mixed | — | Lọc tài khoản theo `UserFilterDto`. | EF | — |
| 2 | DS user theo đơn vị (filter) | (lưới theo đơn vị) | POST `api/QuanTriArea/User/LayDanhSachUserDonViTheoDieuKien` | `User_LayDanhSachUserDonViTheoDieuKien` (HT-AD, HT-CSKH) | mixed | — | Lọc user theo `SA_Search_User_Unit_Filter`. | EF | — |
| 3 | Lấy tất cả theo đơn vị | — | POST `api/QuanTriArea/User/LayTatCaTheoIdDonVi/{idDonVi:Guid}` | `User_LayTatCaTheoIdDonVi` (HT-AD, HT-CSKH) | mixed | — | Trả toàn bộ user của `idDonVi`. | EF | — |
| 4 | Xem chi tiết | (mở form) | GET `api/QuanTriArea/User/TimTheoId/{id:Guid}` | `User_TimTheoId` (HT-AD) | mixed | — | `TimTheoIdAsync(id, CurrentUser)`. | EF | — |
| 5 | Thêm mới | Thêm | POST `api/QuanTriArea/User/ThemMoi` | `User_ThemMoi` (HT-AD) | mixed | — | Resolve đơn vị (GetOrgByIdAsync) lấy ngành nghề/domain; nếu không có đơn vị → set Guid.Empty + domain theo môi trường (prod/staging/sprint/dev) → `ThemMoiTaiKhoanAsync`. | EF + cross-service | — |
| 6 | Cập nhật | Sửa | POST `api/QuanTriArea/User/CapNhatThongTin` | `User_CapNhatThongTin` (HT-AD) | mixed | — | Nếu có đơn vị → resolve ngành nghề/domain → `CapNhapTaiKhoanAsync`. | EF + cross-service | — |
| 7 | Xóa tài khoản | Xóa | POST `api/QuanTriArea/User/XoaTaiKhoan` | `User_XoaTaiKhoan` (HT-AD) | mixed | — | `XoaTaiKhoanAsync(req, CurrentUser)` theo `UserNhanVienDeleteReq`. | EF | — |
| 8 | Đổi mật khẩu | Đổi MK | POST `api/QuanTriArea/User/DoiMatKhau` | `User_DoiMatKhau` (HT-AD) | mixed | — | `DoiMatKhau(req)` theo `ChangePassworDto`. | EF | — |
| 9 | Khóa/Mở tài khoản | Khóa/Mở | POST `api/QuanTriArea/User/KhoaTaiKhoan` | `User_KhoaTaiKhoan` (HT-AD) | mixed | TK hoạt động ↔ khóa | `KhoaTaiKhoanAsync(req, CurrentUser)` theo `UserLockReq`. | EF | — |

## Endpoint nội bộ (quyền check trong service / CSKH-super)
- [internal] **GET `GetDonViChiNhanhByUser?u=`** (`[SBAuthorize]` rỗng) — HT-CSKH tìm đơn vị/chi nhánh để chuyển; service tự chặn chỉ super-admin & HT-CSKH.
- [internal] **POST `GetDonViByMaDon`** — tìm đơn vị theo mã đơn (CSKH); quyền check trong service.
- [internal] **POST `DongBoUserByDonVi`** — admin super đồng bộ user theo đơn vị nếu sai (qua IUserSyncService).
- [internal] **GET `GetStatusDongBoUserByDonVi`** — trạng thái tiến trình đồng bộ user.
- [internal] **POST `AdminSearchByKeyword_DV`** (`[SBAuthorize]`) — admin tìm user theo keyword (`AdminFilterUser`).

## Quy tắc nghiệp vụ (gating)
1. ThemMoi: `IdDonVi` không rỗng → `GetOrgByIdAsync`; nếu đơn vị null → trả NotFound. (UserController.cs:101-105)
2. ThemMoi không đơn vị: set IdDonVi/IdChiNhanh/IdNganhNghe = Guid.Empty; `Domain` theo môi trường: Production→quantri.sandbox.com.vn, Staging→staging..., Sprint→sprint..., Development→localhost. (dòng 112-126)
3. CapNhat: chỉ resolve ngành nghề/domain khi `IdDonVi.HasValue` và đơn vị tồn tại. (dòng 140-147)

## Quan hệ
- **Gọi sang:** IUserService, IUserSyncService (đồng bộ), ICommonService.GetOrgByIdAsync (cross-service lấy đơn vị), IRoleService/IPermisionService/IPageService (phân quyền), IRedisCacheService, IVerificationCodeService, AppCoreDataContext.
- **Dữ liệu (entities):** `User` (gắn IdDonVi/IdChiNhanh/IdNganhNghe/Domain), `Role`, `DonVi`.
- **Ripple / đồng bộ:** ThemMoi/CapNhat/DongBoUserByDonVi → đồng bộ user sang các service khác theo đơn vị; thay đổi tác động phân quyền & đăng nhập.

## Luồng xuyên module
- **Tạo/sửa user** → resolve đơn vị (cross-service `GetOrgByIdAsync`) → gán ngành nghề + domain → `ThemMoiTaiKhoanAsync` (service tự đồng bộ sang nơi cần).
- **DongBoUserByDonVi** (admin super) → IUserSyncService re-sync toàn bộ user của đơn vị khi dữ liệu lệch.

## Lỗi đã biết / lưu ý kỹ thuật
- Nhiều endpoint nội bộ chỉ `[SBAuthorize]` rỗng, **dựa vào service tự check quyền** (super-admin/HT-CSKH) — không gating ở controller (UserController.cs:200-243).
- Domain theo môi trường hardcode trong controller (dòng 118-125) — đổi domain phải sửa code.
- Trùng tên `UserController` với bản AuthenCustomer → node suffix `_Core`.
