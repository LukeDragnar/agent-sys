---
controller: DeliveryPropertyController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Quản trị > Quản lý địa chỉ (cấu hình thuộc tính địa chỉ theo đơn vị vận chuyển)"
muc_dich: "Quản lý các thuộc tính địa chỉ giao hàng (delivery property) theo từng đơn vị vận chuyển (shippingUnitCode): lấy danh sách property code, lấy/lưu/xóa thuộc tính, tra cứu theo id/code."
alias: ["thuộc tính địa chỉ", "delivery property", "địa chỉ giao hàng", "property vận chuyển"]
auth: internal
state_machine: []
deprecated: false
entities: [DeliveryProperty]
stored_procs: []
depends: ["IDeliveryPropertyService"]
cross_module_in: ["Frontend QuanTriArea (quản lý địa chỉ); endpoint *_Private gọi nội bộ service-to-service"]
cross_module_out: ["module địa chỉ giao hàng / tính phí vận chuyển dùng các property này"]
---

# DeliveryPropertyController — Quản lý thuộc tính địa chỉ giao hàng theo đơn vị vận chuyển

Controller cấu hình thuộc tính địa chỉ (delivery property) gắn theo từng đơn vị vận chuyển. Nằm trong folder `Areas/SanboxArea` nhưng **route thật là `api/QuanTriArea/DeliveryProperty/...`** (`[Route("api/QuanTriArea/[controller]")]`, KHÔNG dùng `[Area]`). Toàn bộ controller có `[SBAuthorize]` ở class-level, mỗi action có quyền riêng (role gợi ý `HT-AD, HT-KT`). Có 1 endpoint biến thể `_Private` dùng `[PrivateSBAuthorize]` cho gọi service-to-service. Mọi action chỉ ủy thác sang `IDeliveryPropertyService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả property code | (danh sách) | GET `api/QuanTriArea/DeliveryProperty/Delivery_GetAllPropertyCode` | `DeliveryProperty_Delivery_GetAllPropertyCode` | internal (`[SBAuthorize]`) | — | Trả toàn bộ thuộc tính theo property code | EF (service) | — |
| 2 | Lấy theo đơn vị vận chuyển | (chi tiết) | GET `api/QuanTriArea/DeliveryProperty/Delivery_GetByShippingUnitCode?shippingUnitCode=` | `DeliveryProperty_Delivery_GetByShippingUnitCode` | internal | — | Trả thuộc tính của 1 đơn vị vận chuyển | EF (service) | — |
| 3 | Lấy theo đơn vị (private) | (không UI) | GET `api/QuanTriArea/DeliveryProperty/Delivery_GetByShippingUnitCode_Private?shippingUnitCode=` | — (không key) | internal (`[PrivateSBAuthorize]`) | — | Giống #2, dùng cho gọi nội bộ service-to-service (không qua quyền user) | EF (service) | Trùng logic với #2, chỉ khác cơ chế auth |
| 4 | Xóa thuộc tính | Xóa | POST `api/QuanTriArea/DeliveryProperty/Delivery_Delete?id=` | `DeliveryProperty_Delivery_Delete` | internal | — | Xóa thuộc tính theo id (Guid?) | EF (service) | — |
| 5 | Lưu thuộc tính | Lưu | POST `api/QuanTriArea/DeliveryProperty/Delivery_Save` (body `DeliveryProperty`) | `DeliveryProperty_Delivery_Save` | internal | — | Thêm/cập nhật thuộc tính địa chỉ | EF (service) | Nhận thẳng entity `DeliveryProperty` làm body (không qua DTO) |
| 6 | Lấy theo property code | (chi tiết) | POST `api/QuanTriArea/DeliveryProperty/Delivery_GetByPropertyCode?shippingUnitCode=&propertyCode=` | `DeliveryProperty_Delivery_GetByPropertyCode` | internal | — | Trả thuộc tính theo cặp (đơn vị, property code) | EF (service) | Dùng `[HttpPost]` nhưng tham số nằm trên query string |
| 7 | Lấy theo id | (chi tiết) | POST `api/QuanTriArea/DeliveryProperty/Delivery_GetById?id=&shippingUnitCode=` | `DeliveryProperty_Delivery_GetById` | internal | — | Trả thuộc tính theo id + đơn vị vận chuyển | EF (service) | Dùng `[HttpPost]` nhưng tham số nằm trên query string |

## Quan hệ
- **Gọi sang:** `IDeliveryPropertyService` (toàn bộ logic CRUD/tra cứu).
- **Dữ liệu (entities):** `DeliveryProperty` (gắn theo `shippingUnitCode` + `propertyCode`).
- **Ripple / đồng bộ:** thuộc tính được dùng bởi luồng địa chỉ giao hàng / cấu hình đơn vị vận chuyển.

## Lỗi đã biết / lưu ý kỹ thuật
- `DeliveryPropertyController.cs:15` — file nằm trong `Areas/SanboxArea` nhưng route khai báo `api/QuanTriArea/[controller]` (không có `[Area]`) → **path thật là `api/QuanTriArea/DeliveryProperty/...`**, dễ nhầm thành `api/SanboxArea/...`.
- `DeliveryPropertyController.cs:54-60` — `Delivery_GetByShippingUnitCode_Private` dùng `[PrivateSBAuthorize]`, không có permission key, trùng logic endpoint công khai #2.
- `DeliveryPropertyController.cs:82` — `Delivery_Save` nhận thẳng entity `DeliveryProperty` làm body thay vì DTO.
- `DeliveryPropertyController.cs:94-114` — các action `[HttpPost]` nhưng tham số truyền qua query string (`shippingUnitCode`, `propertyCode`, `id`) → lệch convention POST-body.
