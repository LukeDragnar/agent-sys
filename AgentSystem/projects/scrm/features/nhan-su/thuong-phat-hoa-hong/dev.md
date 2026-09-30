# Dev — Thưởng phạt & hoa hồng nhân viên

> Service: **Branch** (BE .NET). Chỉ dành cho dev.

## Màn hình & route (UI)
| Màn hình | Vị trí UI |
|---|---|
| Thưởng phạt nhân viên | Nhân sự > Hồ sơ nhân viên > tab Thưởng phạt |
| Danh mục thưởng phạt | Cấu hình > Thưởng phạt > Danh mục thưởng phạt |
| Hoa hồng nhân viên | Cấu hình > Hoa hồng nhân viên (nhiều tab: theo đối tượng-NV / tối đa / mặc định / theo nhóm) |
| Thưởng doanh số | Cấu hình > Thưởng doanh số (panel NV/dịch vụ/SP/thẻ/combo + bảng bậc) |

> FE component/route cụ thể chưa trace session này — bổ sung khi đụng FE.

## Endpoint/Controller (trace registry)
**Thưởng phạt — `ThuongPhatNhanVienController`** — registry: `registry/nhan-su/ThuongPhatNhanVienController.md`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tìm theo điều kiện | POST `TimTheoDieuKien` | `ThuongPhatNhanVien_TimTheoDieuKien` |
| Thưởng phạt theo NV | GET `GetThuongPhatByNhanVien?idNhanVien` | `..._GetThuongPhatByNhanVien` |
| Tổng theo NV | GET `GetSumThuongPhatByNhanVien?idNhanVien` | `..._GetSumThuongPhatByNhanVien` |
| Thêm khoản | POST `ThemMoiThuongPhatNv` | `..._ThemMoiThuongPhatNv` |
| Xóa | POST `XoaTheoId/{id}` | `..._XoaTheoId` |
- Kế thừa `SBBaseController` → có sẵn 5 endpoint base login-only **không key** (`TimTheoId/LayTatCa/ThemMoi/CapNhatThongTin/Xoa`) — nghiệp vụ thật dùng action có key ở trên, tránh dùng base để ghi.

**Danh mục — `DMThuongPhatController`** — registry: `registry/nhan-su/DMThuongPhatController.md`:
- `TimTheoDieuKien`, `TimTheoId/{id}`, `LayTatCaThuongTheoDonVi` (đọc; mở KT/QLDV) · `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` (ghi; **AD-TONG**).

**Hoa hồng — `HoaHongNhanVienController`** (379 dòng, gom 4 service) — registry: `registry/nhan-su/HoaHongNhanVienController.md`:
- Lưu (AD/QLDV, bỏ KT): `LuuHoaHongNhanVien`, `LuuHHNhanVienToiDa`, `LuuHHDoiTuongMacDinh`, `LuuHHDoiTuongMacDinhTheoNhomHoaHong`, `LuuHHDoiTuongTheoNhomHoaHong`.
- Đọc: `LayDS*` / `Lay*` tương ứng + `GetListHoaHongLichSuByNhanVien` + `LayDanhSachNhanVienChuaCoHHDoiTuong`.
- `TaoHHDoiTuongNhanVienVoiGiaTriMacDinh` (áp mặc định cho NV chưa có).
- Xóa: `XoaHH/{id}` (GET), `XoaHoaHongThuocTinhMoRong` (GET, AD-TONG).
- Enum (anonymous, không attribute): `GetListEnumGiaTriDoiTuongTinhHoaHong`, `GetListEnumThoiDiemTinhHoaHong`, `GetListEnumLuaChonChiaDeu`.
- Internal (Order): `XepNhanVien_LayHoaHongNhanVienCauHinh` (POST `[PrivateSBAuthorize]` body `List<DonHangChiTietModel>`; + overload GET login-only), `GetHoaHongApDungByListMaDon` (POST `[PrivateSBAuthorize]`).

**Thưởng doanh số — `ThuongDoanhSoController`** — registry: `registry/nhan-su/ThuongDoanhSoController.md`:
- `ThemMoiAsync` (`ThuongDoanhSo_ThemMoiAsync`, replace-all + transaction), `GetListThuongDoanhSo` (`ThuongDoanhSo_GetListThuongDoanhSo`).
- Kế thừa `SBBaseController` → base CRUD login-only không key.

