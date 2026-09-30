# Đặt hàng online — Luồng

> Tóm tắt: [summarize.md](./summarize.md) · Module: [../README.md](../README.md)

## Tiền điều kiện
- Website đơn vị đã chạy (domain cấu hình đúng).
- Có sản phẩm/combo đang hiển thị (còn hàng, không bị ẩn).

## Luồng khách đặt (user)
1. Khách vào website → xem danh sách sản phẩm / combo / thẻ dịch vụ.
2. Khách chọn món → thêm vào đơn (có thể chọn nhiều).
3. Khách nhập **số điện thoại** + thông tin nhận hàng.
4. (Nếu có) nhập **mã khuyến mãi/voucher** → giá cập nhật.
5. Khách bấm **Đặt hàng** → hệ thống tạo đơn website ở trạng thái **chờ thanh toán**.
- **Kết quả mong đợi:** đơn website được ghi nhận, chuyển sang bước thanh toán.

## Các trường hợp / nhánh
| Tình huống | Kết quả |
|---|---|
| Sản phẩm hết hàng / bị ẩn | Không hiện hoặc không cho thêm vào đơn |
| Nhập sai mã khuyến mãi | Báo mã không hợp lệ, giá giữ nguyên |
| Khách thoát giữa chừng (chưa thanh toán) | Đơn ở trạng thái chờ — **chưa thành đơn thật** |
| Nhập thiếu SĐT | Chặn đặt hàng (SĐT là bắt buộc) |

## Ví dụ cụ thể có số
- Khách chọn **combo 500.000đ** + nhập voucher **giảm 10%** → đơn hiển thị **450.000đ**, trạng thái *chờ thanh toán*.

## Hậu điều kiện
- Có 1 đơn website (chờ thanh toán) gắn với SĐT khách.
- → Chuyển tiếp sang tính năng **Thanh toán & xác nhận**.
