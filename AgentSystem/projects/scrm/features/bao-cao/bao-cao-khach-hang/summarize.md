---
alias: ["báo cáo khách hàng", "báo cáo KH", "khách hàng cũ mới", "báo cáo theo khách hàng", "số liệu khách hàng theo tháng", "khách quay lại", "chân dung khách hàng"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — báo cáo public ở mức controller (key BaoCaoOnlineController_ReportTheoKhachHang, ..._ReportKhachHangCuMoi, ..._ReportSoLieuHienTaiTheoThang, ..._ReportSoLieuQuayLaiTheoThang). Phạm vi dữ liệu bị giới hạn theo chi nhánh và cơ cấu tổ chức của user."
phu_thuoc: ["Khách hàng (nguồn dữ liệu khách)", "Đơn hàng (để tính khách mua/quay lại)", "Cấu hình đơn vị (giới hạn khoảng ngày, ép chi nhánh)", "Cơ cấu tổ chức (lọc theo Sale/MKT)"]
link_flow: ./flow.md
---

# Báo cáo khách hàng

## Là gì
Đây là **bộ báo cáo phân tích khách hàng**: hành vi mua, phân khúc, độ trung thành. Gồm các báo cáo chính:
- **Báo cáo theo khách hàng** — số liệu bán hàng/chăm sóc gắn với từng khách.
- **Khách hàng cũ / mới** — đếm và so sánh khách mới phát sinh với khách cũ trong kỳ.
- **Số liệu khách hàng hiện tại theo tháng** và **quay lại theo tháng** — theo dõi lượng khách mua lần đầu và khách quay lại mua tiếp theo từng tháng.

Mỗi báo cáo là một **bảng (lưới)** lọc theo chi nhánh + khoảng ngày, phần lớn có nút **Xuất Excel**.

Lưu ý: báo cáo này **trùng** một phần với phân tích khách hàng trong module Khách hàng (chân dung KH) — xem "Ảnh hưởng tới".

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo theo khách hàng | ✅ | tùy quyền; trong phạm vi chi nhánh được phép |
| Xem khách hàng cũ / mới | ✅ | tùy quyền |
| Xem số liệu hiện tại / quay lại theo tháng | ✅ | tùy quyền |
| Lọc theo chi nhánh + khoảng ngày | ✅ | bị giới hạn khoảng ngày nếu đơn vị bật |
| Xuất Excel | ✅ | hầu hết báo cáo KH có nút Xuất Excel |
| **Sửa / Xóa số liệu** | ❌ | báo cáo **chỉ đọc** |
| Xem khách của chi nhánh / nhân viên khác | ⚠️ | tùy phân quyền và cấu hình cơ cấu tổ chức |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** biết kỳ này có bao nhiêu khách mới, bao nhiêu khách quay lại; đánh giá khả năng giữ chân khách.
- **Marketing:** đo hiệu quả thu hút khách mới và tỷ lệ khách quay lại theo tháng.
- **Sale / CSKH:** xem báo cáo theo từng khách để chăm sóc đúng đối tượng.

## Cách dùng
1. Vào module **Báo cáo** → chọn báo cáo khách hàng (Theo khách hàng, Khách hàng cũ/mới, Số liệu hiện tại theo tháng, Số liệu quay lại theo tháng).
2. Chọn **chi nhánh** và **khoảng ngày Từ – Đến**; tùy báo cáo có thêm lọc theo cơ cấu tổ chức (nhóm/nhân viên).
3. Bấm **Xem / Tìm** → bảng số liệu hiện ra.
4. Bấm **Xuất Excel** nếu cần tải file.

## Cần chú ý
- **Báo cáo chỉ đọc**, không sửa được dữ liệu khách từ đây.
- **Giới hạn khoảng ngày:** nếu đơn vị bật, chọn khoảng quá rộng sẽ bị chặn.
- **Phạm vi xem:** nhân viên thường bị giới hạn theo chi nhánh và theo khách mình phụ trách (cơ cấu tổ chức Sale/MKT). Người được xem rộng mới thấy toàn bộ.
- **"Khách cũ / mới" và "quay lại theo tháng"** phụ thuộc cách hệ thống đánh dấu khách cũ và lịch sử mua — số liệu thay đổi theo khoảng ngày chọn.

## Ảnh hưởng tới
- **Khách hàng (hồ sơ KH, chân dung KH)** — nguồn dữ liệu gốc; báo cáo này phản ánh dữ liệu khách + đơn hàng.
- **Đơn hàng** — quyết định khách nào là "mua mới" hay "quay lại".
- **Báo cáo doanh số / Dashboard CEO** — dùng chung một số chỉ số khách hàng.
