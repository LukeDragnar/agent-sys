# Dev — Cấu hình chung hệ thống

> Service: **Core** (BE .NET). 3 controller liên quan: `ConfigSystemController` (2 biến thể: QuanTriArea admin + non-Area đọc-only), `DomainCorsController` (CORS whitelist). Liên quan thêm `MobileDictionaryController` (từ điển mobile — chi tiết ở feature "Quản lý ngôn ngữ"). Chỉ dành cho dev.

## Màn hình & route (FE)
- **Quản trị → Quản lý cấu hình:** `{domain}/quantri/quanLyCauHinh` (biến thể QuanTriArea/super admin).
- **Quản trị → Quản lý domain CORS** (frontend QuanTriArea).
- Route FE cụ thể *cần xác nhận trong scrm_frontend* (chưa trace trong registry node).

## ⚠️ Trùng tên controller `ConfigSystemController` — 2 biến thể, route khác nhau
| Biến thể | Namespace / file | Route gốc | Vai trò |
|---|---|---|---|
| **QuanTriArea (admin)** | `Core.API.Areas.SanboxArea` · `Areas/SanboxArea/ConfigSystemController.cs` | `api/QuanTriArea/ConfigSystem/...` | CRUD đầy đủ + đọc; phục vụ màn quản trị (super admin) |
| **non-Area (đọc-only)** | `Core.API.Controllers` · `Controllers/ConfigSystemController.cs` | `api/ConfigSystem/...` | đọc-only cho user thường/app; có `GetConfigApp` `[AllowAnonymous]` |

→ Cùng class name ở 2 namespace, route khác nhau — **rất dễ nhầm khi tra route**.

## Endpoint / Controller (trace registry)

### `ConfigSystemController` (QuanTriArea, admin) — route `api/QuanTriArea/ConfigSystem`
`[TienIch("ConfigSystemController", ..., TI_HeThong)]`, kế thừa `BaseController`, controller-level `[SBAuthorize]`.

| Hành động | key | HTTP path |
|---|---|---|
| Tìm config theo mã | (permission bị comment — chỉ auth controller) | GET `.../TimTheoMa?ma=` |
| Lấy tất cả | `ConfigSystem_LayTatCa` | GET `.../LayTatCa` |
| Lấy theo group + sort | `ConfigSystem_LayTatCaTheoGroupVaSort` | POST `.../LayTatCaByGroupSort` |
| Lấy theo group | `ConfigSystem_LayTatCaTheoGroup` | GET `.../LayTatCaByGroup` |
| Thêm mới | `ConfigSystem_ThemMoi` | POST `.../ThemMoi` |
| **Xóa** (chỉ SuperUser) | `ConfigSystem_XoaConfigSystem` | POST `.../Xoa/{id:Guid}` |
| Cập nhật | `ConfigSystem_CapNhatThongTin` | POST `.../CapNhatThongTin` (không thấy → 404) |
| Lấy theo Id | `ConfigSystem_TimTheoId` | GET `.../TimTheoId/{id:Guid}` |
| Lấy danh sách group | `ConfigSystem_LayCacGroupConfigSystem` | GET `.../LayCacNhomCauHinh` |
| Lấy value theo code | (`[SBAuthorize]` trống) | GET `.../GetConfigSystem/{code}` |

### `ConfigSystemController` (non-Area, đọc-only) — route `api/ConfigSystem`
Kế thừa `BaseController`; auth theo từng action.
| Hành động | Auth | HTTP path |
|---|---|---|
| Lấy config app (Google Map key) | **anonymous** | GET `/api/ConfigSystem/GetConfigApp` |
| Lấy TK ngân hàng SB | internal | GET `/api/ConfigSystem/GetDiaChiBankSB` (`GetValueByCode(HT_TK_NGANHANG)`) |
| Tìm config theo mã | internal | GET `/api/ConfigSystem/TimTheoMa?ma=` |

