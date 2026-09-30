---
alias: ["báo cáo bậc thang", "báo cáo KPI", "bảng xếp hạng", "BXH", "phong thần bảng", "xếp hạng nhân viên", "xếp hạng sale", "xếp hạng marketing", "xếp hạng nhóm sale"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — báo cáo public ở mức controller (key BaoCaoOnlineController_ReportBangXepHangSale, ..._ReportBangXepHangMarketing, ..._ReportPhongThanBangSale/MKT/NhomSale). Phạm vi xem theo chi nhánh + cơ cấu tổ chức."
phu_thuoc: ["Cơ cấu tổ chức (phòng ban / nhóm / nhân viên Sale-MKT)", "Đơn hàng & contact (nguồn tính thành tích)", "Cấu hình đơn vị (giới hạn khoảng ngày, ép chi nhánh)"]
link_flow: ./flow.md
---

# Báo cáo bậc thang & KPI

## Là gì
Đây là **bộ báo cáo xếp hạng/thi đua** giữa nhân viên, nhóm và phòng ban dựa trên thành tích bán hàng/marketing. Gồm:
- **Bảng xếp hạng (BXH) Sale / Marketing** — xếp hạng nhân viên Sale hoặc MKT theo chỉ số.
- **Phong thần bảng (Sale / MKT / Nhóm sale)** — bảng vinh danh/thi đua theo nhân viên hoặc nhóm.

Các báo cáo này phân biệt **loại nhân viên Sale (1) và MKT (3)** để xếp đúng nhóm. Hiển thị dưới dạng **bảng (lưới)** có thể sắp xếp theo cột chỉ số, lọc theo chi nhánh + khoảng ngày + cơ cấu tổ chức.

Lưu ý: "bậc thang" ở đây hiểu theo nghĩa **xếp hạng/thi đua**. Cấu hình hoa hồng bậc thang (tính lương) thuộc module Nhân sự, không nằm trong nhóm báo cáo này.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem bảng xếp hạng Sale / MKT | ✅ | tùy quyền; phạm vi chi nhánh + cơ cấu tổ chức |
| Xem phong thần bảng (Sale / MKT / Nhóm sale) | ✅ | tùy quyền |
| Sắp xếp theo cột chỉ số | ✅ | sắp xếp tại ứng dụng theo TruongSapXep/KieuSapXep |
| Lọc theo chi nhánh + khoảng ngày + nhóm/nhân viên | ✅ | bị giới hạn ngày nếu đơn vị bật |
| **Xuất Excel** | ⚠️ | cần product xác nhận — registry không ghi export riêng cho BXH/phong thần bảng |
| **Sửa / Xóa số liệu** | ❌ | báo cáo **chỉ đọc** |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** tổ chức thi đua, nhìn nhanh ai/nhóm nào đang dẫn đầu, ai cần hỗ trợ.
- **Trưởng nhóm Sale/MKT:** theo dõi thành tích nhóm mình, so sánh với nhóm khác.
- **Nhân viên:** xem vị trí của mình trên bảng xếp hạng (tùy quyền).

## Cách dùng
1. Vào module **Báo cáo** → chọn Bảng xếp hạng Sale / Bảng xếp hạng MKT / Phong thần bảng.
2. Chọn **chi nhánh**, **khoảng ngày Từ – Đến**, và lọc **cơ cấu tổ chức** (phòng ban/nhóm/nhân viên) nếu cần.
3. Bấm **Xem / Tìm** → bảng xếp hạng hiện ra.
4. Bấm vào tiêu đề cột để **sắp xếp** theo chỉ số mong muốn.

## Cần chú ý
- **Báo cáo chỉ đọc**, không sửa số liệu thành tích từ đây.
- **Loại nhân viên cố định theo báo cáo:** BXH Sale ép loại = 1, BXH MKT ép loại = 3 — hai màn dùng chung một nguồn nhưng lọc khác nhóm nhân viên.
- **Phạm vi xem:** giới hạn theo chi nhánh + cơ cấu tổ chức user được phép xem; nhân viên thường thấy phạm vi hẹp hơn quản lý.
- **Giới hạn khoảng ngày** áp dụng như mọi báo cáo.

## Ảnh hưởng tới
- **Cơ cấu tổ chức (phòng ban/nhóm/nhân viên)** — quyết định ai được xếp hạng và ai được xem.
- **Đơn hàng & contact** — nguồn tính thành tích đưa vào xếp hạng.
- **Dashboard CEO** — cũng có bảng xếp hạng Sale/MKT phiên bản tổng hợp cho lãnh đạo (SP riêng `..._Online_BangXepHangSale/Mkt`).
- **Thưởng phạt & hoa hồng (Nhân sự)** — KPI/thành tích ở đây có thể tham chiếu khi tính thưởng, nhưng tính lương/hoa hồng nằm ở module khác.
