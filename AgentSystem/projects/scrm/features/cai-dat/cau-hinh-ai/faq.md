> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Cấu hình AI

**Q: Token AI để làm gì? Lấy token ở đâu?**
A: Token là "chìa khóa" để hệ thống gọi model AI sinh câu trả lời. Bạn khai báo token vào màn **Danh sách Token AI** theo từng loại AI. Nguồn token (mua/cấp ở đâu) tùy nhà cung cấp model — cần product/kỹ thuật hướng dẫn (cần product xác nhận).

**Q: Khác nhau giữa Token AI và Prompt AI?**
A: **Token** là khóa gọi model (xác thực). **Prompt** là nội dung chỉ dẫn cho AI (kịch bản, ngữ cảnh, giọng văn trả lời). Cần cả hai: token để gọi được, prompt để AI trả lời đúng ý.

**Q: "Mặc định" ở token nghĩa là gì?**
A: Khi có nhiều token, token được tích **Mặc định** là token chính được tính năng AI dùng. Nên đảm bảo luôn có đúng một token mặc định còn hiệu lực.

**Q: Tôi mới mở shop, chưa có prompt nào, làm sao có nhanh?**
A: Bấm **Khởi tạo mẫu** ở màn Quản lý Prompt — hệ thống sẽ clone bộ prompt mẫu chuẩn sang shop của bạn để bạn chỉnh lại. Lưu ý nút này chỉ chạy khi shop **chưa có** prompt nào.

**Q: Tôi bấm "Khởi tạo mẫu" mà báo "đơn vị đã khởi tạo mẫu"?**
A: Vì shop của bạn đã có ít nhất một prompt. Tính năng khởi tạo mẫu chỉ áp dụng cho shop chưa có prompt nào, để tránh tạo trùng. Bạn cứ Thêm mới hoặc Sửa các prompt hiện có.

**Q: Vì sao tôi không thấy prompt/token của shop khác?**
A: Token và prompt gắn theo từng shop (đơn vị). Bạn chỉ thấy và sửa được dữ liệu của shop mình — đây là thiết kế để cô lập dữ liệu giữa các shop.

**Q: AI/bot đột nhiên không trả lời, có liên quan cấu hình này không?**
A: Có thể. Hãy kiểm tra token: token có còn hiệu lực không, có bị xóa/sửa sai không, token mặc định có đúng không. Token sai hoặc bị xóa sẽ làm AI ngừng trả lời.

**Q: Sửa prompt rồi mà AI vẫn trả lời như cũ?**
A: Kiểm tra bạn đã Lưu prompt đúng (đúng mã/đơn vị) chưa, và prompt đó có phải prompt mà tính năng AI đang dùng không. Nếu vẫn vậy, liên hệ hỗ trợ kỹ thuật.
