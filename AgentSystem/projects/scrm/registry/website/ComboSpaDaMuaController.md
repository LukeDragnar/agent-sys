---
controller: ComBoSpaDaMuaController
module: website
service: Website
man_hinh_ui: "Storefront website (theme) — trang chi tiết combo spa ĐÃ MUA của khách: URL `/ComBoSpaDaMua/{id}`, render view `ComBoSpaDaMuaDetail.cshtml` theo ThemeCode của đơn vị"
muc_dich: "Render trang chi tiết 1 combo spa mà khách hàng đã mua trên website bán hàng (MVC View, không phải API JSON); lấy data combo đã mua qua private cross-service rồi đổ vào view theo theme."
alias: ["combo spa đã mua", "chi tiết combo đã mua", "trang combo đã mua website", "ComBoSpaDaMuaDetail"]
auth: anonymous
state_machine: []
deprecated: false
entities: [ComBoSpaDaMua, LinkWebsite]
stored_procs: []
depends: [Website (LinkWebsiteService, CommonApiService, TokenService), Customer/Combo service (GetDetailComboKhachHangByIdPrivate — private)]
cross_module_in: []
cross_module_out: [Customer/Combo service (gọi GetDetailComboKhachHangByIdPrivate để lấy combo đã mua của khách)]
---

# ComBoSpaDaMuaController — Trang chi tiết combo spa đã mua (storefront)

> MVC **View** controller (kế thừa `Controller`, không phải `BaseCoreController`/API). Trả về Razor view, không trả JSON. Tên class thật là `ComBoSpaDaMuaController` (chú ý chữ hoa B/M) — file `ComboSpaDaMuaController.cs`. Áp `[TokenAuthentication]` ở cấp controller (lấy token theo cookie/domain, không phải permission key SCRM) — filter này KHÔNG chặn (nhánh redirect đã comment, `TokenAuthorizationFilter.cs:61,124,131`), chỉ best-effort refresh token rồi cho qua → truy cập được khi CHƯA đăng nhập ⇒ `auth: anonymous`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Mở trang chi tiết combo đã mua | (link từ trang khách hàng → trang combo đã mua) | GET `/ComBoSpaDaMua/{ComBoSpaDaMuaId}` | — | anonymous | luôn (storefront) | Resolve domain → `GetDataByDomain` lấy data chung (theme, đơn vị, cart cookie). Nếu fail/null → `NotFound`. Gọi private `GetDetailComboKhachHangByIdPrivate` (IdObject + token) → nếu fail/null → `NotFound`. Gán vào `model.ComBoSpaDaMua` → render `~/Views/Themes/{ThemeCode}/Home/ComBoSpaDaMuaDetail.cshtml` | Cross-service private API | - |

## Endpoint nội bộ (private / service-to-service)
- [internal] Controller này **GỌI SANG** combo/customer service qua `_commonApiService.GetDetailComboKhachHangByIdPrivate(GetDetailCrossServiceDto)` (kèm token từ `TokenService`) để lấy chi tiết combo đã mua của khách.

## Quan hệ
- **Gọi sang:** `ILinkWebsiteService.GetDataByDomain` (resolve domain → đơn vị/theme/cookie cart); `ICommonApiService.GetDetailComboKhachHangByIdPrivate` (private cross-service).
- **Dữ liệu (entities):** combo spa đã mua (`AllDataCommonModel.ComBoSpaDaMua`), `dataCommon` (IdDonVi, ThemeCode).
- **Ripple / đồng bộ:** chỉ đọc/hiển thị; không ghi. Phụ thuộc theme code của đơn vị (view path động).

## Lỗi đã biết / lưu ý kỹ thuật
- View path build động theo `model.dataCommon.ThemeCode` — nếu theme thiếu file `ComBoSpaDaMuaDetail.cshtml` sẽ lỗi runtime (không có guard) (`ComboSpaDaMuaController.cs:84`).
- `ComBoSpaDaMuaId.Value` được gọi trực tiếp dù tham số là `Guid?` → nếu route cho qua null sẽ `NullReferenceException` (`ComboSpaDaMuaController.cs:65`).
- Block `PrivateGetComBoSpaDaMua` (list 4 combo liên quan) đã bị comment toàn bộ → trang hiện không load danh sách combo gợi ý (`ComboSpaDaMuaController.cs:72-94`).
- Tên class lệch tên file (`ComBoSpaDaMua` vs file `ComboSpaDaMua`) — khi grep cần dùng đúng casing class.
