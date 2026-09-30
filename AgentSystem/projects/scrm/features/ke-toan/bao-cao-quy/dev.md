# Dev — Báo cáo quỹ (Báo cáo quỹ tài khoản)

> Service: **Accounting** (BE .NET) + cross-service Order (gate quyền) / Report (ngưỡng log) / Customer + WarehouseProduct (tên đối tượng). Controller `BaoCaoKeToanController` (2 endpoint quỹ). Trace registry: `registry/ke-toan/BaoCaoKeToanController.md`. Chỉ dành cho dev.

> Cùng controller `BaoCaoKeToanController` còn 2 endpoint báo cáo công nợ 131/331 thuộc feature **Công nợ** (`cong-no/dev.md`). File này chỉ phụ trách 2 endpoint **quỹ tài khoản**.

## Màn hình & route
- **Kế toán → Báo cáo → Báo cáo quỹ tài khoản** + drill-down chi tiết theo mã phiếu.

## Endpoint / Controller (trace registry)
**`BaoCaoKeToanController`** (`auth: public`; cả 4 endpoint `[SBAuthorize()]` trống; `[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]`):
| Hành động | Endpoint | Nguồn (SP) |
|---|---|---|
| Báo cáo quỹ tài khoản | `POST api/BaoCaoKeToan/GetBaoCaoQuyTaiKhoan` | `PRC_SCRM_Accounting_ButToan_BaoCaoQuyTaiKhoan` (total = `Tables[1][0][0]`) |
| Chi tiết quỹ theo mã phiếu | `POST api/BaoCaoKeToan/GetBaoCaoQuyTaiKhoan_ChiTiet` | `PRC_SCRM_Accounting_ButToan_BaoCaoQuyTaiKhoan_ByMaPhieu` (total = `Tables[1][0][0]`) |

- Báo cáo quỹ build 10 param (IdDonVi, IdChiNhanh, Từ/Đến ngày, paging, Keyword, **LoaiDoiTuong**, **TypeView**); chi tiết build 12 param (thêm `IdDoiTuong`, `MaDoiTuong`).
- Controller view/report thuần đọc: đi stored proc qua `_dbDapper.Ado.UseStoredProcedure().GetDataSetAll`, trả `jsonData` (serialize DataSet, camelCase) + `total`.

## Entities
- **Không đọc/ghi entity nội bộ** — toàn bộ qua stored proc. KHÔNG ghi gì (read-only).
- DTO: `Models.Core.DTO.Accounting.Client.BaoCaoKeToan` (`ReqBaoCaoQuyTaiKhoanBase`, `ReqBaoCaoQuyTaiKhoanDetail`, `DoiTuongRequest`, `DoiTuongResponse`).

## Luồng code
- **GetBaoCaoQuyTaiKhoan** (`BaoCaoKeToanService.cs`): gate `CheckQuyenXemReportOrder` (Order) → build 10 param → SP `GetDataSetAll` → trả jsonData + total. Lấy ngưỡng `GetTimeToLog` (Report) để log query chậm.
- **GetBaoCaoQuyTaiKhoan_ChiTiet**: gate quyền → build 12 param (thêm IdDoiTuong/MaDoiTuong) → SP `..._ByMaPhieu`.
- Khác với báo cáo công nợ tổng hợp: báo cáo quỹ **không** enrich `TenDoiTuong` qua cross-service (enrich chỉ ở báo cáo công nợ tổng hợp #1).

## Bug-prone / lưu ý
- **Không có permission key riêng** (`BaoCaoKeToanController.cs:60/73`): `[SBAuthorize()]` trống → chỉ cần login. Quyền thực = `CheckQuyenXemReportOrder` (cấu hình Order). Không có quyền đó → chặn.
- **`if (true || ...)`** (`BaoCaoKeToanService.cs:302`, `:373`): vế `true` thừa → nhánh log objectLog luôn chạy.
- **Dead code** (`...Service.cs:325-326`, `:395-396`): `return ResultWithError(...)` rồi `throw;` → throw không bao giờ chạy.
- **SP chạy SYNC** (`GetDataSetAll`): method `async` nhưng thân chính là call sync của SqlSugar; chỉ call cross-service là await → lưu ý hiệu năng khi SP nặng (khoảng ngày dài).
- **Xuất Excel báo cáo quỹ**: registry không liệt kê endpoint export cho báo cáo quỹ → `cần product xác nhận` xem có hỗ trợ xuất không.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Accounting\Accounting.API\Controllers\BaoCaoKeToanController.cs:60` (GetBaoCaoQuyTaiKhoan), `:73` (_ChiTiet).
- BE service: `...\Accounting.Service\Generated\BaoCaoKeToanService.cs:302` (if true #3), `:325-326` (dead throw), `:373` (if true #4), `:395-396` (dead throw).
- SP (DB, không trong repo): `PRC_SCRM_Accounting_ButToan_BaoCaoQuyTaiKhoan`, `PRC_SCRM_Accounting_ButToan_BaoCaoQuyTaiKhoan_ByMaPhieu`.
- Cross-service: Order `CauHinh/CheckQuyenXemReportOrder` (gate); Report `Common/GetTimeToLog` (ngưỡng log).
