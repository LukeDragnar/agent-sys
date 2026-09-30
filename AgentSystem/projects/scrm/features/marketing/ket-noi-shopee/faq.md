> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Kết nối Shopee

**Q: Tôi bấm "Kết nối Shopee" mà không thấy chuyển sang trang đăng nhập Shopee?**
A: Việc kết nối cần đơn vị đã **bật tiện ích Shopee** và bạn có quyền (Admin / Marketing). Nếu vẫn không chuyển, hãy thử vào màn **Gian hàng TMĐT** chọn Shopee để thêm kết nối. (Lưu ý kỹ thuật: một số đường nút cũ chưa hoàn thiện — liên hệ hỗ trợ nếu màn không phản hồi.)

**Q: Ủy quyền Shopee xong rồi, làm sao để đơn Shopee về hệ thống?**
A: Sau khi ủy quyền, bạn phải **map gian hàng** (gắn gian hàng Shopee với đơn vị + chọn kho nhận đơn). Khi map, hệ thống gắn sẵn đường nhận đơn; từ đó đơn mới trên Shopee tự về hệ thống.

**Q: Đơn Shopee không về, tôi cần kiểm tra gì?**
A: Kiểm tra: (1) gian hàng còn kết nối (token chưa hết hạn) không; (2) đã **map gian hàng** với đúng kho chưa; (3) cấu hình kết nối còn hiệu lực. Nếu token hết hạn, hãy **ủy quyền lại**.

**Q: Tôi muốn tồn trên Shopee khớp với tồn trong hệ thống?**
A: Vào phần **Sản phẩm gian hàng**, **ghép sản phẩm** Shopee với sản phẩm hệ thống (tự động theo SKU hoặc ghép tay), rồi bấm **Đồng bộ tồn**. Hệ thống sẽ đẩy số lượng tồn lên Shopee. Lưu ý đồng bộ là một chiều (hệ thống → Shopee).

**Q: Ghép tự động không khớp hết sản phẩm thì sao?**
A: Ghép tự động dựa trên **mã SKU** trùng nhau. Sản phẩm nào không trùng SKU sẽ không ghép được — bạn ghép **thủ công** từng cái, chọn đúng sản phẩm trong hệ thống.

**Q: Đồng bộ tồn báo lỗi "không tìm thấy kho"?**
A: Gian hàng phải được cấu hình **kho đồng bộ tồn** (hoặc kho nhận đơn). Vào lại cấu hình gian hàng, chọn kho rồi đồng bộ lại.

**Q: Xóa kết nối gian hàng có mất dữ liệu đơn cũ không?**
A: Xóa kết nối gian hàng sẽ **ngắt việc nhận đơn mới và ngừng đồng bộ** của gian hàng đó. Đơn đã về hệ thống trước đó vẫn còn. Cân nhắc kỹ trước khi xóa.

**Q: Tôi sửa giá / tên sản phẩm trên hệ thống thì Shopee có tự đổi theo không?**
A: Không. Màn này chỉ **đồng bộ tồn (số lượng)** một chiều lên Shopee. Việc đổi giá / tên / nội dung sản phẩm trên Shopee bạn làm trực tiếp trên Shopee Seller.
