---
controller: CommonController
module: kho
service: WarehouseProduct
man_hinh_ui: "Nội bộ — KHÔNG có 1 màn riêng. Là controller AGGREGATOR cung cấp dropdown/lookup/autocomplete sản phẩm-dịch vụ-combo-thẻ DV-kho dùng chung cho HẦU HẾT màn của SCRM (bán hàng/thu ngân, sale, marketing, kho, đặt lịch, khuyến mãi, website, logistics) — qua FE service dropdownCommon.service.ts"
muc_dich: "Aggregator tiện ích chung của service WarehouseProduct: tổng hợp ~120 endpoint dropdown/lookup/autocomplete/get-by-ids cho sản phẩm (cha/con), dịch vụ, combo, thẻ dịch vụ, nhóm SP/DV, kho, nhà cung cấp, đơn vị tính, tồn kho, danh mục (DM); phục vụ FE đổ dropdown + service khác lấy data cross-service. Gồm cả tiện ích khởi tạo/xóa data mặc định khi đăng ký đơn vị."
alias: ["common warehouse", "dropdown chung", "tiện ích chung kho", "lookup sản phẩm dịch vụ", "autocomplete sản phẩm", "dropdownCommon", "get sản phẩm by ids", "khởi tạo dữ liệu đơn vị"]
auth: mixed
state_machine: []
deprecated: false
entities: [SanPham, DichVu, ComBoSpa, TheDichVu, NhomSanPham, NhomDichVu, Kho, KhoChiTiet, NhaCungCap, DonViTinh]
stored_procs: []
depends: [ICommonService, IKhoChiTietService, ITheDichVuService, IComBoSpaService]
cross_module_in: [Order, OrderLogistic, Sales, Marketing, Promotion, Booking, Website, Report, AuthenCustomer]
cross_module_out: [SanPham/DichVu/ComBoSpa/TheDichVu (chỉ ĐỌC — master data), CauHinhKhoCache (set cache cấu hình kho theo chi nhánh)]
---

# CommonController — Tiện ích chung WarehouseProduct (aggregator dropdown / lookup)

> Controller LỚN (~120 endpoint, 1046 dòng). Bảng dưới chỉ giữ hành động ĐẶC BIỆT / có ý nghĩa riêng (anonymous, ghi data, AI, init đơn vị). Toàn bộ dropdown / lookup / get-by-ids gom ở "Endpoint nội bộ" theo MỤC ĐÍCH. Hầu hết là `[SBAuthorize()]` rỗng (chỉ cần đăng nhập); controller-level gate `[TienIch("Common_WarehouseProduct_Controller", ..., MaTienIchSCRM.TI_Chung)]`.

## Hành động (UI / công khai — chỉ endpoint đặc biệt)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Khởi tạo dữ liệu mặc định khi ĐK đơn vị | (luồng đăng ký đơn vị — backend gọi) | POST `api/Common/KhoiTaoDuLieuMacDinh` | (none) | anonymous | đăng ký đơn vị mới | `[AllowAnonymous]` — seed data mặc định (SP/DV/kho...) cho đơn vị mới từ `InitOrgJson` | EF | Anonymous + GHI data: nhận `InitOrgJson` không token → cần kiểm soát input chặt (chỉ gọi từ luồng đăng ký) |
| 2 | Xóa dữ liệu theo đơn vị | (admin/cleanup đơn vị) | POST `api/Common/XoaDuLieuTheoDonVi` | (none — `[SBAuthorize]` mặc định) | public | cleanup đơn vị | Xóa data WarehouseProduct theo `DonViDeleteReq` | EF | Endpoint xóa hàng loạt theo đơn vị — không permission-key riêng, chỉ cần đăng nhập |
| 3 | Lấy SP theo barcode | quét barcode (thu ngân/POS) | POST `api/Common/GetSanPhamTheoBarcode` | (none) | public | màn bán hàng quét mã | Trả SP khớp barcode trong phạm vi user | EF | - |
| 4 | Lấy SP theo IMEI / theo lô-date | quét IMEI / chọn lô | POST `GetSanPhamTheoImei` · POST `GetSanPhamTheoLoDate` | (none) | public | bán hàng SP serial/lô | Trả SP theo IMEI / theo lô + hạn dùng | EF | - |
| 5 | Check tồn kho website | (luồng website đặt hàng) | POST `WebsiteCheckInventory` · POST `WebsiteCheckInventoryV2` | (none) | public | website check tồn | Kiểm tra tồn kho cho đơn website (qua `IKhoChiTietService`) | EF | V1 + V2 song song — xác định màn nào dùng bản nào |
| 6 | Tìm sản phẩm bằng AI (private) | (tích hợp AI/chatbot) | POST `api/Common/TimKiemSanPhamAI` | (none) | internal | service AI gọi | `[PrivateSBAuthorize]` — tìm SP theo tên qua `TimSPTheoTenAI`, build `UserLogin` từ `IdDonVi`/`IdChiNhanh` trong request | EF | Hardcode `PageSize=100`, `IsThuNgan=true`, `IdNhomSp=Guid.Empty` trong controller (`CommonController.cs:1019-1035`) — logic nằm ở controller thay vì service |
| 7 | Set cache cấu hình kho theo chi nhánh | (warmup cache, private) | POST `GetAndSetCauHinhKhoCache_TheoChiNhanh/{idDonVi}/{idChiNhanh}` | (none) | internal | service khác warmup | `[PrivateSBAuthorize]` — đọc + set cache cấu hình kho; side-effect ghi cache | EF + cache | Endpoint có ripple cache — đây là cross_module_out |

