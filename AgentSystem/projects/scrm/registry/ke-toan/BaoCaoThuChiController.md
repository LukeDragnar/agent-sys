---
controller: BaoCaoThuChiController
module: ke-toan
service: Branch
man_hinh_ui: "Quỹ / Thu chi > Báo cáo thu chi: bảng chi tiết thu-chi theo khoảng ngày, lọc theo chi nhánh / loại quỹ / từ khóa, có phân trang."
muc_dich: "Trả báo cáo chi tiết thu-chi (1 endpoint) bằng stored procedure, lọc theo đơn vị/chi nhánh/khoảng ngày/loại quỹ/keyword/typeView, có phân trang; có log thời gian chạy store nếu vượt ngưỡng cấu hình."
alias: ["báo cáo thu chi", "report thu chi", "chi tiết thu chi", "thống kê thu chi"]
auth: public
state_machine: []
deprecated: false
entities: [ThuChi, ThuChiDonHang]
stored_procs: [PRC_SCRM_Major_ThuChi_BaoCaoChiTietThuChi]
depends: [SqlSugar (Dapper store), Report/Common/GetTimeToLog (ngưỡng log thời gian chạy store)]
cross_module_in: []
cross_module_out: []
---

# BaoCaoThuChiController — Báo cáo chi tiết thu chi

> Controller report 1 endpoint, đi qua **stored procedure** `PRC_SCRM_Major_ThuChi_BaoCaoChiTietThuChi` (SqlSugar). Auth thực tế chỉ `[SBAuthorize()]` trống (chỉ cần login). Trả `DataSet` serialize JSON (camelCase) + tổng số bản ghi. Có cơ chế log query store nếu thời gian chạy ≥ ngưỡng lấy từ Report service.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo chi tiết thu chi | Lưới báo cáo / Lọc | `POST api/BaoCaoThuChi/GetBaoCaoThuChiDetail` | `SBAuthorize()` (any login) | public | màn Báo cáo thu chi | Gọi SP với params IdDonVi/IdChiNhanh/TuNgay/DenNgay/PageIndex/PageSize/Keyword/LoaiQuy/TypeView. Trả `DataSet` → JSON (CamelCase) + total từ `Tables[1].Rows[0][0]`. Log thời gian chạy store nếu ≥ ngưỡng (GetTimeToLog). | **SP** `PRC_SCRM_Major_ThuChi_BaoCaoChiTietThuChi` | `BaoCaoThuChiService.cs:63` điều kiện log `if (true || ...)` luôn true → luôn build objectLog. |

## Quan hệ
- **Gọi sang:** Report `Common/GetTimeToLog` (lấy ngưỡng giây để log store); SqlSugar `_dbDapper` chạy SP.
- **Dữ liệu (entities):** đọc qua SP (nguồn `ThuChi`/`ThuChiDonHang`), không EF trực tiếp.
- **Ripple / đồng bộ:** không side-effect; chỉ đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- `BaoCaoThuChiController.cs:36` — `[SBAuthorize()]` trống: chỉ cần login, không phân quyền theo key.
- `BaoCaoThuChiService.cs:63` — `if (true || userLogin.IsSuperUser ...)` luôn đúng → luôn tạo `objectLog` (log query) bất kể role.
- `BaoCaoThuChiService.cs:65,67` — gọi `LogCommandV2` 2 lần (1 lần có dấu cách thừa cuối tên SP `"... BaoCaoChiTietThuChi "`) → log dư.
- `BaoCaoThuChiService.cs:88` — có `throw;` chết sau `return` trong catch (không reachable).
