---
alias: ["báo cáo doanh số", "báo cáo bán hàng", "báo cáo doanh thu", "doanh thu theo ngày", "doanh thu theo chi nhánh", "doanh thu theo sản phẩm", "doanh số tổng quan", "tổng hợp bán hàng", "báo cáo theo giao vận"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — các báo cáo doanh số public ở mức controller (mỗi báo cáo có key quyền riêng dạng OrderReport_*, BaoCaoCuaHangController_*, BaoCaoOnlineController_*). Người dùng thường chỉ thấy số liệu trong phạm vi chi nhánh của mình; admin/quản lý đơn vị xem rộng hơn."
phu_thuoc: ["Đơn hàng (nguồn doanh số gốc)", "Cấu hình đơn vị (giới hạn khoảng ngày được xem, ép chi nhánh)", "Sản phẩm (để map tên hàng vào báo cáo)", "Cơ cấu tổ chức (phòng ban/nhóm/nhân viên để lọc theo Sale/MKT)"]
link_flow: ./flow.md
---

# Báo cáo doanh số

## Là gì
Đây là **bộ báo cáo doanh thu/doanh số bán hàng** của toàn hệ thống: cho biết bán được bao nhiêu, theo **thời gian** (ngày), theo **điểm bán** (chi nhánh), theo **khách hàng**, theo **sản phẩm/dịch vụ**, theo **thu ngân/nhân viên**, theo **nguồn** và theo **đơn vị vận chuyển (giao vận)**. Ngoài ra còn có báo cáo **doanh số tổng quan** (online) và **tổng hợp bán hàng** gom nhiều chỉ số vào một bảng.

Mỗi báo cáo hiển thị dưới dạng **bảng (lưới)** có thể lọc theo chi nhánh và khoảng ngày; phần lớn có nút **Xuất Excel**.

> Báo cáo **tổng hợp bán hàng** (gộp nhiều chỉ số bán hàng vào một bảng) được tách thành feature riêng — xem `../bao-cao-tong-hop-ban-hang/`.

Lưu ý: bộ báo cáo này **trùng** với báo cáo nằm trong từng module (Cửa hàng, Sales, Kế toán) vì cùng đọc một nguồn dữ liệu doanh thu — xem mục "Ảnh hưởng tới".

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo (lọc theo chi nhánh + khoảng ngày) | ✅ | tùy quyền; chỉ xem trong phạm vi chi nhánh được phép |
| Lọc theo sản phẩm / nhân viên / nguồn | ✅ | tùy báo cáo |
| Xuất Excel | ✅ | hầu hết báo cáo doanh số/online có nút Xuất Excel |
| Phân trang / sắp xếp bảng | ✅ | — |
| **Sửa / Xóa số liệu** | ❌ | báo cáo **chỉ đọc**, không sửa được dữ liệu từ đây |
| Xem số liệu ngoài khoảng ngày cho phép | ⚠️ | bị chặn nếu đơn vị bật giới hạn khoảng ngày |
| Xem doanh số chi nhánh khác | ⚠️ | chỉ admin/quản lý đơn vị; nhân viên thường bị ép về chi nhánh của mình |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** theo dõi doanh thu theo ngày, so sánh chi nhánh, xem mặt hàng/dịch vụ nào bán chạy, nguồn nào ra đơn nhiều.
- **Thu ngân / cửa hàng:** xem doanh số ca, doanh thu theo thu ngân.
- **Sale / Marketing:** xem doanh số tổng quan, doanh số theo nguồn, tổng hợp bán hàng.
- **Kế toán:** đối chiếu doanh thu theo chi nhánh/khách hàng.

## Cách dùng
1. Vào module **Báo cáo** và chọn báo cáo doanh số cần xem (doanh thu theo ngày, theo chi nhánh, theo sản phẩm, doanh số tổng quan, theo giao vận, tổng hợp bán hàng…).
   - Báo cáo doanh số gồm: doanh thu theo ngày, theo chi nhánh, theo khách hàng, theo sản phẩm, theo thu ngân/nhân viên, doanh số tổng quan (online), theo giao vận.
2. Chọn **chi nhánh** (nếu được phép) và **khoảng ngày Từ – Đến**; tùy báo cáo có thêm bộ lọc sản phẩm/nhân viên/nguồn.
3. Bấm **Xem / Tìm** → bảng số liệu hiện ra, có phân trang và sắp xếp.
4. Bấm **Xuất Excel** để tải file (nếu báo cáo đó hỗ trợ).

## Cần chú ý
- **Báo cáo chỉ đọc số liệu**, không thay đổi dữ liệu đơn hàng. Muốn sửa doanh thu phải sửa từ đơn hàng gốc.
- **Giới hạn khoảng ngày:** nếu đơn vị bật cấu hình giới hạn, hệ thống chỉ cho xem dữ liệu trong N ngày — chọn khoảng quá rộng sẽ báo lỗi "Chỉ được xem dữ liệu trong khoảng N ngày".
- **Phạm vi chi nhánh:** nhân viên thường tự động bị ép về chi nhánh của mình; người được xem toàn đơn vị mới thấy tất cả. Đây là phân quyền, không phải lỗi.
- **Số liệu nặng:** các báo cáo này tính trực tiếp trên cơ sở dữ liệu báo cáo, khoảng thời gian dài có thể chạy chậm.
- **Trùng báo cáo giữa các module:** cùng một con số doanh thu có thể xuất hiện ở menu Báo cáo và ở menu Cửa hàng/Sales — vì chung nguồn dữ liệu.

## Ảnh hưởng tới
- **Đơn hàng** — là nguồn gốc mọi con số doanh thu; sửa/hủy đơn sẽ thay đổi báo cáo.
- **Báo cáo Cửa hàng / Sales / Kế toán** — phần lớn dùng chung dữ liệu doanh số này.
- **Dashboard CEO** — tổng hợp lại các chỉ số doanh số ở đây cho lãnh đạo.
- **Sản phẩm, cơ cấu tổ chức** — quyết định cách gom nhóm/hiển thị tên trong báo cáo.
