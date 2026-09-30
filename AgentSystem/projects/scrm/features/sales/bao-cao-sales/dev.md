# Dev — Báo cáo Sales

> Service: **Report** (BE .NET). 2 controller chính: `ContactReportController` (báo cáo chia số) + `BaoCaoOnlineController` (hub báo cáo online lớn nhất, gồm các báo cáo sale). Toàn bộ THUẦN ĐỌC qua stored proc DB SCRM_Report. Gốc registry: `registry/bao-cao/`. Chỉ dành cho dev.

## Màn hình & route (FE)
- FE module báo cáo: `scrm_frontend/src/app/pages/Report/`. Route con xác nhận trong routing module của Report.
- Báo cáo online (sale/MKT/doanh số) + báo cáo chia số nằm trong nhóm màn báo cáo.

## Endpoint / Controller (trace registry)

### `ContactReportController` (1 endpoint — chia số)
| Hành động | Endpoint | Quyền (key) | Nguồn |
|---|---|---|---|
| Báo cáo chia số contact theo ngày | `POST api/ContactReport/BaoCaoChiaSo` | `ContactReportController_BaoCaoChiaSo` | **SP** `PRC_SCRM_ReportBaoCaoChiaSo` |
- `[TienIchAttribute(..., TI_Report_Chung)]`; KHÔNG `[SBAuthorize]` cấp controller; quyền chỉ ở method.

### `BaoCaoOnlineController` (~40 endpoint — các báo cáo sale)
`[TienIchAttribute(..., TI_Report_Online)]`; KHÔNG `[SBAuthorize]` cấp controller → auth **mixed**. Các báo cáo sale chính (mỗi báo cáo thường + 1 `ExportExcel*`):
| Báo cáo sale | Endpoint | SP |
|---|---|---|
| Công việc sale | `ReportCongViecSale` (+Export) | `SCRM_Report_ReportCongViecSale` |
| Tối ưu sale | `ReportToiUuSale` (+Export) | `SCRM_Report_ReportToiUuSale` (+ `_TongDai_MyHub`, `_TongDai_OmiCall` enrich) |
| Bảng xếp hạng sale | `ReportBangXepHangSale` | `SCRM_Report_ReportBangXepHang` (ép LoaiNhanVien=1; dùng chung SP với MKT) |
| Phong thần bảng sale / nhóm sale | `ReportPhongThanBangSale` / `ReportPhongThanBangNhomSale` | `SCRM_Report_ReportPhongThanBang_Sale` / `_NhomSale` (OpenNewConnectionOrDefault) |
| Tỉ lệ chốt đơn SP | `ReportTyLeChotDonSanPham` (+Export) | `SCRM_Report_ReportTyLeChotDonSanPham` (+ `_SoContact`) |
| KH cũ/mới | `ReportKhachHangCuMoi` (+Export) | `SCRM_Report_ReportKhachHangCuMoi` |
| Sale theo nguồn landing | `ReportSaleTheoNguonLanding` (+Export) | `SCRM_Report_ReportDoanhSoTheoNguon` |
| Doanh số tổng quan / theo ngày / giao vận | `ReportDoanhSoTongQuan` / `...TheoNgay` / `...SoLieuGiaoVanSale` | `SCRM_Report_ReportDoanhSoTongQuan` / `ActionDashBoard_BaoCaoTheoNgay` / `...SoLieuGiaoVan` |
| Tổng hợp bán hàng | `ReportTongHopBanHang` (+Export) | `PRC_SCRM_Contact_BangTongHopBanHang` |
| Nhãn/trạng thái contact + danh sách contact | `ReportNhanContact` / `ReportTrangThaiContact` / `TimDanhSachContactTheoNhan(+Total)` / `...TheoTrangThai` | `SCRM_Report_ReportNhanContact` / `..._Final` / `PRC_SCRM_Contact_Basic_GetByIdNhan(+Total)` / `...IdTrangThai` |

