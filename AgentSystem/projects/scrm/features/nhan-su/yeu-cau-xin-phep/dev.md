# Dev — Yêu cầu & xin phép (Phiếu yêu cầu)

> Service: **Branch** (BE .NET). Controller route `api/[controller]/[action]` (action vào URL). Chỉ dành cho dev.

## Màn hình & route (UI)
| Màn hình | Vị trí UI |
|---|---|
| Phiếu yêu cầu (grid quản lý + duyệt/từ chối) | Chấm công > Phiếu yêu cầu |
| App NV: đăng ký ca / làm thay / chấm công trên phiếu / hủy | app di động (mirror nghiệp vụ) |

> FE component/route cụ thể chưa trace session này — bổ sung khi đụng FE.

## Endpoint/Controller (trace registry)
**`PhieuYeuCauController`** — registry: `registry/nhan-su/PhieuYeuCauController.md` (route `api/PhieuYeuCau/[action]`):
| # | Hành động | Endpoint | Quyền (key) |
|---|---|---|---|
| 1 | Grid quản lý | POST `TimTheoDieuKien` | `PhieuYeuCau_Branch_TimTheoDieuKien` (HR/AD/KT-TONG) |
| 2 | Chi tiết | GET `TimTheoId/{id}` | `PhieuYeuCau_Branch_TimTheoId` |
| 3 | Phiếu của tôi | POST `LayPhieuYeuCauCuaToi` | `[SBAuthorize]` login-only |
| 4 | Đăng ký làm (Loại 1) | POST `DangKyLam` | `[SBAuthorize]` login-only |
| 5 | Duyệt Loại 1 | POST `DuyetDangKyLam` | login-only + role inline |
| 6 | Từ chối Loại 1 | POST `TuChoiDangKyLam` | login-only + role inline |
| 7 | Lấy ca của NV (picker L2) | POST `LayCaCuaNhanVien` | login-only |
| 8 | Đăng ký làm thay (Loại 2) | POST `DangKyLamThayThe` | login-only |
| 9 | Chấm công trên phiếu | POST `ChamCong_PhieuYeuCau` (multipart) | login-only |
| 10 | Duyệt làm thay | POST `DuyetLamThayThe` | login-only + role inline |
| 11 | Từ chối làm thay | POST `TuChoiLamThayThe` | login-only + role inline |
| 12 | Hủy phiếu | POST `HuyDangKy/{id}` | login-only |

- Role duyệt thực thi **inline** trong service: `CommonService.CheckRoleThaoTac(user, cauHinh.RoleDuocPhepDuyetPhieuYeuCau)` — KHÔNG qua permission key.

## Entities
- `PhieuYeuCau` (+ `IdCaLamViecNhanVienNangCao`, `IdGioLamViecCuaA/B`, `ThongTinChamCong`).
- Sinh ra: `CaLamViecNhanVienNangCao`, `GioLamViecNhanVien`.
- Tham chiếu: `CaLamViecNhanVien`, `User`.
- State: `EnumTrangThaiPhieuYeuCau` (ChoDuyet/DaDuyet/TuChoi/HuyBoiNhanVien); loại: `EnumLoaiPhieuYeuCau` (DangKyLam/DangKyLamThayThe).

## Luồng code
1. **DangKyLam:** validate khung giờ thuộc CN + không quá khứ → Redis lock theo (NV,ngày,khungGio,đơnVị) → check trùng ca → tạo phiếu Loại 1 ChoDuyet.
2. **DuyetDangKyLam:** role inline → re-check trùng ca (exclude phiếu này) → tạo `CaLamViecNhanVienNangCao` (IsDuyet=true) → set phiếu DaDuyet + gắn IdNangCao.
3. **DangKyLamThayThe:** validate B (cùng đơn vị, có CN, khung giờ thuộc CN B, có ca, chưa đi làm), A không trùng giờ, không tự thay mình → Redis lock → tạo phiếu Loại 2 ChoDuyet.
4. **ChamCong_PhieuYeuCau:** chủ phiếu + Loại 2 ChoDuyet → branch theo TrangThaiCheckIn (1 in/2 out) → selfie bắt buộc trừ khi cấu hình tắt → upload FileService → append `ThongTinChamCong`.
5. **DuyetLamThayThe:** xác định IdKhungGioMap (cùng CN tự lấy; khác CN bắt buộc thuộc CN A) → Case 2A (chưa chấm) tạo NangCao cho A / Case 2B (đã chấm) tạo GLV DiLam cho A → luôn tạo GLV NghiLam cho B → DB writes trong `CreateExecutionStrategy().ExecuteAsync` (retry-safe) → SyncDataUser + clear cache NGOÀI transaction (best-effort).

## Bug-prone / lưu ý
- **Phân quyền duyệt qua cấu hình, không qua key**: `PhieuYeuCauController.cs:37,44...` — đa số action chỉ `[SBAuthorize]` login-only; role duyệt check inline (`CheckRoleThaoTac` + `RoleDuocPhepDuyetPhieuYeuCau`) → audit không thể chỉ dựa permission key.
- **Route khác chuẩn**: `PhieuYeuCauController.cs:14` dùng `api/[controller]/[action]` (khác các controller `[controller]` thường) → tên action là segment URL; grep route phải tính theo action.
- **Anti-race bằng Redis**: `PhieuYeuCauService.cs` lock ~15s (`_submitLockTtl`) cho DangKyLam/DangKyLamThayThe → tránh tạo phiếu trùng khi bấm 2 lần.
- **Transaction + HTTP sync tách bạch**: `PhieuYeuCauService.cs:574-589` — DuyetLamThayThe wrap DB writes trong `CreateExecutionStrategy().ExecuteAsync` (retry-safe vì SqlServerRetryingExecutionStrategy); HTTP sync (SyncDataUser/clear cache) để NGOÀI transaction, best-effort, không fail nghiệp vụ. Khi sửa nhớ giữ ranh giới này (không gọi HTTP trong execution strategy).
- **Cross-branch cần IdKhungGioMap**: duyệt làm thay khác chi nhánh bắt buộc IdKhungGioMap thuộc CN A — thiếu sẽ lỗi; cùng CN mặc định = IdKhungGio của phiếu.
- **Chấm công multipart**: `ChamCong_PhieuYeuCau` parse model + validate (SafeJsonSettings + TryValidateModel) — lưu ý khi đổi DTO.

## Quan hệ ripple (cross-module)
- Lấy KhungGio từ **Booking** (`LayKhungGioListCuaChiNhanh`).
- Upload ảnh selfie lên **FileService** (`/Files/UploadFile`).
- Duyệt → ripple sang **chấm công** (CaLamViecNhanVienNangCao + GioLamViecNhanVien) → bảng công → tính lương.
- Đồng bộ user + clear cache qua **Core** (`SyncDataUser`, `ClearCache_User_Group_By_ChiNhanh`).

## File source (path:line)
- `PhieuYeuCauController.cs:14,37,44…` (route + action login-only + role inline).
- `PhieuYeuCauService.cs:409` (throw đúng), `:574-589` (transaction + sync ngoài tx).
- Registry node: `projects/scrm/registry/nhan-su/PhieuYeuCauController.md`.
- Liên quan: `projects/scrm/registry/nhan-su/{CaLamViecNhanVienNangCao,GioLamViecNhanVien,CaLamViecNhanVien}Controller.md`.
