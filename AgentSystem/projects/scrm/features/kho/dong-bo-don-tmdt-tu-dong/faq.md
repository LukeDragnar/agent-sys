> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Tự động nhận & đồng bộ đơn từ sàn TMĐT

**Q: Khách đặt đơn trên Shopee rồi mà sao chưa thấy trong SCRM?**
A: Đơn về theo thông báo của sàn nên có độ trễ vài giây đến vài phút (cao điểm có thể lâu hơn). Nếu chờ vẫn không thấy: (1) kiểm tra gian hàng còn kết nối không (token có thể hết hạn — vào Marketing → Kết nối Shopee để kết nối lại); (2) bấm nút **"Lấy đơn từ sàn"** trên màn Đơn hàng TMĐT, chọn đúng gian hàng + khoảng thời gian để kéo bù; (3) xem tab **"Đơn lỗi đồng bộ"** — có thể đơn đã về nhưng tạo lỗi (thiếu mapping sản phẩm/kho).

**Q: Đơn về SCRM thì nằm ở đâu?**
A: Trong màn **Đơn hàng TMĐT** (Đơn hàng logistic). Đơn trả hàng/hoàn tiền thì nằm ở màn **Đơn hàng hoàn**.

**Q: Tôi bấm "Lấy đơn từ sàn" có bị tạo trùng đơn không?**
A: Không. Nếu đơn đó đã có trong SCRM, hệ thống nhận ra theo mã đơn sàn và giữ nguyên đơn cũ, không tạo thêm. "Lấy đơn từ sàn" chỉ kéo về những đơn còn thiếu.

**Q: Trạng thái đơn trên sàn đổi rồi mà trong SCRM vẫn cũ?**
A: Trạng thái cập nhật khi sàn gửi thông báo đổi trạng thái. Nếu chậm, với đơn hoàn bạn có thể bấm **"Đồng bộ trạng thái"**; với đơn bán dùng **"Lấy đơn từ sàn"** để làm mới. Nếu vẫn lệch, có thể trạng thái đó chưa được hệ thống ánh xạ — báo bộ phận hỗ trợ.

**Q: Khách trả hàng trên sàn thì xử lý ở đâu?**
A: Đơn hoàn tự về màn **Đơn hàng hoàn**. Nhưng lưu ý: lúc đơn hoàn về, tồn kho và công nợ **chưa** thay đổi — đó mới chỉ là thông tin. Khi hàng thực về kho, bạn vào tạo phiếu **Hàng bán bị trả lại (HBBTL)** thì tồn mới cộng lại và công nợ mới giảm.

**Q: Sao đơn hoàn về rồi mà tôi chưa tạo được HBBTL?**
A: Hệ thống chỉ cho tạo HBBTL khi xác nhận **hàng đã thực về kho** theo trạng thái logistics hoàn của sàn. Đơn đang trên đường về hoặc chưa gửi thì chưa tạo được. Bấm "Đồng bộ trạng thái" để cập nhật, đợi đơn đạt mốc "đã về" rồi thử lại.

**Q: Tôi dùng Lazada, sao không in được vận đơn / đăng đơn lên sàn?**
A: Hiện việc in vận đơn và đăng đơn qua hệ thống chỉ hỗ trợ Shopee và TikTok. Đơn Lazada vẫn nhận về được, nhưng in/đăng phải thao tác trực tiếp trên Lazada.

**Q: Đơn bị "lỗi đồng bộ" là sao, xử lý thế nào?**
A: Là đơn sàn đã gửi về nhưng SCRM tạo không thành công (hay gặp: sản phẩm/kho chưa mapping, đơn vị chưa đúng). Vào tab **"Đơn lỗi đồng bộ"** trên màn Đơn hàng TMĐT: sửa nguyên nhân (mapping) rồi bấm **"Tạo lại"**, hoặc **"Xóa"** nếu không cần.

**Q: Phí sàn / hóa đơn của đơn có tự về không?**
A: Có. Ngoài tạo đơn và cập trạng thái, hệ thống còn nhận **cập nhật phí giao dịch** và **hóa đơn** của đơn từ sàn, gắn vào đúng đơn để đối soát và kế toán.

**Q: Một sản phẩm trên sàn chưa khớp với sản phẩm trong SCRM thì đơn về thế nào?**
A: Đơn vẫn có thể về nhưng dễ rơi vào "Đơn lỗi đồng bộ" hoặc trừ sai kho. Nên mapping sản phẩm sàn ↔ SCRM và mapping kho cho gian hàng trước khi chạy thật.
