---
controller: CauHinhController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Cấu hình: cấu hình chung (VAT, regex SĐT, khoảng thời gian tìm kiếm, báo cáo), cấu hình API/token, cấu hình thiết bị thông minh Lumi theo chi nhánh"
muc_dich: "Quản lý cấu hình tổng của đơn vị trong service Core: cấu hình init/chung, VAT, regex SĐT, báo cáo, token API (public + system), và điều khiển thiết bị đèn thông minh Lumi."
alias: ["CauHinh", "cấu hình core", "cấu hình hệ thống", "cấu hình API", "cấu hình Lumi", "cấu hình VAT"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhSetting, CauHinhChung, CauHinhApiSetting, CauHinhApiSystem, CauHinhBaoCao, CauHinhSmartDevice]
stored_procs: []
depends: ["ICauHinhService", "Lumi smart-device API (qua service)", "MongoDB (CauHinhSetting)"]
cross_module_in: ["FE Hệ thống/Cấu hình gọi Save*/Get*", "Service nội bộ gọi Get/GetCauHinhCore/GetCauHinhAPIPrivate/GetCauHinhAPISystemPrivate (PrivateSBAuthorize)", "AuthenPartner gọi GetCauHinhAPISystemPrivate"]
cross_module_out: ["Token API sinh ở đây dùng cho các service đối tác xác thực; cấu hình VAT/regex/khoảng-thời-gian được nhiều module nghiệp vụ đọc lại"]
---

# CauHinhController — Cấu hình tổng service Core

Màn cấu hình tổng của đơn vị. Gồm nhiều nhóm: cấu hình init/chung, VAT, regex SĐT (login + anonymous theo idDonVi), báo cáo, khoảng thời gian cho phép tìm kiếm, cấu hình API token (đơn vị + system-wide), và điều khiển thiết bị đèn thông minh Lumi theo chi nhánh. Phần lớn permission key bị comment, chỉ còn `[SBAuthorize]` rỗng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình init | Lưu | POST `api/CauHinh/SaveCauHinhInit` | `CauHinh_Core_LuuCauHinhCore` (DV-AD-TONG, HT-CSKH) | public | — | Upsert cấu hình khởi tạo của đơn vị. | EF/Mongo | — |
| 2 | Lưu/Lấy cấu hình chung | Lưu / load | POST `api/CauHinh/SaveCauHinhChung`, `GetCauHinhChung` | `CauHinh_Core_SaveCauHinhChung` / `_GetCauHinhChung` (DV-AD-TONG) | public | — | Save validate model; Get trả cấu hình chung của đơn vị. | EF/Mongo | — |
| 3 | Lưu/Lấy cấu hình báo cáo | Lưu / load | POST `api/CauHinh/SaveCauHinhBaoCao`, `GetCauHinhBaoCao` | `CauHinh_Core_SaveCauHinhBaoCao` (DV-AD-TONG) / Get rỗng | public | — | Cấu hình báo cáo của đơn vị. | EF/Mongo | Get dùng `[SBAuthorize()]` rỗng còn Save có key |
| 4 | Lấy cấu hình VAT | (load) | GET `api/CauHinh/GetCauHinhVat` | — (login) | public | — | Trả `ValueThueVat/TypeThueVat/IsSuDungVat`; nếu chưa cấu hình → mặc định 10%/type1/không dùng VAT. | EF/Mongo | Fallback mặc định khi GetAsync fail |
| 5 | Lấy regex SĐT (đã login) | (nội bộ) | GET `api/CauHinh/GetCauHinhRegexPhone` | — (login) | public | — | Trả `RegexPhone` của đơn vị; rỗng → fallback `LibConstants.REGEX_PHONE_NUMBER`. | EF/Mongo | — |
| 6 | Lấy regex SĐT (chưa login) | (form public) | GET `api/CauHinh/GetCauHinhRegexPhone/{idDonVi}` | — | anonymous | — | Như #5 nhưng theo `idDonVi` truyền vào, không cần login. | EF/Mongo | `[AllowAnonymous]` — public theo idDonVi |
| 7 | Lấy khoảng thời gian cho phép tìm kiếm | (nội bộ) | GET `api/CauHinh/GetCauHinhKhoangThoiGianChoPhepTimKiem` | — (login) | public | — | Ngưỡng khoảng ngày được phép filter ở các màn search. | EF/Mongo | — |
| 8 | Cấu hình API token (đơn vị) | Lưu / lấy / sinh mã | POST `LuuCauHinhAPI`; GET `GetCauHinhAPI`, `SinhMaTokenAPI` | `CauHinh_Core_LuuCauHinhAPI` / `_GetCauHinhAPI` / `_SinhMaTokenAPI` (DV-AD-TONG) | public | — | Lưu cấu hình API; sinh lại token → token cũ hết hiệu lực sau khi lưu. | EF/Mongo | — |
| 9 | Cấu hình API system-wide | Lưu / lấy | POST `LuuCauHinhAPISystem`; GET `GetCauHinhAPISystem` | `CauHinh_Core_LuuCauHinhAPISystem` / `_GetCauHinhAPISystem` (DV-AD-TONG) | public | — | Cấu hình API cấp hệ thống (không gắn riêng đơn vị). | EF/Mongo | — |
| 10 | Thiết bị thông minh Lumi | Lưu token / lưu cấu hình CN / DS / on-off / trạng thái | POST `SaveLumiApiToken`, `SaveChiNhanhSmartDevice`, `GetDSChiNhanhConfigSmartDevice`, `OnOffSmartDevice`, `GetStatusSmartDevice`, `SaveCauHinhLumi`, `GetCauHinhLumi` | phần lớn `[SBAuthorize]` rỗng (key bị comment, gốc DV-AD-TONG) | public | — | Cấu hình + điều khiển đèn thông minh Lumi theo chi nhánh (bật/tắt, trạng thái). | EF + Lumi API | Key DV-AD-TONG bị comment → chỉ chặn login |
| 11 | Check user export Excel / bộ lọc ngành hàng | (nội bộ) | POST `CheckUserExportExcel`, `GetCauHinhSuDungBoLocNganhHang`, `GetAllHome` | — (login) | public | — | Cờ cấu hình phục vụ FE (cho phép export, dùng bộ lọc ngành hàng, dữ liệu home). | EF/Mongo | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] **GET `Get`** `[PrivateSBAuthorize]` — lấy cấu hình core theo `CurrentUser` (key public bị khóa, chỉ service gọi).
- [internal] **GET `GetCauHinhCore/{idDonVi}`** — service khác lấy cấu hình core theo idDonVi.
- [internal] **GET `GetCauHinhAPIPrivate/{idDonVi}`** — lấy cấu hình API theo đơn vị (service-to-service).
- [internal] **GET `GetCauHinhAPISystemPrivate`** — AuthenPartner gọi để lấy setting API hệ thống (không cần idDonVi).

