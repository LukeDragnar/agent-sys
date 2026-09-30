---
alias: ["chathub marketing", "phân quyền fanpage", "quyền tương tác page", "gán user fanpage", "chat facebook marketing", "user fanpage", "lọc fanpage"]
module: marketing
trang_thai: cần product xác nhận — cần bật tiện ích ChatHub
goi: cần product xác nhận
quyen: "cần product xác nhận — cần tiện ích ChatHub; chỉ admin / admin chi nhánh mới phân quyền (gán user vào fanpage); lọc fanpage rẽ theo vai (admin xem toàn bộ, quản lý/lead xem theo nhóm, nhân viên chỉ xem page được gán). Một số endpoint thiếu permission key / không lọc đơn vị — xem dev."
phu_thuoc: ["Kết nối Facebook (fanpage đã kết nối + token)", "Cơ cấu tổ chức (phòng ban / nhóm để phân quyền lọc fanpage)", "Tài khoản người dùng (User của đơn vị để gán)", "ChatHub / tin nhắn Facebook (màn chat dùng quyền này)"]
link_flow: ./flow.md
---

# ChatHub Marketing

## Là gì
Đây là phần **phân quyền tương tác giữa nhân viên và fanpage Facebook** trong ChatHub — quyết định ai được làm gì trên page nào: đăng/sửa/xóa bài, trả lời/sửa/xóa comment, xử lý inbox. Nhờ đó nhiều nhân viên cùng chăm sóc fanpage mà không giẫm chân nhau và đúng quyền hạn.

Gồm các việc chính:
- **Gán nhân viên vào fanpage** kèm bộ quyền tương tác (9 cờ: tạo/sửa/xóa bài, trả lời/sửa/xóa comment, inbox/sửa inbox/xóa inbox).
- **Lọc fanpage** theo vai (admin thấy toàn bộ page đơn vị; quản lý/lead thấy theo phòng/nhóm; nhân viên chỉ thấy page được gán).
- **Lọc nhân viên** quản lý một page; **xem page của tôi**; **gỡ quyền** / rời page.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem fanpage của tôi | ✅ | có tiện ích ChatHub |
| Xem nhân viên trong một fanpage | ✅ | có tiện ích ChatHub |
| Lọc fanpage theo điều kiện (theo vai) | ✅ | rẽ theo vai: admin/quản lý/lead/nhân viên |
| Gán nhân viên vào fanpage (phân quyền) | ✅ | chỉ admin / admin chi nhánh |
| Cập nhật quyền tương tác (9 cờ) | ✅ | có tiện ích ChatHub |
| Gỡ page khỏi mình (rời page) | ✅ | nhân viên tự gỡ quyền của mình |
| Admin xóa hẳn fanpage qua màn này | ❌ | nhánh admin đã bị vô hiệu — báo "liên hệ kỹ thuật" (xem dev) |
| Xóa một bản ghi phân quyền cụ thể | ⚠️ | làm được nhưng **thiếu kiểm tra quyền/đơn vị** — lỗ hổng (xem dev) |

## Để làm gì / cho ai
- **Quản lý ChatHub / Marketing:** phân công nhân viên chăm sóc từng fanpage, giới hạn quyền (ai được đăng bài, ai chỉ trả inbox).
- **Nhân viên chăm sóc:** thấy đúng các page mình phụ trách, làm đúng quyền được cấp.
- **Admin:** thiết lập ai vào page nào với quyền gì.

## Cách dùng
**Gán nhân viên vào fanpage (admin):**
1. Vào **ChatHub → Phân quyền Fanpage**.
2. Chọn fanpage → **Thêm** nhân viên → đặt các quyền tương tác (đăng bài, comment, inbox…) → lưu.
3. Cần sửa quyền → mở bản ghi → bật/tắt các cờ quyền → lưu.

**Nhân viên xem & rời page:**
1. Xem **danh sách page của tôi**.
2. Nếu không còn phụ trách → **gỡ page khỏi mình** (rời page).

**Lọc fanpage:**
- Dùng bộ lọc fanpage → hệ thống tự trả danh sách theo vai bạn (admin toàn bộ; quản lý/lead theo nhóm; nhân viên chỉ page được gán).

## Cần chú ý
- **Chỉ admin / admin chi nhánh mới gán quyền fanpage.** Vai thường gọi vào sẽ bị từ chối ở tầng xử lý, dù menu có hiện.
- **Mỗi nhân viên chỉ thấy page mình được gán** (trừ admin/quản lý). Đây là phân quyền theo vai + cơ cấu tổ chức, không phải lỗi.
- **Cập nhật quyền chỉ lưu 9 cờ quyền** — đổi thông tin khác (người dùng, page) ở bản ghi này sẽ không lưu.
- **Admin không xóa hẳn fanpage qua màn này** — chức năng đó đã bị vô hiệu, hệ thống báo liên hệ kỹ thuật. Nhân viên chỉ tự gỡ quyền của chính mình.
- **Phân quyền lọc phụ thuộc cơ cấu tổ chức** (phòng ban/nhóm). Nếu nhân viên chưa được xếp phòng/nhóm đúng, danh sách fanpage lọc ra có thể thiếu.
- ⚠️ **Có một số lỗ hổng kỹ thuật** ở phần này (xóa bản ghi phân quyền không kiểm tra chủ sở hữu, xem chi tiết không lọc đơn vị) — đã ghi nhận cho đội kỹ thuật (xem dev).

## Ảnh hưởng tới
- **Màn chat ChatHub / tin nhắn Facebook** — quyền gán ở đây quyết định nhân viên thấy & thao tác được page nào.
- **Kết nối Facebook (fanpage + token)** — phân quyền dựa trên các page đã kết nối.
- **Cơ cấu tổ chức** — vai + phòng ban/nhóm quyết định phạm vi lọc fanpage.
