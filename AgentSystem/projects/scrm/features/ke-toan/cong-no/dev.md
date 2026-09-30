# Dev — Công nợ (Bù trừ công nợ + Báo cáo công nợ)

> Service: **Accounting** (BE .NET) + cross-service Customer / WarehouseProduct / Order / Report / MISA. Controller `PhieuBuTruCongNoController` (lập/quản lý bù trừ) + `BaoCaoKeToanController` (phần báo cáo công nợ 131/331). Trace registry: `registry/ke-toan/PhieuBuTruCongNoController.md`, `registry/ke-toan/BaoCaoKeToanController.md`. Chỉ dành cho dev.

## Màn hình & route
- **Kế toán → Phiếu bù trừ công nợ**: popup lập phiếu (Lấy chứng từ → chọn dòng → Lưu) + lưới danh sách + chi tiết + xóa.
- **Kế toán → Báo cáo → Báo cáo công nợ tổng hợp** (131/331) + drill-down chi tiết theo đối tượng.

## Endpoint / Controller (trace registry)

**`PhieuBuTruCongNoController`** (`auth: public`; `[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]`):
| Hành động | Endpoint | Quyền |
|---|---|---|
| Lấy chứng từ phải thu/trả | `POST api/PhieuBuTruCongNo/GetChungTu` | `[SBAuthorize()]` trống |
| Lập phiếu bù trừ | `POST api/PhieuBuTruCongNo/InsertPhieuBuTru` | `[SBAuthorize()]` trống |
| Danh sách | `POST api/PhieuBuTruCongNo/TimTheoDieuKienAsync` | `PhieuBuTruCongNo_TimTheoDieuKien` |
| Chi tiết | `GET api/PhieuBuTruCongNo/ViewChiTiet?idPhieuBuTru=` | `[SBAuthorize()]` trống |
| Xóa | `POST api/PhieuBuTruCongNo/XoaPhieu?idPhieuBuTru=` | `[SBAuthorize()]` trống |

**`BaoCaoKeToanController`** — phần báo cáo công nợ (cả 4 endpoint `[SBAuthorize()]` trống; gate bằng cross-service Order `CheckQuyenXemReportOrder`):
| Hành động | Endpoint | Nguồn |
|---|---|---|
| Báo cáo công nợ tổng hợp (131/331) | `POST api/BaoCaoKeToan/GetBaoCaoCongNoTongHop` | SP `PRC_SCRM_Accounting_ButToan_BaoCaoCongNoTongHop` |
| Chi tiết công nợ theo đối tượng | `POST api/BaoCaoKeToan/GetBaoCaoCongNoTongHop_ChiTiet` | SP `..._BaoCaoCongNoTongHop_ChiTiet` |

(Phần báo cáo quỹ của cùng controller này thuộc feature **Báo cáo quỹ**.)

## Entities
- `BuTruCongNo` + `BuTruCongNoChiTiet`; `AccountingRollback` (cờ IsCustomer/IsWarehouse cho job khôi phục); `PhieuKeToanHistory` (log xóa); `DataReceiveTaoPhieu` (queue MISA).
- Báo cáo công nợ: **không đọc/ghi entity nội bộ** — toàn bộ qua stored proc.
- DTO bù trừ: `Models.Core.DTO.Accounting.Client.BuTruCongNo` (BuTruRequest, BuTruCongNoFilterDto, GetChungTuRequest, ChungTuButruViewModel). DTO báo cáo: `Models.Core.DTO.Accounting.Client.BaoCaoKeToan` (BaoCaoCongNoPhaiThuModel, ...ChiTietModel, DoiTuongRequest/Response).

