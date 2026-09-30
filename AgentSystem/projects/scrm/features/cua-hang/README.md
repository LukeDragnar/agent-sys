# Module: Cửa hàng

> Bán hàng tại điểm/POS của SCRM: tạo & xử lý đơn tại quầy, cấu hình giá/chiết khấu & hệ thống cửa hàng, bàn giao ca, xếp nhân viên xử lý, pre-order qua QR, lịch hẹn và báo cáo bán hàng. Mục lục tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team) · `faq.md` (câu hỏi khách — **chatbot ăn**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Khách hàng / chatbot chỉ thấy `summarize` + `faq`.

## Danh sách tính năng
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Quản lý ca làm việc | Biên bản bàn giao ca tại cửa hàng: ghi người nhận ca, tổng SP/tiền đầu & cuối ca, tiền thiếu, qua 2 mốc xác nhận rồi khóa | [quan-ly-ca-lam-viec/](quan-ly-ca-lam-viec/summarize.md) |
| Cấu hình giá & chiết khấu | Khai báo bộ mặt hàng mặc định theo bàn/khu vực để POS tự gợi ý SP/DV/combo, kèm dịch vụ tính giờ & chuyển bàn | [cau-hinh-gia-chiet-khau/](cau-hinh-gia-chiet-khau/summarize.md) |
| Pre-Order | Khách quét QR tại bàn xem menu & gọi/đặt món trước (không cần đăng nhập); đơn vào danh sách đặt trước để thu ngân nhập thành đơn thật | [pre-order/](pre-order/summarize.md) |
| Quản lý nhân viên xử lý | Cấu hình quy tắc tự động gán nhân viên phục vụ cho đơn hàng tại POS (bước "Xếp nhân viên") | [quan-ly-nhan-vien-xu-ly/](quan-ly-nhan-vien-xu-ly/summarize.md) |
| Cấu hình hệ thống cửa hàng | Nhóm cài đặt nền cho cửa hàng: cấu hình Order (cờ/tham số bán hàng), danh mục máy in, quản lý khu vực/sơ đồ bàn | [cau-hinh-he-thong-cua-hang/](cau-hinh-he-thong-cua-hang/summarize.md) |
| Tạo & quản lý đơn hàng | Màn POS thu ngân xử lý trọn vòng đời đơn: chọn bàn/khách, thêm SP/DV/combo/thẻ, áp KM/voucher/chiết khấu, xếp NV, thanh toán, in HĐ | [tao-quan-ly-don-hang/](tao-quan-ly-don-hang/summarize.md) |
| Lịch hẹn khách hàng | Quản lý lịch hẹn/đặt lịch trước của cửa hàng (qua NV, website, hàng loạt), xem nhiều lát cắt, nhắc lịch SMS/Zalo, quản lý nguồn đặt | [lich-hen-khach-hang/](lich-hen-khach-hang/summarize.md) |
| Báo cáo & thống kê | Báo cáo & dashboard bán hàng tại cửa hàng: doanh thu nhiều chiều, báo cáo thu ngân, doanh số & hoa hồng NV, chấm công — chỉ xem | [bao-cao-thong-ke/](bao-cao-thong-ke/summarize.md) |

> Khung trích từ bảng `Page` (menu thật).
