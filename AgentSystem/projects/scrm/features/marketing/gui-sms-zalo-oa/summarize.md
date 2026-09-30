---
alias: ["gửi sms", "gửi zalo", "gửi tin", "soạn tin", "tin nhắn khách", "brandname", "Zalo OA", "ZOA", "mẫu sms", "campaign sms", "tài khoản sms", "nạp tiền sms"]
module: marketing
trang_thai: cần product xác nhận — cần bật tiện ích SMS cho đơn vị
goi: cần product xác nhận
quyen: "cần product xác nhận — màn gửi tin chỉ yêu cầu đăng nhập (không gắn quyền riêng); màn Mẫu SMS/Zalo: HT-AD, DV-AD-TONG, HT-CSKH, HT-KT, DV-QL-MKT, DV-MKT-LEAD, DV-MKT, DV-AD-CN; màn Tài khoản SMS (cấp hệ thống/CSKH): HT-AD, HT-CSKH; Import giá tin: chỉ super user hoặc kế toán hệ thống (HT-KT)"
phu_thuoc: ["Tiện ích SMS còn hạn dùng của đơn vị", "Tài khoản SMS (brandname/Zalo OA + số dư) do CSKH cấu hình", "Mẫu SMS/Zalo đã duyệt", "Nhà cung cấp gửi tin (ESMS)", "Danh sách khách hàng / chiến dịch (chọn người nhận)"]
link_flow: ./flow.md
---

# Gửi SMS & Zalo OA

## Là gì
Đây là nơi **soạn và gửi tin nhắn SMS / Zalo OA cho khách hàng**: gửi tin chăm sóc, khuyến mãi, nhắc lịch, thông báo đơn hàng… Hệ thống dùng **tài khoản SMS** (brandname hoặc Zalo OA) của đơn vị để gửi qua nhà cung cấp ESMS, trừ tiền theo số tin gửi, và ghi lại lịch sử tin đã gửi (thành công/thất bại).

Tính năng này gồm các phần:
- **Gửi tin**: soạn nội dung (hoặc chọn mẫu) → chọn khách nhận → gửi. Tin được đẩy vào hàng đợi xử lý nền, gửi xong sẽ cập nhật kết quả.
- **Mẫu SMS / Zalo**: khai báo sẵn các mẫu nội dung để gửi nhanh; có thể đồng bộ mẫu đã được nhà mạng duyệt từ nhà cung cấp.
- **Tài khoản SMS**: quản lý brandname/Zalo OA, số dư, nạp tiền — phần này thường do bộ phận CSKH/quản trị hệ thống cấu hình cho đơn vị.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Gửi tin SMS / Zalo cho khách | ✅ | đơn vị có tiện ích SMS còn hạn; tài khoản còn số dư |
| Gửi tin theo mẫu có sẵn | ✅ | đã có mẫu tin |
| Xem danh sách tin đã gửi | ✅ | chỉ cần đăng nhập |
| Thêm / Sửa / Xóa mẫu tin | ✅ | HT-AD, DV-AD-TONG, HT-CSKH |
| Đồng bộ mẫu đã duyệt từ nhà mạng (ESMS) | ✅ | HT-AD, DV-AD-TONG, HT-CSKH |
| Thêm mẫu mặc định có sẵn | ✅ | HT-AD, DV-AD-TONG, HT-CSKH |
| Tạo / Sửa / Xóa tài khoản SMS (brandname/Zalo OA) | ✅ | HT-AD, HT-CSKH (cấp hệ thống) |
| Nạp tiền vào tài khoản SMS | ✅ | HT-AD, HT-CSKH |
| Xem lịch sử nạp tiền | ✅ | HT-AD, HT-CSKH |
| Import cập nhật bảng giá tin (Excel) | ⚠️ | chỉ super user hoặc HT-KT |

## Để làm gì / cho ai
- **Chủ shop / Marketing:** gửi tin khuyến mãi, chăm sóc khách hàng hàng loạt theo chiến dịch.
- **CSKH / Sale:** nhắc lịch hẹn, xác nhận đơn, thông báo cho khách.
- **Hệ thống (tự động):** gửi thông báo đơn hàng, OTP, nhắc tác nghiệp — chạy ngầm, không cần người bấm.
- **CSKH cấp hệ thống / Kế toán:** cấu hình tài khoản gửi tin, nạp tiền, quản lý bảng giá tin cho từng đơn vị.

## Cách dùng

**Gửi tin cho khách:**
1. Vào **Marketing → SMS/Zalo → Gửi tin** (hoặc gửi trực tiếp từ danh sách khách hàng / chiến dịch).
2. Chọn **kênh** (SMS hay Zalo OA) và **brandname / Zalo OA** muốn dùng.
3. Soạn nội dung hoặc **chọn một mẫu** đã khai báo.
4. Chọn **khách hàng nhận** (1 khách, một nhóm, hoặc cả danh sách đã lọc).
5. Bấm **Gửi** → hệ thống nhận yêu cầu và đưa tin vào hàng đợi gửi. Kết quả gửi sẽ cập nhật sau ít phút.

**Quản lý mẫu tin (Mẫu SMS/Zalo):**
1. Vào màn **Mẫu SMS/Zalo**, bấm **Thêm mới**, nhập nội dung mẫu, lưu.
2. Có thể **Đồng bộ mẫu từ ESMS** để lấy về các mẫu đã được nhà mạng duyệt, hoặc **Thêm mẫu mặc định có sẵn**.
3. Sửa / Xóa / Đổi trạng thái mẫu khi cần.

**Quản lý tài khoản SMS (CSKH/Admin hệ thống):**
1. Vào màn **Quản lý tài khoản SMS**, **Thêm** tài khoản (brandname/Zalo OA, apikey…).
2. **Nạp tiền** vào tài khoản; xem **lịch sử nạp**.

## Cần chú ý
- **Bấm "Gửi" báo thành công KHÔNG đảm bảo tin đã tới khách.** Hệ thống nhận yêu cầu và đẩy vào hàng đợi rồi báo thành công ngay; việc gửi thật và kết quả (tới/không tới) chỉ biết sau khi nhà mạng phản hồi. Hãy kiểm tra lại trong **danh sách tin đã gửi** để xem trạng thái thật.
- **Phải còn tiền trong tài khoản SMS** và **brandname/Zalo OA đã được nhà mạng duyệt** thì tin mới gửi được.
- **Tiện ích SMS phải còn hạn**: nếu đơn vị chưa mua hoặc hết hạn tiện ích SMS, tin có thể không được gửi.
- **Mẫu Zalo phải đúng mẫu đã duyệt**: Zalo OA chỉ gửi được nội dung theo template đã được Zalo phê duyệt; tự ý đổi nội dung có thể bị từ chối.
- **Import bảng giá tin** chỉ dành cho super user hoặc kế toán hệ thống — người khác bấm sẽ báo không có quyền.

## Ảnh hưởng tới
- **Số dư tài khoản SMS** — mỗi tin gửi đi sẽ trừ tiền tương ứng.
- **Lịch sử tương tác khách hàng** — tin đã gửi được ghi nhận để chăm sóc.
- **Thông báo tự động** (đơn hàng, OTP, tác nghiệp) — dùng chung hạ tầng gửi tin này.
- **pushsale** — kết quả gửi được đẩy sang để cập nhật.
