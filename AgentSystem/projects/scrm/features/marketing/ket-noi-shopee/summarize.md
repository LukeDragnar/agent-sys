---
alias: ["kết nối Shopee", "gian hàng Shopee", "đồng bộ Shopee", "kết nối sàn Shopee", "ủy quyền Shopee", "đồng bộ sản phẩm Shopee", "nhận đơn Shopee"]
module: marketing
trang_thai: "cần bật tiện ích Shopee cho đơn vị. Lưu ý: luồng kết nối thật chạy qua màn Gian hàng TMĐT chung (một đường nút cũ riêng cho Shopee chưa hoàn thiện — chi tiết kỹ thuật xem dev.md)."
goi: cần product xác nhận
quyen: "cần product xác nhận — khởi tạo kết nối Shopee cho admin đơn vị (tổng/chi nhánh/multi chi nhánh) và nhóm Marketing (MKT, MKT-LEAD, QL-MKT). Các bước map gian hàng / kho / sản phẩm chỉ yêu cầu đăng nhập."
phu_thuoc: ["Tiện ích Shopee của đơn vị", "Dịch vụ kết nối sàn (giữ token + đồng bộ với Shopee)", "Kho (chọn kho nhận đơn / kho đồng bộ tồn)", "Sản phẩm trong hệ thống (ghép mã SKU sàn ↔ sản phẩm hệ thống)", "Đơn hàng (nhận đơn Shopee về)", "Nguồn / Landing"]
link_flow: ./flow.md
---

# Kết nối Shopee

## Là gì
Tính năng giúp shop **kết nối gian hàng Shopee** với hệ thống để: lấy đơn Shopee về xử lý chung, **ghép sản phẩm** Shopee với sản phẩm trong hệ thống, và **đồng bộ tồn kho** từ hệ thống lên Shopee.

Quy trình gồm các bước: **ủy quyền (đăng nhập Shopee cấp quyền)** → **map gian hàng** (gắn gian hàng Shopee với đơn vị/kho) → **ghép sản phẩm** → **đồng bộ tồn**. Sau đó đơn từ Shopee tự chảy về hệ thống.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Khởi tạo kết nối / ủy quyền Shopee | ✅ | cần tiện ích Shopee; quyền Admin / MKT / MKT-LEAD / QL-MKT |
| Map gian hàng Shopee ↔ đơn vị / kho | ✅ | sau khi ủy quyền; chọn kho nhận đơn + kho đồng bộ tồn |
| Xem danh sách sản phẩm bên Shopee | ✅ | sau khi map gian hàng |
| Ghép sản phẩm tự động (theo SKU) | ✅ | có gian hàng đã kết nối |
| Ghép từng sản phẩm thủ công | ✅ | chọn đúng sản phẩm CRM tương ứng |
| Đồng bộ tồn kho lên Shopee | ✅ | sản phẩm đã ghép + đã chọn kho |
| Gỡ / xóa kết nối sản phẩm | ✅ | tùy quyền |
| Xóa kết nối gian hàng | ✅ | gỡ map gian hàng (ngắt nhận đơn + đồng bộ) |
| Nhận đơn Shopee về hệ thống | ✅ (tự động) | sau khi map gian hàng có gắn callback nhận đơn |
| **Đẩy / sửa thông tin sản phẩm lên Shopee** | ❌ | màn này chỉ **đồng bộ tồn** một chiều; không tạo/sửa nội dung SP trên Shopee |

## Để làm gì / cho ai
- **Chủ shop / Marketing:** gom đơn Shopee về một chỗ để xử lý cùng các kênh khác, không phải vào Shopee Seller riêng.
- **Thủ kho / vận hành:** đồng bộ tồn để tồn trên Shopee khớp với tồn thực, tránh bán quá số.
- **Kế toán / báo cáo:** đơn Shopee về hệ thống để thống kê doanh số đa kênh.

## Cách dùng
1. Vào **Marketing → Kết nối sàn → Shopee** (hoặc màn **Gian hàng TMĐT**, chọn Shopee).
2. Bấm **Kết nối Shopee** → hệ thống chuyển sang Shopee để bạn **đăng nhập và cấp quyền**.
3. Cấp quyền xong, Shopee trả về hệ thống; bạn **cấu hình gian hàng**: chọn kho nhận đơn, kho đồng bộ tồn, đơn vị/chi nhánh.
4. Vào phần **Sản phẩm gian hàng**: lấy danh sách sản phẩm Shopee → bấm **Ghép tự động** (theo SKU) hoặc **ghép tay** từng sản phẩm.
5. Bấm **Đồng bộ tồn** để đẩy số lượng tồn từ hệ thống lên Shopee.
6. Từ đây, đơn mới trên Shopee tự về hệ thống để xử lý.

## Cần chú ý
- **Phải bật tiện ích Shopee.** Đơn vị chưa mua tiện ích Shopee thì không vào được.
- **Token ủy quyền có thể hết hạn / bị thu hồi.** Khi gian hàng mất kết nối, phải **ủy quyền lại** (kết nối lại) thì mới nhận đơn / đồng bộ tiếp.
- **Đồng bộ tồn là một chiều (hệ thống → Shopee).** Số lượng trên Shopee được đặt theo tồn trong hệ thống; nếu chưa ghép sản phẩm hoặc chưa chọn kho thì không đồng bộ được.
- **Phải map gian hàng đúng kho.** Sai kho nhận đơn / kho đồng bộ tồn → đơn về sai kho hoặc tồn đẩy lên sai. Map gian hàng còn gắn đường nhận đơn (callback); cấu hình sai thì đơn Shopee không về.
- **Xóa kết nối gian hàng = ngắt nhận đơn + ngừng đồng bộ** của gian hàng đó. Cân nhắc trước khi xóa.
- **Đường kết nối thực tế đi qua màn Gian hàng TMĐT chung** (dùng chung cho Shopee/TikTok/Lazada). Một đường nút "Kết nối Shopee" riêng cũ chưa hoàn thiện — *cần product xác nhận* màn nào đang dùng cho khách (chi tiết kỹ thuật ghi ở dev.md).

## Ảnh hưởng tới
- **Đơn hàng / OrderLogistic:** đơn Shopee về hệ thống qua đường callback gắn khi map gian hàng.
- **Kho / tồn kho:** đồng bộ tồn đọc số lượng kho trong hệ thống đẩy lên Shopee.
- **Sản phẩm:** việc ghép SKU quyết định đơn Shopee về đúng sản phẩm CRM nào.
- **Báo cáo doanh số / theo sàn:** đơn Shopee được tính vào doanh số đa kênh.
