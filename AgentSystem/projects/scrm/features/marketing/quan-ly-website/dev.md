# Dev — Quản lý Website

> Service: **Website** (BE .NET) — controllers `Website.API/Controllers/Api/`, base `BaseCoreController`, `partial class`, gate `TI_Website`. Gom 5 controller: TinTuc, Banner, Domain, Theme, ChuyenGia. Storefront render qua endpoint `[PrivateSBAuthorize]`. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Mục đích |
|---|---|
| Quản lý tin tức | CRUD bài tin theo nhóm |
| Quản lý banner | CRUD banner theo vị trí/thời gian |
| Quản lý domain | CRUD tên miền + gán link website |
| Theme (gán) | tìm/gán theme cho link website |
| Quản lý chuyên gia | CRUD chuyên gia hiển thị web |

## Endpoint / Controller (trace registry)

**TinTucController** (`api/TinTuc`):
| Endpoint | Quyền (key) | Ghi chú |
|---|---|---|
| POST `TimTheoDieuKien` | `TinTuc_TimTheoDieuKien` | list, **loại DV-NV** |
| POST `LayTheoNhomTinTuc` | `TinTuc_LayTheoNhomTinTuc` | theo list id nhóm |
| POST `ThemMoi` / `CapNhatThongTin` | `TinTuc_ThemMoi` / `_CapNhatThongTin` | scope DV-AD-TONG |
| GET `TimTheoId/{id}` / `TimTinTucTheoId/{id}` | `TinTuc_TimTheoId` / `_TimTinTucTheoId` | 2 biến thể (DTO khác) |
| POST `Xoa/{id}` | `TinTuc_Xoa` | scope DV-AD-TONG |
| POST `GetListTinTucByIds` / GET `GetDsTinTucTheoDonVi` | `TinTuc_*` | bulk / theo đơn vị |
| POST `PrivateGetTinTucByIds` / `LayTatCa_Private` | `[PrivateSBAuthorize]` | storefront render |

**BannerController** (`api/Banner`, gate `TI_Website`):
| Endpoint | Quyền (key) |
|---|---|
| POST `TimTheoDieuKien` | `Banner_TimTheoDieuKien` |
| POST `ThemMoi` | `Banner_ThemMoi` |
| GET `TimTheoId/{id}` + POST `CapNhatThongTin` | `Banner_TimTheoId` / `_CapNhatThongTin` |
| POST `Xoa/{id}` | `Banner_Xoa` |

**DomainController** (`api/Domain`):
| Endpoint | Quyền (key) |
|---|---|
| POST `TimTheoDieuKien` | `Domain_TimTheoDieuKien` |
| POST `GetDomainToSetLinkWebsite` | `Domain_GetDomainToSetLinkWebsite` (mở rộng DV-AD/KT/QLDV/NV) |
| POST `ThemMoi` | `Domain_ThemMoi` |
| GET `TimTheoId/{id}` + POST `CapNhatThongTin` | `Domain_TimTheoId` / `_CapNhatThongTin` |
| POST `Xoa/{id}` | `Domain_Xoa` |

**ThemeController** (`api/Theme`) — chỉ còn 2 action đọc:
| Endpoint | Quyền (key) |
|---|---|
| POST `TimTheoDieuKien` | `Theme_TimTheoDieuKien` (scope HT-AD) |
| POST `GetThemeToSetLinkWebsite` | `Theme_GetThemeToSetLinkWebsite` (HT-AD + DV-AD-TONG/CN/MCN/KT/QLDV/NV) |
| ~~ThemMoi/TimTheoId/CapNhat/Xoa~~ | **đã comment** → bản superadmin `Areas/SanboxArea/ThemeController.cs`, route `api/QuanTriArea/Theme` |

