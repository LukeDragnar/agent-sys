---
controller: CauHinhChiaSoController
module: sales
service: Contact
man_hinh_ui: "Chia số (route 'chia-so') > tabs: Chia số theo nguồn (chia-so-theo-nguon) | Chia số thủ công (chia-so-thu-cong) | Chia số tự động (chia-so-tu-dong) | Bật/tắt nhận lead sale-cskh (bat-tat-nhan-lead) | Bật/tắt nhóm sale-cskh (bat-tat-nhan-lead-nhom-sale-cskh)"
muc_dich: "Trung tâm cấu hình & vận hành chia số (phân bổ lead): cấu hình chia theo nguồn/mặc định chi nhánh, CRUD cấu hình chia tự động (định mức theo nhóm/sale/sản phẩm/ca làm việc), phân bổ thủ công lead về nhóm/sale, bật-tắt nhận lead theo tài khoản/nhóm, và bật-tắt giới hạn check trùng số."
alias: [CauHinhChiaSo, CHCS, "cấu hình chia số", "chia số tự động", "chia số thủ công", "chia số theo nguồn", "phân bổ lead", "bật tắt nhận lead"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhChiaSo, CauHinhChiaSoDoiTuong, CauHinhChiaSoLienKet, CauHinhChiaSoSetting, Contact]
stored_procs: []
depends:
  - "CauHinhChiaSoService (ICauHinhChiaSoService — toàn bộ logic)"
  - "Core (GetCheckPermissionViewDropdownList... — lấy nhóm/user theo quyền: phòng ban, nhóm NV, nhân viên sale/cskh)"
  - "CommonCacheService (cache cấu hình CHCS theo IdCHCS — set/remove sau Save và Bật-tắt giới hạn)"
  - "LogLichSuTacNghiep (log tác nghiệp khi phân bổ lead)"
cross_module_in:
  - "ChiaSoV2Service / ChiaSoTheoNhomService / ChiaSoPUSHSALEService — runtime chia tự động đọc cấu hình tạo ở đây"
  - "KhoSoController (gián tiếp — cấu hình team/nhóm sale nhận data ảnh hưởng pool kho số)"
cross_module_out:
  - "Contact — phân bổ lead cập nhật SaleUserId/IdNhomNhanVien của contact (PhanBoTuDau, log tác nghiệp)"
  - "Cache CauHinhCS theo IdCHCS — set/remove sau mỗi thay đổi"
  - "Core/User — Bật-tắt nhận data cập nhật trạng thái nhận lead của tài khoản"
---

# CauHinhChiaSo — Cấu hình & vận hành chia số (lead)

