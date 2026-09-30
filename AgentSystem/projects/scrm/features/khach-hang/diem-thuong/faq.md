# FAQ — Điểm thưởng & đổi thưởng

> Draft — cần bổ sung từ ticket khách thật.

### 1. Tích điểm hoạt động thế nào?
Khi khách mua hàng và thanh toán, hệ thống lấy số tiền khách thực trả (theo các loại hàng được bật tích điểm) chia cho tỷ lệ "Số tiền đổi được 1 điểm" để ra số điểm. Số điểm này được cộng vào tài khoản điểm của khách. Ví dụ cấu hình 10.000₫ = 1 điểm, khách trả 350.000₫ → tích 35 điểm.

### 2. 1 điểm bằng bao nhiêu tiền?
Do đơn vị tự cấu hình ở ô **"1 điểm đổi được số tiền"**. Ví dụ đặt 1 điểm = 10.000₫ thì khi đổi điểm thanh toán, mỗi điểm trừ được 10.000₫ giá trị đơn. Lưu ý tỷ lệ tích điểm (tiền→điểm) và tỷ lệ đổi điểm (điểm→tiền) là **hai cấu hình riêng**, có thể khác nhau.

### 3. Có thể tắt tích điểm hoặc tắt đổi điểm riêng không?
Có. Hai công tắc độc lập: "Cho phép tích điểm" và "Cho phép đổi điểm". Có thể bật cái này tắt cái kia. Khi tắt, ô tỷ lệ tương ứng được ẩn và không bắt buộc nhập.

### 4. Loại hàng nào được tích điểm?
Cấu hình theo từng loại: Dịch vụ, Sản phẩm, Combo, Thẻ (gồm cả nạp thẻ), và Thu khác. Chỉ những loại được bật mới tính tiền vào để quy ra điểm. Phần thanh toán bằng điểm và bằng "thẻ tiền mặt" không được tính lại để tích điểm.

### 5. Khách hạng VIP có được tích điểm tỷ lệ tốt hơn không?
Có thể, qua phần **Quy đổi chi tiết**: thêm dòng theo hạng hoặc nhóm khách hàng với tỷ lệ riêng. Khi khách thuộc hạng/nhóm đó thanh toán, hệ thống ưu tiên dùng tỷ lệ riêng thay cho tỷ lệ chung.

### 6. Khách đổi điểm để thanh toán bằng cách nào?
Tại màn thanh toán, chọn phương thức **"Điểm"** rồi nhập số tiền muốn trả bằng điểm. Hệ thống quy ra số điểm cần trừ theo tỷ lệ đổi điểm và trừ vào tài khoản điểm của khách. Số tiền trả bằng điểm không được vượt giá trị đơn.

### 7. Khách không đủ điểm thì sao?
Nếu số điểm cần đổi lớn hơn số điểm hiện có, hệ thống báo lỗi "Số điểm hiện tại của khách hàng không đủ!" và không cho hoàn tất thanh toán bằng điểm.

### 8. Sửa tỷ lệ xong có ảnh hưởng đơn cũ không?
Không. Cấu hình mới chỉ áp dụng cho các đơn thanh toán **sau** khi lưu. Đơn đã thanh toán trước đó giữ nguyên điểm đã tích/đổi.

### 9. (Cần product xác nhận) Điểm có hạn sử dụng / hết hạn không?
Chưa thấy quy tắc hết hạn điểm trong cấu hình hiện tại — cần product xác nhận chính sách.
