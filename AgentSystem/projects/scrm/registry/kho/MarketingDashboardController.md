---
controller: MarketingDashboardController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI hoạt động (toàn bộ endpoint đang bị comment chết)"
muc_dich: "Controller dashboard marketing trong service OrderLogistic — HIỆN KHÔNG CÓ ENDPOINT HOẠT ĐỘNG: cả 2 action (GetOrderInfoSourceFilter, GetDoanhSoLandingTheoDieuKien) đều bị comment, chỉ còn constructor inject IMarketingDashboardService."
alias: ["marketing dashboard", "MarketingDashboard", "dashboard marketing OrderLogistic", "doanh số landing"]
auth: internal
state_machine: []
deprecated: true
entities: [DonHangLogistic]
stored_procs: []
depends:
  - "IMarketingDashboardService (MarketingDashboardService.cs — service được inject nhưng controller không gọi endpoint nào)"
cross_module_in: []
cross_module_out: []
---

# MarketingDashboardController — Dashboard marketing (rỗng, endpoint đã comment)

> TienIch `MarketingDashboardController` / `MaTienIchSCRM.TI_OrderLogistic`. **`deprecated: true`** vì toàn bộ endpoint đã bị comment — controller hiện không expose API nào, chỉ giữ DI `IMarketingDashboardService`. Logic dashboard marketing thực tế nằm ở service/controller khác (cần xác nhận).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| - | (không có endpoint hoạt động) | - | - | - | - | - | - | - | Cả 2 action comment chết |

## Endpoint nội bộ (private / service-to-service) — ĐÃ COMMENT
- [comment] `GetOrderInfoSourceFilter` (`[PrivateSBAuthorize]`) — lọc nguồn đơn cho dashboard marketing (MarketingDashboardController.cs:22-27).
- [comment] `GetDoanhSoLandingTheoDieuKien` (`[PrivateSBAuthorize]`) — doanh số landing theo điều kiện (MarketingDashboardController.cs:29-34).

## Quan hệ
- **Gọi sang:** `IMarketingDashboardService` (inject sẵn nhưng không dùng).
- **Dữ liệu (entities):** `DonHangLogistic` (qua service, nếu bật lại).
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller rỗng chức năng — toàn bộ endpoint đã comment (MarketingDashboardController.cs:22-34). Nếu cần dashboard marketing → kiểm tra service `MarketingDashboardService.cs` còn được gọi từ đâu hoặc đã chuyển nơi khác.
- Đánh dấu `deprecated: true` để AI bỏ qua nhanh khi query graph.
