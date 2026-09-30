---
alias: ["kết nối Facebook", "fanpage", "kết nối fanpage", "đồng bộ page facebook", "bài đăng facebook", "post fanpage", "tài khoản quảng cáo facebook", "ads account", "ngân sách facebook", "kết nối page chat"]
module: marketing
trang_thai: "cần product xác nhận — gồm 2 tiện ích riêng: ChatHub (cho fanpage + bài đăng) và Facebook quảng cáo (cho tài khoản quảng cáo). Đơn vị phải mua tiện ích tương ứng mới vào được."
goi: cần product xác nhận
quyen: "cần product xác nhận — theo registry: Đồng bộ fanpage / Kiểm tra token cho AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT, QL-SALE, SALE, SALE-LEAD, TRUC-PAGE; Lưu cấu hình fanpage hẹp hơn (AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT); Tài khoản quảng cáo (Tìm/Sửa) cho DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT. Một số nút (kích hoạt page, gắn landing, thêm tài khoản ads) chỉ kiểm tra đăng nhập + tiện ích, không gắn quyền riêng."
phu_thuoc: ["Tài khoản Facebook đăng nhập (lấy danh sách page, token)", "Gói cước ChatHub (giới hạn số fanpage được kích hoạt)", "Nguồn dữ liệu / Landing (gắn nguồn cho fanpage, bài post, tài khoản ads)", "ChatHub (tin nhắn / bình luận chảy về sau khi kích hoạt page)", "Quản lý nguồn dữ liệu (landing)"]
link_flow: ./flow.md
---

# Kết nối Facebook

## Là gì
Đây là nơi shop **kết nối hệ thống với Facebook** để làm 3 việc chính:
- **Fanpage**: đồng bộ các fanpage của shop từ tài khoản Facebook về hệ thống, bật/tắt sử dụng, gắn nguồn (landing), và **kích hoạt** page để tin nhắn / bình luận của khách chảy về ChatHub.
- **Bài đăng (post)**: xem danh sách bài viết của fanpage, bật sử dụng và gắn nguồn (landing) cho từng bài để theo dõi khách tương tác từ bài đó.
- **Tài khoản quảng cáo (Ads Account)**: kéo các tài khoản quảng cáo Facebook về hệ thống để theo dõi và đồng bộ **ngân sách quảng cáo** (chi tiêu theo chiến dịch / nhóm quảng cáo / quảng cáo) gắn với từng nguồn.

Nói gọn: fanpage + bài đăng phục vụ **chăm sóc / chat với khách**, còn tài khoản quảng cáo phục vụ **đo lường ngân sách marketing** trên Facebook.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Đồng bộ (kéo) fanpage từ tài khoản Facebook | ✅ | sau khi đăng nhập Facebook; cần quyền MKT/Sale/Admin |
| Bật/tắt sử dụng fanpage + gắn nguồn (landing) | ✅ | bật sử dụng phải chọn ít nhất 1 (đồng bộ tin nhắn / đồng bộ bình luận) và phải gắn nguồn; quyền hẹp hơn (Admin/MKT/MKT-LEAD/QL-MKT) |
| Kích hoạt / hủy kích hoạt fanpage | ✅ / ⚠️ | bị giới hạn theo **số page của gói cước**; 1 fanpage chỉ được kích hoạt ở 1 chi nhánh tại 1 thời điểm |
| Kiểm tra token kết nối fanpage | ✅ | tùy quyền |
| Xem danh sách bài đăng + gắn nguồn cho bài đăng | ✅ | chỉ thấy bài của page mình có quyền |
| Xem chi tiết 1 bài đăng | ✅ | tùy quyền |
| Thêm (kéo) tài khoản quảng cáo từ Facebook | ✅ | cần đã kết nối tài khoản Facebook + có tiện ích Facebook |
| Sửa nguồn (landing) cho tài khoản quảng cáo | ✅ | Admin / MKT / MKT-LEAD / QL-MKT |
| **Xóa fanpage** | ❌ | chức năng xóa cũ đã ngừng dùng (*cần product xác nhận* có cách thay thế trên giao diện không) |
| Tự thêm tin nhắn / sửa nội dung bài đăng Facebook | ❌ | hệ thống chỉ **đồng bộ về để xem/định tuyến**, không đăng/sửa nội dung trên Facebook |

