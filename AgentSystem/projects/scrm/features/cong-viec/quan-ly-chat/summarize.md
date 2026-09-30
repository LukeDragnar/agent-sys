---
alias: ["quản lý chat", "chat hub", "chathub", "chat facebook", "tin nhắn fanpage", "chia chat", "phân công chat", "báo cáo chat"]
module: cong-viec
trang_thai: "Add-on — cần bật tiện ích ChatHub (TI_ChatHub) cho đơn vị"
goi: "cần product xác nhận — toàn bộ chức năng gate qua tiện ích ChatHub (TI_ChatHub); đơn vị chưa mua tiện ích sẽ không vào được"
quyen: "cần product xác nhận — màn cấu hình chia chat dành cho HT-CSKH, DV-AD-TONG/CN/MCN, DV-TRUC-PAGE; phân quyền fanpage chỉ admin/admin chi nhánh thực hiện được; chia hội thoại thủ công chỉ cần đăng nhập (xem dev.md về lỗ hổng quyền)"
phu_thuoc: ["Kết nối Facebook (Fanpage phải được kết nối + cấp token mới có hội thoại để chia)", "Cơ cấu tổ chức (phòng ban / nhóm / nhân viên chat) để chia chat theo cấp", "Cấu hình chia chat tự động (quy tắc auto-gán hội thoại)", "Khách hàng / Contact (gắn SĐT, tag chat vào hồ sơ khách)"]
link_flow: ./flow.md
---

# Quản lý Chat (Chat Hub)

## Là gì
Đây là **trung tâm tiếp nhận và xử lý tin nhắn / bình luận Facebook** của shop. Tất cả hội thoại (inbox) và bình luận từ các Fanpage đã kết nối đổ về một chỗ; nhân viên trực page trả lời, gắn nhãn (tag), đưa số điện thoại khách về hệ thống, và quản lý được hội thoại nào do ai phụ trách.

Tính năng gồm 3 phần chính:
- **Hộp thoại ChatHub**: màn chat thực tế — xem danh sách hội thoại theo từng Fanpage, đọc/trả lời tin nhắn và bình luận, gắn nhãn, xem thông tin khách.
- **Chia chat (phân công hội thoại)**: quyết định hội thoại đến tay nhân viên/nhóm/phòng ban nào — có **chia tự động** (đặt quy tắc) và **chia thủ công** (chọn người rồi gán ngay). Có cả phần **phân quyền Fanpage**: gán nhân viên vào Fanpage và quy định họ được làm gì (đăng/sửa/xóa bài, comment, inbox).
- **Báo cáo chat**: thống kê hiệu suất — báo cáo theo nhãn (tag), tổng quan tin nhắn/bình luận theo ngày, top nhân viên phản hồi nhanh, tỷ lệ và chi tiết phản hồi từng nhân viên.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Trả lời tin nhắn / bình luận | ✅ | có tiện ích ChatHub + được gán quyền inbox/comment trên Fanpage |
| Gắn nhãn (tag) hội thoại | ✅ | tùy quyền |
| Đưa số điện thoại khách về hệ thống | ✅ | trong lúc xử lý hội thoại |
| Phân quyền nhân viên vào Fanpage | ✅ | chỉ admin / admin chi nhánh |
| Cập nhật quyền tương tác (đăng/sửa/xóa bài, comment, inbox) | ✅ | chỉ admin / admin chi nhánh |
| Cấu hình chia chat tự động | ✅ | HT-CSKH, DV-AD-TONG/CN/MCN, DV-TRUC-PAGE |
| Chia hội thoại thủ công cho nhóm / nhân viên | ⚠️ | chỉ yêu cầu đăng nhập (chưa siết role — xem Cần chú ý) |
| Gán / bỏ gán 1 nhân viên phụ trách hội thoại | ⚠️ | chỉ yêu cầu đăng nhập |
| Xem báo cáo chat (tag / tổng quan / nhân viên) | ✅ | có tiện ích ChatHub |
| **Admin xóa Fanpage qua màn chat** | ❌ | chức năng đang bị vô hiệu hóa — báo "Liên hệ kỹ thuật" |
| Tự gỡ Fanpage khỏi tài khoản của chính mình | ✅ | nhân viên thường tự gỡ page mình đang giữ |

