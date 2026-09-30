# Dev — Quản lý chấm công

> Service: **Branch** (BE .NET) · TienIch `TI_ChamCong`. Báo cáo dùng thêm `IBaoCaoChamCongService`. Chỉ dành cho dev.

## Màn hình & route (UI)
| Màn hình | Vị trí UI |
|---|---|
| Phân ca / Phân ca nâng cao | Chấm công > Phân ca (danh sách & lịch calendar) |
| Giờ làm việc / Chấm công | Chấm công > Giờ làm việc (danh sách & lịch, duyệt/từ chối) |
| Wifi BSSID | Chấm công > Wifi BSSID |
| IMEI thiết bị | Chấm công > IMEI thiết bị |
| Báo cáo chấm công | Chấm công > Báo cáo (chi tiết/theo ca/theo ngày) + xuất Excel |

> FE component/route cụ thể chưa trace trong session này — cần product/dev bổ sung khi đụng FE.

## Endpoint/Controller (trace registry)
**Chấm công giờ làm — `GioLamViecNhanVienController`** (registry: `registry/nhan-su/GioLamViecNhanVienController.md`):
| MaTinhNang / hành động | Route | Ghi chú |
|---|---|---|
| `GioLamViecNhanVien_TimTheoDieuKienTypeList` / `...TypeCalendar` | POST `TimTheoDieuKienTypeList` / `TimTheoDieuKienTypeCalendar` | danh sách / lịch |
| (key commented → login-only) | GET `DuyetGioLam/{id}` · POST `TuChoiGioLam` · POST `Xoa/{id}` | duyệt/từ chối/xóa |
| (key commented → login-only) | POST `ThemMoi_Location` (multipart) · POST `ThemMoi_QuanLy` | NV check-in theo vị trí / quản lý chấm công hộ |
| `GioLamViecNhanVien_ThongKeNgayNghiNhanVien` | POST `ThongKeNgayNghiNhanVien` | thống kê nghỉ |
| `GioLamViecNhanVien_BaoCaoChamCongChiTiet` / `...TheoCa` / `...TheoNgay` | POST tương ứng | báo cáo |
| (login-only) | POST `BaoCaoChamCongChiTietUser` | ép ListIdChiNhanh=[CurrentUser.IdChiNhanh] + IdNhanVien=CurrentUser.Id |
| `GioLamViecNhanVien_ExportExcelBaoCaoChamCong(V2)` | POST `ExportExcelBaoCaoChamCong`/`...V2` | GemBox, 2 layout |

**Phân ca — `CaLamViecNhanVienController` + `CaLamViecNhanVienNangCaoController`** (registry: 2 node cùng tên):
- `CaLamViecNhanVien`: `CapNhatThongTin` (lưu ca, key commented), `DeleteUserCaLamViec` (xóa, key commented), `CheckUserCaLamViecToChangeIsSuDungKhungGio` (chặn đổi khung giờ đang dùng), `GetLichLamViecCaNhan` (lịch cá nhân), `GetCaLamViecNhanVienByDay` **[AllowAnonymous]**.
- `CaLamViecNhanVienNangCao`: `CreateBasic`/`CreateAdvanced`/`CreateSingle` (tạo, key commented), `DeleteBasic`/`DeleteById/{id}` (xóa, key commented), `CaLamViecNhanVienNangCao_TimTheoDieuKien` / `...TypeCalendar` (tìm, có key).

**Wifi — `BSSIDController`** (registry: `registry/nhan-su/BSSIDController.md`):
- Đọc có key: `BSSID_TimTheoDieuKien`, `BSSID_TimTheoId`.
- Ghi **không key** (login-only + role inline): POST `ThemMoi`, POST `CapNhat`, GET `Xoa/{id}`, GET `PheDuyet/{id}` (toggle IsDuyet).
- App NV: POST `CheckBSSID` (login-only) — match Value HOẶC Ten trong CN.

**IMEI — `UserIMEIController`** (registry: `registry/nhan-su/UserIMEIController.md`):
- Đọc có key: `UserIMEI_TimTheoDieuKien`, `UserIMEI_TimTheoId`.
- Ghi **không key** (login-only + role inline): POST `CapNhat`, POST `ThemMoi`, GET `Xoa/{id}`, GET `PheDuyet/{id}` (toggle).
- App NV: POST `CheckExsit` (lọc UserId==user.Id), POST `DangKyIMEI`, POST `CheckImei`.