### `DomainCorsController` — route thật `api/QuanTriArea/DomainCors` (file ở `Core.API.Controllers`, route `api/QuanTriArea/[controller]`)
Class-level `[SBAuthorize]`, mỗi action key riêng (role gợi ý HT-AD). **Sau mỗi `ThemMoi`/`CapNhatThongTin`/`Xoa` luôn gọi `_service.UpdateLaiDomainGlobal(CurrentUser)`** → ripple chính.
| Hành động | key | HTTP path |
|---|---|---|
| Xem chi tiết | `DomainCors_TimTheoId` | GET `.../TimTheoId/{id:Guid}` |
| Lấy tất cả | `DomainCors_LayTatCa` | GET `.../LayTatCa` |
| Thêm mới | `DomainCors_ThemMoi` | POST `.../ThemMoi` → insert → `UpdateLaiDomainGlobal` |
| Cập nhật | `DomainCors_CapNhatThongTin` | POST `.../CapNhatThongTin` → update → `UpdateLaiDomainGlobal` |
| Xóa | `DomainCors_Xoa` | POST `.../Xoa/{id:Guid}` → delete → `UpdateLaiDomainGlobal` |
| Tìm theo điều kiện | `DomainCors_TimTheoDieuKien` | POST `.../TimTheoDieuKien` (phân trang) |

## Entities
- `ConfigSystem` (cặp code → value, group) ↔ `ConfigSystemDto`.
- `DomainCors` ↔ `DomainCorsDto` (AutoMapper); filter `DomainCorsFilterDto`.

## Luồng code
- **ConfigSystem (admin):** `IConfigSystemService` (TimTheoMa, GetAll, GetLikeTree(Sort), Insert, Update, Delete, GetByIdAsync, GetGroupCode, GetValueByCode). Thêm/sửa set audit từ `CurrentUser`. Xóa giới hạn SuperUser qua phân quyền.
- **DomainCors:** `IDomainCorsService` (CRUD + `TimTheoDieuKienAsync` + `UpdateLaiDomainGlobal`). Mỗi mutation → refresh CORS toàn cục → mọi middleware kiểm tra origin áp danh sách mới (không restart).

## Bug-prone / lưu ý (path:line)
- **Trùng tên `ConfigSystemController`** ở 2 namespace (Area vs non-Area), route khác (`api/QuanTriArea/ConfigSystem` vs `api/ConfigSystem`) → dễ tra nhầm route/quyền.
- **`ConfigSystemController.cs:35` (Area)** — `[SBAuthorize("ConfigSystem_TimTheoMa", ...)]` **bị comment** → `TimTheoMa` chỉ còn auth controller-level, mất kiểm soát quyền chi tiết.
- **`ConfigSystemController.cs:20-25` (non-Area)** — `GetConfigApp` `[AllowAnonymous]`: chỉ trả client-side key, **không** trả secret server-side. *(Security session lưu ý — không sửa ở đây.)*
- Method service tên không đồng nhất (`Insert` vs `InsertAsync`, `Delete` vs `DeleteAsync`) — lưu ý khi trace.
- **`DomainCorsController.cs:13`** — file ở `Core.API.Controllers` nhưng route `api/QuanTriArea/[controller]` → path thật `api/QuanTriArea/DomainCors/...`.
- **`DomainCorsController.cs:70,103,127`** — `UpdateLaiDomainGlobal` gọi sau insert/update/delete **không có rollback nếu nó ném lỗi** → có thể lệch giữa DB domain và cache CORS toàn cục. *Cần kỹ thuật lưu ý.*
- **`DomainCorsController.cs:15`** — class khai báo `partial` → có thể có phần định nghĩa khác ở file riêng (kiểm tra khi mở rộng).

## File source (path:line)
- ConfigSystem admin: `Core/Core.API/Areas/SanboxArea/ConfigSystemController.cs` (`TimTheoMa` permission comment `:35`).
- ConfigSystem non-Area: `Core/Core.API/Controllers/ConfigSystemController.cs` (`GetConfigApp` anonymous `:20-25`).
- DomainCors: `Core/Core.API/Controllers/DomainCorsController.cs` (route `:13`; `UpdateLaiDomainGlobal` `:70,103,127`; partial `:15`).
- Đường dẫn gốc theo BE `D:\PROJECT\Git\_SourceForCode\Group2\scrm`.
- Registry node: `projects/scrm/registry/he-thong/ConfigSystemController_Core.md` (admin), `.../ConfigSystemController.md` (non-Area), `.../DomainCorsController.md`.
