# Dev — Báo cáo theo nhãn / phí sàn

> Service: **Report** (BE .NET). Hai báo cáo, **hai controller khác nhau** (gom 1 feature vì cùng phục vụ phân tích theo nhãn/sàn). Báo cáo THUẦN ĐỌC qua stored proc, DB `SCRM_Report`. Chỉ dành cho dev.
> Trace registry: `projects/scrm/registry/bao-cao/BaoCaoCuaHangController.md` (nhãn, #10) + `projects/scrm/registry/bao-cao/BaoCaoPhiSanController.md` (phí sàn).

## Màn hình & route (FE)
- **Báo cáo theo nhãn:** cụm màn **Báo cáo cửa hàng** (menu "Báo cáo theo nhãn"). Route/Component: **cần xác nhận từ FE**.
- **Báo cáo phí sàn TMĐT:** màn **Phí sàn TMĐT** trong báo cáo. Route/Component: **cần xác nhận từ FE**.

## Endpoint / Controller (trace registry)

### A. Báo cáo theo nhãn — `BaoCaoCuaHangController` (`[TienIchAttribute(..., TI_Report_CuaHang)]`)
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Báo cáo theo nhãn | `POST api/BaoCaoCuaHang/ReportTheoNhan` | `BaoCaoCuaHangController_ReportNhan` (LỆCH: endpoint `ReportTheoNhan` ≠ key `...ReportNhan`) |

- Stored proc: `SCRM_Report_ReportNhanContact_Offline`. Dùng chung `IOrderReportService` với `OrderReportController`.

### B. Báo cáo phí sàn TMĐT — `BaoCaoPhiSanController` (`[TienIchAttribute(..., TI_Report_Chung)]`)
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Lưới phí sàn | `POST api/BaoCaoPhiSan/GetBaoCaoPhiSanTMDT` | `BaoCaoPhiSanController_GetBaoCaoPhiSanTMDT` |
| Chi tiết 1 đơn | `POST api/BaoCaoPhiSan/GetBaoCaoPhiSanTMDT_ChiTiet` | `BaoCaoPhiSanController_GetBaoCaoPhiSanTMDT_ChiTiet` |
| Chi tiết — sandbox (đơn SCRM gốc) | `POST api/BaoCaoPhiSan/GetBaoCaoPhiSanTMDT_ChiTiet_Sandbox` | `BaoCaoPhiSanController_GetBaoCaoPhiSanTMDT_ChiTiet_Sandbox` |

- Vai trò (phí sàn): DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, HT-CSKH, DV-QLDV.
- Stored proc: `PRC_SCRM_OrderLogistic_DonHangLogistic_BaoCaoPhiSanTMDT` (+ `_ChiTiet`, `_ChiTiet_Sandbox`). SP đọc dữ liệu `DonHangLogistic` (đơn sàn TMĐT) nhưng truy vấn qua Report-service.

## Entities
- **Không đọc/ghi entity nội bộ.** Cả hai 100% qua stored proc DB `SCRM_Report`. Read-only, không bắn message/event.

## Luồng code
**A. Báo cáo theo nhãn:** gate khoảng ngày + ép chi nhánh user khi không admin → chạy SP `SCRM_Report_ReportNhanContact_Offline` (`OrderReportService.cs` L3391).

**B. Phí sàn (lưới):**
1. Gate khoảng ngày `CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn.
2. **Ticket lock** `DoProcessTicket(SO_USER_READ_REPORT_BAO_CAO_PHI_SAN_TMDT, ...)` == false → status 250 "Chưa đến lượt truy cập"; `finally` luôn `ReleaseLockedTicket`.
3. Build param chi nhánh/sàn/gian hàng/trạng thái/keyword/ngày/paging → chạy SP → jsonData + total (`ds.Tables[2].Rows[0]["TotalRecord"]`).
4. Chi tiết: bắt buộc `DonHangLogisticId` (≠ empty) → chạy SP chi tiết tương ứng.

## Bug-prone / lưu ý (known-issue — KHÔNG sửa ở session này)
**Báo cáo theo nhãn (`BaoCaoCuaHangController.cs`):**
- L95-102: endpoint là `ReportTheoNhan` nhưng permission key là `BaoCaoCuaHangController_ReportNhan` → **lệch tên endpoint/key**, dễ nhầm khi cấu hình quyền.

**Phí sàn (`Report/Report.Service/Generated/Ecomm/BaoCaoPhiSanService.cs`):**
- L159-161 `GetBaoCaoPhiSanTMDT_ChiTiet`: `if (true || ...)` + `LogCommandV2(db, " ", listParam)` → điều kiện luôn true + tên SP log là chuỗi rỗng " " → ghi log lệnh rác mỗi lần gọi.
- L105 + L108 `GetBaoCaoPhiSanTMDT`: `LogCommandV2(...)` gọi 2 lần (1 trong if super/CSKH, 1 vô điều kiện) → log trùng/thừa.
- `reportLog.SoGiayChayStore` (L118) gate log nhưng không gán thời gian chạy thực (khởi tạo rỗng) → điều kiện log debug gần như không kích hoạt đúng (dead/khả nghi).
- `GetBaoCaoPhiSanTMDT_ChiTiet` (L177): `throw;` sau `return` trong catch → unreachable code.
- Total đọc cứng từ `ds.Tables[2]` (L110) → SP phải trả ≥ 3 result set theo đúng thứ tự, dễ vỡ nếu SP đổi.

## File source (path:line)
- BE controller nhãn: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoCuaHangController.cs:95` (ReportTheoNhan).
- BE service nhãn: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\OrderReportService.cs:3391` (ReportNhanContact_Offline).
- BE controller phí sàn: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoPhiSanController.cs`.
- BE service phí sàn: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\Ecomm\BaoCaoPhiSanService.cs:105` / `:159` / `:177`.
- Stored proc (DB): `SCRM_Report_ReportNhanContact_Offline`, `PRC_SCRM_OrderLogistic_DonHangLogistic_BaoCaoPhiSanTMDT` (+ `_ChiTiet`, `_ChiTiet_Sandbox`).
- Registry node: `projects/scrm/registry/bao-cao/BaoCaoCuaHangController.md`, `projects/scrm/registry/bao-cao/BaoCaoPhiSanController.md`.