## Endpoint nội bộ (private / service-to-service + dropdown/lookup gom theo MỤC ĐÍCH)

**A. Dropdown danh mục (DM) & nhóm — `[SBAuthorize()]`, FE đổ select:**
- Nhóm SP/DV: `DropdownNhomSanPham`, `DropdownNhomDichVu`, `DropdownNhomSpHienThi`, `DropdownNhomDvHienThi`.
- DM sản phẩm (`DropdownDmSanPham/{type}`: 1 trạng thái, 2 đơn vị đếm, 3 dung tích, 4 NSX, 5 nhãn hiệu), DM kho (`DropdownDmKho/{type}`: tình trạng/phân loại phiếu nhập-xuất-chuyển), `DropdownDM_SP_KieuLoai`, `DropdownDM_SP_Option_KhoiTao`, `DropdownDM_Kho_KieuLoai`, `DropdownTrangThaiTheDichVu`, `RequestGetNganhNghe`, `DropdownTkCongNoPhaiTra`, `DropdownSuDungKho`.

**B. Dropdown kho — theo phạm vi user/chi nhánh:**
- `DropdownDsKho`, `DropdownDsAllKho`, `DropdownDsKhoRole`, `DropdownDsKhoTheoChiNhanh` (+ `V2` cho phép idChiNhanh null), `GetKhoByIdWithConfig/{id}`.
- Lấy kho cho đơn hàng sale: `LayDanhSachKhoChoDonHangSale`, `GetLstKhoDonHangSaleCheckKhoTong` (+ `_V2` set `IsCheckThemSaleKho=true`), `LayIdKhoGanChoContact`.

**C. Autocomplete SP/DV/combo/thẻ DV (POST, lazy):**
- SP cha: `DropdownSanPhamChaAutoComplete` (+ `Private`, + `LimitedChiNhanh` có permission-key `CommonWarehouse_DropdownSanPhamChaLimitedChiNhanhAutoComplete`). SP con: `DropdownSanPhamChildAutoComplete` (+ `Private`, + `LimitedChiNhanh` có key `...ChildLimitedChiNhanhAutoComplete`).
- DV: `DropdownDichVuAutoComplete` (key `CommonWarehouse_DropdownDichVuAutoComplete`). Thẻ DV: `DropdownTheDichVuAutoComplete` (key `CommonWarehouse_DropdownTheDichVuAutoComplete`). Combo: `DropdownComboAutoComplete` (key `CommonWarehouse_DropdownComboAutoComplete`).
- By-keyword: `DropDownDichVuByKeyWord`, `DropDownTheDichVuByKeyWord`, `DropDownComboByKeyWord`, `GetAllDichVuVaNhomDVLazyLoadAutocomplete`.
- SP sản xuất: `DropdownSanPhamSanXuat`. Auto giới hạn chi nhánh: controller TỰ set `payload.IdChiNhanh = CurrentUser.IdChiNhanh` nếu user KHÔNG phải admin (`CommonController.cs:198-201,223-226`).

