---
controller: SanPhamDuKienDatCocController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Quản lý sản phẩm giới hạn tồn kho / dự kiến đặt cọc (route warehouse-product/du-kien-dat-coc, du-kien-dat-coc.component) — list theo chi nhánh + sản phẩm, nút Thêm mới (popup create đa SP), Nhập Excel, Xóa đã chọn, menu per-row Sửa/Xóa"
muc_dich: "Cấu hình số lượng tồn kho dự kiến/giới hạn (số lượng đặt cọc) theo từng sản phẩm × chi nhánh: tạo/sửa hàng loạt, xóa, import Excel; non-admin bị khóa về đúng chi nhánh đang đăng nhập."
alias: ["sản phẩm giới hạn tồn kho", "dự kiến đặt cọc", "du kien dat coc", "số lượng giới hạn tồn", "đặt cọc tồn kho", "SanPhamDuKienDatCoc"]
auth: public
state_machine: []
deprecated: false
entities: [SanPhamDuKienDatCoc, SanPham]
stored_procs: [PRC_SCRM_WarehouseProduct_DuKienDatCoc_Search]
depends: [WarehouseProduct, CommonService, SanPhamDuKienDatCocImportService]
cross_module_in: []
cross_module_out: []
---

# SanPhamDuKienDatCocController — Sản phẩm giới hạn tồn kho (dự kiến đặt cọc)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | nút Tìm kiếm / lọc chi nhánh + SP | POST `api/SanPhamDuKienDatCoc/SearchByConditions` | `SanPhamDuKienDatCoc_SearchByConditions` | public | màn list | Trả list cấu hình theo `IdDonVi` + chi nhánh + nhóm SP/keyword, phân trang, sort mặc định `NgayCapNhat DESC`; **non-admin bị ép `ListChiNhanh = [CurrentUser.IdChiNhanh]`** (controller); gắn TenChiNhanh từ CommonService | **SP** `PRC_SCRM_WarehouseProduct_DuKienDatCoc_Search` | - |
| 2 | Thêm mới (hàng loạt) | "Thêm mới" → popup create (nhiều SP) | POST `api/SanPhamDuKienDatCoc/Create` | `SanPhamDuKienDatCoc_Create` | public | popup Thêm | Tạo/upsert ≤100 dòng; **non-admin** bị ép `IsApDungTatCaChiNhanh=false` + chỉ chi nhánh đang đăng nhập. Admin chọn "áp tất cả chi nhánh" → fan-out ra mọi chi nhánh đơn vị | EF (BulkInsert/BulkUpdate) | Create thực chất upsert: trùng (IdSanPham,IdChiNhanh) trong DB → **update đè** chứ không báo trùng (chỉ chặn trùng trong CÙNG payload) |
| 3 | Cập nhật | menu "Sửa" per-row | POST `api/SanPhamDuKienDatCoc/Update` | `SanPhamDuKienDatCoc_Update` | public | per-row | Update theo (SP × chi nhánh); ép `IsApDungTatCaChiNhanh=false`, bắt buộc có `IdChiNhanh`; non-admin khóa về chi nhánh mình | EF | Cùng hàm `CreateOrUpdate` với #2, chỉ khác `ActionControl.UPDATE` |
| 4 | Xóa theo id | menu "Xóa" per-row | POST `api/SanPhamDuKienDatCoc/Delete/{id:Guid}` | `SanPhamDuKienDatCoc_Delete` | public | per-row, confirm | Guard tenant (`IdDonVi==CurrentUser.DonViId`) rồi hard-delete bản ghi | EF | Sai đơn vị → "Bạn không có quyền xóa!" |
| 5 | Xóa nhiều | "Xóa đã chọn" | POST `api/SanPhamDuKienDatCoc/DeleteByListId` | `SanPhamDuKienDatCoc_DeleteByListId` | public | có dòng tick | Xóa ≤1000 id trong scope đơn vị | EF | - |
| 6 | Tải file mẫu import | flow Nhập Excel | POST `api/SanPhamDuKienDatCoc/DownloadFileTemplateImport` | `SanPhamDuKienDatCoc_DownloadFileTemplateImport` | public | popup Import | Trả file `ImportSanPhamDuKienDatCoc.xlsx` | GemBox Excel | - |
| 7 | Import danh sách | "Nhập Excel" → upload | POST `api/SanPhamDuKienDatCoc/Import` | `SanPhamDuKienDatCoc_Import` | public | popup Import | Nhận `IFormFile` → tạo/upsert qua import service (có quy đổi đơn vị tính khi `isImport`); trả id batch lỗi | EF | - |
| 8 | Tải dòng import lỗi | sau import | GET `api/SanPhamDuKienDatCoc/DownloadDataWrong/{id}` | `SanPhamDuKienDatCoc_DownloadDataWrong` | public | sau import lỗi | Trả file `DataImportSanPhamDuKienDatCocFail.xlsx` | GemBox Excel | - |

