# Dev — Quản lý hồ sơ nhân viên

> Service: **Core** (BE .NET) cho tài khoản/phòng ban/nhóm. Khu admin `QuanTriArea`. Chỉ dành cho dev.

## Màn hình & route (UI)
| Màn hình | Vị trí UI |
|---|---|
| Quản lý nhân viên (tài khoản) | Hệ thống > Quản lý nhân viên (admin QuanTriArea) |
| Cơ cấu phòng ban | Đơn vị > Cơ cấu tổ chức > Phòng ban (cây kéo-thả) |
| Nhóm nhân viên | Đơn vị > Cơ cấu tổ chức > Nhóm nhân viên |
| Nhân viên trong phòng ban/nhóm | Đơn vị > Cơ cấu tổ chức > (chi tiết phòng ban) |

> FE component/route cụ thể chưa trace session này — bổ sung khi đụng FE.

## Endpoint/Controller (trace registry)
**Tài khoản — `UserController` (service Core)** — registry: `registry/he-thong/UserController_Core.md` (route `api/QuanTriArea/User`):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tìm theo điều kiện | POST `TimTheoDieuKien` | `User_TimTheoDieuKien` (HT-AD) |
| DS user theo đơn vị | POST `LayDanhSachUserDonViTheoDieuKien` | `User_LayDanhSachUserDonViTheoDieuKien` (HT-AD, HT-CSKH) |
| Chi tiết | GET `TimTheoId/{id}` | `User_TimTheoId` (HT-AD) |
| Thêm / Sửa | POST `ThemMoi` / `CapNhatThongTin` | `User_ThemMoi` / `User_CapNhatThongTin` (HT-AD) |
| Xóa / Khóa-Mở / Đổi MK | POST `XoaTaiKhoan` / `KhoaTaiKhoan` / `DoiMatKhau` | `User_XoaTaiKhoan` / `_KhoaTaiKhoan` / `_DoiMatKhau` (HT-AD) |
| Tìm đơn vị (CSKH chuyển) | GET `GetDonViChiNhanhByUser` · POST `GetDonViByMaDon` | `[SBAuthorize]` rỗng — check trong service (super/CSKH) |
| Đồng bộ user theo đơn vị | POST `DongBoUserByDonVi` · GET `GetStatusDongBoUserByDonVi` | internal, check trong service |

⚠️ Có 2 `UserController` cùng module he-thong: bản **Core** (node `_Core`, admin nhân viên — node này) khác bản **AuthenCustomer** (`UserController_AuthenCustomer.md`, API app khách hàng).

**Phòng ban — `PhongBanController` (Core)** — registry: `registry/he-thong/PhongBanController.md` (route `api/PhongBan`):
- `GetCoCauPhongBan_Unit/_Branch`, `SapXepCoCauPhongBan`, `ThemMoi_Unit/_Branch`, `CapNhat_*`, `Xoa_*`, `TimTheoId_*`, `ThemMoi_UnitQuanLy/_BranchQuanLy`, `GetCoCauPhongBan` (CurrentUser), `UserGetThongTinPhongBan`.
- Key: `_Unit` → DV-AD-TONG; `_Branch` → DV-AD-CN (ép IdChiNhanh).
- Private: `GetAndSetCoCauCache_TheoDonVi/{id}` `[PrivateSBAuthorize]` (build+cache cơ cấu).

**Nhân viên trong nhóm — `PhongBanNhanVienController` (Core)** — registry: `registry/he-thong/PhongBanNhanVienController.md`:
- `ThemMoi_Unit/_Branch` (AddMultiUser), `Xoa_Unit/_Branch`, `GetAllIdUser`, `GetDanhSachNhanVien`.
- Private: `KiemTraLeadSaleOrQuanLySale` `[PrivateSBAuthorize]` (service sale check quan hệ lead/quản lý).

**Nhóm nhân viên — `NhomNhanVienController` (Core)** — registry: `registry/he-thong/NhomNhanVienController.md`:
- `TimTheoDieuKien` (`NhomNhanVien_TimTheoDieuKien`, DV-AD-TONG), `ThemMoi-Nhom`, `CapNhap-Nhom`, `Xoa/{id}`, `GetListByDonVi`, `LayAllNhomCSKH`, `LayNhanVienByNhom/{idNhom}`, `InitStaffGroupSystem`/`InitStaffGroupOrg`.
- Private dropdown theo role: `LayNhomNhanVienByRole/{roleCode}` v.v.

## Entities
- `User`, `Role`, `DonVi` — tài khoản & phân quyền (gắn IdDonVi/IdChiNhanh/IdNganhNghe/Domain).
- `PhongBan` — cơ cấu cây phòng ban.
- `NhomNhanVien`, `UserNhomNhanVien` — nhóm + map user↔nhóm.

