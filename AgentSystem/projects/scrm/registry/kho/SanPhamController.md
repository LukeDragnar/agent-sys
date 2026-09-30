---
controller: SanPhamController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Quản lý sản phẩm (quanly-sanpham.component) — danh sách SP, nút Thêm mới + sửa/xóa per-row, Import/Update Excel, Sửa giá hàng loạt; nhiều endpoint còn dùng nội bộ làm nguồn dữ liệu SP cho POS/Sale/Ecommerce/Website"
muc_dich: "CRUD + tìm kiếm sản phẩm (SP cha/con, theo chi nhánh tạo), import/update SP bằng Excel, sửa giá/sử-dụng-giao-ca hàng loạt, tra cứu IMEI và giá vốn; là nguồn dữ liệu SP cho hầu hết module khác."
alias: ["sản phẩm", "quản lý sản phẩm", "danh mục sản phẩm", "import sản phẩm", "sửa giá hàng loạt", "lấy sản phẩm theo id", "tìm sp theo barcode", "sp chi nhánh tạo"]
auth: mixed
state_machine: []
deprecated: false
entities: [SanPham, SanPhamCha, SanPhamChiNhanh, SanPhamChaChiNhanh, SanPhamImei, SanPhamUpdateImport, KhoChiTiet]
stored_procs: [GetSanPhamPagingStore, GetSanPham_DonVi_PagingStore]
depends: [WarehouseProduct, GiaVon (giá vốn), Ecommerce/ProductConnect, Promotion (SP tặng)]
cross_module_in: [POS/ThuNgan (TimTheoIdsThuNgan*, LaySpTheoIdsKemSoLuongTon), Website (LayTatCa_Private, TimSanPhamTheoId_Private, LayTatCaSpCha_Private), Ecommerce (GetProductBySku/ById, *NoAuthen, GetListSanPhamByIdsWithNoAuthen), Promotion (GetListSanPhamTangByIds*), PreOrder (GetForPreOrder), Accounting/Order (GetListSanPhamByIds, GetDonViTinhByIds)]
cross_module_out: [KhoChiTiet (sửa giá/SP), GiaVon (đọc giá vốn), SanPhamImei]
---

# SanPham — Quản lý sản phẩm

