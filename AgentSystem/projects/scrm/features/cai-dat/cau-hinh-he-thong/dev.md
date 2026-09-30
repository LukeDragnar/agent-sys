# Dev — Cấu hình hệ thống

> Khu **Cài đặt** của đơn vị (FE module `caidat-hethong`, route gốc `caidat/*`). Gom nhiều màn cấu hình. Tài liệu này tập trung 4 nhóm: tham số chung (Cấu hình chức năng), chi nhánh, tài khoản ngân hàng, bảo mật.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Cấu hình chức năng (tham số chung) | `caidat/he-thong` | `ConfigsystemComponent` (tabset → `app-config-chung`…) |
| Chi nhánh đơn vị | `caidat/chinhanh` | `ChinhanhDonviComponent` |
| Thông tin chung đơn vị | `caidat/thongtinchung` | `DonviInfoComponent` |
| Cài đặt bảo mật | `caidat/cai-dat-bao-mat` | `UserSettingListComponent` |
| Lịch sử đổi mật khẩu | `caidat/lich-su-doi-mat-khau` | `LichSuDoiMatKhauComponent` |

- Route gốc module: `app-routing.module.ts:141` (`path: 'caidat'` → lazy `CaidatHethongModule`).
- Khai báo route con: `caidat-hethong-routing.module.ts` (he-thong `:111`, chinhanh `:115`, cai-dat-bao-mat `:228`).
- Tab Cài đặt chung chỉ hiện cho `userLogin.isSuperUser || userLogin.isHeThongCSKH() || userLogin.isAdmin` (`configsystem.component.html:8`).
- Màn tài khoản ngân hàng: FE không nằm trong `caidat-hethong` (nhúng trong cấu hình kế toán/thanh toán); BE controller là nguồn chuẩn cho hành vi.

## Endpoint / Controller (trace registry)

### Tài khoản ngân hàng — `TkNganHangHeThongController` (service WebhookBank / Payment.API)
> Registry: `projects/scrm/registry/ke-toan/TkNganHangHeThongController.md`. Class-level `[SBAuthorize()]` (yêu cầu đăng nhập), key chi tiết từng method. Bản trùng tên trong Core đã **bị comment toàn bộ** (xem `registry/he-thong/TkNganHangHeThongController_Core.md`, deprecated) — đừng nhầm.

| MaTinhNang | Route | Mục đích |
|---|---|---|
| `TkNganHangHeThong_TimTheoDieuKien` | `POST .../TimTheoDieuKien` | lưới TK ngân hàng |
| `TkNganHangHeThong_ThemMoi` / `_CapNhatThongTin` / `_Xoa` | `POST .../ThemMoi`, `/CapNhatThongTin`, `/Xoa/{id}` | CRUD (Xóa chặn nếu `KiemTraNganHangDaSuDung`) |
| `TkNganHangHeThong_SetMacDinh` | `GET .../SetMacDinh/{id:Guid}` | đặt mặc định cho CurrentUser |
| `_GetListNganHangByType` | `GET .../GetListNganHangByType/{type}` | dropdown theo loại |
| (login) | `GET .../GetListTaiKhoanByChiNhanh[_V2]`, `/GetListTaiKhoanByDonVi`, `/GetThongTinNganHangByIdChiNhanh` | danh sách theo CN/đơn vị |
| (login) | `GET .../GetNganHangThuMacDinh`, `/GetNganHangChiMacDinh`, `/CheckKetNoiOnline/{id}`, `/CheckKetNoiThuOnline/{idNganHang}` | TK mặc định thu(0)/chi(1) + check terminal |
| `_TimTheoId` | `GET .../TimTheoId/{id:Guid}` | non-admin lọc theo chi nhánh |
| `[PrivateSBAuthorize]` | `POST GetAllByDonViPrivateAsync`, `GetByIdPrivateAsync`, `GetTkNganHangByIdChiNhanhs` | service-to-service |
| **`[AllowAnonymous]`** | `GET TimTheoTerminalId/{terminalId}` | ⚠️ trả TK ngân hàng theo TerminalId KHÔNG cần auth |

### Chi nhánh — `ChiNhanhController` (service Core)
> Registry: `registry/he-thong/ChiNhanhController.md`. **2 class trùng tên:** bản app (`api/ChiNhanh`, role đơn vị) và bản quản trị (`api/QuanTriArea/ChiNhanh`, HT-AD/HT-KT).

