# Dev — Quản lý cấu hình SMS (duyệt đơn vị dùng tài khoản SMS)

> Service: **SMS** (BE .NET) · FE module **quantri** (Angular 15 + ng-zorro). Trace registry: `registry/marketing/AccountSmsUnitUsedController.md`. Phần cấu hình gateway: `registry/he-thong/ConfigSMSController.md` (xem mục "Cấu hình gateway/nhà cung cấp SMS (ConfigSMS)" cuối file — bổ sung Pha 2).

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Quản lý phê duyệt tài khoản cấu hình hệ thống | `/quantri/quanly-capphep-taikhoan-guitin-nangcao` | `CauhinhCapphepTaikhoanComponent` |

- Title màn: "Quản lý phê duyệt tài khoản cấu hình hệ thống".
- Grid cột: `idDonVi`, `idChiNhanh`, `messageType`, `tenTaiKhoan`, `isUseDefaultAccepted` (3 trạng thái), `ngayCapNhat`.
- **Lưu ý:** màn FE này gọi `AccountSmsService.ListAccountSmsUseDefault` / `ChangeAcceptAccount` (`services/sms/accountsms.service`), KHÔNG trực tiếp gọi `AccountSmsUnitUsedService`. FE có sẵn `AccountSmsUnitUsedService` (`TimTheoDieuKien`/`ChangeAccept`/`IsAccepted`) trỏ `api/AccountSmsUnitUsed/*` — controller registry trace. Cần product/dev xác nhận màn nào dùng controller nào (có thể có 2 đường: cấu hình chung + AccountSmsUnitUsed). Trace registry chuẩn là controller `AccountSmsUnitUsed`.

## Endpoint / Controller (trace registry)
`AccountSmsUnitUsedController` (SMS service, route `api/AccountSmsUnitUsed`, controller-level `MaTienIchSCRM.TI_HeThong`):
| # | Hành động | Route | Quyền (key) | State |
|---|---|---|---|---|
| 1 | Tìm theo điều kiện | `POST .../TimTheoDieuKien` | `AccountSmsUnitUsed_TimTheoDieuKien` (DV-AD-TONG, HT-KT) | — |
| 2 | Thêm mới | `POST .../ThemMoi` | `AccountSmsUnitUsed_ThemMoi` (DV-AD-TONG, HT-KT) | tạo bản ghi |
| 3 | Chi tiết | `GET .../TimTheoId/{id}` | `AccountSmsUnitUsed_TimTheoId` (DV-AD-TONG, HT-KT) | lọc theo `IdDonVi == DonViId` |
| 4 | Duyệt | `POST .../ChangeAccept/{id}` | `AccountSmsUnitUsed_ChangeAccept` (DV-AD-TONG, HT-KT) | ChuaDuyet → DaDuyet |
| 5 | Xóa | `POST .../Xoa/{id}` | `AccountSmsUnitUsed_Xoa` (DV-AD-TONG, HT-KT) | kiểm tra tồn tại |
| 6 | Kiểm tra đã duyệt | `POST .../IsAccepted` | (`[SBAuthorize()]` rỗng) | theo CurrentUser |

> State machine: `[ChuaDuyet, DaDuyet]`. FE bổ sung mức "Bị từ chối" (isUseDefaultAccepted=false).

## Entities
- `AccountSmsUnitUsed` — quan hệ đơn vị ↔ tài khoản SMS + trạng thái duyệt (`IsUseDefaultAccepted`: null=chờ, true=duyệt, false=từ chối), `IdDonVi`, `IdChiNhanh`, `MessageType` (SMS/Zalo), tên tài khoản.

## Luồng code
1. Tìm: `TimTheoDieuKien(filter)` → list bản ghi (gate quyền DV-AD-TONG/HT-KT).
2. Chi tiết: `LayTheoIdAsync` filter `CurrentUser.DonViId` → null nếu khác đơn vị.
3. Duyệt: `ChangeAccept/{id}` cập nhật trạng thái duyệt.
4. Ripple: trạng thái duyệt được dùng để **gate quyền gửi tin** của đơn vị — Consumer/SmsSend kiểm tra đơn vị đã được duyệt trước khi gửi.

## Bug-prone / lưu ý (path:line)
- **Thông báo lỗi copy nhầm domain** — `AccountSmsUnitUsedController.cs:53`: khi không tìm thấy bản ghi của đơn vị, trả message "Không tìm thấy Q&A!" (copy từ domain Q&A) → text sai, không ảnh hưởng logic.
- **Param thừa không dùng** — `AccountSmsUnitUsedController.cs:74-77`: `IsAccepted(Guid id)` khai báo param `id` nhưng service chỉ dùng `CurrentUser` → truyền id nào cũng cho kết quả theo đơn vị đăng nhập (dễ hiểu nhầm là check theo bản ghi cụ thể).
- **`IsAccepted` `[SBAuthorize()]` rỗng** — chỉ check login, không có permission key → khác các action cùng controller (đều DV-AD-TONG/HT-KT).
- **FE-BE controller mismatch:** màn `cauhinh-capphep-taikhoan` gọi `AccountSmsService` chứ không phải `AccountSmsUnitUsedService` — cần xác nhận đường đi thực tế khi debug (có thể 2 màn/2 controller cho cùng nghiệp vụ duyệt tài khoản).
- **Controller liên quan KHÁC:** `ConfigSMSController` (CRUD cấu hình gateway) — đã được mô tả ở mục **"Cấu hình gateway/nhà cung cấp SMS (ConfigSMS)"** bên dưới (bổ sung Pha 2). Khác hẳn AccountSmsUnitUsed = duyệt đơn vị-dùng-tài-khoản.

---

