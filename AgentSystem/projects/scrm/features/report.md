# Report — Báo Cáo & Phân Tích

> Tổng hợp và hiển thị báo cáo từ toàn hệ thống: doanh thu, lịch hẹn, khách hàng, kho, SMS, tổng đài, marketing. Hỗ trợ dashboard tùy chỉnh và báo cáo AI.

---

## Vai trò

- ← **Tất cả service**: đọc dữ liệu từ Order, Booking, Customer, Warehouse, SMS, Contact...
- → **MongoDB**: lưu cấu hình dashboard tùy chỉnh
- → **Redis**: cache báo cáo thường dùng
- → **Elasticsearch**: tìm kiếm nhanh khi báo cáo lớn

---

## Entities chính (Read-only, lấy từ các service khác)

| Entity | Ý nghĩa |
|--------|---------|
| `CauHinh` | Cấu hình báo cáo |
| `SanPhamLanding` | Sản phẩm Landing page (cho báo cáo marketing) |
| `DashboardProConfig` (MongoDB) | Cấu hình dashboard tùy chỉnh theo user |
| `ReportCustomColumnConfig` (MongoDB) | Cột báo cáo tùy chỉnh theo user |

---

## Nhóm báo cáo chính

| Service / Controller | Mô tả |
|---------------------|-------|
| `BookingReportService` | Báo cáo lịch hẹn: đếm, thống kê theo thời gian |
| `OrderReportService` | Báo cáo đơn hàng, doanh thu |
| `BranchReportService` | KPI theo chi nhánh |
| `KhachHangReportService` | Phân tích hành vi, giá trị khách hàng |
| `ContactReportService` | Tỷ lệ chuyển đổi lead → KH |
| `WarehouseReportService` | Tồn kho, xuất nhập |
| `SmsReportService` | Tỷ lệ gửi/nhận SMS, chiến dịch |
| `TongDaiReportService` | Thống kê cuộc gọi tổng đài |
| `MarketingDashboardService` | ROI marketing campaign |
| `ChiaSoCareDonReportService` | Phân chia doanh thu chăm sóc |
| `MyHubCallHistoryReportService` | Chi tiết lịch sử cuộc gọi |
| `AIReportService` | Báo cáo AI / gợi ý tự động |
| `ReportCeoService` | Dashboard CEO (KPI cấp cao) |
| `ReportChuoiCuaHangService` | Báo cáo chuỗi nhiều cửa hàng |
| `BaoCaoPhiSanService` | Phí sàn TMĐT |

---

## API Endpoints (tiêu biểu)

| Endpoint | Mô tả |
|----------|-------|
| `POST /DashboardGetCountDatLich` | Đếm lịch hẹn cho dashboard |
| `POST /BookingReportBookings` | Báo cáo lịch hẹn chi tiết |
| `POST /DashboardPro/SaveConfig` | Lưu cấu hình dashboard tùy chỉnh |
| `POST /DashboardPro/GetConfig` | Lấy cấu hình dashboard |
| `POST /ReportCustomColumn/Save` | Lưu cột báo cáo tùy chỉnh |
| `POST /TroLyAI/...` | API báo cáo AI |
| CEO endpoints | Báo cáo tổng hợp cho CEO |

---

## Đặc điểm quan trọng

- **Stored Procedures**: `SP_DashboardGetCountDatLich` và nhiều SP khác cho báo cáo phức tạp (performance)
- **Permission filtering**: dữ liệu lọc theo role (admin thấy tất cả, CSKH thấy KH mình quản lý)
- **Multi-level view**: user → chi nhánh → đơn vị → hệ thống
- **Dashboard tùy chỉnh (MongoDB)**: mỗi user có layout dashboard riêng
- **Cột tùy chỉnh (MongoDB)**: user tự chọn cột hiển thị trong báo cáo
- **Caching**: Redis cache cho các báo cáo query nặng
- **AI Report**: phân tích xu hướng, gợi ý hành động

---

## Checklist fix bug

- [ ] Báo cáo sai số liệu → kiểm tra quyền user (permission filtering)
- [ ] Báo cáo chậm → kiểm tra Redis cache, xem SP đang chạy bao lâu
- [ ] Dashboard không hiển thị → kiểm tra `DashboardProConfig` trong MongoDB
- [ ] Cột tùy chỉnh mất → kiểm tra `ReportCustomColumnConfig` trong MongoDB
- [ ] Báo cáo chuỗi cửa hàng sai → kiểm tra `ReportChuoiCuaHangService`, quyền xem nhiều đơn vị
