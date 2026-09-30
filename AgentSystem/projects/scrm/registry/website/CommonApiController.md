---
controller: CommonApiController
module: website
service: Website
man_hinh_ui: "nội bộ — không có UI. API chung phục vụ render storefront (HomeController/CartController) và service-to-service đọc danh mục sản phẩm/dịch vụ/combo/thẻ/khuyến mãi theo đơn vị-website."
muc_dich: "Tập hợp các private API 'chung' của Website service: lấy danh mục nhóm SP/DV, sản phẩm/dịch vụ/combo/thẻ theo filter private, chi tiết theo id, tài khoản ngân hàng hệ thống, chi tiết khuyến mãi — luôn kèm thông tin LinkWebsite của đơn vị."
alias: ["api chung website", "common api", "lấy sản phẩm website", "lấy dịch vụ website", "danh mục website", "private api website"]
auth: internal
state_machine: []
deprecated: false
entities: [LinkWebsite, SanPham (DMNhom), DichVu (DMNhom), ComBoSpa, TheDichVu, TkNganHangHeThong, KhuyenMai]
stored_procs: []
depends: [CommonApiService (Website.Service), LinkWebsiteService, WarehouseProduct (SP/DV/Combo/Thẻ), Promotion (KhuyenMai)]
cross_module_in: [HomeController/Website (PrivateGet... render trang chủ), CartController/Website (verify item + ShopCartGroup), bất kỳ caller service-to-service có SystemAuthorization]
cross_module_out: [WarehouseProduct (đọc danh mục SP/DV/Combo/Thẻ), Promotion (đọc khuyến mãi)]
---

# CommonApiController — API chung website (private, đọc danh mục theo đơn vị)

> `[ApiController]`, route gốc `api/CommonApi`, `[TienIchAttribute("CommonApiController","Các api chung", MaTienIchSCRM.TI_Website)]`. **Toàn bộ method gắn `[PrivateSBAuthorize]`** → chỉ gọi nội bộ service-to-service (header `SystemAuthorization`), KHÔNG có UI người dùng. `partial class` → có thể còn method ở file partial khác. Mỗi response bọc kèm `LinkWebsite` (cấu hình website của đơn vị) trong `DataGetPrivateWebsiteDto<T>` / `DataGetDetailPrivateWebsiteDto<T>`.

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] **Danh mục theo filter** (`LayTatCaPrivateFilterDto`, tự fill `IdChiNhanhKhoDefault` từ LinkWebsite nếu rỗng): `GetDMNhomSanPham`, `GetDMNhomDichVu`, `GetDichVu`, `GetSanPham`, `GetComBoSpa`, `GetTheDichVu`, `GetTimTkNganHangHeThongTheo` — trả list + LinkWebsite. Caller chính: **HomeController** (render component trang chủ) + bất kỳ màn storefront cần danh mục.
- [internal] **Chi tiết theo id** (`GetByIdPrivateFilter`): `TimSanPhamTheoId`, `TimSanPhamChaTheoId`, `TimDichVuId`, `TimTheDichVuTheoId`, `TimComBoSpaTheoId`, `TimComBoSpaTheoId_FullDetail` (dùng `GetDetailComboPayload`), `TimTkNganHangHeThongTheoIdAsync`, `GetChiTietKhuyenMai`. Caller chính: **CartController** (verify item khi thêm vào giỏ), màn chi tiết storefront.

## Quan hệ
- **Gọi sang:** `ICommonApiService` (Website.Service) → đọc danh mục/chi tiết từ **WarehouseProduct** (SanPham, DichVu, ComBoSpa, TheDichVu, TkNganHangHeThong) và **Promotion** (KhuyenMai). `ILinkWebsiteService.GetLinkWebsiteByUnit(IdDonVi)` để lấy cấu hình website + `IdChiNhanhKhoDefault`.
- **Dữ liệu (entities):** đọc-only theo `IdDonVi` + `IdChiNhanhKhoDefault`; DTO trả về kèm `LinkWebsite`. Không ghi dữ liệu (controller này thuần đọc).
- **Ripple / đồng bộ:** không ghi → không ripple ra. Là tầng cung cấp data cho HomeController/CartController; thay đổi danh mục bên WarehouseProduct/Promotion phản ánh ngay qua đây.

## Lỗi đã biết / lưu ý kỹ thuật
- **Tất cả bọc try/catch trả `ResultWithError(ex.ToString())`** — lộ stack trace nguyên văn vào response (`CommonApiController.cs:57-60` và lặp lại mọi method). Internal nên rủi ro thấp, nhưng nên dùng message gọn.
- **Auto-fill `IdChiNhanhKhoDefault`**: nhóm `Get...` filter tự lấy từ LinkWebsite nếu payload rỗng (`:43-46` ...). Nhóm `Tim...TheoId` KHÔNG auto-fill (lấy thẳng theo id).
- **`TimComBoSpaTheoId_FullDetail`** không bọc LinkWebsite (trả `DataGetDetailPrivateWebsiteDto` chỉ có `Data`, `:391-394`) — khác các method còn lại; dùng `GetDetailComboPayload` thay vì `GetByIdPrivateFilter`.
- **Comment "Bỏ DV-NV theo yêu cầu"** (`:35`) — dịch vụ-nhân viên đã bị gỡ khỏi API chung; lưu ý khi tìm endpoint DV-NV.
