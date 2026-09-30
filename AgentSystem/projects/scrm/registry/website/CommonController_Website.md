---
controller: CommonController
module: website
service: Website
man_hinh_ui: "Nội bộ — không có UI riêng; cấp dropdown + danh mục enum cho các màn website (banner, tin tức, cấu hình hiển thị)."
muc_dich: "Cung cấp dropdown nhóm tin tức và các danh mục enum dùng chung cho UI website: kiểu banner (EnumKieuBanner), vị trí banner (EnumViTriBanner), trang website (EnumTrangWebsite), loại component dòng trang chủ (EnumComponentDong)."
alias: ["common website", "dropdown website", "enum banner", "nhóm tin tức dropdown", "danh mục chung website"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMNhomTinTuc]
stored_procs: []
depends: [Website]
cross_module_in: []
cross_module_out: []
---

# CommonController (Website) — Dropdown & enum dùng chung website

> Tên file `CommonController_Website.md` vì `CommonController` tồn tại ở nhiều module (collision). Bản này thuộc **service Website** (`Controllers/Common`, route `api/Common`). Controller có `[SBAuthorize()]` cấp class (rỗng — cần đăng nhập). Endpoint chỉ trả dữ liệu tra cứu/enum, không ghi DB.
>
> **Auth mixed:** `DropdownNhomTinTuc` có thêm `[SBAuthorize()]` per-method; 4 endpoint enum còn lại KHÔNG có attribute per-method nhưng vẫn chịu `[SBAuthorize()]` cấp controller (đăng nhập là đủ).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Dropdown nhóm tin tức | dropdown chọn nhóm tin tức | GET `api/Common/DropdownNhomTinTuc` | `[SBAuthorize]` rỗng | public | - | List `DMNhomTinTuc` của đơn vị (SuDung=true), sort Stt → {Id, Ten} | EF | - |
| 2 | Enum kiểu banner | dropdown kiểu banner | GET `api/Common/GetKieuBanner` | (kế thừa `[SBAuthorize]` controller) | public | - | Convert `EnumKieuBanner` → list EnumItem | in-memory enum | - |
| 3 | Enum vị trí banner | dropdown vị trí banner | GET `api/Common/GetViTriBanner` | (kế thừa controller) | public | - | Convert `EnumViTriBanner` → list | in-memory enum | - |
| 4 | Enum trang website | dropdown chọn trang | GET `api/Common/GetTrangWebsite` | (kế thừa controller) | public | - | Convert `EnumTrangWebsite` → list | in-memory enum | - |
| 5 | Enum loại component dòng | dropdown loại component trang chủ | GET `api/Common/GetEnumComponentDong` | (kế thừa controller) | public | - | Convert `EnumComponentDong` → list (dùng cho HomeDisplayConfig) | in-memory enum | - |

## Quan hệ
- **Gọi sang:** `CommonService` (Website). DropdownNhomTinTuc đọc repo `DMNhomTinTuc`; các enum convert in-memory qua `CommonHelper.ConvertEnumToList<T>`.
- **Dữ liệu (entities):** `DMNhomTinTuc` (Id, Ten, Stt, SuDung, IdDonVi). Các enum: EnumKieuBanner / EnumViTriBanner / EnumTrangWebsite / EnumComponentDong.
- **Ripple / đồng bộ:** read-only, không side-effect. `CommonService` còn nhiều method cache (ConfigSystem, HomeDisplayConfig) nhưng KHÔNG expose qua controller này — gọi nội bộ từ service khác.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonController.cs:31-56` — 4 endpoint enum không khai báo `[SBAuthorize]` per-method, chỉ dựa attribute cấp controller; cần đăng nhập nhưng không phân quyền chi tiết.
- `CommonService` (CommonService.cs) chứa logic cache ConfigSystem + HomeDisplayConfig (LayHomeConfigDisplay, LuuRedis..., XoaRedis...) dùng internal — không phải endpoint của controller này.
