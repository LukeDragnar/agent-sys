# Danh mục hành chính (địa giới — cấp hệ thống) — Kỹ thuật

> Service: **Core** (BE .NET). 4 controller cây địa giới kế thừa `SBBaseController<TEntity, TDto>`. Tiện ích `TI_HeThong`. Chỉ dành cho dev.
> Registry: `projects/scrm/registry/he-thong/QuocGiaController.md`, `TinhTPController.md`, `QuanHuyenController.md`, `PhuongXaController.md`.

## Màn hình & route (FE)
Khu Hệ thống > Danh mục địa giới: Quốc gia / Tỉnh-TP / Quận-Huyện / Phường-Xã (lưới + Thêm/Sửa/Xóa; dropdown cascade).

## Endpoint / Controller (trace registry)

**QuocGiaController** (`api/QuocGia`, entity `DMQuocGia`) — auth mixed; key HT-AD/HT-CSKH:
| Hành động | Endpoint | Quyền |
|---|---|---|
| Tìm theo điều kiện | `POST api/QuocGia/TimTheoDieuKien` | `QuocGia_TimTheoDieuKien` |
| Thêm mới | `POST .../ThemMoi` | `QuocGia_ThemMoi` |
| Cập nhật | `POST .../CapNhatThongTin` | `QuocGia_CapNhatThongTin` |
| Xóa | `POST .../Xoa/{id:Guid}` | `QuocGia_Xoa` |
| Lấy thông tin địa chỉ | `POST .../GetThongTinDiaChi` | (login) |
| [private] cây địa chỉ | `POST .../GetThongTinDiaChi_Private` | `[PrivateSBAuthorize]` |

**TinhTPController** (`api/TinhTP`, entity `DMTinhThanh`, FK `IdQuocGia`): `TimTheoDieuKien`, `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}`, `GetTinhByQuocGia/{id}` (GET, dropdown) — key `TinhTP_*` (HT-AD/HT-CSKH).

**QuanHuyenController** (`api/QuanHuyen`, entity `DMQuanHuyen`, FK `IdTinhThanh`): `TimTheoDieuKien`, `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}`, `GetQuanHuyenByTinh/{id}` (GET) — key `QuanHuyen_*` (HT-AD/HT-CSKH).

**PhuongXaController** (`api/PhuongXa`, entity `DMPhuongXa`, FK `IdTinhThanh` + `IdQuanHuyen`): `TimTheoDieuKien`, `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` — key `PhuongXa_*` (HT-AD/HT-CSKH).

Các endpoint đọc kế thừa từ base (`TimTheoId/{id}`, `LayTatCa`) chỉ còn `[SBAuthorize]` rỗng (login).

## Entities
- `DMQuocGia` (gốc cây) · `DMTinhThanh` (FK IdQuocGia) · `DMQuanHuyen` (FK IdTinhThanh) · `DMPhuongXa` (FK IdTinhThanh + IdQuanHuyen). Mọi entity có địa chỉ (KH/đơn vị/đơn hàng) tham chiếu xuống cây này.

## Luồng code
- CRUD kế thừa `SBBaseController` (override `ThemMoi`/`CapNhatThongTin`/`XoaAsync` để gắn key + validate).
- ThemMoi: validate cấp cha bắt buộc + Mã/Tên không trống + `GetCodeByRequest(...)` check trùng theo scope cấp cha (chỉ khi `Id == null`).
- CapNhatThongTin: KHÔNG gọi `ValidateRequest`/check trùng → drift so với ThemMoi.

## Bug-prone / lưu ý (đã ghi trong registry — KHÔNG sửa code)
- **`QuocGiaController.cs:88`** `ValidateRequest` viết `IsNullOrWhiteSpace(request.Ma) && IsNullOrWhiteSpace(request.Ma)` — **lặp `Ma`, bỏ sót `Ten`** → tạo được Quốc gia chỉ có Tên (thiếu Mã) / thiếu Tên không bị bắt như mong đợi.
- **`QuocGiaController.cs:16`** controller-level `[SBAuthorize("QuocGiaController")]` **bị comment** → bảo vệ dựa hoàn toàn vào key từng method; `LayTatCa`/`TimTheoId` chỉ còn `[SBAuthorize]` rỗng.
- **`TinhTPController.cs:74-91`** `CapNhatThongTin` không check trùng → sửa Tỉnh có thể trùng Mã/Tên với Tỉnh khác cùng Quốc gia.
- **`QuanHuyenController.cs:15`** controller-level `[SBAuthorize]` bị comment; `CapNhatThongTin` không re-validate trùng.
- **`PhuongXaController.cs:99-100`** check trùng chỉ khi `Id == null` (thêm); update KHÔNG check trùng.
- **Xóa mọi cấp** chỉ kiểm tra tồn tại, **không** chặn khi còn cấp con/đối tượng tham chiếu → phụ thuộc FK ở DB; dễ để lại dữ liệu mồ côi.

## File source (path:line)
- `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\Core\...\Controllers\QuocGiaController.cs:16` (SBAuthorize comment), `:88` (validate lỗi).
- `...\TinhTPController.cs:74-91` (CapNhat không validate).
- `...\QuanHuyenController.cs:15` (SBAuthorize comment), `:60`/`:99-110` (validate ThemMoi).
- `...\PhuongXaController.cs:99-100` (check trùng chỉ khi thêm).
- (Đường dẫn chính xác file Core controller: tra registry node tương ứng; service Core.)
