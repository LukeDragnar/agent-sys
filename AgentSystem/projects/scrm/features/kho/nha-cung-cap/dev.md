# Dev — Quản lý nhà cung cấp (chỉ kỹ thuật)

Service BE: **WarehouseProduct** (.NET 6). FE: **Angular + ng-zorro**, module `WarehouseProduct`.

## Màn hình & route (FE)
| Màn hình | Route | Component list | Component edit (popup) |
|---|---|---|---|
| Nhà cung cấp | `quan-ly-ncc` | `QuanlyNhacungcapComponent` | `EditQuanlyNhacungcapComponent` |
| Nhóm nhà cung cấp | `quan-ly-nncc` | `QuanlyNhomnhacungcapComponent` | `QuanlyNhomnhacungcapEditComponent` |

Routing: `pages/WarehouseProduct/warehouseProduct-routing.module.ts`.
Service FE: `services/warehouseproduct/nhacungcap.service.ts`, `services/warehouseproduct/nhomNhaCungCap.service.ts`.

> **Lưu ý FE quan trọng:** form edit NCC submit qua **`NhaCungCap_Insert` / `NhaCungCap_Udpate`** (xem `EditQuanlyNhacungcapComponent.insertData/updateData`), KHÔNG dùng `ThemMoi`/`CapNhatThongTin` (các call cũ đã bị comment trong `addOrEdit()`). `ThemMoi`/`CapNhatThongTin` vẫn còn ở controller/service nhưng là luồng legacy.

## Controller (BE)
`WarehouseProduct.API/Controllers/NhaCungCapController.cs` — `[Route("api/NhaCungCap")]`, attr `[TienIchAttribute("NhaCungCapController", ..., MaTienIchSCRM.TI_Kho)]`.

Endpoint chính:
- `POST TimTheoDieuKien` — list/search phân trang.
- `POST ThemMoi` (`NhaCungCap_ThemMoi`) / `POST CapNhatThongTin` — **legacy** (FE không gọi nữa).
- `POST NhaCungCap_Insert` / `POST NhaCungCap_Udpate` — **luồng FE hiện tại** (có tích hợp Customer + confirm code). Quyền dùng chung `NhaCungCap_ThemMoi` / `NhaCungCap_CapNhatThongTin`.
- `GET TimTheoId/{id}` — có guard `IdDonVi == CurrentUser.DonViId && IdNganhNghe == CurrentUser.IdNganhNghe`.
- `POST Xoa/{id}` (`NhaCungCap_Xoa`) — chặn xóa nếu có phiếu mua hàng / khách hàng liên quan.
- `POST ExportExcelNhaCungCap`, `POST ImportNhaCungCap`, `POST DownloadFileTemplateImportNhaCungCap`, `POST DownloadDataWrongNhaCungCap/{id}`.
- `POST SearchNhaCungCapAutoComplete`, các endpoint `[PrivateSBAuthorize]` cho service nội bộ (`GetNhaCungCapByCodes`, `DanhSachNhaCungCapTuDong`, `KhachHangDongThoiLaNhaCungCap`...).

`NhomNhaCungCapController.cs` — `[Route("api/NhomNhaCungCap")]`: `TimTheoDieuKien`, `ThemMoi` (`NhomNhaCungCap_ThemMoi`, **chỉ `DV-AD-TONG`**), `TimTheoId`, `CapNhatThongTin`, `Xoa` (GET), `DropdownDsNhomNhaCungCap`.

## Entities
- `WarehouseProduct.DataContext/Entity/NhaCungCap.cs : BaseEntity` — `Ten`, `NguoiLienHe`, `SdtNguoiLienHe(max 11)`, `DiaChi`, `Email`, `SuDung`, `IdDonVi`, `IdNganhNghe`, `Ma`, `MaSoThue`, `IdNhomNhaCungCap`, `IdNhanVienMuaHang`, `SoTaiKhoan`, `BankCode(16)`, `TenChuTaiKhoan`, `TenNganHang`, `SoNgayDuocNo`, `SoNgayNoToiDa`, `TaiKhoanCongNoPhaiTra`, `IsLaToChuc`, `IsLaCaNhan`, `IsDongThoiLaKhachHang`, `IdDoiTacTraGop`, `IsAuto`.
- `NhomNhaCungCap.cs : BaseEntity` — `IdDonVi`, `Stt`, `Ten`.
- `NhaCungCapGhiNo.cs` — ghi nhận công nợ NCC (liên quan phiếu mua/trả tiền/bù trừ).

DTO: `Libs/Models.Core/DTO/WarehouseProduct.Client/NhaCungCap/NhaCungCapDto.cs` — chứa `NhaCungCapDto`, `ThemMoiNhaCungCapDto`, `CapNhatNhaCungCapDto`, `ReqNhaCungCap_Insert/Update`, `ResNhaCungCapInsertUpdate`, `NhomNhaCungCapDto`, `ReqKhachHangDongThoiLaNhaCungCap`...

## Luồng code (`NhaCungCap_ThemMoi`)
Service: `WarehouseProduct.Service/Generated/NhaCungCapService.cs`.

