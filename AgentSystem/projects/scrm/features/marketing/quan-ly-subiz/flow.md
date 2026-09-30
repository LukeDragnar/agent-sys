# Luồng — Quản lý Subiz

## Tiền điều kiện
- Đơn vị đã bật tiện ích **External Chat** (`TI_ExternalChat`).
- Người dùng có quyền admin đơn vị / chi nhánh (DV-AD-TONG, DV-AD-CN) để xem/quản lý cấu hình.
- Có **tài khoản Subiz** (account_id + secret) và widget Subiz đã gắn trên website của shop.
- Đã khai báo sẵn nguồn (landing), đơn vị/chi nhánh, nhân viên sale để chọn làm mặc định.

## Luồng cấu hình (admin)
1. Admin mở khu **Cấu hình chat nguồn ngoài (Subiz)** → bấm **Thêm** (`AddChatConfig`).
2. Nhập: **account_id**, **secret (Password)**, **domain website**, chọn **nguồn (IdLandingDefault)**, **đơn vị (DonViId)**, **chi nhánh (ChiNhanhId)**, **nhân viên sale mặc định (IdUserSaleDefault / IdNhomUserSaleDefault)** → lưu (EF `ConfigChat`).
3. Cần sửa → mở chi tiết (`LayChiTiet/{id}`) → cập nhật (`UpdateChatConfig`).
4. Xem danh sách domain của cấu hình (`LayDSDomain/{id}`).
5. Xóa cấu hình không dùng (`DeleteConfig/{id}`).

## Luồng dùng (khách chat — tự động)
1. Khách nhắn tin qua widget Subiz trên website shop.
2. Subiz gửi **webhook** sự kiện về hệ thống (service Webhook_Subiz, `EventReceiveController`).
3. Hệ thống đọc cấu hình `ConfigChat` theo **account_id**, tính **HMAC từ secret (Password)** để xác thực chữ ký → đúng mới chấp nhận.
4. Tạo / cập nhật **contact** theo các giá trị mặc định trong cấu hình (nguồn, đơn vị, chi nhánh, sale).
5. Contact xuất hiện trong luồng contact/chat để Sale/Marketing chăm sóc.

## Case biên
- **Sai account_id / không tìm thấy cấu hình** → webhook không khớp cấu hình → sự kiện bị bỏ (không tạo contact).
- **Sai secret / chữ ký HMAC không khớp** → xác thực thất bại → từ chối sự kiện (chống giả mạo).
- **Thiếu giá trị mặc định** (nguồn/đơn vị/chi nhánh/sale) → contact tạo ra thiếu thông tin định tuyến → có thể không gán đúng người.
- **Lấy danh sách domain** (`LayDSDomain`) chỉ kiểm tra đăng nhập (không gắn quyền riêng) → user bất kỳ đã đăng nhập đều gọi được (lệch chuẩn so với 5 endpoint còn lại — xem dev.md).
- **Lộ secret** → kẻ xấu có thể giả chữ ký webhook, bơm contact giả; cần đảm bảo không trả secret ra response chi tiết.

## Ví dụ cụ thể có số
Shop E có website bán mỹ phẩm, tài khoản Subiz `acc_12345`.
- Admin thêm cấu hình: account_id=`acc_12345`, secret=`s3cr3t...`, domain=`myphamE.vn`, nguồn="Website", đơn vị="Shop E", sale mặc định="Chị Hoa".
- Khách nhắn "còn serum size nhỏ không?" trên website → Subiz gửi webhook → hệ thống khớp `acc_12345`, xác thực HMAC bằng secret → tạo contact mới, nguồn "Website", gán Chị Hoa.
- Chị Hoa thấy contact mới trong danh sách contact → gọi/nhắn lại chăm sóc.
- Nếu ai đó gửi webhook giả với account_id đúng nhưng chữ ký sai → hệ thống từ chối, không tạo contact.

## Hậu điều kiện
- Cấu hình lưu vào DB (`ConfigChat`); không bắn event.
- Bản ghi cấu hình được `EventReceiveService` đọc lại mỗi lần có webhook để xác thực + định tuyến contact.
- Contact mới từ Subiz được tạo theo giá trị mặc định và đi vào luồng chăm sóc.
