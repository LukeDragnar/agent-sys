# FAQ — Đơn hàng hoàn

> Draft — cần bổ sung từ ticket khách thật.

### 1. Đơn hoàn từ đâu mà tự xuất hiện trong màn hình?

Khi khách bấm "Trả hàng / Hoàn tiền" trên sàn (Shopee, TikTok, Lazada), sàn báo về SCRM
và hệ thống tự tạo đơn hoàn tương ứng, gắn với đơn bán gốc. Bạn không phải nhập tay.

### 2. Đơn hoàn của tôi chưa thấy về, làm sao lấy?

Bấm nút **"Lấy đơn hoàn mới"**, chọn sàn + gian hàng + khoảng thời gian. Hệ thống sẽ kéo
các đơn hoàn còn thiếu từ sàn về. Nếu chỉ muốn cập nhật trạng thái cho đơn đã có, tick đơn
rồi bấm **"Đồng bộ trạng thái"**.

### 3. "Trả hàng & hoàn tiền" và "Chỉ hoàn tiền" khác nhau thế nào?

- **Trả hàng & hoàn tiền**: khách gửi hàng về, shop nhận lại hàng (nhập kho hoàn) rồi mới hoàn tiền.
- **Chỉ hoàn tiền**: khách giữ hàng, shop chỉ hoàn tiền, **không có bước nhập kho**.

Cột "Phương án" trên màn hình cho biết đơn thuộc loại nào.

### 4. "Loại đơn hoàn" (Trước / Sau hoàn thành đơn) nghĩa là gì?

Đây là trục **kế toán**, cho biết khách yêu cầu hoàn **trước hay sau** khi đơn gốc hoàn thành
(đã ghi nhận doanh thu hay chưa):
- **Sau hoàn thành đơn**: đơn gốc đã ghi nhận doanh thu → cần lập HBBTL để đảo doanh thu.
- **Trước hoàn thành đơn**: chưa ghi doanh thu → không cần HBBTL.

### 5. Khi nào thì tạo được HBBTL (Hóa đơn bán hàng trả lại)?

- Đơn **chỉ hoàn tiền**: tạo được luôn (không cần nhập kho).
- Đơn **trả hàng**: chỉ nên tạo **khi hàng đã về kho**. Hệ thống tự điền sẵn form HBBTL từ
  đơn bán gốc (khách, sản phẩm, giá vốn, số tiền) để bạn không phải nhập lại.

### 6. Sản phẩm trong đơn hoàn không khớp với đơn bán gốc thì sao?

Đa số trường hợp hệ thống tự khớp. Nếu không khớp được (thường là đơn cũ trước khi bật tính
năng, hoặc SKU đã đổi mã), mở chức năng **"Map sản phẩm"** để khớp tay từng dòng hoàn ↔ dòng
bán, trước khi lập HBBTL.

### 7. Cột "Hạn khiếu nại — còn N ngày" để làm gì?

Với đơn sàn đang chờ shop xử lý, đây là **đếm ngược tới hạn cuối** shop được khiếu nại / phản
hồi với sàn. Quá hạn shop mất quyền. Nên ưu tiên xử lý các đơn sắp hết hạn (có bộ lọc ưu tiên
"hết hạn sau 1 ngày / 2 ngày").

### 8. Tại sao có đơn nằm ở tab "Giao hàng không thành công"?

Đó là đơn khách **từ chối nhận hàng ngay tại cửa** khi shipper giao (return-on-the-spot). Sàn
ghi nhận đây là một dạng hoàn, nên SCRM tách riêng tab này. Luồng này thường tự hoàn tiền, không
qua bước khách gửi hàng về.

### 9. Số tiền hoàn / lý do hoàn có sửa được không?

Phần lớn thông tin (tiền hoàn, lý do, trạng thái) **lấy từ sàn, không sửa**. SCRM hiển thị lại
để xử lý nhập kho + kế toán. Riêng một vài giá trị phục vụ kế toán (ví dụ chiết khấu sàn từng
dòng) sửa được ở màn chi tiết.