## Cấu hình gateway/nhà cung cấp SMS (ConfigSMS) *(bổ sung Pha 2 — ConfigSMS gateway)*

> Service **Core** (BE .NET) · Trace registry: `registry/he-thong/ConfigSMSController.md`. Đây là **CRUD cấu hình kết nối tới nhà cung cấp SMS (gateway)** mà service gửi tin đọc qua `GetByMa`. Độc lập với `AccountSmsUnitUsed`.

### Endpoint / Controller (trace registry)
`ConfigSMSController` (Core service, area-namespace `Core.API.Areas.SanboxArea` nhưng **không** có `[Area]` attribute; kế thừa `BaseController`, route `[Route("api/QuanTriArea/[controller]")]` → path gốc `api/QuanTriArea/ConfigSMS`; controller-level `[SBAuthorize]` + `[TienIch(..., MaTienIchSCRM.TI_HeThong, true)]`):

| # | Hành động | Route | Quyền (key) | Auth | State |
|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | `POST .../TimTheoDieuKien` | — (chỉ `[SBAuthorize]` controller) | đăng nhập | — |
| 2 | Tìm theo Id | `GET .../TimTheoId/{id:Guid}` | — | đăng nhập | `GetByIdAsync(id)` |
| 3 | Lấy tất cả | `GET .../LayTatCa` | — | đăng nhập | `GetAllAsync()` |
| 4 | Thêm mới | `POST .../ThemMoi` | — | đăng nhập | validate → map Dto→Entity → `InsertAsync` |
| 5 | Cập nhật | `POST .../CapNhatThongTin` | — | đăng nhập | validate → check tồn tại (`data.Data == null` → `MESS_NotFound`) → `UpdateAsync` |
| 6 | Xóa | `POST .../Xoa/{id:Guid}` | — | đăng nhập | check tồn tại (⚠️ sai, xem Bug-prone) → `DeleteAsync(id)` |
| 7 | Lấy cấu hình theo mã | `GET .../GetByMa/{ma}` | — | **anonymous (`[AllowAnonymous]`)** | `GetCauHinhSMSByMaAsync(ma)` |

> KHÔNG có permission key riêng cho action nào → mọi user authenticated đều CRUD được; riêng `GetByMa` mở public.

### Entities
- `ConfigSMS : BaseEntity` — cấu hình gateway. Field nhạy cảm: `Ma`, `ServerCode`, **`ApiKey`**, **`SecretKey`**, `BrandName`, `UseAccountBase`, `DictionaryParam`, `IsUnicode`, `SmsType`, `IsSanbox`, `SuDung`, `PatternSmsId`, `IdCorporation`/`IdCompany`/`IdUnit`. Map qua `ConfigSMSDto`.

### Luồng code
1. CRUD: controller → `IConfigSMSService` (`TimTheoDieuKien`/`GetById`/`GetAll`/`Insert`/`Update`/`Delete`).
2. Đọc khi gửi tin: service gửi SMS gọi `GetByMa/{ma}` → `GetCauHinhSMSByMaAsync(ma)` → trả cấu hình theo mã (anonymous) để kết nối nhà cung cấp.
3. Ripple: đổi/tắt/xóa cấu hình ảnh hưởng trực tiếp việc gửi tin thực tế qua gateway tương ứng.

### Bug-prone / lưu ý (path:line)
- **`GetByMa` `[AllowAnonymous]` lộ secret gateway** — `ConfigSMSController.cs:135-136`: endpoint `GET .../GetByMa/{ma}` mở public, không auth. Bản ghi trả về gồm `ApiKey`/`SecretKey`/`BrandName` → nếu đoán được `ma` thì **lộ khóa kết nối nhà cung cấp SMS**, có thể bị giả mạo gửi tin. **Lỗ hổng bảo mật** (chỉ ghi nhận, không sửa code ở session này — session security xử lý sau).
- **Check tồn tại khi Xóa sai** — `ConfigSMSController.cs:121-122`: `var data = await _service.GetByIdAsync(id); if (data == null)` — `GetByIdAsync` trả `MethodResult<...>` (luôn != null) nên điều kiện không bao giờ đúng → không bao giờ vào nhánh `MESS_NotFound`, vẫn gọi `DeleteAsync`. Đúng phải dùng `data.Data == null` như `CapNhatThongTin` (`:101`).
- **Không có permission key từng action** — toàn bộ action chỉ dựa `[SBAuthorize]` controller-level → mọi user authenticated CRUD được cấu hình gateway (không phân quyền theo nghiệp vụ).

### File source (path:line) — ConfigSMS
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\Core.API\Areas\SanboxArea\ConfigSMSController.cs` (`Xoa` check sai `:121-122`, `GetByMa` `[AllowAnonymous]` `:135-136`, `CapNhatThongTin` check đúng `:100-104`).
- BE entity: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\Core.Data\Entity\ConfigSMS.cs` (`ApiKey` `:36`, `SecretKey` `:40`, `BrandName` `:44`).
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\Core.Service\Generated\ConfigSMService.cs`.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\SMS\SMS.API\Controllers\AccountSmsUnitUsedController.cs` (`LayTheoIdAsync` `:51-53`, `IsAccepted` `:74-77`).
- FE màn: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\quantri\cauhinh-capphep-taikhoan\cauhinh-capphep-taikhoan.component.ts` (`ListAccountSmsUseDefault`, `ChangeAcceptAccount`).
- FE service AccountSmsUnitUsed: `...\scrm_frontend\src\app\services\sms\accountSmsUnitUsed.service.ts`.
- FE routing: `...\scrm_frontend\src\app\pages\quantri\quantri-routing.module.ts:169` (`quanly-capphep-taikhoan-guitin-nangcao`).
- Cẩm nang nền SMS: `projects/scrm/features/sms.md`.