> Lưu ý overlap: nhiều báo cáo trong BaoCaoOnline dùng chung cho cả Sales và Marketing (phong thần bảng MKT, công việc MKT, bảng xếp hạng MKT...). Feature "Báo cáo Sales" chỉ lấy phần sale; phần MKT thuộc Báo cáo Marketing.

## Entities / nguồn dữ liệu
- **Không đọc/ghi entity** — gần như 100% qua stored proc DB `SCRM_Report` (read-only).
- Gate chung: `CommonService.CheckCauHinhKhoangThoiGianChoPhepTimKiem` (giới hạn ngày), `GetStatusShowQueryReport` (log query), `GetCheckPermissionViewDropdownListNhomVaUser` (lọc cơ cấu tổ chức).
- Ngoại lệ: `GetCauHinhOrderAsync` gọi HTTP OrderLogistic (không SP) — không thuộc báo cáo sale.

## Luồng code
1. Controller validate khoảng ngày → ép chi nhánh user nếu không admin/super/test/CSKH → lọc nhóm/user sale theo quyền.
2. Build param → `db.Ado.UseStoredProcedure().GetDataSetAll(...)` trên `_dbDapper.CopyNew()` (hoặc `OpenNewConnectionOrDefault` cho phong thần bảng/giao vận — store nặng).
3. Serialize DataSet → trả jsonData + total.
4. Export: build lại cùng SP → GemBox xuất .xlsx; lỗi `Service_Unavailable` → 503, lỗi khác → 500.

## Bug-prone / lưu ý (path:line — KHÔNG sửa code ở session này)
- `BaoCaoOnlineController.cs:34` — `ReportDoanhSoTheoNgay` dùng `[SBAuthorize]` **không key** (key cũ comment L33) → chỉ chặn theo auth chung.
- `BaoCaoOnlineController.cs:127` — `ExportExcelReportDoanhSoTheoGiaoVan` **dùng chung permission key** với báo cáo (không audit riêng export).
- `BaoCaoOnlineController.cs:946` — `ExportExcelReportComboSanPham` lỗi trả `BadRequest(Message)` — KHÁC chuẩn export khác (503/500).
- `BaoCaoOnlineController.cs:450` — `ReportViewXuHuong` try/catch chỉ gán err rồi rethrow (vô nghĩa).
- `ReportService.cs:14948` (`ReportTongHopBanHang`) & `:15327/15337` (`ReportThaoTacNhapSo`) — `if (true || ...)` → **LUÔN** gọi `LogCommandV2` (điều kiện vô hiệu hóa) → ghi log mỗi request, overhead.
- `ReportService.cs:8286/8413/8556` — phong thần bảng Mkt/Sale/NhomSale dùng `OpenNewConnectionOrDefault(..., true)` (mở connection riêng store nặng).
- `BaoCaoOnlineController` — 2 widget `GetCountDonHangLogisticByDonVi` / `GetCountContactByDonVi` là `[AllowAnonymous]` (internal/cross-service, không UserLogin).
- `ExportExcelReportCombo` nhận `List<ReportComboDto>` từ FE và **KHÔNG re-query SP** — build Excel từ payload client (số liệu phụ thuộc FE gửi đúng).
- `ContactReportController.cs` — không `[SBAuthorize]` cấp controller; quyền chỉ ở method `BaoCaoChiaSoAsync`; code ADO cũ comment từ L93 (`ContactReportService.cs`).
- Phân quyền report nhìn chung **lỏng cấp controller** (không khóa controller-level) → dựa hoàn toàn vào key per-method + gate ngày/cơ cấu tổ chức.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoOnlineController.cs`, `...\ContactReportController.cs`.
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\ReportService.cs` (L14948, L15327/15337, L8286/8413/8556...), `...\ContactReportService.cs` (L84-85).
- Registry nodes: `projects/scrm/registry/bao-cao/BaoCaoOnlineController.md`, `projects/scrm/registry/bao-cao/ContactReportController.md`.
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\Report\`.
- Stored procs: DB SCRM_Report (không trong repo source) — danh sách SP đầy đủ ở frontmatter node registry.
