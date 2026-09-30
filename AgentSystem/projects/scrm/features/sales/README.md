# Module: Sales

> Vận hành đội bán hàng SCRM: quản lý contact/lead, phân bổ lead cho sale, tác nghiệp chăm sóc, telesales/tổng đài, lịch hẹn, kho số và xử lý đơn hàng — kèm cấu hình & báo cáo năng suất. Mục lục tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team) · `faq.md` (câu hỏi khách — **chatbot ăn**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Khách hàng / chatbot chỉ thấy `summarize` + `faq`.

## Danh sách tính năng
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Quản lý Contact | Danh sách liên hệ (contact/lead) sale & MKT chăm sóc: thông tin, kết quả tác nghiệp, nhãn, lịch hẹn, phát hiện trùng số + Black list | [quan-ly-contact/](quan-ly-contact/summarize.md) |
| Thiết lập tác nghiệp | Luật chăm khách dạng "NẾU – THÌ" cho sale (tác nghiệp A + kết quả B → gợi ý C sau thời gian D); nhắc việc theo kịch bản | [thiet-lap-tac-nghiep/](thiet-lap-tac-nghiep/summarize.md) |
| Cấu hình Sale | Bộ cấu hình tổng cho bán hàng của sale: contact, đơn hàng sale, giảm giá theo giá trị đơn, kho số, chia contact xoay vòng | [cau-hinh-sale/](cau-hinh-sale/summarize.md) |
| Chia Lead / số | Trung tâm cấu hình & vận hành chia lead cho sale/CSKH theo 3 cách: theo nguồn, tự động, thủ công; có công tắc nhận lead & giới hạn trùng | [chia-lead-so/](chia-lead-so/summarize.md) |
| Sale DMS | Công cụ sale đi thị trường theo tuyến/lịch ghé điểm bán: bán hàng, kiểm tồn, ghi kết quả viếng thăm (⚠️ chưa thấy code triển khai) | [sale-dms/](sale-dms/summarize.md) |
| Telesales | Quy trình đội gọi điện chăm/bán qua điện thoại trên nền Contact: nhận data, gọi qua tổng đài, ghi kết quả, gắn nhãn, đặt lịch/chốt đơn | [telesales/](telesales/summarize.md) |
| Lịch hẹn gặp khách | Đặt & quản lý lịch hẹn (booking) với khách: gắn dịch vụ/combo, gán NV, nhắc lịch, theo dõi trạng thái (NV/khách/hàng loạt/từ contact) | [lich-hen-gap-khach/](lich-hen-gap-khach/summarize.md) |
| Quản lý đơn hàng | Trung tâm đơn bán (logistic): vòng đời đơn nháp → chốt → xuất kho (IMEI/lô) → đăng vận chuyển → cập giao hàng → đối soát → phát hành HĐ | [quan-ly-don-hang/](quan-ly-don-hang/summarize.md) |
| Tổng đài | Gọi điện cho khách ngay trên hệ thống (click-to-call), lưu lịch sử + ghi âm, gắn cuộc gọi với contact; cấu hình đầu số/IVR (3 phiên bản) | [tong-dai/](tong-dai/summarize.md) |
| Quản lý kho số | "Bể" chứa số/lead chưa có người phụ trách; sale vào xem & chọn số (nhặt lead), có cooldown/giới hạn tránh tranh giành | [quan-ly-kho-so/](quan-ly-kho-so/summarize.md) |
| Báo cáo Sales | Báo cáo quản lý đội sale: chia số, năng suất/KPI, hiệu quả bán (tỉ lệ chốt, doanh số, KH cũ/mới, theo nguồn); chủ yếu chỉ đọc + xuất Excel | [bao-cao-sales/](bao-cao-sales/summarize.md) |
| ChatHub Sale | Phân quyền tương tác nhân viên ↔ Fanpage Facebook trong ChatHub (đăng/sửa/xóa bài, comment, đọc/trả/xóa inbox); add-on cần TI_ChatHub | [chathub-sale/](chathub-sale/summarize.md) |

> Khung trích từ bảng `Page` (menu thật). Chi tiết kỹ thuật cũ: [../sales.md](../sales.md).
