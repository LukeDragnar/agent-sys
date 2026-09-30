# Dev — Tổng đài (Voice IP / Sim số / IP mới)

> Service: **Switchboard** (BE .NET). 3 phiên bản trộn trong cùng service → nhiều controller trùng tên / bản cũ comment. Gốc registry: nhiều node `registry/sales/`. Chỉ dành cho dev.

## Phân biệt 3 phiên bản (QUAN TRỌNG)

| Phiên bản | Controller chính (active) | Controller cũ/deprecated | TienIch |
|---|---|---|---|
| **(1) Voice IP cũ** | `TongDaiDonViController`, `TongDaiUserController`, `TongDaiGoiDichVuController` | `SwitchboardController` (deprecated — endpoint comment), `CallHistoryController` (deprecated — MyHub v1, comment) | — |
| **(2) Sim số (MyHub)** | `MyHubController` (ở `Controllers_MyHub`), `MyHubGroupController`, `MyHubHotlineController`, `MyHubUserSwitchboardController`, `MyHubCallHistoryController` | `Controllers\MyHubController` (rỗng), endpoint nghe ghi âm trong MyHubCallHistory comment | `TI_TongDai_SimSo` |
| **(3) IP mới (Omicall)** | `TongDaiIpDonViController`, `TongDaiIpUserController`, `TongDaiIpCuocGoiController`, `TongDaiLichSuCuocGoiController`, `TongDaiIpDonViSAController` (super-admin duyệt) | — | — |

> ⚠️ Có nhiều node `_Switchboard` (vd `MyHubController_Switchboard.md`, `GroupController_Switchboard.md`...) — là biến thể/trùng tên khác service; khi trace controller tổng đài, ưu tiên node không hậu tố cho hành vi UI thật.

## Màn hình & route (FE)
- FE module tổng đài: `scrm_frontend/src/app/pages/switchboard/`. Route con xác nhận trong `switchboard-routing.module.ts`.
- Bản IP: cấu hình đơn vị (khởi tạo, nạp tiền QR, kịch bản, greeting, nhóm nội bộ, auto-call), quản lý user, lịch sử/báo cáo cuộc gọi.
- Bản sim số: tổng đài đơn vị (active), hotline, user, lịch sử cuộc gọi.
- Super-admin: phê duyệt giao dịch nạp tiền (quantri/he-thong — TongDaiIpDonViSA).

## Endpoint / Controller (trace registry)

### (3) IP mới — `TongDaiIpDonViController` (~40 endpoint)
- Khởi tạo: `KhoiTaoTongDaiIpDonVi` (key `TongDaiIpDonVi_KhoiTaoTongDaiIpDonVi`), `KiemTraDonViKhoiTao` (`[SBAuthorize]` login).
- Nạp tiền/giao dịch (state machine `GiaoDich_ChoThanhToan/DaThanhToan/HuyThanhToan`): `NapTienTongDai`, `GetThongTinQrTongDaiDonVi`, `ThanhToanGiaoDich`, `TongDaiDonViHuyThanhToan`, `GetLichSuThanhToanDonVi`, `XoaGiaoDichThanhToan`.
- Cấu hình: `CRMUnit_CallScipt*` (call script), `CRMUnit_*KeyInteraction` (IVR), `CRMUnit_Greeting*` (greeting), `CRMUnit_*InternalGroup` (nhóm nội bộ), `CRMUnit_*Hotline`, đầu số.
- Auto-call: `GetDanhSachPhien`, `TaoPhienCuocGoi`, `CapNhatPhienCuocGoi`, `GetChiTietPhien`, `XoaPhienCuocGoi`, `CapNhatContactKhiAcceptAutoCall` (ripple Contact).
- User: `TongDaiIpUserController` — `MapUserDonViVoiUserTongDai`, `ThayDoiUserTongDaiIp`, `CalculateSoTienKhiThayDoiUser`, `CapNhatUserXoaTongDai`, `DonViDangKyDauSo`.
- Cuộc gọi: `TongDaiIpCuocGoiController` — `LayDanhSachCuocGoi`, `LayDanhSachBaoCaoCuocGoi`, `ExportLichSuCuocGoi` (503 `EXPORT_ERROR`); `TongDaiLichSuCuocGoiController` — `TaoLichSuCuocGoi`, `GetListCallHistory`, `GetListThongKeCuocGoi`, `GetListCallHistoryByIds`, private `GetLichSuCuocGoiOmi`.

