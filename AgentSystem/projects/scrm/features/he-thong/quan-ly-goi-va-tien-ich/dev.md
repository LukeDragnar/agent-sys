# Dev — Quản lý gói & tiện ích

> Service: **Core** (BE .NET). GoiSuDung ở QuanTriArea. TienIch có 2 class trùng tên (admin CRUD `Areas/SanboxArea` route `api/QuanTriArea/TienIch`; đơn vị/chợ `Controllers/` route `api/TienIch`). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Gói sử dụng + duyệt nâng cấp | Quản trị (QuanTriArea) | CRUD + duyệt |
| Quản lý tiện ích (danh mục) | Hệ thống (QuanTriArea) | CRUD danh mục |
| Mua tiện ích / Chợ tiện ích | Đơn vị | mua/đã mua/lịch sử/xóa |

> Route FE cụ thể: cần product/dev xác nhận trong `scrm_frontend` (chưa trace FE trong session này).

## Endpoint / Controller (trace registry)
**GoiSuDungController** — `registry/he-thong/GoiSuDungController.md` · route `api/QuanTriArea/GoiSuDung` (SBBaseController):
| MaTinhNang | Route | Quyền / Gating |
|---|---|---|
| `GoiSuDung_ThemMoiGoiSd` / `_CapNhatGoiSd` | POST `.../ThemMoiGoiSd`, `/CapNhatGoiSd` | CRUD nền chỉ `[SBAuthorize]` |
| `GoiSuDung_TimTheoDieuKien` | POST `.../TimTheoDieuKien` | HT-AD/HT-KT |
| `GoiSuDung_Xoa` | POST `.../Xoa/{id}` | override XoaAsync (check tồn tại) |
| `GoiSuDung_DanhSachNangCap` | POST `.../DanhSachNangCap` | DV-AD-TONG/HT-KT |
| `GoiSuDung_DuyetYeuCauNangCap` | POST `.../DuyetYeuCauNangCap` | gating code: super/KT-HT/AD-HT |
| (no key) `HuyDuyetNangCap` | GET `.../HuyDuyetNangCap?idNangCap=` | gating code: super/KT-HT/AD-HT |
| `GoiSuDung_GoiPheDuyetInfo` | POST `.../GoiPheDuyetInfo/{id}` | DV-AD-TONG/HT-KT |

**TienIchController (A — admin CRUD)** — `registry/he-thong/TienIchController.md` · route `api/QuanTriArea/TienIch`:
| MaTinhNang | Route | Quyền |
|---|---|---|
| `TienIch_TimTheoDieuKien` / `_LayTatCa` / `_TimTheoId` | POST `.../TimTheoDieuKien`, GET `.../LayTatCa`, `.../TimTheoId/{id}` | HT-AD/HT-KT (+HT-MB cho LayTatCa) |
| `TienIch_ThemMoi` / `_CapNhatThongTin` / `_Xoa` | POST `.../ThemMoi`, `/CapNhatThongTin`, `/Xoa/{id}` | HT-AD; Xoa check `CheckTienIchDaSuDung` |

**TienIchController (B — đơn vị / chợ)** — cùng node · route `api/TienIch`:
| MaTinhNang | Route | Quyền |
|---|---|---|
| `TienIch_GetDanhSachTienIch` | POST `.../GetDanhSachTienIchFilter` | DV-AD-TONG/HT-KT |
| `TienIch_GetDanhSachTienIchDaMua` | GET `.../GetDanhSachTienIchDaMua/{IsConHan}` | DV-AD-TONG/CN/MCN, DV-QLDV, HT-KT |
| `TienIch_MuaNgay` | POST `.../MuaNgay` | DV-AD-TONG/HT-KT |
| (login) `LichSuGiaoDich` | POST `.../LichSuGiaoDich` | login |
| `TienIch_XoaTiDaDangKy` | GET `.../XoaTiDaDangKy/{id}` | DV-AD-TONG (HttpGet ⚠) |
| (internal `[PrivateSBAuthorize]`) | GET `.../GetDanhSachTienIchDaMua/{idDonVi}/{IsConHan}` | service-to-service |

## Entities
- `GoiSuDung` (gói), `NangCapGoiSuDung` (yêu cầu nâng cấp; state {ChoNangCap, DaDuyet, HuyDuyet}). DTO: `GoiSuDungDto`, `GoiSuDungFilterDto`, `NangCapGoiSuDungFilterDto`, `RequestDuyetYeuCap`.
- `TienIch` (danh mục), `HopDongTienIch` (hợp đồng đơn vị mua). DTO: `TienIchDto`, `TienIchFilterDto`, `TienIchFilterNhomDto`, `MuaNgayTienIchDto`.

## Luồng code
- `GoiSuDungController`: `IGoiSuDungService` (gói) + `INangCapGoiSuDungService` (nâng cấp). Duyệt/Hủy gate `CurrentUser.IsSuperUser || IsHeThongKeToan() || IsAdminHeThong()` → else `ResultWithError("Bạn không có quyền...")`.
- `TienIchController`: `ITienIchService` (EF + `CheckTienIchDaSuDung`) + `IHopDongTienIchService` (LichSuGiaoDich). `MuaNgay` → tạo `HopDongTienIch` → kích hoạt `[TienIch]` cho đơn vị.

## Bug-prone / lưu ý
- **`GoiSuDungController.cs:97-99` `HuyDuyetNangCap` chỉ `[SBAuthorize()]`** (permission key bị comment), gate quyền hoàn toàn bằng `if` trong code → audit attribute không thấy quyền, dễ tưởng "ai cũng gọi được".
- **Kiểm tra quyền duyệt/hủy lặp 2 lần** cùng pattern (DuyetYeuCauNangCap, HuyDuyetNangCap) → nên gom helper.
- **2 class `TienIchController` cùng tên** ở 2 namespace (`api/QuanTriArea/TienIch` vs `api/TienIch`) → grep theo tên class dễ nhầm; phân biệt bằng route.
- **`XoaTiDaDangKy` dùng `[HttpGet]`** cho thao tác xóa nghiệp vụ → không idempotent, dễ bị gọi nhầm (crawler/prefetch). Để session security xem xét.
- **Xóa danh mục tiện ích** bị chặn nếu `CheckTienIchDaSuDung == true` (`Area/TienIchController.cs:123`) — đây là bảo vệ đúng, không phải bug.

## File source (path:line)
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\...` — `GoiSuDungController.cs:97-99` (HuyDuyetNangCap no-key); `Areas\SanboxArea\TienIchController.cs:123` (Xoa check da su dung); `Controllers\TienIchController.cs:43` (GetDanhSachTienIchDaMua IsConHan).
- Registry: `projects/scrm/registry/he-thong/GoiSuDungController.md`, `TienIchController.md`, `HopDongTienIchController.md` (liên quan).
