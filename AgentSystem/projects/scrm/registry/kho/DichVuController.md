---
controller: DichVuController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Quản lý dịch vụ (quanly-dichvu.component) — list dịch vụ spa theo nhóm, nút Thêm mới + Sửa/Xóa per-row + popup edit-dichvu / edit-nhomdv / import-dichvu"
muc_dich: "Quản lý danh mục dịch vụ spa (CRUD): tên/giá/nhóm/ảnh, tùy chọn theo nhân viên/tùy chọn khác, tính tiền theo block/giờ, dịch vụ bàn, VAT; import Excel; cung cấp dropdown dịch vụ cho thu ngân/sale/đặt lịch/website."
alias: ["dịch vụ", "quản lý dịch vụ", "dịch vụ spa", "danh mục dịch vụ", "service spa", "import dịch vụ", "dịch vụ chi nhánh"]
auth: mixed
state_machine: []
deprecated: false
entities: [DichVu, NhomDichVu, TuyChonDichVuKhac, TuyChonNhanVien, SanPhamThem]
stored_procs: []
depends: [WarehouseProduct, Promotion, Website/Service-private, ThuNgan/Order, DatLich]
cross_module_in: [ThuNgan/Order (TimTheoIdsThuNgan*, TimFullDvTheoIds, DsDichVuBan), Website (LayTatCa_Private, TimDichVuId_Private, GetAllDichVuBasicPrivate, PrivateGetDichVuByIds), DatLich (GetDichVuByChiNhanh — anonymous), Contact/Sale (GetDichVuComBoToInsertContact — anonymous)]
cross_module_out: [Combo spa (SanPhamThem trong dịch vụ combo), TheDichVu / TheDichVuChiNhanhTao (thẻ dịch vụ tham chiếu DichVu)]
---

# DichVuController — Quản lý dịch vụ (danh mục dịch vụ spa)

