> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Thu chi (Quỹ tiền mặt / tiền gửi)

**Q: Tôi muốn lập một phiếu chi mới ở màn Thu chi mà không thấy nút tạo phiếu?**
A: Màn Thu chi chủ yếu để **xem và tổng hợp** dòng tiền. Phiếu thu/chi thật thường phát sinh tự động từ luồng bán hàng, chứng từ mua bán hoặc bù trừ công nợ. Nếu bạn cần ghi nhận một khoản thu/chi, hãy thực hiện từ đúng nghiệp vụ gốc (đơn hàng, phiếu mua/bán, chứng từ khác). Nếu vẫn cần phiếu chi độc lập, liên hệ bộ phận hỗ trợ để được hướng dẫn theo cấu hình đơn vị.

**Q: Vì sao đồng nghiệp thấy nhiều phiếu thu-chi hơn tôi?**
A: Hệ thống phân quyền theo vai trò. Admin tổng hoặc admin nhiều chi nhánh xem được dòng tiền toàn đơn vị; người dùng thường chỉ thấy phiếu của chi nhánh mình. Đây là thiết lập, không phải lỗi.

**Q: Tồn quỹ "đầu kỳ / cuối kỳ" được tính thế nào?**
A: Hệ thống cộng dồn các phiếu thu trừ phiếu chi (và nạp/rút) theo khoảng ngày bạn lọc. Đầu kỳ là số dư trước khoảng ngày, trong kỳ là phát sinh, cuối kỳ là kết quả sau khi cộng/trừ. Đổi bộ lọc ngày thì các con số này thay đổi theo.

**Q: Số dư ban đầu để làm gì, có bắt buộc không?**
A: Khi mới chuyển sang dùng phần mềm, công nợ khách/nhà cung cấp và số dư quỹ thực tế đã có sẵn. Số dư ban đầu giúp khai báo các số này để báo cáo công nợ và quỹ khớp với thực tế. Không bắt buộc, nhưng nếu bỏ qua thì công nợ/quỹ sẽ tính từ 0.

**Q: Tôi khai số dư ban đầu cho một khách hàng nhưng công nợ của khách không đổi?**
A: Số dư ban đầu của khách/nhà cung cấp được ghi sang phần công nợ tương ứng. Nếu không thấy thay đổi, có thể lần ghi gặp lỗi ở một bước. Hãy kiểm tra lại dòng số dư đã khai và công nợ của khách; nếu lệch, liên hệ hỗ trợ để rà soát.

**Q: Xóa số dư ban đầu có khôi phục lại được không?**
A: Không. Xóa số dư ban đầu là xóa vĩnh viễn và đồng thời hoàn lại phần công nợ đã ghi cho khách/nhà cung cấp. Hãy chắc chắn trước khi xóa.

**Q: Khi đổi phương thức thanh toán của một đơn hàng thì phiếu thu-chi cũ có sai không?**
A: Không. Hệ thống tự khớp lại: phương thức bị bỏ thì xóa phiếu tương ứng, phương thức mới thì thêm phiếu, phương thức còn lại thì cập nhật số tiền. Bạn không cần sửa tay phiếu thu-chi của đơn.

**Q: Xuất Excel báo "Không có dữ liệu để xuất" dù tôi thấy có phiếu?**
A: Mỗi nút xuất Excel áp đúng bộ lọc của loại phiếu đó (ví dụ tab thu tiền mặt chỉ xuất phiếu thu tiền mặt). Hãy chọn đúng tab/loại phiếu có dữ liệu rồi xuất lại.
