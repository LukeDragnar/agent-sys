---
controller: ViTriKhoController
module: kho
service: WarehouseProduct
man_hinh_ui: "Cấu hình kho › 'Cấu hình vị trí sản phẩm' (cauhinhkho-vitri-sanpham): trái = cây Vị trí kho theo từng kho; phải = lưới sản phẩm theo vị trí. Menu kebab có Import vị trí sản phẩm + Cập nhật vị trí kho."
muc_dich: "Quản lý cây vị trí (kệ/ô) trong từng kho và gán sản phẩm vào vị trí: CRUD vị trí, gán/bỏ sản phẩm khỏi vị trí, import Excel vị trí-sản phẩm, import Excel cập nhật vị trí kho."
alias: [vi tri kho, vị trí kho, vi tri san pham, vị trí sản phẩm, cau hinh vi tri, kệ kho, ô kho, location, bin, ViTriKho, ViTriSanPham]
auth: mixed
state_machine: []
deprecated: false
entities: [ViTriKho, ViTriSanPham, ViTriSanPhamImport, SanPham, Kho]
stored_procs: []
depends: [Kho, SanPham, CommonService, GemBox/Excel template, IWebHostEnvironment (ImportFail file)]
cross_module_in: []
cross_module_out: []
---

# ViTriKho — Quản lý vị trí kho & gán sản phẩm vào vị trí

