---
alias: ["quản lý Subiz", "cấu hình chat Subiz", "kết nối Subiz", "chat nguồn ngoài", "config chat", "subiz", "domain chat subiz"]
module: marketing
trang_thai: "cần bật tiện ích External Chat. Là cấu hình kết nối chat nguồn ngoài Subiz."
goi: cần product xác nhận
quyen: "cần product xác nhận — theo registry, các thao tác CRUD cấu hình (tìm/thêm/sửa/xóa/chi tiết) cho DV-AD-TONG, DV-AD-CN. Riêng 'Lấy danh sách domain' chỉ kiểm tra đăng nhập (không gắn quyền riêng)."
phu_thuoc: ["Tiện ích External Chat", "Tài khoản Subiz (account_id + secret)", "Nguồn / Landing (gán mặc định cho contact)", "Đơn vị / chi nhánh / user sale mặc định (định tuyến contact)", "Kênh nhận sự kiện chat từ Subiz"]
link_flow: ./flow.md
---

# Quản lý Subiz

## Là gì
Đây là nơi **khai báo cấu hình kết nối chat Subiz** — một công cụ chat trên website. Khi khách nhắn tin qua widget Subiz trên website của shop, **Subiz gửi sự kiện về hệ thống** (webhook); hệ thống dựa vào cấu hình ở đây để **xác thực** và **tạo / định tuyến contact** (gán đúng nguồn, đơn vị, chi nhánh, nhân viên sale).

Mỗi cấu hình gồm: **mã tài khoản Subiz (account_id)**, **mã bí mật (secret)** để xác thực chữ ký webhook, **domain website**, và các giá trị mặc định (nguồn/landing, đơn vị, chi nhánh, nhân viên sale) để gán cho contact mới.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách cấu hình Subiz | ✅ | Admin tổng / Admin chi nhánh (DV-AD-TONG, DV-AD-CN) |
| Thêm cấu hình Subiz mới | ✅ | tùy quyền (key riêng) |
| Sửa cấu hình | ✅ | tùy quyền |
| Xem chi tiết một cấu hình | ✅ | tùy quyền |
| Xóa cấu hình | ✅ | tùy quyền |
| Lấy danh sách domain của cấu hình | ✅ | chỉ cần đăng nhập (không gắn quyền riêng) |
| **Trả lời tin nhắn Subiz tại màn này** | ❌ | đây chỉ là màn **cấu hình kết nối**; tin nhắn đến được tạo thành contact, xử lý ở luồng contact/chat |

## Để làm gì / cho ai
- **Admin đơn vị / chi nhánh:** thiết lập để chat Subiz trên website tự đổ về hệ thống thành contact, không bỏ sót khách nhắn từ website.
- **Marketing / Sale:** nhận contact mới từ website (qua Subiz) đã được gán sẵn nguồn + nhân viên phụ trách để chăm sóc.

## Cách dùng
1. Vào khu **Cấu hình chat nguồn ngoài (Subiz)** (cần tiện ích External Chat).
2. Bấm **Thêm**, nhập: **account_id** (lấy từ tài khoản Subiz), **secret** (mã bí mật để xác thực), **domain website**, và chọn **nguồn (landing)**, **đơn vị / chi nhánh**, **nhân viên sale mặc định**.
3. Lưu. Cấu hình hình thành "cầu nối" giữa Subiz và hệ thống.
4. Khi khách chat trên website qua Subiz → sự kiện về hệ thống → hệ thống xác thực bằng secret rồi tạo contact theo cấu hình.
5. Cần xem/sửa lại → mở chi tiết cấu hình; có thể xem **danh sách domain** đã khai báo.

## Cần chú ý
- **Phải bật tiện ích External Chat** mới dùng được.
- **Mã bí mật (secret) rất quan trọng** — đây là chìa khóa xác thực chữ ký webhook. Ai có được secret có thể giả mạo sự kiện chat. Không chia sẻ ra ngoài; khi nghi lộ, đổi secret bên Subiz và cập nhật lại.
- **account_id phải khớp** với tài khoản Subiz thật, nếu không hệ thống sẽ không nhận diện được sự kiện gửi về.
- **Giá trị mặc định (nguồn / đơn vị / chi nhánh / sale)** quyết định contact mới được gán cho ai và tính về nguồn nào — đặt sai thì contact về sai người / sai nguồn.
- Đây là **màn cấu hình**, không phải màn chat. Việc trả lời khách diễn ra ở luồng contact / chat tương ứng.

## Ảnh hưởng tới
- **Contact / khách hàng:** chat Subiz về tạo contact mới theo cấu hình (nguồn, đơn vị, chi nhánh, sale).
- **Báo cáo nguồn / marketing:** nguồn (landing) gán mặc định quyết định contact website tính về nguồn nào.
- **Phân bổ Sale:** nhân viên sale mặc định nhận contact từ website.
