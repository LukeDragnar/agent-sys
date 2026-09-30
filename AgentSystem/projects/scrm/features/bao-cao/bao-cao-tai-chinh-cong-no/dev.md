# Dev — Báo cáo tài chính (công nợ)

> **Trùng với feature `ke-toan/cong-no`.** Báo cáo công nợ KHÔNG có controller riêng dưới module Báo cáo tổng — INDEX khung ghi "(chưa gate)". Controller thật là **`BaoCaoKeToanController`** thuộc service **Accounting**, đã có và đang chạy dưới menu Kế toán.
> Trace registry: `projects/scrm/registry/ke-toan/BaoCaoKeToanController.md`.
> Tài liệu dev gốc (gồm cả phiếu bù trừ): `features/ke-toan/cong-no/dev.md`.

## Màn hình & route (FE)
- Màn **Kế toán → Báo cáo → Báo cáo công nợ tổng hợp**. Route/Component cụ thể: xem `features/ke-toan/cong-no/dev.md` (chưa trace lại ở đây để tránh trùng).

## Endpoint / Controller (trace registry)
`BaoCaoKeToanController` (Accounting) — `[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]`; mọi endpoint `[SBAuthorize()]` **trống** (chỉ cần login, KHÔNG có permission key). Quyền thực dựa hoàn toàn vào gate cross-service Order.

| Hành động | Endpoint | Quyền |
|---|---|---|
| Báo cáo công nợ tổng hợp (131/331) | `POST api/BaoCaoKeToan/GetBaoCaoCongNoTongHop` | `[SBAuthorize()]` + gate `CheckQuyenXemReportOrder` |
| Chi tiết công nợ theo đối tượng | `POST api/BaoCaoKeToan/GetBaoCaoCongNoTongHop_ChiTiet` | như trên |

(Controller còn 2 endpoint quỹ tài khoản — thuộc feature "Báo cáo quỹ", không nằm trong feature công nợ này.)

- Stored proc: `PRC_SCRM_Accounting_ButToan_BaoCaoCongNoTongHop` (table0=data, table1=tổng), `..._BaoCaoCongNoTongHop_ChiTiet` (3 table, total ở Tables[2][0][4]).

## Entities
- **Không đọc/ghi entity nội bộ.** Toàn bộ qua stored proc. Read-only, không bắn message/event.
- DTO: `Models.Core.DTO.Accounting.Client.BaoCaoKeToan` (`BaoCaoCongNoPhaiThuModel`, `BaoCaoCongNoPhaiThuChiTietModel`, `DoiTuongRequest`, `DoiTuongResponse`).

## Luồng code
1. Gate quyền: gọi Order `GET /api/CauHinh/CheckQuyenXemReportOrder`. `Data==false` → "Bạn không có quyền xem báo cáo đơn hàng"; không xác định được → "Không xác định được quyền hạn của user".
2. Validate `LoaiTaiKhoan` ∈ {131,331} (xem bug-prone).
3. Nếu `IdNhomDoiTuong` có giá trị → `GetMaDoiTuongByNhom` (Customer+Warehouse) ra list mã; rỗng → truyền `"KHONG_CO_DU_LIEU"`.
4. Build param → chạy SP → enrich cột `TenDoiTuong` từ `GetNccAndKhByCodes` (Customer+Warehouse, vì SP ở DB khác không join tên được).

## Bug-prone / lưu ý (known-issue — KHÔNG sửa ở session này)
- **Validate loại TK lỏng** (`BaoCaoKeToanService.cs` L93-96): điều kiện viết `IsNullOrEmpty(LoaiTaiKhoan) && != "131" && != "331"` (3 vế `&&`) → chỉ true khi chuỗi RỖNG. Truyền giá trị lạ (vd "999") vẫn lọt xuống SP. Nên dùng `!= "131" && != "331"` (bỏ `IsNullOrEmpty`) hoặc `||`.
- **Log luôn chạy** (L125, L233...): `if (true || userLogin.IsSuperUser || ...)` → vế `true` thừa, nhánh log `objectLog` luôn chạy mỗi request.
- **Throw dead code** (L186-187, L256-257...): trong `catch` `return ResultWithError(...)` rồi `throw;` ngay sau → `throw` không bao giờ chạy.
- **Không có permission key riêng**: cả controller `[SBAuthorize()]` trống → ai login + có quyền xem báo cáo đơn hàng đều xem được; không tách quyền chi tiết theo báo cáo.
- `GetNccAndKhByCodes` / `GetMaDoiTuongByNhom` (`OtherService.cs`) nuốt exception trả `ResultWithError` → khi Customer/Warehouse lỗi, enrich tên có thể thiếu mà không báo rõ.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Accounting\Accounting.API\Controllers\BaoCaoKeToanController.cs` (L34/L47 GetBaoCaoCongNoTongHop + _ChiTiet, `[SBAuthorize()]` trống).
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Accounting\Accounting.Service\Generated\BaoCaoKeToanService.cs` (L93-96 validate; L125/L233 log; L186-187/L256-257 throw dead).
- BE other: `Accounting\Accounting.Service\Generated\Others\OtherService.cs:36` (GetNccAndKhByCodes), `:95` (GetMaDoiTuongByNhom).
- Stored proc (DB): `PRC_SCRM_Accounting_ButToan_BaoCaoCongNoTongHop`, `..._ChiTiet`.
- Registry node: `projects/scrm/registry/ke-toan/BaoCaoKeToanController.md`.
- Tài liệu trùng (gốc): `projects/scrm/features/ke-toan/cong-no/` (4 file).