**D. Dropdown SP/DV "hiển thị" theo nhóm (nhiều BIẾN THỂ + VERSION) — cẩn trọng:**
- `DropdownSpByNspHienThi`, `DropdownDvByNspHienThi`, `DropdownNhomSpHienThi`, `DropdownAllSanPhamChiNhanhMarketing`, `DropdownSpForDsGiaoCa`, `DropdownDichVuByNdv/{idNhomDv}`, `DropdownSanPham`, `DropdownSanPhamFilterSuDung/{filterSuDung}`, `DropdownNhaCungCap`, `DropdownNhaCungCapAndKhachHang`.
- **Biến thể "Pt" / "KM" / "Logistic" + VERSION ngày:** `DropdownSpByNspHienThiPt`, `...PtVersion20251126_1706`, `DropdownSpByNspHienThiLogisticPt`, `...LogisticPtVer20251127`, `DropdownDvByNspHienThiPt`, `DropdownDvByNspHienThiKM`, `DropdownDvByNspHienThi_Private`. → mỗi version phục vụ 1 màn/đợt release; sửa logic phải biết màn nào gọi version nào.

**E. Get-by-Ids / lookup snapshot (FE + cross-service):**
- Public: `GetSanPhamByIds`, `GetSanPhamElasticByIds`, `GetDichVuByIds`, `GetSanPhamChaByIds`, `GetListTenAndIdSanPhamGroupByIdsSP`, `GetListSanPhamByIds` (`[AllowAnonymous]`).
- Private (cross-service, có `idDonVi`): `GetSanPhamByIds_Private`, `GetDichVuByIds_Private`, `GetSanPhamByListMa_Private/{idDonVi}`, `GetSanPhamByIdNhomSanPham/{idNhomSanPham}/{idDonVi}`, `GetDichVuByIdNhomDichVu/{idNhomDichVu}/{idDonVi}`, `GetListIdSanPhamByListIdNhanSanPham/{idDonVi}`, `GetSpDvTheComboByListId` (gộp SP+DV+thẻ+combo theo list id).
- Combo/thẻ DV private (qua `_comboSpaService`/`_theDichVuService`): `GetComboByIds_Private`, `GetTheDichVuByIds_Private`, `GetTatCaCombo/{idDonVi}`, `GetTatCaTheDichVu/{idDonVi}`, `DropDownAllCombo`, `LayTatCaComboChiNhanhPhanTrang_Private`.

**F. Tìm kiếm phân trang cross-service (private):**
- `TimKiemSanPhamPhanTrang`, `TimKiemDichVuPhanTrang`, `TimKiemTheDichVuPhanTrang` (→ `_theDichVuService`), `TimKiemComboPhanTrang` (→ `_comboSpaService`), `GetSanPhamByKhoPagging` — đều check `ModelState.IsValid`, trả lỗi `MESS_Model_UnValidate` nếu sai.

**G. Lazyload SP theo nhóm/kho:**
- `GetAllSanPhamVaNhomSPLazyLoad`, `GetAllSanPhamChaLazyLoad`, `GetAllSanPhamByNhomLazyLoad/{idNhomSP}`, `GetAllSanPhamByKhoLazyLoad/{idKho}` (+ `V01`), `GetSanPhamByKhoPagging`.

**H. "Lấy tất cả ... có thể lấy được" + theo đơn vị/chi nhánh:**
- `LayTatCa_SanPham_CoTheLayDuoc`, `LayTatCa_DichVu_CoTheLayDuoc`, `GetAllDichVuVaNhomDV` (+ `TheoChiNhanh`), `GetAllDichVuDonVi`, `GetAllSanPhamConDonVi`, `GetAllSanPhamChaDonVi` (+ `Private/{idDonVi}`), `GetDichVuDatLichByChiNhanh`, `GetAllSanPhamDinhKem`, `IsAllowSellectAllSanPham`, `CheckPermissionDropdownSanPham`, `CheckSanPhamThuocKho`.

**I. Đơn vị tính / tồn kho / dung tích:**
- `DropdownDonViTinhBySanPham/{idSanPham}`, `LayDungTichListSanPham` (+ `_WithNoAuthen` anonymous, + `Private`), `LayDonViTinhListSanPhamPrivate`, `LayDonViTinhListIdDonViTinhPrivate`, `GetTonKhoByListSanPham`, `GetTonKhoVaDonViTinhByListSanPham`, `SearchSanPhamConBaoCaoTonKhoDonVi`, `SearchSanPhamAutocomplete`.

**J. Anonymous (không token) — lưu ý bảo mật:**
- `DropdownDmSanPham_WithNoAuthen`, `LayDungTichListSanPham_WithNoAuthen`, `GetListSanPhamByIds`, `KhoiTaoDuLieuMacDinh`, `LayDichVuTheoChiNhanh`, `LayDichVuTheoDonVi` — gọi được KHÔNG cần đăng nhập (phục vụ website/landing/đăng ký). 6 endpoint, có cả endpoint GHI data (`KhoiTaoDuLieuMacDinh`).

