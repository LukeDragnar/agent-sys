# Dev — Chia số care đơn

> Chỉ dành cho dev. BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm` · FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`. Service nghiệp vụ thuộc **OrderLogistic**; phần user care đơn nằm ở **Core**; engine chia tự động chạy ở service riêng **ChiaSoCareDon.API**.

## Màn hình & route (FE)
Module lazy-load tại `app-routing.module.ts` path `chia-so-care-don` → `ChiaSoCareDonModule`.
Routing con: `src/app/pages/chia-so-care-don/chia-so-care-don-routing.module.ts`

| Màn | Route | Component |
|-----|-------|-----------|
| Chia số care đơn thủ công | `chia-so-care-don/chiaso-care-don-thu-cong` | `ChiaSoCareDonThuCongComponent` (`pages/chia-so-care-don/chia-so-care-don-thu-cong/`) |
| Cấu hình care đơn | `chia-so-care-don/chiaso-care-don-cau-hinh` | `ChiaSoCareDonCauHinhAddComponent` (`.../chia-so-care-don-cau-hinh/chia-so-care-don-cau-hinh-add/`) |
| Bật/tắt nhận dữ liệu | `chia-so-care-don/chiaso-care-don-nhan-du-lieu` | `BatTatNhanDataComponent` (`.../bat-tat-nhan-data/`) |

FE services: `services/orderLogistic/chia-so-care-don.service.ts`, `services/orderLogistic/cau-hinh-chia-so-care-don.service.ts`, `services/core/user.service.ts`.
Enum FE: `shared/Enums/ChiaSoCareDonEnum.enum.ts` (`TabCHCSCareDonEnum`, `TrangThaiPhanBoEnum {None, DaChia, ChuaChia}`, `TrangThaiUserCareDonEnum`).
Lưu ý: trong FE còn các thư mục có hậu tố ngày (`...20250619`, `...20250620`) — bản đang refactor song song; xác minh component nào đang được route trỏ tới trước khi sửa.

## Controller (BE)
- `OrderLogistic/OrderLogistic.API/Controllers/ChiaSoCareDonController.cs` — `[TienIch("ChiaSoCareDonController", ...)]`, inject `IChiaSoCareDonService`. Endpoint chính của **chia thủ công**:
  - `POST GetDSAllDonHangLGTChuaPhanBo` → `GetDSAllDonHangLGTChuaPhanBoAsync20250620` (trace mục tiêu).
  - `GetDSPhongBan`, `GetDSNhomNVByPhongBan`, `GetDSNhomNVKho`, `GetDSNhanVienKhoByNhom`, `GetDSDonHangLGTChuaPhanBoTheoNhom`.
  - `ChiaNhomCareDon_PhanBoNgay`, `ChiaNhomCareDon_PhanBoVeNhomUser`, `ChiaUserCareDon_PhanBoNgay`, `ChiaUserCareDon_PhanBoVeUser`, `GetLogChiaSoByIdDonHang`, `GetDsNhomCareDon`, `GetDsUserCareDon`.
- `OrderLogistic/OrderLogistic.API/Controllers/CauHinhChiaSoCareDonController.cs` — `[TienIch("CauHinhChiaSoCareDonController", ...)]`, inject `ICauHinhChiaSoCareDonService`. **Cấu hình**:
  - `GET GetDetail` → quyền `CauHinhChiaSoCareDon_GetDetail`; controller tự resolve `idCHCS` qua `GetIdCHCSCareDonByChiNhanhAsync(DonViId, IdChiNhanh)` rồi gọi `GetDetailAsync(idCHCS, currentUser)` (trace mục tiêu).
  - `Save_Kieu1`, `ChangeTypeCHCSCareDon`, `Save_Kieu2_CareDonLienKet`, `Update_Kieu2_CareDonLienKet`, `BatTatUserCareDonNhomSale`, `BatTatUserCareDonAllNhomSale`, `GetDSUserCareDonLienKetNhomSale`, `DeleteMultiUserCareDonNhomSale`, `CheckApDungCSCareDonChoChiNhanh`, `LichSuThayDoiCauHinhChiaSo`.
