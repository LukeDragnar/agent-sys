# Luồng — Gửi SMS & Zalo OA

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId).
- Đơn vị đã được cấu hình **tài khoản SMS** (AccountSms) với brandname/Zalo OA đã duyệt, apikey/secretkey hợp lệ và **còn số dư**.
- Đơn vị nên có **tiện ích SMS (TI_SMS) còn hạn** (xem case biên — code không chặn cứng).
- Để gửi nhanh: đã khai báo **mẫu tin (PatternSms)**; với Zalo OA cần mẫu đã được Zalo duyệt.
- Đã chọn được người nhận (1 khách / nhóm / danh sách lọc từ Khách hàng hoặc Chiến dịch).

## Luồng dùng (gửi tin)
1. Người dùng mở màn **Gửi tin** (hoặc gửi từ danh sách khách / chiến dịch).
2. Chọn kênh (SMS / Zalo), chọn brandname hoặc Zalo OA, soạn nội dung hoặc chọn mẫu.
3. Chọn khách hàng nhận, bấm **Gửi**.
4. Hệ thống đọc **cấu hình SMS của đơn vị** để quyết định nhánh xử lý:
   - Nếu đơn vị bật **cấu hình nâng cao** (`SuDungCauHinhNangCao = true`) → đẩy **1 message gộp SMS+Zalo (V2)** vào hàng đợi.
   - Ngược lại → đẩy **message riêng** cho SMS và cho Zalo (V1).
5. Hệ thống **trả "Thành công" ngay** (gửi bất đồng bộ — chưa gửi thật).
6. **Consumer nền** nhận message từ hàng đợi → gọi nhà mạng ESMS gửi thật → **trừ tiền tài khoản SMS** → ghi log tin (SmsSend).
7. ESMS xử lý xong → **gọi callback về hệ thống** (ReceiveDataQuery / ReceiveDataQueryZalo) với số tin thành công/thất bại và tổng tiền → hệ thống cập nhật trạng thái tin, đẩy kết quả sang pushsale.
8. Người dùng xem **danh sách tin đã gửi** để biết trạng thái thật.

## Luồng cấu hình (admin / CSKH)

**Mẫu tin (PatternSms):**
1. Vào màn Mẫu SMS/Zalo → Thêm mới / Sửa / Xóa mẫu; Đổi trạng thái mẫu.
2. **Đồng bộ mẫu từ ESMS** (GetTemplateSmsFromEsms / GetTemplateZaloFromEsms) → lấy mẫu đã duyệt về đơn vị.
3. **Thêm mẫu mặc định có sẵn** → tạo loạt mẫu chuẩn cho đơn vị.

**Tài khoản SMS (AccountSms — cấp hệ thống/CSKH):**
1. Thêm tài khoản (brandname/Zalo OA + apikey/secretkey/số dư).
2. Sửa / Xóa tài khoản; **Nạp tiền** (DepositMoneyIntoAccount); xem **Lịch sử nạp** (RechargeHistory).

## Case biên
- **Publish message lên hàng đợi thất bại** → endpoint gửi **vẫn trả "Thành công"** (try/catch nuốt lỗi). Tin KHÔNG vào queue, không tới khách nhưng UI báo thành công → cần đối chiếu bằng danh sách tin đã gửi. (Known-issue, xem dev.md.)
- **Đơn vị không lấy được tiện ích SMS (list rỗng/null)** → code **vẫn publish** ("không lấy được tiện ích thì cứ gửi vào xử lý sau") → tin vẫn được đưa vào xử lý.
- **Cấu hình nâng cao bật nhưng client gọi nhầm endpoint Zalo riêng (V1)** → endpoint Zalo V1 **không publish gì** (đã gộp ở message V2) → **mất tin Zalo** nếu gọi sai endpoint.
- **Callback ESMS sai định dạng RequestId** → bước parse Guid ném lỗi 500 (không bọc try) → kết quả tin không cập nhật được.
- **Tài khoản SMS hết số dư** → consumer gửi thất bại → tin báo lỗi ở danh sách tin đã gửi (không trừ được tiền).
- **Import giá tin bởi người không đủ quyền** → trả lỗi "Bạn không có quyền".
- **Gửi theo mẫu Zalo không đúng template đã duyệt** → nhà mạng từ chối → tin thất bại.

## Ví dụ cụ thể có số
Shop B bật cấu hình nâng cao, tài khoản SMS còn 50.000đ, giá 1 SMS brandname = 800đ.
- Marketing lọc 40 khách VIP, soạn tin khuyến mãi, bấm Gửi → hệ thống đẩy **1 message V2** vào hàng đợi, báo "Thành công" ngay.
- Consumer gửi 40 tin: 38 thành công, 2 số sai → trừ `38 × 800 = 30.400đ`, số dư còn `50.000 − 30.400 = 19.600đ`.
- ESMS callback về: 38 thành công / 2 thất bại, TotalPrice 30.400đ → danh sách tin đã gửi hiển thị đúng 38 OK, 2 lỗi.
- Nếu cùng lúc hàng đợi bị lỗi (RabbitMQ down) khi publish: UI vẫn báo "Thành công" nhưng danh sách tin đã gửi **trống** → dấu hiệu tin chưa vào queue.

## Hậu điều kiện
- Mỗi tin gửi thành công → **trừ số dư AccountSms** tương ứng, ghi log SmsSend.
- Callback cập nhật **số tin thành công/thất bại + tổng tiền**, đẩy kết quả sang **pushsale**.
- Thêm/Sửa/Xóa mẫu, tài khoản, nạp tiền → ghi DB; nạp tiền sinh bản ghi RechargeHistory.
- Việc gửi tin **không sửa dữ liệu khách hàng**, chỉ ghi nhận tương tác/log.
