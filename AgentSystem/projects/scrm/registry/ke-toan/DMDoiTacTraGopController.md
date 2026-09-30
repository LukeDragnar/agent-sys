---
controller: DMDoiTacTraGopController
module: ke-toan
service: Accounting
man_hinh_ui: "Quản lý danh mục > Đối tác trả góp (danh mục dùng chung, không phân đơn vị): lưới list + Thêm mới / Sửa / Xóa / Xem chi tiết. Mỗi bản ghi = 1 đối tác trả góp (tên, mã, ảnh, phương thức thanh toán, cờ ẩn form thanh toán)."
muc_dich: "CRUD danh mục đối tác trả góp dùng chung toàn hệ thống (master data, KHÔNG scope theo đơn vị). Là nguồn để DonViDoiTacTraGop map cấu hình trả góp theo đơn vị (lọc IdPhuongThucThanhToan=8)."
alias: ["danh mục đối tác trả góp", "dm đối tác trả góp", "dm doi tac tra gop", "đối tác trả góp", "doi tac tra gop", "danh mục trả góp"]
auth: public
state_machine: []
deprecated: false
entities: [DMDoiTacTraGop]
stored_procs: []
depends: [BaseService/Repository (EF CRUD), AutoMapper (Dto↔Entity), RedisCache (invalidate "DMDoiTacTraGop_All" + prefix "DoiTacTraGop_ByDonVi_")]
cross_module_in: []
cross_module_out: []
---

# DMDoiTacTraGopController — Danh mục đối tác trả góp (master data, CRUD)

> CRUD danh mục dùng chung GỌN: Thêm/Sửa/Xóa/Xem chi tiết/Tìm theo điều kiện. Là **master data toàn hệ thống** — KHÔNG scope theo đơn vị, KHÔNG phân quyền chi nhánh. Mọi `[SBAuthorize(...)]` đều **bị comment** → endpoint chỉ chịu auth mặc định của `BaseController` (public, không key). Update dùng `MarkDirty` các field cụ thể. Mọi thao tác ghi (Insert/Update/Delete) **xóa cache** `DMDoiTacTraGop_All` + prefix `DoiTacTraGop_ByDonVi_`. `[TienIch(MaTienIchSCRM.TI_Chung)]` (danh mục chung, không phải TI_KeToan).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (lưới) | Lưới list + paging | `POST api/DMDoiTacTraGop/TimTheoDieuKien` | — (no key) | public | màn danh mục đối tác trả góp | `TableNoTracking`, `total = Count()`, OrderBy `NgayTao`, paging, map sang `DMDoiTacTraGopDto`. | **EF** `DMDoiTacTraGop` | OrderBy luôn `NgayTao` (cả 2 nhánh if/else giống hệt → `gridInfo.Sorts` không tác dụng). |
| 2 | Xem chi tiết | (mở chi tiết 1 dòng) | `GET api/DMDoiTacTraGop/TimTheoId/{id:Guid?}` | — (no key) | public | click 1 dòng | `GetByIdAsync(id)`. | **EF** `DMDoiTacTraGop` | — |
| 3 | Thêm mới | nút "Thêm mới" / Lưu | `POST api/DMDoiTacTraGop/ThemMoi` | — (no key) | public | popup thêm | Validate `ModelState`. Default `IsHideFormThanhToan ??= false`. Map Dto→Entity → `InsertAsync` → xóa cache. | **EF** BulkInsert (`DMDoiTacTraGop`) | — |
| 4 | Cập nhật | nút "Sửa" / Lưu | `POST api/DMDoiTacTraGop/CapNhatThongTin` | — (no key) | public | popup sửa | Validate ModelState. Default `IsHideFormThanhToan ??= false`. `GetByIdAsync(Id)` — null → "Không tìm thấy". `UpdateAsync` MarkDirty (Ma, Ten, Images, IdPhuongThucThanhToan, IsHideFormThanhToan, IsDelete) → xóa cache. | **EF** `DMDoiTacTraGop` | — |
| 5 | Xóa | nút "Xóa" / confirm | `POST api/DMDoiTacTraGop/Xoa/{id:Guid}` | — (no key) | public | mỗi dòng | `GetByIdAsync` — null → "Không tìm thấy". `DeleteAsync(id)` (soft-delete qua BaseService) → xóa cache. | **EF** `DMDoiTacTraGop` | — |

## Quan hệ
- **Gọi sang:** không cross-service; chỉ EF + RedisCache.
- **Dữ liệu (entities):** `DMDoiTacTraGop` (BaseEntity + Ma, Ten, Images, IdPhuongThucThanhToan, IsHideFormThanhToan).
- **Ripple / đồng bộ:** ghi xong xóa cache `DMDoiTacTraGop_All` và prefix `DoiTacTraGop_ByDonVi_` (cache do DonViDoiTacTraGopService dùng) → đảm bảo cấu hình trả góp theo đơn vị đọc lại master mới. Không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/DMDoiTacTraGopController.cs`
  - L29/L41/L61/L88: các `[SBAuthorize("DMDoiTacTraGop_*", ...)]` đều **bị comment** → endpoint không có permission key, chỉ auth mặc định BaseController. Permission key dự kiến (theo comment): `DMDoiTacTraGop_ThemMoi`, `DMDoiTacTraGop_CapNhatThongTin`, `DMDoiTacTraGop_Xoa`.
  - Các comment dùng từ "chi nhánh" (copy từ ChiNhanh controller) nhưng entity là đối tác trả góp — chỉ là chú thích sai, không ảnh hưởng hành vi.
- BE `Accounting/Accounting.Service/Generated/DMDoiTacTraGopService.cs`
  - L76-83 (`TimTheoDieuKienAsync`): nhánh `if (Sorts.Any())` và `else` đều `OrderBy(b => b.NgayTao)` giống nhau → tham số sort của FE bị bỏ qua.
  - `GetAllDoiTacTraGop` cache 60*24 phút (1 ngày) key `DMDoiTacTraGop_All`.
- DTO: `Models.Core.DTO.Accounting.Client.DMDoiTacTraGop` (`DMDoiTacTraGopDto`, `DMDoiTacTraGopFilterDto`). Entity: `Accounting/Accounting.DataContext/Entity/DMDoiTacTraGop.cs`.
