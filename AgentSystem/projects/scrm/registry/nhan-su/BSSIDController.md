---
controller: BSSIDController
module: nhan-su
service: Branch
man_hinh_ui: "Chấm công > Wifi BSSID (danh sách BSSID theo chi nhánh + thêm/sửa/xóa/phê duyệt); app NV: kiểm tra wifi BSSID khi chấm công"
muc_dich: "Đăng ký & phê duyệt BSSID wifi của chi nhánh để chấm công theo wifi: CRUD BSSID, phê duyệt (toggle IsDuyet), và CheckBSSID cho app NV xác minh wifi đã đăng ký + đã duyệt."
alias: [BSSID, "wifi chấm công", "BSSID wifi", "wifi BSSID", "chấm công wifi", "đăng ký wifi"]
auth: mixed
state_machine: [ChuaDuyet, DaDuyet]
deprecated: false
entities: [BSSID]
stored_procs: []
depends: [CauHinhService (RoleDuocPhepThaoTacThietBiWifi), Core (ChiNhanh GetChiNhanhTheoDonVi), LogActivityHelper (lịch sử tác động)]
cross_module_in: [App chấm công NV (CheckBSSID khi check-in/out theo wifi)]
cross_module_out: [Core (GetChiNhanhTheoDonVi để denormalize tên chi nhánh), Log (LichSuTacDong)]
---

# BSSID — Đăng ký & phê duyệt wifi BSSID chấm công

> `[TienIch(...TI_ChamCong)]`. Kế thừa `BaseController`. Phân quyền hỗn hợp: chỉ 2 action đọc (`TimTheoDieuKien`, `TimTheoId`) còn giữ key `BSSID_*`; các action ghi (ThemMoi/CapNhat/Xoa/PheDuyet) đã **comment key**, để `[SBAuthorize()]` rỗng → login-only, role thực thi **inline** qua `cauHinh.RoleDuocPhepThaoTacThietBiWifi`. CheckBSSID `[SBAuthorize]` cho mọi NV login.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm BSSID theo điều kiện | Danh sách/Lọc | POST `TimTheoDieuKien` | `BSSID_TimTheoDieuKien` | public | Quản lý (AD/QLDV/HR) | Lọc BSSID theo chi nhánh user (keyword/IdChiNhanh/IsDuyet) + denormalize tên CN. | EF + Core | Luôn ép `IdChiNhanh == user.IdChiNhanh` |
| 2 | Thêm mới BSSID | Lưu | POST `ThemMoi` | `[SBAuthorize()]` rỗng | mixed | Quản lý | Role inline (`RoleDuocPhepThaoTacThietBiWifi`); validate trùng Value/CN; IsDuyet mặc định false. | EF | Key comment dòng 46; role inline |
| 3 | Cập nhật BSSID | Lưu | POST `CapNhat` | `[SBAuthorize()]` rỗng | mixed | Quản lý | Role inline; chỉ sửa bản ghi thuộc CN+đơn vị user; ghi LichSuTacDong. | EF | Key comment dòng 33 |
| 4 | Xóa BSSID | Xóa | GET `Xoa/{id:Guid}` | `[SBAuthorize()]` rỗng | mixed | Quản lý | Role inline; xóa bản ghi thuộc CN user; ghi log DELETE. | EF | GET cho xóa; key comment dòng 59 |
| 5 | Phê duyệt BSSID | Phê duyệt | GET `PheDuyet/{id:Guid}` | `[SBAuthorize()]` rỗng | mixed | Quản lý, bản ghi thuộc CN | **Toggle** `IsDuyet = !IsDuyet` (duyệt/bỏ duyệt); ghi log. | EF | GET; toggle 2 chiều; key comment dòng 72 |
| 6 | Tìm BSSID theo Id | Mở edit | GET `TimTheoId/{id:Guid}` | `BSSID_TimTheoId` | public | Quản lý | Trả chi tiết BSSID thuộc CN+đơn vị user. | EF | — |
| 7 | Kiểm tra BSSID (app NV) | (nền chấm công) | POST `CheckBSSID` | `[SBAuthorize]` login-only | mixed | NV chấm công | Trả `IsExisted`/`IsApproved`: tìm theo Value HOẶC Ten trong CN; tồn tại + IsDuyet → approved. | EF | Key comment dòng 98 → mọi NV login |

## Quy tắc nghiệp vụ (gating / state)
- **State duyệt:** mỗi BSSID có `IsDuyet` (false=Chưa duyệt, true=Đã duyệt). `PheDuyet` là **toggle** (gọi lần nữa = bỏ duyệt). Mặc định khi thêm/sửa = false nếu null.
- **Role thao tác (inline):** ThemMoi/CapNhat/Xoa/PheDuyet đều check `CommonService.CheckRoleThaoTac(user, cauHinh.RoleDuocPhepThaoTacThietBiWifi)` trước → fail trả "Bạn không có quyền thực hiện thao tác này". KHÔNG dựa vào permission key (đã comment).
- **Scope chi nhánh:** mọi thao tác (kể cả admin — nhánh `IsAdmin` đã comment) đều ép `IdChiNhanh == user.IdChiNhanh && IdDonVi == user.DonViId`. Validate trùng theo `Value` trong cùng (CN, đơn vị).
- **CheckBSSID:** non-admin ép `IdChiNhanh = user.IdChiNhanh`; match `Value == bssid || Ten == bssid`; không tìm thấy → IsExisted=false.

## Luồng xuyên module
- App chấm công NV gọi `CheckBSSID` (truyền WifiBssid) → quyết định cho phép chấm công wifi hay không (chỉ pass khi BSSID đã đăng ký + đã duyệt trong CN).
- Mọi thao tác ghi/xóa/duyệt ghi **LichSuTacDong** (`SCRM_MAJOR_BSSID`) sang Log. Đọc danh sách gọi Core `GetChiNhanhTheoDonVi` để hiện tên CN.

## Lỗi đã biết / lưu ý kỹ thuật
- `BSSIDController.cs:34,47,60,73,100` — ThemMoi/CapNhat/Xoa/PheDuyet/CheckBSSID để `[SBAuthorize()]` rỗng (key gốc bị comment ngay trên) → login-only, phân quyền chuyển hẳn sang role inline `RoleDuocPhepThaoTacThietBiWifi`.
- `BSSIDController.cs:61,74` — `Xoa` và `PheDuyet` dùng HTTP GET.
- `BSSIDService.cs:123` — `PheDuyet` là **toggle** `IsDuyet = !IsDuyet` (không phải set cứng true) — gọi 2 lần quay về chưa duyệt.
- `BSSIDService.cs:71,119,173,236,294` — nhánh `IsAdmin` (cho admin xem toàn đơn vị) đã bị comment → admin cũng bị giới hạn theo `IdChiNhanh` của mình.
