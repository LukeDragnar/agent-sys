---
controller: WebsiteSanPhamChaController
module: kho
service: WarehouseProduct
man_hinh_ui: "nội bộ — không có UI. Endpoint `TimTheoDieuKien` KHÔNG được FE gọi và KHÔNG có service nào gọi sang (không tìm thấy ref 'api/WebsiteSanPhamCha' trong toàn bộ BE/FE). Entity WebsiteSanPhamCha được dùng nội bộ trong CommonSanPhamService để dựng dữ liệu sản phẩm cha cho website."
muc_dich: "Kỳ vọng: tra cứu cấu hình hiển thị website của 'sản phẩm cha' (giá hiển thị, % giảm giá, trả góp, flash sale, mô tả/ảnh website...) theo điều kiện. Bản hiện tại: chỉ có 1 endpoint list phân trang, KHÔNG lọc theo đơn vị/điều kiện thực và CHƯA có caller → là endpoint chưa wire / gần như dead."
alias: ["website sản phẩm cha", "sản phẩm cha website", "WebsiteSanPhamCha", "cấu hình website sản phẩm cha", "san pham cha website"]
auth: public               # KHÔNG có attribute auth ở controller/method (không [SBAuthorize], không [Private], không [AllowAnonymous])
state_machine: []
deprecated: false
entities: [WebsiteSanPhamCha]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: []
---

# WebsiteSanPhamChaController — Cấu hình hiển thị website cho sản phẩm cha

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (list phân trang) | (không có UI gọi) | POST `api/WebsiteSanPhamCha/TimTheoDieuKien` | (none) | public (KHÔNG có attribute auth) | — | **Kỳ vọng:** list cấu hình website sản phẩm cha theo điều kiện + scope đơn vị. **Hiện tại:** trả TOÀN BỘ bảng phân trang, sort `NgayTao`, **KHÔNG filter IdDonVi / điều kiện nào** từ `gridInfo` (chỉ dùng PageInfo) | EF | **KHÔNG lọc tenant**: bất kỳ filter nào trong `gridInfo` đều bị bỏ qua, lấy cả bảng (rò rỉ dữ liệu liên đơn vị nếu được gọi). KHÔNG có caller → dead |

## Quan hệ
- **Gọi sang:** chỉ EF nội bộ (`IRepository<WebsiteSanPhamCha>`).
- **Dữ liệu (entities):** `WebsiteSanPhamCha` (IdDonVi, IdChiNhanh, IdSanPhamCha, IdDmNganhHang, TienHienThi, PhanTramGiamGia, TienGiamGia, PhamTramTraGop, IsNew, IsFlashSale, IdsVideo, WebsiteMoTaNgan/DayDu, WebsiteIdImages, StringThongSoKyThuat/ChiTietSanPham/UuDaiKhuyenMai...). Đây là lớp cấu hình hiển thị website cho 1 sản phẩm cha.
- **Ripple / đồng bộ:** entity được đọc nội bộ trong `CommonSanPhamService` (WarehouseProduct.Service/Generated/Common/CommonSanPhamService.cs:6453) để dựng dữ liệu SP cho website, NHƯNG endpoint controller này không nằm trên luồng đó. Module Website (nếu cần) đọc qua service/common khác, không qua `api/WebsiteSanPhamCha`.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/WebsiteSanPhamChaController.cs`.
- **Không có ref nào tới `api/WebsiteSanPhamCha`** trong BE lẫn FE (verify bằng ripgrep toàn repo) → endpoint `TimTheoDieuKien` hiện **không có caller** (dead/chưa wire). Việc tiêu thụ entity WebsiteSanPhamCha đi qua `CommonSanPhamService`, không qua controller này.
- **Thiếu phân quyền hoàn toàn**: controller/method KHÔNG có `[SBAuthorize]`/`[PrivateSBAuthorize]`/`[AllowAnonymous]` và KHÔNG có `[TienIchAttribute]` (khác hẳn các controller kho cùng module) → nếu được gọi sẽ không kiểm soát quyền.
- **`TimTheoDieuKienAsync` không lọc tenant/điều kiện** (WebsiteSanPhamChaService.cs:69-86): `query = _repos.TableWithNolock()` lấy cả bảng, `gridInfo.Sorts` chỉ để rẽ nhánh nhưng cả 2 nhánh đều `OrderBy(NgayTao)`; không dùng `IdDonVi`, keyword, hay field filter nào. Rủi ro rò rỉ cross-tenant nếu wire lên UI.
- `public partial class WebsiteSanPhamChaController` (L10) — **controller `partial`**, có thể còn endpoint ở file partial khác; node này cover file Controllers/WebsiteSanPhamChaController.cs. Ctor nhận `IServiceProvider inject` nhưng không dùng (L13-19).
- Service có override `UpdateAsync` MarkDirty đầy đủ field (WebsiteSanPhamChaService.cs:34-67) → CRUD entity dự kiến đi qua `IBaseService` (BaseController generic) ở partial khác hoặc qua common, không lộ ở controller hiện tại.
</content>
