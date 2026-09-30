---
controller: ComBoSpaController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý kho > Quản lý combo (route warehouse-product/combo, combo-spa.component) — list combo, nút Thêm mới combo thương mại / combo dịch vụ + Import, menu thao tác per-row (Sửa, Xóa, Lịch sử bán)"
muc_dich: "Quản lý combo/gói liệu trình spa (gói sản phẩm + dịch vụ bán kèm): CRUD combo cấp đơn vị + cấp chi nhánh, import Excel, bật/tắt sử dụng, xem lịch sử bán; là nguồn data combo cho bán hàng (thu ngân/sale), đặt lịch, khuyến mãi, website."
alias: ["combo", "combo spa", "gói liệu trình", "gói combo", "quản lý combo", "combo thương mại", "combo dịch vụ", "combo đóng sẵn"]
auth: mixed
state_machine: []
deprecated: false
entities: [ComBoSpa, ComBoSpaDichVu, ComBoSpaSanPham, ComBoSpaChiNhanh, ComBoSpaKhachHang]
stored_procs: []
depends: [WarehouseProduct, ImportComboService, CommonService]
cross_module_in: [Order, OrderLogistic, Promotion, Booking, Customer, Report, Website]
cross_module_out: [ComBoSpaKhachHang (sở hữu combo của khách), ComBoSpaChiNhanh (phân bổ combo theo chi nhánh)]
---

