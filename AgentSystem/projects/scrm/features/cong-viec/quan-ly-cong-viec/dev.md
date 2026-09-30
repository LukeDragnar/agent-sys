# Dev — Quản lý công việc

> Service BE: **WorkFlow**. FE: module **Work** (`pages/work`, route prefix `/work`). Tìm kiếm 2 đường (ES / stored proc). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component (`pages/work/work-routing.module.ts`) |
|---|---|---|
| Danh sách công việc (lưới cây) | `/work/danh-sach-cong-viec` | `DanhSachCongViecComponent` (`:20`) |
| Timeline công việc | `/work/timeline-cong-viec` | `TimelineCongViecComponent` (`:24`) |
| Custom view | `/work/custom-view` | `CustomViewPageComponent` (`:28`) |
| Chi tiết 1 công việc | `/work/cong-viec/:id` | `CongViecDetailPageComponent` (`:32`) |
| Báo cáo công việc | `/work/bao-cao-cong-viec` | `BaoCaoCongViecComponent` (`:36`) |
| Node tree | `/work/node-tree` | `NodeTreeComponent` (`:64`) |

- App route prefix: `app-routing.module.ts:318` (`path: 'work'` → lazy `WorkModule`).

## Endpoint / Controller (trace registry)
**`CongViecController`** — registry `cong-viec/CongViecController.md` (service WorkFlow):
| Hành động | Endpoint | Quyền |
|---|---|---|
| Tìm công việc (lưới cây, phân trang) | `POST api/CongViec/TimTheoDieuKien` | `CongViec_TimTheoDieuKien` (đa role) |
| Chi tiết | `POST api/CongViec/TimTheoId/{id}` | `[SBAuthorize]` |
| Lưu (thêm/sửa) | `POST api/CongViec/Save` | `[SBAuthorize]` |
| Xóa (mềm) | `POST api/CongViec/Xoa/{id}` | `[SBAuthorize]` |
| Thông tin trước khi xóa | `POST api/CongViec/LayThongTinXoa/{id}` | `[SBAuthorize]` |
| Cập nhật 1 trường (inline) | `POST api/CongViec/CapNhatTruong` | `[SBAuthorize]` |
| Lịch sử công việc | `POST api/CongViec/LayLichSuCongViec/{idCongViec}` | `[SBAuthorize]` |
| Tự động sắp xếp thời gian | `POST api/CongViec/TuDongSapXepThoiGian` | `[SBAuthorize]` |
| Tải template import | `GET api/CongViec/DownloadTemplateImport?congViecMauId=` | `[SBAuthorize]` |
| Import Excel | `POST api/CongViec/ImportExcel` | `[SBAuthorize]` |
| Dropdown user/nhóm/cột group-by | `POST .../LayDanhSachUser` · `LayDanhSachNhomPhanTrang` · `LayDanhSachCotGroupByDuoc` | `[SBAuthorize]` |

**`CongViecBaoCaoController`** — registry `cong-viec/CongViecBaoCaoController.md` (KHÔNG có `[KiemTraTienIch]`):
- `POST api/CongViecBaoCao/TongQuan` · `TheoNguoiDung` · `TheoMucDoUuTien` — đều `[SBAuthorize]`, scope cứng theo `IdChiNhanh` user.

## Entities
- **`CongViec`** (gốc, cây qua `IdCongViecCha`, `Level`), **`CongViecUser`** (người thực hiện/phê duyệt + COTMORONG field user), **`CongViecMoRong`** (field động EAV), **`CongViecFile`**, map `DmTrangThai`/`DmMucDoUuTien`/`CongViecMauField`.
- **`CongViecLichSu`** — audit log ở **MongoDB** (không EF).
- **`LichSuXoa`** — snapshot Ids khi xóa mềm.

## Luồng code
- **Tìm kiếm 2 đường**: `ResolveUseEsAsync` đọc `CauHinh.JsonSetting_SuperAdmin.IsUseElasticSearch` (per đơn vị) → fallback global `Search:CongViec:UseES` (default true). ES path (`TimTheoDieuKienESAsync`) giữ cây bằng "tập hiển thị" (CV khớp filter + mọi cha tổ tiên). SP path = `sp_CongViec_TimTheoDieuKien` (3 result-set: data/users/extfields).
- **Save**: Id rỗng → tạo (sinh mã nếu trống, check trùng, `Level = cha+1`, chặn vượt cấp theo `DoSauCongViecCon`); có Id → update. Sync `CongViecMoRong` + `CongViecUser` (field user). Ghi audit + publish `CongViecChanged`.
- **CapNhatTruong**: lưu giá trị cũ → ghi CongViecLichSu (Mongo); field user → sync CongViecUser.
- **Xóa**: gom subtree + lượt QuyTrinhThucThi → soft-delete + snapshot `LichSuXoa` (Loai="CongViec") + publish isDelete=true.
- **Lịch sử**: đọc `CongViecLichSu` từ MongoDB (sort Ngay DESC, phân trang, scope IdDonVi).

## Bug-prone / lưu ý (path:line) — KHÔNG sửa ở session này
- **ES path mất security filter**: `CongViecService.cs:394-403` — filter "chỉ thấy CV mình tạo/giao" đã comment → ES path không giới hạn theo user (mọi user thấy toàn bộ CV đơn vị); chỉ SP path chặn AdminTong.
- **Cap 10.000 node**: `CongViecService.cs:560` (`MaxMatched=10000`) — cây > 10k node có thể mất nhánh ở ES path.
- **Last-write-wins**: `CongViecService.cs:2961-2965` (`CapNhatTruongAsync`) — OT conflict detection (ClientTimestamp) đã comment → 2 user sửa cùng ô ghi đè không cảnh báo.
- **Xóa ripple lớn**: `CongViec.Xoa` kéo cả lượt thực thi gắn CV gốc trong subtree — lưu ý khi xóa CV cha (registry node #4).
- **Lịch sử ở Mongo**: đổi schema field phải sửa cả nơi ghi (Save/CapNhatTruong/Spawn) — không EF.
- **CV tạo từ ngoài**: `CongViecExternalController` dùng chung `ICongViecService.LuuAsync` nhưng không xác thực user thật → CV gắn đơn vị cố định (registry node `CongViecExternalController`).
- **Báo cáo scope chi nhánh**: `CongViecBaoCaoService.cs:208-211` (`BuildQuery`) — scope cứng theo `IdChiNhanh` user → admin tổng KHÔNG xem được số liệu toàn đơn vị; `:55-56` "quá hạn" chỉ tính `NgayBatDauDuKien + Estimate` (bỏ NgayKetThucDuKien); báo cáo theo người đếm trùng CV nếu CV nhiều người thực hiện.
- **Endpoint Save dùng chung Thêm và Sửa** (phân biệt bằng Id) — chú ý khi đổi behavior.

## File source (path:line)
- BE registry nodes: `projects/scrm/registry/cong-viec/CongViecController.md`, `.../CongViecBaoCaoController.md`, `.../CongViecExternalController.md`.
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\` — service WorkFlow (`CongViecService.cs`, `CongViecBaoCaoService.cs`); stored proc `sp_CongViec_TimTheoDieuKien` (DB, không trong repo).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\work\` (routing `work-routing.module.ts`, list `danh-sach-cong-viec/`, chi tiết `cong-viec-detail-page/`, báo cáo `bao-cao-cong-viec/`).