> Controller lớn nhất cụm sales (368 dòng, ~22 endpoint), gom 6 vùng nghiệp vụ: **Cấu hình theo nguồn (CSTN)**, **Chia số tự động (CHCS)**, **Chia số thủ công (CSTC)**, **Bật/tắt nhận lead**, **Bật/tắt nhóm sale-cskh nhận data**, **Bật/tắt giới hạn check trùng**. Auth = **mixed** (đa số keyed SBAuthorize theo cụm role; vài endpoint `[SBAuthorize]` rỗng; 1 endpoint không gắn auth).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình chia số theo nguồn mặc định/chi nhánh | Lưu (CSTN setting) | POST `CHCS_CSTN_CauHinhChiaSoSetting_Save` | `CauHinhChiaSo_CHCS_CSTN_CauHinhChiaSoSetting_Save` | public | Tab Chia số theo nguồn | `CHCS_CSTN_CauHinhChiaSoSetting_SaveAsync(req)` — lưu mapping nguồn→kiểu chia mặc định theo chi nhánh. | EF | Service không nhận `CurrentUser` — đơn vị lấy từ req |
| 2 | Lấy cấu hình theo nguồn | (load tab) | GET `CHCS_CSTN_CauHinhChiaSoSetting_Get` | `CauHinhChiaSo_CHCS_CSTN_CauHinhChiaSoSetting_Get` | public | Tab Chia số theo nguồn | Trả setting CSTN theo `CurrentUser`. | EF | — |
| 3 | Dropdown CSTN | (combo) | GET `CHCS_CSTN_DropdownCHCS` | — (`[SBAuthorize]` rỗng) | public | Combo tab nguồn | Trả options kiểu chia. | EF | Không permission key |
| 4 | DS nguồn dữ liệu | (grid nguồn) | POST `CHCS_CSTN_GetDSNguonDuLieu` | `CauHinhChiaSo_CHCS_CSTN_GetDSNguonDuLieu` | public | Tab nguồn | List nguồn (landing) theo filter. | EF | Key có nhưng label rỗng |
| 5 | Lưu kiểu chia theo nguồn | Lưu (kiểu chia) | POST `CHCS_CSTN_Save` | — (`[SBAuthorize]` rỗng) | public | Tab nguồn | Gán kiểu chia số cho nguồn dữ liệu. | EF | **Key gốc bị comment-out** (cs:80) → hạ xuống `[SBAuthorize]` rỗng |
| 6 | DS cấu hình chia tự động (grid) | Danh sách CHCS | POST `CHCS_TimTheoDieuKien` | `CauHinhChiaSo_CHCS_TimTheoDieuKien` | public | Tab Chia số tự động | List cấu hình chia tự động theo filter. | EF | — |
| 7 | Xóa cấu hình chia tự động | Xóa | GET `CHCS_Xoa/{idCHCS}` | `CauHinhChiaSo_CHCS_Xoa` | public | Mỗi dòng grid CHCS | `CHCS_XoaAsync(idCHCS)`. | EF | GET cho hành động xóa (không idempotent-safe) |
| 8 | Lưu cấu hình chia tự động | Lưu | POST `CauHinhChiaSo_Save` | `CauHinhChiaSo_CauHinhChiaSo_Save` | public | Form thêm/sửa CHCS | Validate (xem "Quy tắc"); tạo `CauHinhChiaSo` + cây `CauHinhChiaSoDoiTuong`/`CauHinhChiaSoLienKet` (nhóm sale/cskh, định mức, sản phẩm, ca làm việc). | EF | — |
| 9 | Chi tiết cấu hình chia tự động | Mở edit | GET `CauHinhChiaSo_GetDetail/{idCHCS}` | `CauHinhChiaSo_CauHinhChiaSo_GetDetail` | public | Mở 1 cấu hình | `CauHinhChiaSo_GetDetailAsync(idCHCS)` (cache-aware). | EF + cache | — |
| 10 | DS phòng ban trong chi nhánh | (combo phòng ban) | GET `GetDSPhongBan/{isTrucThuoc:bool}` | `CauHinhChiaSo_CHCS_CSTC_GetDSPhongBan` | public | Tab thủ công | List phòng ban. | EF | — |
| 11 | DS phòng ban (bản 20250620) | (combo) | GET `GetDSPhongBan20250620` | — (`[SBAuthorize]` rỗng) | public | — | Bản mới nhận `idChiNhanh` param; key gốc bị comment (cs:151). | EF | Trùng chức năng #10, quyền lỏng hơn |
| 12 | DS nhóm NV theo phòng ban | (combo nhóm) | POST `GetDSNhomNVByPhongBan` | `CauHinhChiaSo_GetDSNhomNVByPhongBan` | public | Tab thủ công | List nhóm NV trong phòng. | EF | — |
| 13 | DS nhân viên theo nhóm | (combo NV) | POST `GetDSNhanVienByNhom` | — (`[SBAuthorize]` rỗng) | public | Tab thủ công | List NV trong nhóm. | EF | Không permission key |
| 14 | DS contact chưa phân bổ (all / theo nhóm) | Grid contact chưa chia | POST `CHCS_CSTC_GetDSAllContactChuaPhanBo` / `CHCS_CSTC_GetDSContactTheoNhomChuaPhanBo` | `CauHinhChiaSo_CHCS_CSTC_GetDSAllContactChuaPhanBo` / `...GetDSContactTheoNhomChuaPhanBo` | public | Tab thủ công | List contact chưa có chủ để chia. | EF | — |
| 15 | DS nguồn chưa phân bổ (nhóm / sale) | Grid nguồn | POST `CHCS_CSTC_ChiaTheoNhom_GetDSNguonChuaPhanBoContact` / `CHCS_CSTC_ChiaTheoSale_GetDSNguonChuaPhanBoContact` | `...ChiaTheoNhom_GetDSNguonChuaPhanBoContact` / `...ChiaTheoSale_...` | public | Tab thủ công | List nguồn dữ liệu còn data chưa chia. | EF | — |
| 16 | DS nhóm/nhân viên đích (nhóm / sale) | Combo đích | POST `CHCS_CSTC_ChiaTheoNhom_GetDSNhomNhanVien` / `CHCS_CSTC_ChiaTheoSale_GetDSNhanVien` | `...ChiaTheoNhom_GetDSNhomNhanVien` / `...ChiaTheoSale_GetDSNhanVien` | public | Tab thủ công | List nhóm/NV nhận lead. | EF + Core | — |
| 17 | **Phân bổ lead về nhóm/sale (theo cấu hình)** | Áp dụng phân bổ | POST `CHCS_CSTC_ChiaTheoNhom_PhanBoLeadVeNhomSaleOrCSKH` / `CHCS_CSTC_ChiaTheoSale_PhanBoLeadVeSaleOrCSKH` | `...ChiaTheoNhom_PhanBoLeadVeNhomSaleOrCSKH` / `...ChiaTheoSale_PhanBoLeadVeSaleOrCSKH` | public | Tab thủ công | Phân bổ lead theo định mức → update Contact (SaleUserId/nhóm) + log tác nghiệp. | EF | — |
| 18 | **Phân bổ ngay (nhóm/sale)** | Phân bổ ngay | POST `CHCS_CSTC_ChiaTheoNhom_PhanBoNgayVeNhomSaleOrCSKH` / `CHCS_CSTC_ChiaTheoSale_PhanBoNgayVeSaleOrCSKH` | `...PhanBoNgayVeNhomSaleOrCSKH` / `...ChiaTheoSale_PhanBoNgayVeSaleOrCSKH` | public | Tab thủ công | Nhận `List<...ReqDto>` — gán ngay lead cụ thể về NV/nhóm. | EF | Body là List (không bọc) |
| 19 | DS sales để bật/tắt nhận data | Grid sales | POST `CHCS_LayDSSales` | `CauHinhChiaSo_CHCS_LayDSSales` | public | Tab bật/tắt nhận lead | List sale + trạng thái nhận data. | EF | — |
| 20 | Bật/tắt nhận data cho 1 / nhiều tài khoản | Toggle nhận data | POST `BatTatNhanDuLieuChoTaiKhoan` / `BatTatNhanDuLieuChoNhieuTaiKhoan` / `BatTatNhanDuLieuChoNhieuTaiKhoanUsername` | `..._BatTatNhanDuLieuChoTaiKhoan` / `...ChoNhieuTaiKhoan` / `...ChoNhieuTaiKhoanUsername` | public | Tab bật/tắt nhận lead | Bật/tắt cờ nhận lead của tài khoản (1 / nhiều theo Id / nhiều theo Username). | EF + Core | 3 biến thể song song (drift risk) |
| 21 | Lịch sử thay đổi cấu hình chia số | (popup) Lịch sử | POST `LichSuThayDoiCauHinhChiaSo` | — (key bị comment-out) | public | Mở log CHCS | List lịch sử thay đổi cấu hình. | EF | **Không có auth filtering** — key bị comment (cs:335) |
| 22 | DS nhóm sale-cskh nhận data + Bật/tắt | Grid + toggle nhóm | POST `CHCS_GetDSNhomSaleNhanData` / `CHCS_BatTatNhomSaleNhanData` | `CHCS_GetDSNhomSaleNhanData` / `CHCS_BatTatNhomSaleNhanData` | public | Tab nhóm sale-cskh | Tìm + bật/tắt nhận data cho cả nhóm sale/cskh. | EF | — |
| 23 | Bật/tắt giới hạn check trùng số | Toggle giới hạn check trùng | POST `CHCS_BatTatGioiHanCheckSoContact` | `CHCS_BatTatGioiHanCheckSoContact` (chỉ role HT-CSKH) | public | Tab nâng cao (chỉ HT-CSKH) | Set `ApDungCheckGioiHanSo` cho danh sách CHCS, rồi remove+set lại cache CHCS theo Id. | EF + cache | Chỉ HT-CSKH (toàn hệ thống) thao tác |

