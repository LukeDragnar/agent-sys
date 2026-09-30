> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Thiết lập tác nghiệp

**Q: "Thiết lập tác nghiệp" để làm gì?**
A: Để cài sẵn quy trình chăm khách kiểu "Nếu – Thì": nếu nhân viên làm việc A ra kết quả B thì làm tiếp việc C sau bao lâu. Nhờ đó sale chăm khách theo đúng kịch bản và hệ thống tự nhắc bước tiếp theo, không bị bỏ sót.

**Q: Tôi thêm luật nhưng báo "Cấu hình tác nghiệp đã tồn tại"?**
A: Đã có một luật trùng với cặp "Nếu tác nghiệp + Kết quả" mà bạn đang nhập. Mỗi cặp này chỉ được khai báo một lần. Hãy tìm luật cũ trong danh sách để sửa thay vì thêm mới.

**Q: Sao chi nhánh khác không thấy luật tôi vừa cài?**
A: Tùy cấu hình của đơn vị. Nếu đơn vị bật "tác nghiệp theo chi nhánh" thì mỗi chi nhánh có bộ luật riêng — luật bạn cài chỉ áp cho chi nhánh của bạn. Nếu tắt thì cả đơn vị dùng chung. Nhờ admin kiểm tra cấu hình này.

**Q: Đơn vị mới chưa biết cài luật gì thì làm sao?**
A: Bạn có thể bấm **Copy** để lấy một bộ luật mẫu (theo ngành nghề) về đơn vị mình, rồi chỉnh sửa lại cho phù hợp. Lưu ý màn này chỉ copy về chính đơn vị bạn đang dùng.

**Q: Tôi muốn copy luật từ đơn vị này sang đơn vị khác được không?**
A: Qua màn Thiết lập tác nghiệp thông thường thì không — hệ thống luôn copy về đơn vị bạn đang đăng nhập. Việc copy chéo giữa các đơn vị thuộc quyền quản trị hệ thống (super-admin).

**Q: Một luật có bắt buộc phải có "bước tiếp theo" và "sau bao lâu" không?**
A: Một luật tối thiểu cần "Nếu tác nghiệp" và "Kết quả". Phần "bước tiếp theo" + "sau bao lâu" là để hệ thống nhắc làm tiếp; theo thiết kế hai phần này nên đi cùng nhau (có cái này thì có cái kia).

**Q: Tôi xóa một loại tác nghiệp trong danh mục, các luật đang dùng nó thì sao?**
A: Luật vẫn còn nhưng vế dùng tác nghiệp đã xóa sẽ hiển thị kèm chữ "(Deleted)" để bạn biết cần sửa lại. Nên rà các luật này và chọn lại giá trị hợp lệ.

**Q: Ai được phép cài đặt tác nghiệp?**
A: Việc thêm/sửa/xóa luật cần quyền admin của đơn vị hoặc chi nhánh. Nhân viên sale thông thường chỉ thực hiện theo luật chứ không cài luật.
