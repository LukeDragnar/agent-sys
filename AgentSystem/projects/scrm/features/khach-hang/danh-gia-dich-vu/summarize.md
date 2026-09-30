---
alias: ["đánh giá dịch vụ", "phản hồi khách", "feedback khách hàng", "nhận xét dịch vụ", "phản hồi dịch vụ", "khách đánh giá"]
trang_thai: đã triển khai
goi: "cần product xác nhận — chưa rõ thuộc gói nào"
quyen: "Xem/xử lý danh sách phản hồi: DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLDV. Quản lý danh mục đánh giá (theo đơn vị): admin đơn vị (DV-AD-TONG). Khách đánh giá qua link: không cần đăng nhập (ẩn danh)."
phu_thuoc: [Order (DonHang — mã hóa đơn, dịch vụ, nhân viên phục vụ), OrderLogistic (đơn online), Booking/ShortLink (sinh link đánh giá rút gọn), Core (chi nhánh, đơn vị, ngành nghề), KhachHang (số điện thoại khách), Thông báo (gửi noti khi khách đánh giá xong)]
link_flow: ./flow.md
module: khach-hang
---

# Nhận xét & đánh giá dịch vụ

## Là gì
Tính năng cho phép **khách hàng tự đánh giá chất lượng dịch vụ** sau khi mua/sử dụng tại shop/spa, và cho **shop xem lại, thống kê, xử lý** những phản hồi đó.

Khách nhận một **link đánh giá rút gọn** (qua SMS/Zalo/in trên hóa đơn), bấm vào để chấm **mức độ hài lòng** cho cả đơn và cho từng **dịch vụ – nhân viên** đã phục vụ, kèm lời nhận xét. Phía shop có 3 màn hình:
- **Phản hồi dịch vụ** — màn khách chấm điểm (mở qua link, không cần đăng nhập).
- **Danh sách phản hồi** — chỗ shop xem tất cả đánh giá đã nhận, lọc, thống kê, đánh dấu "đã xử lý", xuất Excel.
- **Quản lý danh mục đánh giá** — shop cấu hình sẵn các "lý do/nhận xét nhanh" theo từng mức hài lòng (vd "Nhân viên thân thiện", "Chờ lâu") để khách chọn cho nhanh.

Mức độ hài lòng hiện có **3 bậc**: **Rất hài lòng**, **Hài lòng**, **Không hài lòng** (chưa hài lòng).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tạo link đánh giá cho đơn | ✅ | DV-AD-TONG/CN/MCN, DV-QLDV, DV-NV; đơn offline |
| Khách chấm điểm / gửi đánh giá (qua link) | ✅ | ẩn danh, không cần đăng nhập; phải chọn điểm > 0 |
| Khách gửi lại đè điểm cũ | ⚠️ | hiện không bị chặn (check "đã đánh giá" đang bị tắt trong code) |
| Xem danh sách phản hồi dịch vụ | ✅ | DV-AD-TONG/CN/MCN, DV-QLDV; non-admin chỉ thấy chi nhánh mình |
| Đánh dấu đã xử lý phản hồi | ✅ | quyền bị nới lỏng — ai đăng nhập cũng cập nhật được (key gốc bị comment) |
| Xuất Excel phản hồi | ✅ | DV-AD-TONG/CN/MCN, DV-QLDV; tối đa 5.000 dòng |
| Cấu hình danh mục tiêu chí (theo đơn vị) | ✅ | chỉ DV-AD-TONG (thêm/sửa/xóa/khởi tạo) |
| Cấu hình danh mục tiêu chí (cấp admin hệ thống) | ✅ | chỉ HT-AD (nhóm endpoint _SA) |
| **Xóa phản hồi của khách** | ❌ | không có endpoint xóa phản hồi đã gửi |

## Để làm gì / cho ai
- **Cho chủ shop / quản lý:** nắm được khách hài lòng hay không, nhân viên nào / dịch vụ nào hay bị chê, theo dõi tỉ lệ hài lòng theo chi nhánh và thời gian để cải thiện.
- **Cho CSKH / quản lý dịch vụ:** có danh sách phản hồi để chủ động liên hệ lại khách chưa hài lòng, đánh dấu đã xử lý để không bỏ sót.
- **Cho khách hàng:** có kênh nói lên trải nghiệm nhanh gọn, không cần đăng nhập, chấm điểm ngay trên điện thoại.
- **Actor:** khách hàng (đánh giá ẩn danh qua link); admin/quản lý dịch vụ của shop (xem/xử lý danh sách + cấu hình danh mục).