## Entities
- `ThuongPhatNhanVien` (gắn IdNhanVien, loại = `DMThuongPhat`, số tiền, thưởng/phạt/khấu trừ), `DMThuongPhat`.
- `HoaHongNhanVien` (HH theo NV/đối tượng, **IdNhanVien = Guid.Empty = mặc định**), `CaiDatHoaHongMacDinh` (HH tối đa), `NhomHoaHong`.
- `ThuongDoanhSo` (1 cấu hình/chi nhánh) + `ThuongDoanhSoChiTiet` (panel NV/đối tượng + bậc PointItems).

## Luồng code
1. **Hoa hồng tính cho đơn** (`HoaHongNhanVienLogicService`): gating quyền (QLDV/Admin → tất cả NV; còn lại chỉ chính mình) → nạp HH các NV liên quan trong (IdDonVi, IdChiNhanh), IdLoaiObject ∈ lstIdObj, IdNhanVien == Guid.Empty HOẶC ∈ lstIdNhanVien → map NapThe→The → chọn cấu hình (ưu tiên theo NV, fallback mặc định).
2. **Thưởng doanh số lưu** (`ThuongDoanhSoService.InsertMuilpleAsync`, dòng 225): validate bậc → transaction RemoveRange toàn bộ cấu hình cũ của (IdDonVi, IdChiNhanh) → tạo mới (tách panel theo KieuLoaiObject + PointItems).
3. **Đọc thưởng doanh số** (`GetThuongDoanhSoData`): gọi Core `GetUserById` + WarehouseProduct `GetDichVuByIds/GetListSanPhamByIds/GetTheDichVuByIds/GetComboByIds` để denormalize tên.

## Bug-prone / lưu ý
- **Replace-all nguy hiểm**: `ThuongDoanhSoService.cs:269-297` — lưu = xóa hết cấu hình cũ chi nhánh rồi ghi mới. Gửi list rỗng/thiếu = xóa sạch cấu hình chi nhánh. FE phải gửi đầy đủ mỗi lần lưu.
- **`throw ex;` mất stack**: `ThuongDoanhSoService.cs:409` (anti-pattern, nên `throw;`).
- **Endpoint enum anonymous**: `HoaHongNhanVienController.cs:292,303,319` — 3 enum không `[SBAuthorize]` → truy cập anonymous (chỉ ghi nhận, session security xử lý).
- **Xóa bằng GET**: `HoaHongNhanVienController.cs:277,335` (`XoaHH`, `XoaHoaHongThuocTinhMoRong`) — không idempotent.
- **Label sai**: `HoaHongNhanVienController.cs:336` — desc của `XoaHoaHongThuocTinhMoRong` copy nhầm "Lưu hoa hồng đối tượng mặc định" (key vẫn đúng).
- **2 overload cùng tên action**: `HoaHongNhanVienController.cs:346,354` `XepNhanVien_LayHoaHongNhanVienCauHinh` phân biệt verb POST(internal)/GET(login) → grep/map route lưu ý.
- **Base endpoint không key (audit)**: `ThuongPhatNhanVienController.cs:14`, `ThuongDoanhSoController.cs:15` — SBBaseController phát sinh CRUD base login-only; soi cả base khi audit quyền. `ThuongPhatNhanVienController.cs:64` `XoaTheoId` dùng POST (convention SCRM).
- **Invariant mặc định HH**: `IdNhanVien == Guid.Empty` phân biệt mặc định vs HH riêng — không được phá khi lưu (registry quy tắc 5).
- **DMThuongPhat — comment key thừa**: `DMThuongPhatController.cs:93` để lại key cũ `DMThuongPhat_Xoa` ở action `LayTatCaThuongTheoDonVi` (đã thay key đúng dòng 94) → lưu ý khi grep key.

## File source (path:line)
- `ThuongPhatNhanVienController.cs:14,64`.
- `DMThuongPhatController.cs:23,92,93`.
- `HoaHongNhanVienController.cs:277,292,303,319,335,336,346,354`; `HoaHongNhanVienLogicService.cs` (logic tính HH).
- `ThuongDoanhSoController.cs:15`; `ThuongDoanhSoService.cs:225,269-297,409`.
- Registry nodes: `projects/scrm/registry/nhan-su/{ThuongPhatNhanVien,DMThuongPhat,HoaHongNhanVien,ThuongDoanhSo,ThuongDoanhSoChiTiet}Controller.md`.