# ComBoSpa — Quản lý combo (gói liệu trình spa: sản phẩm + dịch vụ)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm/list combo theo điều kiện | "Tìm kiếm" (icon search) → `gridLoadData` | POST `api/ComBoSpa/TimTheoDieuKien` | `ComBoSpa_TimTheoDieuKien` | public | màn list | Trả list combo (kèm dsDichVu + dsSanPham để expand row) theo keyword/loại combo (IsComboDongSan) + phân trang | EF | - |
| 2 | Thêm mới combo thương mại | "Thêm mới combo thương mại" (plus) → `onAddNew(true)` | POST `api/ComBoSpa/CreateCombo` | `ComBoSpa_CreateCombo` | public (role chỉ DV-AD-TONG) | luôn hiện nếu có quyền | Tạo combo IsComboThuongMai=true (chỉ sản phẩm, **không thêm dịch vụ**). Validate: tên hợp lệ, mã không trùng theo đơn vị+ngành nghề, SP/DV thuộc tất cả chi nhánh | EF | Combo thương mại + có dịch vụ → chặn "Combo thương mại thì không thêm dịch vụ" (ComBoSpaService.cs:1150) |
| 3 | Thêm mới combo dịch vụ | "Thêm mới combo dịch vụ" (plus) → `onAddNew(false)` | POST `api/ComBoSpa/CreateCombo` | `ComBoSpa_CreateCombo` | public | luôn hiện nếu có quyền | Tạo combo dịch vụ (gói liệu trình: cho phép dsDichVu + dsSanPham). Cùng endpoint #2, khác cờ IsComboThuongMai | EF | - |
| 4 | Sửa combo | "Chỉnh sửa" (icon edit) → `onEdit(id, isComboThuongMai)` | POST `api/ComBoSpa/Update/{id}` | `ComBoSpa_Update` | public | per-row | Cập nhật combo cấp đơn vị; mở popup `ComboSpaEditComponent` | EF | - |
| 5 | Bật/tắt sử dụng | nz-switch cột "Sử dụng" → `onChangeSuDung` | GET `api/ComBoSpa/ChangeSuDungCombo/{id}` | `ComBoSpa_ChangeSuDungCombo` | public | per-row toggle | Đảo cờ IsSuDung của combo (ẩn/hiện combo khỏi bán hàng). Lỗi → FE revert lại switch | EF | - |
| 6 | Xóa combo | menu "Xóa" (icon delete) → `onDelete` | GET `api/ComBoSpa/Xoa/{id}` | `ComBoSpa_Delete` | public | per-row, confirm | Soft-delete combo (IsDelete) | EF | - |
| 7 | Lịch sử bán | menu "Lịch sử bán" → `onViewLichSuBan` | POST `api/ComBoSpa/ViewLichSuBan` | `ComBoSpa_ViewLichSuBan` | public | per-row | Trả lịch sử bán của 1 combo (popup `ComboSpaLichSuBanComponent`) | EF | - |
| 8 | Import combo (download template) | "Import combo mới" → flow → tải mẫu | POST `api/ComBoSpa/DownloadFileTemplateImportCombo` | `ComBoSpa_DownloadFileTemplateImportDichVu` | public | popup import | Trả file `ImportCombo.xlsx` (template) | GemBox Excel | Permission key tên `..._ImportDichVu` không khớp tên combo — lệch convention |
| 9 | Import combo (upload) | "Import combo mới" → upload | POST `api/ComBoSpa/ImportCombo` | `ComBoSpa_ImportCombo` | public | popup import | Đẩy file Excel → tạo combo hàng loạt qua `ImportComboService` | EF | - |
| 10 | Tải file dòng combo import lỗi | (màn import) | GET `api/ComBoSpa/DownloadDataWrongCombo/{id}` | `ComBoSpa_DownloadDataWrongCombo` | public | sau import lỗi | Trả file `DataImportComboFail.xlsx` các dòng lỗi | GemBox Excel | - |
| 11 | CRUD combo theo CHI NHÁNH | (màn combo-chinhanh, route combo-chinhanh) | POST `CreateComboChiNhanh` · POST `UpdateAsyncChiNhanh/{id}` · GET `GetByIdAsyncChiNhanh/{id}` · POST `TimTheoDieuKienAsyncChiNhanh` · GET `XoaTheoChiNhanh/{id}` | `ComBoSpa_CreateComboChiNhanh` / `_UpdateAsyncChiNhanh` / `_GetByIdAsyncChiNhanh` / `_TimTheoDieuKienAsyncChiNhanh` / `_XoaTheoChiNhanh` | public | màn combo cấp chi nhánh | CRUD combo do chi nhánh tự tạo (tách bộ quyền riêng, role gồm DV-AD-CN, DV-QLDV, DV-QL-KHO...) | EF | Bộ endpoint song song với combo đơn vị — sửa logic combo nhớ đối chiếu cả 2 |
| 12 | Lấy combo cho bán hàng/dropdown | nhiều: `GetListComBo/{isSuDung}`, `GetListComBo_DV/{isSuDung}`, `GetListComBo_DV_Lazy`, `GetComboSpaPaging`, `GetComBoSpa_DonVi_Paging`, `GetListComBoThuongMaiPt`, `GetListComBoPt`, `GetComBoById/{id}`, `GetComboByIds`, `GetComBoByIdsThuNgan` | tương ứng `ComBoSpa_GetListComBo*` / `_GetComBoById` / `_GetComboByIds` / `_GetComBoByIdsThuNgan` ... | public | dropdown chọn combo (sale/thu ngân/đặt lịch) | Trả combo theo chi nhánh / đơn vị / thương mại (đã gộp ds dịch vụ + ds sản phẩm vào 1) cho các màn bán hàng nội bộ FE | EF | Rất nhiều biến thể list gần trùng — gom theo mục đích, không tách dòng |
| 13 | Lấy combo có thể mua + đã mua theo KH | (màn chọn combo cho khách) | GET `api/ComBoSpa/GetListComBoActiveAndOwn` | `ComBoSpa_GetListComBoActiveAndOwn` | public | có idKhachHang | Trả combo còn bán được + combo khách đã sở hữu (ComBoSpaKhachHang) | EF | - |
| 14 | Lấy combo active (không đăng nhập) | (website/landing) | POST `api/ComBoSpa/GetListComboActive` | (none) | anonymous | - | `[AllowAnonymous]` — trả list combo active, dùng cho luồng public/website | EF | Anonymous: bất kỳ ai gọi được, không cần token |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] **Order / OrderLogistic** (bán hàng, thu ngân) gọi `GetComboByIdsPrivate`, `GetComboFullByIds`, `PrivateGetComboByIds`, `GetComBoByIdsThuNganByChiNhanhPrivate`, `TimComBoSpaTheoIds_Private`, `TimComBoSpaTheoId_FullDetail_Private` → lấy snapshot combo (giá, ds SP/DV) để dựng đơn hàng combo.
- [internal] **Website** (`ComBoSpaListController`, `ComboSpaDaMuaController`) gọi `LayTatCa_Private`, `LayTatCaComboPrivate/{id}`, `TimComBoSpaTheoId_Private` → hiển thị combo trên website + combo khách đã mua.
- [internal] **Promotion** (khuyến mãi) gọi `GetComboByIds`/private combo → áp KM lên combo.
- [internal] **Booking** (đặt lịch) gọi list combo private → chọn combo cho lịch hẹn.
- [internal] **Customer** (hợp đồng khách hàng) gọi combo private → gắn combo vào hợp đồng/sở hữu của khách.
- [internal] `TimTheoDieuKien_Private` (ComBoSpaFilterDtoAPI) — list combo cho service ngoài.
- [internal] `GetInfoCombo_SoCongThuongCapPhep_ByListId` — trả thông tin "số công thương cấp phép" của combo theo list id + đơn vị (đối soát/báo cáo).
- [internal] `GetDictComboKhachHang` — dict combo theo khách hàng.

