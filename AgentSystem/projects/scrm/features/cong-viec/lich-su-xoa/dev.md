# Dev — Lịch sử xóa (thùng rác công việc)

> Service BE: **WorkFlow**. FE: module **Work** (`pages/work`, route prefix `/work`). Quản lý xóa mềm (`LichSuXoa`) + khôi phục/xóa vĩnh viễn. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Lịch sử xóa - khôi phục | `/work/lich-su-xoa` | `LichSuXoaComponent` (`pages/work/work-routing.module.ts:68`) |

- Source FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\work\lich-su-xoa\lich-su-xoa.component.ts`.

## Endpoint / Controller (trace registry)
**`LichSuXoaController`** — registry `cong-viec/LichSuXoaController.md` (service WorkFlow):
| Hành động | Endpoint | Quyền |
|---|---|---|
| Tìm lịch sử xóa (lưới) | `POST api/LichSuXoa/TimTheoDieuKien` | `LichSuXoa_TimTheoDieuKien` (chỉ `DV-AD-TONG`) |
| Khôi phục | `POST api/LichSuXoa/KhoiPhuc/{id}` | `[SBAuthorize]` (any login — ⚠ lệch quyền với list) |
| Xóa vĩnh viễn | `POST api/LichSuXoa/XoaVinhVien/{id}` | `[SBAuthorize]` (any login) |

## Entities
- **`LichSuXoa`** (gốc) — mang `PhamViXoaSnapshot` (JSON): CongViecIds, CongViecUserIds, CongViecMoRongIds, CongViecFileIds, QuyTrinhThucThiIds; cờ `DaKhoiPhuc`; Loai (CongViec / QuyTrinhThucThi); TenGoi; người/ngày xóa.
- Khôi phục động chạm: `CongViec`, `CongViecUser`, `CongViecMoRong`, `CongViecFile`, `QuyTrinhThucThi` (qua Ids snapshot).

## Luồng code
- **Nguồn sinh bản ghi** (cross_module_in): `CongViecController.Xoa` và `QuyTrinhThucThiController.XoaTheoId` gọi `CongViec.XoaTheoIdAsync(id, userLogin, loaiGhiNhan, idGocGhiNhan, tenGoiGhiNhan)` → tạo LichSuXoa với Loai/IdGoc/TenGoi tương ứng.
- **Khôi phục** (`LichSuXoaService.KhoiPhuc`): DaKhoiPhuc=true → lỗi; snapshot rỗng/hỏng → lỗi; load bản ghi soft-deleted theo snapshot bằng `IgnoreQueryFilters` → reset IsDelete=false → set DaKhoiPhuc=true → transaction → publish `CongViecChanged` (isDelete=false) cho từng CongViecId.
- **Xóa vĩnh viễn** (`LichSuXoaService.XoaVinhVien`): hard-delete bản ghi LichSuXoa (không reload / không hard-delete CV trong snapshot).

## Bug-prone / lưu ý (path:line) — KHÔNG sửa ở session này
- **Lệch phân quyền**: `LichSuXoaController.cs:29` & `:37` — `KhoiPhuc`/`XoaVinhVien` chỉ `[SBAuthorize]`, KHÔNG siết `DV-AD-TONG` như `TimTheoDieuKien` (`:21`) → user thường biết id vẫn gọi được (khôi phục/xóa vĩnh viễn cụm bất kỳ).
- **Rác tồn đọng + mất khôi phục**: `LichSuXoaService.cs:228-245` (`XoaVinhVien`) — chỉ hard-delete bản ghi LichSuXoa, KHÔNG dọn CV soft-deleted trong snapshot → CV ẩn vĩnh viễn không có đường khôi phục.
- **Khôi phục thiếu im lặng**: `LichSuXoaService.cs:154-179` (`KhoiPhuc`) — dùng `IgnoreQueryFilters` load bản ghi đã soft-delete; nếu bị xóa cứng nơi khác → snapshot khôi phục thiếu mà không báo.

## File source (path:line)
- BE registry node: `projects/scrm/registry/cong-viec/LichSuXoaController.md`; nguồn sinh: `.../CongViecController.md` (#4), `.../QuyTrinhThucThiController.md` (#8).
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\` — service WorkFlow (`LichSuXoaController.cs:21/29/37`, `LichSuXoaService.cs:154-179`/`228-245`, `CongViecService.XoaTheoIdAsync`).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\work\lich-su-xoa\lich-su-xoa.component.ts` (routing `work-routing.module.ts:68`).
