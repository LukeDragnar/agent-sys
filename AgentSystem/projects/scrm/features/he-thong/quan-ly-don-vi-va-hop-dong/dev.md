# Dev — Quản lý đơn vị & hợp đồng

> Service: **Core** (BE .NET). DonVi có 2 class trùng tên (bản app `Controllers/` route `api/DonVi`, bản quản trị `Areas/SanboxArea` route `api/QuanTriArea/DonVi`). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Thông tin đơn vị (của user) | Cấu hình hệ thống | bản app |
| Quản trị đơn vị | Quản trị (QuanTriArea) | CRUD + duyệt mã + gói + merchant |
| Hợp đồng | Quản trị (QuanTriArea) | CRUD |
| Hợp đồng tiện ích / Yêu cầu mua tiện ích | Quản trị (QuanTriArea) | duyệt/hủy/chi tiết/xóa |
| Đơn vị tiện ích | Quản trị (QuanTriArea) | tra cứu |

> Route FE cụ thể: cần product/dev xác nhận trong `scrm_frontend` (chưa trace FE trong session này).

## Endpoint / Controller (trace registry)
**DonViController** — `registry/he-thong/DonViController.md` (2 class):
| Hành động | Route | Quyền |
|---|---|---|
| Lấy thông tin đơn vị của user | GET `api/DonVi/GetDonViById`, `GetDonViByIdForEdit` (no-cache), `GetInfoDonViBasic/{id}`, POST `GetInfoBasicByIds`, GET `GetAllInfoBasic` | `[SBAuthorize]` |
| Đăng ký đơn vị mới | POST `api/DonVi/ThemMoi` | **`[AllowAnonymous]`** ⚠ |
| Tìm / Lấy tất cả (quản trị) | POST `api/QuanTriArea/DonVi/TimTheoDieuKien`, GET `.../LayTatca` | `DonVi_TimTheoDieuKien`/`_LayTatCa` (HT-AD/HT-KT/HT-CSKH) |
| Chi tiết (no-cache) | GET `api/QuanTriArea/DonVi/TimTheoId/{id}` | `DonVi_TimTheoId` |
| Xuất Excel | POST `api/QuanTriArea/DonVi/ExportExcel` | **chỉ `[SBAuthorize]`, không key** ⚠ |
| Thêm/Sửa/Xóa (quản trị) | POST `.../ThemMoi`, `/CapNhatThongTin`, `/Xoa/{id}` | `DonVi_ThemMoi`/`_CapNhatThongTin`/`_Xoa` |
| Gen / Duyệt mã | POST `.../GenCode`, `.../ApproveCode` | `DonVi_GenCode`/`_ApproveCode` |
| Gói + merchant | GET `GetGoiVaUserByDonVi/{id}`, POST `YeuCauThayDoiGoiSuDungDonVi`, GET `GetDonViMerchant/{id}`, POST `UpdateDonViMerchant` | key tương ứng |
| (internal `[PrivateSBAuthorize]`) | `GetDonViByListIds`, `GetDonViById/{id}`, `GetAllIdDonVi`, `GetTenMienDanhGiaByListId` | service-to-service |

**HopDongController** — `registry/he-thong/HopDongController.md` · route `api/QuanTriArea/HopDong` (SBBaseController): CRUD chuẩn (`[SBAuthorize]` mức controller, không key riêng) + `HopDong_TimTheoDieuKien` (HT-AD/HT-KT).

**DonViTienIchController** — `registry/he-thong/DonViTienIchController.md` · route `api/QuanTriArea/DonViTienIch`: `DonViTienIch_TimTheoDieuKien` (HT-AD/HT-KT).

**HopDongTienIchController** — `registry/he-thong/HopDongTienIchController.md` · route `api/QuanTriArea/HopDongTienIch`:
| MaTinhNang | Route | Quyền / State |
|---|---|---|
| `HopDongTienIch_TimTheoDieuKien` | POST `.../TimTheoDieuKien` | HT-AD/HT-KT |
| `HopDongTienIch_DuyetHopDong` | POST `.../DuyetHopDong/{ma}` | theo **mã**; chờ duyệt |
| `HopDongTienIch_HuyDuyetHopDong` | POST `.../HuyDuyetHopDong/{id}` | đã duyệt |
| `HopDongTienIch_ChiTietHopDong` | POST `.../ChiTietHopDong/{id}` | — |
| `HopDongTienIch_Xoa` | POST `.../Xoa/{id}` | check tồn tại |

## Entities
- `DonVi` (tenant) — DTO/Req: `DonViCreateReq`, `DonViFilterReq`, `DonViGenCodeReq`, `DonViCodeApproveReq`, `DonViSearchDto`, `YeuCauDoiGoiSuDungDonViDto`, `UpdateDonViMerchant`, `DonViViewBasicDto`.
- `HopDong` — `HopDongDto`, `HopDongFilterDto`.
- `DonViTienIch` — `DonViTienIchPayload`.
- `HopDongTienIch` — `HopDongTienIchFilterDto`; state {ChoDuyet, DaDuyet, HuyDuyet}.

## Luồng code
- `DonViController`: `IDonViService` (CRUD/tra cứu) + `IDonViKhoiTaoServiceService` (ThemMoi luôn đi qua để khởi tạo tenant).
- `HopDongController`: `IHopDongService` + `IBaseService<HopDong>` (CRUD nền).
- `DonViTienIchController`: `IDonViTienIchService`.
- `HopDongTienIchController`: `IHopDongTienIchService`; Duyệt theo `{ma}`, các thao tác khác theo `{id}`.

## Bug-prone / lưu ý
- **`Controllers/DonViController.cs:104-109` ThemMoi `[AllowAnonymous]`** → đăng ký đơn vị không cần auth → rủi ro đăng ký rác (để session security xử lý).
- **`Areas/SanboxArea/DonViController.cs:64-65` ExportExcel bỏ permission key** (chỉ `[SBAuthorize]`) → bất kỳ user login nào gọi endpoint cũng xuất được.
- **Bản quản trị còn endpoint comment-out** (`DisableMultiRole`) — không active.
- **HopDongTienIch Duyệt nhận `{ma}` (string) còn Hủy/Chi tiết/Xóa nhận `{id}` (Guid)** → không đồng nhất, dễ truyền nhầm.
- **`HopDongController.cs:32` chuỗi role `[SBAuthorize]` ghi `"(HT-AD, HT-KT)"`** (kèm ngoặc thừa) → có thể lệch khi parse role.
- **`DonViTienIchController.cs:21` text mô tả `[SBAuthorize]` ghi nhầm "Tìm kiếm chi nhánh"** (copy-paste, không ảnh hưởng hành vi).

## File source (path:line)
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\...` — `Controllers\DonViController.cs:104-109` (ThemMoi anonymous); `Areas\SanboxArea\DonViController.cs:64-65` (ExportExcel no-key); `HopDongController.cs:32`; `DonViTienIchController.cs:21`.
- Registry: `projects/scrm/registry/he-thong/DonViController.md`, `HopDongController.md`, `DonViTienIchController.md`, `HopDongTienIchController.md`.
