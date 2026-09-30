# Module: Kho

> Quản lý kho hàng, sản phẩm, vận đơn, giao vận của phần mềm SCRM. Mục lục các tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team đọc) · `faq.md` (câu hỏi khách — **chatbot ăn cái này**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Khách hàng / chatbot chỉ thấy `summarize` + `faq`. `dev.md` (MaTinhNang, code) KHÔNG cho khách.

## Danh sách tính năng
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Quản lý vận đơn | Đăng đơn, cấu hình vận đơn, mẫu đóng hàng | [quan-ly-van-don/](quan-ly-van-don/) |
| Quản lý tồn kho | Theo dõi số lượng hàng có sẵn theo kho/chi nhánh | [quan-ly-ton-kho/](quan-ly-ton-kho/) |
| Rà soát / Reset tồn kho & giá vốn | Dò lệch, reset/tái tạo tồn + giá vốn cả đơn vị (job nền) | [ra-soat-reset-ton-kho/](ra-soat-reset-ton-kho/) |
| Kiểm kho | Lập phiếu kiểm tra số lượng tồn thực tế | [kiem-kho/](kiem-kho/) |
| Quản lý lô date & Imei | Lô hàng, hạn sử dụng, mã imei sản phẩm | [lo-date-imei/](lo-date-imei/) |
| Chia số care đơn | Phân bổ đơn cho nhân viên chăm sóc | [chia-so-care-don/](chia-so-care-don/) |
| Báo cáo kho | Báo cáo nhập/xuất/tồn/sản phẩm bán | [bao-cao-kho/](bao-cao-kho/) |
| Quản lý sản phẩm | Danh mục SP, dịch vụ, combo, thẻ, nhãn | [quan-ly-san-pham/](quan-ly-san-pham/) |
| Quản lý nhà cung cấp | Danh sách nhà cung cấp + nhóm | [nha-cung-cap/](nha-cung-cap/) |
| Quản lý SP chi nhánh | Cấu hình SP/DV/combo riêng từng chi nhánh | [san-pham-chi-nhanh/](san-pham-chi-nhanh/) |
| Kết nối đơn vị vận chuyển | Tích hợp 12 hãng (Viettel, GHN, J&T...) | [ket-noi-don-vi-van-chuyen/](ket-noi-don-vi-van-chuyen/) |
| Lịch sử nghiệp vụ logistics | Log sự kiện/trạng thái giao hàng | [lich-su-nghiep-vu-logistics/](lich-su-nghiep-vu-logistics/) |
| Quản lý bảo hành | Tiếp nhận, xử lý yêu cầu bảo hành | [bao-hanh/](bao-hanh/) |
| Quản lý đơn hàng hoàn | Xử lý đơn khách trả lại (xem thêm ecommerce_connect) | [don-hang-hoan/](don-hang-hoan/) |

> Khung trích từ bảng `Page` (menu thật). Chi tiết kỹ thuật cũ: [../warehouse.md](../warehouse.md).
