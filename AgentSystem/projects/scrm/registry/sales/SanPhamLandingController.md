---
controller: SanPhamLandingController
module: sales
service: Contact
man_hinh_ui: "Marketing/Landing > Quản lý nguồn dữ liệu (landing-page module: sanpham-landing.service; cấu hình chia số/chia chat, kênh bán hàng, import nguồn dữ liệu)"
muc_dich: "Quản lý 'sản phẩm landing' = nguồn dữ liệu marketing: CRUD nguồn, cấu hình chia số/chia chat, kênh bán hàng, gán user MKT, import Excel nguồn dữ liệu; quản lý landing-data (số liệu landing theo FB Ads/ngày); expose API public/anonymous cho web booking & ecommerce."
alias: [SanPhamLanding, "nguồn dữ liệu", "landing", "sản phẩm landing"]
auth: mixed
state_machine: []
deprecated: false
entities: [SanPhamLanding, SanPhamLandingData]
stored_procs: []
depends: [SanPhamLandingService, SanPhamLandingDataService, "AutoMapper", "GemBox.Spreadsheet (template/import Excel)", "Redis (GetCacheLandingById)"]
cross_module_in: ["service nội bộ (GetLandingDataPrivate, ThemSanPhamLandingDataPrivate, CapNhatThongTinLandingDataPrivate, GetDropdownLandingFilter)", "Facebook/Ads (đẩy landing data theo FB Ads)", "Ecommerce (TimTheoIdWithNoAuthen)", "web booking (GetSanPhamLandingById anonymous)"]
cross_module_out: ["Redis landing cache (GetCacheLandingById)"]
---

# SanPhamLanding — Quản lý nguồn dữ liệu (sản phẩm landing)