## Quy tắc nghiệp vụ (validate khi tạo/sửa combo — blueprint để trace)
1. **Tên combo** rỗng/whitespace → chặn "Tên không hợp lệ" (TabIndex 0). (ComBoSpaService.cs:1143)
2. **Combo thương mại + có dịch vụ**: `IsComboThuongMai == true && DsDichVu.Any()` → chặn "Combo thương mại thì không thêm dịch vụ". → Combo thương mại chỉ gồm sản phẩm. (ComBoSpaService.cs:1150)
3. **Mã trùng** (chỉ khi tạo mới, `!isEdit`): tồn tại combo chưa xóa cùng `Ma` + `IdDonVi` + `IdNganhNghe` → chặn "Đã tồn tại mã combo trên...". (ComBoSpaService.cs:1159-1169)
4. **Giới hạn số lượng** (`IsGioiHanSoLuong`):
   - `SoLuongToiDa < 0` → chặn "Số lượng tối đa không được nhỏ hơn 0".
   - `IsGioiHanSoLuong == true` nhưng `SoLuongToiDa` null/≤0 → chặn "Hãy nhập số lượng tối đa".
   - `IsGioiHanSoLuong != true` nhưng có `SoLuongToiDa > 0` → chặn "Không phải combo giới hạn nên không được nhập số lượng tối đa". (ComBoSpaService.cs:1395-1415)
5. **SP/DV thuộc chi nhánh**: khi tạo (combo đơn vị) check SP/DV có được tất cả chi nhánh cung cấp; thiếu → trả lỗi TabIndex 1 (SP) / 2 (DV). (ComBoSpaService.cs:695-701)
6. **Combo đóng sẵn** (`IsComboDongSan`): cờ phân loại; filter list theo IsComboDongSan (true = đóng sẵn / false = không đóng sẵn). (ComBoSpaService.cs:275-282)

## Quan hệ
- **Gọi sang:** `ImportComboService` (import Excel + file lỗi), `CommonService` (GetListComboActive), check chi nhánh SP/DV nội bộ WarehouseProduct.
- **Dữ liệu (entities):** `ComBoSpa` (đầu combo: Ten/Ma/GiaGoc/GiaBan/ChietKhau/HanSuDung/IsComboThuongMai/IsComboDongSan/IsGioiHanSoLuong/SoLuongToiDa/SoLuongDaBan/WebsiteMoTa*), `ComBoSpaDichVu` (chi tiết dịch vụ), `ComBoSpaSanPham` (chi tiết sản phẩm), `ComBoSpaChiNhanh` (phân bổ theo chi nhánh), `ComBoSpaKhachHang` + `ComBoSpaKhachHangChiTiet` (combo khách đã mua/sở hữu).
- **Ripple / đồng bộ:** combo là master data → đổi giá/ds SP-DV/IsSuDung ảnh hưởng đơn hàng combo (Order/OrderLogistic), KM áp combo (Promotion), đặt lịch combo (Booking), hợp đồng KH (Customer), hiển thị + đã-mua trên Website, báo cáo (Report). Snapshot lấy qua endpoint private nên đơn cũ thường giữ giá tại thời điểm bán.

## Lỗi đã biết / lưu ý kỹ thuật
- **Controller `partial`** (`public partial class ComBoSpaController`) — có thể còn endpoint ở file partial khác; node cover file `WarehouseProduct.API/Controllers/ComBoSpaController.cs`. *Cần xác nhận* có partial bổ sung.
- **Lệch convention permission key**: `DownloadFileTemplateImportCombo` dùng quyền `ComBoSpa_DownloadFileTemplateImportDichVu` (tên "DichVu" trong key của combo) — dễ nhầm khi map quyền.
- **2 endpoint anonymous** trùng logic: `GetListComboActive` và `GetComboFullByIdsWithNoAuthen` (`[AllowAnonymous]`) — gọi được không cần đăng nhập, cần xác nhận có chủ ý (rủi ro lộ data combo nếu không kiểm soát đầu vào).
- **Bộ endpoint combo CHI NHÁNH song song combo ĐƠN VỊ** (CreateComboChiNhanh / UpdateAsyncChiNhanh / TimTheoDieuKienAsyncChiNhanh / XoaTheoChiNhanh...) — sửa logic 1 nhánh nhớ đối chiếu nhánh kia, tránh lệch hành vi.
- **Rất nhiều biến thể list/get** (GetListComBo, _DV, _DV_Lazy, ThuongMai, ThuongMaiPt, Pt, ByIds, ThuNgan, paging đơn vị/chi nhánh...) phục vụ từng màn bán hàng khác nhau — khi debug "combo không hiện ở màn X" phải xác định đúng endpoint màn đó dùng.
- **`GetByIdAsync`/`GetComBoByIdAsync`/`GetComboByIds` bọc try/catch rethrow `new Exception(ex.Message)`** (controller) làm mất stack trace gốc — khó trace lỗi sâu.
- Lưu ý nhóm controller cùng họ: `ComBoSpaDichVuController`, `ComBoSpaSanPhamController`, `ComBoSpaKhachHangController` là các controller riêng cho từng bảng chi tiết — node này chỉ mô tả `ComBoSpaController` (đầu combo).
