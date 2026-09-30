---
controller: KhachHangReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Khách hàng: (1) báo cáo khách hàng theo đơn hàng (daily) — số khách phát sinh đơn theo ngày; (2) báo cáo khách viếng thăm (visitors) — phân bố theo tỉnh/thành, giới tính, nhóm KH, hạng KH. Bộ lọc: chi nhánh, Từ/Đến ngày, kiểu ngày."
muc_dich: "2 báo cáo khách hàng đọc qua stored proc: KH theo đơn hàng daily + KH viếng thăm (enrich tên tỉnh/thành từ AddressLevel1)."
alias: ["báo cáo khách hàng", "bao cao khach hang", "khách hàng theo đơn", "khách viếng thăm", "customer report visitors", "customer by order daily"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [PRC_SCRM_Customer_by_OrderReport_Daily, PRC_SCRM_CustomerReport_Visitors]
depends: [CommonService/CheckCauHinhKhoangThoiGianChoPhepTimKiem (giới hạn khoảng ngày), CauHinhService/GetCauHinhInit (cờ ChiNhanhHienThiTatCaKhachHangDonVi), SA_GetAllAddressLevel1 (enrich tên tỉnh/thành), CommonService/GetStatusShowQueryReport, SqlSugar (CopyNew + GetDataSetAll stored proc)]
cross_module_in: []
cross_module_out: []
---

# KhachHangReportController — Báo cáo khách hàng (theo đơn hàng + viếng thăm)

> Controller view/report THUẦN ĐỌC: 2 endpoint đi **stored proc** trên `_dbDapper.CopyNew()`. `[TienIchAttribute(..., TI_Customer)]` + `[SBAuthorize]` **controller-level** → cần login; 2 method `[SBAuthorize()]` trống (không key riêng → chỉ cần login). Validate khoảng ngày + cờ "hiển thị tất cả KH đơn vị" để quyết định có ép chi nhánh user hay không. Báo cáo visitors còn enrich tên tỉnh/thành từ `SA_GetAllAddressLevel1`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | KH theo đơn hàng (daily) | Lưới KH phát sinh đơn theo ngày | `POST api/KhachHangReport/CustomerByOrderReportDaily` | `SBAuthorize()` (any login) | public | màn báo cáo KH theo đơn | Validate khoảng ngày. Build param (IdDonVi, IdChiNhanh, Từ/Đến ngày, kiểu ngày). Chạy SP, trả jsonData + total. | **SP** `PRC_SCRM_Customer_by_OrderReport_Daily` | code cũ ADO comment L122+. |
| 2 | KH viếng thăm (visitors) | Biểu đồ KH theo tỉnh/giới tính/nhóm/hạng | `POST api/KhachHangReport/CustomerReportVisitors` | `SBAuthorize()` (any login) | public | màn báo cáo KH viếng thăm | Validate khoảng ngày. Lấy `GetCauHinhInit`; nếu `ChiNhanhHienThiTatCaKhachHangDonVi` false → `isViewAll=false`. User thường + không chọn chi nhánh + isViewAll=false → ép `idChiNhanh=user.IdChiNhanh`. Chạy SP **song song** với `SA_GetAllAddressLevel1` (Task.WhenAll); map tên tỉnh/thành vào `lstAddress` (lọc NationalCode=="VN"); trả 4 nhóm: address, gender, group, customerLevel. | **SP** ⚠️ **GỌI SAI SP** — exec `PRC_SCRM_BookingReport_Bookings` (đáng lẽ `PRC_SCRM_CustomerReport_Visitors`) | **BUG nặng** (xem dưới). |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Giới hạn khoảng ngày:** `CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn.
- **Phạm vi chi nhánh (#2):** cờ cấu hình `ChiNhanhHienThiTatCaKhachHangDonVi`==true → xem toàn đơn vị (isViewAll). User admin/super/test/CSKH/KeToan luôn không bị ép chi nhánh.
- **Log query chậm:** `objectLog = LogCommandV2(...)` khi super/CSKH/showQuery.

## Quan hệ
- **Gọi sang:** Report `CauHinhService.GetCauHinhInit`, `CommonService` (khoảng ngày, showQuery), `SA_GetAllAddressLevel1` (danh mục tỉnh/thành).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — qua stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/KhachHangReportService.cs`
  - **L203 — BUG GỌI SAI STORED PROC:** `CustomerReportVisitors` exec `GetDataSetAll("PRC_SCRM_BookingReport_Bookings", ...)` trong khi log L199 ghi đúng tên `PRC_SCRM_CustomerReport_Visitors`. → báo cáo visitors đang chạy SP của Booking, gần như chắc chắn trả sai shape / sai dữ liệu (hoặc ném khi map cột). Cần sửa thành `PRC_SCRM_CustomerReport_Visitors`.
  - L201: SP được bọc trong `Task.Run(...)` rồi `Task.WhenAll` với `SA_GetAllAddressLevel1` để chạy song song — lưu ý dùng connection `db` (CopyNew) trong Task.Run.
- BE `Report/Report.API/Controllers/KhachHangReportController.cs`
  - Controller `[SBAuthorize]` + 2 method `[SBAuthorize()]` trống → chỉ cần login, KHÔNG có permission key riêng cho 2 báo cáo này.
