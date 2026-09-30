---
alias: ["báo cáo quỹ", "báo cáo quỹ tài khoản", "sổ quỹ", "số dư quỹ", "quỹ tiền mặt theo tài khoản", "sổ quỹ tiền mặt", "ví điện tử", "tài khoản ngân hàng"]
module: ke-toan
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — báo cáo chỉ cần đăng nhập (không có quyền riêng theo key), nhưng phải qua cổng kiểm quyền xem báo cáo đơn hàng (cấu hình bên module Đơn hàng). Không có quyền đó sẽ bị chặn."
phu_thuoc: ["Thu chi (phiếu thu/chi là nguồn phát sinh số dư quỹ)", "Khách hàng / Nhà cung cấp (tên đối tượng trên báo cáo)", "Cấu hình quyền xem báo cáo đơn hàng (gate báo cáo)", "Danh mục tài khoản (tiền mặt / ví điện tử / ngân hàng)"]
link_flow: ./flow.md
---

# Báo cáo quỹ (Báo cáo quỹ tài khoản)

## Là gì
Báo cáo **sổ quỹ theo tài khoản** — cho biết số dư và phát sinh của các quỹ (tiền mặt, ví điện tử, tài khoản ngân hàng) theo từng đối tượng trong một khoảng thời gian. Bấm vào một dòng để xem **chi tiết theo mã phiếu** (từng giao dịch tạo nên số dư đó).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo quỹ tài khoản | ✅ | chỉ cần đăng nhập + qua cổng kiểm quyền xem báo cáo đơn hàng |
| Xem chi tiết quỹ theo mã phiếu | ✅ | như trên |
| Lọc theo chi nhánh / khoảng ngày / loại tài khoản / loại đối tượng / từ khóa | ✅ | trong màn báo cáo |
| Sửa / ghi nhận giao dịch trực tiếp ở báo cáo | ❌ | đây là báo cáo **chỉ xem** — số liệu lấy từ phiếu thu-chi; muốn thay đổi thì xử lý ở chứng từ gốc |
| Xuất Excel báo cáo quỹ | ❓ | cần product xác nhận — registry không liệt kê endpoint xuất Excel cho báo cáo quỹ |

## Để làm gì / cho ai
- **Kế toán / thủ quỹ:** nắm số dư từng quỹ (tiền mặt, ví, ngân hàng), phát sinh thu – chi theo kỳ; đối chiếu với thực tế.
- **Chủ shop / quản lý:** xem tổng quan tiền đang nằm ở đâu, kiểm soát dòng tiền theo chi nhánh.

## Cách dùng
1. Vào **Kế toán → Báo cáo → Báo cáo quỹ tài khoản**.
2. Lọc theo chi nhánh, **Từ / Đến ngày**, loại tài khoản (tiền mặt / ví / ngân hàng), loại đối tượng, từ khóa.
3. Bảng hiện số dư quỹ theo từng đối tượng/tài khoản.
4. Bấm vào một dòng để xem **chi tiết theo mã phiếu** — từng giao dịch thu/chi tạo nên số dư.

## Cần chú ý
- **Đây là báo cáo chỉ xem, không thay đổi dữ liệu.** Số liệu được tổng hợp từ các phiếu thu-chi; muốn điều chỉnh phải xử lý ở chứng từ gốc (phiếu thu/chi, đơn hàng…).
- **Báo cáo đi qua cổng kiểm quyền xem báo cáo đơn hàng** — không có quyền đó sẽ bị chặn ("Bạn không có quyền xem báo cáo đơn hàng"), không phải lỗi báo cáo.
- **Báo cáo nặng (chạy trên dữ liệu lớn) có thể chậm** với khoảng ngày dài; nên lọc khoảng thời gian hợp lý.
- Tên đối tượng (khách / nhà cung cấp) trên báo cáo lấy từ dữ liệu KH/NCC — nếu thiếu, có thể hiển thị trống.

## Ảnh hưởng tới
- Không thay đổi dữ liệu (read-only). Chỉ phản ánh kết quả của **Thu chi** và các giao dịch quỹ.
