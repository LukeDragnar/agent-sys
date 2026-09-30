---
alias: ["contact", "lead", "danh sách contact", "data sale", "data MKT", "quản lý contact", "khách tiềm năng", "blacklist số điện thoại", "black list"]
module: sales
trang_thai: Core
goi: cần product xác nhận
quyen: "cần product xác nhận — phần lớn endpoint chỉ gắn `[SBAuthorize]` (chỉ cần đăng nhập), một số siết riêng: xóa nhiều contact chỉ admin tổng đơn vị (DV-AD-TONG); danh sách blacklist chỉ admin chi nhánh/multi/tổng (DV-AD-CN/MCN/TONG). Phần lớn thao tác blacklist khác chỉ yêu cầu đăng nhập."
phu_thuoc: ["Kho số & Chia số (chia data xoay vòng cho sale — nút Nhận data)", "Đơn hàng logistic (tạo đơn từ contact, đồng bộ trạng thái đơn ↔ contact, lịch sử mua)", "Lịch hẹn / Booking (đặt lịch từ contact, duyệt lịch)", "Danh mục tác nghiệp (kết quả tác nghiệp, nhãn khách hàng)", "Khách hàng (gán Sale/MKT phụ trách, tạo KH khi đơn TMĐT về)"]
link_flow: ./flow.md
---

# Quản lý Contact

## Là gì
Đây là nơi **quản lý danh sách liên hệ (contact / lead)** mà đội Sale và Marketing đang chăm sóc: thông tin khách tiềm năng, kết quả từng lần gọi/chăm (tác nghiệp), nhãn phân loại, ghi chú nội bộ, lịch hẹn, và phát hiện trùng số / khách cũ. Mỗi contact là một "đầu mối" để sale theo đuổi cho tới khi lên đơn.

Kèm theo đó là **Black list (số điện thoại đen)** — danh sách những số bị chặn (khách bom hàng, số rác, khách yêu cầu không liên hệ…), để hệ thống cảnh báo khi gặp lại số đó lúc tạo contact hoặc lên đơn.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm contact (thủ công) | ✅ | tùy quyền (sale tự thêm; MKT thêm từ hồ sơ landing) |
| Sửa contact | ✅ | tùy quyền |
| Xóa 1 contact | ✅ | tùy quyền |
| **Xóa nhiều contact (hàng loạt)** | ⚠️ | chỉ **admin tổng đơn vị (DV-AD-TONG)** |
| Nhận data (bắt đầu chăm) | ✅ | contact đang ở dạng chia xoay vòng, chưa ai nhận |
| Cập nhật kết quả tác nghiệp | ✅ | tùy quyền |
| Gắn nhãn khách hàng (đơn / hàng loạt) | ✅ | tùy quyền |
| Chuyển tác nghiệp / tác nghiệp tiếp | ✅ | tùy quyền |
| Phân bổ lại data cho sale / nhóm / MKT | ✅ | tùy quyền |
| Thu hồi sale khỏi contact theo bộ lọc | ✅ | tùy quyền |
| Đặt lịch hẹn từ contact | ✅ | tùy quyền (xem feature Lịch hẹn gặp khách) |
| Duyệt / hủy duyệt lịch (leader) | ✅ | sale leader; chỉ với lịch đang "Chờ duyệt" |
| Tạo đơn hàng từ contact / khách cũ | ✅ | tùy quyền |
| Import contact từ Excel | ✅ | chỉ cần đăng nhập (dữ liệu bắt đầu từ dòng 7 theo file mẫu) |
| Ghi chú / lưu link nhóm Zalo | ✅ | tùy quyền |
| Xem trùng số / khách cũ / trùng sản phẩm | ✅ | tùy quyền |
| Thêm / sửa / xóa số trong Black list | ✅ | tùy quyền (phần lớn chỉ cần đăng nhập) |
| Xem danh sách Black list | ⚠️ | chỉ **admin chi nhánh / multi / tổng** (DV-AD-CN/MCN/TONG) |

