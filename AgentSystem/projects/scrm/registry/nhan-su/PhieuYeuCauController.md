---
controller: PhieuYeuCauController
module: nhan-su
service: Branch
man_hinh_ui: "Chấm công > Phiếu yêu cầu (đăng ký ca làm / làm thay) — danh sách + duyệt/từ chối cho quản lý; app NV: đăng ký ca, đăng ký làm thay, chấm công trên phiếu, hủy phiếu"
muc_dich: "Phiếu yêu cầu chấm công 2 loại: (1) Đăng ký làm ca của chính mình; (2) Đăng ký làm thay nhân viên khác (cross-branch). Gồm self-service submit, chấm công check-in/out trên phiếu, và duyệt/từ chối của quản lý (tạo CaLamViecNhanVienNangCao / GioLamViecNhanVien)."
alias: [PhieuYeuCau, "phiếu yêu cầu", "đăng ký ca làm", "đăng ký làm thay", "làm hộ", "đăng ký làm hộ"]
auth: mixed
state_machine: [ChoDuyet, DaDuyet, TuChoi, HuyBoiNhanVien]
deprecated: false
entities: [PhieuYeuCau, CaLamViecNhanVienNangCao, GioLamViecNhanVien, CaLamViecNhanVien, User]
stored_procs: []
depends: [CauHinhService (RoleDuocPhepDuyetPhieuYeuCau, KhongBatBuocChupAnhKhiLamHo), RedisCache (anti-race lock), Booking (KhungGio), FileService (upload ảnh selfie)]
cross_module_in: []
cross_module_out: [Booking (lấy KhungGio chi nhánh), FileService (UploadFile ảnh chấm công), Core (ChiNhanh denormalize)]
---

# PhieuYeuCau — Phiếu yêu cầu đăng ký ca làm / làm thay

