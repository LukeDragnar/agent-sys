---
alias: ["lịch sử hoạt động", "audit log", "nhật ký hoạt động", "lịch sử tác động", "ai đã sửa", "ai đã xem", "lịch sử bộ lọc", "lịch sử tìm kiếm", "lịch sử xem thông tin"]
module: cai-dat
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — các màn lịch sử đều theo quyền nội bộ (mỗi thao tác có quyền riêng) và tự lọc theo người dùng hiện tại. Nút Xóa dòng lịch sử tác động chỉ hiện với SuperUser."
phu_thuoc: ["Toàn bộ nghiệp vụ sinh log (thêm/sửa/xóa/lọc/xuất Excel trên các màn) — đây là nơi đọc lại", "Khách hàng / Contact (cho màn Lịch sử xem thông tin)"]
link_flow: ./flow.md
---

# Lịch sử hoạt động

## Là gì
Đây là **nhật ký (audit log)** ghi lại các thao tác đã diễn ra trong hệ thống: ai đã thêm/sửa/xóa dữ liệu gì, ai đã lọc/tìm kiếm, ai đã xuất Excel, và ai đã mở xem thông tin khách hàng nào. Mục đích là tra cứu lại "chuyện gì đã xảy ra, ai làm" để giám sát, đối chiếu khi có nghi vấn.

Tính năng gồm các màn hình (trong nhóm Cài đặt hệ thống):
- **Lịch sử hoạt động (Lịch sử tác động)**: ai đã Thêm/Cập nhật/Xóa/Lọc tìm kiếm/Xuất Excel ở bảng nào, kèm chi tiết giá trị cũ → giá trị mới.
- **Lịch sử bộ lọc** và **Lịch sử tìm kiếm**: các bộ lọc / lần tìm kiếm người dùng đã thực hiện.
- **Lịch sử xem thông tin**: ai đã mở xem hồ sơ khách hàng/contact nào, kèm thống kê "top người xem".

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem / lọc lịch sử tác động (theo bảng, loại thao tác, ngày, từ khóa) | ✅ | theo quyền nội bộ; tự lọc theo người dùng hiện tại |
| Xem chi tiết thay đổi (giá trị cũ → mới) | ✅ | hiển thị ngay trên dòng (Xem thêm/Thu gọn) |
| **Xóa 1 dòng lịch sử tác động** | ⚠️ | chỉ **SuperUser** mới thấy nút Xóa |
| Xem / lọc lịch sử bộ lọc | ✅ | theo quyền nội bộ |
| Xuất Excel lịch sử bộ lọc | ✅ | tùy quyền |
| Xem / lọc lịch sử tìm kiếm | ✅ | theo quyền nội bộ |
| Xuất Excel lịch sử tìm kiếm | ✅ | tùy quyền |
| Xem lịch sử xem thông tin + thống kê top người xem | ✅ | theo quyền nội bộ |
| Xuất Excel lịch sử xem thông tin | ✅ | tùy quyền |
| **Sửa nội dung một dòng lịch sử** | ❌ | lịch sử là dữ liệu chỉ-đọc, không sửa được (chỉ xem/xóa) |

## Để làm gì / cho ai
- **Admin / chủ shop:** kiểm tra ai đã sửa/xóa dữ liệu quan trọng, đối chiếu khi có sai lệch hay khiếu nại.
- **Quản lý / giám sát:** xem nhân viên đã xuất Excel danh sách nào, đã xem thông tin khách nào (chống lạm dụng tra cứu dữ liệu).
- **CSKH / hỗ trợ:** dựng lại tình huống khi khách phản ánh thông tin bị thay đổi.

## Cách dùng
**Lịch sử hoạt động (tác động):**
1. Vào **Cài đặt → Lịch sử hoạt động**.
2. Chọn **Bảng** (đối tượng dữ liệu), **Thao tác** (Thêm mới / Cập nhật / Xóa / Lọc tìm kiếm / Xuất Excel), **khoảng ngày**, hoặc nhập **từ khóa** → bấm Tìm kiếm.
3. Mỗi dòng hiện: thao tác, đối tượng, người làm, thời điểm, và chi tiết thay đổi (giá trị cũ màu đỏ → giá trị mới màu xanh). Bấm **Xem thêm** nếu nhiều thay đổi.
4. (Chỉ SuperUser) có thể **Xóa** một dòng lịch sử.

**Lịch sử bộ lọc / tìm kiếm / xem thông tin:**
1. Vào màn tương ứng trong nhóm Cài đặt.
2. Lọc theo điều kiện, xem danh sách. Có thể **Xuất Excel**.

## Cần chú ý
- **Lịch sử là chỉ-đọc** — bạn chỉ xem (và SuperUser xóa được dòng tác động), không sửa nội dung log.
- **Mỗi người thường chỉ thấy lịch sử trong phạm vi quyền của mình** — các màn lọc theo người dùng hiện tại.
- **Nút Xóa lịch sử tác động chỉ hiện với SuperUser** — admin thường không xóa được.
- **File Excel xuất ra có thể trùng tên** giữa các màn (lịch sử bộ lọc / tìm kiếm / xem thông tin xuất ra file cùng tên) → đổi tên khi lưu để khỏi nhầm (lỗi đặt tên đã biết — xem dev.md).

## Ảnh hưởng tới
- **Không tạo/sửa dữ liệu nghiệp vụ** — chỉ phản ánh lại các thao tác đã xảy ra ở những màn khác.
- Là nguồn đối chiếu khi điều tra sai lệch dữ liệu / lạm dụng tra cứu.
