# Dev — Quản lý quyền & tài khoản (thiết bị & đăng nhập)

> 3 controller chính: `UserDeviceController` (Core), `UserDeviceLoginExtendController` (Core, QuanTriArea), `UserDeviceLogController` (Authen). FE chia 2 module: `device-login` (duyệt thiết bị) và `caidat-hethong` (slot + log).

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Quản lý thiết bị đăng nhập | `device-login/danh-sach` | `DanhSachDeviceComponent` |
| Giới hạn / mở rộng slot đăng nhập | `caidat/login-limited` | `LoginLimitedComponent` (`login-limited-edit`) |
| Lịch sử (log) đăng nhập | `caidat/dangnhap-log` | `DangNhapLogComponent` (dùng chung từ `quantri/dangnhap-log`) |

- Route gốc: `app-routing.module.ts:435` (`device-login` → `DeviceLoginModule`), `:141` (`caidat` → `CaidatHethongModule`).
- Route con: `device-login-routing.module.ts:7` (`danh-sach`); `caidat-hethong-routing.module.ts:288` (`login-limited`), `:284` (`dangnhap-log`).
- FE service: `device-login/services/userdevice.service.ts`, `services/core/userdeviceloginextend.service.ts`, `services/core/userdevicelog.service.ts`.

## Endpoint / Controller (trace registry)

### Thiết bị — `UserDeviceController` (service Core)
> Registry: `registry/he-thong/UserDeviceController.md`. Route `api/UserDevice`, `[TienIch(..., TI_Chung)]`, state `[ChoDuyet, DaDuyet, HuyDuyet]`.

| MaTinhNang | Route | Mục đích |
|---|---|---|
| `UserDevice_TimTheoDieuKien` (DV-AD-TONG/CN/MCN) | `POST api/UserDevice/TimTheoDieuKien` | lưới thiết bị theo `UserDeviceFilterDto` + CurrentUser |
| `UserDevice_DuyetThietBi` | `POST api/UserDevice/DuyetThietBi` | cho phép thiết bị đăng nhập |
| `UserDevice_HuyDuyetThietBi` | `POST api/UserDevice/HuyDuyetThietBi` | thu hồi quyền thiết bị |
| `UserDevice_Xoa` | `POST api/UserDevice/Xoa/{id:Guid}` | xóa; gating `IdDonVi == CurrentUser.DonViId` |

### Slot đăng nhập — `UserDeviceLoginExtendController` (Core, Area SanboxArea)
> Registry: `registry/he-thong/UserDeviceLoginExtendController.md`. Route `api/QuanTriArea/UserDeviceLoginExtend`, `[TienIch(..., TI_HeThong, true)]`. Redis lock chống race. Key `UserDeviceLoginExtend_*` (HT-AD, HT-KT).

| MaTinhNang | Route | Mục đích |
|---|---|---|
| `_TimTheoDieuKien` | `POST .../TimTheoDieuKien` | lưới slot |
| `_TimTheoId` | `GET .../TimTheoId/{id:Guid?}` | chi tiết (⚠ gọi GetByIdAsync 2 lần) |
| `_ThemMoi` | `POST .../ThemMoi` | cấp slot (validate + Redis lock + hạn mức) |
| `_CapNhatThongTin` | `POST .../CapNhatThongTin` | sửa slot + `LogOutUserTheoSoLuong` |
| `_Xoa` | `POST .../Xoa/{id:Guid}` | xóa + logout thiết bị mở rộng |
| (login) | `GET .../GetTongUserThem/{id:Guid}` | `{TongDuocPhep, TongHienTai}` theo đơn vị |

### Log đăng nhập — `UserDeviceLogController` (service Authen)
> Registry: `registry/he-thong/UserDeviceLogController.md`. Route `api/UserDeviceLog`, ghi async bởi `AuthenService` mỗi login thành công.

