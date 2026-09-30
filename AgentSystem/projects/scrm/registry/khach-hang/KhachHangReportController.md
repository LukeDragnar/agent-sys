---
controller: KhachHangReportController
module: khach-hang
service: Customer
man_hinh_ui: "(không có UI — controller rỗng, mọi action đã bị comment)"
muc_dich: "Báo cáo của khách hàng (theo đơn hàng / lượt khách) — hiện KHÔNG còn endpoint active, toàn bộ action bị comment"
alias: [KhachHangReport, "Báo cáo khách hàng"]
auth: public
state_machine: []
deprecated: true
entities: [KhachHang]
stored_procs: []
depends: [IKhachHangReportService]
cross_module_in: []
cross_module_out: []
---

# KhachHangReport — Báo cáo khách hàng (rỗng / đã vô hiệu)

> **`deprecated: true`** — controller `[SBAuthorize]` nhưng **không còn action nào active**: cả 2 endpoint (`CustomerByOrderReportDaily`, `CustomerReportVisitors`) đã bị **comment toàn bộ**. AI có thể bỏ qua nhanh khi triage.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | Controller chỉ còn constructor + DI `IKhachHangReportService`; không expose route nào | — | Toàn bộ action bị comment |

> Endpoint **đã bị comment** (không hoạt động, ghi lại để tham chiếu lịch sử):
> - `POST CustomerByOrderReportDaily` → `IKhachHangReportService.CustomerByOrderReportDaily(input, CurrentUser)` — báo cáo KH theo đơn hàng (daily).
> - `POST CustomerReportVisitors` → `IKhachHangReportService.CustomerReportVisitors(input, CurrentUser)` — báo cáo lượt khách.
> Cả hai dùng `[SBAuthorize()]` không key → nếu bật lại sẽ là quyền nới lỏng. Nguồn EF/SP của service **chưa xác nhận** (action bị comment, không đọc được trong controller).

## Quan hệ
- **Gọi sang:** `IKhachHangReportService` (inject nhưng hiện không gọi).
- **Dữ liệu (entities):** `KhachHang` (theo ý nghĩa báo cáo; không có truy vấn active).
- **Ripple/đồng bộ:** không có.

## Lỗi đã biết / lưu ý kỹ thuật
- `KhachHangReportController.cs:25-38` — toàn bộ 2 action **bị comment** → controller rỗng, không có route hoạt động. Nếu cần báo cáo KH, xem `CustomerReportController` (dashboard SP) thay vì controller này.
- Service `IKhachHangReportService` vẫn được DI nhưng không sử dụng → dead dependency.
