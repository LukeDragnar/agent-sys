---
alias: ["báo cáo sale", "báo cáo lead", "công việc sale", "tối ưu sale", "báo cáo KPI sale", "hiệu suất sale", "báo cáo bán hàng cá nhân"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — báo cáo public ở mức controller (key BaoCaoOnlineController_ReportCongViecSale, ..._ReportToiUuSale). Phạm vi xem theo chi nhánh + cơ cấu tổ chức Sale."
phu_thuoc: ["Cơ cấu tổ chức (nhóm / nhân viên Sale)", "Contact & đơn hàng (nguồn công việc/kết quả sale)", "Tổng đài (MyHub / OmiCall — để enrich báo cáo tối ưu sale)", "Cấu hình đơn vị (giới hạn khoảng ngày, ép chi nhánh)"]
link_flow: ./flow.md
---

# Báo cáo sale & lead

## Là gì
Đây là **bộ báo cáo đánh giá hoạt động của đội Sale** dựa trên công việc và kết quả bán/chăm sóc lead. Gồm:
- **Báo cáo công việc sale** — khối lượng và kết quả công việc của từng nhân viên Sale (gọi, chăm sóc, chốt đơn…).
- **Báo cáo tối ưu sale** — phân tích để tối ưu hiệu suất Sale, có ghép thêm dữ liệu cuộc gọi tổng đài (MyHub, OmiCall).

Mỗi báo cáo là một **bảng (lưới)** lọc theo chi nhánh + khoảng ngày + cơ cấu tổ chức Sale; phần lớn có nút **Xuất Excel**.

Lưu ý: việc **chia lead/chia số** cho sale (cấu hình phân bổ) thuộc module Sales; tại đây chỉ là **báo cáo kết quả**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo công việc sale | ✅ | tùy quyền; phạm vi chi nhánh + cơ cấu tổ chức |
| Xem báo cáo tối ưu sale | ✅ | tùy quyền; có ghép dữ liệu cuộc gọi tổng đài |
| Lọc theo chi nhánh + khoảng ngày + nhóm/nhân viên | ✅ | bị giới hạn ngày nếu đơn vị bật |
| Xuất Excel | ✅ | công việc sale & tối ưu sale có nút Xuất Excel |
| **Sửa / Xóa số liệu** | ❌ | báo cáo **chỉ đọc** |
| Xem sale của chi nhánh / nhóm khác | ⚠️ | tùy phân quyền và cơ cấu tổ chức |

## Để làm gì / cho ai
- **Quản lý Sale / trưởng nhóm:** theo dõi khối lượng công việc và hiệu suất từng nhân viên, tìm điểm nghẽn để tối ưu.
- **Chủ shop:** đánh giá hiệu quả đội Sale, kết hợp với báo cáo cuộc gọi tổng đài.
- **Nhân viên Sale:** xem kết quả công việc của mình (tùy quyền).

## Cách dùng
1. Vào module **Báo cáo** → chọn Báo cáo công việc sale hoặc Báo cáo tối ưu sale.
2. Chọn **chi nhánh**, **khoảng ngày Từ – Đến**, lọc **cơ cấu tổ chức** (nhóm/nhân viên Sale).
3. Bấm **Xem / Tìm** → bảng số liệu hiện ra.
4. Bấm **Xuất Excel** nếu cần tải file.

## Cần chú ý
- **Báo cáo chỉ đọc**, không sửa số liệu công việc/kết quả từ đây.
- **Tối ưu sale ghép dữ liệu tổng đài:** nếu đơn vị dùng tổng đài MyHub/OmiCall, báo cáo bổ sung số liệu cuộc gọi; nếu không dùng tổng đài thì phần đó trống.
- **Phạm vi xem** theo chi nhánh + cơ cấu tổ chức Sale; nhân viên thường thấy phạm vi hẹp.
- **Giới hạn khoảng ngày** áp dụng như mọi báo cáo.

## Ảnh hưởng tới
- **Cơ cấu tổ chức (nhóm/nhân viên Sale)** — quyết định ai vào báo cáo và ai được xem.
- **Contact & đơn hàng** — nguồn dữ liệu công việc và kết quả sale.
- **Tổng đài (MyHub/OmiCall)** — nguồn dữ liệu cuộc gọi ghép vào báo cáo tối ưu sale.
- **Báo cáo bậc thang & KPI** — thành tích sale ở đây liên quan tới bảng xếp hạng Sale.