## Luồng code
1. **Thêm user:** `UserController.ThemMoi` — `IdDonVi` rỗng → tạo user "không đơn vị" (Guid.Empty + domain theo môi trường, dòng 112-126); có đơn vị → `ICommonService.GetOrgByIdAsync` lấy ngành nghề/domain (đơn vị null → NotFound, dòng 101-105) → `ThemMoiTaiKhoanAsync` (service tự đồng bộ).
2. **Phòng ban/nhóm Branch:** ép `IdChiNhanh = CurrentUser.IdChiNhanh` trước khi `Create/Update/Delete`.
3. **Cache cơ cấu:** đổi phòng ban → `GetAndSetCoCauCache_TheoDonVi` build lại cache → phục vụ `PhongBanNhanVien.KiemTraLeadSaleOrQuanLySale`.

## Bug-prone / lưu ý
- **Endpoint nội bộ chỉ `[SBAuthorize]` rỗng, dựa service tự check quyền**: `UserController.cs:200-243` (GetDonViChiNhanhByUser/DongBoUserByDonVi…) — audit không thấy key, phải đọc service.
- **Domain hardcode theo môi trường**: `UserController.cs:118-125` — đổi domain phải sửa code.
- **Role string thiếu dấu phẩy**: `PhongBan_SapXepCoCauPhongBan` (PhongBanController dòng 33) khai báo `"DV-AD-TONG DV-AD-CN"` → cần kiểm tra parser tách role đúng 2 role.
- **Biến thể \_UnitQuanLy/\_BranchQuanLy tái dùng key thêm thường** → không gate riêng được hành vi gán user quản lý.
- **Route nhóm sai chuẩn**: `ThemMoi-Nhom`/`CapNhap-Nhom` (CapNhap thiếu dấu) — không phải `ThemMoi`/`CapNhatThongTin` kế thừa từ `SBBaseController`; FE phải gọi đúng.
- **Init nhóm mẫu không gắn key**: `InitStaffGroupSystem`/`InitStaffGroupOrg` chỉ `[SBAuthorize]` login-only → mọi user login có thể sinh dữ liệu mẫu (rủi ro — chỉ ghi nhận, session security xử lý).
- **Payload Unit dùng nguyên giá trị client**: `PhongBanNhanVien` thao tác Unit không ép IdDonVi/IdChiNhanh từ CurrentUser → cần service validate hợp lệ với CurrentUser (xem registry known-issue).

## File source (path:line)
- `UserController.cs:101-105,112-126,140-147,200-243` (service Core).
- `PhongBanController.cs:33` (role sắp xếp); biến thể QuanLy tái dùng key.
- `NhomNhanVienController.cs:99` (key GetListByDonVi đã comment) + route `ThemMoi-Nhom`/`CapNhap-Nhom`.
- `PhongBanNhanVienController` — thao tác Unit/Branch + private `KiemTraLeadSaleOrQuanLySale`.
- Registry nodes: `projects/scrm/registry/he-thong/{UserController_Core,PhongBanController,PhongBanNhanVienController,NhomNhanVienController}.md`.

---

## (Bổ sung Pha 2 — phần Hồ sơ nhân sự HR; product xác nhận có tách feature riêng không)

> Service: **HumanResource** (BE .NET) — màn "Hồ sơ nhân sự HR". Độc lập với service Core ở phần trên; gắn vào nhân viên qua `NhanSu.IdUser` map 1-1 với `User.Id`.

### Màn hình & route (UI — HR)
| Màn hình | Vị trí UI |
|---|---|
| Hồ sơ nhân sự (form nhân thân + nghiệp vụ HR) | Nhân sự > Hồ sơ nhân sự (mở theo từng nhân viên) |
| Tab Ảnh nhân sự | Trong form hồ sơ nhân sự |
| Tab Giấy tờ liên quan / Quyết định bổ nhiệm | Trong form hồ sơ nhân sự |

> FE component/route cụ thể chưa trace session này — bổ sung khi đụng FE.

### Endpoint/Controller — `HumanController` (service HumanResource)
Registry: `registry/nhan-su/HumanController.md` · route `api/Human`. Tất cả endpoint `[SBAuthorize]` với vai `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, DV-QLDV`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Lấy hồ sơ theo Id (User) | GET `GetHumanById/{id}` | `Human_GetHumanById` |
| Thêm thông tin HR | POST `ThemMoi` | `Human_ThemMoi` |
| Sửa HR + đồng bộ giấy tờ | POST `CapNhat` | `Human_CapNhat` |
| Lấy ảnh nhân sự | POST `GetImage` | `Human_GetImage` |
| Thêm ảnh | POST `UploadImage` | `Human_UploadImage` |
| Xóa ảnh | POST `XoaImage/{id}` | `Human_XoaImage` |
| Thêm loại giấy tờ | POST `ThemLoaiGiayTo` | `Human_XoaImage` ⚠️ (dùng nhầm key) |
| Xóa file giấy tờ | POST `XoaFileGiayTo/{id}` | `Human_XoaImage` ⚠️ (dùng nhầm key) |

