---
alias: ["báo cáo marketing", "báo cáo MKT", "công việc marketing", "công việc MKT", "báo cáo campaign", "hiệu suất marketing", "MKT theo nguồn landing"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — báo cáo public ở mức controller (key BaoCaoOnlineController_ReportCongViecMktViewTacNghiep, ..._ReportCongViecMktViewSale, ..._ReportMktTheoNguonLanding). Phạm vi xem theo chi nhánh + cơ cấu tổ chức MKT."
phu_thuoc: ["Cơ cấu tổ chức (nhóm / nhân viên Marketing)", "Lead & contact (nguồn công việc MKT)", "Nguồn / kênh landing (để phân tích theo nguồn)", "Cấu hình đơn vị (giới hạn khoảng ngày, ép chi nhánh)"]
link_flow: ./flow.md
---

# Báo cáo marketing

## Là gì
Đây là **bộ báo cáo đánh giá hoạt động của đội Marketing** dựa trên công việc và kết quả thu hút/chăm sóc lead. Gồm:
- **Báo cáo công việc MKT (view tác nghiệp)** — khối lượng công việc marketing theo góc nhìn tác nghiệp.
- **Báo cáo công việc MKT (view sale)** — kết quả MKT nhìn theo góc độ chuyển giao cho Sale.
- **MKT theo nguồn landing** (doanh số MKT cho quản lý) — kết quả marketing theo từng nguồn/kênh landing.

Mỗi báo cáo là một **bảng (lưới)** lọc theo chi nhánh + khoảng ngày + cơ cấu tổ chức MKT; phần lớn có nút **Xuất Excel**.

Lưu ý: bộ báo cáo này **trùng** một phần với Báo cáo Marketing trong chính module Marketing (theo SP/kênh/nguồn) — xem "Ảnh hưởng tới".

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem công việc MKT (view tác nghiệp) | ✅ | tùy quyền; phạm vi chi nhánh + cơ cấu tổ chức |
| Xem công việc MKT (view sale) | ✅ | tùy quyền |
| Xem MKT theo nguồn landing | ✅ | tùy quyền |
| Lọc theo chi nhánh + khoảng ngày + nhóm/nhân viên | ✅ | bị giới hạn ngày nếu đơn vị bật |
| Xuất Excel | ✅ | công việc MKT (2 view) có nút Xuất Excel |
| **Sửa / Xóa số liệu** | ❌ | báo cáo **chỉ đọc** |
| Xem MKT của chi nhánh / nhóm khác | ⚠️ | tùy phân quyền và cơ cấu tổ chức |

## Để làm gì / cho ai
- **Quản lý Marketing:** theo dõi khối lượng và hiệu quả công việc của đội MKT, đánh giá nguồn/kênh nào ra lead tốt.
- **Chủ shop:** đo hiệu quả marketing, kết hợp với báo cáo lead và doanh số.
- **Nhân viên MKT:** xem kết quả công việc của mình (tùy quyền).

## Cách dùng
1. Vào module **Báo cáo** → chọn Báo cáo công việc MKT (tác nghiệp hoặc view sale) hoặc MKT theo nguồn landing.
2. Chọn **chi nhánh**, **khoảng ngày Từ – Đến**, lọc **cơ cấu tổ chức** (nhóm/nhân viên MKT).
3. Bấm **Xem / Tìm** → bảng số liệu hiện ra.
4. Bấm **Xuất Excel** nếu cần tải file.

## Cần chú ý
- **Báo cáo chỉ đọc**, không sửa số liệu công việc/kết quả từ đây.
- **Hai view công việc MKT** (tác nghiệp / view sale) nhìn cùng dữ liệu MKT theo hai góc độ khác nhau — đừng nhầm là hai chỉ số tách biệt.
- **Phạm vi xem** theo chi nhánh + cơ cấu tổ chức MKT (loại nhân viên = 3); nhân viên thường thấy phạm vi hẹp.
- **Giới hạn khoảng ngày** áp dụng như mọi báo cáo.

## Ảnh hưởng tới
- **Cơ cấu tổ chức (nhóm/nhân viên MKT)** — quyết định ai vào báo cáo và ai được xem.
- **Lead & contact** — nguồn dữ liệu công việc và kết quả MKT.
- **Nguồn / kênh landing** — cơ sở phân tích MKT theo nguồn.
- **Báo cáo Marketing (module Marketing)** — báo cáo MKT theo sản phẩm/kênh nằm ở module Marketing, bổ trợ cho nhóm này.
- **Báo cáo bậc thang & KPI** — thành tích MKT ở đây liên quan tới bảng xếp hạng MKT.
