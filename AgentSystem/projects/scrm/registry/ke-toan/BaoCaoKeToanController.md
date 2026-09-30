---
controller: BaoCaoKeToanController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Báo cáo: (1) Báo cáo công nợ tổng hợp (phải thu 131 / phải trả 331) + chi tiết theo đối tượng; (2) Báo cáo quỹ tài khoản (sổ quỹ tiền mặt/ví/ngân hàng theo đối tượng) + chi tiết theo mã phiếu. Bộ lọc: chi nhánh, Từ/Đến ngày, loại tài khoản, loại đối tượng, nhóm đối tượng, keyword, paging."
muc_dich: "4 báo cáo kế toán đọc qua stored proc: công nợ tổng hợp (TK 131/331) + chi tiết, báo cáo quỹ tài khoản + chi tiết theo mã phiếu. Enrich tên đối tượng (KH/NCC) từ Customer+Warehouse vì SP khác DB không join được."
alias: ["báo cáo kế toán", "bao cao ke toan", "báo cáo công nợ tổng hợp", "công nợ phải thu", "công nợ phải trả", "131", "331", "báo cáo quỹ tài khoản", "sổ quỹ", "báo cáo quỹ"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [PRC_SCRM_Accounting_ButToan_BaoCaoCongNoTongHop, PRC_SCRM_Accounting_ButToan_BaoCaoCongNoTongHop_ChiTiet, PRC_SCRM_Accounting_ButToan_BaoCaoQuyTaiKhoan, PRC_SCRM_Accounting_ButToan_BaoCaoQuyTaiKhoan_ByMaPhieu]
depends: [OrderService/CauHinh/CheckQuyenXemReportOrder (gate quyền xem báo cáo), ReportService/Common/GetTimeToLog (ngưỡng log chậm), Customer/GetCustomerByCodes + GetMaKhachHangByNhom (qua OtherService), WarehouseProduct/GetNhaCungCapByCodes + GetMaNhaCungCapByNhom (qua OtherService), SqlSugar (GetDataSetAll stored proc)]
cross_module_in: []
cross_module_out: []
---

# BaoCaoKeToanController — Báo cáo kế toán (công nợ tổng hợp 131/331 + quỹ tài khoản)

> Controller view/report THUẦN ĐỌC: cả 4 endpoint đều đi **stored proc** (`_dbDapper.Ado.UseStoredProcedure().GetDataSetAll`), trả `jsonData` (serialize cả DataSet, camelCase) + `total` lấy từ table tổng. Trước khi chạy: **gate quyền** bằng cross-service Order `CheckQuyenXemReportOrder` (false → chặn), và lấy ngưỡng `GetTimeToLog` từ Report để log query chậm. Báo cáo công nợ tổng hợp còn **enrich `TenDoiTuong`** (KH/NCC) bằng cross-service Customer+Warehouse vì SP nằm DB khác không join tên được. `[SBAuthorize()]` trống ở mọi endpoint → chỉ cần login (không có permission key). `[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo công nợ tổng hợp | Lưới báo cáo công nợ (tab phải thu 131 / phải trả 331) | `POST api/BaoCaoKeToan/GetBaoCaoCongNoTongHop` | `SBAuthorize()` (any login) | public | màn báo cáo công nợ; cần `LoaiTaiKhoan` 131/331 | Gate `CheckQuyenXemReportOrder`. Validate `LoaiTaiKhoan` ∈ {131,331}. Nếu có `IdNhomDoiTuong` → resolve danh sách mã đối tượng qua `GetMaDoiTuongByNhom` (Customer+Warehouse), rỗng → truyền `"KHONG_CO_DU_LIEU"`. Build 9 param (IdDonVi, IdChiNhanh, Từ/Đến ngày, paging, Keyword, LoaiTaiKhoan, MaDoiTuongList). Chạy SP, **enrich cột `TenDoiTuong`** từ `GetNccAndKhByCodes`. `total` = `Tables[1][0][8]`. | **SP** `PRC_SCRM_Accounting_ButToan_BaoCaoCongNoTongHop` (table0=data, table1=tổng) | (a) Validate loại TK sai logic L93-96: `string.IsNullOrEmpty(LoaiTaiKhoan) && != "131" && != "331"` → khi rỗng vẫn báo lỗi nhưng khi truyền giá trị BẤT KỲ (kể cả sai) đều lọt; điều kiện không bao giờ chặn giá trị lạ. (b) `if (true \|\| ...)` L125 → nhánh log objectLog luôn chạy. (c) L186-187 `return` rồi `throw;` → throw dead code. |
| 2 | Chi tiết công nợ theo đối tượng | (drill-down 1 đối tượng) | `POST api/BaoCaoKeToan/GetBaoCaoCongNoTongHop_ChiTiet` | `SBAuthorize()` (any login) | public | click 1 dòng đối tượng | Gate quyền. Build 8 param (IdDonVi, MaDoiTuong, Từ/Đến ngày, paging, LoaiTaiKhoan, KeyWord). Chạy SP, trả jsonData; `total` = `Tables[2][0][4]` (SP trả 3 table). | **SP** `PRC_SCRM_Accounting_ButToan_BaoCaoCongNoTongHop_ChiTiet` | `if (true \|\| ...)` L233; `return` rồi `throw;` L256-257 (dead code). |
| 3 | Báo cáo quỹ tài khoản | Lưới sổ quỹ (tiền mặt/ví/ngân hàng) theo đối tượng | `POST api/BaoCaoKeToan/GetBaoCaoQuyTaiKhoan` | `SBAuthorize()` (any login) | public | màn báo cáo quỹ | Gate quyền. Build 10 param (IdDonVi, IdChiNhanh, Từ/Đến ngày, paging, Keyword, **LoaiDoiTuong**, **TypeView**). Chạy SP, trả jsonData; `total` = `Tables[1][0][0]`. | **SP** `PRC_SCRM_Accounting_ButToan_BaoCaoQuyTaiKhoan` | `if (true \|\| ...)` L302; `return`+`throw;` L325-326. |
| 4 | Chi tiết quỹ theo mã phiếu | (drill-down 1 đối tượng/phiếu) | `POST api/BaoCaoKeToan/GetBaoCaoQuyTaiKhoan_ChiTiet` | `SBAuthorize()` (any login) | public | click 1 dòng | Gate quyền. Build 12 param (thêm `IdDoiTuong`, `MaDoiTuong` so với #3). Chạy SP `..._ByMaPhieu`, trả jsonData; `total` = `Tables[1][0][0]`. | **SP** `PRC_SCRM_Accounting_ButToan_BaoCaoQuyTaiKhoan_ByMaPhieu` | `if (true \|\| ...)` L373; `return`+`throw;` L395-396. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Gate quyền (mọi endpoint):** gọi Order `GET /api/CauHinh/CheckQuyenXemReportOrder`. `Data==false` → trả lỗi "Bạn không có quyền xem báo cáo đơn hàng"; không xác định được → "Không xác định được quyền hạn của user". → KHÔNG có permission key riêng, quyền dựa hoàn toàn vào cấu hình bên Order.
- **Loại tài khoản (#1, #2):** 131 = công nợ phải thu (khách hàng), 331 = công nợ phải trả (nhà cung cấp). (#1 còn validate — nhưng logic điều kiện lỏng, xem known-issue.)
- **Nhóm đối tượng (#1):** `IdNhomDoiTuong` có giá trị → resolve về list mã KH+NCC (`GetMaDoiTuongByNhom`), nếu rỗng truyền sentinel `"KHONG_CO_DU_LIEU"` để SP không trả gì.
- **Log query chậm:** `GetTimeToLog` (Report) trả ngưỡng giây; thời gian chạy SP ≥ ngưỡng → `LogHelper.DebugAsync(..., "log_report")`.
- **Enrich tên đối tượng (chỉ #1):** sau khi có data, gom `MaDoiTuong` distinct → `GetNccAndKhByCodes` (Customer+Warehouse) → map thêm cột `TenDoiTuong` (nếu trùng cả KH lẫn NCC → "KH: ... - NCC: ...").

## Quan hệ
- **Gọi sang:** Order `CauHinh/CheckQuyenXemReportOrder`; Report `Common/GetTimeToLog`; Customer + Warehouse qua `OtherService` (`GetNccAndKhByCodes` = `GetCustomerByCodes`+`GetNhaCungCapByCodes`; `GetMaDoiTuongByNhom` = `GetMaKhachHangByNhom`+`GetMaNhaCungCapByNhom`).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — toàn bộ qua 4 stored proc. KHÔNG ghi gì (report read-only).
- **Ripple / đồng bộ:** không bắn message/event; chỉ đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/BaoCaoKeToanController.cs`
  - L34/L47/L60/L73: cả 4 endpoint `[SBAuthorize()]` trống → chỉ cần login, không kiểm permission key. Quyền thực = `CheckQuyenXemReportOrder` (cấu hình Order).
- BE `Accounting/Accounting.Service/Generated/BaoCaoKeToanService.cs`
  - L93-96: điều kiện validate `LoaiTaiKhoan` viết bằng `&&` ba vế `IsNullOrEmpty && !="131" && !="331"` → chỉ true khi chuỗi rỗng; truyền giá trị lạ (vd "999") vẫn lọt xuống SP. Nên dùng `!= "131" && != "331"` (bỏ IsNullOrEmpty hoặc dùng `||`).
  - L125, L233, L302, L373: `if (true || userLogin.IsSuperUser || ...)` → vế `true` thừa, nhánh log `objectLog` luôn chạy (objectLog luôn được set = LogCommandV2).
  - L186-187, L256-257, L325-326, L395-396: trong `catch` `return ResultWithError(...)` rồi `throw;` ngay sau → `throw` là dead code (không bao giờ chạy).
  - Cả 4 method `async` nhưng thân chính (`GetDataSetAll`) là call SYNC của SqlSugar; chỉ các call cross-service là await — chấp nhận được, lưu ý hiệu năng SP nặng.
- BE `Accounting/Accounting.Service/Generated/Others/OtherService.cs`
  - `GetNccAndKhByCodes` (L36) gọi song song Customer+Warehouse, nuốt exception trả ResultWithError (L89-92); `GetMaDoiTuongByNhom` (L95) tương tự.
- DTO: `Models.Core.DTO.Accounting.Client.BaoCaoKeToan` (`BaoCaoCongNoPhaiThuModel`, `BaoCaoCongNoPhaiThuChiTietModel`, `ReqBaoCaoQuyTaiKhoanBase`, `ReqBaoCaoQuyTaiKhoanDetail`, `DoiTuongRequest`, `DoiTuongResponse`).
