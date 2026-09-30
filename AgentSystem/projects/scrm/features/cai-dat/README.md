# Module: Cài đặt

> Khu **Cài đặt của đơn vị (shop)**: chủ shop / admin đơn vị khai báo thiết lập chung, gói cước, AI, thông báo, tài chính, kiểm soát thiết bị đăng nhập và tra nhật ký hoạt động. Mục lục tính năng — mỗi tính năng 1 folder.
>
> **Mỗi feature folder gồm 4 file:** `summarize.md` (khách + team) · `faq.md` (câu hỏi khách — **chatbot ăn**) · `flow.md` (luồng/test/đào tạo) · `dev.md` (kỹ thuật, chỉ dev).
> Khách hàng / chatbot chỉ thấy `summarize` + `faq`.

## Danh sách tính năng
| Tính năng | Mô tả ngắn | Folder |
|---|---|---|
| Cấu hình hệ thống | Tham số chung của đơn vị, bảo mật tài khoản, chi nhánh, tài khoản ngân hàng thu/chi | [cau-hinh-he-thong/](cau-hinh-he-thong/) |
| Cấu hình gói sử dụng | Xem/nâng cấp/gia hạn gói, nạp tiền SMS (qua duyệt admin hệ thống) | [cau-hinh-goi-su-dung/](cau-hinh-goi-su-dung/) |
| Cấu hình AI | Khai báo token (khóa) gọi model AI + bộ prompt theo đơn vị | [cau-hinh-ai/](cau-hinh-ai/) |
| Cấu hình thông báo | Thiết lập tự động gửi thông báo web khi có sự kiện + kho mẫu thông báo | [cau-hinh-thong-bao/](cau-hinh-thong-bao/) |
| Quản lý tài chính | Cấu hình đối tác/tài khoản trả góp + yêu cầu & lịch sử đổi domain đơn vị | [quan-ly-tai-chinh/](quan-ly-tai-chinh/) |
| Quản lý quyền & tài khoản | Kiểm soát thiết bị đăng nhập: duyệt thiết bị, cấp slot, log đăng nhập | [quan-ly-quyen-tai-khoan/](quan-ly-quyen-tai-khoan/) |
| Lịch sử hoạt động | Nhật ký (audit log): ai thêm/sửa/xóa/lọc/xuất Excel/xem thông tin KH | [lich-su-hoat-dong/](lich-su-hoat-dong/) |