| MaTinhNang | Route | Mục đích |
|---|---|---|
| `UserDeviceLog_TimTheoDieuKien` (login) | `POST api/UserDeviceLog/TimTheoDieuKien` | tra log; gating đơn vị (SuperUser/CSKH lọc tự do) |

## Entities
- **`UserDevice`** — IdDonVi + trạng thái duyệt (ChoDuyet/DaDuyet/HuyDuyet).
- **`UserDeviceLoginExtend`** — UserId/TongDeviceLogin/IdDonVi; ràng `DonVi.LoginOneDeviceExtend` (hạn mức tổng).
- **`UserDeviceLog`** — UserName, IpAddress, UserAgent (`Device:<deviceId>|<ua>`), TenDonVi, DonViId, NgayTao.

## Luồng code
- **Thiết bị:** controller → `IUserDeviceService` (EF). Xóa kiểm tra scope đơn vị thủ công (`UserDeviceController.cs:83`).
- **Slot:** controller → `IUserDeviceLoginExtendService` + `IUserService` + `IRedisCacheService` + `IRepository<UserDeviceLoginExtend>` & `<DonVi>`. ThemMoi/CapNhat: validate (≥1, user tồn tại, không trùng) → Redis lock 30s → check `SUM + mới ≤ LoginOneDeviceExtend` → Insert/Update → `LogOutUserTheoSoLuong`.
- **Log:** controller → `IUserDeviceLogService` + `IUserPrincipalService` (gating đơn vị) + AutoMapper. Lọc KeyWord/TuNgay/DenNgay(+1), sort NgayTao desc, paging.

## Bug-prone / lưu ý
- **`UserDeviceLoginExtendController.cs:52-53`** — `TimTheoId` gọi `GetByIdAsync(id)` 2 lần (lần đầu phí).
- **Lock kẹt 30s:** nếu Insert ném exception sau khi lấy lock, lock chỉ tự gỡ khi hết TTL 30s → thao tác kế tiếp phải chờ.
- **Hạn mức null** (`DonVi.LoginOneDeviceExtend == null`) → coi như 0, không cấp được slot — cần đảm bảo đơn vị đã set hạn mức theo gói.
- **`UserDeviceLogService.cs:82-89`** — nhánh `if(Sorts.Any())` và `else` đều OrderByDescending(NgayTao) → tham số Sorts của client bị bỏ qua (luôn sort theo ngày desc).
- **`UserDeviceLogService.cs:54-64`** — chỉ SuperUser/CSKH được lọc tự do theo IdDonVi; user thường khóa cứng `DonViId` của mình (đúng bảo mật đa-đơn-vị).
- **Log async** → có độ trễ giữa lúc login và lúc xuất hiện trong tra cứu.
- **`UserDeviceController.cs:29-41`** — endpoint `TimTheoId` bị comment (code cũ map sai `Role`→`RoleDto`).
- **Phân quyền vai trò / đồng bộ danh mục quyền** không nằm ở đây — xem `registry/he-thong/DongBoQuyenController.md` (super-admin). Lưu ý: `DongBoQuyenController.cs:199-220` try/catch nuốt lỗi → service down sẽ "mất quyền" thầm lặng.

## File source (path:line)
- BE thiết bị: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\Controllers\UserDeviceController.cs:83` (Xoa scope), `:29-41` (TimTheoId comment).
- BE slot: `...\Areas\SanboxArea\UserDeviceLoginExtendController.cs:52-53`, `:70/:77/:88/:106-110/:112-116`.
- BE log: `...\Authen\...\Controllers\UserDeviceLogController.cs`; service `Authen.Service/Generated/UserDeviceLogService.cs:54-64`, `:82-89`.
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\device-login\danhsach-device\danhsach-device.component.ts`; `...\caidat-hethong\login-limited\login-limited.component.ts`; `...\quantri\dangnhap-log\dangnhap-log.component.ts`.
- Registry: `projects/scrm/registry/he-thong/{UserDeviceController, UserDeviceLoginExtendController, UserDeviceLogController, DongBoQuyenController}.md`.