## Luồng code
- **GetChungTu** (`BuTruCongNoService.cs`): validate `MaDoiTuong`, `NgayBuTru`, ít nhất 1 trong TkPhaiThu/TkPhaiTra → `Task.WhenAll(GetCongNoPhaiThu, GetCongNoPhaiTra)` → gộp `ChungTuButruViewModel`. Phân loại theo `KieuLoai`: `"ChungTuPhaiThu"` (KH) / `"ChungTuPhaiTra"` (NCC).
- **InsertPhieuBuTru** (`BuTruCongNoService.cs`): `ValidateBuTruRequest` → sinh/chống trùng `SoChungTu` → tạo phiếu + N chi tiết + `AccountingRollback`. Trong EF transaction (BeginTransaction/ExecutionStrategy): insert + `Task.WhenAll(GhiNhanBuTruCongNoNCC, GhiNhanBuTruCongNoKH)`; cả hai OK → commit + rollback=ThanhCong; fail → set Rollback + không commit. Thành công → MISA + `DataReceiveTaoPhieu` (`IsTaoTuDong` → Status=Pending; thủ công → Received); `IsTaoTuDong` → gắn `IdPhieuLienQuan` vào phiếu mua/bán. Bút toán: `ButToan_PhieuBuTru_Save` riêng.
- **XoaPhieu** (`BuTruCongNoService.cs:272`): tìm theo id+DonVi+CN; `IsTaoTuDong==true` → chặn. Transaction: `PhieuKeToanHistory(DELETE)` → hard delete phiếu + chi tiết → rollback công nợ từng dòng (`ChungTuPhaiTra`→Warehouse, `ChungTuPhaiThu`→Customer); OK → commit.
- **Báo cáo công nợ** (`BaoCaoKeToanService.cs`): gate `CheckQuyenXemReportOrder` → resolve nhóm đối tượng (`GetMaDoiTuongByNhom`) → build param → SP `GetDataSetAll` → enrich `TenDoiTuong` qua `GetNccAndKhByCodes`.

## Bug-prone / lưu ý
- **Lập phiếu bù trừ (ghi tài chính) không có permission key** (`PhieuBuTruCongNoController.cs`): chỉ `TimTheoDieuKienAsync` (L72) có key; `GetChungTu`/`InsertPhieuBuTru`/`ViewChiTiet`/`XoaPhieu` đều `[SBAuthorize()]` trống → rủi ro phân quyền.
- **Task.Run trong EF transaction** (`BuTruCongNoService.cs:733-746`): `GhiNhanBuTruCongNoNCC`/`KH` chạy `Task.Run(...)` + `Task.WhenAll` **bên trong** `BeginTransactionAsync`. Cần xác nhận 2 call này thuần HTTP (không chạm `_accountingDataContext`) để tránh DbContext concurrency.
- **Xóa phiếu = hard delete** (`BuTruCongNoService.cs:272`); **không thấy gọi xóa bút toán** đã sinh — cần kiểm `ButToan_Xoa_IdPhieu(maChungTu=BT)` có được gọi ở luồng khác không. `cần product xác nhận`.
- **Báo cáo — validate loại TK lỏng** (`BaoCaoKeToanService.cs:93-96`): điều kiện `IsNullOrEmpty(LoaiTaiKhoan) && !="131" && !="331"` chỉ true khi rỗng → truyền giá trị lạ ("999") vẫn lọt xuống SP. Nên `!= "131" && != "331"`.
- **Báo cáo — `if (true || ...)`** (L125, L233): vế `true` thừa → nhánh log objectLog luôn chạy.
- **Báo cáo — dead code** (L186-187, L256-257): `return ResultWithError(...)` rồi `throw;` → throw không bao giờ chạy.
- **Báo cáo không có permission key riêng** — quyền dựa hoàn toàn vào `CheckQuyenXemReportOrder` (cấu hình Order).
- **OtherService nuốt exception** (`OtherService.cs:89-92`): `GetNccAndKhByCodes`/`GetMaDoiTuongByNhom` gọi song song Customer+Warehouse, lỗi → ResultWithError (tên đối tượng có thể trống).

## File source (path:line)
- BE controllers: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Accounting\Accounting.API\Controllers\PhieuBuTruCongNoController.cs` (L72 key; L41-52 dead `TaoPhieuBuTru`), `BaoCaoKeToanController.cs` (L34/L47 báo cáo công nợ).
- BE service bù trừ: `...\Accounting.Service\Generated\BuTruCongNoService.cs` (L272 XoaPhieu, L733-746 Task.Run, L848-851 Status MISA).
- BE service báo cáo: `...\Accounting.Service\Generated\BaoCaoKeToanService.cs` (L93-96 validate, L125/L233 if true, L186-187/L256-257 dead throw); `...\Others\OtherService.cs:36` (GetNccAndKhByCodes), `:95` (GetMaDoiTuongByNhom).
- SP: `PRC_SCRM_Accounting_ButToan_BaoCaoCongNoTongHop`, `..._ChiTiet` (DB, không trong repo).
- Cross-service: Customer `GetCongNoPhaiThu`/`GhiNhanBuTruCongNoKH`/`GetCustomerByCodes`/`GetMaKhachHangByNhom`; Warehouse `GetCongNoPhaiTra`/`GhiNhanBuTruCongNoNCC`/`GetNhaCungCapByCodes`/`GetMaNhaCungCapByNhom`; Order `CauHinh/CheckQuyenXemReportOrder`; Report `Common/GetTimeToLog`.
