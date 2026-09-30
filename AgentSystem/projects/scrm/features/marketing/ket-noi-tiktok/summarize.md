---
alias: ["kết nối TikTok", "kết nối TikTok Shop", "gian hàng TikTok", "đồng bộ TikTok", "ủy quyền TikTok", "đồng bộ sản phẩm TikTok", "nhận đơn TikTok"]
module: marketing
trang_thai: "cần bật tiện ích TikTok cho đơn vị. Luồng kết nối thật chạy qua màn Gian hàng TMĐT chung (chi tiết kỹ thuật xem dev.md)."
goi: cần product xác nhận
quyen: "cần product xác nhận — khởi tạo kết nối TikTok cho admin đơn vị (tổng/chi nhánh/multi chi nhánh) và nhóm Marketing (MKT, MKT-LEAD, QL-MKT). Các bước map gian hàng / kho / sản phẩm chỉ yêu cầu đăng nhập."
phu_thuoc: ["Tiện ích TikTok của đơn vị", "Dịch vụ kết nối sàn (giữ token + đồng bộ với TikTok)", "Kho (kho nhận đơn / kho đồng bộ tồn)", "Sản phẩm trong hệ thống (ghép mã SKU sàn ↔ sản phẩm hệ thống)", "Đơn hàng (nhận đơn TikTok)", "Nguồn / Landing"]
link_flow: ./flow.md
---

# Kết nối TikTok

## Là gì
Tính năng giúp shop **kết nối gian hàng TikTok Shop** với hệ thống để: lấy đơn TikTok về xử lý chung, **ghép sản phẩm** TikTok với sản phẩm trong hệ thống, và **đồng bộ tồn kho** từ hệ thống lên TikTok.

Quy trình giống các sàn khác: **ủy quyền (đăng nhập TikTok cấp quyền)** → **map gian hàng** → **ghép sản phẩm** → **đồng bộ tồn**. Sau đó đơn TikTok tự về hệ thống. TikTok dùng **chung màn gian hàng TMĐT** với Shopee/Lazada, chỉ khác mã sàn.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Khởi tạo kết nối / ủy quyền TikTok | ✅ | cần tiện ích TikTok; quyền Admin / MKT / MKT-LEAD / QL-MKT |
| Map gian hàng TikTok ↔ đơn vị / kho | ✅ | sau khi ủy quyền; chọn kho nhận đơn + kho đồng bộ tồn |
| Xem danh sách sản phẩm bên TikTok | ✅ | sau khi map gian hàng |
| Ghép sản phẩm tự động (theo SKU) | ✅ | có gian hàng đã kết nối |
| Ghép từng sản phẩm thủ công | ✅ | chọn đúng sản phẩm CRM tương ứng |
| Đồng bộ tồn kho lên TikTok | ✅ | sản phẩm đã ghép + đã chọn kho |
| Gỡ / xóa kết nối sản phẩm | ✅ | tùy quyền |
| Xóa kết nối gian hàng | ✅ | gỡ map gian hàng (ngắt nhận đơn + đồng bộ) |
| Nhận đơn TikTok về hệ thống | ✅ (tự động) | sau khi map gian hàng có gắn callback nhận đơn |
| **Đẩy / sửa thông tin sản phẩm lên TikTok** | ❌ | màn này chỉ **đồng bộ tồn** một chiều; không tạo/sửa nội dung SP trên TikTok |

## Để làm gì / cho ai
- **Chủ shop / Marketing:** gom đơn TikTok về một chỗ xử lý cùng các kênh khác.
- **Thủ kho / vận hành:** đồng bộ tồn để tồn TikTok khớp tồn thực, tránh bán quá.
- **Kế toán / báo cáo:** đơn TikTok về hệ thống để thống kê doanh số đa kênh.

## Cách dùng
1. Vào **Marketing → Kết nối sàn → Gian hàng TMĐT**, chọn **TikTok**.
2. Bấm **Thêm kết nối** (hoặc **Xác thực lại** nếu đã từng kết nối) → hệ thống chuyển sang TikTok để **đăng nhập + cấp quyền**.
3. Cấp quyền xong, TikTok trả về hệ thống; **cấu hình gian hàng**: chọn kho nhận đơn, kho đồng bộ tồn, đơn vị/chi nhánh.
4. Vào **Sản phẩm gian hàng**: lấy danh sách SP TikTok → **Ghép tự động** (theo SKU) hoặc **ghép tay**.
5. Bấm **Đồng bộ tồn** để đẩy tồn từ hệ thống lên TikTok.
6. Đơn mới trên TikTok tự về hệ thống.

## Cần chú ý
- **Phải bật tiện ích TikTok.** Đơn vị chưa mua tiện ích TikTok thì không vào được.
- **Token ủy quyền có thể hết hạn / bị thu hồi.** Khi mất kết nối, dùng **Xác thực lại** để ủy quyền lại.
- **Đồng bộ tồn một chiều (hệ thống → TikTok).** Chưa ghép sản phẩm / chưa chọn kho thì không đồng bộ được.
- **Map gian hàng đúng kho.** Sai kho → đơn về sai / tồn đẩy sai. Cấu hình URL dịch vụ sai → đơn TikTok không về.
- **Xóa kết nối gian hàng = ngắt nhận đơn + ngừng đồng bộ.**
- **Kết nối TikTok dùng chung màn Gian hàng TMĐT** với Shopee/Lazada. Nút "Thêm kết nối / Xác thực lại" trên màn gian hàng gọi thẳng dịch vụ kết nối sàn — đây là đường đang dùng cho khách (chi tiết kỹ thuật ghi ở dev.md).

## Ảnh hưởng tới
- **Đơn hàng / OrderLogistic:** đơn TikTok về hệ thống qua callback gắn khi map gian hàng.
- **Kho / tồn kho:** đồng bộ tồn đọc số lượng kho hệ thống đẩy lên TikTok.
- **Sản phẩm:** ghép SKU quyết định đơn TikTok về đúng sản phẩm CRM nào.
- **Báo cáo doanh số / theo sàn:** đơn TikTok tính vào doanh số đa kênh.