> Controller `partial` (`public partial class DichVuController`) — có thể còn endpoint ở file partial khác. Tồn tại bản trùng tên ở Area SandBox (`Areas/SandBoxArea/DichVuController.cs`, route `api/SanboxArea/DichVu`, `deprecated`/nội bộ hệ thống — xem dòng SA bên dưới). Node này cover chủ yếu bản `Controllers/DichVuController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | "Tìm kiếm" (list quanly-dichvu) | POST `api/DichVu/TimTheoDieuKien` | `DichVu_TimTheoDieuKien` | public | luôn | List dịch vụ theo đơn vị/chi nhánh/nhóm/keyword (grid filter) | EF | - |
| 2 | Thêm mới dịch vụ | "Thêm mới" → popup edit-dichvu | POST `api/DichVu/ThemMoi` | `DichVu_ThemMoi` | public | luôn | Validate (ValidateInput) → check trùng tên trong đơn vị → tạo DichVu + tùy chọn NV/khác + SP thêm | EF | ValidateInput bug: nhánh "thiếu giá tùy chọn khác" không bao giờ chạy (xem Lỗi đã biết) |
| 3 | Sửa / mở chi tiết | "Sửa" / xem (icon edit) → popup edit-dichvu | GET `api/DichVu/TimTheoId/{id}` + POST `CapNhatThongTin` | `DichVu_TimTheoId` / `DichVu_CapNhatThongTin` | public | per-row | TimTheoId trả full DTO để load popup; CapNhatThongTin validate lại + cập nhật (typeCreateUpdate=2, check trùng tên loại trừ chính nó) | EF | cùng ValidateInput bug |
| 3b | Tìm theo id (basic) | (load nhẹ cho popup/ref) | GET `api/DichVu/TimTheoIdBasic/{id}` | `DichVu_TimTheoIdBasic` | public | - | Trả thông tin cơ bản dịch vụ (không full chi tiết) | EF | - |
| 4 | Xóa dịch vụ | "Xóa" (icon delete, per-row) | POST `api/DichVu/Xoa/{id}` | `DichVu_Xoa` | public | per-row | Xóa dịch vụ theo id (soft/hard theo XoaTheoId) | EF | - |
| 5 | Lấy dropdown dịch vụ theo nhóm | dropdown (filter/popup) | GET `api/DichVu/GetDichVuByNhomDichVu` | `DichVu_GetDichVuByNhomDichVu` | public | - | Group dịch vụ theo nhóm (optional idChiNhanh) cho dropdown | EF | - |
| 5b | Dropdown dịch vụ (all basic / lazy) | dropdown chọn dịch vụ (sale, KM, voucher, thu ngân...) | GET `GetAllDichVuBasic` / POST `GetAllDichVuBasicLazy` | `DichVu_GetAllDichVuBasic` | public | - | List basic toàn bộ dịch vụ (lazy = phân trang dropdown). Quyền mở rộng nhiều role sale/mkt/page/chia sẻ | EF | - |
| 5c | Lấy theo id / ids | dropdown / load chi tiết nhiều dịch vụ | GET `GetDichVuById/{id}` · POST `GetDichVuByIds` | `DichVu_GetDichVuById` / `DichVu_GetDichVuByIds` | public | - | Trả 1 hoặc nhiều dịch vụ theo id(s) | EF | - |
| 6 | Phân trang dịch vụ | list/dropdown phân trang theo nhóm | GET `GetDichVuPaging` · GET `GetDichVu_DonVi_Paging` | `DichVu_GetDichVuPaging` / `GetDichVu_DonVi_Paging` | public | - | List dịch vụ phân trang, sort theo IdNhom (keyWord/pageIndex/pageSize/idChiNhanh) | EF | - |
| 7 | Lấy tất cả theo chi nhánh | (màn cần full theo CN) | GET `GetAllDichVuTheoChiNhanh` | `[SBAuthorize]` (mở — chỉ cần đăng nhập) | public | - | List dịch vụ theo chi nhánh của user | EF | quyền rỗng, không gắn key riêng |
| 8 | Tìm DV khung giờ theo ids | (đặt lịch / khung giờ) | POST `TimDvKhungGioTheoIds` | `[SBAuthorize]` (mở) | public | - | Trả dịch vụ + cấu hình khung giờ theo list ids | EF | quyền rỗng |
| 9 | Download file mẫu import | "Import" → import-dichvu | POST `DownloadFileTemplateImportDichVu` | `DichVu_DownloadFileTemplateImportDichVu` | public | - | File `ImportSanPham.xlsx` (template) | GemBox | - |
| 10 | Import danh sách dịch vụ | "Import" (upload file) → import-dichvu | POST `ImportDichVu` (IFormFile) | `DichVu_ImportDichVu` | public | - | Parse Excel → validate → tạo dịch vụ; bản lỗi giữ lại để tải về | EF | - |
| 11 | Tải file dịch vụ import lỗi | link tải dòng lỗi (màn import) | GET `DownloadDataWrongDichVu/{id}` | `DichVu_DownloadDataWrongDichVu` | public | sau import lỗi | File `DataImportDichVuFail.xlsx` các dòng lỗi | GemBox | - |
| 12 | CRUD dịch vụ "chi nhánh tạo" | (luồng dịch vụ do chi nhánh tự tạo) | POST `TimTheoDieuKienAsyncDvChiNhanh` · `ThemMoiDichVuChiNhanh` · GET `TimTheoIdDvChiNhanh/{id}` · POST `CapNhatThongTinDvChiNhanh` · `XoaTheoIdDvChiNhanh/{id}` | `DichVu_*DvChiNhanh` (TimTheoDieuKienAsyncDvChiNhanh / ThemMoiDichVuChiNhanh / TimTheoIdDvChiNhanh / CapNhatThongTinDvChiNhanh / XoaTheoIdDvChiNhanh) | public | quyền theo role kho/QLDV/chi nhánh | Như CRUD chính nhưng cho dịch vụ chi nhánh tự tạo; `ThemMoiDichVuChiNhanh` gọi ValidateInput với `isCheck=false` (BỎ check chọn khu vực kho) | EF | dùng chung ValidateInput → cùng bug |
| 13 | Lấy chi tiết DV (không login) | (trang public/website preview) | GET `GetDetailDichVuNotLogin/{id}` | — | anonymous | - | Trả chi tiết dịch vụ không cần user đăng nhập | EF | `[AllowAnonymous]` — không gắn quyền |
| 14 | Lấy DV combo để chèn contact | (luồng đặt lịch/sale insert contact) | POST `GetDichVuComBoToInsertContact` | — | anonymous | - | Trả dịch vụ combo phục vụ insert contact | EF | `[AllowAnonymous]` |
| 15 | Lấy DV theo chi nhánh (đặt lịch) | (màn đặt lịch chọn dịch vụ) | GET `GetDichVuByChiNhanh/{chiNhanhId}` | — | anonymous | - | List dịch vụ theo chi nhánh cho đặt lịch | EF | `[AllowAnonymous]` |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] **ThuNgan/Order** gọi `TimTheoIdsThuNgan` / `TimTheoIdsThuNganByChiNhanh` / `TimTheoIdsThuNganPrivate` (DonViId/IdDonVi trong payload) → lấy dịch vụ phục vụ màn thu ngân; `TimFullDvTheoIds` → full dịch vụ theo ids; `DsDichVuBan` → danh sách dịch vụ được bán.
- [internal] **Website** gọi `LayTatCa_Private` (filter), `TimDichVuId_Private` (payload.Id), `GetAllDichVuBasicPrivate/{id}`, `PrivateGetDichVuByIds` (PrivateGetItemFilterDto), `TimTheoDieuKien_Private` (DichVuFilterDtoAPI) → đồng bộ/hiển thị dịch vụ trên website.

## Quan hệ
- **Gọi sang:** chủ yếu nội bộ service `WarehouseProduct` (repo DichVu, nhóm dịch vụ, sản phẩm thêm). Import dùng GemBox Spreadsheet.
- **Dữ liệu (entities):** `DichVu` (Ma, Ten, GiaDichVu, IdNhomDichVu, IdImage, PhutPhucVu, IsTinhGioTheoBlock/SoPhutTinhBlock/GiaBlock, IsTinhGio, IsDichVuBan, TypeVatPercent/ValueVat), `NhomDichVu`, `TuyChonDichVuKhac` (Ten, Gia), `TuyChonNhanVien` (NhanVien, Gia), `SanPhamThem` (dịch vụ combo). Lọc theo `IdDonVi` của user.
- **Ripple / đồng bộ:** dịch vụ được tham chiếu bởi thu ngân/đơn hàng, thẻ dịch vụ (`TheDichVu`/`TheDichVuChiNhanhTao`), combo spa, khuyến mãi/voucher (dropdown chọn dịch vụ), đặt lịch và website. Đổi giá/xóa dịch vụ → ảnh hưởng các màn đang dùng id đó (không có cascade tự động — cần cẩn trọng khi xóa dịch vụ đã phát sinh giao dịch).

## Quy tắc nghiệp vụ (gating / validate khi Thêm/Sửa — `ValidateInput`)
Thứ tự kiểm tra (DichVuService.cs ~296–410), trả `ResultWithErrorTabIndex(msg, tabIndex)`:
1. `isTinhTienTheoBlock == true` → bắt buộc `soPhutTinhBlock > 0` và `giaBlock > 0`.
2. `(isTinhTienTheoBlock == true || isDichVuBan == true)` mà `isTinhGio != true` → lỗi: phải tích "dịch vụ tính giờ".
3. `ten` rỗng → "Hãy nhập tên dịch vụ".
4. `idImage` rỗng → "Hãy chọn ảnh dịch vụ" (ảnh bắt buộc).
5. `giaDichVu == null` → "Hãy nhập giá dịch vụ" — **cho phép giá ÂM** (check `< 0` đã bị comment, DichVuService.cs:325–327).
6. `idNhomDichVu` null/empty → "Hãy chọn nhóm dịch vụ".
7. `listIdChiNhanh` rỗng **VÀ `isCheck == true`** → "Hãy chọn khu vực kho hàng" (tabIndex 2). DV chi nhánh tạo gọi với `isCheck=false` → bỏ qua bước này.
8. `phutPhucVu == null || < 0` → "Phút phục vụ không hợp lệ".
9. typeCreateUpdate==1 (thêm): check trùng tên trong `IdDonVi` (so sánh bỏ space + upper). typeCreateUpdate==2 (sửa): check trùng tên loại trừ `Id` hiện tại.
10. Nếu có `lstTuyChonDichVuKhac`: thiếu Ten → lỗi; thiếu Gia (`Gia == null || Gia < 0`) → **BUG: guard sai biến, không bao giờ chặn** (xem Lỗi đã biết).
11. Nếu có `lstTuyChonNhanVien`: thiếu nhân viên → lỗi; thiếu giá (`isThieuGiaNv`) → lỗi (chỗ này guard ĐÚNG).

## Lỗi đã biết / lưu ý kỹ thuật
- **BUG ValidateInput — check "thiếu giá tùy chọn khác" không bao giờ chạy** (`DichVuService.cs:379-380`): tính `var isThieuGia = lstTuyChonDichVuKhac.Any(a => a.Gia == null || a.Gia < 0);` nhưng guard lại `if (isThieuTen == true)` (copy-paste sai biến). Hệ quả: dịch vụ có "tùy chọn khác" thiếu/âm giá vẫn lưu được. So sánh: nhánh `TuyChonNhanVien` ngay dưới dùng đúng `isThieuGiaNv` (DichVuService.cs:394-395). Fix: đổi `if (isThieuTen == true)` ở dòng 380 thành `if (isThieuGia == true)`.
- **Bug lặp lại ở `ValidateInput_SA`** (`DichVuService.cs:1927-1928`): cùng pattern — tính `isThieuGia` rồi guard `if (isThieuTen == true)`. Cần fix cả 2 chỗ. ValidateInput_SA dùng cho controller Area SandBox (`*_SA`).
- **`TimTheoDieuKien` cho phép giá dịch vụ ÂM có chủ ý** — check `giaDichVu < 0` đã bị comment (DichVuService.cs:325-326), chỉ chặn null. Lưu ý khi trace giá.
- **Controller bản SandBox trùng tên** (`Areas/SandBoxArea/DichVuController.cs`, route `api/SanboxArea/DichVu`, `TienIchAttribute(..., MaTienIchSCRM.TI_HeThong, true)` — cờ true = nội bộ/ẩn): các endpoint `*_SA` (TimTheoDieuKien_SA, ThemMoi_SA, TimTheoId_SA, CapNhat_SA, Xoa_SA), quyền `HT-AD`, quản lý "dịch vụ hệ thống" theo ngành nghề (yêu cầu `IdNganhNghe`). KHÔNG có `[SBAuthorize]` cấp controller (chỉ per-method) và không `[SBAuthorize()]` rỗng như bản chính — khác auth model.
- **Auth không đồng nhất**: phần lớn endpoint dùng permission key cụ thể, nhưng `GetAllDichVuTheoChiNhanh` / `TimDvKhungGioTheoIds` dùng `[SBAuthorize]` rỗng, và 3 endpoint `[AllowAnonymous]` (GetDetailDichVuNotLogin, GetDichVuComBoToInsertContact, GetDichVuByChiNhanh) — public hoàn toàn, cần xác nhận có chủ ý (rò rỉ dữ liệu dịch vụ theo chi nhánh không cần đăng nhập).
- **`ThemMoi` / nhiều method `virtual`** → có thể bị override ở bản kế thừa; khi trace nhớ kiểm tra lớp con.