## Quan hệ
- **Gọi sang:** `ICauHinhService` (lưu Mongo `CauHinhSetting` + Lumi smart-device API).
- **Dữ liệu (entities):** `CauHinhSetting` (init/chung/VAT/regex), `CauHinhApiSetting` + `CauHinhApiSystem` (token API), `CauHinhBaoCao`, `CauHinhSmartDevice` (Lumi).
- **Ripple / đồng bộ:** token API điều khiển xác thực service đối tác; cấu hình VAT/regex/khoảng-thời-gian được module nghiệp vụ đọc lại lúc runtime.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs:55-100,140-166` — nhiều endpoint Lumi/cấu hình có permission key gốc (DV-AD-TONG) **bị comment**, chỉ còn `[SBAuthorize]` rỗng → chặn login, mất phân quyền chi tiết.
- `CauHinhController.cs:241-257` `GetCauHinhRegexPhone/{idDonVi}` `[AllowAnonymous]` — public theo idDonVi (chấp nhận được vì chỉ trả regex, nhưng cần lưu ý lộ cấu hình theo đơn vị).
- `CauHinhController.cs:168-174` `GetCauHinhBaoCao` dùng `[SBAuthorize()]` rỗng trong khi `SaveCauHinhBaoCao` có key DV-AD-TONG → đọc lỏng hơn ghi.
- `CauHinhController.cs:331-338` `SinhMaTokenAPI` — sinh token mới làm token cũ hết hiệu lực sau khi lưu LuuCauHinhAPI; cần cảnh báo phía gọi.
