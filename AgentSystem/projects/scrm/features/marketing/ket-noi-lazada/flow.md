# Luồng — Kết nối Lazada

> ⚠️ **CHƯA HOÀN THIỆN.** Không tìm thấy controller / tiện ích / màn riêng cho Lazada trong registry. Luồng dưới đây là **kỳ vọng** (suy ra từ cụm kết nối sàn chung), **cần product/dev xác nhận** trước khi coi là sự thật.

## Tiền điều kiện (kỳ vọng)
- **Ecommerce-connect gateway** (dịch vụ ngoài) phải hỗ trợ Lazada — đây là điều kiện then chốt chưa xác nhận.
- (Nếu áp dụng cơ chế chung) đơn vị có tiện ích sàn tương ứng, người dùng có quyền marketing/admin.
- Đã khai báo kho + có sản phẩm CRM để ghép.

## Luồng dùng (kỳ vọng — giống Shopee/TikTok)
> *Cần xác nhận màn nào thực sự phục vụ Lazada.* Nếu đi qua cụm chung:
1. Mở màn **Gian hàng TMĐT**, chọn **Lazada** → bấm Thêm kết nối → lấy đường ủy quyền từ gateway → chuyển sang Lazada.
2. Đăng nhập + cấp quyền trên Lazada → quay lại hệ thống → lưu thông tin ủy quyền (token).
3. Map gian hàng: chọn kho nhận đơn + kho đồng bộ tồn.
4. Ghép sản phẩm: tự động theo mã SKU hoặc ghép tay.
5. Đồng bộ tồn → đẩy tồn lên Lazada.
6. Đơn Lazada → nhận về hệ thống (qua luồng đơn hàng/giao vận).

## Case biên
- **Gateway chưa hỗ trợ Lazada** → bước ủy quyền không trả URL hợp lệ → kết nối không thực hiện được. Đây là rủi ro chính của trạng thái "chưa hoàn thiện".
- **Không có tiện ích/quyền riêng** → việc kiểm soát truy cập Lazada chưa rõ ràng (khác Shopee/TikTok mỗi sàn đều có tiện ích riêng). *Cần product xác nhận.*
- Các case khác (token hết hạn, đồng bộ tồn thiếu kho, SP lệch SKU…) — nếu dùng cụm chung thì giống Shopee/TikTok; xem flow của 2 sàn đó.

## Ví dụ cụ thể có số
> Chưa có ví dụ thực tế do tính năng chưa hoàn thiện. Khi Lazada được mở và xác nhận, bổ sung ví dụ tương tự Shopee (ủy quyền → map kho → ghép SKU → đồng bộ tồn N cái → nhận đơn).

## Hậu điều kiện (kỳ vọng)
- Nếu chạy qua cụm chung: lưu thông tin ủy quyền, lưu map gian hàng, lưu cặp ghép sản phẩm, đồng bộ tồn một chiều lên Lazada, đơn về qua luồng đơn hàng/giao vận.
- **Hiện tại: không xác nhận được hậu điều kiện** vì chưa có đường code riêng cho Lazada.