**ChuyenGiaController (Website)** (`api/ChuyenGia`) — ⚠️ trùng tên với bản he-thong:
| Endpoint | Quyền (key) |
|---|---|
| POST `TimTheoDieuKien` | `ChuyenGia_TimTheoDieuKien` (set IdDonVi = CurrentUser.DonViId) |
| GET `TimTheoId/{id}` | `ChuyenGia_TimTheoId` |
| POST `ThemMoi` / `CapNhatThongTin` | `ChuyenGia_ThemMoi` / `_CapNhatThongTin` |
| POST `Xoa/{id}` | `ChuyenGia_Xoa` |
| `TimTheoDieuKienPrivate` / `TimTheoIdPrivate/{id}` | `[PrivateSBAuthorize]` (storefront) |

## Entities
- **`TinTuc`**, **`DMNhomTinTuc`** (nhóm tin).
- **`Banner`** (Ten, ViTri `EnumViTriBanner`, NgayBatDau/KetThuc, Image/ImageMobile, Link, KieuMoLink, Stt, IsSuDung, IdDonVi, IdChiNhanh).
- **`Domain`** (Ten, IsSuDung, IdDonVi).
- **`Theme`** (`ThemeCode` dựng path view `~/Views/Themes/{ThemeCode}/...`).
- **`ChuyenGia`** (lọc theo IdDonVi).

## Bug-prone / lưu ý (path:line)
- **Domain check trùng tên không lọc đơn vị** — `DomainService.cs:91`: `AnyAsync(x => x.Ten == model.Ten)` toàn bảng → 2 đơn vị không tạo cùng tên. Có thể đúng chủ ý (tên miền duy nhất toàn cục) nhưng khác cách cô lập đơn vị ở update/delete — **cần product xác nhận**.
- **Domain CapNhat chỉ map Ten + IsSuDung** — `DomainService.cs:130-141`: field liên hệ (NguoiLienHe/Sdt/DiaChi/Email) comment cả entity update lẫn validate → UI gửi cũng không lưu.
- **GetDomainToSetLinkWebsite không phân trang** — `DomainService.cs:74` (skip/take comment) → trả toàn bộ, dropdown nặng với đơn vị nhiều domain.
- **Domain message copy nhầm** — `DomainController.cs:59`: "Không tìm thấy nhà cung cấp!" thay vì domain.
- **Banner message ngày ngược nghĩa** — `BannerService.cs:94-98`: chặn đúng `NgayKetThuc < NgayBatDau` nhưng message ghi "Ngày bắt đầu không được lớn hơn ngày kết thúc".
- **Banner message copy nhầm** — `BannerController.cs:48` & `BannerService.cs:135`: "Không tìm thấy nhà cung cấp!" thực chất là banner.
- **Theme CRUD comment** (L47-79) ở bản đơn vị → đi qua superadmin; `ThemeCode` là khóa dựng path view → đổi code phải đảm bảo folder `Views/Themes/{code}` tồn tại.
- **ChuyenGia Private không ép IdDonVi từ token** — `ChuyenGiaController.cs:70-74`: caller private phải tự truyền `IdDonVi`, thiếu có thể trả chéo đơn vị.
- **2 bản ChuyenGiaController** (he-thong vs website) — đừng nhầm permission key.
- **Tất cả `partial class`** — có thể còn endpoint ở file partial khác (vd `TinTuc.LayTatCaTheoNhom_Private`); method nhiều chỗ `virtual` → kiểm tra lớp con khi trace.

## File source (path:line)
- `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Website\Website.API\Controllers\Api\TinTucController.cs`, `BannerController.cs` (`:48`), `DomainController.cs` (`:59`), `ThemeController.cs` (L47-79 comment), `ChuyenGiaController.cs` (`:70-74`).
- Service: `DomainService.cs` (`:74,91,130-141`), `BannerService.cs` (`:94-98,135`).
- Bản superadmin theme: `Website.API/Areas/SanboxArea/ThemeController.cs` (route `api/QuanTriArea/Theme`).
- Registry: `projects/scrm/registry/website/{TinTuc,Banner,Domain,Theme,ChuyenGia_Website}Controller.md`.
