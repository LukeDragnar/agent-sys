---
controller: KhoController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý kho > Danh mục kho (route WarehouseProduct/dm-kho) — lưới danh sách kho theo chi nhánh + form Thêm/Sửa kho (dm-kho-edit). Phần lớn endpoint còn lại là dropdown/internal phục vụ chọn kho ở các màn khác."
muc_dich: "Quản lý danh mục KHO (đơn vị lưu hàng) trong 1 chi nhánh: thêm/sửa/xóa kho, đặt kho mặc định (IsMacDinh), cấp dropdown chọn kho cho các module khác, và phục vụ kho mặc định / thông tin kho cho service-to-service (Accounting, Ecommerce)."
alias: [kho, danh mục kho, danh sách kho, quản lý kho, kho mặc định, warehouse, dm kho, Kho]
auth: mixed
state_machine: []
deprecated: false
entities: [Kho, KhoChiTiet, DichVuChiNhanh, SanPhamChaChiNhanh, ComBoSpa, TheDichVuDoiTuongApDung]
stored_procs: [PRC_SCRM_WarehouseProduct_Ds_Kho]
depends: [CauHinhService, CommonService, AccountingService, ElasticSearchService, GiaVonService, ServiceCacheCommon (Redis Kho_DonVi), CoreService (DonVi/ChiNhanh)]
cross_module_in: [Accounting (GetInfoKhoKeToanByListId, GetByIdsPrivate, GetByIdsNoAuthen, GetKhoByIdPrivate, đồng bộ MISA), Ecommerce (TimTheoIdWithNoAuthen), mọi module cần dropdown chọn kho (Phiếu nhập/xuất, dịch vụ, bán hàng)]
cross_module_out: [Redis cache Kho_DonVi + CauHinhKho_ChiNhanh (clear sau ghi), ElasticSearch (re-index KhoChiTiet khi xóa kho ở chế độ kho tổng)]
---

# KhoController — Quản lý kho (danh mục kho & kho mặc định)

