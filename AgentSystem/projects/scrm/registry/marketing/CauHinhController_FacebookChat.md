---
controller: CauHinhController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub (Tin nhắn Facebook) > Cấu hình / Danh mục cấu hình nghiệp vụ (CRUD nội bộ admin đơn vị)"
muc_dich: "CRUD bản ghi cấu hình nghiệp vụ ChatHub (entity CauHinh, lưu JsonSetting theo đơn vị). Cung cấp tìm theo id, thêm/sửa/xóa và tìm theo điều kiện (phân trang). Service còn có Save/Get setting theo đơn vị kèm cache Redis (chưa expose ở controller này)."
alias: ["cấu hình ChatHub", "cấu hình nghiệp vụ chat", "CauHinh Facebook", "danh mục cấu hình", "setting ChatHub"]
auth: internal
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends:
  - "ICauHinhService (FacebookChat.Services.Generated.CauHinhService : BaseService<CauHinh>)"
  - "IRepository<CauHinh> (SQL)"
  - "IRedisCacheService (cache key 'CK_CauHinhFacebookChatSetting_' + IdDonVi)"
  - "AutoMapper (CauHinhDto <-> CauHinh)"
  - "Core/TienIch (MaTienIchSCRM.TI_ChatHub)"
cross_module_in: []
cross_module_out: []
---

# CauHinhController (FacebookChat) — Cấu hình nghiệp vụ ChatHub

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết theo id | (mở chi tiết) | GET `api/CauHinh/TimTheoId/{id:Guid?}` | `CauHinh_FacebookChat_TimTheoIdAsync` ("Tìm cấu hình theo id" — DV-AD-TONG) | internal | có id | `_service.GetByIdAsync(id)` (BaseService) → trả 1 bản ghi `CauHinh`. | EF `CauHinh` | — |
| 2 | Thêm mới | Thêm mới | POST `api/CauHinh/ThemMoi` | `CauHinh_FacebookChat_ThemMoiAsync` ("Thêm mới cấu hình nghiệp vụ" — DV-AD-TONG) | internal | form thêm | ModelState invalid → trả lỗi `MESS_Model_UnValidate`; hợp lệ → map `CauHinhDto→CauHinh` → `InsertAsync`. | EF `CauHinh` | — |
| 3 | Cập nhật | Lưu | POST `api/CauHinh/CapNhatThongTin` | `CauHinh_FacebookChat_CapNhatAsync` ("Lưu cấu hình nghiệp vụ" — DV-AD-TONG) | internal | form sửa | Invalid → lỗi; map DTO→entity → `GetByIdAsync(Id)` check tồn tại (null → `MESS_NotFound`) → `UpdateAsync` (override MarkDirty IdDonVi/JsonSetting/IsDelete). | EF `CauHinh` | — |
| 4 | Xóa | Xóa | POST `api/CauHinh/Xoa/{id:Guid}` | `CauHinh_FacebookChat_Xoa` ("Xóa cấu hình nghiệp vụ" — DV-AD-TONG) | internal | có id | `GetByIdAsync(id)` check tồn tại (null → `MESS_NotFound`) → `DeleteAsync(id)` → trả true + `MESS_Delete_Success`. | EF `CauHinh` | — |
| 5 | Tìm theo điều kiện (phân trang) | Tìm kiếm / lưới | POST `api/CauHinh/TimTheoDieuKien` | `CauHinh_FacebookChat_TimTheoDieuKien` ("Tìm kiếm cấu hình nghiệp vụ" — DV-AD-TONG) | internal | màn danh sách | `TimTheoDieuKienAsync`: query toàn bảng (no-lock), order `NgayTao`, Skip/Take theo `PageInfo`, map sang DTO, trả kèm `total`. | EF `CauHinh` | `CauHinhService.cs:54-71` — query KHÔNG lọc `IdDonVi`/`IsDelete` → trả cấu hình của MỌI đơn vị + cả bản đã xóa mềm. Nhánh sort if/else giống hệt nhau (luôn order NgayTao). |

## Quy tắc nghiệp vụ (gating)
- **Tiện ích gate:** `[TienIch("CauHinh_FacebookChat_Controller", "Quản lý danh mục", MaTienIchSCRM.TI_ChatHub)]` controller-level → đơn vị chưa mua tiện ích ChatHub không vào được.
- **Phân quyền:** tất cả endpoint chỉ role `DV-AD-TONG` (admin tổng đơn vị) → cấu hình nghiệp vụ là thao tác admin.
- **Mô hình setting (Save/Get qua service — chưa có endpoint):** mỗi đơn vị có TỐI ĐA 1 bản `CauHinh` (where `IsDelete=false && IdDonVi=...`), lưu `JsonSetting` (serialize `CauHinhSettingDto`). `GetAsync(idDonVi)` ưu tiên Redis (`CK_CauHinhFacebookChatSetting_{IdDonVi}`, TTL 1 ngày); cache/DB miss → fallback bản ghi `IdDonVi = Guid.Empty` (cấu hình mặc định hệ thống). `SaveAsync` upsert theo IdDonVi rồi xóa cache.

## Quan hệ
- **Gọi sang:** chỉ `ICauHinhService` (BaseService CRUD trên `CauHinh`) + AutoMapper. `SaveAsync/GetAsync` dùng Redis cache.
- **Dữ liệu (entities):** `CauHinh` (Id, IdDonVi, JsonSetting, IsDelete, NgayTao).
- **Ripple / đồng bộ:** không phát event/consumer. Side-effect duy nhất: `SaveAsync`/`UpdateAsync` clear Redis cache setting của đơn vị (đường Save chưa expose ở controller này).

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhService.cs:54-71` — `TimTheoDieuKienAsync` KHÔNG filter `IdDonVi` và `IsDelete` → rò rỉ cấu hình giữa các đơn vị + hiển thị bản đã xóa mềm. Endpoint `TimTheoDieuKien` (controller dòng 106-112) phụ thuộc trực tiếp.
- `CauHinhService.cs:59-66` — nhánh `if/else` sort giống hệt nhau (cả 2 đều `OrderBy(NgayTao)`) → `gridInfo.Sorts` thực tế bị bỏ qua.
- Controller có `CRM`-style ctor đơn giản; các method `SaveAsync/GetAsync(UserLogin)` trong service chưa được wire ra endpoint → muốn lưu setting JSON theo đơn vị phải thêm endpoint.
