---
alias: ["cấu hình xếp nhân viên", "auto gán nhân viên", "xếp nhân viên phục vụ", "config nhân viên order", "quản lý nhân viên xử lý", "tự động gán nhân viên đơn"]
module: cua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — Xem chi tiết & Lưu cấu hình có phân quyền riêng; riêng việc lấy danh sách cấu hình chỉ yêu cầu đăng nhập (không gắn quyền riêng)."
phu_thuoc: ["Tài khoản nhân viên (đối tượng được gán phục vụ đơn)", "Luồng tạo/phục vụ đơn POS (bước Xếp nhân viên đọc cấu hình này)"]
link_flow: ./flow.md
---

# Quản lý nhân viên xử lý (Cấu hình xếp nhân viên)

## Là gì
Đây là nơi **cấu hình quy tắc tự động gán nhân viên phục vụ cho đơn hàng** tại cửa hàng. Khi tạo/phục vụ đơn ở POS, hệ thống dựa vào cấu hình này để quyết định bước **"Xếp nhân viên"** — gán đúng nhân viên phục vụ cho đơn. Bạn vào đây để xem và lưu lại quy tắc xếp nhân viên cho đơn vị.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách cấu hình xếp nhân viên | ✅ | chỉ cần đăng nhập |
| Xem chi tiết một cấu hình | ✅ | tùy quyền |
| Lưu / cập nhật cấu hình | ✅ | tùy quyền |
| Xóa cấu hình | ❌ | không có chức năng xóa riêng (chỉ Lấy DS / Xem / Lưu) |

## Để làm gì / cho ai
- **Quản lý cửa hàng / admin đơn vị:** thiết lập cách hệ thống tự gán nhân viên phục vụ cho đơn, để khi bán hàng ở quầy không phải chọn tay từng đơn.
- **Thu ngân / nhân viên POS:** hưởng lợi gián tiếp — đơn được tự xếp nhân viên phục vụ theo quy tắc đã cấu hình.

## Cách dùng
1. Vào **Cài đặt hệ thống → Cấu hình Order → popup "Auto gán user" → tab cấu hình nhân viên**.
2. Hệ thống tải danh sách cấu hình xếp nhân viên hiện có.
3. Mở một cấu hình để xem chi tiết, chỉnh theo nhu cầu.
4. Bấm **Lưu** để áp dụng quy tắc xếp nhân viên cho đơn vị.

## Cần chú ý
- Đây là **cấu hình nền** cho bước "Xếp nhân viên" của luồng tạo đơn POS — sửa cấu hình ở đây sẽ thay đổi cách đơn được gán nhân viên về sau.
- Không có chức năng **xóa** cấu hình riêng; bạn chỉ lấy danh sách, xem chi tiết và lưu lại.
- *Cần product xác nhận:* các tham số chi tiết trong cấu hình (gán theo ca, theo khu vực, theo lượt...) — cấu trúc cụ thể do product/đội Order định nghĩa.

## Ảnh hưởng tới
- **Luồng tạo / phục vụ đơn POS** — bước "Xếp nhân viên" (gán nhân viên phục vụ đơn) đọc cấu hình này.
- **Báo cáo theo nhân viên phục vụ** (nếu có) — phụ thuộc việc đơn được gán đúng nhân viên.
