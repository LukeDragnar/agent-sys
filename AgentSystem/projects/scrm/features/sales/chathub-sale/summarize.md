---
alias: ["chathub sale", "phân quyền fanpage", "quyền chat facebook", "gán page cho sale", "user fanpage", "quyền tương tác page", "chat khách facebook"]
module: sales
trang_thai: "Add-on — cần tiện ích ChatHub (TI_ChatHub)"
goi: cần product xác nhận
quyen: "cần product xác nhận — toàn bộ controller gate bằng tiện ích TI_ChatHub. Các thao tác có mã quyền riêng `FBUserFanpages_FacebookChat_*`. Riêng việc PHÂN QUYỀN (thêm mới) chỉ admin / admin chi nhánh thực hiện được (kiểm tra trong service, dù mã quyền cho nhiều role). Xóa 1 bản ghi phân quyền (`XoaUF`) chỉ gắn `[SBAuthorize]` không key."
phu_thuoc: ["Tiện ích ChatHub (TI_ChatHub) — bắt buộc", "Kết nối Facebook / Fanpage (FBFanpage — page đã kết nối mới gán được)", "Cơ cấu tổ chức (phòng ban / nhóm — để lọc fanpage theo cấp quản lý)", "Quản lý người dùng (User — chọn user để gán)"]
link_flow: ./flow.md
---

# ChatHub Sale — Phân quyền Fanpage

## Là gì
Đây là phần **phân quyền tương tác giữa nhân viên (User) và Fanpage Facebook** trong ChatHub: quyết định nhân viên nào được vào page nào, và trong page đó được làm gì — **đăng/sửa/xóa bài, comment, đọc/trả/xóa inbox**. Nhờ vậy đội Sale chỉ thấy và xử lý đúng những fanpage mình được giao, và quản lý kiểm soát được ai làm gì trên từng page.

> Lưu ý phạm vi: tính năng này tập trung vào **gán quyền & lọc fanpage theo nhân viên/cấp quản lý**. Việc trả tin nhắn / chat thực tế với khách diễn ra trong màn ChatHub (tham chiếu feature ChatHub bên Công việc / Marketing).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách fanpage của tôi | ✅ | có tiện ích ChatHub |
| Xem user trong 1 fanpage | ✅ | có tiện ích ChatHub |
| Lọc fanpage theo điều kiện (theo cấp quản lý) | ✅ | có tiện ích; lọc theo role (admin/manager/leader/user) |
| Lọc user quản lý 1 page | ✅ | có tiện ích ChatHub |
| **Phân quyền (gán user vào page)** | ⚠️ | chỉ **admin / admin chi nhánh** (dù mã quyền cho nhiều role, service vẫn chặn) |
| Cập nhật quyền tương tác (9 cờ) | ✅ | có tiện ích ChatHub |
| Xem chi tiết 1 bản ghi phân quyền | ✅ | có tiện ích ChatHub |
| Xóa fanpage khỏi tôi (tự rời page) | ✅ | user thường tự gỡ; **admin bị chặn** (báo liên hệ kỹ thuật) |
| Xóa 1 bản ghi phân quyền cụ thể | ⚠️ | chỉ cần đăng nhập + có tiện ích (không có quyền chi tiết — *xem dev.md*) |

## Để làm gì / cho ai
- **Admin / admin chi nhánh:** gán nhân viên vào fanpage và cấp quyền tương tác (post/comment/inbox) cho từng người.
- **Quản lý (QL Sale / QL MKT) / leader:** xem được các fanpage trong phạm vi quản lý của mình (theo phòng ban / nhóm).
- **Nhân viên Sale / MKT:** chỉ thấy các fanpage được gán cho mình; có thể tự rời page.

## Cách dùng
**Gán quyền (admin):**
1. Vào **ChatHub → Phân quyền Fanpage**.
2. Chọn fanpage và **chọn user** cần gán (lấy từ danh sách user của đơn vị).
3. Bấm **Thêm mới (phân quyền)** → tick các quyền tương tác cho user trên page đó.
4. Lưu → user được gán vào page với đúng quyền.

**Cập nhật / gỡ quyền:**
1. Mở user trong page → chỉnh các cờ quyền (đăng/sửa/xóa bài, comment, inbox) → **Lưu quyền**.
2. Gỡ user khỏi page: dùng xóa bản ghi phân quyền; nhân viên cũng có thể tự rời page.

**Lọc fanpage:**
1. Dùng bộ lọc fanpage → hệ thống tự rẽ theo cấp của bạn: admin thấy toàn bộ page đơn vị, quản lý thấy page của phòng/nhóm, leader thấy page của nhóm, nhân viên thấy page được gán.

## Cần chú ý
- **Cần tiện ích ChatHub:** toàn bộ tính năng chỉ dùng được khi đơn vị đã được kích hoạt tiện ích ChatHub.
- **Chỉ admin / admin chi nhánh phân quyền được:** dù hệ thống cho nhiều vai trò gọi, phần thêm mới phân quyền bị chặn ở bên trong — vai khác sẽ bị từ chối.
- **Admin không xóa fanpage qua màn này được:** nhánh admin xóa fanpage hiện đang bị vô hiệu hóa, hệ thống báo "Liên hệ kỹ thuật". Nhân viên thì chỉ tự rời page của mình.
- **9 cờ quyền tương tác** gồm: tạo/sửa/xóa bài, trả/sửa/xóa comment, đọc/sửa/xóa inbox. Cập nhật quyền chỉ lưu đúng 9 cờ này.
- **Lọc fanpage phụ thuộc cơ cấu tổ chức:** nếu phòng ban / nhóm của nhân viên chưa khai báo đúng, danh sách page theo cấp quản lý có thể không đầy đủ.
- Một số điểm phân quyền lỏng / bug đã được ghi nhận (xóa bản ghi không kiểm tra chủ sở hữu, rò rỉ ngang tenant ở xem chi tiết) — *xem dev.md, để session security xử lý*.

## Ảnh hưởng tới
- **ChatHub (chat thực tế):** quyết định nhân viên thấy/được thao tác page nào khi vào chat.
- **Kết nối Facebook / Fanpage:** chỉ gán được những page đã kết nối (FBFanpage).
- **Cache fanpage:** khi xóa fanpage (admin — hiện disabled) sẽ clear cache theo page + đơn vị.
- **Cơ cấu tổ chức:** phân quyền lọc fanpage dựa trên phòng ban / nhóm của user.
