---
controller: HomeController
module: website
service: Website
man_hinh_ui: "Website bán hàng (storefront public) — trang chủ (Home/Index.cshtml) theo theme động (ThemeCode). Bố cục động theo cấu hình hiển thị trang chủ (HomeDisplayConfig): nhóm SP/DV, sản phẩm, tin tức, khuyến mãi, dịch vụ, combo, thẻ, combo/thẻ đã mua."
muc_dich: "Render trang chủ website bán hàng public: đọc cấu hình component hiển thị theo đơn vị, lần lượt nạp danh mục (nhóm SP/DV, sản phẩm, tin tức, KM, dịch vụ, combo, thẻ) + dữ liệu cá nhân hóa (combo/thẻ đã mua nếu đã login) rồi gắn vào model."
alias: ["trang chủ website", "home website", "trang chủ storefront", "index website"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LinkWebsite, HomeDisplayConfig, DMNhomSanPham, DMNhomDichVu, SanPham, DichVu, ComBoSpa, TheDichVu, TinTuc, KhuyenMai]
stored_procs: []
depends: [LinkWebsiteService, HomeDisplayConfigService, CommonApiService, TinTucService, TokenService, WarehouseProduct, Promotion]
cross_module_in: []
cross_module_out: [WarehouseProduct (SP/DV/Combo/Thẻ + combo/thẻ đã mua), Promotion (KhuyenMai), Core (đơn vị theo domain)]
---

# HomeController — Trang chủ website bán hàng (storefront public)

> MVC `Controller` (trả về **View** Razor). Action `Index` gắn `[TokenAuthentication]` (soft cookie filter — không chặn khách vô danh; chỉ prefill nếu đã login). Auth thực tế = `anonymous`. Đơn vị + theme resolve theo **domain**. Bố cục trang chủ **hoàn toàn động** theo `HomeDisplayConfig` (cấu hình bên admin): chỉ nạp các component (`EnumComponentDong`) mà cấu hình bật. Mỗi component nạp tối đa 4 item (PageSize=4).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Mở trang chủ | logo / "Trang chủ" | GET `/` (route mặc định Home/Index) | — | anonymous | luôn | Resolve domain→theme; lấy `HomeDisplayConfig` (Private) theo đơn vị; nạp nhóm SP + nhóm DV; theo `types` bật trong config nạp tiếp SanPham/TinTuc/KhuyenMai/DichVu/Combo/TheDichVu; nếu đã login nạp Combo/Thẻ ĐÃ MUA; render `Home/Index.cshtml` | EF (qua private API) | toàn bộ bọc try/catch → fail trả `View("Error", ex)` (lộ exception object vào view) |
| 2 | (lỗi) | — | GET `Home/Error` | — | anonymous | khi lỗi | Trả `NotFound()` | - | action Error đơn giản, khác nhánh catch của Index (catch render View("Error", ex)) |

## Quy tắc nghiệp vụ (component hiển thị động — `EnumComponentDong`)
Trang chủ nạp component theo `types` (lấy từ `HomeDisplayConfig.Type`), thứ tự kiểm tra trong `Index`:
1. Luôn nạp: `DMNhomSanPham`, `DMNhomDichVu`.
2. `SanPham` → gom `LstSanPhamId` từ các config `IsChonThuCong==true` (sản phẩm chọn tay) rồi nạp 4 sản phẩm.
3. `TinTuc` → `TinTucService.LayTatCa_Private`.
4. `ChuongTrinhKM` → `GetKhuyenMai`.
5. `DichVu` → `GetDichVu`. 6. `Combo` → `GetComBoSpa`. 7. `TheDichVu` → `GetTheDichVu`.
8. `ChuyenGia` / `DanhGiaHtml` → nhánh rỗng (chưa implement, chỉ giữ chỗ).
9. `ComBoDaMua` → `GetListComboDaMua` (theo token — cá nhân hóa). 10. `TheDaMua` → `GetListTheDaMua` (theo token).
> `ComBoDaMua`/`TheDaMua` dùng `LayTatCaPrivateWithoutIdDonViFilterDto` với `Token` (đã login mới có dữ liệu); các component còn lại dùng `LayTatCaPrivateFilterDto` theo `IdDonVi`.

## Quan hệ
- **Gọi sang:** `IHomeDisplayConfigService.DanhSachCauHinhHienThiTrangChu_Private`; `ICommonApiService` (GetDMNhomSanPham/DichVu, GetSanPham, GetDichVu, GetComBoSpa, GetTheDichVu, GetKhuyenMai, GetListComboDaMua, GetListTheDaMua); `ITinTucService.LayTatCa_Private`; `ILinkWebsiteService.GetDataByDomain`; `ITokenService` (khách login). → đọc từ WarehouseProduct + Promotion.
- **Dữ liệu (entities):** đọc-only, gắn vào `AllDataCommonModel` (DMNhomSanPhams/DMNhomDichVus/SanPhams/DichVus/ComBoSpas/TheDichVus/TinTucs/KhuyenMais/ComBoSpaDaMuas/TheDichVuDaMuas). Không ghi DB.
- **Ripple / đồng bộ:** không ghi → không ripple ra. Phụ thuộc cấu hình `HomeDisplayConfig` (đổi cấu hình admin → đổi bố cục trang chủ) và danh mục WarehouseProduct/Promotion.

## Lỗi đã biết / lưu ý kỹ thuật
- **Catch nuốt lỗi render `View("Error", (object)ex)`** (`HomeController.cs:184-188`) — đẩy nguyên exception object vào view Error; dòng `throw;` sau `return` là dead code không bao giờ chạy.
- **Null-guard "trả ra trang lỗi" chỉ là comment** (`:86-94`): nếu `HomeDisplayConfig` null/rỗng thì comment ghi "trả ra trang lỗi" nhưng KHÔNG return → chạy tiếp tới `dsCauHinhHienThiTrangChuData.Select(...)` ngay dưới → **NullReferenceException** khi config rỗng (rồi rơi vào catch → View Error). Cần guard thật.
- **Các method `PrivateGet...` để `public`** (lẽ ra private helper) — không gây lỗi nhưng lộ ra ngoài class không cần thiet.
- **`payloadU.Token = token`**: combo/thẻ đã mua chỉ có dữ liệu khi khách đăng nhập; khách vô danh → list rỗng (đúng kỳ vọng).
