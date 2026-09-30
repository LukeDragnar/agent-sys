---
alias: ["cấu hình thông báo", "cấu hình gửi thông báo web", "thiết lập thông báo tự động", "mẫu thông báo", "ai nhận thông báo", "noti config", "cài đặt thông báo"]
module: cai-dat
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — màn Cấu hình gửi thông báo (NotificationConfig): các role HT-AD, DV-AD-TONG, HT-CSKH cho Lưu/Tải/Xóa (riêng tab Trạng thái lịch hẹn lỏng hơn, chỉ cần đăng nhập — xem dev.md). Màn Mẫu thông báo (NotiTemplate): hầu hết thao tác cần DV-AD-TONG."
phu_thuoc: ["Mẫu thông báo (NotiTemplate) — cấu hình gửi phải chọn mẫu đã khai báo sẵn", "Thiết lập tác nghiệp CSKH (để hiện danh sách Tác nghiệp cần / Kết quả / Tác nghiệp tiếp)", "Trạng thái đơn hàng / giao hàng", "Trạng thái lịch hẹn", "Hồ sơ nhân viên & phân quyền (để chọn đối tượng nhận: sale, MKT, thu ngân, admin, user cụ thể)"]
link_flow: ./flow.md
---

# Cấu hình thông báo

## Là gì
Đây là nơi shop **thiết lập hệ thống tự động gửi thông báo (trên web) khi có sự kiện xảy ra** — ví dụ khi nhân viên hoàn tất một tác nghiệp chăm sóc khách, khi đơn hàng đổi trạng thái giao hàng, hoặc khi lịch hẹn đổi trạng thái. Với mỗi sự kiện, shop chọn **gửi nội dung mẫu nào** và **gửi cho ai** (sale, trưởng nhóm/phòng sale, marketing, thu ngân, admin chi nhánh, admin tổng, hoặc một số nhân viên chỉ định).

Tính năng gồm 2 màn hình:
- **Cấu hình gửi thông báo web**: bảng "khi sự kiện X xảy ra thì gửi mẫu Y cho những ai". Có 4 kiểu cấu hình: Hành động hệ thống, Tác nghiệp, Trạng thái đơn hàng, Trạng thái lịch hẹn.
- **Mẫu thông báo**: kho các mẫu nội dung thông báo (định danh bằng Mã). Cấu hình gửi ở trên sẽ chọn mẫu từ kho này.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem / lọc cấu hình gửi theo kiểu thông báo | ✅ | tùy quyền (HT-AD, DV-AD-TONG, HT-CSKH) |
| Lưu cấu hình gửi (chọn mẫu + đối tượng nhận) | ✅ | tùy quyền |
| Khởi tạo lại (xóa toàn bộ cấu hình của một kiểu) | ✅ | tùy quyền; thao tác xóa hàng loạt, cần cẩn thận |
| Xem / lọc danh sách mẫu thông báo | ✅ | thường cần admin tổng đơn vị (DV-AD-TONG) |
| **Thêm mới mẫu thông báo** | ❌ | màn này không có nút Thêm mới — chỉ Sửa/Xóa mẫu có sẵn (mẫu mới do hệ thống cấp/seed; cần product xác nhận cách bổ sung mẫu) |
| Sửa nội dung mẫu thông báo | ✅ | tùy quyền |
| Xóa mẫu thông báo | ✅ | tùy quyền; lưu ý mẫu đang được cấu hình tham chiếu |
| Xuất Excel danh sách mẫu | ✅ | tùy quyền |
| Đặt thời điểm gửi trước / sau X phút | ✅ | với một số kiểu cấu hình (tác nghiệp tiếp) |

## Để làm gì / cho ai
- **Admin / chủ shop:** muốn nhân viên được nhắc/được báo đúng người đúng lúc khi đơn đổi trạng thái, khi khách cần chăm sóc tiếp, khi lịch hẹn thay đổi — không phải canh thủ công.
- **CSKH / quản lý vận hành (HT-CSKH):** thiết lập luồng nhắc việc tự động giữa các bước chăm sóc khách (tác nghiệp này xong → nhắc tác nghiệp tiếp theo).
- **Người quản trị đơn vị (DV-AD-TONG):** quản lý nội dung mẫu thông báo cho chuẩn, đồng bộ giọng văn.

## Cách dùng

**Thiết lập gửi thông báo (Cấu hình gửi thông báo web):**
1. Vào **Thông báo → Cấu hình gửi thông báo web**.
2. Chọn **Kiểu thông báo** muốn cấu hình: Hành động, Tác nghiệp, Trạng thái đơn hàng, hoặc Trạng thái lịch hẹn.
3. Với từng dòng sự kiện: tích chọn **Sử dụng**, chọn **Mẫu tin nhắn** (lấy từ kho mẫu), và tích các ô **gửi cho ai** (sale, trưởng nhóm/phòng sale, marketing, trưởng nhóm/phòng MKT, thu ngân, admin chi nhánh, admin tổng); có thể chọn thêm **danh sách nhân viên cụ thể**.
4. (Với kiểu Tác nghiệp) chọn thêm mẫu gửi luôn khi chọn tác nghiệp và mẫu gửi sau khi chuyển tác nghiệp tiếp, kèm số phút gửi trước/sau.
5. Bấm **Lưu**. Nếu muốn làm lại từ đầu cho một kiểu, bấm **Khởi tạo lại** (sẽ xóa toàn bộ cấu hình hiện có của kiểu đó).

**Quản lý mẫu nội dung (Mẫu thông báo):**
1. Vào **Thông báo → Mẫu thông báo**.
2. Tìm mẫu theo tên / nhóm / kiểu sử dụng.
3. Bấm **Sửa** để chỉnh nội dung mẫu, hoặc **Xóa** mẫu không dùng. Có thể **Xuất Excel** danh sách.

## Cần chú ý
- **Cấu hình gửi không tự gửi thông báo** — nó chỉ là "bảng luật": khi nghiệp vụ (chăm sóc khách / đơn hàng / lịch hẹn) chạy, hệ thống mới đọc luật này để biết gửi mẫu nào, cho ai.
- **Phải có mẫu trước.** Mẫu tin nhắn trong cấu hình gửi lấy từ màn Mẫu thông báo. Nếu mẫu chưa có thì không chọn được.
- **"Khởi tạo lại" xóa toàn bộ cấu hình của kiểu đang chọn** — cân nhắc kỹ, đây là xóa hàng loạt.
- **Xóa mẫu thông báo có thể làm "mồ côi" cấu hình** đang trỏ tới mẫu đó → thông báo có thể rỗng nội dung. Nên kiểm tra trước khi xóa.
- Màn Mẫu thông báo **không có nút Thêm mới** — chỉ sửa/xóa mẫu có sẵn (cần product xác nhận quy trình tạo mẫu mới).

## Ảnh hưởng tới
- **Luồng chăm sóc khách (tác nghiệp CSKH), đơn hàng/giao hàng, lịch hẹn** — đổi cấu hình ở đây làm đổi việc ai được báo, báo gì khi các luồng đó chạy.
- **Hộp thông báo trên web của nhân viên** — nội dung họ nhận đến từ mẫu + luật cấu hình này.