## Để làm gì / cho ai
- **Nhân viên Sale:** nhận data, gọi/chăm khách, ghi kết quả từng lần liên hệ, đặt lịch, lên đơn. Đây là màn hình làm việc hằng ngày của sale.
- **Marketing:** tạo / sửa contact từ hồ sơ khách landing, phân loại bằng nhãn.
- **Sale leader / quản lý:** phân bổ data cho nhân viên, thu hồi data, duyệt lịch hẹn, theo dõi hiệu suất.
- **Admin / CSKH:** quản lý black list để chặn số rác / khách bom hàng; xử lý contact trùng.

## Cách dùng
**Chăm contact (đội Sale):**
1. Vào **Sale → Danh sách contact**.
2. Lọc / tìm contact theo từ khóa, nhãn, hoặc cuộc hội thoại.
3. Bấm **Nhận data** (nếu contact đang được chia xoay vòng và đang ẩn thông tin) → hệ thống mở thông tin liên hệ và bắt đầu tính thời gian chăm sóc.
4. Mở chi tiết contact, gọi khách, sau đó **cập nhật kết quả tác nghiệp**, **gắn nhãn**, **ghi chú**.
5. Cần theo tiếp → **Chuyển tác nghiệp** / **Tác nghiệp tiếp**; muốn hẹn khách → **Đặt lịch**; chốt được → **Tạo đơn hàng**.

**Phân bổ & thu hồi data (leader):**
1. Vào danh sách contact, chọn các contact (hoặc dùng bộ lọc).
2. Bấm **Phân bổ** để gán cho sale / nhóm sale / MKT; hoặc **Thu hồi sale** để gỡ sale khỏi các contact khớp bộ lọc.

**Quản lý Black list:**
1. Vào **Sale → Danh sách black list** (cần quyền admin chi nhánh trở lên để xem danh sách).
2. **Thêm** số cần chặn (kèm lý do), hoặc thêm trực tiếp từ popup chi tiết contact.
3. Khi tạo contact / lên đơn gặp số trong black list → hệ thống cảnh báo.

## Cần chú ý
- **"Nhận data" mở thông tin bị ẩn:** với cơ chế chia số xoay vòng, contact ban đầu **ẩn** số điện thoại / thông tin. Sale phải bấm **Nhận data** mới thấy đầy đủ và mới bắt đầu được tính thời gian chăm sóc.
- **Xóa nhiều contact chỉ admin tổng làm được** — đây là thao tác phá dữ liệu hàng loạt nên bị siết quyền. Xóa từng contact thì nhẹ hơn.
- **Trùng số / khách cũ:** trước khi lên đơn, nên kiểm tra tab Trùng số và Khách cũ để tránh tạo trùng và để biết khách đã từng mua gì.
- **Duyệt lịch:** nếu đơn vị bật yêu cầu duyệt, lịch sale đặt sẽ vào trạng thái **Chờ duyệt**, leader phải duyệt thì lịch mới được tạo thật.
- **Black list là dữ liệu nhạy cảm nhưng phân quyền khá lỏng** ở phần thêm/sửa/xóa (chỉ cần đăng nhập) — *cần product xác nhận* đây có phải chủ ý không (đã ghi vào dev.md).
- Một số nút (Nhận data, import, một vài thao tác trùng số) **chỉ yêu cầu đăng nhập**, không gắn quyền chi tiết — *cần product xác nhận*.

## Ảnh hưởng tới
- **Đơn hàng logistic:** tạo đơn từ contact; ngược lại đơn đổi/xóa trạng thái sẽ đồng bộ ngược về contact (cờ đơn, nhãn KH).
- **Lịch hẹn / Booking:** đặt lịch từ contact và đồng bộ trạng thái lịch ↔ contact.
- **Khách hàng (Customer):** khi contact lên đơn / khi đơn TMĐT về, hệ thống tạo hoặc gán khách hàng tương ứng.
- **Kho số / Chia số:** data sale lấy từ kho số, chia xoay vòng cho sale qua nút Nhận data.
- **Black list ⇄ Đơn hàng:** lúc tạo đơn, hệ thống kiểm tra số có trong black list không để cảnh báo.