## Để làm gì / cho ai
- **Nhân viên trực page / Sale / CSKH:** đọc và trả lời tin nhắn, bình luận khách trên Facebook tập trung một chỗ; gắn nhãn để phân loại; lấy SĐT khách về để chăm sóc.
- **Quản lý / Trưởng nhóm:** chia hội thoại cho đúng người, đặt quy tắc chia tự động để khách không bị bỏ sót, theo dõi ai đang giữ hội thoại nào.
- **Admin đơn vị:** phân quyền nhân viên vào từng Fanpage và quy định được làm gì.
- **Chủ shop / quản lý:** xem báo cáo hiệu suất chat — ai trả lời nhanh, tỷ lệ phản hồi, lượng tin nhắn theo ngày, nhãn nào nhiều.

## Cách dùng

**Trả lời khách (Hộp thoại ChatHub):**
1. Vào **ChatHub** → chọn Fanpage muốn xử lý.
2. Danh sách hội thoại hiện ra; bấm vào một hội thoại để đọc và trả lời.
3. Gắn nhãn, ghi nhận SĐT khách, xem thông tin khách bên cạnh khi cần.

**Đặt quy tắc chia tự động (Cấu hình chia chat):**
1. Vào màn **Chia chat tự động** → bấm **Thêm / Lưu** một cấu hình.
2. Khai báo quy tắc: hội thoại mới đến sẽ tự gán cho phòng ban / nhóm / nhân viên nào.
3. Lưu lại — từ đó hội thoại mới sẽ được hệ thống tự chia theo quy tắc.

**Chia thủ công ngay (khi cần):**
1. Mở màn **Chia chat thủ công**, chọn phòng ban → nhóm → nhân viên.
2. Chọn hội thoại cần chia → bấm **Chia ngay**.
3. Nhân viên được chọn sẽ thấy hội thoại trong inbox của mình.

**Phân quyền Fanpage (admin):**
1. Vào màn **Phân quyền Fanpage**, chọn Fanpage.
2. Thêm nhân viên vào page và bật/tắt các quyền: đăng bài, sửa bài, xóa bài, trả lời comment, sửa/xóa comment, inbox, sửa/xóa inbox.
3. Lưu.

**Xem báo cáo:**
1. Vào **Báo cáo chat**, chọn tab: Tag chat / Tổng quan / Nhân viên.
2. Chọn khoảng thời gian (tối đa 31 ngày cho báo cáo tổng quan), xem số liệu và biểu đồ.

## Cần chú ý
- **Phải bật tiện ích ChatHub** cho đơn vị thì mới dùng được — chưa mua/chưa bật sẽ không vào được màn nào.
- **Báo cáo tổng quan / biểu đồ giới hạn 31 ngày**: chọn khoảng dài hơn 31 ngày sẽ báo lỗi. Nên xem từng đợt ngắn.
- **Chia hội thoại thủ công và gán/bỏ gán nhân viên hiện chỉ cần đăng nhập** (chưa giới hạn theo vai trò) — nghĩa là người không phải quản lý vẫn có thể thao tác. Đây là điểm cần product/kỹ thuật xem lại (đã ghi rõ trong tài liệu kỹ thuật).
- **Admin không xóa được Fanpage trực tiếp qua màn chat** — chức năng này đang bị tắt, hệ thống báo "Liên hệ kỹ thuật". Nhân viên thường chỉ có thể tự gỡ page khỏi tài khoản của mình.
- **Báo cáo "khách chat mới/cũ"** phụ thuộc dữ liệu khách chat đã được ghi nhận; nếu khách chưa từng được lưu thì có thể đếm lệch.
- Số liệu báo cáo **tách theo đơn vị và chi nhánh** của người đăng nhập — mỗi người chỉ thấy số của phạm vi mình.

## Ảnh hưởng tới
- **Hồ sơ khách hàng / Contact**: SĐT và nhãn lấy từ hội thoại được gắn vào khách.
- **Phân công công việc của nhân viên chat**: chia hội thoại quyết định inbox ai thấy gì.
- **Cơ cấu tổ chức**: chia chat theo phòng ban/nhóm dựa trên cơ cấu đã khai báo — đổi cơ cấu ảnh hưởng phạm vi chia.
- **Kết nối Facebook**: Fanpage ngắt kết nối / hết token thì hội thoại không về, không chia được.
