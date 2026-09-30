> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý công việc

**Q: Tôi tạo công việc con nhưng báo "chỉ cho phép tối đa N cấp"?**
A: Hệ thống giới hạn số cấp công việc con (mặc định 3 cấp con). Nếu cây đã sâu tới mức tối đa, bạn không tạo thêm cấp dưới được. Có thể tăng giới hạn ở Cấu hình công việc, hoặc tạo công việc ở cấp cao hơn.

**Q: Tôi lỡ xóa nhầm công việc thì lấy lại được không?**
A: Được. Xóa công việc là xóa mềm, dữ liệu vào màn **Lịch sử xóa**. Vào đó tìm và bấm Khôi phục để lấy lại cả cụm (gồm công việc con). Lưu ý màn Lịch sử xóa thường chỉ admin tổng đơn vị thấy.

**Q: Vì sao xóa một công việc lại mất luôn nhiều việc khác?**
A: Vì công việc có cấu trúc cây cha–con. Xóa việc cha sẽ kéo theo toàn bộ việc con và các lượt thực thi quy trình gắn vào đó. Hãy kiểm tra số lượng ảnh hưởng (hệ thống hiển thị trước khi xóa) rồi mới xác nhận.

**Q: Nhập danh sách công việc từ Excel được không, tối đa bao nhiêu?**
A: Được. Tải file mẫu, điền dữ liệu rồi tải lên. Mỗi lần tối đa 1.000 dòng. Có chế độ kiểm thử để kiểm tra trước mà chưa ghi. Hệ thống sẽ báo dòng nào lỗi.

**Q: Tôi và đồng nghiệp cùng sửa một công việc, kết quả ai thắng?**
A: Hiện hệ thống lưu theo lần lưu sau cùng (ghi đè), chưa có cảnh báo khi hai người sửa cùng lúc. Nên thống nhất ai phụ trách trước khi sửa để tránh mất thay đổi.

**Q: "Tự động sắp xếp thời gian" có tự lưu không?**
A: Không. Nó chỉ tính trước và cho bạn xem thử ngày mới của cả cây. Bạn phải xác nhận thì hệ thống mới ghi lại.

**Q: Báo cáo công việc tính "quá hạn" như thế nào?**
A: Quá hạn tính theo ngày bắt đầu dự kiến cộng thời gian ước lượng (estimate) — nếu mốc đó đã qua mà việc chưa xong. Lưu ý nếu công việc chỉ có ngày kết thúc dự kiến mà thiếu estimate thì có thể không bị tính là quá hạn.

**Q: Vì sao đồng nghiệp thấy danh sách công việc khác tôi?**
A: Phạm vi xem phụ thuộc vai trò và cấu hình tìm kiếm của đơn vị. Admin tổng thường thấy toàn đơn vị; một số báo cáo lại giới hạn theo chi nhánh của người đăng nhập. Nếu nghi sai phạm vi, báo admin kiểm tra.
