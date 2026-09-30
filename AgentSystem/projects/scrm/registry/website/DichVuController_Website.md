---
controller: DichVuController
module: website
service: Website
man_hinh_ui: "Trang storefront khách hàng (Razor view) — chi tiết 1 dịch vụ: `/DichVu/{id}` render `DichVuDetail.cshtml`; trang danh sách dịch vụ `/DanhSachServices` render `DanhSachServices.cshtml`; search dịch vụ (ajax)"
muc_dich: "MVC controller storefront cho dịch vụ trên website đơn vị: trang chi tiết 1 dịch vụ + dịch vụ liên quan, trang list dịch vụ phân trang, và search dịch vụ. (Khác hẳn DichVuController bên kho/WarehouseProduct — đây là front-end website.)"
alias: ["dịch vụ website", "chi tiết dịch vụ storefront", "trang dịch vụ", "danh sách dịch vụ website", "DichVu website"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LinkWebsite, DichVu]
stored_procs: []
depends: [Website/ILinkWebsiteService (resolve domain→đơn vị), Website/ICommonApiService (gọi sang admin-API private), WarehouseProduct (qua private endpoint)]
cross_module_in: []
cross_module_out: [WarehouseProduct (TimDichVuId / GetDichVu — private theo IdDonVi)]
---

# DichVuController (Website) — Trang dịch vụ storefront

> **Trùng tên** với `DichVuController` bên kho (`registry/kho/DichVuController.md`) nhưng KHÁC HOÀN TOÀN: bản này là **MVC view controller** của website bán hàng (kế thừa `Controller`, trả `View(...)`), không có CRUD, không `[SBAuthorize]`/permission key. Cả controller gắn `[TokenAuthentication]` (filter không-chặn). Định danh đơn vị qua **domain** (`GetDataByDomain`). Dữ liệu thật lấy từ admin-API qua `_commonApiService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết 1 dịch vụ | click vào 1 dịch vụ | GET `/DichVu/{DichVuId}` `[TokenAuthentication]` | — | anonymous | luôn | Resolve đơn vị theo domain → `TimDichVuId` (GetByIdPrivateFilter: Id + IdDonVi) → nếu fail/null trả NotFound; load thêm 4 dịch vụ liên quan → render `DichVuDetail.cshtml` theo `ThemeCode` | private API (CommonApiService) | - |
| 2 | Danh sách dịch vụ (phân trang) | menu "Dịch vụ" | GET `/DanhSachServices?pageIndex=&pageSize=` `[TokenAuthentication]` | — | anonymous | luôn | Resolve đơn vị → `GetDichVu` (page/size) → render `DanhSachServices.cshtml`; tính `ViewBag.TotalPages` (chia cứng /12) | private API | TotalPages tính cứng `/12` dù có `pageSize` param (DichVuController.cs:115) — sai khi pageSize≠12 |
| 3 | Search dịch vụ | ô tìm kiếm + phân trang (ajax) | GET `/SearchListService?keyword=&idNhom=&pageIndex=&pageSize=` `[TokenAuthentication]` | — | anonymous | trên trang dịch vụ | Trả `Json(model)` DS dịch vụ theo keyword (idNhom mặc định Guid.Empty nếu trống) phân trang; gán `TotalRecord`=totalPages, `PageNumber`=pageIndex | private API | `idNhom` được parse nhưng KHÔNG đẩy vào payload (chỉ set Keyword) → lọc theo nhóm không có tác dụng ở endpoint này (DichVuController.cs:146-163) |

## Endpoint nội bộ (private / service-to-service)
- Controller là caller: `TimDichVuId` (GetByIdPrivateFilter) cho chi tiết; `GetDichVu` (LayTatCaPrivateFilterDto có IdDonVi) cho list/search.

## Quan hệ
- **Gọi sang:** `ILinkWebsiteService.GetDataByDomain`; `ICommonApiService` → admin-API dịch vụ (kho `WarehouseProduct`).
- **Dữ liệu (entities):** `LinkWebsite`, `DichVu` (chỉ đọc — DTO `DichVuDto`).
- **Ripple / đồng bộ:** chỉ đọc. Đổi dịch vụ/giá bên kho → phản ánh ngay lên storefront.

## Lỗi đã biết / lưu ý kỹ thuật
- **Trùng tên controller** với bản kho → file này dùng suffix `_Website`; node bản kho ở `registry/kho/DichVuController.md`.
- `SearchListService` nhận `idNhom` nhưng không gán vào `payload` → filter theo nhóm vô hiệu (DichVuController.cs:152-163).
- `DanhSachDichVu` tính số trang chia cứng `/12`, bỏ qua `pageSize` truyền vào (DichVuController.cs:115-119).
- `[TokenAuthentication]` áp ở controller-level nhưng không-chặn (token null vẫn render) → storefront public.
