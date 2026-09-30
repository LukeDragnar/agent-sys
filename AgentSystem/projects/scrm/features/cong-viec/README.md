# Module: Công việc

> Module công việc của SCRM: tạo/giao/theo dõi công việc dạng cây cha–con, thiết kế mẫu & quy trình tự sinh việc, trung tâm chat Facebook, thùng rác khôi phục, và bộ máy thông báo realtime chạy nền. Mục lục tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team) · `faq.md` (câu hỏi khách — **chatbot ăn**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Khách hàng / chatbot chỉ thấy `summarize` + `faq`.

## Danh sách tính năng
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Quản lý công việc | Tạo/giao/theo dõi công việc (task) dạng cây cha–con, lưới + timeline + lịch, nhập Excel | [quan-ly-cong-viec/](quan-ly-cong-viec/) |
| Cấu hình công việc | Thiết lập mẫu công việc (field động), thiết kế quy trình & lượt thực thi tự sinh việc | [cau-hinh-cong-viec/](cau-hinh-cong-viec/) |
| Quản lý Chat (Chat Hub) | Trung tâm tiếp nhận tin nhắn/bình luận Facebook, chia chat tự động/thủ công, phân quyền Fanpage | [quan-ly-chat/](quan-ly-chat/) |
| Lịch sử xóa (thùng rác) | Xóa mềm công việc/lượt thực thi, khôi phục nguyên cụm hoặc xóa vĩnh viễn | [lich-su-xoa/](lich-su-xoa/) |
| Thông báo & realtime nền | Bộ máy chạy ngầm sau chuông thông báo: đẩy đơn/chat/việc/deadline qua FCM, không cần load lại | [thong-bao-realtime-nen/](thong-bao-realtime-nen/) |
| Ticket hỗ trợ (Guideline SandBox ERP) | Đề xuất/yêu cầu/bug của khách → AI gom thành phiếu chức năng → SB team xử lý, chat 2 chiều (kèm ảnh), SLA. Entity TicketCustomer/SandboxIssue + chat Mongo | [ticket-ho-tro/](ticket-ho-tro/) |

> Khung trích từ bảng `Page` (menu thật). "Thông báo & realtime nền" là Core chạy nền — không có menu riêng.
> **Ticket hỗ trợ** dùng dịch vụ WorkFlow (cùng service với Công việc). Registry node: `registry/cong-viec/TicketCustomerController.md`. *(Đắp 2026-07-15 từ workflow nghiên cứu read-only + verify code.)*
