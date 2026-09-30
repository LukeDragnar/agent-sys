---
controller: UserDeviceLogController
module: he-thong
service: Authen
man_hinh_ui: "Hệ thống > Quản lý đăng nhập (log thiết bị/IP đăng nhập): màn tra cứu lịch sử đăng nhập của nhân viên theo đơn vị, từ ngày/đến ngày, từ khóa."
muc_dich: "Tra cứu (phân trang) lịch sử log đăng nhập thiết bị của nhân viên: username, IP, user-agent, đơn vị, thời gian."
alias: ["log đăng nhập", "lịch sử đăng nhập", "quản lý login", "user device log", "nhật ký thiết bị đăng nhập"]
auth: internal
state_machine: []
deprecated: false
entities: [UserDeviceLog, User]
stored_procs: []
depends: ["SQL Server (AuthenConnectStr) — EF Core (UserDeviceLog)", "IUserPrincipalService (lấy UserLogin để gating theo đơn vị/super user)", "AutoMapper (UserDeviceLog → UserDeviceLogDto)"]
cross_module_in: ["Màn Quản lý đăng nhập (web) → TimTheoDieuKien"]
cross_module_out: []
---

# UserDeviceLogController — Tra cứu lịch sử đăng nhập thiết bị

Controller view/report cho log đăng nhập. Log được ghi async bởi `AuthenService` mỗi lần login thành công (`UserDeviceLog`: UserName, IpAddress, UserAgent="Device:<deviceId>|<ua>", TenDonVi, DonViId, NgayTao). Logic ở `IUserDeviceLogService` (`Authen.Service/Generated/UserDeviceLogService.cs`). Gắn `[TienIch]` (đăng ký menu tiện ích "Quản lý login"). Endpoint duy nhất có permission key.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm log đăng nhập | "Tìm kiếm" (grid) | POST `api/UserDeviceLog/TimTheoDieuKien` | `UserDeviceLog_TimTheoDieuKien` | login | Màn Quản lý login | ModelState invalid → báo lỗi model. Gating: nếu user `IsSuperUser`/`IsCSKH` → có thể lọc theo `IdDonVi` truyền lên; ngược lại ép `DonViId == userLogin.DonViId` (chỉ thấy log đơn vị mình). Lọc KeyWord (UserName/IP/TenDonVi), TuNgay/DenNgay (DenNgay +1 ngày). Sắp xếp `NgayTao` desc, phân trang Skip/Take, map sang `UserDeviceLogDto`, trả kèm total. | EF (UserDeviceLog) + AutoMapper | — |

## Quan hệ
- **Gọi sang:** `IUserPrincipalService.GetUserLogin` (gating đơn vị), AutoMapper.
- **Dữ liệu (entities):** `UserDeviceLog` (log đăng nhập, gắn `DonViId`/`TenDonVi`), join gián tiếp `User` qua service khác.
- **Ripple / đồng bộ:** chỉ đọc; không ghi.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserDeviceLogService.cs:82-89` — nhánh `if(Sorts.Any())` và `else` đều `OrderByDescending(NgayTao)` (giống nhau) → tham số Sorts của client thực tế bị bỏ qua, luôn sắp xếp theo ngày giảm dần.
- `UserDeviceLogService.cs:54-64` — chỉ SuperUser/CSKH mới được lọc tự do theo IdDonVi; user thường bị khóa cứng theo `DonViId` của mình (đúng theo bảo mật đa-đơn-vị).
- Log ghi async qua background queue trong `AuthenService.ProcecssLoginUser` → có độ trễ nhỏ giữa lúc login và lúc log xuất hiện trong tra cứu.