## Quy tắc nghiệp vụ (gating / validate)
**Lưu cấu hình chia tự động — `CauHinhChiaSo_SaveAsync` (CauHinhChiaSoService.cs:769):**
1. `Ten` rỗng ⇒ lỗi "Tên không được để trống!".
2. Trùng tên trong cùng đơn vị+chi nhánh (khác Id) ⇒ lỗi `MESS_Model_EXISTS`.
3. `req.Id == null` ⇒ **tạo mới** `CauHinhChiaSo` (gán đơn vị/chi nhánh từ `_currentUser`, cờ `ApDungCheckGioiHanSo ?? false`, các cờ `ApDungChiaLead...` lấy từ `CauHinhChiaSoChoNhomSale`/`...ChoNhomCSKH`/`CauHinhSoDuocChia`); có Id ⇒ cập nhật.
4. Duyệt 2 cấp đối tượng (NhomSale → NhanVien, NhomCSKH → CSKH): mỗi node tạo `CauHinhChiaSoDoiTuong` với `DinhMucChiaSo`, `LoaiDoiTuong`, `IsAutoFitTheoDinhMucChiaSoUser`, `IsTatNhanDuLieu`.
5. `KieuChonSanPham`: có `IdSanPham` ⇒ `ChonSanPhamCuThe`; rỗng + `IsChonTatCa` ⇒ `ChonTatCa`; ngược lại ⇒ `KhongChon`.

