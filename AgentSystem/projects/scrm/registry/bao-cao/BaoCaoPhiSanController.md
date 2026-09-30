---
controller: BaoCaoPhiSanController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Phí sàn TMĐT: lưới báo cáo phí sàn thương mại điện tử (theo chi nhánh, sàn/EcommerceId, gian hàng, trạng thái giao hàng, Từ/Đến ngày, keyword, paging) + xem chi tiết 1 đơn (phí sàn) + chi tiết thông tin sandbox (đơn hàng SCRM gốc)."
muc_dich: "Báo cáo phí sàn TMĐT (phí hoa hồng/dịch vụ/thanh toán... của đơn logistic sàn): 3 endpoint đọc qua stored proc (lưới tổng + chi tiết + chi tiết-sandbox). Có gate khoảng ngày + chống truy cập đồng thời (ticket lock)."
alias: ["báo cáo phí sàn", "bao cao phi san", "phí sàn TMĐT", "phí sàn thương mại điện tử", "ecommerce fee report", "báo cáo phí sàn tmdt"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [PRC_SCRM_OrderLogistic_DonHangLogistic_BaoCaoPhiSanTMDT, PRC_SCRM_OrderLogistic_DonHangLogistic_BaoCaoPhiSanTMDT_ChiTiet, PRC_SCRM_OrderLogistic_DonHangLogistic_BaoCaoPhiSanTMDT_ChiTiet_Sandbox]
depends: [CommonService/CheckCauHinhKhoangThoiGianChoPhepTimKiem (giới hạn khoảng ngày), CommonService/GetStatusShowQueryReport + GetTimeToLog (log query chậm), TicketInfoService (DoProcessTicket/ReleaseLockedTicket — chống truy cập đồng thời), SqlSugar CopyNew + GetDataSetAll (DB SCRM_Report)]
cross_module_in: []
cross_module_out: []
---

# BaoCaoPhiSanController — Báo cáo phí sàn TMĐT

> Controller view/report THUẦN ĐỌC qua **stored proc** (SqlSugar `_dbDapper.CopyNew()` → `Ado.UseStoredProcedure().GetDataSetAll(...)`, DB `SCRM_Report`, trả jsonData camelCase). `[TienIchAttribute(..., TI_Report_Chung)]` (utility tag); cả 3 method có `[SBAuthorize("Key", "label", "roles...")]` — public với permission key + danh sách role (DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, HT-CSKH, DV-QLDV). SP đọc dữ liệu `DonHangLogistic` (đơn logistic sàn TMĐT) — báo cáo phí sàn nằm trong domain OrderLogistic nhưng truy vấn qua Report-service. Endpoint lưới (`GetBaoCaoPhiSanTMDT`) có gate: validate khoảng ngày (`CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn) + **chống truy cập đồng thời** qua ticket lock (`CheckDongThoi`/`ReleaseLockedTicket` theo ConfigCode `SO_USER_READ_REPORT_BAO_CAO_PHI_SAN_TMDT`) — quá lượt trả status 250 "Chưa đến lượt truy cập"; 2 endpoint chi tiết không gate ngày/ticket. Tham số phân trang `@PageSize/@PageIndex`; total đọc từ `ds.Tables[2].Rows[0]["TotalRecord"]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo phí sàn TMĐT (lưới) | Lưới phí sàn | `POST api/BaoCaoPhiSan/GetBaoCaoPhiSanTMDT` | `BaoCaoPhiSanController_GetBaoCaoPhiSanTMDT` | public | màn báo cáo phí sàn | Validate khoảng ngày (≠ -1 → chặn); ticket-lock chống đồng thời (quá lượt → status 250); param chi nhánh/sàn/gian hàng/trạng thái/keyword/ngày/paging; chạy SP; trả jsonData + total (Tables[2].TotalRecord). Finally release ticket. | **SP** `PRC_SCRM_OrderLogistic_DonHangLogistic_BaoCaoPhiSanTMDT` | Log store: gọi `LogCommandV2` 2 lần liên tiếp (L105+L108) kể cả khi không phải super/CSKH → log thừa. |
| 2 | Chi tiết báo cáo phí sàn | (mở chi tiết đơn) | `POST api/BaoCaoPhiSan/GetBaoCaoPhiSanTMDT_ChiTiet` | `BaoCaoPhiSanController_GetBaoCaoPhiSanTMDT_ChiTiet` | public | click 1 dòng | Bắt buộc `DonHangLogisticId` (≠ empty); param IdDonVi + DonHangLogisticId; chạy SP; trả jsonData. | **SP** `PRC_SCRM_OrderLogistic_DonHangLogistic_BaoCaoPhiSanTMDT_ChiTiet` | `if (true || ...)` + `LogCommandV2(db, " ", ...)` (L159-161) → luôn log với tên SP rỗng " " → bản ghi log rác/lỗi. |
| 3 | Chi tiết phí sàn — thông tin sandbox | (xem đơn SCRM gốc) | `POST api/BaoCaoPhiSan/GetBaoCaoPhiSanTMDT_ChiTiet_Sandbox` | `BaoCaoPhiSanController_GetBaoCaoPhiSanTMDT_ChiTiet_Sandbox` | public | tab thông tin đơn SCRM | Bắt buộc `DonHangLogisticId`; param IdDonVi + DonHangLogisticId; chạy SP; trả jsonData (thông tin đơn hàng SCRM). | **SP** `PRC_SCRM_OrderLogistic_DonHangLogistic_BaoCaoPhiSanTMDT_ChiTiet_Sandbox` | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Giới hạn khoảng ngày (chỉ lưới #1):** `CheckCauHinhKhoangThoiGianChoPhepTimKiem(userLogin, TuNgay, DenNgay)` ≠ -1 → trả lỗi "Chỉ được xem dữ liệu trong khoảng 'N ngày'".
- **Chống truy cập đồng thời (chỉ lưới #1):** `DoProcessTicket(SO_USER_READ_REPORT_BAO_CAO_PHI_SAN_TMDT, ...)` == false → status 250 "Chưa đến lượt truy cập"; `finally` luôn `ReleaseLockedTicket`.
- **Log query chậm:** lấy `GetTimeToLog`; chỉ log debug khi `SoGiayChayStore >= thoiGianLogReport` (super/CSKH/showQuery).
- **Validate chi tiết (#2/#3):** `DonHangLogisticId.GuidIsNullOrEmpty()` → lỗi "Id chi tiết là bắt buộc".

## Quan hệ
- **Gọi sang:** Report `CommonService` (gate ngày + log config), `TicketInfoService` (ticket lock). Không gọi service ngoài Report.
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — 100% qua stored proc DB `SCRM_Report`. SP truy vấn dữ liệu `DonHangLogistic` (đơn logistic sàn TMĐT). Read-only.
- **Ripple / đồng bộ:** không bắn message/event. Báo cáo này phụ thuộc dữ liệu do module OrderLogistic/Ecommerce đồng bộ vào (nhưng controller chỉ đọc).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/Ecomm/BaoCaoPhiSanService.cs`
  - **L159-161 `GetBaoCaoPhiSanTMDT_ChiTiet`:** `if (true || ...)` rồi `LogCommandV2(db, " ", listParam)` — điều kiện luôn true + tên SP log là chuỗi " " (rỗng) → tạo log lệnh sai/rác mỗi lần gọi. Nên xóa block thừa.
  - **L105 + L108 `GetBaoCaoPhiSanTMDT`:** `LogCommandV2(...)` gọi 2 lần (1 trong if super/CSKH, 1 vô điều kiện) → log trùng/thừa.
  - `reportLog.SoGiayChayStore` (L118) dùng để gate log nhưng `reportLog`/`objLogStore` không được gán thời gian chạy thực (khởi tạo rỗng) → điều kiện log debug gần như không kích hoạt đúng (dead/khả nghi).
  - `GetBaoCaoPhiSanTMDT_ChiTiet` (L177) có `throw;` sau `return` trong catch → unreachable code.
- Total đọc cứng từ `ds.Tables[2]` (L110) → SP phải trả ≥ 3 result set, lệ thuộc thứ tự table.
