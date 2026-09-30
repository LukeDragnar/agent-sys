# Module: Website bán hàng online (B2C)

> Kênh bán online cho khách hàng cuối — mỗi đơn vị một tên miền riêng, bán combo spa / sản phẩm / thẻ dịch vụ.
> Đây là **mục lục module** — mỗi tính năng con có folder riêng (summarize + flow). Kỹ thuật cho dev: [../website.md](../website.md).

## Danh sách tính năng con
| Tính năng | Mô tả ngắn | Folder | Trạng thái cẩm nang |
|---|---|---|---|
| Đặt hàng online | Khách chọn SP/combo → tạo đơn | [dat-hang-online/](dat-hang-online/) | ✅ draft |
| Thanh toán & xác nhận | CK/quẹt thẻ → ngân hàng báo về → tạo đơn thật | `thanh-toan-xac-nhan/` | 🔲 chưa làm |
| Cấu hình hiển thị | Đơn vị set domain + chọn SP/combo/thẻ lên web | `cau-hinh-hien-thi/` | 🔲 chưa làm |
| Áp khuyến mãi web | Voucher / giảm giá trên đơn web | `ap-khuyen-mai/` | 🔲 chưa làm |
| Đồng bộ đơn sang Order | Đơn web chốt → thành đơn hàng thật | `dong-bo-don/` | 🔲 chưa làm |

> Mỗi tính năng con = 1 folder `{ten}/` chứa `summarize.md` (mọi người đọc) + `flow.md` (test/đào tạo).
