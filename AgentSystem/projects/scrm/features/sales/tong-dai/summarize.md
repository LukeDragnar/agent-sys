---
alias: ["tổng đài", "call center", "cuộc gọi", "gọi điện tổng đài", "ghi âm cuộc gọi", "click to call", "voice ip", "sim số", "omicall", "myhub", "tổng đài ip"]
module: sales
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — quản trị tổng đài đa số HT-AD/HT-KT (cấp hệ thống) hoặc DV-AD-TONG/CN/MCN (cấp đơn vị); thao tác gọi & xem lịch sử mở cho nhiều role sale DV. Một số endpoint quyền bị nới lỏng (xem dev.md)."
phu_thuoc: ["Đối tác/nhà cung cấp tổng đài (Omicall/MyHub — external)", "Cổng thanh toán QR (nạp tiền tổng đài IP)", "User CRM (map user đơn vị ↔ user tổng đài)", "Contact (chia/cập nhật contact khi auto-call)", "Phê duyệt giao dịch tổng đài SA (super-admin duyệt nạp tiền)"]
link_flow: ./flow.md
---

# Tổng đài (Voice IP / Sim số / IP mới)

> ⚠️ **Có 3 PHIÊN BẢN tổng đài — rất dễ nhầm.** Khả năng và màn hình khác nhau theo phiên bản đơn vị đang dùng. Phải xác định đơn vị đang ở phiên bản nào trước khi tư vấn.

## Là gì
**Tổng đài** giúp đơn vị **gọi điện cho khách ngay trên hệ thống** (click-to-call), **lưu lịch sử cuộc gọi**, **ghi âm**, gắn cuộc gọi với contact để chăm sóc/bán hàng. Quản trị viên còn cấu hình **đầu số/hotline**, **kịch bản cuộc gọi (IVR/bấm phím)**, **lời chào**, **nhóm nội bộ**, **nạp tiền** và **quản lý user gọi**.

Hệ thống có **3 phiên bản tổng đài**:

| Phiên bản | Tên hay gọi | Đặc điểm |
|---|---|---|
| **(1) Voice IP cũ** | tổng đài đơn vị (bản cũ) | Quản lý cấp hệ thống: tạo/kết nối tổng đài, nạp tiền & duyệt số dư, quản lý user, gói dịch vụ. Bản đăng ký gốc & lịch sử cuộc gọi đời đầu **đã ngừng dùng**. |
| **(2) Sim số (MyHub)** | tổng đài sim số, MyHub | Dùng sim số qua đối tác MyHub: nhận cuộc gọi, gọi trực tiếp (click-to-call), quản lý tổng đài/hotline/user sim số, lịch sử cuộc gọi sim số, xuất Excel. |
| **(3) IP mới (Omicall)** | tổng đài IP, Omicall | Bản mới nhất, đầy đủ nhất: khởi tạo tổng đài, **nạp tiền qua QR**, kịch bản cuộc gọi/bấm phím, lời chào (đọc văn bản/tải mp3), nhóm nội bộ, **auto-call (tự động quay số → chia contact cho sale)**, lịch sử + báo cáo cuộc gọi + xuất Excel. |

