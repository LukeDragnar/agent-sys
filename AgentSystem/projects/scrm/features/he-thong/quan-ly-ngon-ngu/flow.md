# Luồng — Quản lý ngôn ngữ

## Tiền điều kiện
- Người dùng đã đăng nhập (hiện code chỉ yêu cầu đăng nhập cho phần CRUD/Gen/Import, **không** gắn permission key riêng — *cần product xác nhận có siết về HT-AD không*).
- Có sẵn các bản dịch trong dữ liệu (key → giá trị theo từng ngôn ngữ) và thư mục `Resources/i18n` để chứa file ngôn ngữ tĩnh đã sinh.
- Giao diện (web/app) cấu hình đọc nhãn từ file ngôn ngữ tĩnh.

## Luồng dùng (sửa nhãn giao diện)
1. Mở **Hệ thống → Quản lý ngôn ngữ**.
2. Lọc/tìm key cần sửa; mở chi tiết để xem bản dịch theo các ngôn ngữ của key đó.
3. Thêm key mới / sửa nội dung / xóa key không dùng (hoặc nhập hàng loạt).
4. Bấm **Gen file lang** → hệ thống đọc toàn bộ bản dịch trong dữ liệu → sinh lại file ngôn ngữ tĩnh trong `Resources/i18n`.
5. Tải lại giao diện → giao diện gọi tải file ngôn ngữ (công khai) → nhận nhãn mới.
- Kết quả mong đợi: nhãn hiển thị đúng nội dung vừa sửa **sau khi đã Gen**.

## Luồng cấu hình (nhập bản dịch theo ngôn ngữ)
1. Chuẩn bị file bản dịch cho 1 ngôn ngữ.
2. Dùng chức năng Import theo đúng mã ngôn ngữ → hệ thống nạp bản dịch vào dữ liệu.
3. Bấm **Gen file lang** để áp ra file tĩnh.

## Luồng phụ (từ điển mobile)
1. Vào màn Quản lý Dictionary (mobile): grid lọc + form Thêm/Sửa/Xóa cặp Mã–Value–DefaultValue–MoTa.
2. App di động đọc toàn bộ từ điển để render nhãn/cấu hình (có bản mã hóa cho client).

## Các trường hợp / case biên
- **Sửa bản dịch nhưng quên Gen** → dữ liệu đổi nhưng file tĩnh chưa đổi → giao diện vẫn hiện nhãn cũ. Đây là case lỗi phổ biến nhất.
- **Xóa key đang được giao diện dùng** → không có cảnh báo; chỗ dùng key đó hiển thị trống/lỗi nhãn sau khi Gen lại.
- **Tải file ngôn ngữ không tồn tại** → trả 404 (giao diện không có nhãn cho ngôn ngữ đó).
- **Tải file ngôn ngữ là công khai (anonymous)** → giao diện lấy được nhãn trước đăng nhập; nhưng do ghép đường dẫn theo tham số ngôn ngữ từ phía gọi mà không kiểm tra → có **rủi ro path traversal** nếu tham số chứa `../` (ghi rõ ở dev.md, *security session xử lý sau*).
- **Phần quản trị chỉ chặn "đã đăng nhập"** → bất kỳ user đăng nhập nào cũng CRUD/Gen/Import được → rủi ro phân quyền (*cần product xác nhận*).

## Ví dụ cụ thể có số
Đội nội bộ sửa nhãn nút "Lưu" trên 1 màn hình.
- Tìm key `common.save` (giả định) → hiện giá trị tiếng Việt "Lưu lai" (sai chính tả) → sửa thành "Lưu lại" → bấm Lưu.
- Lúc này dữ liệu đã đúng nhưng file ngôn ngữ tĩnh **chưa** đổi → giao diện vẫn hiện "Lưu lai".
- Bấm **Gen file lang** → hệ thống sinh lại file i18n. Mở lại trang → nút hiện "Lưu lại".
- Nhập hàng loạt: upload file English 1.200 dòng cho mã `en` → nạp 1.200 bản dịch → Gen → giao diện chuyển sang `en` hiển thị đủ 1.200 nhãn.

## Hậu điều kiện
- Bản dịch trong dữ liệu được cập nhật ngay khi lưu/import.
- File ngôn ngữ tĩnh chỉ đổi **sau khi Gen** → đây mới là thứ giao diện thực sự đọc.
- Xóa/sửa key chỉ có hiệu lực thực tế trên giao diện sau khi Gen lại file.
