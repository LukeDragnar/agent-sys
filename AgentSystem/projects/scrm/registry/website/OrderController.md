---
controller: OrderController
module: website
service: Website
man_hinh_ui: "nội bộ — API cho app mobile (MobileApi); không có màn quản trị web. Dùng cho luồng tạo đơn từ app mobile khách hàng"
muc_dich: "Endpoint mobile tạo đơn hàng từ app khách hàng (CreateOrder) — chỉ 1 action, xác thực bằng token mobile."
alias: ["tạo đơn mobile", "create order mobile", "đặt đơn từ app", "order app khách hàng"]
auth: mixed                # [TokenMobileAuthentication] ở method-level
state_machine: []
deprecated: false
entities: [WebsiteDonHang]
stored_procs: []
depends: [Website (IOrderService)]
cross_module_in: []
cross_module_out: [Order/cửa-hàng (tạo đơn hàng thực tế từ đơn website mobile)]
---

# OrderController — Tạo đơn hàng từ app mobile

> Controller `Website.API/Controllers/MobileApi/OrderController.cs`, route `api/Order`, base `BaseCoreController`, `[ApiController]`. Action gắn `[TokenMobileAuthentication]` (token app mobile, khác token web nội bộ). Chỉ 1 endpoint.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo đơn từ mobile | nút "Đặt hàng" (app) | POST `api/Order/CreateOrder` | — (chỉ token mobile) | mixed | app đã đăng nhập | Nhận `OrderDto`, gọi `_orderService.CreateOrder(req)`, trả kết quả qua `ResponseResult` | EF (service) | Không có permission key SBAuthorize — chỉ chặn bằng `[TokenMobileAuthentication]` |

## Quan hệ
- **Gọi sang:** `IOrderService.CreateOrder`.
- **Dữ liệu (entities):** đơn hàng website (WebsiteDonHang + chi tiết).
- **Ripple / đồng bộ:** đơn tạo từ mobile chảy vào luồng đơn hàng website → đồng bộ sang Order/cửa-hàng (xem `WebsiteDonHangController.TaoDonTuWebsite`).

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers/MobileApi/OrderController.cs:21` — auth chỉ ở method-level (`TokenMobileAuthentication`), controller-level không có `[SBAuthorize]`; chú ý mọi action mới phải tự gắn token-attribute.
