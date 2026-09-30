---
controller: CauHinhController
module: cua-hang
service: Booking
man_hinh_ui: "Cửa hàng / Booking — màn 'Cấu hình booking / đặt lịch' (lưu cấu hình chung đặt lịch: khoảng cách khung giờ, max khách nhóm, quyền xem lịch cho thu ngân...; cấu hình check-in; role thao tác ngày làm việc). Cấu hình lưu 1 bản ghi/đơn vị dưới dạng JsonSetting. (LƯU Ý: trùng tên với CauHinhController của service PreOrder — file PreOrder ở CauHinhController.md; file này là bản Booking.)"
muc_dich: "Quản lý cấu hình của service Booking theo Đơn vị (1 bản ghi CauHinh/đơn vị, lưu JsonSetting): lưu/đọc cấu hình init đặt lịch, cấu hình check-in, và quyền xem lịch hẹn cho thu ngân (AllowAppointmentViewForCashier)."
alias: ["cấu hình booking", "cấu hình đặt lịch", "setting booking", "cấu hình check in", "quyền xem lịch thu ngân", "khoảng cách khung giờ đặt lịch"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [Booking (CauHinh, lưu JsonSetting), Redis/Cache (CK_CauHinhBookingSetting_), LogActivityHelper (LichSuTacDong)]
cross_module_in: [Booking.CommonService — gọi GetAsync(idDonVi) lấy DistanceTwoTimeFrame khi sinh khung giờ đặt lịch; consumer SaveCauHinhChung (đồng bộ cấu hình chung từ message broker)]
cross_module_out: [Cache cấu hình CK_CauHinhBookingSetting_{idDonVi} (clear mỗi lần lưu, TTL 1 ngày); màn đặt lịch / khung giờ đọc lại cấu hình]
---

# CauHinhController (Booking) — Cấu hình service Booking (đặt lịch / check-in)

> Trùng tên controller với `CauHinhController` của PreOrder (xem `CauHinhController.md`). Node này là bản service **Booking** (`Booking.API/Controllers/CauHinhController.cs`, TienIch `CauHinh_Booking_Controller`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình init đặt lịch | "Lưu cấu hình" | POST `api/CauHinh/SaveCauHinhInit` | `CauHinh_Booking_LuuCauHinhBooking` | public | màn cấu hình | Controller set `AllowAppointmentViewForCashierENDUSER = null` (không lưu DB); `Save<T>`: tìm bản ghi CauHinh theo IdDonVi → insert/update JsonSetting (merge field qua AutoMapper khi update); clear cache; ghi LichSuTacDong | EF (1 bản ghi/đơn vị, JsonSetting) | - |
| 2 | Lưu cấu hình check-in | "Lưu" | POST `api/CauHinh/SaveCheckIn` | `CauHinh_Booking_LuuCauHinhCheckIn` | public | màn check-in | `Save<CauHinhCheckInSettingDto>`: merge vào JsonSetting (chủ yếu `RoleDuocPhepThaoTacNgayLamViecVaKhungGio`); clear cache; log | EF | - |
| 3 | Lấy cấu hình check-in | load form | GET `api/CauHinh/GetCheckIn` | `[SBAuthorize]` (chỉ login) | public | - | `GetAsync(DonViId)` → trả `RoleDuocPhepThaoTacNgayLamViecVaKhungGio` | EF + cache | - |
| 4 | Lấy cấu hình init | load form | GET `api/CauHinh/GetCauHinhInit` | `[SBAuthorize]` | public | - | `GetAsync(DonViId)` → map sang `CauHinhSettingInitDto` | EF + cache | - |
| 5 | Lấy cấu hình (full) | load màn | GET `api/CauHinh/Get` | `[SBAuthorize]` | public | - | `GetAsync(user)`; nếu user là SuperUser/IsTest/CSKH/AdminTong/AdminCN/AdminMCN → `AllowAppointmentViewForCashierENDUSER = true`, ngược lại = `AllowAppointmentViewForCashier` | EF + cache | - |

## Endpoint nội bộ / public-anonymous
- [anonymous — `[AllowAnonymous]`] **`GetByDonViId/{id}`** (GET) — đọc cấu hình theo `idDonVi` truyền vào (không token); set `AllowAppointmentViewForCashierENDUSER = AllowAppointmentViewForCashier`. Dùng cho màn đặt lịch online / client end-user.
- [consumer/internal] **`SaveCauHinhChung`** (không qua controller này — gọi từ message broker `CauHinhChungMessage`) — đồng bộ cấu hình chung (che số điện thoại, role...) vào JsonSetting + log + clear cache.

## Quy tắc nghiệp vụ (cấu hình & cache)
- **1 bản ghi CauHinh / đơn vị**: tìm theo `IsDelete=false && IdDonVi`; chưa có → insert mới, có → update JsonSetting (merge bằng AutoMapper, giữ field cũ).
- **Validation lưu** (`ValidationSaveAsync`): `MaxCustomerInGroup > 999999` → "Số khách tối đa trong nhóm không được lớn hơn 999999". Lưu ý: chỉ áp cho `SaveAsync(CauHinhSettingDto)`, KHÔNG áp cho `SaveCauHinhInit`/`SaveCheckIn` (2 hàm này đi qua `Save<T>` không gọi validation).
- **Fallback đơn vị**: `GetAsync(idDonVi)` nếu DB + cache đều null và `idDonVi != Guid.Empty` → lấy bản ghi mặc định `IdDonVi = Guid.Empty` (cấu hình hệ thống chung).
- **Cache**: key `CK_CauHinhBookingSetting_{idDonVi}`, TTL 1 ngày; clear sau mỗi lần lưu.
- **`AllowAppointmentViewForCashierENDUSER`**: field tính runtime, KHÔNG lưu DB (controller set null trước khi lưu); giá trị trả phụ thuộc role người gọi.

## Quan hệ
- **Gọi sang:** không gọi service ngoài khi lưu/đọc; nhưng `Booking.CommonService` (LayCaLamViec...) GỌI SANG `CauHinhService.GetAsync(idDonVi)` để lấy `DistanceTwoTimeFrame` sinh các khung giờ đặt lịch.
- **Dữ liệu (entities):** `CauHinh` (IdDonVi, JsonSetting, IsDelete) — toàn bộ setting nằm trong cột JSON `JsonSetting`.
- **Ripple / đồng bộ:** clear cache cấu hình → màn đặt lịch / sinh khung giờ lấy lại cấu hình mới; `SaveCauHinhChung` (consumer) đồng bộ cấu hình che số/role từ service khác.

## Lỗi đã biết / lưu ý kỹ thuật
- **`Get` không null-check `rs.Data`** — `CauHinhService`/controller:79,83: truy cập `rs.Data.AllowAppointmentViewForCashierENDUSER`; nếu Data null → NRE (thực tế `GetAsync` trả object rỗng nên hiếm).
- **`GetByDonViId` ẩn danh, nhận idDonVi từ URL** — `CauHinhController.cs:92-106`: `[AllowAnonymous]` đọc cấu hình bất kỳ đơn vị theo Id → lộ cấu hình đặt lịch nếu biết idDonVi; cân nhắc giới hạn field trả về.
- **`SaveCauHinhInit`/`SaveCheckIn` KHÔNG chạy `ValidationSaveAsync`** — chỉ `SaveAsync` (không thấy controller gọi) mới validate; 2 endpoint UI thực tế đi qua `Save<T>` (CauHinhService.cs:307,324 → :385) bỏ qua validate `MaxCustomerInGroup`.
- **Controller-level auth đã comment** — `CauHinhController.cs:24` `[SBAuthorize("CauHinhController")]` bị comment; auth dựa vào base `[SBAuthorize]` + key từng method.
- Code thật: BE controller `Booking/Booking.API/Controllers/CauHinhController.cs`, service `Booking/Booking.Service/Generated/CauHinhService.cs`.
