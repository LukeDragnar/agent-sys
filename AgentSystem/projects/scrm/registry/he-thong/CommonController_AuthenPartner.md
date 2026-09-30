---
controller: CommonController
module: he-thong
service: AuthenPartner
man_hinh_ui: "Nội bộ — không có UI. API cho đối tác (3rd-party) gọi vào để tra dữ liệu danh mục: chi nhánh, sản phẩm cha/con, danh mục sản phẩm, thẻ dịch vụ, combo, dịch vụ."
muc_dich: "Cổng API đối tác cho các danh mục dùng chung: lấy list chi nhánh, sản phẩm (cha/con), danh mục sản phẩm, thẻ dịch vụ, combo theo chi nhánh, dịch vụ — proxy sang Core/WarehouseProduct service."
alias: ["api đối tác lấy chi nhánh", "api lấy sản phẩm partner", "common partner api", "lấy danh mục cho đối tác", "dropdown sản phẩm partner"]
auth: internal
state_machine: []
deprecated: false
entities: [ChiNhanh, SanPham, DMSanPham, TheDichVu, ComBoSpa, DichVu]
stored_procs: []
depends: ["SBAuthorizePartner (JWT partner + CauHinh AccessToken/DeviceId + IsSuDungApiGetData)", "Core service (/api/ChiNhanh/LayListChiNhanhPrivate)", "WarehouseProduct service (Dropdown sản phẩm cha/con, DMSanPham, TheDichVu, ComBoSpa, DichVu Private)", "Redis (CommonCacheService: throttle 60s/lần per đơn vị+user+API)"]
cross_module_in: ["Hệ thống đối tác (3rd-party Pushsale/Tingtong/...) → các endpoint Lay* (qua JWT partner cấp phát + CauHinh đơn vị)"]
cross_module_out: ["Core service (ChiNhanh)", "WarehouseProduct service (SanPham/DMSanPham/TheDichVu/ComBoSpa/DichVu)"]
---

# CommonController — API danh mục dùng chung cho đối tác (AuthenPartner)

Đây là controller thuộc service **AuthenPartner** — cổng xác thực & tích hợp cho **đối tác bên thứ ba** (không phải end-user, không phải back-office). Vì đã tồn tại `CommonController` của các project khác trong folder (`_OriginalProject`, `_TrackMap`) nên file này mang hậu tố `_AuthenPartner`.

Mọi endpoint kế thừa `BasePartnerController` và gắn `[SBAuthorizePartner]`. Mô hình auth đối tác (xem `SBAuthorizePartnerAttribute.cs`): client gửi `Authorization: Bearer <JWT>`; filter validate chữ ký bằng `AppAPISettings.Secret`, đọc claim `id` / `idDonVi` / `device`; sau đó tra `CauHinhSettingDto` của đơn vị trong Redis/DB và **đối chiếu** `AccessToken` + `DeviceId` đã đăng ký, đồng thời bắt buộc `IsSuDungApiGetData == true`. `CurrentUserPartner` (`UserLogin`) có `Id = Guid.Empty`, `DonViId`, `IdNganhNghe` (lấy từ đơn vị). Controller **luôn ghi đè** `DonViId`/`IdNganhNghe` của payload bằng giá trị từ token → đối tác không thể truy vấn chéo đơn vị khác.