## Quy tắc nghiệp vụ (gating)
- **Controller-level `[TienIch]`:** `MaTienIchSCRM.TI_Chung` — gate tiện ích "chung" (gần như mọi đơn vị có). Lưu ý `[SBAuthorize()]` controller-level đang COMMENT (`CommonController.cs:16`), auth gắn per-method.
- **Auto-giới hạn chi nhánh:** với autocomplete SP, nếu `!CheckUserLoginIsRoleAdmin(CurrentUser)` → controller ép `payload.IdChiNhanh = CurrentUser.IdChiNhanh` (chặn user thường xem SP ngoài chi nhánh). Bản `LimitedChiNhanh` ÉP cứng IdChiNhanh không cần check admin.
- **Permission-key:** chỉ một số autocomplete có key cụ thể (`CommonWarehouse_Dropdown...AutoComplete`); đa số endbase `[SBAuthorize()]` rỗng (chỉ cần login). Private endpoint = `[PrivateSBAuthorize]` (service-to-service token).

## Quan hệ
- **Gọi sang:** `ICommonService` (đa số), `IKhoChiTietService` (WebsiteCheckInventory*), `ITheDichVuService` (thẻ DV phân trang/by-ids), `IComBoSpaService` (combo phân trang/by-ids). Đều trong service WarehouseProduct (không gọi external gateway).
- **Dữ liệu (entities, chỉ ĐỌC):** `SanPham`/`SanPhamCha`-`Con`, `DichVu`, `ComBoSpa`, `TheDichVu`, `NhomSanPham`, `NhomDichVu`, `Kho`/`KhoChiTiet`, `NhaCungCap`, `DonViTinh`. Hầu hết read-only (dropdown/lookup); ngoại lệ ghi: `KhoiTaoDuLieuMacDinh` (seed), `XoaDuLieuTheoDonVi` (xóa), `GetAndSetCauHinhKhoCache` (ghi cache).
- **Ripple / đồng bộ:** là NGUỒN dropdown/lookup cho hầu hết module → đổi cấu trúc trả về (field/null) của 1 endpoint có thể vỡ nhiều màn bán hàng/sale/marketing cùng lúc. Đây là controller "rủi ro lan rộng" khi refactor.

## Lỗi đã biết / lưu ý kỹ thuật
- **Bùng nổ biến thể + VERSION trong tên endpoint:** `DropdownSpByNspHienThiPtVersion20251126_1706` (`CommonController.cs:569`), `DropdownSpByNspHienThiLogisticPtVer20251127` (`:553`) — đặt version vào tên path thay vì versioning chuẩn → khó dọn, dễ để lại endpoint chết. Debug "dropdown sai" phải xác định đúng version màn dùng.
- **Logic nằm ở controller (anti-pattern):** `TimKiemSanPhamAI` hardcode `PageSize=100`/`IsThuNgan=true`/`IdNhomSp=Guid.Empty` và build `UserLogin` ngay trong controller (`CommonController.cs:1019-1035`) — nên đẩy xuống service.
- **6 endpoint `[AllowAnonymous]`** (mục J) gọi được không token, gồm cả `KhoiTaoDuLieuMacDinh` GHI data — rủi ro nếu input không kiểm soát; cần xác nhận chỉ dùng cho luồng website/đăng ký.
- **Endpoint comment-out còn trong file:** `GetAllSanPhamVaNhomSPLazyLoadV01` bị comment (`CommonController.cs:614-619`) nhưng method service vẫn được gọi bởi `GetAllSanPhamByKhoLazyLoadV01` (`:644-649`) — V01 còn sống một phần.
- **Nhiều cặp V1/V2** (`WebsiteCheckInventory`/`V2`, `DropdownDsKhoTheoChiNhanh`/`V2`, `GetLstKhoDonHangSaleCheckKhoTong`/`_V2`) — sửa 1 bản nhớ đối chiếu bản kia.
- **Controller phụ thuộc 4 service** (`_commonService`, `_khoChiTietService`, `_theDichVuService`, `_comboSpaService`) — không thuần "common", có endpoint đẩy thẳng sang service nghiệp vụ khác (thẻ DV, combo); khi map quyền/trace nhớ service đích thật.
- **Permission-key không nhất quán:** đa số `[SBAuthorize()]` rỗng, vài cái có key `CommonWarehouse_*`, có cái còn comment sẵn key trong code (`CommonController.cs:194,219`) → khó audit quyền toàn controller.
