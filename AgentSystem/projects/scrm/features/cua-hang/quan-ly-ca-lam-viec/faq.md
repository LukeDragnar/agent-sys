> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý ca làm việc (Biên bản bàn giao ca)

**Q: Tôi lập nhầm biên bản, muốn xóa thì làm sao?**
A: Bạn chỉ xóa được biên bản khi nó **chưa xác nhận** (cả đầu ca lẫn cuối ca). Nếu đã bấm xác nhận thì hệ thống khóa, không cho xóa nữa — lúc này bạn chỉ có thể sửa lại thông tin (phần chưa bị khóa) hoặc nhờ admin xử lý. Ngoài ra, chỉ **người đã lập biên bản** mới xóa được biên bản của mình.

**Q: Vì sao tôi bấm xóa lại báo "Người tạo phiếu mới có thể xóa!"?**
A: Vì biên bản đó do người khác lập. Hệ thống chỉ cho người tạo (hoặc super user) xóa biên bản. Bạn nhờ đúng người đã lập, hoặc nhờ admin hỗ trợ.

**Q: Tại sao bấm xóa lại báo "Không thể xóa biên bản đã được xác nhận!"?**
A: Biên bản đó đã được xác nhận đầu ca hoặc cuối ca nên bị khóa để giữ số liệu đối chiếu. Biên bản đã xác nhận thì không xóa được nữa.

**Q: Tôi chưa nhập đầu ca mà muốn xác nhận luôn cuối ca có được không?**
A: Không. Thứ tự cố định là **xác nhận đầu ca trước, cuối ca sau**. Bạn cần hoàn tất và xác nhận đầu ca, rồi mới ghi nhận và xác nhận cuối ca.

**Q: "Tiền thiếu cuối ca" là gì, nhập ở đâu?**
A: Đó là khoản tiền mặt bị thiếu hụt khi đối chiếu lúc giao ca cuối ngày. Bạn nhập ở bước cuối ca, trong phần thông tin cuối ca, trước khi xác nhận cuối ca.

**Q: Một biên bản có thể ghi nhiều mặt hàng/khoản nhận không?**
A: Có. Mỗi biên bản gồm nhiều dòng chi tiết (mỗi dòng là một sản phẩm/khoản nhận trong ca). Bạn thêm các dòng này ngay trong màn biên bản.

**Q: Tôi không thấy nút thêm/xóa từng dòng chi tiết ở màn quản trị?**
A: Thao tác thêm/xóa dòng chi tiết qua màn quản trị riêng chỉ dành cho tài khoản super user của hệ thống. Người dùng cửa hàng thì nhập chi tiết trực tiếp trong biên bản, không cần vào màn quản trị đó.

**Q: Tôi sửa được biên bản đã xác nhận không?**
A: Một số thông tin của phần đã xác nhận sẽ bị khóa. Bạn vẫn xem lại được, nhưng các trường thuộc phần đã xác nhận không cho sửa để giữ tính toàn vẹn của số liệu giao ca.