> Route `api/[controller]/[action]` (action vào URL). Kế thừa `BaseController`. Phân quyền hỗn hợp: List/Detail role-restricted bằng `[SBAuthorize(key,...)]`; các action self-service/duyệt dùng `[SBAuthorize]` login-only + **role check inline** trong service (`CommonService.CheckRoleThaoTac` với `cauHinh.RoleDuocPhepDuyetPhieuYeuCau`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm phiếu (grid quản lý) | Danh sách/Lọc | POST `TimTheoDieuKien` | `PhieuYeuCau_Branch_TimTheoDieuKien` | public | Quản lý (HR/AD/KT-TONG) | Lọc phiếu theo đơn vị (ép tenant) + filter loại/trạng thái/ngày/NV. Phân trang. | EF | — |
| 2 | Xem chi tiết phiếu | Mở chi tiết | GET `TimTheoId/{id}` | `PhieuYeuCau_Branch_TimTheoId` | public | Quản lý | Trả chi tiết phiếu + denormalize NV/ChiNhanh/KhungGio. Ép tenant. | EF + Booking/Core | — |
| 3 | Phiếu của tôi theo ngày | (app NV) | POST `LayPhieuYeuCauCuaToi` | `[SBAuthorize]` login-only | mixed | NV đăng nhập | Phiếu liên quan tôi (đăng ký HOẶC được làm hộ) theo ngày, loại trừ DaDuyet. | EF | — |
| 4 | Đăng ký làm ca của mình (Loại 1) | Đăng ký ca | POST `DangKyLam` | `[SBAuthorize]` login-only | mixed | NV đăng nhập | Tạo phiếu Loại 1 trạng thái ChoDuyet; Redis lock + check trùng ca + chống trùng phiếu. | EF + Booking + Redis | Quá khứ/khung giờ sai CN → chặn |
| 5 | Duyệt đăng ký làm (Loại 1) | Duyệt | POST `DuyetDangKyLam` | `[SBAuthorize]` login-only + role inline | mixed | Phiếu Loại 1, ChoDuyet | Re-check trùng ca → tạo `CaLamViecNhanVienNangCao` → phiếu → DaDuyet. | EF | Role gate qua cấu hình, không qua key |
| 6 | Từ chối đăng ký làm (Loại 1) | Từ chối | POST `TuChoiDangKyLam` | `[SBAuthorize]` login-only + role inline | mixed | Phiếu Loại 1, ChoDuyet | Bắt buộc lý do (GhiChu) → phiếu → TuChoi. | EF | — |
| 7 | Lấy ca của NV (picker Loại 2) | Chọn ca làm thay | POST `LayCaCuaNhanVien` | `[SBAuthorize]` login-only | mixed | NV đăng nhập | List ca (weekly + nangcao) của NV B theo ngày + cờ DaCoPhieuChoDuyet. | EF + Booking | — |
| 8 | Đăng ký làm thay (Loại 2) | Đăng ký làm thay | POST `DangKyLamThayThe` | `[SBAuthorize]` login-only | mixed | NV đăng nhập | Tạo phiếu Loại 2 ChoDuyet; validate B cùng đơn vị + có ca + chưa đi làm; Redis lock + chống trùng. | EF + Booking + Redis | Không làm thay chính mình |
| 9 | Chấm công trên phiếu (Loại 2) | Check-in / Check-out | POST `ChamCong_PhieuYeuCau` (multipart) | `[SBAuthorize]` login-only | mixed | Phiếu Loại 2, ChoDuyet, đúng chủ phiếu | 1 endpoint branch theo `TrangThaiCheckIn` (1=in,2=out); selfie bắt buộc tùy cấu hình; upload FileService. | EF + FileService | F14: SafeJsonSettings + TryValidateModel |
| 10 | Duyệt làm thay (Loại 2) | Duyệt | POST `DuyetLamThayThe` | `[SBAuthorize]` login-only + role inline | mixed | Phiếu Loại 2, ChoDuyet | Tạo GLV/NangCao cho A + GLV NghiLam cho B (transaction); sync user + clear cache ngoài tx. | EF (TX) + Core | Cross-branch cần IdKhungGioMap |
| 11 | Từ chối làm thay (Loại 2) | Từ chối | POST `TuChoiLamThayThe` | `[SBAuthorize]` login-only + role inline | mixed | Phiếu Loại 2, ChoDuyet | Bắt buộc lý do → phiếu → TuChoi. | EF | — |
| 12 | Hủy phiếu | Hủy | POST `HuyDangKy/{id}` | `[SBAuthorize]` login-only | mixed | Chủ phiếu, chỉ ChoDuyet | NV tự hủy phiếu của mình → HuyBoiNhanVien. | EF | — |

## Quy tắc nghiệp vụ (gating / state)
**State machine `EnumTrangThaiPhieuYeuCau`:** ChoDuyet(1) → DaDuyet(2) | TuChoi(3) | HuyBoiNhanVien(4). Chỉ ChoDuyet mới được duyệt/từ chối/hủy/chấm công; mọi action khác trên phiếu đã xử lý đều bị chặn.
**Loại phiếu `EnumLoaiPhieuYeuCau`:** DangKyLam(1), DangKyLamThayThe(2). Mỗi action chặn nếu sai loại.
1. **DangKyLam:** chặn khung giờ không thuộc CN của user; chặn ca quá khứ; Redis lock theo (NV,ngày,khungGio,đơnVị); check trùng ca trong ngày; chống tạo 2 phiếu ChoDuyet cùng key.
2. **DuyetDangKyLam:** role qua `cauHinh.RoleDuocPhepDuyetPhieuYeuCau`; re-check trùng ca (exclude phiếu này); tạo `CaLamViecNhanVienNangCao` (IsDuyet=true) rồi set phiếu DaDuyet + gắn IdNangCao.
3. **DangKyLamThayThe:** B phải cùng đơn vị + có chi nhánh; khung giờ thuộc CN của B; B thực sự có ca (`KiemTraNhanVienCoCa`) và chưa đi làm; A không trùng giờ; Redis lock theo (B,ngày,khungGio); chống trùng phiếu Loại 2 ChoDuyet.
4. **ChamCong_PhieuYeuCau:** chỉ chủ phiếu; chỉ phiếu Loại 2 ChoDuyet; CheckIn chỉ khi chưa có GioDen, CheckOut chỉ khi đã có GioDen & chưa có GioVe; selfie bắt buộc trừ khi `cauHinh.KhongBatBuocChupAnhKhiLamHo==true`; append `ThongTinChamCong` event.
5. **DuyetLamThayThe:** cùng CN → IdKhungGioMap mặc định = IdKhungGio của phiếu; khác CN → bắt buộc IdKhungGioMap thuộc CN A. Validate B chưa check-in/out (ScanStatus 1/2). Case 2A (chưa chấm công trên phiếu) → tạo NangCao cho A; Case 2B (đã chấm trên phiếu) → tạo GLV DiLam cho A tính đi muộn/sớm/về sớm. Luôn tạo GLV NghiLam cho B. DB writes trong transaction (ExecutionStrategy), SyncDataUser + clear cache best-effort ngoài tx.

## Luồng xuyên module
- Submit/chấm công → lấy KhungGio từ **Booking** (`LayKhungGioListCuaChiNhanh`); chấm công Loại 2 upload ảnh selfie lên **FileService** (`/Files/UploadFile`).
- Duyệt Loại 1/2 → sinh **CaLamViecNhanVienNangCao** và/hoặc **GioLamViecNhanVien** (ripple sang lịch làm việc + bảng công); sau commit gọi `SyncDataUser` + `ClearCache_User_Group_By_ChiNhanh` (Core) cho cả CN A và CN B nếu khác nhau.

## Quan hệ
- **Gọi sang:** Booking (KhungGio), FileService (upload ảnh), Core (ChiNhanh denormalize + cache user-group), CauHinhService + RedisCache nội bộ.
- **Dữ liệu (entities):** `PhieuYeuCau` (+ `IdCaLamViecNhanVienNangCao`, `IdGioLamViecCuaA/B`, `ThongTinChamCong`), `CaLamViecNhanVienNangCao`, `GioLamViecNhanVien`, `CaLamViecNhanVien`, `User`.
- **Ripple/đồng bộ:** duyệt phiếu = side-effect tạo ca nâng cao / giờ làm việc → ảnh hưởng bảng công chấm công.

## Lỗi đã biết / lưu ý kỹ thuật
- `PhieuYeuCauController.cs:14` — route `api/[controller]/[action]` (khác các controller `[controller]` thường) → tên action là segment URL.
- `PhieuYeuCauController.cs:37,44...` — đa số action chỉ `[SBAuthorize]` login-only; phân quyền duyệt thực thi **inline** trong service (`CheckRoleThaoTac` + `RoleDuocPhepDuyetPhieuYeuCau`), không phải qua permission key → audit không thể chỉ dựa vào key.
- `PhieuYeuCauService.cs:409` — không có (controller này dùng `throw;` đúng); lock anti-race qua Redis 15s (`_submitLockTtl`) cho DangKyLam/DangKyLamThayThe.
- `PhieuYeuCauService.cs:574-589` — DuyetLamThayThe wrap DB writes trong `CreateExecutionStrategy().ExecuteAsync` (retry-safe vì SqlServerRetryingExecutionStrategy); HTTP sync để NGOÀI transaction, best-effort không fail nghiệp vụ.
