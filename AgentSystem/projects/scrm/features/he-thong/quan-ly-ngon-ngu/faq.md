> Draft — cần bổ sung từ ticket vận hành thật.

# FAQ — Quản lý ngôn ngữ

**Q: Tôi sửa nhãn trong Quản lý ngôn ngữ rồi mà giao diện vẫn hiển thị chữ cũ?**
A: Sau khi sửa, bạn phải bấm **"Gen file lang"** để hệ thống sinh lại file ngôn ngữ. Giao diện đọc nhãn từ file đó, nên nếu chưa Gen lại thì vẫn thấy nhãn cũ. Sau khi Gen, tải lại trang (hoặc mở lại app) để nhận nhãn mới.

**Q: "Gen file lang" để làm gì?**
A: Đây là bước "xuất bản" bản dịch: hệ thống lấy toàn bộ bản dịch trong dữ liệu rồi tạo ra các file ngôn ngữ tĩnh. Giao diện luôn đọc nhãn từ các file này (không đọc trực tiếp từ dữ liệu chỉnh sửa), nên Gen là bước bắt buộc để thay đổi có hiệu lực.

**Q: Tôi muốn thêm một ngôn ngữ mới thì làm thế nào?**
A: Chuẩn bị file bản dịch cho ngôn ngữ đó, dùng chức năng **Nhập bản dịch (Import)** theo đúng mã ngôn ngữ để nạp vào, sau đó **Gen file lang**. Chi tiết quy trình chuẩn cho từng ngôn ngữ *cần product xác nhận*.

**Q: Xóa một key dịch có an toàn không?**
A: Cần cẩn thận. Hệ thống không kiểm tra xem giao diện có còn dùng key đó hay không, nên nếu xóa nhầm key đang dùng, chỗ hiển thị tương ứng sẽ bị trống hoặc hiện sai. Hãy chắc chắn key không còn dùng trước khi xóa.

**Q: Vì sao bất kỳ ai đăng nhập cũng vào sửa được bản dịch?**
A: Hiện phần quản lý ngôn ngữ chỉ chặn ở mức "phải đăng nhập", chưa gắn quyền riêng cho admin hệ thống. Đây là điểm cần siết lại về phân quyền (*cần product xác nhận*). Trong thực tế nên giới hạn cho đội nội bộ.

**Q: Khác nhau giữa "ngôn ngữ giao diện web" và "từ điển mobile"?**
A: Ngôn ngữ giao diện web quản lý nhãn cho bản web theo từng key dịch. Từ điển mobile (Dictionary) quản lý nhãn/cấu hình riêng cho app di động theo cặp Mã–Giá trị. Hai phần tách biệt, sửa phần nào ảnh hưởng phần đó.

**Q: Giao diện tải file ngôn ngữ khi nào?**
A: Khi mở app/giao diện, giao diện tự gọi để tải file ngôn ngữ tương ứng — kể cả trước khi đăng nhập, vì cần có nhãn để hiển thị màn đăng nhập. Đây là hành vi bình thường.
