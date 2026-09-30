---
controller: OrderProcessController
module: he-thong-rollback-don
service: OrderProcess
man_hinh_ui: "nội bộ — không có UI (route sandbox tra cứu bản ghi rollback đơn theo điều kiện)"
muc_dich: "Tra cứu (grid) các bản ghi rollback/hoàn tác đơn hàng theo điều kiện lọc. Là endpoint search phục vụ theo dõi quá trình rollback đơn."
alias: ["tra cứu rollback đơn", "OrderProcess search", "DMDonHang sandbox"]
auth: internal
state_machine: []
deprecated: false
entities: [OrderRollBack]
stored_procs: []
depends: ["IOrderRollBackService"]
cross_module_in: ["Công cụ nội bộ / màn sandbox gọi để xem danh sách rollback"]
cross_module_out: []
---

# OrderProcessController (OrderProcess) — Tra cứu bản ghi rollback đơn

Controller mỏng (1 action) trỏ vào `IOrderRollBackService.TimTheoDieuKien` để lấy danh sách bản ghi hoàn tác đơn theo điều kiện grid. Route `api/SanboxArea/OrderProcess`. Class gắn `[TienIch("DMDonHangController", ...)]` (nhãn copy từ danh mục đơn hàng) nhưng class-level KHÔNG có `[SBAuthorize]`; bảo vệ ở mức action bằng `[SBAuthorize("OrderProcess_TimTheoDieuKien", ...)]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm rollback theo điều kiện | (grid nội bộ) | POST `api/SanboxArea/OrderProcess/TimTheoDieuKien` | `OrderProcess_TimTheoDieuKien` | internal | — | `TimOrderProcessDto` → `_orderRollBackService.TimTheoDieuKien` trả danh sách bản ghi rollback đơn theo filter | EF | — |

## Quan hệ
- **Gọi sang:** `IOrderRollBackService.TimTheoDieuKien`.
- **Dữ liệu (entities):** `OrderRollBack` (và các bảng rollback liên quan).
- **Ripple / đồng bộ:** Chỉ đọc, không ghi, không phát event.

## Lỗi đã biết / lưu ý kỹ thuật
- `OrderProcessController.cs:13` — `[TienIch("DMDonHangController", ...)]` đặt nhãn tiện ích là "Quản lý danh mục đơn hàng" (copy nhầm context) trong khi controller phục vụ tra cứu rollback.
- Class không có `[SBAuthorize]` ở mức controller; an toàn nhờ action có key riêng — nếu thêm action mới mà quên key thì sẽ hở.