## Để làm gì / cho ai
- **Marketing / chủ shop:** đưa fanpage về một chỗ, kích hoạt để khách nhắn tin / bình luận chảy về ChatHub trả lời tập trung; gắn nguồn để biết khách đến từ page nào.
- **Marketing (đo ngân sách):** kéo tài khoản quảng cáo về để theo dõi shop đang chi bao nhiêu cho từng chiến dịch, gắn với nguồn để tính hiệu quả.
- **Nhân viên trực page / Sale:** đồng bộ và kiểm tra token để biết page còn kết nối được hay không.

## Cách dùng

**Kết nối & kích hoạt fanpage:**
1. Vào **Marketing / ChatHub → Cấu hình fanpage**.
2. **Đăng nhập Facebook** (cấp quyền cho hệ thống) rồi bấm **Đồng bộ fanpage** — danh sách page của bạn sẽ hiện ra.
3. Mở 1 page, **chọn nguồn (landing)**, bật **đồng bộ tin nhắn** và/hoặc **đồng bộ bình luận**, bấm **Lưu**.
4. Bấm **Kích hoạt** page đó. Nếu page đang được kích hoạt ở chi nhánh khác, hệ thống hỏi xác nhận trước khi chuyển. Số page kích hoạt bị giới hạn theo gói cước.
5. Cần kiểm tra kết nối → bấm **Kiểm tra token** để xem token còn hợp lệ không.

**Bài đăng:**
1. Vào màn **Bài đăng (post)**, chọn page và lọc theo "đã gắn / chưa gắn nguồn".
2. Mở 1 bài, bật **sử dụng** và **gắn nguồn (landing)**, lưu.

**Tài khoản quảng cáo:**
1. Vào màn **Tài khoản quảng cáo**, bấm **Thêm tài khoản quảng cáo** (chọn tài khoản Facebook đã kết nối) — hệ thống kéo các tài khoản ads về.
2. Mở 1 tài khoản, **chọn nguồn (landing)** rồi lưu. Ngân sách sẽ được đồng bộ dần (chiến dịch → nhóm quảng cáo → quảng cáo).

## Cần chú ý
- **Phải mua đúng tiện ích.** Fanpage + bài đăng nằm trong tiện ích **ChatHub**; tài khoản quảng cáo nằm trong tiện ích **Facebook** — đây là 2 tiện ích khác nhau, có thể có cái này thiếu cái kia.
- **Số fanpage kích hoạt bị giới hạn theo gói cước.** Khi đã kích hoạt đủ số page tối đa, muốn bật page mới phải hủy bớt page cũ. Hệ thống báo "Đơn vị chỉ được kích hoạt tối đa N FanPage".
- **1 fanpage chỉ kích hoạt ở 1 chi nhánh.** Kích hoạt page đang dùng ở chi nhánh khác sẽ tự tắt page bên kia (cần xác nhận).
- **Bật sử dụng page bắt buộc gắn nguồn + chọn ít nhất 1 loại đồng bộ.** Nếu không, hệ thống không cho lưu.
- **Token Facebook có thể hết hạn.** Nếu page bỗng không nhận tin nhắn, hãy **Kiểm tra token**; hết hạn thì phải đăng nhập Facebook lại và đồng bộ lại.
- **Hệ thống không đăng bài / không gửi tin thay bạn lên Facebook ở màn này** — chỉ kéo dữ liệu về để xem và định tuyến. (Trả lời tin nhắn là ở ChatHub.)
- Người dùng **chỉ thấy fanpage / bài đăng / tài khoản ads thuộc quyền của mình** (page mình có quyền, tài khoản ads mình kết nối). Admin thấy toàn đơn vị. Đây là phân quyền, không phải lỗi.

## Ảnh hưởng tới
- **ChatHub (tin nhắn / bình luận Facebook):** chỉ khi fanpage được **kích hoạt** thì tin nhắn / bình luận khách mới chảy về để nhân viên trả lời.
- **Nguồn dữ liệu / Landing:** nguồn gắn ở fanpage / bài đăng / tài khoản ads dùng để gom khách và tính ngân sách về đúng nguồn.
- **Báo cáo Marketing (ngân sách):** tài khoản quảng cáo là điểm bắt đầu của chuỗi đồng bộ ngân sách Facebook; gắn sai nguồn → ngân sách về sai nơi.
- **Khách hàng / Contact:** tin nhắn Facebook về có thể tạo contact / khách hàng mới.
