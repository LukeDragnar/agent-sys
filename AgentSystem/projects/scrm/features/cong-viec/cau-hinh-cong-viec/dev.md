# Dev — Cấu hình công việc

> Service BE: **WorkFlow**. FE: module **Work** (`pages/work`, route prefix `/work`). Phần thiết lập của module Công việc: mẫu (`CongViecMau`), quy trình (`QuyTrinh` versioned), lượt thực thi (`QuyTrinhThucThi` → sinh cây công việc). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component (`pages/work/work-routing.module.ts`) |
|---|---|---|
| Danh sách quy trình (thiết kế flow) | `/work/danh-sach-quy-trinh` | `DanhSachQuyTrinhComponent` (`:40`) |
| Danh sách lượt thực thi | `/work/danh-sach-quy-trinh-thuc-thi` | `DanhSachQuyTrinhThucThiComponent` (`:44`) |
| Tạo mới lượt thực thi (khởi chạy) | `/work/quy-trinh-thuc-thi-tao-moi` | `QuyTrinhThucThiTaoMoiComponent` (`:48`) |
| Chi tiết lượt thực thi | `/work/quy-trinh-thuc-thi/:id` | `QuyTrinhThucThiDetailComponent` (`:52`) |
| Danh sách mẫu công việc | `/work/danh-sach-cong-viec-mau` | `DanhSachCongViecMauComponent` (`:56`) |
| Thiết lập chung | `/work/thiet-lap-chung` | `ThietLapChungComponent` (`:60`) |
| Công việc + quy trình mở rộng | `/work/congviec-quytrinh-morong` | `CongviecQuytrinhMorongComponent` (`:72`) |

- App route prefix: `app-routing.module.ts:318` (`path: 'work'`).

## Endpoint / Controller (trace registry)
**`CongViecMauController`** — registry `cong-viec/CongViecMauController.md` (service WorkFlow, đều `[SBAuthorize]`):
- `POST api/CongViecMau/TimTheoDieuKien` · `TimTheoId/{id}` · `LayBanDoFieldTheoMau` (map field hợp lệ) · `Luu` · `XoaTheoId/{id}`.

**`QuyTrinhController`** — registry `cong-viec/QuyTrinhController.md` ([KiemTraTienIch], `[SBAuthorize]`):
- `POST api/QuyTrinh/TimTheoDieuKien` · `TimTheoId/{id}` · `Luu` (insert QuyTrinhLichSu Version+1) · `XoaTheoId/{id}` · `LichSuTheoQuyTrinh/{quyTrinhId}`.

**`QuyTrinhThucThiController`** — registry `cong-viec/QuyTrinhThucThiController.md` ([KiemTraTienIch], `[SBAuthorize]`):
| Hành động | Endpoint |
|---|---|
| Tìm lượt (lưới) | `POST api/QuyTrinhThucThi/TimTheoDieuKien` |
| Chi tiết lượt | `POST api/QuyTrinhThucThi/TimTheoId/{id}` |
| **Khởi chạy** | `POST api/QuyTrinhThucThi/KhoiChay` |
| **Bắt đầu lượt** | `POST api/QuyTrinhThucThi/BatDau/{id}` |
| Sinh mã | `POST api/QuyTrinhThucThi/SinhMa/{idQuyTrinh}` |
| Kiểm tra mã | `POST api/QuyTrinhThucThi/KiemTraMa` |
| Thông tin trước khi xóa | `POST api/QuyTrinhThucThi/LayThongTinXoa/{id}` |
| Xóa lượt (mềm) | `POST api/QuyTrinhThucThi/XoaTheoId/{id}` |

> Field động / mapping field: `DmCongViecMauFieldController` (registry `cong-viec/DmCongViecMauFieldController.md`) + `CongViecMauField` junction.

## Entities
- **`CongViecMau`** + **`CongViecMauField`** (junction field-mẫu).
- **`QuyTrinh`** (định nghĩa) + **`QuyTrinhLichSu`** (Version + FlowData snapshot).
- **`QuyTrinhThucThi`** (lượt, có thể nest qua `IdQuyTrinhThucThiCha`), **`CongViec`** (gốc + con sinh từ flow), **`DmTrangThai`** (mã CHUA_BAT_DAU/DANG_XU_LY), **`LichSuXoa`**.

## Luồng code
- **Lưu quy trình** (`QuyTrinhService.Luu`): ghi QuyTrinh + insert QuyTrinhLichSu `Version = lastVersion+1`; `KhoiChay` luôn dùng version mới nhất.
- **Khởi chạy** (`QuyTrinhThucThiController.KhoiChay`): lấy QuyTrinhLichSu Version cao nhất (chưa có → lỗi) → sinh/nhận mã → tạo lượt + CongViec gốc (Level 1, CHUA_BAT_DAU) via `ICongViecService.GanNguoiThucHienGocAsync` + `ApplyThongTinMacDinhGocAsync` → transaction → publish CongViecChanged.
- **Bắt đầu** (`BatDau`): chặn nếu `NgayBatDau != null` → chuyển DANG_XU_LY + `ICongViecService.SpawnCongViecConTuFlowAsync` → publish CongViecChanged cho tất cả CV.
- **Xóa lượt**: có CV gốc → delegate `CongViec.XoaTheoIdAsync` (Loai="QuyTrinhThucThi"); chưa bắt đầu → soft-delete lượt + lượt con + snapshot LichSuXoa.
- Trạng thái tra theo `DmTrangThai.Ma` của đơn vị.

## Bug-prone / lưu ý (path:line) — KHÔNG sửa ở session này
- **Thiếu DmTrangThai mã chuẩn**: `QuyTrinhThucThiService.cs:390-393` — trạng thái tra theo `DmTrangThai.Ma == "CHUA_BAT_DAU"/"DANG_XU_LY"`; đơn vị thiếu mã → IdTrangThai = null (lượt/CV không có trạng thái).
- **Race bắt đầu lượt**: `QuyTrinhThucThiService.cs:485-486` — chỉ chặn bằng `NgayBatDau.HasValue`, không khóa concurrency → 2 request Bắt đầu song song (race hiếm).
- **Version phình**: `QuyTrinhService.cs:166-180` (`Luu`) — mỗi save sinh QuyTrinhLichSu mới, không dedupe nếu flow không đổi.
- **Version pinning** (đúng): lượt đã khởi chạy giữ `QuyTrinhLichSuId` cố định → sửa quy trình không ảnh hưởng lượt đang chạy.
- **Xóa mẫu dangling**: `CongViecMauController` — xóa mẫu không kiểm tra CV đang dùng (`CongViec.CongViecMauId`) → reference treo.
- **Xóa lượt ripple lớn**: luôn đi qua CongViecService khi có gốc → cascade subtree + lượt nested (xem node `CongViecController` #4).

## File source (path:line)
- BE registry nodes: `projects/scrm/registry/cong-viec/CongViecMauController.md`, `.../QuyTrinhController.md`, `.../QuyTrinhThucThiController.md`, `.../DmCongViecMauFieldController.md`, `.../CongViecController.md`.
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\` — service WorkFlow (`QuyTrinhService.cs`, `QuyTrinhThucThiService.cs`, `CongViecMauService.cs`, `CongViecService.cs`).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\work\` — `danh-sach-quy-trinh/`, `danh-sach-quy-trinh-thuc-thi/` (+ `quy-trinh-thuc-thi-tao-moi/`, `quy-trinh-thuc-thi-detail/`), `danh-sach-cong-viec-mau/`, `thiet-lap-chung/` (routing `work-routing.module.ts`).
