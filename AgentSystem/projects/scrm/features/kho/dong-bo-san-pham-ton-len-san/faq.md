> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Đồng bộ sản phẩm & tồn kho lên sàn TMĐT

**Q: Sao tồn trên sàn (Shopee/TikTok) bị lệch so với tồn trong SCRM?**
A: Hãy kiểm tra lần lượt: (1) Sản phẩm đó đã **kết nối (map)** với sản phẩm SCRM chưa? Chưa map thì tồn không đẩy. (2) Đã **bật "đồng bộ số lượng"** ở gian hàng và ở sản phẩm chưa? (3) Tồn bạn đổi có đúng **kho đã cấu hình để đồng bộ** không? (4) Có đặt **tỷ lệ đồng bộ** dưới 100% hoặc **ngưỡng báo hết hàng** không — khi đó số đẩy lên sàn cố tình khác số thật. (5) Đồng bộ có độ trễ vài chục giây đến vài phút, đợi một lát rồi xem lại.

**Q: Tôi đổi tồn trong SCRM rồi mà trên sàn vẫn chưa thấy đổi?**
A: Đồng bộ chạy nền theo tiến trình (gom xử lý mỗi ~15 giây), không tức thì. Nếu cần đẩy ngay, vào màn sản phẩm bấm nút **Đồng bộ**. Nếu vẫn không đổi sau vài phút, kiểm tra kết nối gian hàng (token còn hạn không) và sản phẩm đã map đúng chưa.

**Q: Vì sao sàn báo "hết hàng" trong khi kho tôi vẫn còn vài cái?**
A: Có thể gian hàng đang đặt **ngưỡng báo hết hàng khi tồn dưới X**. Khi tồn (sau khi nhân tỷ lệ đồng bộ) còn ≤ ngưỡng đó, hệ thống chủ động đẩy 0 lên sàn để tránh khách đặt rồi không kịp giao. Chỉnh lại ngưỡng trong cấu hình gian hàng/kho kết nối nếu không muốn vậy.

**Q: Tôi sửa giá trong SCRM sao giá trên sàn không đổi?**
A: Hiện chức năng **đẩy giá lên sàn chưa hoạt động** — dù có ô bật "đồng bộ giá". Bạn vẫn cần chỉnh giá trực tiếp trên Shopee/TikTok. Đồng bộ tự động hiện chỉ áp dụng cho **số lượng tồn**, chưa áp dụng cho giá.

**Q: Kết nối tự động (map theo SKU) không ghép được sản phẩm nào, vì sao?**
A: Kết nối tự động ghép theo **mã SKU trùng nhau** giữa SCRM và sàn. Nếu mã SKU hai bên đặt khác nhau thì không tự ghép được — khi đó phải **kết nối thủ công**: chọn tay đúng sản phẩm/SKU SCRM cho từng sản phẩm trên sàn.

**Q: Tôi muốn đẩy chỉ một phần tồn lên sàn để giữ hàng đệm, có được không?**
A: Được. Đặt **tỷ lệ đồng bộ tồn** dưới 100% (ví dụ 80%) ở cấu hình gian hàng — hệ thống sẽ đẩy lên sàn theo tỷ lệ đó thay vì toàn bộ tồn.

**Q: Tôi có sản phẩm mới trong SCRM, muốn đăng lên sàn luôn được không?**
A: Được. Tạo **sản phẩm nháp** từ sản phẩm SCRM (chọn danh mục, thuộc tính, giá, ảnh, cân nặng/kích thước của sàn), rồi bấm **Gửi lên sàn**. Hệ thống sẽ đăng lên Shopee/TikTok và báo kết quả từng dòng (thành công hoặc lỗi để bạn sửa).

**Q: Nếu tồn trong kho bị âm thì đẩy lên sàn ra sao?**
A: Hệ thống không bao giờ đẩy số âm lên sàn. Tồn âm (hoặc dưới ngưỡng) sẽ thành **0 (hết hàng)** trên sàn.

**Q: Một sản phẩm bán ở nhiều kho, tồn nào được đẩy lên sàn?**
A: Chỉ tồn của **kho/chi nhánh đã được cấu hình để đồng bộ** với gian hàng đó. Nếu dùng nhiều kho mapping thì tồn của các kho khớp cấu hình mới được tính. Thay đổi ở kho không nằm trong cấu hình sẽ không tác động lên sàn.

**Q: Tôi gỡ kết nối một sản phẩm thì tồn trên sàn có bị xóa không?**
A: Gỡ kết nối chỉ ngắt việc đồng bộ về sau — sản phẩm trên sàn và số tồn hiện có trên sàn vẫn giữ nguyên, chỉ là từ đó SCRM không đẩy số mới lên nữa.