> Controller lớn (~50 endpoint, `partial class`). Bảng "Hành động" chỉ giữ thao tác có UI/ý nghĩa nghiệp vụ; phần service-to-service gom ở "Endpoint nội bộ". Hai class trùng tên ở service khác — xem mục Lỗi đã biết.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm SP theo điều kiện (grid chính) | "Tìm kiếm" | POST `api/SanPham/TimTheoDieuKien` | `SanPham_TimTheoDieuKien` | public | màn Quản lý sản phẩm | List SP theo filter (kho/nhóm/chi nhánh/keyword) | EF | role DV-NV bị bỏ chủ ý (comment "Bỏ DV-NV theo yêu cầu") |
| 1b | Tìm theo điều kiện SP cha | (tab/list SP cha) | POST `api/SanPham/TimTheoDieuKienSPCha` | `SanPham_TimTheoDieuKienSPCha` | public | - | List SP cha theo filter | EF | - |
| 1c | List SP theo chi nhánh tạo | màn "Quản lý SP chi nhánh tạo" | POST `api/SanPham/TimTheoDieuKienSpChiNhanhAsync` | `SanPham_TimTheoDieuKienSpChiNhanhAsync` | public | - | List SP do chi nhánh tự tạo | EF | - |
| 2 | Thêm mới SP | "Thêm mới" → edit-sanpham | POST `api/SanPham/ThemMoi` | `SanPham_ThemMoi` | public | chỉ DV-AD-TONG | Tạo SP. **Ép `IdChiNhanhTao = null`** (SP cấp tổng, không gán chi nhánh) | EF | quyền chỉ Admin Tổng |
| 2b | Thêm mới SP chi nhánh tạo | "Thêm mới" (màn chi nhánh tạo) | POST `api/SanPham/ThemMoiSanPhamChiNhanh` | `SanPham_ThemMoiSanPhamChiNhanh` | public | admin chọn nhiều CN | Gán `IdChiNhanhTao = CurrentUser.IdChiNhanh`; admin giữ `ListIdChiNhanh` từ FE (luôn ép thêm CN hiện tại), non-admin lock về 1 CN | EF | logic admin/non-admin phân nhánh — dễ sai chi nhánh |
| 3 | Xem / sửa SP | "Sửa" → edit-sanpham | GET `api/SanPham/TimTheoId/{id}` + POST `api/SanPham/SanPhamInit` | `SanPham_TimTheoId` / `SanPham_Init` | public | - | Lấy full SP để edit; `SanPhamInit` trả data form (kèm isDisableThuocTinh) | EF | - |
| 4 | Cập nhật SP | "Lưu" (edit-sanpham) | POST `api/SanPham/CapNhatThongTin` | `SanPham_CapNhatThongTin` | public | chỉ DV-AD-TONG | Cập nhật SP cấp tổng | EF | - |
| 4b | Cập nhật SP chi nhánh tạo | "Lưu" (màn chi nhánh tạo) | POST `api/SanPham/CapNhatThongTinSpChiNhanh` | `SanPham_CapNhatThongTinSpChiNhanh` | public | admin truyền ListIdChiNhanh | Admin: truyền `idChiNhanhTao = null` để service xử lý add/xóa CN trực thuộc; non-admin: pass CN hiện tại → service skip update ListIdChiNhanh | EF | **gotcha**: truyền IdChiNhanhTao ≠ null khiến service skip update ListIdChiNhanh (CapNhatThongTin@SanPhamService.cs:2155-2159) |
| 5 | Xóa SP | "Xóa" (icon delete) | POST `api/SanPham/Xoa/{id}` | `SanPham_Xoa` | public | chỉ DV-AD-TONG | Xóa SP cấp tổng | EF | - |
| 5b | Xóa SP chi nhánh tạo | "Xóa" (màn chi nhánh tạo) | POST `api/SanPham/XoaTheoIdSpChiNhanh/{id}` | `SanPham_XoaTheoIdSpChiNhanh` | public | - | Xóa SP do chi nhánh tạo | EF | - |
| 6 | Kiểm tra tồn/đơn trước khi đổi SP | (trước thao tác nhạy cảm) | POST `api/SanPham/KiemTraCoTonKhoHoacDonHang/{idSanPhamCha}` | `[SBAuthorize]` (chỉ đăng nhập) | public | - | Trả true nếu SP cha còn tồn kho hoặc đang trong đơn hàng → chặn xóa/sửa cấu hình | EF | permission key bị comment, chỉ còn `[SBAuthorize]` rỗng (controller:714) |
| 7 | Sửa giá hàng loạt | "Sửa giá hàng loạt" → sua-hang-loat | POST `api/SanPham/SuaGiaHangLoat` | `SanPham_SuaGiaHangLoat` | public | chỉ DV-AD-TONG | Cập nhật giá nhiều SP cùng lúc | EF | - |
| 7b | Sửa "sử dụng giao ca" hàng loạt | sua-su-dung-giao-ca-hang-loat | POST `api/SanPham/SuaSuDungGiaoCaHangLoat` | `SanPham_SuaSuDungGiaoCaHangLoat` | public | chỉ DV-AD-TONG | Bật/tắt cờ giao ca cho nhiều SP | EF | label nhãn quyền sai-lệch ("Sửa giá khi thanh toán hàng loạt") |
| 7c | Sửa SP+IMEI hàng loạt | sua-hang-loat-sp-imei | POST `api/SanPham/SuaSpImeiHangLoat` | `SanPham_SuaSpImeiHangLoat` | public | chỉ DV-AD-TONG | Cập nhật SP có IMEI hàng loạt | EF | nhãn quyền copy-paste sai |
| 8 | Tải template import SP | "Import" → download mẫu | POST `api/SanPham/DownloadFileTemplateImportSanPham` | `SanPham_DownloadFileTemplateImportSanPham` | public | - | File `ImportSanPham.xlsm` (giữ macro VBA multi-select; MIME macroEnabled) | - | template .xlsm, GemBox preserve macro qua XlsxDefault |
| 9 | Import SP | "Import" → upload | POST `api/SanPham/ImportSanPham` | `SanPham_ImportSanPham` | public | - | Import danh sách SP từ Excel | - | nhãn quyền sai ("Import danh sách khách hàng") |
| 9b | Import SP chi nhánh tạo | màn chi nhánh tạo → import | POST `api/SanPham/ImportSanPhamChinhanh` | `SanPham_ImportSanPham` | public | bắt buộc chọn ≥1 CN | Parse `ListIdChiNhanh` ("guid1,guid2,..") từ FE; rỗng → trả lỗi "Hãy chọn ít nhất 1 chi nhánh!" | - | dùng chung key `SanPham_ImportSanPham` với #9 |
| 9c | Tải file SP import lỗi | (sau import) | GET `api/SanPham/DownloadDataWrongSanPham/{id}` | `SanPham_DownloadDataWrongSanPham` | public | có bản ghi lỗi | File `DataImportSanPhamFail.xlsx` các dòng lỗi | - | - |
| 10 | Update SP theo file (tải mẫu) | màn import-sanpham-update | GET `api/SanPham/DownloadFileUpdateSanPham` | `SanPham_DownloadFileUpdateSanPham` | public | - | File `ImportSanPhamUpdate.xlsm` | - | - |
| 10b | Update SP — upload file | màn import-sanpham-update | POST `api/SanPham/UploadFileUpdateSanPham` | `SanPham_UploadFileUpdateSanPham` | public | - | Đẩy file update vào bảng tmp (`SanPhamUpdateImport`) | EF | - |
| 10c | Update SP — list / xóa / hoàn tất | màn import-sanpham-update | POST `GetListFileUpdateSanPham`, `DeleteFileUpdateSanPham`, `HoanTatImportUpdateSanPham` | `SanPham_GetListFileUpdateSanPham` / `_DeleteFileUpdateSanPham` / `_HoanTatImportUpdateSanPham` | public | theo status | List/xóa bản ghi tmp; "Hoàn tất" áp update thật (mặc định 100 SP/lần) | EF | - |
| 11 | Tìm SP theo barcode | quét barcode (POS/kho) | GET `api/SanPham/TimSanPhamTheoBarcode/{barcode}` | `SanPham_TimSanPhamTheoBarcode` | public | - | Trả SP khớp barcode | EF | - |
| 12 | Lấy giá vốn SP theo kho | (màn cần giá vốn) | GET `api/SanPham/LayThongTinGiaSanPham` + POST `.../LayThongTinGiaSanPhamBatch` | `[SBAuthorize()]` (chỉ đăng nhập) | public | - | Trả giá vốn SP theo (idSanPham, idKho) — đi qua `IGiaVonService` | EF | nguồn = GiaVonService, không phải SanPhamService |
| 13 | Lấy danh sách IMEI của phiếu | (màn cần IMEI) | GET `api/SanPham/GetDanhSachImeiPhieu/{id}` | `[SBAuthorize()]` (chỉ đăng nhập) | public | - | List IMEI theo phiếu + trạng thái SP + kho | EF | permission key bị comment (controller:158) |