> Controller-level `[SBAuthorize]`. Một số endpoint mở `[AllowAnonymous]` cho ecommerce/web booking, một số `[PrivateSBAuthorize]` cho service Facebook đẩy landing data.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới nguồn | Lưu | POST `ThemMoi` | `SanPhamLandingController_ThemMoi` (DV-AD-TONG/CN/MCN, QL-MKT, MKT-LEAD, MKT) | public | form thêm nguồn | Insert nguồn dữ liệu mới. | EF | — |
| 2 | Cập nhật thông tin | Lưu | POST `CapNhatThongTin/{id}` | `SanPhamLandingController_CapNhatThongTin` (như #1) | public | form sửa | Update nguồn theo id. | EF | — |
| 3 | Đổi cờ cho phép sale tạo contact thủ công | Toggle | POST `ChangeIsAllowSaleTaoThuCong/{id}` | `SanPhamLandingController_ChangeIsAllowSaleTaoThuCong` (như #1) | public | dòng nguồn | Bật/tắt cho phép sale tạo contact thủ công cho nguồn. | EF | — |
| 4 | Cấu hình chia số / chia chat / kênh bán hàng | Lưu cấu hình | POST `ChangeCauHinhChiaSo`, POST `ChangeCauHinhChiaChat`, POST `ChangeKenhBanHang` | ChiaSo/KenhBanHang: key tương ứng (MKT roles); **ChiaChat: key rỗng** (chỉ login + role list) | public | tab cấu hình nguồn | Lưu cấu hình chia số / chia chat / kênh bán hàng cho nguồn. | EF | `ChangeCauHinhChiaChat` `[SBAuthorize("", ...)]` **key rỗng** |
| 5 | Số nguồn đang áp dụng chia chat | — | GET `SoNguonApDungChiaChat/{idCHCSC}` | `[SBAuthorize()]` (login) | public | tab chia chat | Đếm số nguồn đang áp dụng 1 cấu hình chia chat. | EF | — |
| 6 | Cập nhật marketing user | Lưu | POST `UpdateMarketingUser` | `SanPhamLandingController_UpdateMarketingUser` (DV-AD-TONG/CN/MCN) | public | gán user MKT | Gán/cập nhật user MKT cho nguồn. | EF | — |
| 7 | Tìm theo điều kiện / dropdown / theo id | Danh sách | POST `TimTheoDieuKienAsync`, GET `TimListLandingDropdownAsync`, GET `TimTheoId/{id}` | key tương ứng (danh sách role rộng: AD/SALE/MKT/KHO/KT...) | public | grid nguồn | Trả danh sách / dropdown / chi tiết nguồn theo quyền user. | EF | — |
| 8 | Xóa nguồn | Xóa | POST `Xoa/{id}` | `SanPhamLandingController_Xoa` (DV-AD-TONG/CN/MCN, MKT) | public | dòng nguồn | Xóa nguồn theo id (POST). | EF | — |
| 9 | Landing data (tìm/lưu/xóa) | Lưu / Xóa | POST `Sanpham_LandingData_TimTheoDieuKien`, POST `Sanpham_LandingData_Save`, POST `Sanpham_LandingData_Xoa/{idLandingData}` | `[SBAuthorize]` (login) | tab số liệu landing | CRUD số liệu landing (theo FB Ads/ngày). | EF | — |
| 10 | Import nguồn dữ liệu (template / DS import / xóa import / import / insert) | Tải template / Import | GET `DownloadTemplateNguonDuLieuAsync`, POST `GetDanhSachNguonDuLieuImport`, POST `DeleteNguonDuLieuImport`, POST `ImportTemplateNguonDuLieu`, POST `InsertDataImportToSanPhamLanding` | key `SanPhamLandingController_*` (MKT roles) | public | tab import | Tải template Excel; import file → DS tạm; insert DS tạm vào nguồn dữ liệu. | EF + GemBox | — |
| 11 | Update toàn bộ data landing | — | GET `UpdateDataLanding` | `SanPhamLandingController_UpdateData` (DV-AD-TONG/CN/MCN) | public | nút bảo trì | Cập nhật lại toàn bộ data sản phẩm landing của đơn vị. | EF | GET cho hành động ghi hàng loạt (nên POST) |
| 12 | Lấy landing theo id (cache) / theo ids | — | GET `GetSanPhamLandingById/{id}`, POST `GetListLandingByIds` | ById: `[AllowAnonymous]`; ByIds: `[SBAuthorize]` | mixed | web booking / nội bộ | `GetSanPhamLandingById` trả landing từ cache Redis (public); `GetListLandingByIds` lấy nhiều theo ids. | Redis/EF | `GetSanPhamLandingById` anonymous |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` `GetLandingDataPrivate(req)` — lấy landing data theo FB Ads + ngày.
- `[PrivateSBAuthorize]` `ThemSanPhamLandingDataPrivate(req)` / `CapNhatThongTinLandingDataPrivate(req)` — service **Facebook/Ads** đẩy/cập nhật landing data (map `SanPhamLandingDataFB` → `SanPhamLandingData`; update check NotFound trước).
- `[PrivateSBAuthorize]` `GetDropdownLandingFilter(req)` — dropdown landing cho filter ở service khác.
- `[AllowAnonymous]` `TimTheoIdWithNoAuthen/{id}` — Ecommerce lấy landing không cần auth.

## Quan hệ
- **Gọi sang:** `SanPhamLandingService`, `SanPhamLandingDataService`, AutoMapper, GemBox (Excel), Redis (cache landing).
- **Dữ liệu (entities):** `SanPhamLanding` (nguồn dữ liệu + cấu hình chia số/chat/kênh bán hàng), `SanPhamLandingData` (số liệu theo FB Ads/ngày).
- **Ripple / đồng bộ:** `GetSanPhamLandingById` đọc cache → cần đảm bảo cache được làm mới sau Create/Update/Delete landing; Facebook đẩy landing data qua endpoint private.

## Lỗi đã biết / lưu ý kỹ thuật
- `SanPhamLandingController.cs:66` — `ChangeCauHinhChiaChat` `[SBAuthorize("", ...)]` **key permission rỗng** → chỉ chặn bằng danh sách role, không có permission key (lệch convention so với ChangeCauHinhChiaSo).
- `SanPhamLandingController.cs:185,283` — `TimTheoIdWithNoAuthen`, `GetSanPhamLandingById` `[AllowAnonymous]`: trả thông tin nguồn/landing không qua token → dựa hoàn toàn vào id (cần đảm bảo không lộ dữ liệu nhạy cảm).
- `SanPhamLandingController.cs:263` — `UpdateDataLanding` dùng HTTP **GET** cho thao tác cập nhật hàng loạt (không idempotent-safe, dễ trigger ngoài ý muốn).
