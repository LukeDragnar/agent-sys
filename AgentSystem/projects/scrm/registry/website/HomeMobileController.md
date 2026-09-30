---
controller: HomeMobileController
module: website
service: Website
man_hinh_ui: "App khách (mobile) — trang chủ: hiển thị sản phẩm/danh mục trang chủ; list item theo danh mục (tin tức/dịch vụ/KM/combo/thẻ) và xem chi tiết item."
muc_dich: "Cấp dữ liệu trang chủ cho app mobile khách: sản phẩm trang chủ; danh sách item theo danh mục (IdDanhMuc: 2=tin tức, 3=khuyến mãi, 4=dịch vụ, 5=combo spa, 8=thẻ dịch vụ) và chi tiết item tương ứng."
alias: ["trang chủ app", "home mobile", "trang chủ mobile", "danh mục app", "item danh mục"]
auth: public
state_machine: []
deprecated: false
entities: [LinkWebsite]
stored_procs: []
depends: [Website (SanPhamService/ContentService), WarehouseProduct/Promotion (qua CommonApiService), TinTuc]
cross_module_in: []
cross_module_out: [WarehouseProduct (GetDichVu/GetComBoSpa/GetTheDichVu/TimDichVuId Private), Promotion (GetKhuyenMai/GetChiTietKhuyenMai Private), Website TinTuc (LayTatCa_Private)]
---

# HomeMobileController — Trang chủ app mobile khách

> Controller app khách (`Controllers/MobileApi`), route `api/HomeMobile`. Tất cả endpoint dùng `[TokenMobileAuthentication]` — auth bằng customer token: yêu cầu header `sb-device: mobile` + `Authorization: Bearer <token>`, resolve khách qua `IAuthService.GetCurrentUserByToken` → set CustomerLogin. KHÔNG dùng permission key (auth khách app, không phải nhân viên).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin trang chủ | (load màn trang chủ app) | GET `api/HomeMobile/GetThongTinTrangChu` | `[TokenMobileAuthentication]` | public | luôn | `SanPhamService.GetSanPhamTrangChu()` trả sản phẩm trang chủ cho app | EF + private call | - |
| 2 | List item theo danh mục | (tab danh mục: tin tức/dịch vụ/KM/combo/thẻ) | POST `api/HomeMobile/GetListItemDanhMuc` | `[TokenMobileAuthentication]` | public | theo IdDanhMuc | `ContentService.GetListItemDanhMuc`: lấy DonViId từ CustomerLogin + LinkWebsite → switch theo IdDanhMuc gọi private (4→GetDichVu, 2→TinTuc LayTatCa_Private, 3→GetKhuyenMai, 5→GetComBoSpa, 8→GetTheDichVu); phân trang theo payload | private cross-service | IdDanhMuc ngoài {2,3,4,5,8} → trả data null, total 0 (không lỗi); LinkWebsite null → NRE khi đọc IdChiNhanhKhoDefault (ContentService.cs:79,90) |
| 3 | Chi tiết item danh mục | (mở chi tiết item) | POST `api/HomeMobile/GetChiTietItemDanhMuc` | `[TokenMobileAuthentication]` | public | theo IdDanhMuc | `ContentService.GetChiTietItemDanhMuc`: theo IdDanhMuc gọi private lấy chi tiết (4→TimDichVuId, 3→GetChiTietKhuyenMai...) theo IdItemDanhMuc + IdDonVi | private cross-service | - |

## Quan hệ
- **Gọi sang:** `SanPhamService` (trang chủ), `ContentService` → `CommonApiService` (proxy sang WarehouseProduct: dịch vụ/combo/thẻ; Promotion: khuyến mãi) và `TinTucService.LayTatCa_Private` (tin tức). Đơn vị xác định qua `CustomerLogin.DonViId` + `LinkWebsite.IdChiNhanhKhoDefault`.
- **Dữ liệu (entities):** chủ yếu đọc từ service khác qua private API; nội bộ chỉ tra `LinkWebsite` để lấy kho default.
- **Ripple / đồng bộ:** chỉ đọc (read-only), không phát sinh side-effect.

## Lỗi đã biết / lưu ý kỹ thuật
- `ContentService.cs:79,90` & `:138` — đọc `linkWebsite.IdChiNhanhKhoDefault` không null-check `linkWebsite` → NRE nếu đơn vị khách chưa cấu hình LinkWebsite.
- `ContentService.cs:122-131` — IdDanhMuc không thuộc {2,3,4,5,8} (hoặc call private thất bại) → trả `ResultWithData(null,"",0)`, client tự hiểu là rỗng.
- Auth bắt buộc header `sb-device: mobile`; nếu không có header này thì `TokenMobileAuthentication` không lấy được token → 401 (TokenMobileAuthenticationFilter.cs:23-47).