## Quy tắc nghiệp vụ (validate khi tạo/sửa — blueprint để trace)
1. Payload rỗng → "Danh sách không được rỗng!"; **>100 dòng** → "Danh sách không được quá 100 sản phẩm!". (SanPhamDuKienDatCocService.cs:60-67)
2. `SoLuong` null → "Số lượng không được để trống!"; `SoLuong < 0` → "Số lượng không được để âm!". (SanPhamDuKienDatCocService.cs:82-89)
3. `IsApDungTatCaChiNhanh != true` mà `ListChiNhanh` rỗng → "Vui lòng chọn chi nhánh!". (SanPhamDuKienDatCocService.cs:91-94)
4. `IdSanPham` rỗng → "Sản phẩm không được để trống!". (SanPhamDuKienDatCocService.cs:96-99)
5. **UPDATE**: ép `IsApDungTatCaChiNhanh=false` + bắt buộc `IdChiNhanh` (thiếu → "Thiếu thông tin chi nhánh!"). (SanPhamDuKienDatCocService.cs:73-81)
6. `IsApDungTatCaChiNhanh=true` (admin): sinh 1 bản ghi cho MỖI chi nhánh đơn vị (CommonService.GetAllChiNhanh); trùng SP trong payload → "Trước đó đã có bản ghi trùng sản phẩm và chi nhánh!". (SanPhamDuKienDatCocService.cs:106-130)
7. Upsert theo (IdSanPham, IdChiNhanh): tồn tại → update (giữ NgayTao/người tạo cũ), chưa có → insert. (SanPhamDuKienDatCocService.cs:222-240)

## Quan hệ
- **Gọi sang:** `CommonService.GetAllChiNhanh` (danh sách chi nhánh đơn vị, gắn TenChiNhanh); `SanPhamDuKienDatCocImportService` (import + quy đổi đơn vị tính theo `BangQuyDoiJson`).
- **Dữ liệu (entities):** `SanPhamDuKienDatCoc` (IdSanPham, IdChiNhanh, IdDonVi, SoLuong, audit fields), join `SanPham` để quy đổi đơn vị tính khi import.
- **Ripple / đồng bộ:** không có cross-service. Controller chỉ ghi cấu hình; việc tiêu thụ "số lượng dự kiến/giới hạn" (nếu có) nằm ở luồng bán hàng/tồn kho khác — chưa thấy caller private.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/SanPhamDuKienDatCocController.cs`.
- Toàn bộ batch insert/update **nuốt exception** (`catch {}` / `catch(Exception ex){}` rỗng) — SanPhamDuKienDatCocService.cs:165-167, 249, 258. Lỗi DB không nổi lên; chỉ phản ánh gián tiếp qua "Chưa hoàn thành tiến độ, x/y" (L169-172). Khó trace khi import/save "im lặng" thiếu bản ghi.
- `SearchByConditions` luôn log store (`|| true` ở SanPhamDuKienDatCocService.cs:335) — log mọi user, không chỉ super/CSKH như ý định ban đầu.
- Controller chặn non-admin về chi nhánh đăng nhập **2 lớp** (controller L28-31/45-52 + service) — invariant phân quyền theo chi nhánh, giữ nguyên.
</content>
</invoke>
