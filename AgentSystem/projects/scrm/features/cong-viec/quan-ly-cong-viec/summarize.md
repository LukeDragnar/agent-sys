---
alias: ["quản lý công việc", "công việc", "nhiệm vụ", "task", "giao việc", "theo dõi công việc", "công việc cha con"]
module: cong-viec
trang_thai: "Core — cần bật tiện ích (KiemTraTienIch) ở các màn quản lý; màn báo cáo công việc chỉ cần đăng nhập"
goi: "cần product xác nhận — các màn quản lý công việc/quy trình gate qua KiemTraTienIch; báo cáo công việc không gate tiện ích"
quyen: "cần product xác nhận — tìm công việc (TimTheoDieuKien) mở cho đa role (AD/KT/QLDV/SALE/MKT/KHO...); xem/sửa/lưu/xóa công việc chỉ yêu cầu đăng nhập (SBAuthorize), scope theo đơn vị; xem lưu ý phân quyền trong dev.md"
phu_thuoc: ["Cấu hình công việc (mẫu công việc, field động, độ sâu cấp con)", "Quy trình & lượt thực thi (công việc có thể sinh tự động từ quy trình)", "Danh mục trạng thái / mức độ ưu tiên", "Cơ cấu tổ chức (người thực hiện / người phê duyệt)"]
link_flow: ./flow.md
---

# Quản lý công việc

## Là gì
Đây là nơi **tạo, giao và theo dõi công việc (task)** của shop dưới dạng **cây cha–con**: một công việc lớn có thể chia thành nhiều công việc con, mỗi việc có người thực hiện, người phê duyệt, trạng thái, mức độ ưu tiên, ngày dự kiến, ước lượng thời gian (estimate) và các trường thông tin mở rộng tùy đơn vị.

Màn hình chính là **lưới công việc** với nhiều cách nhìn (dạng danh sách, timeline/Gantt, lịch), cho phép **sửa nhanh từng ô ngay trên lưới** (inline edit), mở popup chi tiết kèm lịch sử thay đổi, **tự động sắp xếp thời gian** cho cả cây, và **nhập công việc hàng loạt từ Excel**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm / xem danh sách công việc (cây, lọc, phân trang) | ✅ | đa role; quyền `CongViec_TimTheoDieuKien` |
| Xem chi tiết 1 công việc + lịch sử | ✅ | đăng nhập, scope theo đơn vị |
| Thêm công việc (cha hoặc con) | ✅ | đăng nhập; bị chặn nếu vượt số cấp con cho phép |
| Sửa công việc | ✅ | đăng nhập |
| Sửa nhanh 1 ô trên lưới (inline) | ✅ | đăng nhập |
| Xóa công việc | ⚠️ | xóa **mềm** — kéo theo cả công việc con và lượt thực thi liên quan; khôi phục được ở Lịch sử xóa |
| Tự động sắp xếp thời gian cây công việc | ✅ | chỉ tính trước (preview), phải xác nhận mới ghi |
| Nhập công việc từ Excel | ✅ | tối đa 1.000 dòng/lần; có chế độ kiểm thử (validate không ghi) |
| Tải file mẫu Excel (theo mẫu/đơn vị) | ✅ | đăng nhập |
| Xem báo cáo công việc (tổng quan / theo người / theo ưu tiên) | ✅ | đăng nhập (màn báo cáo không gate tiện ích) |

## Để làm gì / cho ai
- **Quản lý / trưởng nhóm:** giao việc, chia nhỏ công việc thành cây cha–con, theo dõi tiến độ, ai trễ hạn.
- **Nhân viên:** xem việc được giao, cập nhật trạng thái, ghi chú, đánh dấu hoàn thành.
- **Người phê duyệt:** theo dõi công việc cần duyệt.
- **Chủ shop:** xem báo cáo công việc — tổng số, quá hạn, phân bổ theo người và theo mức độ ưu tiên.

## Cách dùng
**Xem & tìm công việc:**
1. Vào **Công việc → Danh sách công việc**.
2. Dùng các tab lọc / bộ điều kiện (theo trạng thái, người thực hiện, người phê duyệt, trường mở rộng…) rồi xem.
3. Lưới hiện dạng cây cha–con; có thể chuyển sang xem timeline/lịch, phân trang, group theo cột.

**Tạo & cập nhật:**
1. Bấm **Thêm mới** → điền tiêu đề, mô tả, người thực hiện/phê duyệt, trạng thái, ưu tiên, ngày dự kiến, estimate, mẫu công việc (nếu có).
2. Lưu. Để tạo công việc con: thêm dưới một công việc cha (hệ thống tự tính cấp).
3. Trên lưới có thể **sửa nhanh từng ô** rồi lưu ngay (inline edit).

**Tự động sắp xếp thời gian:**
1. Bấm **Tự động sắp xếp** → hệ thống tính ngày mới cho cả cây và hiển thị thử.
2. Xem trước, nếu đồng ý thì **xác nhận** để áp.

**Nhập Excel:**
1. Tải file mẫu (chọn mẫu công việc nếu muốn cột theo mẫu) → điền dữ liệu.
2. Tải lên (có thể bật kiểm thử để chỉ kiểm tra). Hệ thống báo lỗi theo dòng.

## Cần chú ý
- **Xóa công việc là xóa mềm và lan rộng**: xóa một công việc cha sẽ kéo theo toàn bộ công việc con và các lượt thực thi quy trình gắn vào đó. Có thể khôi phục ở màn **Lịch sử xóa**, nhưng phải cẩn thận khi xóa việc cha.
- **Giới hạn số cấp con**: hệ thống chỉ cho tạo tối đa một số cấp công việc con nhất định (mặc định 3 cấp con). Vượt quá sẽ báo lỗi. Số cấp này đặt ở Cấu hình công việc.
- **Nhập Excel tối đa 1.000 dòng/lần** — nhiều hơn phải chia file.
- **Hai người sửa cùng một ô cùng lúc**: hiện hệ thống ghi đè theo lần lưu cuối, **không cảnh báo đụng độ** (xem dev.md). Cần dặn nhân viên tránh sửa trùng.
- **Phạm vi xem công việc** có thể khác nhau tùy cấu hình tìm kiếm của đơn vị (hệ thống dùng tìm kiếm nhanh hoặc truy vấn trực tiếp) — một số trường hợp người dùng thấy nhiều/ít hơn mong đợi (xem dev.md).
- **Lịch sử thay đổi** của công việc lưu riêng (audit) — mỗi lần sửa được ghi lại để tra cứu.

## Ảnh hưởng tới
- **Cấu hình công việc**: mẫu / field động / số cấp con quyết định cách tạo và sửa công việc.
- **Quy trình & lượt thực thi**: công việc có thể được sinh tự động khi khởi chạy quy trình; xóa lượt thực thi cũng kéo theo công việc.
- **Lịch sử xóa**: công việc xóa mềm nằm ở đây để khôi phục.
- **Tìm kiếm nhanh**: mọi thay đổi công việc được đồng bộ lại để tìm nhanh.