### (2) Sim số — MyHub
- `MyHubController` (Controllers_MyHub): `ReceiveDataHotline`/`ReceiveData` (webhook anonymous), `ClickToCall` (key `MyHub_ClickToCall`).
- `MyHubGroupController`: `GetTongDaiActive`, `TimTheoDieuKien`, `ChangeActiveTongDai/{id}`.
- `MyHubHotlineController`: `GetHotline`, `GetHotlineByIdUserPartner`, `DeleteHotline`.
- `MyHubUserSwitchboardController`: `TimTheoDieuKien`, `GetTongDaiSimSo`, `ExportExcelTongDaiSimSo`, `ChangeUser`, `GetById/{id}`.
- `MyHubCallHistoryController`: `GetHistoryByCustomer`, `TimTheoDieuKien` (auto-scope chi nhánh nếu !IsAdmin).

### (1) Voice IP cũ
- `TongDaiDonViController`: `TongDaiDonViTaoMoi/CapNhat/KetNoi`, `TongDaiDangKyWebhook`, `TongDaiGetBalance`, `TongDaiNapTienDonVi`, `TongDaiDuyetSoDuDonVi`, `TongDaiXoaLenhNapTien`. (`TongDaiDonViDanhSach` auto-scope đơn vị nếu không super/HT/CSKH.)
- `TongDaiUserController`: `TongDaiUserTaoMoi`, `GetListUserConnective`, `ChangeUserConnective`, `TongDaiUserChiTiet`.
- `TongDaiGoiDichVuController`: `TongDaiGoiDichVuDanhSach`, `TongDaiGiaHanGoiDichVu`, `TongDaiNangCapGoiDichVu` (HT-AD/HT-KT).
- **Đăng ký tổng đài cấp hệ thống — `SanboxArea` (active, KHÔNG dùng SwitchboardController)**: `GroupController` (`api/SanboxArea/Group`) — `RegisterGroup`, `UpdateGroup`, `ChangePasswordGroup`, `GetAllTongDaiDonVi/{id}`, `GetTongDaiActive` (HT-AD/HT-KT); `UserSwitchboardController` (`api/SanboxArea/UserSwitchboard`) — `RegisterUser`, `UpdateUser`, `DeleteUser/{id}`, `ChangeTrangThai/{id}`, `ChangePassword` (HT-KT/HT-AD, auto-scope đơn vị); `HotlineController` (`api/SanboxArea/Hotline`) — `GetHistoryActiveHotline`, `ExtendTimeHotline` (HT-KT/HT-AD). Đây là controller đăng ký/quản lý tổng đài thật, thay cho `SwitchboardController` đã chết.
- **Cấu hình đầu số — `CauHinhDauSoController`** (FE `switchboard/cauhinh-dauso`): `Create`/`Update`/`Delete/{id}`/`GetById/{id}`/`TimTheoDieuKien` (DV-AD-TONG), `CheckCallReceivingPermissions` (`[SBAuthorize]` login), private `GetAndSetCauHinhDauSoCache/{idDonVi}` (`[PrivateSBAuthorize]`).
- `SwitchboardController` + `CallHistoryController`: **DEPRECATED** (endpoint comment). Lưu ý: đăng ký tổng đài KHÔNG còn đi qua `SwitchboardController` mà qua `GroupController`/`UserSwitchboardController` (SanboxArea) + `TongDaiDonViController`/`TongDaiIpDonViController`.

## Entities
- IP: `TongDaiIpDonVi`, `GiaoDichTongDai`, `DauSoTenant`, `Hotline`, `CallScript`, `InternalGroup`, `Greeting`, `KeyInteraction`, `PhienCuocGoi`, `TongDaiLichSuCuocGoi`, `CallHistoryOmicall`, `TongDaiIpUser`.
- Sim số: `Group`, `Hotline`, `UserSwitchboard`/`TongDaiSimSo`, `CallHistory`.
- Cũ: `TongDaiDonVi`, `TongDaiUser`/`UserConnective`, `TongDaiGoiDichVu`/`ServicePackage`, `LichSuNapTien`, `Switchboard`.

## Luồng code (ripple đáng chú ý)
1. **Auto-call → Contact**: `CapNhatContactKhiAcceptAutoCall` (TongDaiIpDonVi) chạm module Contact (chia contact theo SĐT cho sale accept).
2. **Nạp tiền IP → super-admin**: giao dịch ChoThanhToan ↔ `TongDaiIpDonViSAController.DuyetThanhToanNapTienTongDaiSA` (cross-module duyệt).
3. **Cấu hình → tenant external**: call script/greeting/key interaction/user/đầu số đẩy lên Omicall/MyHub partner qua service.
4. **Lịch sử Omi → Contact/CSKH**: `GetLichSuCuocGoiOmi` (private) cho contact đọc lịch sử omicall theo contact.