- `Core/Core.API/Controllers/UserController.cs` (region "User care đơn", ~dòng 851) — **bật/tắt nhận dữ liệu**:
  - `POST CHCS_LayDSUserCareDon` quyền `ChiaSoCareDonController_CHCS_LayDSUserCareDon` → `UserService.CHCS_LayDSUserCareDonAsync` (trace mục tiêu).
  - `POST BatTatNhanDuLieuChoTaiKhoanCareDon` → `UserService.BatTatNhanDuLieuChoTaiKhoanCareDonAsync`.

Quyền (SBAuthorize): nhóm role `HT-CSKH, DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-TP-QL-KHO, DV-TN-QL-KHO, DV-CHIASO-CAREDON`. Các endpoint **ghi cấu hình** (`Save_Kieu1`, `Save/Update_Kieu2`, `ChangeType`, `DeleteMulti`) **không** kèm `DV-TP-QL-KHO`/`DV-TN-QL-KHO`.

## Entities (BE — `OrderLogistic.DataContext/Entity/`)
- `CauHinhChiaSoCareDon` — cấu hình gốc theo (`IdDonVi`,`IdChiNhanh`). Cờ `ApDungChiaLeadTheoNhomUserTuDong`, `ApDungChiaLeadTheoUserTuDong`, `ApDungChiaLeadNhomUserTheoSanPham`; `InitNumber`; `Type: EnumCauHinhChiaSoCareDonType`.
- `CauHinhChiaSoDoiTuongCareDon` — cây đối tượng nhận đơn (nhóm/user). `LoaiDoiTuong` (`CHCSCareDonEnum.NhomUser`...), `IdDoiTuong`, `DinhMucChiaSo` (định mức), `IdSanPham` (`;`-joined) + `KieuChonSanPham` (`KieuChonSanPhamEnum` {KhongChon, ChonTatCa, ChonSanPhamCuThe}), `IdParent`/`IdDoiTuongParent` (quan hệ nhóm→user), `IdNhomSaleLienKet`, `IsCareDon`.
- `CauHinhChiaSoCareDonLienKet` — map nhóm sale ↔ nhóm care đơn: `IdNhomSaleLienKet`, `IdNhomCareDonLienKet`, `IdSanPhamLienKet`, `KieuChonSanPham`.
- `QueueDonHangChiaCareDon` — hàng đợi chia-trễ-X-phút (`NgayThucHienChia`, `IsCompleted`, `IsProcess`).
- `ChiaSoCareDonDayCount` — bộ đếm chia theo ngày/đối tượng (dùng để chọn "xa nhất" + kiểm định mức).
- Đơn hàng `DonHangLogistic`: `IdNhomCareDon`, `UserIdCareDon` — đích của việc chia. `User.IsCareDon` (Core) — cờ nhận dữ liệu của tài khoản.
- Enum: `Libs/Models.Core/Enum/DonHangLogistic/EnumCauHinhChiaSoCareDon.cs` → `EnumCauHinhChiaSoCareDonType { Option1=1 (liên kết về nhóm), Option2=2 (trực tiếp tài khoản) }`.

## Luồng code
**Cấu hình — GetDetail** (`CauHinhChiaSoCareDonService.GetDetailAsync`, ~dòng 223):
`GetAsync(DonViId, IdChiNhanh, idCHCS)` → null thì trả **template** mặc định `Type=Option1` với các list rỗng; có thì bind thêm: đếm `SoLuongNhanVien` mỗi nhóm từ cache user chi nhánh, split `IdSanPham` → `LstIdSanPham`, set `IsChonTatCa` theo `KieuChonSanPham`, duyệt cả cấp con + gán `IdImage` (avatar) cho user, và phần liên kết.

**Chia thủ công — GetDSAllDonHangLGTChuaPhanBo** (`ChiaSoCareDonThuCongService20250620.GetDSAllDonHangLGTChuaPhanBoAsync20250620`, ~dòng 461):
Đọc `CauHinhSetting` đơn vị (`IsSuDungKhoTong`/`IsSuDungChiaSoCareDonKhoTong`/`IsSuDungChiaSoCareDon`) để quyết `idChiNhanh` (chi nhánh tổng → null = lấy tất cả, hoặc theo `gridInfo.IdChiNhanh`). Map `TrangThaiPhanBo` → `isHasNhomCareDon` (ChuaChia=false, DaChia=true). Gọi **stored proc** `SCRM_CauHinhChiaCareDon_GetDSAllDonHangLGTByNhom` (Dapper/SqlSugar `UseStoredProcedure().GetDataSetAll`) trả 3 bảng: data / totalCount / totalCountChuaPhanBo. Áp dụng **che số** (`CauHinhCheSoTheoUserLogin`): che SĐT và/hoặc nullable theo `IsCheSo`/`IsChoPhepGoiDien`. Trả `ChiaSo_DonHangLGTGridRes20250620 { Datas, TotalDonHangLGTChuaPhanBo }` + log command.