**Bật/tắt giới hạn check trùng — `CHCS_BatTatGioiHanCheckSoContactAsync` (cs:4126):**
1. Lấy các CHCS theo đơn vị+chi nhánh user trong `req.LstIdCHCS`; rỗng ⇒ lỗi "Không tìm thấy cấu hình chia số nào để cập nhật".
2. Set `ApDungCheckGioiHanSo = req.IsEnabled` cho từng cấu hình (MarkDirty) rồi UpdateAsync.
3. Mỗi CHCS: remove cache `CauHinhCS_RemoveBy_IdCHCS` rồi set lại từ `CauHinhChiaSo_GetDetailAsync` ⇒ cache đồng bộ với DB.

## Quan hệ
- **Gọi sang:** `ICauHinhChiaSoService` (toàn bộ), Core (dropdown nhóm/user theo quyền), CommonCacheService (cache CHCS), LogLichSuTacNghiep (phân bổ lead).
- **Dữ liệu (entities):** `CauHinhChiaSo` (master), `CauHinhChiaSoDoiTuong` (cấp nhóm/NV + định mức + sản phẩm), `CauHinhChiaSoLienKet` (liên kết), `CauHinhChiaSoSetting` (theo nguồn/chi nhánh), `Contact` (đối tượng được phân bổ).
- **Ripple / đồng bộ:** cấu hình tạo ở đây được runtime chia tự động (`ChiaSoV2Service`/`ChiaSoTheoNhomService`/`ChiaSoPUSHSALEService`) đọc khi auto-chia lead; phân bổ thủ công update Contact + log; cache CHCS phải set lại sau Save/Bật-tắt.

## Luồng xuyên module (phân bổ lead thủ công → cập nhật contact)
trigger: admin/QL bấm "Áp dụng phân bổ" (theo nhóm/sale) → `PhanBoLeadVe...Async(req, CurrentUser)`: chọn contact chưa phân bổ + đích (nhóm/NV) theo định mức → update `Contact.SaleUserId/IdNhomNhanVien` (`PhanBoTuDau`), ghi log tác nghiệp → side-effect: contact đổi chủ ảnh hưởng kho số/đơn hàng của contact đó. Runtime auto-chia (tách service) dùng chính cấu hình này.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhChiaSoController.cs:80-81` — `CHCS_CSTN_Save`: permission key gốc bị comment-out, thay bằng `[SBAuthorize]` rỗng → ghi cấu hình kiểu chia số chỉ cần login (mất phân quyền HT-CSKH/AD).
- `CauHinhChiaSoController.cs:151-152` — `GetDSPhongBan20250620` bỏ permission key (`[SBAuthorize]` rỗng) trong khi bản cũ `GetDSPhongBan` (cs:144) còn key → 2 endpoint cùng chức năng, quyền lệch nhau (drift).
- `CauHinhChiaSoController.cs:335-336` — `LichSuThayDoiCauHinhChiaSo` key `[SBAuthorize("LichSuThayDoiCauHinhChiaSo",...)]` bị comment-out hoàn toàn → endpoint **không gắn `[SBAuthorize]` nào** ở method, chỉ còn `[SBAuthorize]` controller-level rỗng ⇒ mọi user login xem được lịch sử cấu hình chia số.
- `CauHinhChiaSoController.cs:108` — `CHCS_Xoa` dùng HTTP GET cho hành động xóa cấu hình (dễ trigger ngoài ý muốn, không CSRF-safe).
- `CauHinhChiaSoController.cs:57,82,153,177` — nhiều endpoint dùng `[SBAuthorize]` rỗng (DropdownCHCS, CSTN_Save, GetDSPhongBan20250620, GetDSNhanVienByNhom) → controller auth = **mixed**, cần rà soát những cái ghi dữ liệu (CSTN_Save).
- `CauHinhChiaSoController.cs:20-29` — `IChiaSoService` inject bị comment-out (chỉ dùng `ICauHinhChiaSoService`) → di tích, không ảnh hưởng.
- `CauHinhChiaSoService.cs:774` — check trùng tên dùng `_currentUser` (field) thay vì param `currentUser` truyền vào → cần đảm bảo `_currentUser` đã được set đúng (DI scoped) khớp `currentUser`, nếu lệch sẽ check trùng sai đơn vị.
- `CauHinhChiaSoController.cs` — cụm endpoint V1/V2 song song (`GetDSPhongBan` vs `GetDSPhongBan20250620`; 3 biến thể `BatTatNhanDuLieuCho...`) → sửa rule phải đồng bộ nhiều nhánh, rủi ro drift.