Throttle: `LayListChiNhanh` qua `CommonCacheService.GetCache_Time_Call_API` (60s/lần per đơn vị+user). Các endpoint còn lại **không** throttle.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy list chi nhánh | (API) | GET `api/Common/LayListChiNhanh` | — | internal `[SBAuthorizePartner]` | — | Throttle 60s/lần (per đơn vị+user) → gọi Core `ChiNhanh/LayListChiNhanhPrivate/{DonViId}` lấy danh sách chi nhánh của đơn vị. | HTTP Core | Throttle key gồm cả `Id` user — partner luôn `Id=Guid.Empty` nên throttle thực chất theo đơn vị |
| 2 | Lấy sản phẩm cha (autocomplete) | (API) | POST `api/Common/LayLstSanPhamChaAPI` | — | internal `[SBAuthorizePartner]` | — | Ghi đè `payload.DonViId = DonViId` → gọi WarehouseProduct `Common/DropdownSanPhamChaAutoCompletePrivate`. | HTTP WarehouseProduct | — |
| 3 | Lấy sản phẩm con (autocomplete) | (API) | POST `api/Common/LayLstSanPhamConAPI` | — | internal `[SBAuthorizePartner]` | — | Ghi đè `payload.DonViId = DonViId` → gọi WarehouseProduct `Common/DropdownSanPhamChildAutoCompletePrivate`. | HTTP WarehouseProduct | Dùng `CurrentUserPartner?.DonViId` (null-safe) — khác các method còn lại dùng trực tiếp |
| 4 | Lấy danh mục sản phẩm | (API) | POST `api/Common/LayDanhMucSanPhamAPI` | — | internal `[SBAuthorizePartner]` | — | Ghi đè `payload.IdDonVi = DonViId` → gọi WarehouseProduct `DMSanPham/TimTheoDieuKien_DV_Private`. | HTTP WarehouseProduct | — |
| 5 | Lấy thẻ dịch vụ | (API) | POST `api/Common/LayTheDichVuAPI` | — | internal `[SBAuthorizePartner]` | — | Ghi đè `payload.IdDonVi = DonViId` → gọi WarehouseProduct `TheDichVu/TimTheoDieuKien_Private`. | HTTP WarehouseProduct | — |
| 6 | Lấy combo theo chi nhánh | (API) | POST `api/Common/LayLstComboByChiNhanhAPI` | — | internal `[SBAuthorizePartner]` | — | Ghi đè `payload.IdDonVi = DonViId` → gọi WarehouseProduct `ComBoSpa/TimTheoDieuKien_Private`. | HTTP WarehouseProduct | — |
| 7 | Lấy dịch vụ | (API) | POST `api/Common/LayListDichVu` | — | internal `[SBAuthorizePartner]` | — | Ghi đè `IdDonVi = DonViId` + `IdNganhNghe = IdNganhNghe` → gọi WarehouseProduct `DichVu` Private. | HTTP WarehouseProduct | — |

## Quy tắc nghiệp vụ (gating)
1. Auth filter `SBAuthorizePartner`: JWT hợp lệ (chữ ký + claim `id`/`idDonVi`/`device`) → tra `CauHinhSettingDto` đơn vị. Nếu `AccessToken != token && DeviceId != device` → 401. Nếu `IsSuDungApiGetData` null/false → 401.
2. Mọi `DonViId`/`IdNganhNghe` payload bị override bằng giá trị từ token → cách ly dữ liệu theo đơn vị (đối tác không thể chỉ định đơn vị khác).
3. `LayListChiNhanh`: nếu đã gọi trong 60s gần nhất → trả lỗi "Bạn cần chờ Ns để tiếp tục call API".

## Quan hệ
- **Gọi sang:** Core (`ChiNhanh/LayListChiNhanhPrivate`); WarehouseProduct (Dropdown sản phẩm cha/con, DMSanPham, TheDichVu, ComBoSpa, DichVu — đều `..._Private`).
- **Dữ liệu (entities):** danh mục đọc-only — `ChiNhanh`, `SanPham`, `DMSanPham`, `TheDichVu`, `ComBoSpa`, `DichVu`.
- **Ripple / đồng bộ:** không ghi dữ liệu — chỉ proxy đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- `SBAuthorizePartnerAttribute.cs:212` — điều kiện chặn dùng `AccessToken != token && DeviceId != device`: chỉ reject khi **cả hai** đều lệch → request vẫn pass nếu chỉ token đúng (hoặc chỉ device đúng). Logic chặn yếu hơn ý định, lưu ý bảo mật.
- `CommonController.cs:72` — `LayLstSanPhamConAPI` dùng `CurrentUserPartner?.DonViId` (nullable) trong khi các method khác dùng `CurrentUserPartner.DonViId`; không nhất quán (nếu account null sẽ truyền `DonViId=null` thay vì NRE).
- Throttle 60s chỉ áp cho endpoint #1; các endpoint sản phẩm/dịch vụ không giới hạn tần suất → đối tác có thể spam.