## Hành động làm được
| Hành động | Được? | Phiên bản / điều kiện / quyền |
|---|---|---|
| Khởi tạo / kết nối tổng đài cho đơn vị | ✅ | (1) admin/kế toán hệ thống tạo & kết nối; (3) admin/kế toán hệ thống hoặc admin đơn vị/chi nhánh |
| Gọi điện (click-to-call) | ✅ | (2) bản sim số; (3) bản IP — gọi qua nhà cung cấp |
| Nạp tiền tổng đài | ✅ | (1) nạp + hệ thống duyệt số dư; (3) nạp qua **mã QR** rồi tự thanh toán hoặc chờ quản trị hệ thống duyệt |
| Duyệt giao dịch nạp tiền (quản trị hệ thống) | ✅ | (3) ở màn quản trị hệ thống — duyệt/hủy |
| Cấu hình đầu số / hotline | ✅ | (2) quản lý hotline sim số; (3) đăng ký đầu số + hotline |
| Kịch bản cuộc gọi | ✅ | (3) bản IP — khai báo/sửa/xóa kịch bản |
| Kịch bản bấm phím / IVR | ✅ | (3) bản IP |
| Lời chào (greeting) — tải lên / đọc văn bản | ✅ | (3) tải file **chỉ .mp3** (file rỗng → lỗi) hoặc chuyển văn bản thành giọng nói |
| Nhóm nội bộ | ✅ | (3) bản IP — thêm/sửa/xóa nhóm + thành viên |
| Auto-call (phiên gọi tự động) | ✅ | (3) tạo/sửa/xóa phiên; khi sale nhận cuộc gọi → tự chia contact |
| Map / đổi / gia hạn user gọi | ✅ | (2) đổi user sim số; (3) map/đổi/xóa user + tính tiền gia hạn trước |
| Quản lý gói dịch vụ (gia hạn/nâng cấp) | ✅ | (1) admin/kế toán hệ thống |
| Xem lịch sử cuộc gọi | ✅ | (2) theo khách/điều kiện (tự giới hạn theo chi nhánh); (3) danh sách + báo cáo |
| Nghe ghi âm cuộc gọi | ⚠️ | tùy phiên bản & quyền — một số nút nghe ghi âm/quản lý ở bản cũ đã ngừng (xem dev.md) |
| Xuất Excel lịch sử cuộc gọi | ✅ | (2) bản sim số; (3) bản IP — khi dịch vụ xuất bận có thể lỗi tạm thời, thử lại |
| Thống kê cuộc gọi | ✅ | (3) bản IP — lọc theo bộ lọc, không tự giới hạn theo từng nhân viên (xem dev.md) |

## Để làm gì / cho ai
- **Nhân viên sale/tele:** gọi khách ngay trên hệ thống, gắn cuộc gọi với contact, nghe lại ghi âm.
- **Quản lý đơn vị (DV-AD-TONG):** cấu hình tổng đài, kịch bản, user, xem báo cáo cuộc gọi.
- **Admin/Kế toán hệ thống (HT-AD/HT-KT):** khởi tạo/kết nối tổng đài, quản lý gói, duyệt nạp tiền cấp hệ thống.

## Cách dùng
**Quản trị (admin):**
1. **Khởi tạo / kết nối** tổng đài cho đơn vị (theo phiên bản đơn vị mua).
2. **Nạp tiền** vào tổng đài (bản IP: tạo lệnh → quét **QR** thanh toán; chờ duyệt nếu cần).
3. Cấu hình **đầu số/hotline**, **kịch bản cuộc gọi & bấm phím**, **lời chào**, **nhóm nội bộ** (bản IP).
4. **Map user** CRM với user tổng đài để nhân viên gọi được.

**Nhân viên:**
1. Mở màn gọi → **click-to-call** số khách (hoặc nhận **auto-call**).
2. Sau cuộc gọi: hệ thống lưu **lịch sử**; cập nhật kết quả trên contact.
3. Xem lại **lịch sử / ghi âm**, **thống kê cuộc gọi**.

## Cần chú ý
- **Xác định đúng phiên bản** (Voice IP cũ / Sim số / IP mới) trước khi hướng dẫn — màn hình & khả năng khác nhau; nhầm phiên bản là nguyên nhân hiểu lầm phổ biến.
- **Bản đăng ký tổng đài gốc và lịch sử cuộc gọi sim số đời đầu đã khóa** — không dùng nữa; thay bằng các bản mới.
- **Nạp tiền tổng đài IP** đi qua **mã QR + có thể cần quản trị hệ thống duyệt**; tiền chỉ vào sau khi giao dịch ở trạng thái đã thanh toán/được duyệt.
- **Upload lời chào chỉ chấp nhận file .mp3**; file rỗng bị từ chối.
- **Một số phân quyền bị nới lỏng** (auto-call, tạo lịch sử cuộc gọi, vài thao tác hotline chỉ cần đăng nhập) — xem dev.md; không khẳng định quyền chặt với khách.
- **Thống kê / lịch sử cuộc gọi** ở bản IP **không tự giới hạn theo từng nhân viên** — kết quả phụ thuộc bộ lọc bạn chọn (đặt bộ lọc đúng phạm vi muốn xem).

## Ảnh hưởng tới
- **Contact** (auto-call chia/cập nhật contact cho sale; cuộc gọi gắn contact).
- **Telesales** (đội tele gọi qua tổng đài).
- **Báo cáo Sales / tối ưu sale** (số liệu cuộc gọi MyHub/Omicall enrich báo cáo tối ưu sale).
- **Đối tác tổng đài (Omicall/MyHub)** + **cổng QR** + **phê duyệt giao dịch SA**.