## Cách dùng
**Khách đánh giá:**
1. Khách nhận **link đánh giá** (rút gọn) sau khi shop tạo, bấm mở.
2. Màn "Phản hồi dịch vụ" hiện thông tin đơn + danh sách dịch vụ/nhân viên đã phục vụ.
3. Khách chọn mức hài lòng chung, chấm thêm cho từng nhân viên (tuỳ chọn), ghi nhận xét → **Gửi**.

**Shop xem & xử lý (vào CRM → Khách hàng → Phản hồi dịch vụ):**
1. Xem danh sách phản hồi đã nhận, kèm bảng tổng hợp **Tổng link đánh giá / Tổng đánh giá / Rất hài lòng / Hài lòng / Chưa hài lòng**.
2. Lọc theo **chi nhánh, mức hài lòng, khoảng ngày, trạng thái xử lý**, hoặc gõ **từ khoá** (mã hóa đơn / tên dịch vụ / tên nhân viên).
3. Bấm **đánh dấu đã xử lý** với phản hồi đã liên hệ khách xong.
4. Bấm **Xuất Excel** để tải danh sách (tối đa 5.000 dòng).

**Cấu hình danh mục đánh giá (admin đơn vị):**
1. Vào màn quản lý danh mục, chọn **kiểu loại** (Rất hài lòng / Hài lòng / Không hài lòng) và nhập các **nhận xét nhanh** tương ứng.
2. Có thể bấm **Khởi tạo** để copy bộ danh mục mẫu (ngành nghề) về đơn vị mình rồi chỉnh lại.

## Cần chú ý
- **Chỉ 3 mức hài lòng** (Rất hài lòng / Hài lòng / Không hài lòng). Một số chỗ giao diện cũ hiển thị 5 sao/“Bình thường” nhưng nghiệp vụ lưu chỉ nhận 3 mức — đừng hứa với khách thang 5 sao.
- **Link đánh giá là ẩn danh** — màn khách chấm điểm không yêu cầu đăng nhập. Ai có link đều mở được, nên link gắn với 1 đơn cụ thể.
- **Phân quyền theo chi nhánh:** nhân viên không phải admin/CSKH/superuser chỉ thấy phản hồi của **chi nhánh mình**, không thấy toàn đơn vị.
- **Chỉ tính đơn đã được chấm điểm:** danh sách & thống kê chỉ đếm các đánh giá đã có mức hài lòng; link đã gửi mà khách chưa chấm thì nằm ở "Tổng link đánh giá" chứ không vào "Tổng đánh giá".
- **Danh mục đánh giá theo từng đơn vị (shop):** mỗi shop tự cấu hình; sửa danh mục **không** đổi kiểu loại của bản ghi cũ (chỉ sửa được tên), tên không được trùng trong cùng đơn vị.
- **Xuất Excel giới hạn 5.000 dòng** — dữ liệu nhiều hơn cần lọc bớt theo ngày/chi nhánh trước khi xuất.

## Ảnh hưởng tới
- **Đơn hàng (Order / OrderLogistic)** — đánh giá gắn theo đơn (offline/online), lấy mã hóa đơn, dịch vụ và nhân viên phục vụ từ đơn.
- **Link rút gọn (Booking/ShortLink)** — mỗi yêu cầu đánh giá tạo 1 link rút gọn để gửi khách; tổng số khách nhận link lấy từ đây.
- **Hồ sơ khách hàng (KhachHang)** — khi khách đánh giá xong, ghi một dòng **lịch sử tương tác "Đánh giá"** vào hồ sơ khách.
- **Thông báo** — khi khách gửi đánh giá, hệ thống bắn **thông báo về cho chi nhánh** (web + mobile) để shop biết và xử lý.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · [dev.md](./dev.md) · Module: [../README.md](../README.md)