**Bật/tắt — CHCS_LayDSUserCareDon** (`Core/Core.Service/Generated/UserService.cs`, ~dòng 4942):
POST `CheckPermissionViewrReport_ELSE_WithMultiUser` (role `DV_QL_KHO`) sang Core để lọc user/nhóm theo quyền → ép `gridInfo.StrUserIdCareDon = userKhoIds`. Lấy cache user (`GetCache_User_Group_By_ChiNhanh`) + cơ cấu tổ chức (`GetCache_CoCauToChuc`), join user với nhóm role `DV_QL_KHO`, build `CHCS_GridUserCareDon` (gồm `IsCareDon`). Lọc theo keyword (tên user/nhóm), theo `StrUserIdCareDon`, theo `IsCareDon`; phân trang in-memory `Skip/Take`.
`BatTatNhanDuLieuChoTaiKhoanCareDonAsync` (~dòng 5022): check quyền tương tự + `listUserId.Contains(req.Id)`, set `User.IsCareDon`, `MarkDirty`, update trong **transaction + ExecutionStrategy**, `UserSyncData`, sau commit phát `EventUserSync`, xóa cache user, ghi `EventLichSuTacDong`.

**Engine chia tự động** (`ChiaSoCareDonThuCongService.ChiaSoCareDon`, ~dòng 956):
Guard sớm: đơn null / đã có `IdNhomCareDon` / đã có `UserIdCareDon` → return. Check cấu hình đơn vị, kho tổng thiếu chi nhánh tổng → log+return. Lọc trạng thái giao hàng có cờ `ChiaSoCareDon`; nếu trạng thái có `SoPhutCauHinhChiaSau>0` và không phải từ job (`isBoQua==false`) → insert `QueueDonHangChiaCareDon` (`NgayThucHienChia = now + X phút`) rồi return. Xác định `idChiNhanhApDungCHCSCareDon` (kho tổng → chi nhánh tổng + gom user toàn chi nhánh). Khởi tạo counter ngày. Theo `chcs.Type`:
- `Option1` → `ChiaNhomUserCareDon(...)` rồi (nếu thành công) `ChiaUserCareDon(...)`.
- `Option2` → `ChiaUserCareDonLienKetTrucTiepNhomSale(...)`.
`ChiaNhomUserCareDon` (~dòng 1139): ưu tiên nhóm care đơn **liên kết** với nhóm sale của đơn (lọc theo sản phẩm `ContainsAny`); nếu không có liên kết thì dùng cấu hình nhóm tự động (lọc sản phẩm nếu bật). Chọn counter "xa nhất" qua `ChiaSoCareDon_DayCount_GetCounter_XaNhat` trong các counter còn dưới định mức/valid, gán nhóm `DonHang_SetNhom`, tăng counter `ChiaSoCareDon_UpdateCounter`. Toàn bộ chọn-rồi-tăng được bao trong **Redis lock** (`RedisLockRetry`/`LockRelease`, key qua `GetKeyLockByLoaiDoiTuong`).
Trigger engine: consumers `ChiaSoCareDon.API/Consumer/ChiaSoCareDonQueue{1..4}Consumer.cs` (map `EventChiaSoChiaSoCareDonQueueN` → gọi `ChiaSoCareDon(donHang, null, isBoQua)`). Job nền: `JobQuetDonHangChiaCareDon.cs` (loop 10s gọi `IQueueDonHangChiaCareDonService.ResendDataReceiveDonHang`) và `JobClearDonHangChiaCareDonDone.cs`. **Lưu ý: job không chạy ở môi trường *Debug*** (`IsDebug` return sớm).