## Bug-prone / lưu ý (path:line — KHÔNG sửa code ở session này)
- `TongDaiIpDonViController.cs:503-572` — **toàn bộ nhóm auto-call** (`GetDanhSachPhien`, `TaoPhienCuocGoi`, `CapNhatPhienCuocGoi`, `GetChiTietPhien`, `XoaPhienCuocGoi`, `CapNhatContactKhiAcceptAutoCall`) chỉ `[SBAuthorize()]` rỗng — không permission key, dù có side-effect chia contact.
- `TongDaiIpDonViController.cs:425-440` — `CRMUnit_GreetingUpload` validate chỉ `.mp3`, file null/rỗng → 400.
- `TongDaiLichSuCuocGoiController.cs:25-26` — `TaoLichSuCuocGoi` key gốc `TongDaiDonVi_TaoLichSuCuocGoi` đã comment → còn `[SBAuthorize]` rỗng (ai login cũng tạo lịch sử cuộc gọi).
- `TongDaiLichSuCuocGoiController.cs:43-49,69-75` — `GetListThongKeCuocGoi` & `GetListCallHistoryByIds` KHÔNG truyền `CurrentUser` → không scope user/đơn vị (phụ thuộc filter client → rủi ro lộ dữ liệu).
- `TongDaiIpCuocGoiController.cs:69-77` — `ExportLichSuCuocGoi` lỗi trả `StatusCode(503)` body `Code="EXPORT_ERROR"` (không phải 500).
- `MyHubUserSwitchboardController.cs:48-78` — `ExportExcelTongDaiSimSo` phân biệt 503 (`Service_Unavailable`) vs 500 cùng code `EXPORT_ERROR`.
- `MyHubController.cs (Controllers_MyHub):37,116` — `ReceiveDataHotline`/`ReceiveData` **anonymous webhook** (không auth) → bảo mật phải dựa IP whitelist/validate signature. Trùng tên `Controllers\MyHubController` (rỗng) cùng route `api/MyHub` → lưu ý conflict map.
- `MyHubHotlineController.cs:25,48` — `GetHotline`/`DeleteHotline` key role comment; `DeleteHotline` dùng `[SBAuthorize(true)]` (cờ, không key) → phân quyền xóa hotline lỏng.
- `MyHubCallHistoryController.cs:47-72` — endpoint nghe ghi âm/quản lý (`GetCallHistoryWithConditions`, `..._Manager`) đã comment → không nghe ghi âm qua màn này.
- `TongDaiUserController.cs:27-39,41-43` — `TongDaiUserDanhSach` comment; `TongDaiUserLayAllUserPartner` key role comment → `[SBAuthorize]` rỗng.
- `TongDaiIpUserController.cs:66-73` — `ThayDoiUserTongDaiIp` có `//TODO` (logic chưa chốt); `:75-99` 2 endpoint job/test comment.
- `SwitchboardController.cs:9,18-44` & `CallHistoryController.cs:19-58` — **DEPRECATED** (toàn bộ comment); giữ node chỉ để map alias, đừng gọi.

## File source (path:line)
- BE service Switchboard: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\` (folder service tổng đài). Controllers: `TongDaiIpDonViController.cs`, `TongDaiIpUserController.cs`, `TongDaiIpCuocGoiController.cs`, `TongDaiLichSuCuocGoiController.cs`, `Controllers_MyHub\MyHubController.cs`, `MyHubGroupController.cs`, `MyHubHotlineController.cs`, `MyHubUserSwitchboardController.cs`, `MyHubCallHistoryController.cs`, `TongDaiDonViController.cs`, `TongDaiUserController.cs`, `TongDaiGoiDichVuController.cs`, `SwitchboardController.cs`, `CallHistoryController.cs`.
- Registry nodes: `projects/scrm/registry/sales/TongDaiIpDonViController.md`, `TongDaiIpUserController.md`, `TongDaiIpCuocGoiController.md`, `TongDaiLichSuCuocGoiController.md`, `TongDaiIpDonViSAController.md`, `CauHinhDauSoController.md`, `MyHubController.md`, `MyHubGroupController.md`, `MyHubHotlineController.md`, `MyHubUserSwitchboardController.md`, `MyHubCallHistoryController.md`, `TongDaiDonViController.md`, `TongDaiUserController.md`, `TongDaiGoiDichVuController.md`, `GroupController.md` (SanboxArea active), `UserSwitchboardController.md` (SanboxArea active), `HotlineController.md` (SanboxArea active), `SwitchboardController.md` (deprecated), `CallHistoryController.md` (deprecated). Lưu ý các node hậu tố `_Switchboard` (`GroupController_Switchboard.md`, `HotlineController_Switchboard.md`, `UserSwitchboardController_Switchboard.md`, `MyHubController_Switchboard.md`) là bản trùng tên RỖNG trong folder `Controllers` — không dùng.
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\switchboard\`.
