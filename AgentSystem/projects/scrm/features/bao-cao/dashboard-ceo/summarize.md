---
alias: ["dashboard ceo", "dashboard tổng quan", "báo cáo ceo", "báo cáo lãnh đạo", "báo cáo tổng quan", "report ceo", "bảng điều khiển ceo"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — endpoint public theo khóa quyền tính năng (`BaoCaoCeoController_*`), nhưng vai trò đặc trưng để xem Dashboard CEO gồm `DV-VIEW-REPORT-CEO` cùng `DV-AD-TONG` / `HT-AD` / `HT-CSKH`. Hệ thống áp giới hạn khoảng ngày như các báo cáo khác."
phu_thuoc: ["Báo cáo online (phễu online, bảng xếp hạng, giao vận, công việc sale)", "Báo cáo offline cửa hàng (doanh thu chi nhánh/SP, lịch hẹn, tỉ lệ lấp đầy, chân dung KH)", "Chấm công (báo cáo chấm công tổng quát/top)", "Tổng đài (báo cáo cuộc gọi)", "Chia lead, nhãn/trạng thái contact theo nguồn", "Cấu hình giới hạn khoảng ngày được phép tra báo cáo"]
link_flow: ./flow.md
---

# Dashboard tổng quan / CEO

## Là gì
**Bảng điều khiển toàn cảnh kinh doanh dành cho lãnh đạo (CEO/chủ chuỗi).** Gom nhiều nhóm báo cáo vào một màn hình: doanh thu offline (chi nhánh/cửa hàng/sản phẩm, lịch hẹn, tỉ lệ lấp đầy, chân dung khách), báo cáo online (phễu online, số liệu chung, bảng xếp hạng sale/MKT, giao vận, công việc sale), chấm công, tổng đài, doanh số theo nguồn/ngày/sản phẩm, chia lead, và nhãn/trạng thái contact.

Mục tiêu: lãnh đạo nhìn **một chỗ** ra toàn bộ sức khỏe kinh doanh, không phải mở từng báo cáo lẻ. Báo cáo chỉ đọc, không thay đổi dữ liệu.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Mở Dashboard CEO (load toàn bộ widget 1 lần) | ✅ | có vai trò xem Dashboard CEO (vd `DV-VIEW-REPORT-CEO` / admin tổng) |
| Xem từng widget riêng (doanh thu, phễu, bảng xếp hạng, chấm công...) | ✅ | như trên |
| Lọc theo chi nhánh, khoảng ngày, cơ cấu tổ chức | ✅ | trong giới hạn khoảng ngày của đơn vị |
| Xuất Excel | ⚠️ | cần product xác nhận — registry node không liệt kê endpoint Export riêng cho dashboard CEO |
| Thêm / Sửa / Xóa dữ liệu | ❌ | báo cáo chỉ đọc — không tạo/sửa/xóa gì |

## Để làm gì / cho ai
- **CEO / chủ chuỗi / lãnh đạo:** nắm toàn cảnh doanh thu, lead, chấm công, tổng đài, hiệu suất sale/MKT trong một màn.
- **Quản lý cấp cao:** so sánh chi nhánh, theo dõi xu hướng, ra quyết định nhanh.

## Cách dùng
1. Vào **Báo cáo → Dashboard CEO** (cần vai trò xem báo cáo CEO).
2. Chọn **chi nhánh**, **Từ ngày / Đến ngày**, và cơ cấu tổ chức nếu cần.
3. Dashboard tải các widget (có thể tải tất cả một lần hoặc từng phần tùy màn).
4. Xem các nhóm: doanh thu offline, online, chấm công, tổng đài, doanh số, lead, contact.

## Cần chú ý
- **Cần vai trò riêng để xem:** Dashboard CEO không dành cho mọi nhân viên — thường giới hạn vai trò lãnh đạo/admin (`DV-VIEW-REPORT-CEO`, admin tổng...). Nếu không thấy menu hoặc bị chặn là do chưa được cấp quyền.
- **Khoảng ngày bị giới hạn** như các báo cáo khác — chọn rộng quá có thể bị chặn.
- **Dashboard là báo cáo nặng:** tải toàn bộ widget một lần sẽ gọi nhiều truy vấn → có thể chậm khi dữ liệu lớn hoặc khoảng ngày rộng. Nên thu hẹp khoảng ngày khi cần nhanh.
- **Nhiều widget trùng nội dung với báo cáo lẻ** (bảng xếp hạng, giao vận, doanh số tổng quan dùng chung nguồn với Báo cáo online). Số liệu nhất quán nếu cùng bộ lọc.

## Ảnh hưởng tới
- Không ghi/đổi dữ liệu (read-only) — chỉ tổng hợp đọc từ nhiều nguồn báo cáo.
- Dùng chung nguồn với Báo cáo online, Báo cáo cửa hàng, chấm công, tổng đài.
