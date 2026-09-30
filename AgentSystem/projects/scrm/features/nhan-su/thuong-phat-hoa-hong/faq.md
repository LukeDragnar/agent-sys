> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Thưởng phạt & hoa hồng nhân viên

**Q: Tôi lưu cấu hình thưởng doanh số thì các bậc cũ biến mất hết?**
A: Đúng cơ chế. Khi lưu, hệ thống **thay toàn bộ** cấu hình thưởng doanh số của chi nhánh bằng danh sách bạn vừa gửi. Vì vậy mỗi lần lưu phải gửi **đầy đủ** tất cả các bậc muốn giữ, không chỉ phần thay đổi. Lưu danh sách rỗng sẽ xóa sạch cấu hình của chi nhánh.

**Q: Lưu cấu hình thưởng doanh số báo lỗi "Giá trị doanh thu từ không thể lớn hơn giá trị doanh thu đến"?**
A: Mỗi bậc phải có "doanh số đến" lớn hơn "doanh số từ". Hãy kiểm tra lại các mốc, đảm bảo khoảng tăng dần và không để mốc nào bằng 0 hoặc bỏ trống.

**Q: Tôi đặt thưởng theo % mà báo lỗi "Giá trị thưởng không được > 100%"?**
A: Khi chọn đơn vị thưởng là phần trăm, mức thưởng mỗi bậc không được vượt quá 100%. Nếu muốn thưởng số tiền cố định lớn, hãy chọn đơn vị VND thay vì %.

**Q: Hoa hồng "mặc định" và hoa hồng "theo nhân viên" khác nhau thế nào?**
A: Hoa hồng mặc định áp cho mọi nhân viên chưa có cấu hình riêng (trong hệ thống lưu dưới dạng nhân viên rỗng). Nếu một nhân viên có cấu hình hoa hồng riêng thì hệ thống ưu tiên dùng cấu hình riêng đó; ai chưa có thì mới lấy mặc định.

**Q: Kế toán không sửa được cấu hình hoa hồng?**
A: Đúng. Quyền lưu cấu hình hoa hồng dành cho admin/quản lý đơn vị; kế toán chỉ xem. Đây là thiết kế phân quyền.

**Q: Vì sao nhân viên báo hoa hồng tính ra khác nhau giữa các đơn?**
A: Hoa hồng phụ thuộc cấu hình theo đối tượng (dịch vụ/SP/thẻ/combo), hoa hồng tối đa (trần) và hoa hồng theo nhóm. Mỗi mặt hàng trên đơn có thể rơi vào cấu hình khác nhau. Hãy kiểm tra cấu hình hoa hồng của nhân viên cho từng đối tượng.

**Q: Quản lý xem được hoa hồng của cả nhóm, nhân viên chỉ thấy của mình?**
A: Đúng. Quản lý/admin xem hoa hồng của tất cả nhân viên trong đơn; nhân viên thường chỉ xem hoa hồng của chính mình.

**Q: Thêm khoản phạt cho nhân viên thì danh sách "loại phạt" trống, không có gì để chọn?**
A: Loại thưởng/phạt lấy từ **danh mục thưởng phạt**. Admin tổng cần vào cấu hình > thưởng phạt > danh mục để khai báo trước thì mới có loại để chọn khi ghi thưởng/phạt cho nhân viên.

**Q: Tổng thưởng/phạt của nhân viên dùng để làm gì?**
A: Tổng thưởng/phạt/khấu trừ là đầu vào cho bảng lương / tổng hợp công của nhân viên. Ghi sai/thiếu khoản sẽ làm lệch lương.