> CRUD thuần EF (không stored proc). Mọi truy vấn scope theo `IdDonVi` + `IdChiNhanh` của user (đa tenant). Cây vị trí lồng nhau qua `IdParent`. Mỗi action ghi đều bọc transaction + `ExecutionStrategy`. Controller-level `[SBAuthorize]` (đăng nhập) — KHÔNG có permission key riêng từng nút (các comment permission đã bị comment-out, xem L84).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint | Quyền | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cây vị trí theo kho | Cây "Vị trí kho" (pane trái) | `GET api/ViTriKho/GetTreeViTriKho` | `SBAuthorize` (login) | internal-user | luôn | Lấy danh sách Kho của user (DonViId+IdChiNhanh) làm node gốc; mỗi kho build cây vị trí từ `ViTriKho` qua `IdParent` (`.ToTree`). Trả `List<TreeNode>` (Key=IdKho, children=vị trí). | EF | `lstViTri` lấy TOÀN BỘ `ViTriKho` **không lọc IdDonVi/IdChiNhanh** (L120) rồi mới filter theo IdKho — kho thì có scope nhưng vị trí join lỏng; cross-tenant chỉ chặn gián tiếp qua IdKho. |
| 2 | Lấy 1 vị trí theo Id | (mở form sửa node) | `GET api/ViTriKho/TimTheoId/{id}` | `SBAuthorize` | internal-user | node level>0 (nút Sửa) | `GetByIdAsync(id)` trả entity ViTriKho. | EF | — |
| 3 | Tạo / sửa vị trí | Form thêm/sửa node cây | `POST api/ViTriKho/CreateOrUpdateViTriKho` | `SBAuthorize` | internal-user | luôn | Tìm entity theo Id+DonViId+IdChiNhanh; không thấy → tạo mới (gán DonViId/IdChiNhanh hiện tại); có → map lại + `MarkDirty(Ten/ThuTu/IsViTriCuoi)`. Id rỗng → Insert, else Update. Transaction. | EF | Nhánh update map FULL DTO→entity rồi chỉ MarkDirty 3 field → các field khác (vd IdParent/IdKho) trong DTO **không** được persist trừ khi MarkDirty; dễ lệch nếu DTO đổi parent. |
| 4 | Gán sản phẩm vào vị trí | Nút "Thêm sản phẩm" (sanpham-vitri-add) | `POST api/ViTriKho/SaveSanPhamVitri` | `SBAuthorize` | internal-user | đã chọn 1 vị trí | Theo `IdKho`+`IdViTriKho`+`LstIdSanPham`: lọc SP **chưa** có trong vị trí (Except cái đã tồn tại) → insert `ViTriSanPham`. Idempotent (không nhân đôi). Transaction. | EF | Không chặn 1 SP nằm ở **nhiều** vị trí khác trong cùng kho ở đường này (chỉ import #6 mới chặn). |
| 5 | Lưới SP theo vị trí | Lưới pane phải | `POST api/ViTriKho/GetDSSanPhamTheoViTriKho` | `SBAuthorize` | internal-user | đã chọn vị trí | Lấy `IdSanPham` từ ViTriSanPham (DonViId+IdChiNhanh+IdKho+IdViTriKho) → join SanPham. Filter KeyWord (Ten/Barcode), IdNhomSanPham. Paging + sort (default `NgayCapNhat` desc). Trả data + totalCount. | EF | — |
| 6 | Lấy cấu hình SP đã gán của vị trí | (preselect khi mở popup gán) | `POST api/ViTriKho/GetDetailConfig` | `SBAuthorize` | internal-user | mở popup gán SP | Trả `List<Guid>` IdSanPham đã gán (DonViId+IdChiNhanh+IdKho+IdViTriKho). | EF | Method khai báo `async` nhưng không `await` (L294) — chạy đồng bộ, vô hại. |
| 7 | Bỏ 1 SP khỏi vị trí | Nút xóa dòng lưới | `POST api/ViTriKho/DeleteSanPhamVitri` | `SBAuthorize` | internal-user | dòng SP | Tìm ViTriSanPham theo DonViId+IdChiNhanh+IdKho+IdViTriKho+IdSanPham; không thấy → "Không tìm thấy dữ liệu!". Có → DeleteAsync, transaction. | EF | — |
| 8 | Xóa 1 vị trí (kèm SP) | Nút xóa node cây | `POST api/ViTriKho/DeleteVitri` | `SBAuthorize` | internal-user | node level>0 | Xóa `ViTriKho` (theo IdViTriKho) + **tất cả** `ViTriSanPham` của vị trí đó, cùng transaction. | EF | `DeleteVitri` tìm vị trí theo Id **không** kèm DonViId/IdChiNhanh (L349) → về lý thuyết xóa được vị trí của tenant khác nếu biết Id (kiểm soát đa tenant lỏng). Cũng không cascade theo IdParent (xóa node cha không tự xóa node con). |
| 9 | Tải template import SP-vị trí | Menu "Import vị trí sản phẩm" → Tải mẫu | `GET api/ViTriKho/DownloadFileTemplateImportViTriSanPham` | `SBAuthorize` | internal-user | popup import | Load `Template/TemplateImportViTriSanPham.xlsm`, ghi dòng "tối đa 1000", trả file `ViTriSanPhamTemplateImport.xlsx`. | GemBox | License GemBox hardcode `ERDD-...` (L385). Permission gốc bị comment (L84). |
| 10 | Import Excel SP-vị trí (vào bảng tạm) | Popup "Import vị trí sản phẩm" → Tải lên | `POST api/ViTriKho/ImportViTriSanPham` (multipart) | `SBAuthorize` | internal-user | đã chọn IdKho + file | Đọc từ row 7 (cột TenViTriKho, MaSanPham). Max 1000. `AllowRemoveOldVitri=true` → xóa vị trí cũ của các SP đó trong kho trước. Validate từng dòng: trống / vị trí không tồn tại / mã SP không tồn tại / SP đã có ở vị trí này / SP đã cấu hình vị trí khác trong kho → ghi lỗi. Dòng OK + dòng lỗi đều insert vào `ViTriSanPhamImport` (Success/Error). Lỗi → sinh file fail trả `Id`. | EF | 1 SP chỉ được 1 vị trí/kho (chặn ở L499). `AllowRemoveOldVitri` xóa thật trước khi validate → nếu import lỗi sau đó, vị trí cũ đã mất (không rollback xóa). |
| 11 | Lưới preview dữ liệu import | Lưới trong popup import | `POST api/ViTriKho/GetDSViTriSanPhamImport` | `SBAuthorize` | internal-user | sau khi upload | Lấy `ViTriSanPhamImport` theo `NguoiTaoUserId = currentUser.Id`, filter TrangThaiImport, paging. Trả grid + TotalSuccess/TotalError. | EF | Scope theo **người tạo**, không theo DonViId ở filter chính (chỉ tổng hợp dùng cả 2). |
| 12 | Xác nhận import (commit) | Nút "Hoàn tất import" | `POST api/ViTriKho/ImportViTriSanPhamComplete` | `SBAuthorize` | internal-user | có data tạm | Lấy bản ghi tạm Success (DonViId+người tạo) → insert vào `ViTriSanPham` (bỏ trùng vị trí+SP), rồi **xóa toàn bộ** bảng tạm của user. Không có data → lỗi. Transaction. | EF | `lstViTriSPCheck` query dùng `lstDataInsert` (L685) — biến **rỗng** lúc đó → list check rỗng → vòng lặp thực chất luôn add; dedup dựa trên DB hiện tại **không** hoạt động như tên gọi. Có thể tạo trùng nếu DB đã có cặp vị trí-SP. |
| 13 | Xóa data import tạm theo trạng thái | Nút "Xóa tất cả / xóa lỗi" | `POST api/ViTriKho/DeleteViTriSanPhamImportByTrangThai` | `SBAuthorize` | internal-user | có data tạm | Xóa `ViTriSanPhamImport` (DonViId+người tạo, optional theo TrangThaiImport). | EF | — |
| 14 | Tải file lỗi import SP-vị trí | Link "Tải file lỗi" | `GET api/ViTriKho/DownloadDataWrongViTriSanPham/{id}` | `SBAuthorize` | internal-user | có file lỗi | Đọc file `ImportFail/ViTriSanPham/{UserName}/{id}` từ WebRootPath, trả `DataImportViTriSanPhamFaild.xlsx`. | file | Đường dẫn theo UserName + id (file name) — không kiểm chéo owner ngoài UserName trong path. |
| 15 | Tải DS vị trí kho để cập nhật | Menu "Cập nhật vị trí kho" → Tải DS | `GET api/ViTriKho/DownloadFileDanhSachViTriKho/{idKho}` | `SBAuthorize` | internal-user | popup cập nhật | Đổ tất cả `ViTriKho` (DonViId+idKho) vào template `ViTriKho_Update.xlsm` (Ten, ThuTu, IsViTriCuoi 0/1) từ row 6; trả `ViTriKho_Update.xlsx`. | GemBox | — |
| 16 | Import Excel cập nhật vị trí kho | Popup "Cập nhật vị trí kho" → Tải lên | `POST api/ViTriKho/ImportExcelUpdateViTriKho` (multipart) | `SBAuthorize` | internal-user | đã chọn IdKho + file | Đọc từ row 6 (TenViTriKho, ThuTu, IsViTriCuoi). Mỗi dòng: trống tên / vị trí không tồn tại → lỗi. OK → update `ThuTu` + `IsViTriCuoi` (1→true) của vị trí (match theo Ten trong kho), `MarkDirty`. Lỗi → sinh file fail trả Id. | EF | Match vị trí theo **Ten** (không theo Id) → trùng tên trong cùng kho lấy bản ghi đầu. Thông báo kết quả dùng `lstErrorDataUpdated.Count` (số lỗi) ở chỗ "thành công N bản ghi" (L874) — message số liệu sai. |
| 17 | Tải file lỗi cập nhật vị trí | Link "Tải file lỗi" | `GET api/ViTriKho/DownloadDataUpdatedWrongViTriKho/{id}` | `SBAuthorize` | internal-user | có file lỗi | Đọc `ImportFail/ViTriKho_Update_Fail/{UserName}/{id}` → trả `ViTriKho_Update_Fail.xlsx`. | file | — |

## Quy tắc nghiệp vụ (gating)

- **Một sản phẩm → một vị trí trong một kho** (chỉ enforced ở luồng import #10): chặn nếu SP đã ở vị trí này (`r.IdSanPham == sanPham.Id`), hoặc SP đã cấu hình vị trí khác trong kho (`existedViTri.Any()`). Luồng gán tay #4 KHÔNG enforce ràng buộc này.
- **`AllowRemoveOldVitri` (import #10):** true → xóa các `ViTriSanPham` cũ của những SP trong file (theo IdKho) TRƯỚC khi validate/insert. false → giữ nguyên, dòng trùng sẽ báo lỗi.
- **`IsViTriCuoi`:** cờ vị trí lá (cuối cùng, được phép gán SP). Import #16 map `1→true`, khác `→false`.

## Quan hệ
- **Gọi sang:** không gọi service ngoài/đối tác. Chỉ EF trên `WarehouseProductDataContext` + GemBox Excel (template trong `Template/`, file lỗi ghi xuống `WebRootPath/ImportFail/...`). DI có `ICommonService`/`ICommonCacheService`/`IRedisCacheService` nhưng các method hiện không dùng tới.
- **Dữ liệu (entities):** ghi/đọc `ViTriKho` (Ten, ThuTu, IsViTriCuoi, IdParent, IdKho, IdDonVi, IdChiNhanh), `ViTriSanPham` (IdKho, IdViTriKho, IdSanPham), bảng tạm `ViTriSanPhamImport`; đọc `Kho`, `SanPham` (Ma/Ten/Barcode).
- **Ripple / đồng bộ:** KHÔNG bắn message/đồng bộ cross-service. Dữ liệu vị trí chỉ phục vụ tra cứu/soạn phiếu nội bộ kho.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/ViTriKhoController.cs`
  - L13 `[TienIchAttribute(... MaTienIchSCRM.TI_Kho)]` — tiện ích thuộc nhóm Kho.
  - L84 permission key (DV-AD-TONG...) bị **comment-out** → mọi action chỉ còn `[SBAuthorize]` mức login, không phân quyền chi tiết.
  - Import dùng `[FromForm] IFormFile file` (L96, L144) — multipart.
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/ViTriKhoService.cs`
  - L120 `GetTreeViTriKho`: `_viTriKhoRepos.TableWithNolock()` lấy vị trí **không lọc IdDonVi/IdChiNhanh** (chỉ kho mới scope) → đa tenant lỏng.
  - L294 `GetDetailConfigAsync`: `async` nhưng không `await`.
  - L349 `DeleteVitriAsync`: tìm vị trí theo Id không kèm DonViId/IdChiNhanh; không cascade node con theo IdParent.
  - L431-436 `ImportViTriSanPham`: `AllowRemoveOldVitri` xóa vị trí cũ TRƯỚC validate, không rollback nếu import lỗi sau đó.
  - L685-692 `ImportViTriSanPhamCompleteAsync`: dedup query dùng biến `lstDataInsert` đang RỖNG → check trùng vô hiệu, có thể insert cặp vị trí-SP trùng DB.
  - L874 message "thành công N bản ghi" dùng `lstErrorDataUpdated.Count()` (số lỗi) → số liệu hiển thị sai.
  - License GemBox hardcode `ERDD-TNCL-YKZ5-3ZTU` lặp nhiều chỗ (L385, L588, L633, L732, L899, L942).
- FE `scrm_frontend/src/app/services/warehouseproduct/vitrikho.service.ts` — base `api/ViTriKho`, route FE↔BE khớp (không lệch). Màn hình: `pages/WarehouseProduct/quanly-nhatkho/cauhinhkho-vitri-sanpham/` (cây trái + lưới phải + popup import/cập nhật).