**`ThemMoiAsync(ThemMoiNhaCungCapDto, userLogin)`** (luồng legacy):
1. AutoMapper DTO→entity, set `IdDonVi`/`IdNganhNghe` từ `userLogin`.
2. Validate: `Ten`, `SdtNguoiLienHe`, `Ma` not empty → `Ma.Trim()` → check SĐT bằng `ValidationCommon.CheckPhoneByRegex` → check email `Helper.IsValidEmail`.
3. Check trùng `Ma` trong cùng `IdDonVi + IdNganhNghe` → lỗi nếu trùng.
4. Check trùng `IdDoiTacTraGop`; check `SoNgayNoToiDa >= SoNgayDuocNo`.
5. `reposNhaCungCap.InsertAsync(model)` (bọc try/catch rỗng — xem Bug-prone).
6. Nếu `IsDongThoiLaKhachHang == true` → `UpdateKhachHangFromNhaCungCap` POST sang `CustomerServiceUrl/api/KhachHang/UpdateKhachHangFromNhaCungCap`.

**`NhaCungCap_Insert(ReqNhaCungCap_Insert, userLogin)`** (luồng FE thật):
1. Validate null (`ValidationCommon.CheckNull`) cho `Ten`, `Ma`; nếu `IsAuto != true` thì bắt buộc `SdtNguoiLienHe`.
2. Validate SĐT/email/`IdDoiTacTraGop`/khoảng ngày nợ; check trùng `Ma`.
3. **Gọi Customer trước**: `customerService.KhachHang_NhaCungCapDongThoiLaKhachHang(...)`. Nếu Customer trả `Success=false` mà có `Data` → trả về `ResNhaCungCapInsertUpdate { IsConfirm, ConfirmCode, IdKhachHang }` để FE bật popup xác nhận, rồi gọi lại với `ConfirmStatus=true`.
4. Insert entity; nếu `IsAuto==true` → cập nhật Redis cache key `NhaCungCapAutoByDonVi_{DonViId}`.

**`NhomNhaCungCap_ThemMoi`** (`NhomNhaCungCapService.ThemMoiAsync`): map DTO→entity, set `IdDonVi`, validate `Ten` not empty, `base.InsertAsync(model)`. Đơn giản, không gọi cross-service.

## Bug-prone / lưu ý khi sửa
- **`try/catch` rỗng quanh `InsertAsync`** ở cả `ThemMoiAsync` và `NhaCungCap_Insert`: lỗi insert bị **nuốt** → `ThemMoiAsync` vẫn trả `Id` (đã gán client-side), `NhaCungCap_Insert` rơi xuống `ResultWithError("Lỗi không xác định")`. Khó debug khi DB fail.
- **Hai luồng song song** `ThemMoi`/`CapNhatThongTin` (legacy) vs `NhaCungCap_Insert`/`NhaCungCap_Udpate` (hiện tại). Sửa nghiệp vụ phải sửa đúng luồng FE đang gọi (`*_Insert`/`*_Udpate`), tránh sửa nhầm legacy.
- **`TableWithNolock` (dirty read)** dùng cho check trùng mã → có race nếu 2 request tạo cùng mã đồng thời (không có unique constraint/lock thật trong code).
- **Mã NCC ↔ mã KhachHang dùng chung không gian**: logic confirm "đồng thời là khách hàng" trải qua `customerService`. Đổi mã NCC cũng kích hoạt nhánh này (`NhaCungCap_Udpate` khi `Ma` đổi hoặc cờ `IsDongThoiLaKhachHang` đổi).
- **NCC `IsAuto==true` không cho `NhaCungCap_Udpate`** (chặn trong service) — nhưng `ThemMoiAsync` legacy không set `IsAuto`.
- **Xóa NCC**: guard ở controller gọi `_commonService.IsNccDaCoPhieuMuaHang(id)` và `IsNccDaCoKhachHang(ma)` — không chỉ check ở service.
- **`CheckPhoneByRegex(...).Result`** gọi `.Result` trên Task (blocking) trong code async — pattern cũ, dễ deadlock nếu context đổi.

## File source
- BE controller: `WarehouseProduct/WarehouseProduct.API/Controllers/NhaCungCapController.cs`, `.../NhomNhaCungCapController.cs`
- BE service: `WarehouseProduct/WarehouseProduct.Service/Generated/NhaCungCapService.cs`, `.../NhomNhaCungCapService.cs`
- BE entity: `WarehouseProduct/WarehouseProduct.DataContext/Entity/NhaCungCap.cs`, `NhomNhaCungCap.cs`, `NhaCungCapGhiNo.cs`
- DTO: `Libs/Models.Core/DTO/WarehouseProduct.Client/NhaCungCap/NhaCungCapDto.cs`
- FE component: `pages/WarehouseProduct/quanly-nhacungcap/quanly-nhacungcap.component.{ts,html}`, `.../edit-quanly-nhacungcap/edit-quanly-nhacungcap.component.{ts,html}`, `.../import-nhacungcap/`, `pages/WarehouseProduct/quanly-nhomnhacungcap/...`
- FE service: `services/warehouseproduct/nhacungcap.service.ts`, `services/warehouseproduct/nhomNhaCungCap.service.ts`
- FE route: `pages/WarehouseProduct/warehouseProduct-routing.module.ts` (`quan-ly-ncc`, `quan-ly-nncc`)
