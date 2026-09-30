---
alias: ["đặt hàng online", "khách đặt hàng trên web", "lên đơn website"]
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: cần product xác nhận
phu_thuoc: [WarehouseProduct (SP/combo), Promotion (KM), AuthenCustomer]
link_flow: ./flow.md
module: website
---

# Đặt hàng online

## Là gì
Khách hàng cuối tự **chọn sản phẩm/combo trên website và tạo đơn đặt hàng** (chưa tính bước thanh toán — xem tính năng "Thanh toán & xác nhận").

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Mở giỏ hàng / xem giỏ | ✅ | công khai, không cần đăng nhập |
| Thêm sản phẩm / combo / thẻ vào giỏ | ✅ | còn hàng, đang hiển thị trên web |
| Thêm dịch vụ / nạp thẻ / thu khác | ⚠️ | thêm được nhưng **không kiểm tra tồn tại** (xem Cần chú ý) |
| Sửa số lượng trong giỏ | ✅ | công khai |
| Xóa dòng khỏi giỏ | ✅ | công khai |
| Nhập mã khuyến mãi / voucher | ✅ | mã còn hiệu lực |
| Nhập thông tin nhận hàng (SĐT, địa chỉ) | ✅ | **SĐT bắt buộc** |
| Đặt hàng (tạo đơn website) | ✅ | giỏ + thông tin hợp lệ; **chưa phải đơn thật** cho tới khi thanh toán |
| Đặt đơn khi chưa đăng nhập (khách vô danh) | ✅ | storefront cho khách vô danh đặt; đăng nhập chỉ để điền sẵn thông tin |
| Đặt nhiều kho trong 1 lần checkout | ⚠️ | giỏ tách nhiều kho phải đi qua màn tách đơn riêng (xem Cần chú ý) |

> ⚠️ = có nhưng kèm cảnh báo / điều kiện; chi tiết ở phần "Cần chú ý". Quyền/gói cụ thể: **cần product xác nhận**.

## Để làm gì / cho ai
- **Cho khách:** tự đặt hàng 24/7, không cần gọi nhân viên.
- **Actor:** khách hàng cuối (B2C).

## Cần chú ý (cạm bẫy)
- Đặt hàng xong **chưa chắc đã có đơn thật** — đơn chỉ chốt sau khi **thanh toán xác nhận** (tính năng riêng).
- **Số điện thoại** là cái định danh khách — nhập sai SĐT → khó tra cứu/CSKH sau này.
- Sản phẩm/combo hiển thị lấy từ **kho**; hết hàng hoặc ẩn SP thì khách không đặt được.
- **Giỏ hàng lưu trong cookie**, không lưu DB cho tới khi bấm Đặt hàng → mất cookie (xóa cookie/đổi máy) là **mất giỏ**.
- **Giá hiển thị lấy giá hiện tại của kho**, không "đóng băng" giá lúc bỏ vào giỏ → đổi giá/ngừng bán bên kho ảnh hưởng ngay tới giỏ đang mở.
- **Đặt hàng nhiều kho cùng lúc**: nếu giỏ tách thành nhiều kho, luồng đặt nhanh chỉ xử lý **kho đầu tiên**; tách nhiều kho phải đi qua màn tách đơn riêng (`/Checkout`) — cần product xác nhận hành vi đầy đủ.

## Ảnh hưởng tới
- **Kho (WarehouseProduct)** — nguồn sản phẩm/combo hiển thị.
- **Khuyến mãi (Promotion)** — giá sau giảm.
- **Thanh toán & Đồng bộ đơn** — bước tiếp theo sau khi đặt.

## Link
→ [flow.md](./flow.md) · Module: [../README.md](../README.md) · Kỹ thuật: [../../website.md](../../website.md)
