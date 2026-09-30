> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý đơn hàng

**Q: Tôi bấm tạo đơn (hoặc chốt đơn) mà báo lỗi đang xử lý / không cho tạo?**
A: Hệ thống có cơ chế chống tạo đơn trùng cho cùng một số điện thoại khách: khi bạn vừa tạo/chốt một đơn cho contact đó, hệ thống khóa tạm khoảng một phút. Bạn đợi vài giây rồi thử lại, hoặc kiểm tra xem đơn đã được tạo chưa để tránh tạo trùng.

**Q: Tôi là admin nhưng không cập nhật được trạng thái giao hàng hàng loạt, báo "Bạn không có quyền"?**
A: Quyền cập nhật trạng thái giao hàng / đối soát / care hàng loạt không nằm trong phân quyền vai trò thông thường, mà nằm trong **cấu hình quản trị**. Bạn cần được bật quyền cho user của mình trong phần cấu hình quản trị thì mới thao tác được, kể cả khi bạn là admin.

**Q: Vì sao tôi không phát hành được hóa đơn điện tử?**
A: Phát hành hóa đơn điện tử cần đơn vị đã mua **tiện ích hóa đơn** (Misa hoặc Viettel). Nếu chưa có tiện ích này thì hệ thống chặn phát hành. Liên hệ bộ phận hợp đồng để kích hoạt tiện ích.

**Q: Tôi muốn xóa nhiều đơn cùng lúc nhưng không thấy / không bấm được?**
A: Xóa nhiều đơn theo bộ lọc là thao tác phá dữ liệu lớn nên chỉ **admin tổng đơn vị** mới làm được, và thao tác này có ghi log. Người dùng khác chỉ xóa được từng đơn (nếu được cấp quyền).

**Q: Đăng đơn Ahamove báo lỗi, kiểm tra ở đâu?**
A: Khi đăng đơn Ahamove, hệ thống lấy mã giao dịch và gửi sang hệ thống vận chuyển. Lỗi thường do thông tin đơn chưa đủ (địa chỉ, người gửi) hoặc do phía sàn vận chuyển trả lỗi. Kiểm tra lại thông tin đơn và thử đăng lại; nếu vẫn lỗi, xem nội dung lỗi sàn trả về.

**Q: Tôi xuất Excel đơn hàng mà báo lỗi "chưa xuất được lúc này"?**
A: Dịch vụ xuất file đang bận (báo mã tạm 503). Bạn đợi một lát rồi xuất lại. Nếu cần, lọc bớt phạm vi đơn (theo thời gian, trạng thái) để file nhẹ hơn.

**Q: Quét IMEI / lô date để làm gì khi xuất kho?**
A: Với sản phẩm quản lý theo IMEI hoặc lô/hạn dùng (lô date), khi xuất kho bạn phải quét đúng IMEI / lô date của hàng giao cho khách. Việc này gắn chính xác sản phẩm thực tế vào đơn, phục vụ bảo hành và truy xuất sau này. Sau khi quét, bấm Hoàn thành sản phẩm để trừ tồn.

**Q: Đơn của tôi không lên kế toán / không sinh phiếu bán?**
A: Phiếu bán chỉ sinh sang kế toán khi đơn đạt đến trạng thái cấu hình (thường là một mốc trạng thái giao hàng). Kiểm tra trạng thái đơn và cấu hình mốc sinh phiếu; nếu đúng mốc mà vẫn chưa lên, nhờ kỹ thuật kiểm tra đồng bộ kế toán.
