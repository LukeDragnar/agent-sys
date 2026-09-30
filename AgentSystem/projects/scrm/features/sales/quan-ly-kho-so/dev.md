# Dev — Quản lý kho số

> Service: **Contact** (BE .NET) · FE module Sale (Angular 15 + ng-zorro). Controller: `KhoSoController`. Chỉ dành cho dev. Trace registry: `projects/scrm/registry/sales/KhoSoController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Kho số | `sale/khoso` | `KhoSoComponent` |
| Lịch sử xem số (popup) | `lichsu-xemso` | (popup component) |

## Endpoint / Controller (trace registry)
**`KhoSoController`** (service Contact):
| MaTinhNang (SBAuthorize) | Route (HTTP) | Mục đích |
|---|---|---|
| `KhoSo_TimTheoDieuKien` | POST `TimTheoDieuKien` | List/lọc kho số (qua KhoSoStoreService) |
| `KhoSo_XemSo` | POST `XemSo` | Lộ SĐT (gate cooldown), insert KhoSoViewLog |
| `KhoSo_LichSuXemSo` | POST `LichSuXemSo` | Lịch sử xem số (khi `ShowLsTacNghiepKhoSo=true`) |
| `KhoSo_ChonSo` | POST `ChonSo` | Nhặt lead — gate 10 bước + transaction |
| `KhoSo_KhoiTaoLaiKhoSoTheoDonVi` | POST `KhoiTaoLaiKhoSoTheoDonVi` | Reset kho theo đơn vị (admin, chỉ khi `IsSuDungKhoSo=false`) |
| `KhoSo_KhoiTaoLaiKhoSoTheoChiNhanh` | POST `KhoiTaoLaiKhoSoTheoChiNhanh` | Reset kho theo chi nhánh |
| `[SBAuthorize]` rỗng | GET `DropdownNhomFilter` | Combo lọc nhóm — **không gắn permission key** |

## Entities
- **`KhoSo`** — pool lead chưa có chủ (IdNhomNhanVien, KhoSoIsDaVaoKho, đơn vị/chi nhánh).
- **`KhoSoLog`** — log mỗi lần nhặt số (SaleFrom → SaleTo).
- **`KhoSoViewLog`** — log mỗi lần xem số (ai xem IdKhoSo nào, khi nào).
- **`Contact`** — đối tượng được gán chủ khi ChonSo (SaleUserId, IdNhomNhanVien, PhanBoTuDau).

## Luồng code
- **List**: `KhoSoStoreService.TimTheoDieuKienAsync(gridInfo, CurrentUser)` — list + phân quyền team/đơn vị.
- **XemSo** (`KhoSoService.cs:1108+`): gate `IsSuDungKhoSo` → `ChoPhepSuDungIconViewSo` → tồn tại + đúng đơn vị → cooldown sale (`SoPhutGiuaCacLanSaleXemSo`) → cooldown contact (`SoPhutGiuaCacLanContactMoXemSoTrongNgay`) → insert `KhoSoViewLog`, trả `{Id, KhachHangPhone}`.
- **ChonSo** (`KhoSoService.cs:1373+`): chuỗi gate 10 bước (xem flow.md / registry "Quy tắc nghiệp vụ") → transaction: `DeleteAsync(khoSo)` + Insert `KhoSoLog` + Update `Contact` (`PhanBoTuDau=KhoSo`) + `CapNhatSaleUserIdDonHangLog` (đẩy SaleUserId lên đơn hàng/đơn logistic) + log tác nghiệp → commit → ESIndexContact + ESIndexKhoSo + publish `DataUpdateContactMessage` (Mongo) + `KiemTraThayDoiContact` (Rabbit). Lỗi → RollbackAsync.
- **KhởiTạoLại** (`KhoSoService.cs:1593-1648`): chỉ chạy khi `IsSuDungKhoSo=false`; loop `StartKhoiDong(byChiNhanh)` reset theo lô 200, **không transaction tổng**.

## Bug-prone / lưu ý (KHÔNG sửa ở đây — chỉ ghi)
- **Lệch role giữa gate API và gate service ở ChonSo** (`KhoSoController.cs:58` vs `KhoSoService.cs:1387`): key `[SBAuthorize]` của endpoint `ChonSo` cho phép `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-SALE, DV-SALE-LEAD` — **KHÔNG có `DV-QL-SALE`**; trong khi gate service (step 3) lại chỉ chấp nhận `DV-QL-SALE, DV-SALE, DV-SALE-LEAD`. Hệ quả: user thuần `DV-QL-SALE` bị chặn ngay ở gate API (403) trước khi tới service → không bao giờ chọn được số dù service "cho phép" role này. Ngược lại admin (`DV-AD-*`) qua được gate API nhưng bị service từ chối. → 2 danh sách role lệch nhau, dễ gây hiểu nhầm "ai chọn được số". (Doc mô tả theo gate service — hành vi thực tế bị giới hạn thêm bởi gate API.)
- **`DropdownNhomFilter` quyền lỏng** (`KhoSoController.cs:84`): dùng `[SBAuthorize]` rỗng, không permission key — khác các endpoint còn lại đều keyed.
- **Comment auth di tích** (`KhoSoController.cs:66-67, 75-76`): còn `//[SBAuthorize()]` cạnh nút khởi tạo lại — đảm bảo không bật/tắt nhầm.
- **Endpoint test anonymous** (`KhoSoController.cs:91-97`): `TestKhoSoVaoKho` `[AllowAnonymous]` đã comment out nhưng giữ trong source — KHÔNG uncomment lên prod.
- **Đếm số lần/ngày dùng nolock đồng bộ** (`KhoSoService.cs:1411`): `TableWithNolock().Count(...)` → dưới race nhiều request đồng thời có thể đếm thiếu → vượt nhẹ `CountMaxSaleLaySoTrongNgay`.
- **Cast IdNhomNhanVien không null-check** (`KhoSoService.cs:1453, 1461`): `(Guid)khoSo.IdNhomNhanVien` — nếu kho số có `IdNhomNhanVien=null` mà rơi vào nhánh lọc nhóm → NullReferenceException.
- **Dead code ChonSo cũ** (`KhoSoService.cs:1181-1370`): ~190 dòng bản ChonSo cũ bị comment (`//TODO:Backup hàm này - tamdc 20251106`) — dễ sửa nhầm nhánh.
- **Comment lọc đơn vị di tích** (`KhoSoService.cs:1108-1117, 1422-1425`): `// && a.IdDonVi == userLogin.DonViId` bị comment rồi check đơn vị bằng if riêng → đọc dễ nhầm.
- **KhởiTạoLại không transaction tổng** (`KhoSoService.cs:1593-1648`): reset lô 200, lỗi giữa chừng để pool ở trạng thái nửa reset.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Contact\...\Controllers\KhoSoController.cs:84` (DropdownNhomFilter), `:66-67/:75-76` (comment auth), `:91-97` (test anonymous).
- BE service: `...\KhoSoService.cs:1108+` (XemSo), `:1373+` (ChonSo gate), `:1411` (count nolock), `:1453/:1461` (cast null), `:1593-1648` (KhoiTaoLai), `:1181-1370` (dead code).
- Entities: `KhoSo`, `KhoSoLog`, `KhoSoViewLog`, `Contact` (service Contact).
- Cấu hình kho số: `GetKhoSoCfgApplyAsync` (KhoSoCauHinhChiNhanhService + CauHinhService).
- FE: `KhoSoComponent` (route `sale/khoso`), popup `lichsu-xemso`.
