---
alias: ["báo cáo theo nhãn", "báo cáo nhãn", "báo cáo theo nhãn contact", "báo cáo phí sàn", "phí sàn TMĐT", "phí sàn thương mại điện tử", "doanh số theo nhãn", "báo cáo theo sàn"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận. (1) Báo cáo theo nhãn (cửa hàng): public theo khóa quyền tính năng `BaoCaoCuaHangController_ReportNhan`; tự ép chi nhánh user nếu không admin. (2) Báo cáo phí sàn TMĐT: public theo khóa `BaoCaoPhiSanController_*` với danh sách vai trò DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, HT-CSKH, DV-QLDV."
phu_thuoc: ["Nhãn contact / nhãn khách hàng (phân loại để gom doanh số)", "Đơn hàng cửa hàng (nguồn doanh số theo nhãn)", "Đơn logistic sàn TMĐT (nguồn dữ liệu phí sàn — Shopee/TikTok/Lazada)", "Cấu hình giới hạn khoảng ngày được phép tra báo cáo"]
link_flow: ./flow.md
---

# Báo cáo theo nhãn / phí sàn

## Là gì
Nhóm báo cáo gồm **2 góc nhìn** (gom chung vì cùng phục vụ phân tích theo nhãn/sàn):
- **Báo cáo theo nhãn:** thống kê doanh số/contact theo **nhãn** đã gắn (nhãn contact/khách hàng) — gom theo từng nhãn để biết nhãn nào hiệu quả. Đây là một báo cáo trong nhóm **Báo cáo cửa hàng**.
- **Báo cáo phí sàn TMĐT:** thống kê **phí sàn thương mại điện tử** (phí hoa hồng/dịch vụ/thanh toán... mà sàn Shopee/TikTok/Lazada thu) trên các đơn bán qua sàn, theo chi nhánh/sàn/gian hàng/trạng thái giao hàng. Có drill-down xem chi tiết từng đơn.

Cả hai chỉ đọc, không thay đổi dữ liệu.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo theo nhãn | ✅ | đăng nhập + có quyền xem báo cáo theo nhãn; tự ép chi nhánh nếu không admin |
| Xem báo cáo phí sàn TMĐT (lưới) | ✅ | đăng nhập + vai trò admin/kế toán/QLĐV (xem frontmatter) |
| Xem chi tiết 1 đơn phí sàn / thông tin đơn gốc | ✅ | bắt buộc chọn 1 đơn (DonHangLogisticId) |
| Lọc theo chi nhánh, khoảng ngày, sàn, gian hàng, trạng thái | ✅ | trong giới hạn khoảng ngày của đơn vị |
| Thêm / Sửa / Xóa dữ liệu | ❌ | báo cáo chỉ đọc — không tạo/sửa/xóa gì |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** biết doanh số phân bổ theo nhãn nào, và sàn TMĐT đang thu bao nhiêu phí trên đơn của mình → tính lãi/lỗ thực tế khi bán qua sàn.
- **Kế toán:** đối chiếu phí sàn để hạch toán/đối soát chi phí bán hàng online.

## Cách dùng
**Báo cáo theo nhãn:**
1. Vào **Báo cáo → Báo cáo cửa hàng → Báo cáo theo nhãn**.
2. Chọn chi nhánh, Từ/Đến ngày (và bộ lọc khác nếu có).
3. Bấm xem → bảng gom doanh số/contact theo nhãn.

**Báo cáo phí sàn TMĐT:**
1. Vào **Báo cáo → Phí sàn TMĐT**.
2. Lọc chi nhánh, sàn (Shopee/TikTok/Lazada...), gian hàng, trạng thái giao hàng, Từ/Đến ngày, từ khóa.
3. Bảng phí sàn hiện ra; bấm một dòng để xem **chi tiết phí của đơn đó** và **thông tin đơn SCRM gốc**.

## Cần chú ý
- **Báo cáo phí sàn có "xếp lượt truy cập":** đây là báo cáo nặng nên hệ thống giới hạn số người xem đồng thời. Nếu quá lượt sẽ báo **"Chưa đến lượt truy cập"** (status 250) — chờ một lát rồi xem lại, không phải lỗi.
- **Khoảng ngày bị giới hạn** (cả 2 báo cáo) — chọn rộng quá có thể bị chặn "Chỉ được xem dữ liệu trong khoảng N ngày".
- **Phí sàn cần dữ liệu đồng bộ từ sàn:** số phí lấy từ đơn logistic sàn TMĐT đã đồng bộ về. Đơn chưa đồng bộ/chưa có thông tin phí sẽ không có số phí.
- **Báo cáo theo nhãn**: nhãn phải được gắn cho contact/khách trước thì mới gom được; nhãn trống → bảng không có dòng tương ứng.
- **Phân biệt với "Báo cáo nhãn/trạng thái contact" trong Báo cáo online** — đó là báo cáo nhãn của nhóm online (controller khác); báo cáo nhãn ở đây thuộc nhóm cửa hàng (offline). Tên gần giống, dễ nhầm.

## Ảnh hưởng tới
- Read-only — chỉ đọc.
- Phí sàn phụ thuộc dữ liệu đồng bộ từ module kết nối sàn (Shopee/TikTok/Lazada); báo cáo theo nhãn phụ thuộc việc gắn nhãn.