## Bug-prone
- **Idempotency là rào duy nhất chặn chia lại:** guard dựa trên `IdNhomCareDon`/`UserIdCareDon`. Code nào set 2 trường này (kể cả thủ công) phải giữ nhất quán, nếu reset nhầm → đơn bị chia lại.
- **Catch nuốt lỗi:** consumers (vd `ChiaSoCareDonQueue1Consumer`) bắt `Exception` rỗng → lỗi chia tự động **không nổi lên**; nhưng trong `ChiaNhomUserCareDon` lại `throw ex;` (mất stacktrace, và có dòng `Console.WriteLine` chết sau `throw`). Khi debug phải bật log command/ChiaSoCareDon_Log.
- **Race định mức:** chỉ đúng khi Redis lock hoạt động; lock fail/timeout có thể cho qua mà không chia (cần kiểm `RedisLockRetry` trả false).
- **Stored proc là nguồn sự thật của list đơn thủ công** (`SCRM_CauHinhChiaCareDon_GetDSAllDonHangLGTByNhom`, `..._GetDSDonHangLGTByNhom`): logic lọc đơn chưa/đã chia nằm trong SP, không phải C# — sửa filter phải đọc SP.
- **Kho tổng:** rẽ nhánh `IsSuDungKhoTong && IsSuDungChiaSoCareDonKhoTong` xuất hiện ở nhiều chỗ (GetDS thủ công + engine); thiếu `IdChiNhanhTong` làm dừng engine — kiểm cấu hình trước khi nghi code.
- **Quyền ghi vs đọc khác nhau:** endpoint Save/ChangeType/Delete loại bỏ role TP/TN-QL-KHO; khác bộ role các endpoint đọc → đừng giả định một bộ quyền chung.
- **`CHCS_LayDSUserCareDon` phân trang in-memory** sau khi join cache — danh sách lớn có thể nặng RAM; `total` đếm trước Skip/Take.
- **Nhiều bản refactor song song** (file/thư mục hậu tố `20250619/20250620/20250624`): service thủ công có cả `ChiaSoCareDonThuCongService.cs` và `...20250620.cs`. Xác minh DI đang bind bản nào (`IChiaSoCareDonService`) trước khi sửa kẻo sửa nhầm bản chết.

## File source
BE (`scrm`):
- `OrderLogistic/OrderLogistic.API/Controllers/ChiaSoCareDonController.cs`
- `OrderLogistic/OrderLogistic.API/Controllers/CauHinhChiaSoCareDonController.cs`
- `OrderLogistic/OrderLogistic.Service/Generated/CauHinhChiaSoCareDonService.cs`
- `OrderLogistic/OrderLogistic.Service/Generated/ChiaSoCareDonThuCongService.cs` (engine chia tự động + `ChiaSoCareDon`)
- `OrderLogistic/OrderLogistic.Service/Generated/ChiaSoCareDonThuCongService20250620.cs` (`GetDSAllDonHangLGTChuaPhanBoAsync20250620`)
- `OrderLogistic/OrderLogistic.Service/Generated/QueueDonHangChiaCareDonService.cs`
- `OrderLogistic/OrderLogistic.DataContext/Entity/CauHinhChiaSoCareDon.cs` (+ `...DoiTuongCareDon.cs`, `...CareDonLienKet.cs`)
- `Libs/Models.Core/Enum/DonHangLogistic/EnumCauHinhChiaSoCareDon.cs`
- `Core/Core.API/Controllers/UserController.cs` (region User care đơn)
- `Core/Core.Service/Generated/UserService.cs` (`CHCS_LayDSUserCareDonAsync`, `BatTatNhanDuLieuChoTaiKhoanCareDonAsync`)
- `ChiaSoCareDon.API/Consumer/ChiaSoCareDonQueue{1..4}Consumer.cs`
- `ChiaSoCareDon.API/Jobs/JobQuetDonHangChiaCareDon.cs`, `JobClearDonHangChiaCareDonDone.cs`
- Stored proc (DB): `SCRM_CauHinhChiaCareDon_GetDSAllDonHangLGTByNhom`, `SCRM_CauHinhChiaCareDon_GetDSDonHangLGTByNhom`

FE (`scrm_frontend`):
- `src/app/pages/chia-so-care-don/` (toàn module) — `chia-so-care-don-routing.module.ts`, `chia-so-care-don-thu-cong/`, `chia-so-care-don-cau-hinh/chia-so-care-don-cau-hinh-add/`, `bat-tat-nhan-data/`
- `src/app/services/orderLogistic/chia-so-care-don.service.ts`, `cau-hinh-chia-so-care-don.service.ts`
- `src/app/services/core/user.service.ts`
- `src/app/shared/Enums/ChiaSoCareDonEnum.enum.ts`, `src/app/shared/interfaces/ICauHinhChiaSoCareDon.ts`