## Entities
- `GioLamViecNhanVien` — buổi công, có trạng thái duyệt (ChoDuyet/DaDuyet/TuChoi).
- `CaLamViecNhanVien` — gán NV ↔ khung giờ ↔ ngày (phân ca; bản nâng cao tạo theo lô vẫn cùng bảng).
- `BSSID` — wifi theo chi nhánh, cờ `IsDuyet`.
- `UserIMEI` — thiết bị NV theo chi nhánh, cờ `IsDuyet`.
- Phụ thuộc: `KhungGioNhanVien`, `User`/NhanVien (Core), `BaoCaoChamCongService`.

## Luồng code
1. Check-in vị trí: `GioLamViecNhanVienController.ThemMoi_Location(gridInfo, file)` parse JSON `model` thủ công bằng JsonConvert (không qua ModelState) → validate → ghi buổi công.
2. Chấm công wifi/IMEI: app NV gọi `CheckBSSID`/`CheckImei` (non-admin ép `IdChiNhanh = user.IdChiNhanh`) → trả IsExisted/IsApproved; chỉ bản ghi tồn tại + IsDuyet mới approved.
3. Thao tác wifi/IMEI: service check `CommonService.CheckRoleThaoTac(user, cauHinh.RoleDuocPhepThaoTac...)` trước; pass mới ghi DB + LichSuTacDong.
4. Duyệt buổi công: `DuyetGioLam` chuyển state sang Đã duyệt; `TuChoiGioLam` cần lý do.
5. Báo cáo + Excel: qua `BaoCaoChamCongService`; export GemBox, `!rs.Success || rs.Data==null` → BadRequest.

## Bug-prone / lưu ý
- **Mất phân quyền role ở các thao tác ghi**: `GioLamViecNhanVienController.cs:74,83,92,117,127,154` (duyệt/từ chối/check-in/chấm-công-hộ/show) và `CaLamViecNhanVienNangCaoController.cs:15` (5/7 action) để key SBAuthorize bị comment → chỉ login-only. Rủi ro: NV thường có thể tự duyệt / chấm công hộ / tạo-xóa ca nếu chỉ chặn ở UI. (chỉ ghi nhận — session security xử lý)
- **`CaLamViecNhanVienController.cs:49` `GetCaLamViecNhanVienByDay` để [AllowAnonymous]** → gọi không cần login. `CaLamViecNhanVienController.cs:90-99` có endpoint private đã comment chết.
- **Wifi/IMEI dùng HTTP GET cho thao tác ghi**: `BSSIDController.cs:61,74` (Xoa/PheDuyet GET), `UserIMEIController.cs:62,75` (Xoa/PheDuyet GET) — không idempotent, CSRF-prone.
- **PheDuyet là toggle**: `BSSIDService.cs:123` (`IsDuyet = !IsDuyet`) — gọi 2 lần quay về chưa duyệt; FE/người dùng dễ vô tình bỏ duyệt.
- **Nhánh admin xem toàn đơn vị bị comment**: `BSSIDService.cs:71,119,173,236,294` → cả admin cũng bị giới hạn theo `IdChiNhanh`.
- **Log gắn nhầm bảng (IMEI)**: `UserIMEIService.cs:141` LichSuTacDong ghi `TableId = SCRM_MAJOR_BSSID` (copy nhầm từ BSSID) → log IMEI bị gắn nhầm bảng BSSID.
- **Tên action sai chính tả**: `UserIMEIController.cs:100` `CheckExsit` (giữ nguyên để khớp FE/route).
- **Role config khác nhau**: `UserIMEIService.cs:114,151` dùng `RoleDuocPhepThaoTacThietBiChamCong` (IMEI) vs BSSID dùng `RoleDuocPhepThaoTacThietBiWifi` — đừng nhầm 2 key cấu hình.
- **Check-in multipart validate thủ công**: `GioLamViecNhanVienController.cs:131` dùng JsonConvert thay vì ModelState → lỗi parse trả message khác nhau (xem flow case biên).

## File source (path:line)
- `GioLamViecNhanVienController.cs:74,83,92,117,127,131,154` (BE service Branch).
- `CaLamViecNhanVienController.cs:36,49,67,90-99`.
- `CaLamViecNhanVienNangCaoController.cs:15`.
- `BSSIDController.cs:34,47,60,61,73,74,100`; `BSSIDService.cs:71,119,123,173,236,294`.
- `UserIMEIController.cs:35,48,61,62,74,75,99,100,112,126`; `UserIMEIService.cs:114,141,151`.
- Registry nodes: `projects/scrm/registry/nhan-su/{GioLamViecNhanVien,CaLamViecNhanVien,CaLamViecNhanVienNangCao,BSSID,UserIMEI}Controller.md`.
