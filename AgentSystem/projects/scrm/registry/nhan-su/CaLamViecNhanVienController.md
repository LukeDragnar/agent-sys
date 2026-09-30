---
controller: CaLamViecNhanVienController
module: nhan-su
service: Branch
man_hinh_ui: "Chấm công > Phân ca làm việc (gán nhân viên vào khung giờ/ca theo ngày); panel lịch cá nhân của NV"
muc_dich: "Quản lý phân ca làm việc cho nhân viên: gán/sửa/xóa ca theo nhân viên + khung giờ + ngày, kiểm tra ràng buộc khung giờ trước khi đổi, và cung cấp lịch ca cho NV/đơn hàng."
alias: [CaLamViecNhanVien, "phân ca", "ca làm việc nhân viên", "xếp ca", "lịch ca"]
auth: mixed
state_machine: []
deprecated: false
entities: [CaLamViecNhanVien]
stored_procs: []
depends: [KhungGioNhanVien, NhanVien/User (Core)]
cross_module_in: [Order/cua-hang (private: GetCaLamViecNhanVienByDay_ListUser_Private, GetStaffByWorkingTime — lấy NV có ca để xếp đơn/booking)]
cross_module_out: []
---

# CaLamViecNhanVien — Phân ca làm việc cho nhân viên

> Controller-level `[SBAuthorize]` (login-only). Một số action có key riêng; vài action commented key → fallback `[SBAuthorize()]` login-only (lưu/xóa ca). Có nhóm endpoint private service-to-service cho Order/Booking xếp nhân viên.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy ca của 1 NV theo Id | (tải khi mở NV) | GET `TimCaLamViecTheoIdNhanVien/{id}` | `CaLamViecNhanVien_TimCaLamViecTheoIdNhanVien` | public | — | Trả các ca làm việc đã gán cho nhân viên id. | EF | — |
| 2 | Lưu/cập nhật ca cho NV | Lưu | POST `CapNhatThongTin` | (key commented → login-only) | public | — | Validate model → upsert ca cho NV (theo khung giờ/ngày). | EF | Key bị comment → chỉ login-only, không phân quyền riêng |
| 3 | Lấy ca NV theo ngày | (lịch theo ngày) | POST `GetCaLamViecNhanVienByDay` | — | anonymous | — | `[AllowAnonymous]` — trả danh sách NV có ca trong ngày theo req. | EF | Anonymous: bất kỳ ai gọi được — lưu ý audit |
| 4 | Kiểm tra NV đang dùng khung giờ | (trước khi đổi khung giờ) | POST `CheckUserCaLamViecToChangeIsSuDungKhungGio` | `CaLamViecNhanVien_CheckUser...` | public | — | Kiểm tra có user nào đang gán lịch theo khung giờ sắp đổi → chặn đổi nếu đang dùng. | EF | — |
| 5 | Xóa ca của NV | Xóa | POST `DeleteUserCaLamViec` | (key commented → login-only) | public | — | Validate → xóa các ca của NV theo khung giờ truyền vào. | EF | Key bị comment → login-only |
| 6 | Lịch ca cá nhân của NV | Lịch của tôi | POST `GetLichLamViecCaNhan` | — | public | — | NV xem lịch làm việc của chính mình (theo CurrentUser). | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GetCaLamViecNhanVienByDay_ListUser_Private` (`[PrivateSBAuthorize]`) → đi qua `ICommonService.GetCaLamViecNhanVienTODAY_ListUser_Private` — caller lấy danh sách NV có ca hôm nay theo list user (Order/Booking xếp nhân viên).
- [internal] `GetStaffByWorkingTime` (`[PrivateSBAuthorize]`) → trả NV theo khung giờ (req `NhanVienByKhungGioReq`) — Order/Booking lấy NV đang trong khung giờ để gán.

## Quan hệ
- **Gọi sang:** Core/User (thông tin NV), KhungGioNhanVien (ràng buộc khung giờ).
- **Dữ liệu (entities):** `CaLamViecNhanVien` (gán NV ↔ khung giờ ↔ ngày).
- **Ripple/đồng bộ:** không phát event; là nguồn dữ liệu cho chấm công (GioLamViecNhanVien) và xếp nhân viên đơn/booking.

## Lỗi đã biết / lưu ý kỹ thuật
- `CaLamViecNhanVienController.cs:49` — `GetCaLamViecNhanVienByDay` để `[AllowAnonymous]`: gọi không cần login.
- `CaLamViecNhanVienController.cs:36,67` — key SBAuthorize bị comment ở `CapNhatThongTin`/`DeleteUserCaLamViec` → chỉ login-only, mất phân quyền role gốc.
- `CaLamViecNhanVienController.cs:90-99` — endpoint `GetDanhSachNhanVienCoCaLamHienTai_Private` đã comment toàn bộ (chết).
