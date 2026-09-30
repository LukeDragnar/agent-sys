---
alias: ["báo cáo tổng hợp bán hàng", "tổng hợp bán hàng", "bảng tổng hợp bán hàng", "báo cáo tổng quát bán hàng", "report tổng hợp bán hàng"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — endpoint public (đăng nhập + có khóa quyền tính năng `BaoCaoOnlineController_ReportTongHopBanHang`). Hệ thống tự ép phạm vi chi nhánh của user nếu không phải admin/super/CSKH, và lọc theo cơ cấu tổ chức được phép xem."
phu_thuoc: ["Contact / dữ liệu bán hàng (nguồn tổng hợp)", "Cấu hình giới hạn khoảng ngày được phép tra báo cáo", "Cơ cấu tổ chức (phòng ban/nhóm/nhân viên sale-MKT để lọc)"]
link_flow: ./flow.md
---

# Báo cáo tổng hợp bán hàng

## Là gì
Báo cáo **gộp các chỉ số bán hàng và tác nghiệp** vào một bảng tổng quát theo khoảng thời gian — thay vì mở từng báo cáo lẻ, người dùng nhìn một chỗ thấy bức tranh chung về kết quả bán hàng. Đây là một báo cáo trong nhóm **"Báo cáo online"** (bộ báo cáo sale/marketing lớn nhất của hệ thống). Báo cáo chỉ đọc, không thay đổi dữ liệu.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo tổng hợp bán hàng | ✅ | đăng nhập + có quyền tính năng; tự lọc theo chi nhánh/cơ cấu tổ chức được phép |
| Lọc theo chi nhánh, khoảng ngày, cơ cấu tổ chức | ✅ | trong giới hạn khoảng ngày cho phép của đơn vị |
| Xuất Excel báo cáo | ✅ | dùng chung quyền/khóa tính năng |
| Thêm / Sửa / Xóa dữ liệu | ❌ | báo cáo chỉ đọc — không tạo/sửa/xóa gì từ đây |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** nắm nhanh tổng quan kết quả bán hàng trong kỳ, không phải ghép từ nhiều báo cáo.
- **Quản lý sale/MKT:** theo dõi tổng hợp hoạt động bán hàng của bộ phận mình.

## Cách dùng
1. Vào **Báo cáo → Báo cáo online → Báo cáo tổng hợp bán hàng** (tên menu tùy phiên bản — cần product xác nhận).
2. Chọn **chi nhánh**, **Từ ngày / Đến ngày**, và (nếu cần) lọc **phòng ban / nhóm / nhân viên**.
3. Bấm xem → bảng tổng hợp hiện ra.
4. Bấm **Xuất Excel** nếu muốn tải về.

## Cần chú ý
- **Khoảng ngày bị giới hạn:** đơn vị có thể cấu hình "chỉ được xem trong N ngày"; chọn rộng quá sẽ bị chặn.
- **Phạm vi xem theo quyền:** user không phải admin/super/CSKH bị ép về chi nhánh + cơ cấu tổ chức được phép → số có thể nhỏ hơn quản lý. Là phân quyền, không phải lỗi.
- **Báo cáo này nằm chung "Báo cáo online"** với nhiều báo cáo khác; cùng bộ lọc, cùng cách dùng.
- Đây là báo cáo **tổng hợp** — số liệu chi tiết của từng phần (doanh số theo nguồn, theo SP, tỉ lệ chốt đơn...) xem ở các báo cáo chuyên biệt tương ứng.

## Ảnh hưởng tới
- Không ghi/đổi dữ liệu (read-only) — chỉ đọc.
- Cùng nguồn dữ liệu bán hàng với các báo cáo online khác.
