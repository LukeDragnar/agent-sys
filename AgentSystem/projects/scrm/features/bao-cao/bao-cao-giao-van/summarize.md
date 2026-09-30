---
alias: ["báo cáo giao vận", "báo cáo vận chuyển", "doanh số theo đơn vị vận chuyển", "báo cáo theo ĐVVC", "doanh số theo giao vận", "hiệu suất giao hàng", "báo cáo ship"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — endpoint thuộc nhóm public (chỉ cần đăng nhập + có khóa quyền tính năng `BaoCaoOnlineController_ReportDoanhSoTheoGiaoVan`). Hệ thống tự ép phạm vi chi nhánh của user nếu user không phải admin/super/CSKH, và lọc theo cơ cấu tổ chức (phòng ban/nhóm/nhân viên) mà user được phép xem."
phu_thuoc: ["Đơn hàng giao vận (nguồn doanh số/đơn theo ĐVVC)", "Đơn vị vận chuyển (Viettel Post, GHN, J&T, GHTK, AhaMove... — phân nhóm doanh số)", "Cấu hình giới hạn khoảng ngày được phép tra báo cáo", "Cơ cấu tổ chức (phòng ban/nhóm/nhân viên sale-MKT để lọc)"]
link_flow: ./flow.md
---

# Báo cáo giao vận

## Là gì
Báo cáo thống kê **doanh số và hiệu suất bán hàng theo từng đơn vị vận chuyển (ĐVVC)** — ví dụ bao nhiêu đơn, doanh thu bao nhiêu đi qua Viettel Post, GHN, J&T, GHTK, AhaMove... trong một khoảng thời gian. Giúp shop biết hãng vận chuyển nào đang mang lại nhiều đơn/doanh thu nhất.

Đây là **một báo cáo trong nhóm "Báo cáo online"** (bộ báo cáo sale/marketing lớn nhất của hệ thống). Báo cáo chỉ đọc dữ liệu, không thay đổi gì.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo doanh số theo giao vận | ✅ | đăng nhập + có quyền tính năng; tự lọc theo chi nhánh/cơ cấu tổ chức được phép |
| Lọc theo chi nhánh, khoảng ngày, cơ cấu tổ chức | ✅ | trong giới hạn khoảng ngày cho phép của đơn vị |
| Xuất Excel báo cáo | ✅ | dùng chung quyền với báo cáo (cùng 1 khóa quyền) |
| Thêm / Sửa / Xóa dữ liệu | ❌ | báo cáo chỉ đọc — không tạo/sửa/xóa gì từ đây |

## Để làm gì / cho ai
- **Chủ shop / quản lý vận hành:** so sánh các hãng vận chuyển để biết hãng nào nhiều đơn, doanh thu cao, từ đó điều chỉnh ưu tiên hãng giao.
- **Sale / chăm sóc đơn:** nắm bức tranh đơn hàng theo hãng giao trong kỳ.

## Cách dùng
1. Vào **Báo cáo → Báo cáo online → Báo cáo doanh số theo giao vận** (tên menu tùy phiên bản — cần product xác nhận).
2. Chọn **chi nhánh**, **Từ ngày / Đến ngày**, và (nếu cần) lọc theo **phòng ban / nhóm / nhân viên**.
3. Bấm xem → bảng hiện doanh số/đơn theo từng đơn vị vận chuyển.
4. Bấm **Xuất Excel** nếu muốn tải file về.

## Cần chú ý
- **Khoảng ngày bị giới hạn:** mỗi đơn vị có thể cấu hình "chỉ được xem dữ liệu trong N ngày". Nếu chọn khoảng quá rộng sẽ bị chặn với thông báo "Chỉ được xem dữ liệu trong khoảng N ngày".
- **Phạm vi xem theo quyền:** nếu bạn không phải admin/super/CSKH, hệ thống tự lọc về **chi nhánh của bạn** và **cơ cấu tổ chức bạn được phép xem** — nên số liệu có thể nhỏ hơn người quản lý. Đây là phân quyền, không phải lỗi.
- **Báo cáo này nằm chung "Báo cáo online"** với nhiều báo cáo sale/MKT khác (doanh số tổng quan, bảng xếp hạng, tỉ lệ chốt đơn...). Cùng một bộ lọc và cách dùng.
- Số liệu **lấy theo đơn hàng giao vận** đã ghi nhận; đơn chưa phát sinh/chưa gán hãng giao sẽ không nằm trong báo cáo theo ĐVVC.

## Ảnh hưởng tới
- Không ghi/đổi dữ liệu (read-only) — chỉ đọc từ dữ liệu đơn hàng giao vận đã có.
- Cùng nguồn với các báo cáo doanh số khác trong "Báo cáo online"; thay đổi cách tính/đơn hàng gốc sẽ ảnh hưởng số liệu ở đây.
