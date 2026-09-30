---
controller: UserDeviceLoginExtendController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Quản lý user login (mở rộng số thiết bị): lưới user được cấp thêm slot đăng nhập đa thiết bị + Thêm/Sửa/Xóa, theo hạn mức đơn vị (LoginOneDeviceExtend)"
muc_dich: "Cấp/quản lý số lượng thiết bị đăng nhập đồng thời (slot) cho từng nhân viên trong hạn mức tổng của đơn vị; vượt hạn → chặn; sửa/xóa tự logout thiết bị thừa."
alias: ["UserDeviceLoginExtend", "mở rộng đăng nhập", "slot đăng nhập đa thiết bị", "login đa thiết bị", "quản lý user login"]
auth: mixed
state_machine: []
deprecated: false
entities: [UserDeviceLoginExtend, DonVi]
stored_procs: []
depends: ["IUserDeviceLoginExtendService", "IUserService", "IRedisCacheService", "IRepository<UserDeviceLoginExtend>", "IRepository<DonVi>", "BaseController"]
cross_module_in: ["FE Quản lý user login gọi TimTheoDieuKien/TimTheoId/ThemMoi/CapNhatThongTin/Xoa/GetTongUserThem"]
cross_module_out: ["Vượt hạn mức đơn vị (DonVi.LoginOneDeviceExtend) → chặn cấp; sửa/xóa → LogOutUserTheoSoLuong đăng xuất thiết bị thừa (ảnh hưởng session đăng nhập của nhân viên)"]
---

# UserDeviceLoginExtendController — Mở rộng slot đăng nhập đa thiết bị

Controller `partial` (route `api/QuanTriArea/UserDeviceLoginExtend`, Area SanboxArea, `[TienIch(..., TI_HeThong, true)]`). Cấp thêm số thiết bị đăng nhập đồng thời cho nhân viên, ràng buộc **tổng slot không vượt `DonVi.LoginOneDeviceExtend`**, dùng **Redis lock** chống race khi cộng dồn. Sửa/xóa gọi `LogOutUserTheoSoLuong` để logout thiết bị thừa. Key `UserDeviceLoginExtend_*` (HT-AD, HT-KT) → auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/QuanTriArea/UserDeviceLoginExtend/TimTheoDieuKien` | `UserDeviceLoginExtend_TimTheoDieuKien` (HT-AD, HT-KT) | mixed | — | Lọc theo `UserDeviceLoginExtendFilterDto`. | EF | — |
| 2 | Xem chi tiết | (mở form) | GET `api/QuanTriArea/UserDeviceLoginExtend/TimTheoId/{id:Guid?}` | `UserDeviceLoginExtend_TimTheoId` (HT-AD, HT-KT) | mixed | — | `GetByIdAsync(id)`. | EF | Gọi `GetByIdAsync` 2 lần (dòng 52-53), lần đầu bỏ phí |
| 3 | Thêm mới | Thêm | POST `api/QuanTriArea/UserDeviceLoginExtend/ThemMoi` | `UserDeviceLoginExtend_ThemMoi` (HT-AD, HT-KT) | mixed | Tổng slot ≤ hạn mức đơn vị | Validate `TongDeviceLogin ≥ 1`, user tồn tại, không trùng user trong đơn vị; Redis lock → tổng slot hiện tại + mới ≤ `DonVi.LoginOneDeviceExtend` → Insert. | EF + Redis | — |
| 4 | Cập nhật | Sửa | POST `api/QuanTriArea/UserDeviceLoginExtend/CapNhatThongTin` | `UserDeviceLoginExtend_CapNhatThongTin` (HT-AD, HT-KT) | mixed | Tổng slot ≤ hạn mức | Như ThemMoi (đổi user → user cũ về 0); Redis lock kiểm tra tổng; Update rồi `LogOutUserTheoSoLuong`. | EF + Redis | — |
| 5 | Xóa | Xóa | POST `api/QuanTriArea/UserDeviceLoginExtend/Xoa/{id:Guid}` | `UserDeviceLoginExtend_Xoa` (HT-AD, HT-KT) | mixed | — | Kiểm tra tồn tại → Delete → `LogOutUserTheoSoLuong(IdDonVi, UserId, 0)` đăng xuất hết thiết bị mở rộng. | EF | — |
| 6 | Lấy tổng slot được thêm | (hiển thị hạn mức) | GET `api/QuanTriArea/UserDeviceLoginExtend/GetTongUserThem/{id:Guid}` | — (login) | mixed | — | Trả `{TongDuocPhep = DonVi.LoginOneDeviceExtend, TongHienTai = SUM(TongDeviceLogin)}` theo đơn vị `id`. | EF | — |

## Quy tắc nghiệp vụ (gating / hạn mức)
1. ThemMoi: `TongDeviceLogin < 1` → lỗi "Vui lòng nhập giá trị > 0". (dòng 70)
2. ThemMoi: user không tồn tại → lỗi "Nhân viên không tồn tại". (dòng 77)
3. Thêm/Sửa: trùng user trong cùng `IdDonVi` (Id khác) → lỗi "Trùng lặp nhân viên. mời chọn nhân viên khác". (dòng 88, 159)
4. Redis lock `UserDeviceLoginExtend:{IdDonVi}:lock` (30s): không lấy được lock → "Đang có người thao tác vui lòng thử lại". (dòng 112-116, 184-188)
5. Hạn mức: `SUM(TongDeviceLogin các bản khác) + req.TongDeviceLogin > DonVi.LoginOneDeviceExtend` → lỗi "Tổng nhân viên được phép thêm không vượt quá {TongDuocPhep}" + gỡ lock. (dòng 106-110, 178-182)
6. CapNhat đổi user: user cũ về `tonguserduocphep = 0` → LogOutUserTheoSoLuong logout thiết bị thừa user cũ. (dòng 147-156, 192)

## Quan hệ
- **Gọi sang:** IUserDeviceLoginExtendService (Insert/Update/Delete/LogOutUserTheoSoLuong), IUserService (GetUserById), IRedisCacheService (lock), IRepository<UserDeviceLoginExtend> & IRepository<DonVi> (đếm tổng + hạn mức).
- **Dữ liệu (entities):** `UserDeviceLoginExtend` (UserId/TongDeviceLogin/IdDonVi), `DonVi.LoginOneDeviceExtend` (hạn mức tổng).
- **Ripple / đồng bộ:** sửa/xóa → logout thiết bị thừa của user (cắt session đăng nhập theo số slot mới).

## Luồng xuyên module
- Cấp/giảm slot → service `LogOutUserTheoSoLuong(IdDonVi, UserId, slotMoi)` → đăng xuất các thiết bị vượt quá slot ngay lập tức.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserDeviceLoginExtendController.cs:52-53` TimTheoId gọi `GetByIdAsync(id)` 2 lần (lần đầu gán vào `item` không dùng).
- ThemMoi: nếu vào nhánh `else` (lock không lấy được) trả lỗi nhưng **không gỡ lock** ở nhánh thành công của `if(lockAcquired)` khi tổng hợp lệ — lock được gỡ ở cuối (dòng 119), nhưng nếu Insert ném exception thì lock kẹt tới 30s TTL.
- Phụ thuộc `DonVi.LoginOneDeviceExtend` — đơn vị chưa cấu hình (null) → coi như 0 (không cấp được slot nào).
