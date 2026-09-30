> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Công nợ (Bù trừ công nợ + Báo cáo công nợ)

**Q: Bù trừ công nợ là gì, khi nào dùng?**
A: Khi một đối tác vừa nợ bạn (bạn bán cho họ — phải thu) vừa được bạn nợ (bạn mua của họ — phải trả), thay vì thu/chi qua lại, bạn lập một phiếu bù trừ để cấn trừ hai khoản. Kết quả là công nợ hai bên cùng giảm đúng phần đã cấn.

**Q: Tôi bấm Lấy chứng từ nhưng không ra dòng nào?**
A: Có thể đối tượng bạn chọn chưa có công nợ phải thu/phải trả trong khoảng đó, hoặc bạn chưa chọn đúng tài khoản phải thu/phải trả. Kiểm tra lại đối tượng, tài khoản và ngày bù trừ. Chỉ những chứng từ có số tiền bù trừ > 0 mới đưa vào danh sách.

**Q: Lập phiếu bù trừ xong mà công nợ một bên giảm, một bên không giảm?**
A: Bình thường không xảy ra vì hệ thống chỉ lưu khi cả hai bên (giảm phải thu khách + giảm phải trả nhà cung cấp) đều thành công; một bên lỗi thì hủy cả phiếu. Nếu bạn thấy lệch, liên hệ hỗ trợ để rà soát — có cơ chế khôi phục dữ liệu.

**Q: Tôi không xóa được một phiếu bù trừ?**
A: Phiếu bù trừ **tạo tự động** (hệ thống sinh khi mua/bán) không cho xóa thủ công. Chỉ phiếu bạn lập tay mới xóa được. Khi xóa, hệ thống hoàn lại công nợ đã giảm cho cả hai bên — và xóa là vĩnh viễn.

**Q: Báo cáo công nợ báo "Bạn không có quyền xem báo cáo đơn hàng"?**
A: Báo cáo công nợ dùng chung quyền với báo cáo đơn hàng. Bạn cần được cấp quyền xem báo cáo đơn hàng (cấu hình bên module Đơn hàng). Liên hệ admin để được cấp.

**Q: Sự khác nhau giữa 131 và 331?**
A: 131 là công nợ **phải thu** — tiền khách hàng còn nợ bạn. 331 là công nợ **phải trả** — tiền bạn còn nợ nhà cung cấp. Báo cáo công nợ tổng hợp có tab riêng cho mỗi loại.

**Q: Trên báo cáo, một dòng hiển thị "KH: ... - NCC: ..." nghĩa là gì?**
A: Đó là đối tượng có cùng một mã vừa là khách hàng vừa là nhà cung cấp. Hệ thống ghép cả hai tên để bạn nhận biết. Đây cũng chính là kiểu đối tượng hay dùng để bù trừ công nợ.

**Q: Tôi muốn sửa số công nợ của một khách cho đúng thì làm ở đâu?**
A: Công nợ không sửa tay trực tiếp; nó phát sinh từ chứng từ gốc (phiếu mua, phiếu bán, phiếu thu, phiếu chi). Muốn điều chỉnh, bạn xử lý ở chứng từ gốc hoặc dùng nghiệp vụ bù trừ/chứng từ khác. Số dư ban đầu (lúc mới dùng phần mềm) khai ở màn Thu chi.
