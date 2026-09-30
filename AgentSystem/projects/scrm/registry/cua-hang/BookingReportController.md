---
controller: BookingReportController
module: cua-hang
service: Booking
man_hinh_ui: "nội bộ — báo cáo đặt lịch (dashboard số lượng đặt lịch/ra đơn + thống kê nguồn/trạng thái/khách theo giờ). HIỆN TẠI TOÀN BỘ ENDPOINT BỊ COMMENT → controller không expose endpoint nào."
muc_dich: "Báo cáo/thống kê đặt lịch qua stored procedure (đếm số lượng đặt lịch & ra đơn cho dashboard; thống kê theo nguồn đặt lịch / trạng thái / khách theo khung giờ). Bản hiện tại đã bị vô hiệu hóa (comment hết)."
alias: ["báo cáo đặt lịch", "dashboard đặt lịch", "thống kê đặt lịch", "BookingReport", "số lượng đặt lịch ra đơn"]
auth: public
state_machine: []
deprecated: true
entities: []
stored_procs: [sp_DashboardGetCountDatLich, PRC_SCRM_BookingReport_Bookings]
depends: [BookingDataContext (ADO.NET CreateCommand → stored proc), ServiceSettings, ClientHelper]
cross_module_in: []
cross_module_out: []
---

# BookingReportController — Báo cáo đặt lịch (hiện vô hiệu hóa)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (đã tắt) Đếm số lượng đặt lịch / ra đơn cho dashboard | — | ~~POST `api/BookingReport/DashboardGetCountDatLich`~~ (comment) | `DashboardGetCountDatLich` (version 2.0) — đã comment | — | **không khả dụng** (cả action lẫn quyền bị comment) | (theo code cũ) gọi SP `sp_DashboardGetCountDatLich` với `@IdDonVi`, `@IdChiNhanh`, `@ngayBatDau`, `@ngayKetThuc`; map `DashboardGetCountDatLichResponse`. Có check quyền chi nhánh theo user (non-admin ép `IdChiNhanh = user.IdChiNhanh`). | SP `sp_DashboardGetCountDatLich` | Bị comment hoàn toàn — không build vào route |
| 2 | (đã tắt) Thống kê đặt lịch (nguồn/trạng thái/khách theo giờ) | — | ~~POST `api/BookingReport/BookingReportBookings`~~ (comment) | `BookingReportBookings` — đã comment | — | **không khả dụng** | (theo code cũ) gọi SP `PRC_SCRM_BookingReport_Bookings` với `@IdDonVi`, `@IdChiNhanh`, `@TuNgay`, `@DenNgay`; trả 4 result set: `NguonDatLich`, `TrangThaiDatLich`, `CustomerByHour`, `Total`. | SP `PRC_SCRM_BookingReport_Bookings` | Bị comment hoàn toàn |

> **Cảnh báo:** ở thời điểm đọc code, **controller không có endpoint hoạt động nào** — cả 2 action và toàn bộ thân interface `IBookingReportService` đều bị comment. Controller chỉ còn DI `IBookingReportService` rỗng. `[TienIchAttribute]` vẫn đăng ký tiện ích "Báo cáo đặt lịch" nhưng không có quyền/endpoint nào phía sau.

## Quan hệ
- **Gọi sang:** (khi còn bật) trực tiếp DB qua `BookingDataContext` (ADO.NET `CreateCommand` → stored procedure), không gọi cross-service. `ServiceSettings`/`ClientHelper` được inject nhưng không dùng trong code còn lại.
- **Dữ liệu (entities):** không truy cập entity EF — đọc qua stored proc rồi `ObjectHelper.FillCollection` vào DTO.
- **Stored procedures:** `sp_DashboardGetCountDatLich`, `PRC_SCRM_BookingReport_Bookings`.
- **Ripple / đồng bộ:** không có (read-only báo cáo).

## Lỗi đã biết / lưu ý kỹ thuật
- **Controller chết — không expose gì** — cả 2 action bị comment (`BookingReportController.cs:25-40`); interface `IBookingReportService` rỗng, body service comment (`BookingReportService.cs:33-36`, `:52-131`). Bất kỳ FE/báo cáo nào còn gọi 2 route trên sẽ nhận 404. → đánh dấu `deprecated: true`.
- **Quyền cũ (tham khảo)** — `DashboardGetCountDatLich` (version 2.0) và `BookingReportBookings` từng map role `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, HT-AD, HT-CSKH` (`...:25`, `:34`).
- **Phân quyền chi nhánh theo user** (logic cũ trong service) — non-admin/non-superuser/non-test bị ép `IdChiNhanh = userLogin.IdChiNhanh` (`BookingReportService.cs:65-70`, `:103-108`); nếu khôi phục cần giữ ràng buộc này.
- Code thật: controller `Booking.API/Controllers/BookingReportController.cs`; service `Booking.Service/Generated/BookingReportService.cs`.
