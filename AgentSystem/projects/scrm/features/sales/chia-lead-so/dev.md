# Dev — Chia Lead / số

> Service: **Contact** (BE .NET) · FE module Sale (Angular 15 + ng-zorro). Controller: `CauHinhChiaSoController` (controller lớn nhất cụm sales, ~22 endpoint, 6 vùng nghiệp vụ). Chỉ dành cho dev. Trace registry: `projects/scrm/registry/sales/CauHinhChiaSoController.md`. Runtime auto-chia ở service khác: `ChiaSoV2Service` / `ChiaSoTheoNhomService` / `ChiaSoPUSHSALEService`.

## Màn hình & route (FE)
| Màn hình / tab | Route |
|---|---|
| Chia số (gốc) | `chia-so` |
| Chia số theo nguồn | `chia-so-theo-nguon` |
| Chia số thủ công | `chia-so-thu-cong` |
| Chia số tự động | `chia-so-tu-dong` |
| Bật/tắt nhận lead (sale-cskh) | `bat-tat-nhan-lead` |
| Bật/tắt nhóm sale-cskh | `bat-tat-nhan-lead-nhom-sale-cskh` |

## Endpoint / Controller (trace registry — 6 vùng)
**`CauHinhChiaSoController`** (service Contact, auth = mixed). Nhóm chính:
- **Chia theo nguồn (CSTN)**: `CHCS_CSTN_CauHinhChiaSoSetting_Save` / `_Get`, `CHCS_CSTN_DropdownCHCS`, `CHCS_CSTN_GetDSNguonDuLieu`, `CHCS_CSTN_Save`.
- **Chia tự động (CHCS)**: `CHCS_TimTheoDieuKien` (list), `CauHinhChiaSo_Save` (lưu), `CHCS_Xoa/{idCHCS}` (GET xóa), `CauHinhChiaSo_GetDetail/{idCHCS}`.
- **Chia thủ công (CSTC)**: `GetDSPhongBan/{isTrucThuoc}`, `GetDSPhongBan20250620`, `GetDSNhomNVByPhongBan`, `GetDSNhanVienByNhom`, `CHCS_CSTC_GetDSAllContactChuaPhanBo` / `...GetDSContactTheoNhomChuaPhanBo`, `...ChiaTheoNhom/Sale_GetDSNguonChuaPhanBoContact`, `...GetDSNhomNhanVien` / `...GetDSNhanVien`, `...PhanBoLeadVeNhomSaleOrCSKH` / `...PhanBoLeadVeSaleOrCSKH`, `...PhanBoNgayVe...`.
- **Bật/tắt nhận lead**: `CHCS_LayDSSales`, `BatTatNhanDuLieuChoTaiKhoan` / `...ChoNhieuTaiKhoan` / `...ChoNhieuTaiKhoanUsername`.
- **Bật/tắt nhóm sale-cskh**: `CHCS_GetDSNhomSaleNhanData` / `CHCS_BatTatNhomSaleNhanData`.
- **Bật/tắt giới hạn check trùng**: `CHCS_BatTatGioiHanCheckSoContact` (chỉ role HT-CSKH).
- **Lịch sử**: `LichSuThayDoiCauHinhChiaSo` (key bị comment — xem bug-prone).

## Entities
- **`CauHinhChiaSo`** — master cấu hình chia tự động (đơn vị/chi nhánh, cờ `ApDungCheckGioiHanSo`, `ApDungChiaLead...`).
- **`CauHinhChiaSoDoiTuong`** — node nhóm/NV: `DinhMucChiaSo`, `LoaiDoiTuong`, `IsAutoFitTheoDinhMucChiaSoUser`, `IsTatNhanDuLieu`, sản phẩm (`KieuChonSanPham`).
- **`CauHinhChiaSoLienKet`** — liên kết cấu hình.
- **`CauHinhChiaSoSetting`** — setting chia theo nguồn/chi nhánh.
- **`Contact`** — đối tượng được phân bổ (`SaleUserId`, `IdNhomNhanVien`, `PhanBoTuDau`).