## Endpoint nội bộ (private / service-to-service)
Gom theo caller → mục đích (không có nút UI):

- **[private] POS / Thu ngân** (`[PrivateSBAuthorize]`): `PrivateGetSanPhamByIds`, `TimTheoIdsThuNgan`, `TimTheoIdsThuNganByChiNhanh` → lấy SP + tồn cho màn thu ngân; `LaySpTheoIdsKemSoLuongTon` (`[SBAuthorize()]`), `GetListSanPhamAndNgayHetHangByIds`.
- **[private] Website** (`[PrivateSBAuthorize]`): `LayTatCa_Private`, `TimSanPhamTheoId_Private`, `TimSanPhamTheoIds_Private`, `LayTatCaSpCha_Private`, `TimSanPhamChaTheoId_Private`, `TimSanPhamChaTheoSlug_Private`, `GetSanPhamByIds_Private`, `GetDonViTinhByIds` → đồng bộ catalog SP/SP cha cho website. (`LayTatCa` bản `[SBAuthorize]` cho FE đăng nhập.)
- **[anonymous] Ecommerce/Sàn** (`[AllowAnonymous]`): `GetProductBySku`, `GetProductById`, `SanPhamInitNoAuthen`, `GetListSanPhamByIdsWithNoAuthen`, `GetAllSanPhamNoAuth/{idDonVi}` → sàn tạo/map SP. **Cảnh báo:** `AllowAnonymous` mở public, không token.
- **[private] Promotion/Khuyến mãi**: `GetListSanPhamTangByIds` (`[SBAuthorize]`) + `GetListSanPhamTangByIdsPrivate` (`[PrivateSBAuthorize]`, dùng `req.IdDonVi`) → lấy SP tặng cho KM.
- **[private] PreOrder.API**: `GetForPreOrder?idDonVi&idChiNhanh` → list SP `IsShowPreOrder=true`.
- **Phục vụ FE chung** (`[SBAuthorize]`/rỗng): `LayTheoNhomSanPham`, `GetSanPhamByNhomSP`, `TimSanPhamTheoId`, `GetListSanPhamByIds`, `TimTheoIds`, `TimTheoIdBasic`, `GetAllSanPhamBasic`, `GetSanPhamChaEcom`, `DropdownHaCapTheoSp`, `UpdateSanPhamNhaCungCap`, `GetSanPhamPaging` (SP `GetSanPhamPagingStore`), `GetSanPham_DonVi_Paging` (SP `GetSanPham_DonVi_PagingStore`).

