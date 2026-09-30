# Luồng — Quản lý log & nhật ký (Log Delivery / Log Ecommerce)

## Tiền điều kiện
- Người dùng đã đăng nhập với role admin hệ thống (HT-AD). Cả 2 màn dùng `[SBAuthorize]` key nội bộ → user thường/khách không vào được.
- Hệ thống đã chạy tích hợp giao vận / sàn TMĐT, sinh ra log trong kho lưu (MongoDB). Nếu chưa có data trao đổi thì không có log để tra.
- Để **gửi lại** có ý nghĩa: bản ghi cần ở trạng thái **lỗi** và luồng đích (Delivery / Ecommerce) đang hoạt động để nhận lại.

## Luồng dùng (tra cứu — admin hệ thống)
1. Admin mở **Hệ thống → Log Delivery** (hoặc **Log Ecommerce**).
2. Nhập điều kiện lọc (`LogDeliveryFilter` / `LogEcommerceFilter`: thời gian, đơn vị, trạng thái…) rồi bấm **Tìm**.
3. Hệ thống truy vấn kho log (Mongo) trả về danh sách bản ghi khớp điều kiện.
4. Admin xem nội dung từng bản ghi (payload trao đổi, trạng thái, thời điểm) để xác định lỗi ở đâu.

## Luồng gửi lại (resend data lỗi — admin hệ thống)
1. Trong danh sách, admin chọn bản ghi **bị lỗi**, bấm **Gửi lại**.
2. Hệ thống đóng gói bản ghi (`ReqResendDataError`) và đẩy lại vào luồng xử lý đích:
   - Log Delivery → `ResendDataErrorToDelivery` → luồng **Delivery** xử lý lại.
   - Log Ecommerce → `ResendDataErrorToEcom` → luồng **Ecommerce** xử lý lại.
3. Luồng đích nhận và xử lý lại như lần nhận data đầu (có thể cập nhật đơn/vận đơn/trạng thái).

## Các trường hợp / case biên
- **Bản ghi không ở trạng thái lỗi mà vẫn gửi lại** → luồng đích có thể xử lý lại data đã xử lý → nguy cơ xử lý/cập nhật trùng. Chỉ gửi lại bản ghi thực sự lỗi.
- **Gửi lại nhiều lần cùng một bản ghi** → mỗi lần đẩy 1 message vào luồng đích → xử lý trùng nhiều lần.
- **Nguyên nhân gốc chưa khắc phục** (data sai định dạng, đơn vị thiếu cấu hình) → gửi lại vẫn lỗi, sinh thêm log lỗi mới.
- **Log đã bị dọn (job clear log Mongo)** → tra cứu không ra bản ghi cũ; không thể gửi lại bản ghi đã bị dọn.
- **Luồng đích (Delivery/Ecommerce) đang down** → message vào hàng đợi nhưng chưa được xử lý cho tới khi luồng đích hồi phục.

## Ví dụ cụ thể có số
Sàn Shopee đẩy 50 đơn vào hệ thống lúc 09:00. 3 đơn lỗi do thiếu mapping sản phẩm.
- Admin vào **Log Ecommerce**, lọc thời gian 09:00–09:30, trạng thái = lỗi → thấy **3 bản ghi** lỗi.
- Đội nghiệp vụ bổ sung mapping sản phẩm cho 3 đơn đó.
- Admin chọn từng bản ghi → bấm **Gửi lại** → 3 message đẩy vào luồng Ecommerce → 3 đơn được nhận lại thành công, trạng thái lỗi mất.
- Nếu admin lỡ bấm **Gửi lại 2 lần** cho 1 đơn trước khi sửa mapping → đơn vẫn lỗi và sinh thêm 1 bản ghi log lỗi mới (tổng 4 bản ghi lỗi).

## Hậu điều kiện
- **Tra cứu** không thay đổi dữ liệu (read-only trên kho log).
- **Gửi lại** đẩy message vào luồng đích → có thể **tạo/cập nhật dữ liệu nghiệp vụ** (đơn, vận đơn, trạng thái giao hàng) khi luồng đích xử lý thành công.
- Mỗi lần luồng đích xử lý lại có thể sinh thêm bản ghi log mới (thành công hoặc lỗi).