## Luồng code
- **Lưu cấu hình tự động** (`CauHinhChiaSoService.cs:769` `CauHinhChiaSo_SaveAsync`): validate tên rỗng/trùng → `req.Id==null` tạo mới (gán đơn vị/chi nhánh từ `_currentUser`) / có Id cập nhật → duyệt 2 cấp NhomSale→NV, NhomCSKH→CSKH tạo `CauHinhChiaSoDoiTuong` → `KieuChonSanPham` theo IdSanPham/IsChonTatCa.
- **Bật/tắt giới hạn check trùng** (`cs:4126` `CHCS_BatTatGioiHanCheckSoContactAsync`): lấy CHCS theo đơn vị+chi nhánh trong `req.LstIdCHCS` (rỗng → lỗi) → set `ApDungCheckGioiHanSo` → remove cache `CauHinhCS_RemoveBy_IdCHCS` + set lại từ `CauHinhChiaSo_GetDetailAsync`.
- **Phân bổ thủ công** (`PhanBoLeadVe...Async`): chọn contact chưa phân bổ + đích theo định mức → update Contact + `LogLichSuTacNghiep`.
- **Runtime auto-chia**: `ChiaSoV2Service` / `ChiaSoTheoNhomService` / `ChiaSoPUSHSALEService` đọc cấu hình tạo ở đây.

## Bug-prone / lưu ý (KHÔNG sửa ở đây — chỉ ghi; có lỗ hổng auth → để session security)
- **`CHCS_CSTN_Save` mất phân quyền** (`CauHinhChiaSoController.cs:80-81`): permission key gốc bị comment-out, thay `[SBAuthorize]` rỗng → ghi cấu hình kiểu chia số chỉ cần login (mất gate HT-CSKH/AD). **Endpoint GHI dữ liệu mà quyền lỏng.**
- **`LichSuThayDoiCauHinhChiaSo` không gắn auth method-level** (`cs:335-336`): key bị comment-out hoàn toàn → chỉ còn `[SBAuthorize]` controller-level rỗng → mọi user login xem được lịch sử cấu hình chia số.
- **`GetDSPhongBan20250620` quyền lệch bản cũ** (`cs:151-152`): bản mới bỏ permission key (`[SBAuthorize]` rỗng) trong khi `GetDSPhongBan` (cs:144) còn key → 2 endpoint cùng chức năng quyền lệch (drift).
- **`CHCS_Xoa` dùng HTTP GET** (`cs:108`): xóa cấu hình qua GET → dễ trigger ngoài ý muốn, không CSRF-safe.
- **Nhiều endpoint `[SBAuthorize]` rỗng** (`cs:57, 82, 153, 177`): DropdownCHCS, CSTN_Save, GetDSPhongBan20250620, GetDSNhanVienByNhom → cần rà cái nào ghi dữ liệu.
- **Check trùng tên dùng `_currentUser` field thay vì param** (`CauHinhChiaSoService.cs:774`): nếu `_currentUser` (DI scoped) lệch `currentUser` truyền vào → check trùng sai đơn vị.
- **Endpoint V1/V2 song song** (`GetDSPhongBan` vs `GetDSPhongBan20250620`; 3 biến thể `BatTatNhanDuLieuCho...`): sửa rule phải đồng bộ nhiều nhánh → rủi ro drift.
- **`PhanBoNgayVe...` body là `List<...ReqDto>` không bọc** → chú ý khi gọi/test.
- **`IChiaSoService` inject bị comment** (`cs:20-29`): di tích, chỉ dùng `ICauHinhChiaSoService` — không ảnh hưởng.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Contact\...\Controllers\CauHinhChiaSoController.cs:80-81` (CSTN_Save), `:108` (CHCS_Xoa GET), `:144/:151-152` (GetDSPhongBan drift), `:335-336` (LichSu key comment), `:57/:82/:153/:177` (SBAuthorize rỗng).
- BE service: `...\CauHinhChiaSoService.cs:769` (Save), `:774` (_currentUser), `:4126` (BatTatGioiHanCheckSo).
- Runtime auto-chia: `ChiaSoV2Service`, `ChiaSoTheoNhomService`, `ChiaSoPUSHSALEService` (service Contact).
- Entities: `CauHinhChiaSo`, `CauHinhChiaSoDoiTuong`, `CauHinhChiaSoLienKet`, `CauHinhChiaSoSetting`, `Contact`.
- FE: module Sale, route gốc `chia-so` + các tab con (xem bảng route).
