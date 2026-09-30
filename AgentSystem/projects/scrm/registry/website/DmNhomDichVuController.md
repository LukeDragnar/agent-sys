---
controller: DmNhomDichVuController
module: website
service: Website
man_hinh_ui: "Trang storefront khách hàng (Razor view) — trang chi tiết nhóm dịch vụ: `/NhomDichVu/{id}` render `DmNhomDichVuDetail.cshtml` theo theme đơn vị; form search dịch vụ trong nhóm (ajax)"
muc_dich: "MVC controller render trang chi tiết một nhóm dịch vụ trên website bán hàng của đơn vị: list nhóm dịch vụ + chi tiết nhóm + danh sách dịch vụ trong nhóm (có phân trang + search)."
alias: ["nhóm dịch vụ website", "trang nhóm dịch vụ", "danh mục nhóm dịch vụ storefront", "category dịch vụ", "DmNhomDichVu"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LinkWebsite, NhomDichVu, DichVu]
stored_procs: []
depends: [Website/ILinkWebsiteService (resolve domain→đơn vị), Website/ICommonApiService (gọi sang admin-API private), WarehouseProduct (qua private endpoint)]
cross_module_in: []
cross_module_out: [WarehouseProduct (GetDMNhomDichVu / GetChiTietNhomDichVu / GetDichVu — private theo IdDonVi)]
---

# DmNhomDichVuController — Trang chi tiết nhóm dịch vụ (storefront website)

> MVC controller (kế thừa `Controller`, trả `View(...)`), **không phải API** — phục vụ website bán hàng public của từng đơn vị. Định danh đơn vị qua **domain** (`_linkWebsiteService.GetDataByDomain`), không qua login. Không gắn `[SBAuthorize]`/permission key. Dữ liệu thật lấy từ admin-side qua `_commonApiService.*` (endpoint private theo `IdDonVi`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Mở trang nhóm dịch vụ | click vào 1 nhóm dịch vụ trên menu/trang chủ | GET `/NhomDichVu/{NhomDichVuId}` | — | anonymous | luôn | Resolve đơn vị theo domain → load DS nhóm dịch vụ (page 1, size 12) + chi tiết nhóm + DS dịch vụ trong nhóm → render `DmNhomDichVuDetail.cshtml` theo `ThemeCode`; tính `ViewBag.TotalPages`. NotFound nếu domain không map được đơn vị | private API (CommonApiService) | - |
| 2 | Search/lọc dịch vụ trong nhóm | ô tìm kiếm + phân trang (ajax) | GET `/SearchDv?keyword=&idNhom=&pageIndex=&pageSize=` | — | anonymous | trên trang nhóm | Trả `Json(model)` DS dịch vụ theo keyword + idNhom (mặc định Guid.Empty nếu trống) phân trang; cập nhật `TotalRecord`=totalPages, `PageNumber`=pageIndex | private API | `model.TotalRecord` bị gán = totalPages (số trang) chứ không phải tổng bản ghi — tên field gây nhầm khi reuse |

## Endpoint nội bộ (private / service-to-service)
- Không expose endpoint internal; **bản thân controller là caller** gọi sang admin-API: `GetDMNhomDichVu`, `GetChiTietNhomDichVu`, `GetDichVu` (payload `LayTatCaPrivateFilterDto` / `LayChiTietDvPrivateDto` mang `IdDonVi` + `IdChiNhanhKhoDefault`).

## Quan hệ
- **Gọi sang:** `ILinkWebsiteService.GetDataByDomain` (domain → cấu hình + IdDonVi + theme); `ICommonApiService` → admin-API nhóm dịch vụ / dịch vụ (kho `WarehouseProduct`).
- **Dữ liệu (entities):** `LinkWebsite` (map domain→đơn vị, theme, chi nhánh kho default), `NhomDichVu`, `DichVu` (chỉ đọc, hiển thị).
- **Ripple / đồng bộ:** chỉ đọc — không ghi. Đổi nhóm/dịch vụ bên kho (`DichVuController` kho) → phản ánh ngay lên trang này.

## Lỗi đã biết / lưu ý kỹ thuật
- Mọi action `catch`-an-toàn ngầm qua guard `dataLinkWebsite.Success==false` → trả `new NotFound()` (không phải `NotFound()` MVC chuẩn — là class `NotFound` của project, lưu ý khi đọc).
- `SearchDv` gán `model.TotalRecord = totalPages` (DmNhomDichVuController.cs:162) — semantics lệch tên field; view phải hiểu đây là số trang.
- Auth thật sự: storefront public, không chặn login; `[TokenAuthentication]` KHÔNG áp ở controller này (khác `DichVuController`/`ComBoSpaListController`).
