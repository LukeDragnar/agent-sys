# Dev — Quản lý trang & giao diện

> Service: **Core** (BE .NET). Trang web (Page) + Page Mobile có bản quản trị `Areas/SanboxArea` (route `api/QuanTriArea/...`) và bản đọc `Controllers/` (route `api/PageMobile`). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Quản lý trang (cây trang) | Quản trị (QuanTriArea) | CRUD Page web |
| Quản lý Page Mobile | Quản trị (QuanTriArea) | CRUD + import + backup |

> Route FE cụ thể: cần product/dev xác nhận trong `scrm_frontend` (chưa trace FE trong session này).

## Endpoint / Controller (trace registry)
**PageController** — `registry/he-thong/PageController.md` · route `api/QuanTriArea/Page` (chỉ tồn tại ở SanboxArea):
| MaTinhNang | Route | Quyền |
|---|---|---|
| `Page_LayTatCaTheoTree` | GET `.../LayTatCaTheoTree?type=` | HT-AD, HT-MB |
| `Page_TimTheoId` | GET `.../TimTheoId/{id}` (+ GetRoleHomeByPage) | HT-AD, HT-MB |
| `Page_ThemMoi` | POST `.../ThemMoi` → `InsertOrUpdateAsync` | HT-AD, HT-MB |
| `Page_CapNhatThongTin` | POST `.../CapNhatThongTin` → `InsertOrUpdateAsync(...,true)` | HT-AD, HT-MB |
| `Page_Xoa` | POST `.../Xoa/{id}` → DeleteAsync + xóa cache menu | HT-AD, HT-MB |

**PageMobileController (bản đọc)** — `registry/he-thong/PageMobileController.md` · route `api/PageMobile` · `[SBAuthorize]` (login):
- `TimTheoId/{id}` (key `Page_TimTheoId` đã comment → chỉ login), `LayTatCa`, `GetAllForMobileAsync`, `TimTheoDieuKien`, `GetAllKeyMobile`(+`_Encrypt`), `GetKeyByUserMobile`(+`_Encrypt`). KHÔNG có CRUD.

**PageMobileController (bản quản trị)** — `registry/he-thong/PageMobileController_Core.md` · route `api/QuanTriArea/PageMobile`:
| MaTinhNang | Route | Quyền |
|---|---|---|
| `PageMobile_TimTheoId` / `_TimTheoDieuKien` | GET `.../TimTheoId/{id}`, POST `.../TimTheoDieuKien` | HT-AD, HT-MB |
| `PageMobile_ThemMoi` / `_CapNhatThongTin` / `_Xoa` | POST `.../ThemMoi`, `/CapNhatThongTin`, `/Xoa/{id}` | HT-AD, HT-MB |
| `ThemMoiKeyMobile` (+`_Encrypt`) | POST `.../ThemMoiKeyMobile` | login |
| `PageMobile_DownloadPageMobileBackup` | GET `.../DownloadPageMobileBackup` | HT-AD, HT-MB |
| `PageMobile_DownloadFileTemplate` | GET `.../DownloadFileTemplate` | HT-AD, HT-MB |
| `PageMobile_ImportPageMobile` | POST `.../ImportPageMobile` (IFormFile) | HT-AD, HT-MB |

## Entities
- `Page` — trang web (cây cha-con) + mapping role home page.
- `PageMobile` — màn app (key) + role-by-page mapping.

## Luồng code
- **Page**: `IPageService` (EF) + `IHybridCache` (Redis) + AutoMapper. Thêm/Cập nhật chung `InsertOrUpdateAsync` (cờ isUpdate). Chỉ `Xoa` gọi `RemoveWithPatternAsync(Cache_MenuPrefix)`.
- **PageMobile**: `IPageMobileService` (EF) + `AES_ECB_128` (mã hóa key cho client) + `ExcelService` (GemBox) cho backup/template/import + AutoMapper.

## Bug-prone / lưu ý
- **Sửa trang web KHÔNG invalidate cache menu ở controller** — chỉ `Xoa` xóa cache (`Cache_MenuPrefix`). Nếu `IPageService.InsertOrUpdateAsync` không tự dọn cache thì menu **stale** sau khi sửa trang (`PageController` lỗi đã biết).
- **`DownloadFileTemplate` đặt tên file `PageMobile_Backup.xlsx`** trùng tên endpoint backup (`PageMobileController_Core` dòng ~282) → dễ nhầm template với backup.
- **Hai class `PageMobileController`** (Controllers chỉ đọc vs QuanTriArea CRUD) — phân biệt bằng route. Sửa dữ liệu phải dùng QuanTriArea.
- **`GetKeyByUserMobile`/`GetAllKeyMobile` có cặp `_Encrypt`** (AES-ECB-128) — client mobile phải dùng đúng khóa/định dạng camelCase; sai khóa → giải mã lỗi.

## File source (path:line)
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\...\Areas\SanboxArea\PageController.cs`, `PageMobileController.cs` (QuanTriArea, `DownloadFileTemplate` ~dòng 282); `Controllers\PageMobileController.cs` (bản đọc, `TimTheoId` key comment ~dòng 34).
- Registry: `projects/scrm/registry/he-thong/PageController.md`, `PageMobileController.md`, `PageMobileController_Core.md`.
