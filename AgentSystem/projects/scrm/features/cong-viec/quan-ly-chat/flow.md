# Luồng — Quản lý Chat (Chat Hub)

## Tiền điều kiện
- Đơn vị đã **bật tiện ích ChatHub** (`TI_ChatHub`); thiếu tiện ích → chặn ở mọi màn.
- Có ít nhất 1 **Fanpage Facebook đã kết nối** + còn token hợp lệ thì mới có hội thoại đổ về.
- Nhân viên đã được **phân quyền vào Fanpage** (gán user ↔ fanpage) với các cờ quyền tương ứng (inbox/comment/post).
- Để chia chat theo cấp: đơn vị đã khai báo **cơ cấu tổ chức** (phòng ban / nhóm / nhân viên chat); nhân viên cần cờ `IsNhanVienChatFacebook`.
- Báo cáo: người dùng thuộc đúng đơn vị + chi nhánh (số liệu lọc cứng theo `IdDonVi`+`IdChiNhanh`).

## Luồng dùng (nhân viên trực page)
1. Vào **ChatHub** → chọn Fanpage → mở danh sách hội thoại.
2. Bấm vào 1 hội thoại → đọc tin nhắn/bình luận → trả lời (cần cờ quyền inbox/comment trên Fanpage).
3. Gắn nhãn (tag) hội thoại, ghi nhận SĐT khách về hệ thống.
4. Tin nhắn/bình luận được ghi vào MongoDB (MessageChatBox/Conversation) phục vụ báo cáo về sau.

## Luồng cấu hình (admin / quản lý)
**Chia chat tự động:**
1. Vào màn cấu hình → Thêm/Lưu một `CauHinhChiaSoChat` (quy tắc).
2. Khai báo: hội thoại mới → gán cho phòng ban / nhóm / nhân viên nào.
3. Lưu → khi hội thoại mới đến, consumer chia chat đọc cấu hình (endpoint private theo IdDonVi+IdChiNhanh+IdCSC) và tự gán.

**Chia thủ công ngay:**
1. Chọn phòng ban → nhóm → nhân viên chat.
2. Chọn hội thoại (grid) → bấm **Chia ngay** (`ChiaChatNhomNgay` cho phòng ban / `ChiaChatUserNgay` cho nhóm NV).
3. Hội thoại được gán → nhân viên đích thấy trong inbox.

**Gán 1 nhân viên phụ trách 1 hội thoại (UserConversation):**
1. Chọn hội thoại + nhân viên → **Chia hội thoại**.
2. Hệ thống kiểm: hội thoại có thuộc đơn vị/chi nhánh không, đã chia cho người này chưa, người này có phải nhân viên chat (`IsNhanVienChatFacebook`) không.
3. Hợp lệ → tạo bản ghi UserConversation; **Bỏ chia** → xóa bản ghi đó.

**Phân quyền Fanpage (admin):**
1. Chọn Fanpage → Thêm nhân viên (chỉ admin/admin chi nhánh được Thêm).
2. Bật/tắt 9 cờ quyền: IsCreatePost, IsEditPost, IsDeletePost, IsReplyComment, IsEditComment, IsDeleteComment, IsInbox, IsEditInbox, IsDeleteInbox.
3. Lưu (`CapNhatThongTin` chỉ lưu 9 cờ quyền này).

**Báo cáo chat:**
1. Chọn tab: Tag chat / Tổng quan / Nhân viên.
2. Chọn khoảng ngày (tổng quan/biểu đồ tối đa 31 ngày) → xem số liệu.

## Các trường hợp / case biên
- **Chưa bật tiện ích ChatHub** → mọi màn chặn (gate `TienIch`).
- **Báo cáo tổng quan/biểu đồ chọn > 31 ngày** → trả lỗi (gate 31 ngày). Thiếu TuNgay/DenNgay → có thể lỗi hệ thống (NRE — xem dev.md).
- **Chia 1 hội thoại cho người đã giữ nó** → "Người này đã quản lý cuộc trò chuyện này".
- **Chia cho người không phải nhân viên chat** (`IsNhanVienChatFacebook != true`) → "Người này không phải nhân viên chat".
- **Hội thoại không thuộc đơn vị/chi nhánh người dùng** → "Không tìm được cuộc trò chuyện".
- **Thêm phân quyền Fanpage trùng** (user đã có quyền page đó) → bị chặn trùng.
- **Vai trò không phải admin/admin chi nhánh bấm Thêm phân quyền** → bị service từ chối dù màn cho thấy nút.
- **Admin bấm Xóa Fanpage qua màn chat** → "Liên hệ kỹ thuật..." (nhánh admin xóa đã tắt).
- **Top 5 nhân viên / tỷ lệ phản hồi khi không có dữ liệu** → trả rỗng (không lỗi).

## Ví dụ cụ thể có số
Shop B có 2 Fanpage, đội chat 4 người. Trong tháng 6 (01–30/06, 30 ngày — hợp lệ < 31):
- Báo cáo tổng quan: tổng tin nhắn KH 1.250, tin nhắn Trang 1.100, comment KH 430; khách chat mới 86, khách cũ 540.
- Báo cáo Top 5 nhân viên: NV An TB 45.000 ms (45s), NV Bình 60.000 ms, NV Châu 90.000 ms → xếp An #1, Bình #2, Châu #3.
- Tỷ lệ phản hồi: tổng 3.200 tin đến; An xử lý 1.280 → `1280*100/3200 = 40%`; Bình 960 → 30%.
- Chia tự động: đặt quy tắc "hội thoại Fanpage X → nhóm Sale 1". Khách mới nhắn vào Fanpage X → consumer gán vào nhóm Sale 1 ngay.
- Chọn khoảng 01/05–15/06 (46 ngày) cho báo cáo tổng quan → **báo lỗi vượt 31 ngày**, phải tách thành 2 đợt.

## Hậu điều kiện
- Trả lời/gắn tag/lấy SĐT → ghi MessageChatBox/Conversation (Mongo) + cập nhật hồ sơ khách; là nguồn cho báo cáo.
- Chia tự động/thủ công/UserConversation → đổi danh sách hội thoại nhân viên nhìn thấy (không sửa nội dung hội thoại).
- Báo cáo **read-only**: chỉ đọc, không ghi, không phát event.
- Cập nhật phân quyền Fanpage → ghi `FBUserFanpage`; admin xóa Fanpage clear cache (hiện nhánh admin xóa đã tắt).
