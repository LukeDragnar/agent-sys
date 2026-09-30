---
controller: ComboSpaController
module: website
service: Website
man_hinh_ui: "Storefront website (theme) — trang chi tiết combo spa đang bán: URL `/ComboSpa/{id}`, render view `ComboSpaDetail.cshtml` theo ThemeCode của đơn vị, kèm 4 combo liên quan"
muc_dich: "Render trang chi tiết 1 combo spa đang bán trên website bán hàng (MVC View): lấy chi tiết combo theo id + đơn vị qua private cross-service, kèm danh sách combo liên quan, đổ vào view theo theme."
alias: ["combo spa", "chi tiết combo spa", "trang combo spa website", "ComboSpaDetail"]
auth: anonymous
state_machine: []
deprecated: false
entities: [ComBoSpa, LinkWebsite]
stored_procs: []
depends: [Website (LinkWebsiteService, CommonApiService), Combo/WarehouseProduct service (TimComBoSpaTheoId, GetComBoSpa — private)]
cross_module_in: []
cross_module_out: [Combo/WarehouseProduct service (gọi TimComBoSpaTheoId + GetComBoSpa để lấy combo đang bán)]
---

# ComboSpaController — Trang chi tiết combo spa đang bán (storefront)

> MVC **View** controller (kế thừa `Controller`, không phải API). Trả Razor view, không JSON. Áp `[TokenAuthentication]` cấp controller — nhưng filter này KHÔNG chặn (mọi nhánh `context.Result = Redirect` đã bị comment, `TokenAuthorizationFilter.cs:61,124,131`), chỉ best-effort refresh token cookie rồi cho qua → trang truy cập được khi CHƯA đăng nhập ⇒ `auth: anonymous`. Song song với `ComBoSpaDaMuaController` (combo ĐÃ mua) — controller này là combo ĐANG bán.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Mở trang chi tiết combo spa | (link từ list combo → chi tiết) | GET `/ComboSpa/{ComboSpaId}` | — | anonymous | luôn (storefront) | Resolve domain → `GetDataByDomain` (theme/đơn vị/cookie cart); fail/null → `NotFound`. Gọi private `TimComBoSpaTheoId` (Id + IdDonVi); fail/null → `NotFound`. Gán `model.ComBoSpa`, sau đó `PrivateGetComboSpa` lấy 4 combo liên quan (`GetComBoSpa`, page 1 size 4). Render `~/Views/Themes/{ThemeCode}/Home/ComboSpaDetail.cshtml` | Cross-service private API | - |

## Endpoint nội bộ (private / service-to-service)
- [internal] **GỌI SANG** combo service: `_commonApiService.TimComBoSpaTheoId(GetByIdPrivateFilter{Id, IdDonVi})` lấy chi tiết combo; `_commonApiService.GetComBoSpa(LayTatCaPrivateFilterDto{IdDonVi, PageInfo})` lấy danh sách combo liên quan (4 item).

## Quan hệ
- **Gọi sang:** `ILinkWebsiteService.GetDataByDomain`; `ICommonApiService.TimComBoSpaTheoId` + `GetComBoSpa` (private cross-service).
- **Dữ liệu (entities):** combo spa (`AllDataCommonModel.ComBoSpa`, `ComBoSpas`), `dataCommon` (IdDonVi, ThemeCode).
- **Ripple / đồng bộ:** chỉ đọc/hiển thị; không ghi.

## Lỗi đã biết / lưu ý kỹ thuật
- `ComboSpaId.Value` gọi trực tiếp dù param là `Guid?` → null route → `NullReferenceException` (`ComboSpaController.cs:60`).
- View path động theo `ThemeCode` — theme thiếu `ComboSpaDetail.cshtml` sẽ lỗi runtime (không guard) (`ComboSpaController.cs:79`).
- `PrivateGetComboSpa` nuốt lỗi êm (return model nguyên trạng nếu res null/fail/Data null) → trang vẫn render nhưng thiếu danh sách combo liên quan, không báo lỗi (`ComboSpaController.cs:81-89`).