> Controller CRUD danh mục kho + nhiều endpoint dropdown/internal lấy kho theo user/chi nhánh/đơn vị. Logic nghiệp vụ chính: **chỉ 1 kho mặc định (IsMacDinh) trong 1 chi nhánh** và **rule xóa kho phụ thuộc cấu hình kho tổng**. Dữ liệu kho được cache Redis theo đơn vị (`Kho_DonVi`); mọi thao tác ghi đều clear cache.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kho theo điều kiện (lưới) | Lưới "Danh mục kho" / Tìm kiếm | `POST api/Kho/TimTheoDieuKien` | `Kho_TimTheoDieuKien` | public | màn dm-kho | Lấy DS chi nhánh user được phép (`GetLstChiNhanhByUnit`). Có `IdChiNhanh` → phải nằm trong DS được phép, không thì lỗi "Không có quyền xem...". Không truyền chi nhánh + **không phải AdminTổng** → ép `IdChiNhanh = user.IdChiNhanh`. Gọi store với @IdDonVi,@LstStrIdChiNhanh(join ";"),@KeyWord,@PageIndex,@PageSize; gán tên chi nhánh sau. | SP `PRC_SCRM_WarehouseProduct_Ds_Kho` | `totalRecord` lấy từ `data[0].TotalRecord` (mỗi row mang totalRecord) — rỗng → 0. Log store chỉ khi SuperUser/CSKH. |
| 2 | Thêm mới kho | Nút "Thêm" (form dm-kho-edit) | `POST api/Kho/ThemMoi` | `Kho_ThemMoi` (AD-TONG/AD-CN/AD-MCN) | public | — | ModelState valid → `ValidateRequest` (Tên bắt buộc, Chi nhánh bắt buộc, SuDungKho bắt buộc; MaKho duy nhất/chi nhánh; nếu IsUseShipping bắt buộc địa chỉ; check MKT mặc định không trùng kho khác). Set IdDonVi/IdNganhNghe theo user. Insert. **Nếu IsMacDinh=true → set tất cả kho khác cùng đơn vị+chi nhánh = false** (đảm bảo 1 kho mặc định). Clear cache kho chi nhánh + đơn vị. | EF (`_repos.InsertAsync`) | `IsTuSinhMaKho=true` + MaKho rỗng + đơn vị dùng nghiệp vụ kế toán → tự sinh MaKho (`SinhMaPhieu`). SĐT trim + validate regex. |
| 3 | Cập nhật thông tin kho | Nút "Lưu" (form dm-kho-edit) | `POST api/Kho/CapNhatThongTin` | `Kho_CapNhatThongTin` (AD-TONG/AD-CN/AD-MCN) | public | — | ModelState valid → `ValidateRequest` → MarkDirty các field (Ten, địa chỉ, IsMacDinh, IsUseShipping, MaKho, UserQuanKho, IdsMkt/IdsMktMacDinh/IdsSale...) → UpdateAsync. **IsMacDinh=true → set kho khác cùng đơn vị+chi nhánh = false**. Clear cache. | EF (`base.UpdateAsync`) | Rule cũ "bắt buộc 1 kho mặc định" (chặn gỡ IsMacDinh kho mặc định) ĐÃ COMMENT OUT (L195-198) → hiện không chặn việc bỏ cờ mặc định. |
| 4 | Xóa kho | Nút "Xóa" (lưới) | `POST api/Kho/Xoa/{id}` | `Kho_Xoa` (AD-TONG/AD-CN/AD-MCN) | public | — | Tìm kho theo Id + DonViId. Đọc cấu hình `IsSuDungKhoTong`: **Bật kho tổng** → xóa kho + xóa hết KhoChiTiet của kho + re-index ES (theo batch 50). **Tắt kho tổng** → chặn nếu kho là mặc định ("Không thể xóa kho mặc định"), chặn nếu kho đã có KhoChiTiet đang dùng ("Không thể xóa kho đã được sử dụng"). Clear cache. | EF | 2 nhánh hành vi khác hẳn theo cấu hình kho tổng — đừng nhầm. |
| 5 | Lấy kho theo Id | (mở form sửa) | `GET api/Kho/TimTheoId/{id}` | `SBAuthorize` (any) | public | — | Trả KhoDto đầy đủ (tách IdsMkt/IdsMktMacDinh/IdsSale từ chuỗi ";" thành list Guid). | EF | — |
| 6 | Copy SP/DV/combo/thẻ chi nhánh gốc → chi nhánh khác | "Copy dịch vụ sang chi nhánh khác" | `POST api/Kho/CopyDichVuFromChiNhanhRootToChiNhanhDiff` | `Kho_CopyDichVuFromChiNhanhRootToChiNhanhDiff` (chỉ AD-TONG) | public | chỉ AdminTổng | Validate: phải AdminTổng; bắt buộc ChiNhanhGoc; bắt buộc ChiNhanhCopy; nếu copy Combo thì **bắt buộc** copy cả SP + DV. Copy DichVuChiNhanh / SanPhamChaChiNhanh / Combo / TheDichVu từ chi nhánh gốc sang các chi nhánh copy, **bỏ qua bản ghi đã tồn tại** ở chi nhánh đích (idempotent). | EF (nhiều bảng) | Tên controller là "Kho" nhưng việc thực chất là copy danh mục chi nhánh — caller dễ nhầm. Loại ChiNhanhGoc khỏi danh sách copy. |
| 7 | Dropdown kho theo quản kho | (dropdown chọn kho) | `GET api/Kho/GetKhoTheoQuanKho_Dropdown` | `SBAuthorize` (any) | public | — | Admin/CSKH/AdminCN/QL-CG/KiemTrung → **full kho chi nhánh hiện tại**. User thường → theo cấu hình `BoPhanKhoVanChiNhinThayKhoMinhQuanLy`: bật → chỉ kho mình quản lý (IdUserQuanLys chứa user); tắt → full kho chi nhánh. Đọc từ cache Kho_DonVi. | EF/Redis | — |
| 8 | Dropdown kho theo chi nhánh (report) | (dropdown báo cáo) | `POST api/Kho/GetKhoTheoChiNhanh_Dropdown` | `SBAuthorize` (any) | public | — | Non-(CSKH/Super/AdminTổng) bị ép `idChiNhanh = user.IdChiNhanh`. Lọc kho theo chi nhánh, trả {Id,Ten}. | EF/Redis | Ép chi nhánh = invariant phân quyền. |
| 9 | Dropdown kho theo quản kho (toàn đơn vị) | (dropdown) | `GET api/Kho/GetDanhMucKhoTheoQuanKhoByDonVi_Dropdown` | `SBAuthorize` (any) | public | — | Như #7 nhưng phạm vi theo đơn vị (cross chi nhánh). | EF/Redis | — |
| 10 | DS kho toàn đơn vị theo user login | (chọn kho) | `GET api/Kho/GetAllKhoByUserLogin` · `GET GetAllKhoByChiNhanh/{idCN}` · `GET GetAllKhoDonViByUserLogin` | `Kho_GetAllKhoByUserLogin` (#1) / `SBAuthorize` | public | — | Trả `ResGetAllKhoByUserLogin` (gồm `IsKhoMacDinh = IsMacDinh ?? false`). Biến thể: theo user / theo 1 chi nhánh / theo đơn vị. | EF/Redis | — |
| 11 | DS user quản kho theo kho | (popup phân quyền kho) | `POST api/Kho/GetListUserQuanKhoByKhoId` · `POST CheckPermissionGetUserQuanKhoByKhoId` | `Kho_GetListUserQuanKhoByKhoId` / `Kho_CheckPermissionGetUserQuanKhoByKhoId` | public | — | CSKH/AdminTổng/Super + `IsViewReport` → dùng `payload.IdChiNhanh`; còn lại dùng IdChiNhanh user. Lọc kho theo chi nhánh + IdKho(s), gom `IdUserQuanLys`, trả `UserRedis` của các user đó. Không có kho → lỗi "Chi nhánh chưa tạo kho". | Redis (cache kho + user) | — |
| 12 | DS quản kho theo list IdKho | (internal/popup) | `POST api/Kho/GetLstQuanKho` · `POST GetLstIdKhoDangQuanLy` | `SBAuthorize` (any) | public | — | `GetLstQuanKho`: quản kho theo list kho. `GetLstIdKhoDangQuanLy`: list IdKho user đang quản lý (non admin/CSKH → lọc theo UserQuanKho chứa user). | EF | — |
| 13 | Kho tổng | (cấu hình kho tổng) | `POST api/Kho/CheckKhoTong` · `GET LayAllKhoTong` | `SBAuthorize` (any) | public | chỉ khi bật kho tổng | `IsSuDungKhoTong=true` → `CheckKhoTong` trả true nếu IdKho thuộc chi nhánh tổng (`IdChiNhanhTong`); `LayAllKhoTong` trả DS kho của chi nhánh tổng. Tắt kho tổng → false / rỗng. | EF | — |
| 14 | Lấy kho cơ bản theo list Id | (hiển thị tên kho) | `POST api/Kho/GetKhoBasicInfoByIds` | `SBAuthorize` (any) | public | — | Trả thông tin kho cơ bản theo list IdKho. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize] GET TimTheoIdPrivate/{id}` · `GET GetKhoByIdPrivate/{id}` — lấy kho theo Id, gọi nội bộ (Accounting/khác).
- `[PrivateSBAuthorize] POST GetKhoMacDinhPrivate` (`GetKhoMacDinhPrivateAsync`) — trả **kho mặc định** của (IdDonVi + IdChiNhanh) (`IsMacDinh==true`, FirstOrDefault). Dùng khi service khác cần kho mặc định để gán phiếu.
- `[PrivateSBAuthorize] GET GetAllKhoByDonViPrivate/{idDonVi}` — DS kho 1 đơn vị (đọc cache `Kho_DonVi`).
- `[PrivateSBAuthorize] POST GetByIdsPrivate` — DS kho theo list Id (caller bắt buộc truyền IdDonVi), có userLogin. **Dành cho Accounting.**
- `[AllowAnonymous] POST GetByIdsNoAuthen` — DS kho theo list Id KHÔNG cần userLogin → dùng cho luồng đồng bộ MISA từ Accounting (không có user).
- `[PrivateSBAuthorize] POST GetInfoKhoKeToanByListId` — qua `_giaVonService`, trả thông tin kho kế toán theo list IdKho (cho Accounting).
- `[AllowAnonymous] GET TimTheoIdWithNoAuthen/{id}` (#region Ecommerce) — trả `KhoDanhSachDto` không cần auth (cho Ecommerce).

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
1. **1 kho mặc định / chi nhánh:** Khi ThemMoi (#2) hoặc CapNhat (#3) với `IsMacDinh=true` → set tất cả kho khác cùng `IdDonVi + IdChiNhanh` về `IsMacDinh=false`. Điều kiện: cùng đơn vị + cùng chi nhánh, loại trừ chính kho đang lưu (`b.Id != model.Id`).
2. **Xóa kho theo cấu hình kho tổng (#4):**
   - `IsSuDungKhoTong=true` → cho xóa kể cả kho mặc định + xóa luôn KhoChiTiet + re-index ES.
   - `IsSuDungKhoTong=false` → chặn nếu `IsMacDinh==true` ("Không thể xóa kho mặc định") HOẶC kho còn KhoChiTiet đang dùng ("Không thể xóa kho đã được sử dụng").
3. **MaKho duy nhất trong chi nhánh** (ValidateRequest): MaKho trùng (cùng DonVi+ChiNhanh, IsDelete=false, loại trừ chính nó) → "Mã kho đã tồn tại trong chi nhánh!". MaKho rỗng được phép trùng, **trừ khi** đơn vị dùng nghiệp vụ kế toán (`GetIsSuDungPhieuXuatNghiepVuKeToan`) → bắt buộc MaKho.
4. **MKT mặc định không trùng kho khác** (ValidateRequest): `IdsMktMacDinhDto` đã gán kho khác cùng đơn vị+chi nhánh → "Marketing mặc định đã được gán với 1 kho khác!".
5. **Copy combo (#6) phải kèm SP + DV:** `IsCheckCombo=true` mà `IsCheckDichVu=false || IsCheckSanPham=false` → chặn.

## Quan hệ
- **Gọi sang:** `CauHinhService` (`_service.GetAsync` lấy cấu hình kho tổng / `IsSuDungPhieuXuatNghiepVuKeToan` qua AccountingService), `CommonService` (`SinhMaPhieu`, lấy user theo chi nhánh/đơn vị từ cache), `GiaVonService` (info kho kế toán), `ElasticSearchService` (re-index KhoChiTiet khi xóa), CoreService (`GetLstChiNhanhByUnit`, GetLinhVucByUser).
- **Dữ liệu (entities):** ghi/đọc `Kho` (Ten, MaKho, IsMacDinh, IdChiNhanh/IdDonVi/IdNganhNghe, IdsMkt/IdsMktMacDinh/IdsSale là chuỗi join ";", UserQuanKho, SuDungKho, IsUseShipping + địa chỉ, IsAnKhoDoiVoiSale, IsChoPhepChotAm...). Xóa kho (kho tổng) → xóa `KhoChiTiet`. Copy (#6) ghi `DichVuChiNhanh`, `SanPhamChaChiNhanh`, `ComBoSpa(+DichVu/SanPham/ChiNhanh)`, `TheDichVuDoiTuongApDung`.
- **Ripple / đồng bộ:** sau mọi ghi (#2/#3/#4) → `ClearCache_CauHinhKho_ChiNhanh(IdChiNhanh)` + `ClearCache_Kho_DonVi(DonViId)` (cache Redis dùng chung cho mọi dropdown kho). Xóa kho ở chế độ kho tổng → re-index ElasticSearch KhoChiTiet theo batch 50.

## Luồng xuyên module
- **Đặt kho mặc định** (#2/#3, IsMacDinh): ghi 1 kho mặc định → các service khác (Accounting, tạo phiếu) lấy kho mặc định qua `GetKhoMacDinhPrivate` để gán mặc định cho chứng từ.
- **Service-to-service đọc kho:** Accounting đồng bộ chứng từ/MISA gọi `GetByIdsPrivate` (có user) hoặc `GetByIdsNoAuthen` (không user) + `GetInfoKhoKeToanByListId`; Ecommerce gọi `TimTheoIdWithNoAuthen`. → KhoController là **nguồn dữ liệu kho** cho cross-service.
- **Xóa kho (chế độ kho tổng)** → kéo theo xóa KhoChiTiet + re-index ES → ảnh hưởng module tồn kho (KhoChiTietController đọc lại tồn).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/KhoController.cs`
  - L40 `Kho_TimTheoDieuKien`, L53 `Kho_ThemMoi`, L96 `Kho_CapNhatThongTin`, L114 `Kho_Xoa` — quyền ghi giới hạn AD-TONG/AD-CN/AD-MCN (comment "Bỏ thu ngân, DV-QLDV").
  - L228 `TimTheoIdWithNoAuthen` + L335 `GetByIdsNoAuthen` = `[AllowAnonymous]` → bề mặt công khai, KHÔNG truyền UserLogin (Ecommerce/MISA).
  - L82/L237/L286/L293/L311/L323 = `[PrivateSBAuthorize]` (chỉ gọi nội bộ).
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/KhoService.cs`
  - L195-198 rule "bắt buộc 1 kho mặc định" (chặn gỡ cờ mặc định) bị **comment out** → hiện không chặn bỏ IsMacDinh.
  - L256-269 (Update) / L332-346 (Insert): logic ép kho khác về `IsMacDinh=false` — invariant "1 kho mặc định/chi nhánh".
  - L611-686 `DeleteAsync`: 2 nhánh theo `IsSuDungKhoTong` (xóa kèm KhoChiTiet + ES re-index vs chặn kho mặc định/đang dùng).
  - L352-410 `TimTheoDieuKienAsync`: đi SP `PRC_SCRM_WarehouseProduct_Ds_Kho`; non-AdminTổng bị ép IdChiNhanh; nhiều code cũ (EF) bị comment.
  - L529-598 `ValidateRequest`: MaKho duy nhất/chi nhánh, MKT mặc định không trùng, bắt buộc MaKho khi dùng nghiệp vụ kế toán.
  - L1498-1504 `GetKhoMacDinhPrivateAsync`: lấy kho `IsMacDinh==true` theo DonVi+ChiNhanh.
- FE `scrm_frontend/src/app/pages/WarehouseProduct/dm-kho/` (component lưới + `dm-kho-edit/` form). Route `WarehouseProduct/dm-kho` (routing L121).