## Quan hệ
- **Gọi sang:** `IGiaVonService` (giá vốn theo kho — #12), `ISanPhamEcommerceService` (luồng Ecommerce/Website không-auth), `ISanPhamService` (phần lớn nghiệp vụ).
- **Dữ liệu (entities):** `SanPham`, `SanPhamCha`, `SanPhamChiNhanh`, `SanPhamChaChiNhanh`, `SanPhamImei`, `SanPhamUpdateImport` (tmp update), `KhoChiTiet` (tồn + giá).
- **Ripple / đồng bộ:** sửa/xóa SP hoặc đổi cấu hình ảnh hưởng tới phiếu nhập/xuất, POS, KM, website, ecommerce catalog → trước thao tác nhạy cảm phải qua `KiemTraCoTonKhoHoacDonHang` (#6). Là **nguồn dữ liệu SP cho hầu hết module** (nhiều endpoint private/anonymous).

## Lỗi đã biết / lưu ý kỹ thuật
- **`public partial class SanPhamController`** — node này cover file `Controllers/SanPhamController.cs`. Glob không thấy file partial thứ 2 trong `Controllers/`; nếu sau này có thêm partial cần bổ sung node. *Cần xác nhận* không có file partial khác.
- **Hai class TRÙNG TÊN khác scope** (KHÔNG phải partial của file này, đừng nhầm):
  - `Areas/SandBoxArea/SanPhamController.cs` — route `api/SanboxArea/SanPham`, tiện ích `MaTienIchSCRM.TI_HeThong`, quyền `HT-AD`; các endpoint `*_SA` (TimTheoDieuKien_SA, ThemMoi_SA, ...) cho **admin hệ thống** (màn `quanly-sanpham-admin`). Nên có node riêng nếu cần.
  - `SanPhamChiNhanhController.cs` — controller khác (`api/SanPhamChiNhanh`), FE `LayTheoDanhMucSanPham`/`LayTheoDanhMucSanPham` gọi sang. Không thuộc node này.
- **Nhiều `[AllowAnonymous]` mở public** (GetProductBySku/ById, SanPhamInitNoAuthen, GetListSanPhamByIdsWithNoAuthen, GetAllSanPhamNoAuth) → trả dữ liệu SP không cần token. Cần xác nhận có chủ ý (data exposure).
- **Quyền `[SBAuthorize()]` rỗng / key bị comment** ở vài endpoint nhạy cảm: `GetDanhSachImeiPhieu` (controller:158), `KiemTraCoTonKhoHoacDonHang` (controller:714), `LayThongTinGiaSanPham*`, `LaySpTheoIdsKemSoLuongTon`, `TimTheoIds`, `LayTatCa`, `SanPhamChiNhanhInit` → chỉ cần đăng nhập là gọi được.
- **Nhãn permission copy-paste sai** (mô tả không khớp hành động): `SanPham_ImportSanPham` ghi "Import danh sách khách hàng"; `SuaSuDungGiaoCaHangLoat`/`SuaSpImeiHangLoat` ghi "Sửa giá khi thanh toán hàng loạt"; `SanPham_Init` ghi "...chỉnh sửaC". Chỉ là label, không ảnh hưởng logic — nhưng gây nhầm khi map quyền.
- **`SuaGiaHangLoat` & `SuaSuDungGiaoCaHangLoat` cùng nhãn** "Sửa giá khi thanh toán hàng loạt" dù khác nghiệp vụ — phân biệt bằng method/route.
- **Endpoint paging dùng Stored Proc** (`GetSanPhamPagingStore`, `GetSanPham_DonVi_PagingStore`) — bản EF cũ (`GetSanPhamPaging`, `GetSanPham_DonVi_Paging`) đã comment, không chạy. Triage chậm paging → soi 2 SP này.
- **`CapNhatThongTinSpChiNhanh` gotcha chi nhánh**: chỉ admin được truyền `idChiNhanhTao = null` để service add/xóa CN trực thuộc; truyền IdChiNhanhTao ≠ null khiến service **skip update ListIdChiNhanh** (SanPhamService.cs:2155-2159). Sửa add/xóa chi nhánh không ăn → kiểm tra nhánh này.
