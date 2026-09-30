---
alias: ["đối soát đơn online", "kế toán tác nghiệp", "đối soát đơn", "ghi chú kế toán", "tác nghiệp kế toán đơn hàng", "đối soát giao dịch online"]
module: ke-toan
trang_thai: cần bật (tiện ích logistic) — cần product xác nhận
goi: cần product xác nhận — gate qua tiện ích Kế toán logistic
quyen: "kế toán (DV-KT, DV-KT-TONG) + CSKH (HT-CSKH); một số thao tác admin riêng (đăng/hủy đăng) thêm DV-AD-*. Mỗi thao tác có phân quyền riêng — cần product xác nhận chi tiết"
phu_thuoc: ["Đơn hàng logistic / đơn online — đối tượng đối soát", "Đăng/hủy đăng đơn (giao vận ĐTGV) — cập trạng thái giao hàng", "Blacklist số điện thoại + care đơn", "Đồng bộ kế toán (tạo phiếu bán hàng tự động) — bước kế tiếp sau khi đơn đạt trạng thái"]
link_flow: ./flow.md
---

# Đối soát đơn online (Kế toán tác nghiệp)

## Là gì
Đây là **màn tác nghiệp dành cho kế toán** trên đơn hàng online (đơn logistic/giao vận). Kế toán dùng màn này để **tra soát và xử lý đơn**: tìm đơn theo điều kiện, xem chi tiết, đăng/hủy đăng đơn lên đơn vị giao vận, cập nhật trạng thái giao hàng, ghi chú kế toán, trao đổi nội bộ theo đơn, quản lý blacklist số điện thoại, đổi ngày muốn giao, và **xuất Excel** để đối chiếu. Đây là "khẩu độ kế toán" của màn Thủ kho tác nghiệp — cùng dữ liệu đơn nhưng góc nhìn/quyền của kế toán.

> Lưu ý: nút **"Đối soát nội bộ"** riêng (đánh dấu đơn đã đối soát) hiện đang **bị tắt trong code** (endpoint comment). Việc đối soát thực tế đang thực hiện qua các thao tác trên màn này (xem chi tiết, ghi chú kế toán, cập trạng thái, xuất Excel để đối chiếu).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm đơn theo điều kiện | ✅ | kế toán/CSKH |
| Tìm theo trạng thái giao hàng | ✅ | kế toán/CSKH |
| Xem chi tiết đơn | ✅ | kế toán/CSKH |
| Tra đơn + lịch sử theo số điện thoại | ✅ | kế toán/CSKH |
| Đăng đơn (lên đơn vị giao vận) | ✅ | phụ thuộc cấu hình "cho phép kế toán đăng đơn" (mặc định bật) |
| Hủy đăng đơn | ✅ | đơn đã đăng; đơn TMĐT bị chặn hủy đăng |
| Admin đăng / hủy đăng đơn (bỏ qua gate) | ✅ | admin/CSKH; hủy đăng admin chỉ DV-AD-TONG/HT-CSKH |
| Cập nhật trạng thái giao hàng | ✅ | kế toán/CSKH |
| Ghi chú kế toán cho đơn | ✅ | kế toán/CSKH |
| Tin nhắn nội bộ theo đơn / số điện thoại | ✅ | kế toán/CSKH |
| Thêm / xóa blacklist số điện thoại | ✅ | kế toán/CSKH |
| Cập nhật care đơn | ✅ | kế toán/CSKH |
| Đổi ngày khách muốn nhận hàng | ✅ | kế toán/CSKH |
| Đổi mã đơn | ✅ | kế toán/CSKH |
| Xuất Excel tác nghiệp (4 kiểu) | ✅ | chỉ cần đăng nhập (⚠️ 4 endpoint export bỏ quyền — xem dev.md) |
| **Đối soát nội bộ (đánh dấu đã đối soát)** | ❌ | endpoint hiện đang bị tắt trong code (comment) |

## Để làm gì / cho ai
- **Kế toán:** soát đơn online, ghi chú kế toán, cập trạng thái, xuất Excel để đối chiếu doanh thu/giao vận.
- **CSKH:** hỗ trợ tra cứu đơn theo số điện thoại, blacklist, care đơn.
- **Admin:** đăng/hủy đăng đơn ngoại lệ (bỏ qua gate thông thường).

## Cách dùng
1. Vào **Kế toán → Kế toán tác nghiệp**.
2. Tìm đơn theo điều kiện hoặc theo tab trạng thái giao hàng.
3. Bấm **Xem** để mở chi tiết đơn; có thể tra theo số điện thoại để xem lịch sử khách.
4. Thao tác cần thiết: **Đăng đơn / Hủy đăng đơn**, **Cập nhật trạng thái giao hàng**, **Ghi chú kế toán**, **Care đơn**, **Blacklist**, **Đổi ngày giao**.
5. Bấm **Xuất Excel** (chọn kiểu 1–4) để tải danh sách đối chiếu.

## Cần chú ý
- **"Đối soát nội bộ" riêng đang tắt:** nếu cần đánh dấu đơn "đã đối soát" như một trạng thái riêng, hiện chưa dùng được (endpoint comment) — cần product/dev mở lại.
- **Đăng đơn dùng chung với Thủ kho tác nghiệp:** mọi ràng buộc/gate của luồng đăng đơn (kiểm tồn, lock, gửi ĐTGV, trừ kho) giống Thủ kho tác nghiệp.
- **Đơn TMĐT (sàn) bị chặn hủy đăng** ở luồng chung.
- **Quyền đăng đơn của kế toán** phụ thuộc cấu hình cho phép kế toán đăng đơn (mặc định cho phép).
- **4 nút Xuất Excel** chỉ cần đăng nhập là dùng được (không phân quyền riêng); kiểu 1 và kiểu 2 tải ra file **cùng tên** `KeToanTacNghiep.xlsx` — dễ nhầm, chỉ kiểu 3/4 có hậu tố.

## Ảnh hưởng tới
- **Giao vận (ĐTGV):** đăng/hủy đăng đơn gửi/hủy lệnh giao hàng và trừ/hoàn kho.
- **Tạo hóa đơn tự động & đồng bộ kế toán:** khi đơn đạt trạng thái giao hàng cấu hình, hệ thống có thể tự sinh phiếu bán hàng kế toán và/hoặc phát hành hóa đơn tự động (xem feature "Tạo hóa đơn tự động").
- **Contact/Khách hàng:** blacklist, care đơn, ghi chú ảnh hưởng đến chăm sóc khách.