> `GetAllAsync` (lưới toàn bộ nhân sự HR) đã **bị comment** trong controller — không expose; muốn lưới danh sách nhân sự HR riêng → cần product xác nhận + mở lại endpoint.

### Entities (HR)
- `NhanSu` — field nghiệp vụ HR (BiDanh, Facebook, NgayVaoCTy, NgayLenChinhThuc, NgayNghiViec, NguonTuyenDung, QueQuan, `IdUser`). Map 1-1 với `User` core (nhân thân chỉ đọc để hiển thị, không lưu lại ở NhanSu).
- `FileNhanSu` — giấy tờ (`UserType=1`: Giấy tờ liên quan / Quyết định bổ nhiệm); `IdUser` ở đây trỏ tới `NhanSu.Id` (xem lưu ý đặt tên bên dưới).
- `HinhAnh` — ảnh nhân sự (`IdNhanSu`, `IdHinhAnh`, `TenAnh`).

### Luồng code (HR)
1. **Mở hồ sơ** (`NhanSuService.TimTheoIdAsync`, dòng 65-153): lấy User theo `id`; nếu chưa có NhanSu (`IdUser==id`) → tạo mới NhanSu rỗng trong transaction rồi map; trả `NhanSuDto` ghép nhân thân (User) + HR (NhanSu) + `FileNhanSu` (UserType=1, OrderBy Type).
2. **Thêm/Sửa** (`ThemMoiAsync` 177-220 / `CapNhapAsync` 221-311): `ValidateRequest` (387-400) chỉ kiểm tra quan hệ ngày, KHÔNG check HoTen. `CapNhapAsync` đồng bộ `giayToDtos` (insert nếu Id null / update nếu có Id / xóa phần dư) trong cùng transaction với update NhanSu.
3. **Ảnh** (`GetImage` 414-433 / `UploadImage` 434-471 / `DeleteImage` 472-502): thuần EF theo `IdNhanSu`/`Id`.
4. **Giấy tờ rời** (`ThemLoaiGiayToAsync` 506-551 / `XoaFileGiayToAsync` 554-587): insert/soft-delete 1 `FileNhanSu`.

### Bug-prone / lưu ý (HR — chỉ ghi nhận, KHÔNG sửa)
- **GET có side-effect ghi DB**: `HumanController.cs:30-34` (`GetHumanById`) gọi `NhanSuService.TimTheoIdAsync` (`NhanSuService.cs:75-114`) — khi User chưa có NhanSu thì **tự insert NhanSu mới** ngay trong HTTP GET (đọc→ghi). Vi phạm nguyên tắc GET không đổi state; có thể sinh bản ghi rỗng khi chỉ xem.
- **3 endpoint dùng nhầm 1 key quyền**: `HumanController.cs:64,73,81` — `XoaImage`, `ThemLoaiGiayTo`, `XoaFileGiayTo` đều khai `Human_XoaImage` (cả mô tả "Xóa ảnh nhân sự"). Thêm/xóa giấy tờ không có key riêng → không gate tách được, audit quyền sai nghĩa.
- **`ValidateRequest` cố ý bỏ check HoTen**: `NhanSuService.cs:387-400` — đúng thiết kế (HoTen ở User, form HR không gửi), nhưng dev dễ tưởng thiếu validate. Có comment giải thích tại chỗ.
- **Đặt tên cột nhập nhằng `IdUser`**: trên `FileNhanSu`, `IdUser` thực chất trỏ tới `NhanSu.Id` (`TimTheoIdAsync` lọc `x.IdUser == data.Id`, `data` là NhanSu), KHÔNG phải `User.Id`. Dễ nhầm khi join — lưu ý khi viết query giấy tờ.
- **`CapNhat` xóa ngầm giấy tờ không có trong request**: `NhanSuService.cs:286-292` — bất kỳ `FileNhanSu` (UserType=1) không xuất hiện trong `giayToDtos` đều bị `DeleteAsync`. FE phải luôn gửi đủ danh sách giấy tờ muốn giữ, nếu không sẽ mất dữ liệu.

### File source (path:line — HR)
- `HumanResource/HumanResource.API/Controllers/Base/HumanController.cs:29-86` (8 endpoint; key trùng dòng 64,73,81).
- `HumanResource/HumanResource.Service/Generated/NhanSuService.cs:65-153` (GET side-effect tạo NhanSu), `:221-311` (CapNhat đồng bộ giấy tờ + xóa phần dư), `:387-400` (ValidateRequest bỏ HoTen), `:506-587` (Thêm/Xóa giấy tờ rời).
- Registry node: `projects/scrm/registry/nhan-su/HumanController.md`.
