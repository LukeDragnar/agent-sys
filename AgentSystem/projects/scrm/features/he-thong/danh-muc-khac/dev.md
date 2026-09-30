# Danh mục khác (gốc — cấp hệ thống) — Kỹ thuật

> Nguồn đặt lịch → service **Booking** (area SandBox, bản _SA). Ngành nghề / Lĩnh vực / Nhóm ngành nghề → service **Core** (QuanTriArea/Controllers). Chỉ dành cho dev.
> Registry: `projects/scrm/registry/cua-hang/NguonDatLichController_SA.md`, `projects/scrm/registry/he-thong/NganhNgheController.md`, `LinhVucController.md`, `NhomNganhNgheController.md`.

## Endpoint / Controller (trace registry)

**Nguồn đặt lịch (hệ thống)** — `Booking.API/Areas/SandBoxArea/NguonDatLichController.cs`, route `api/SanboxArea/NguonDatLich`, `TI_HeThong`, role **HT-AD**. Dùng chung service `NguonDatLichService` (vùng `#region Dành cho SUPER_ADMIN`):
| Hành động | HTTP | Endpoint | Quyền (key) |
|---|---|---|---|
| Tìm theo điều kiện | POST | `.../TimTheoDieuKien_SA` | `NguonDatLich_TimTheoDieuKien_SA` |
| Thêm mới | POST | `.../ThemMoi_SA` | `NguonDatLich_ThemMoi_SA` |
| Lấy theo Id | GET | `.../TimTheoId/{id}` | `NguonDatLich_TimTheoId` |
| Cập nhật | POST | `.../CapNhat_SA` | `NguonDatLich_CapNhat_SA` |
| Xóa | **GET** | `.../Xoa_SA/{id}` | `NguonDatLich_Xoa_SA` |
| Đặt mặc định | GET | `.../SetMacDinh_SA/{id}` | `NguonDatLich_SetMacDinh_SA` |

**Ngành nghề** — `Core .../Areas/SanboxArea/NganhNgheController.cs`, route `api/QuanTriArea/NganhNghe`, `TI_HeThong`, role **HT-AD**: `TimTheoDieuKien`, `TimTheoId/{id}`, `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` (key `NganhNghe_*`) + `LayTatCaNganhNghe` (`[PrivateSBAuthorize]`).

**Lĩnh vực** — `Core .../Areas/SanboxArea/LinhVucController.cs`, route `api/QuanTriArea/LinhVuc`, `TI_HeThong`: CRUD (`ThemMoi`/`CapNhatThongTin`/`Xoa`/`LayTatCa`, key `LinhVuc_*`, HT-AD); `TimTheoId/{id}` + `TimTheoDieuKien` chỉ login; `GetTienIchTheoLinhVuc/{id}` (key `LinhVuc_GetTienIchTheoNganhNghe`); `CauHinhTienIchTheoLinhVuc` (key `LinhVuc_CauHinhNganhNgheTienIch`). Entities: `LinhVuc`, `LinhVucTienIch`.

**Nhóm ngành nghề** — `Core .../NhomNganhNgheController.cs`, route `api/NhomNganhNghe`: `TimTheoDieuKien` + CRUD kế thừa từ `SBBaseController` — **KHÔNG gắn permission key** (mở).

## Entities
- `NguonDatLich` (IdDonVi=Guid.Empty cho bản hệ thống) · `NganhNghe` · `LinhVuc` + `LinhVucTienIch` (map lĩnh vực ↔ tiện ích) · `NhomNganhNghe`.

## Luồng code
- Nguồn đặt lịch: `BaseService<NguonDatLich>` (EF SqlSugar). Bản _SA dùng chung service với bản _DV; phạm vi hệ thống lọc `IdDonVi == Guid.Empty`.
- Lĩnh vực: `CauHinhTienIchTheoLinhVuc` → gán bộ TienIch cho lĩnh vực → gating tính năng cho đơn vị thuộc lĩnh vực.

## Bug-prone / lưu ý (đã ghi registry — KHÔNG sửa code)
- **NguonDatLich `TimTheoId` (SA) không guard phạm vi** (`Controller:53-56`): trả thẳng `GetByIdAsync(id)`, không check `IdDonVi==Guid.Empty` → HT-AD đọc được nguồn của bất kỳ đơn vị. Dùng chung key `NguonDatLich_TimTheoId` cho cả 2 area.
- **NguonDatLich `ThemMoi_SA` side-effect trước validate trùng** (`Service:84-97`): nhánh `MacDinh==true` bỏ mặc định bản cũ TRƯỚC khi check trùng tên → trùng tên trả lỗi nhưng mặc-định cũ đã bị set false (data lệch, không rollback). Bản `_DV` (`:300`) đã đảo thứ tự đúng.
- **NguonDatLich `CapNhat_SA` không verify chủ sở hữu** (`Service:48`): update theo item client, chỉ chống trùng tên, không khẳng định `item.Id` thật sự là bản hệ thống.
- **NguonDatLich Xóa dùng HTTP GET** (`Controller:73`).
- **NguonDatLich `SetMacDinh_SA` message sai ngữ cảnh** ("Không tìm thấy tài khoản!", `Service:149`).
- **LinhVuc — quyền lỏng ở endpoint đọc** (`LinhVucController.cs:31`, `:117`): `TimTheoId`/`TimTheoDieuKien` chỉ login (permission gốc `ChiNhanh_TimTheoId` bị comment), khác phần CRUD HT-AD. Xóa lĩnh vực không kiểm tra ràng buộc đơn vị → mồ côi.
- **LinhVuc — danh xưng lẫn lộn** "LinhVuc" vs "NganhNghe" (route `GetTienIchTheoLinhVuc` ↔ method `GetTienIchTheoNganhNghe` ↔ key `LinhVuc_GetTienIchTheoNganhNghe`) + XML doc copy từ ChiNhanh ("Tạo chi nhánh mới") → dễ nhầm khi tra cứu.
- **NhomNganhNghe — không phân quyền**: controller-level không `[SBAuthorize]`/`[TienIch]`; CRUD kế thừa không gắn key → mọi request gọi được (khe hở bảo mật).

## File source (path:line)
- BE NguonDatLich (SA): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Booking\Booking.API\Areas\SandBoxArea\NguonDatLichController.cs:53-56`/`:73`; service `Booking\Booking.Service\Generated\NguonDatLichService.cs:47-184` (region SUPER_ADMIN), `:84-97`, `:48`, `:149`.
- BE NganhNghe: `...\Core\...\Areas\SanboxArea\NganhNgheController.cs` (route `api/QuanTriArea/NganhNghe`).
- BE LinhVuc: `...\Core\...\Areas\SanboxArea\LinhVucController.cs:13` (TienIch), `:31`/`:117` (quyền lỏng), `:26-43` (danh xưng lẫn).
- BE NhomNganhNghe: `...\Core\...\Controllers\NhomNganhNgheController.cs` (route `api/NhomNganhNghe`, không key).
