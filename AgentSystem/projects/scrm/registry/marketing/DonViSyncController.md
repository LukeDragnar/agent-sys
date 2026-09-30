---
controller: DonViSyncController
module: marketing
service: MarketingCampaign
man_hinh_ui: "nội bộ — không có UI (controller đồng bộ đơn vị/khách hàng, hiện toàn bộ bị comment)"
muc_dich: "Đáng lẽ quản lý đăng ký đơn vị dùng MarketingCampaign + trigger đồng bộ dữ liệu khách hàng từ các service về (publish RabbitMQ). Hiện TOÀN BỘ endpoint bị comment → không hoạt động."
alias: ["đồng bộ đơn vị marketing", "don vi sync", "đồng bộ khách hàng về marketing campaign"]
auth: internal
state_machine: []
deprecated: true
entities: [DonViSync]
stored_procs: []
depends: [RabbitMQ (EventSyncCustomerDonViMessage), DonViSyncService]
cross_module_in: []
cross_module_out: [Customer (đồng bộ khách hàng về MarketingCampaign — khi mở lại)]
---

# DonViSyncController — Đồng bộ đơn vị / khách hàng (KHÔNG hoạt động)

> ⚠️ **Toàn bộ controller bị comment** (DonViSyncController.cs): không còn endpoint active, không inject dependency (constructor + 4 method đều //). Chỉ còn class rỗng + `[ApiController]`. Đánh dấu `deprecated: true`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| - | (không có endpoint active) | - | - | - | - | - | Controller rỗng — mọi endpoint đã comment | - | Toàn bộ logic dead/commented |

## Endpoint đã comment (tham khảo — KHÔNG chạy)
- `AddOrUpdateDonVi` (POST) — thêm/cập nhật đơn vị dùng marketing campaign (`DonViSyncController_AddOrUpdateDonVi`, HT-AD).
- `HuyDongBo` (POST) — hủy đồng bộ theo list idsDonVi.
- `TimTheoDieuKien` (POST) — list đơn vị dùng marketing campaign.
- `DongBoThongTinKhachHang` (POST) — đồng bộ khách hàng từ các service về; validate BatchSize ≤ 2000, publish `EventSyncCustomerDonViMessage` mỗi đơn vị.

## Quan hệ
- **Gọi sang (khi mở lại):** RabbitMQ publish `EventSyncCustomerDonViMessage`; `DonViSyncService`.
- **Dữ liệu (entities):** `DonViSync`.
- **Ripple / đồng bộ:** Đồng bộ khách hàng (Customer → CustomerInfo trong MarketingCampaign) qua message — hiện thực hiện bởi các consumer `EventSyncCustomerInfoConsumer` / `EventSyncCustomerContactConsumer` / `EventUserSyncDataConsumer`, không qua controller này.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller hoàn toàn vô hiệu (constructor + tất cả method comment). Đồng bộ khách hàng thực tế chạy qua consumer RabbitMQ, không qua API. Giữ node để biết controller tồn tại nhưng không gọi được.