| MaTinhNang | Route | Mục đích |
|---|---|---|
| `ChiNhanh_TimTheoDieuKien` | `POST api/ChiNhanh/TimTheoDieuKien` | lưới (non-super ép `IdDonVi=CurrentUser.DonViId`) |
| `ChiNhanh_TimTheoId` | `GET api/ChiNhanh/TimTheoId/{id}` | chi tiết |
| `ChiNhanh_ThemMoi`/`_CapNhatThongTin`/`_Xoa` | `POST .../ThemMoi`, `/CapNhatThongTin`, `/Xoa/{id}` | CRUD (ThemMoi ép `IdDonVi`) |
| nhiều key dropdown | `LayListChiNhanh`, `LayListChiNhanhTheoRole`, `GetChiNhanhTheoDonVi`… | combobox theo role |

### Bảo mật — `UserBaoMatController` (service Core)
> Registry: `registry/he-thong/UserBaoMatController.md`. Route `api/UserBaoMat`, key `UserBaoMat_YeuCauDoiMatKhau` (DV-AD-TONG/CN/MCN).

| MaTinhNang | Route | Mục đích |
|---|---|---|
| `UserBaoMat_YeuCauDoiMatKhau` | `POST api/UserBaoMat/YeuCauDoiMatKhau` | đặt cờ buộc đổi MK cho user mục tiêu (`ReqYeuCauDoiMatKhauDto`) |

## Entities
- **`TkNganHangHeThong`** — BankCode/SoTaiKhoan/TerminalId/IsKetNoiTerminal + gắn chi nhánh/đơn vị; mặc định thu/chi.
- **`ChiNhanh`** — chi nhánh của đơn vị (IdDonVi, mã, tên, location).
- **`User`** — cờ buộc đổi mật khẩu (UserBaoMat tác động).

## Luồng code
- **TK ngân hàng:** controller → `TkNganHangHeThongService`. Xóa gọi `KiemTraNganHangDaSuDung` trước. `SetMacDinh` gắn TK mặc định cho `CurrentUser`. `TimTheoId` lọc chi nhánh cho non-admin (service `WebhookBank/Payment.API/Controllers/TkNganHangHeThongController.cs:97-99`).
- **Chi nhánh:** controller → `IChiNhanhService`. Bản app `TimTheoDieuKien` ép `req.IdDonVi` cho non-super; bản quản trị `Xoa` dùng `DeleteAsync` còn bản app dùng `Xoa(id, user)`.
- **Bảo mật:** controller → `IUserBaoMatService.YeuCauDoiMatKhau` đặt cờ buộc đổi MK; check quyền tác động lên user nào nằm trong service.

## Bug-prone / lưu ý
- **[BẢO MẬT — phơi dữ liệu]** `WebhookBank/Payment.API/Controllers/TkNganHangHeThongController.cs:184-191` — `[AllowAnonymous] TimTheoTerminalId/{terminalId}` (đè lên `[PrivateSBAuthorize]` đã comment) → ai biết/đoán TerminalId tra được STK & chủ tài khoản mà KHÔNG cần đăng nhập. Nên đổi về `[PrivateSBAuthorize]`. (Không sửa trong session này — để session security xử lý.)
- **Chi nhánh ép `IdDonVi` 2 lần** `Controllers/ChiNhanhController.cs:34,36` → nhánh check role thừa, non-super luôn bị giới hạn về DonViId của mình (đúng nghiệp vụ nhưng code dư).
- **`ChangeLocationChiNhanh`** `Controllers/ChiNhanhController.cs:195` là `[AllowAnonymous]` nhưng GHI dữ liệu vị trí chi nhánh → cần xác minh cơ chế bảo vệ khác.
- **Nhiều endpoint dropdown chi nhánh** chỉ `[SBAuthorize]` không permission key → không phân quyền chi tiết.
- **Tab cấu hình chung** chỉ render khi `isSuperUser/isHeThongCSKH/isAdmin` — nhân viên thường không thấy (đúng chủ ý), không phải bug.

## File source (path:line)
- BE TK ngân hàng (bản hoạt động): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\WebhookBank\Payment.API\Controllers\TkNganHangHeThongController.cs` (`:97-99` lọc chi nhánh, `:184-191` AllowAnonymous TimTheoTerminalId).
- BE TK ngân hàng (bản Core deprecated, comment): `...\scrm\<Core>\...\TkNganHangHeThongController.cs:13-137`.
- BE Chi nhánh: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\Controllers\ChiNhanhController.cs:34-36`, `:195`; bản QuanTriArea `Areas/SanboxArea/ChiNhanhController.cs`.
- BE Bảo mật: `...\scrm\...\Controllers\UserBaoMatController.cs`.
- FE module: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\caidat-hethong\caidat-hethong-routing.module.ts`.
- FE config: `...\caidat-hethong\configsystem\configsystem.component.html:8`.
- FE bảo mật: `...\caidat-hethong\user-setting\user-setting.component.ts:27`.
- Registry: `projects/scrm/registry/ke-toan/TkNganHangHeThongController.md`, `registry/he-thong/{ChiNhanhController,UserBaoMatController,TkNganHangHeThongController_Core}.md`.
