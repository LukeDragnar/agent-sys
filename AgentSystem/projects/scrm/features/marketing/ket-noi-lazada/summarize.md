---
alias: ["kết nối Lazada", "gian hàng Lazada", "đồng bộ Lazada", "kết nối sàn Lazada", "ủy quyền Lazada"]
module: marketing
trang_thai: "⚠️ chưa hoàn thiện — Lazada chỉ xuất hiện như một mã sàn trong cụm kết nối sàn TMĐT chung, nhưng KHÔNG có màn/tiện ích riêng được xác nhận. Mức độ hỗ trợ thực tế cần product/dev xác nhận."
goi: cần product xác nhận
quyen: "cần product xác nhận — nếu chạy qua màn gian hàng TMĐT chung thì dùng quyền của cụm kết nối sàn chung. Không tìm thấy tiện ích/quyền riêng cho Lazada."
phu_thuoc: ["Ecommerce-connect gateway (phải hỗ trợ Lazada phía gateway)", "Kho", "Sản phẩm CRM", "Đơn hàng / OrderLogistic"]
link_flow: ./flow.md
---

# Kết nối Lazada

## Là gì
Về **mục tiêu**, đây là tính năng kết nối gian hàng **Lazada** với hệ thống — tương tự Shopee / TikTok: lấy đơn về, ghép sản phẩm, đồng bộ tồn.

⚠️ **Trạng thái hiện tại: CHƯA HOÀN THIỆN.** Trong hệ thống, Lazada chỉ xuất hiện như một **mã sàn** trong cụm kết nối sàn TMĐT dùng chung; **không tìm thấy** màn cấu hình riêng hay tiện ích riêng cho Lazada (khác Shopee/TikTok mỗi sàn đều có tiện ích + đường kết nối rõ ràng). Vì vậy phần này mô tả **mục tiêu/kỳ vọng**, còn việc Lazada đã chạy được tới đâu **cần product/dev xác nhận**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Ủy quyền / kết nối Lazada | ⚠️ | *cần product xác nhận* — chưa thấy controller/tiện ích riêng; nếu có, đi qua màn gian hàng TMĐT chung |
| Map gian hàng Lazada ↔ đơn vị / kho | ⚠️ | *cần product xác nhận* — phụ thuộc gateway có hỗ trợ Lazada không |
| Ghép sản phẩm / đồng bộ tồn Lazada | ⚠️ | *cần product xác nhận* — dùng chung cơ chế ghép sản phẩm/đồng bộ tồn nếu gateway hỗ trợ |
| Nhận đơn Lazada về hệ thống | ⚠️ | *cần product xác nhận* |
| Có tiện ích / màn cấu hình riêng cho Lazada | ❌ | không tìm thấy (không như Shopee/TikTok) — *cần product xác nhận* |

## Để làm gì / cho ai
- **(Kỳ vọng)** Chủ shop / Marketing: gom đơn Lazada về hệ thống xử lý cùng các kênh khác, đồng bộ tồn để tồn Lazada khớp tồn thực.
- Hiện tại chưa đủ căn cứ xác nhận mức độ dùng được — xem phần "Cần chú ý".

## Cách dùng
> *Cần product xác nhận.* Nếu Lazada được kích hoạt, dự kiến đi theo **cùng màn gian hàng TMĐT chung** với Shopee/TikTok (chọn sàn Lazada → ủy quyền → map gian hàng → ghép sản phẩm → đồng bộ tồn). Xem cẩm nang **Kết nối Shopee** / **Kết nối TikTok** để hình dung luồng tương đương.

## Cần chú ý
- **Tính năng chưa hoàn thiện** — INDEX hệ thống ghi rõ "Lazada (chưa hoàn thiện)" và cột Trace để trống. Không tìm thấy controller / tiện ích / màn riêng cho Lazada.
- **Phụ thuộc gateway:** việc kết nối thật phụ thuộc **ecommerce-connect gateway** (dịch vụ ngoài) có triển khai Lazada hay chưa — nằm ngoài phạm vi mã CRM lõi.
- **Trước khi tư vấn khách dùng Lazada**, phải **xác nhận với product/dev** tính năng đã sẵn sàng chưa, tránh hứa nhầm.

## Ảnh hưởng tới
- **(Kỳ vọng)** Đơn hàng / OrderLogistic, Kho/tồn kho, Sản phẩm, Báo cáo doanh số đa kênh — tương tự Shopee/TikTok, nếu được triển khai.
