---
alias: ["cài đặt chung", "cấu hình hệ thống", "tham số hệ thống", "cấu hình chức năng", "chi nhánh đơn vị", "tài khoản ngân hàng", "bảo mật tài khoản", "thiết lập đơn vị"]
module: cai-dat
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — phần lớn dành cho admin đơn vị (DV-AD-TONG, DV-AD-CN, DV-AD-MCN). Cấu hình chức năng (Cài đặt chung) yêu cầu SuperUser hoặc CSKH hệ thống hoặc Admin đơn vị; tài khoản ngân hàng yêu cầu các quyền TkNganHangHeThong_* ; buộc đổi mật khẩu yêu cầu UserBaoMat_YeuCauDoiMatKhau (DV-AD-TONG/CN/MCN)."
phu_thuoc: ["Đơn vị & chi nhánh (mọi cấu hình gắn theo đơn vị/chi nhánh đang đăng nhập)", "Nhà cung cấp / Thu chi (tài khoản ngân hàng phục vụ thu/chi)", "Các module dùng cấu hình chung: Đơn hàng, Khách hàng, Kho, Contact, Khuyến mãi…"]
link_flow: ./flow.md
---

# Cấu hình hệ thống

## Là gì
Đây là khu **Cài đặt** của đơn vị — nơi chủ shop / admin khai báo các **thiết lập chung của toàn hệ thống** cho riêng đơn vị mình. Gồm 4 nhóm chính:
- **Tham số chung (Cấu hình chức năng):** bật/tắt và tinh chỉnh cách hệ thống hoạt động (đơn hàng, khách hàng, kho, contact, hoa hồng, check-in…). Mỗi nhóm là một tab cấu hình riêng.
- **Bảo mật tài khoản:** thiết lập an toàn cho tài khoản nhân viên — ví dụ admin **bắt buộc nhân viên đổi mật khẩu**.
- **Chi nhánh:** khai báo và quản lý danh sách chi nhánh/cửa hàng của đơn vị.
- **Tài khoản ngân hàng:** khai báo các tài khoản ngân hàng của chi nhánh dùng cho **thu/chi**, chọn tài khoản **mặc định** khi thu và khi chi, và liên kết với máy quẹt thẻ (terminal) nếu có.

Đây là phần "khung xương" của đơn vị: cấu hình ở đây ảnh hưởng tới cách rất nhiều màn hình khác hoạt động.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Sửa tham số chung (cấu hình chức năng) | ✅ | SuperUser / CSKH hệ thống / Admin đơn vị (tab "Cài đặt chung") |
| Thêm / Sửa / Xóa chi nhánh | ✅ | cần quyền quản lý chi nhánh; chi nhánh tự gán vào đơn vị đang đăng nhập |
| Xem chi tiết chi nhánh | ✅ | tùy quyền |
| Thêm tài khoản ngân hàng | ✅ | cần quyền thêm tài khoản ngân hàng |
| Sửa tài khoản ngân hàng | ✅ | cần quyền cập nhật tài khoản ngân hàng |
| **Xóa tài khoản ngân hàng** | ⚠️ | cần quyền xóa, **nhưng bị chặn nếu tài khoản đã được sử dụng** |
| Đặt tài khoản ngân hàng mặc định (thu/chi) | ✅ | cần quyền đặt tài khoản mặc định |
| Buộc nhân viên đổi mật khẩu | ✅ | admin đơn vị |
| Kiểm tra kết nối terminal online của TK ngân hàng | ✅ | tùy quyền (chỉ login) |

## Để làm gì / cho ai
- **Chủ shop / admin đơn vị:** thiết lập một lần cách hệ thống vận hành cho cả đơn vị (quy tắc đơn hàng, hiển thị khách, kho…), mở thêm chi nhánh khi phát triển, khai báo tài khoản ngân hàng để thu/chi đúng quỹ.
- **Kế toán:** dùng tài khoản ngân hàng đã khai báo (và TK mặc định) để ghi nhận thu/chi, tạo QR thu tiền.
- **Người phụ trách bảo mật:** ép nhân viên đổi mật khẩu khi nghi ngờ lộ thông tin.

## Cách dùng
**Cấu hình chức năng (tham số chung):**
1. Vào **Cài đặt → Cấu hình hệ thống**.
2. Chọn tab cần chỉnh (Cài đặt chung và các nhóm cấu hình theo module).
3. Thay đổi thiết lập rồi **Lưu**. Thay đổi áp dụng cho toàn đơn vị.

**Chi nhánh:**
1. Vào **Cài đặt → Chi nhánh**.
2. **Thêm mới** chi nhánh (nhập tên, mã, địa chỉ…) hoặc **Sửa/Xóa** chi nhánh có sẵn.

**Tài khoản ngân hàng:**
1. Mở màn quản lý tài khoản ngân hàng của đơn vị.
2. **Thêm** tài khoản (ngân hàng, số tài khoản, chủ tài khoản, chi nhánh, terminal nếu có).
3. Bấm **Mặc định** để chọn tài khoản dùng mặc định khi thu/chi.
4. Có thể **kiểm tra kết nối online** với máy quẹt thẻ (terminal).

**Bảo mật:**
1. Vào **Cài đặt → Cài đặt bảo mật**.
2. Chọn nhân viên và **Yêu cầu đổi mật khẩu** → lần đăng nhập sau nhân viên buộc phải đổi.

## Cần chú ý
- **Không xóa được tài khoản ngân hàng nếu đã dùng** — hệ thống kiểm tra trước khi xóa; tài khoản đã phát sinh giao dịch/cấu hình sẽ bị chặn.
- **Tài khoản mặc định có 2 loại:** mặc định khi **thu** và mặc định khi **chi** — đặt riêng cho từng loại.
- **Cấu hình chức năng ảnh hưởng diện rộng:** đổi một thiết lập (ví dụ hiển thị khách toàn đơn vị, quy tắc đơn hàng) sẽ thay đổi hành vi của nhiều màn hình khác → nên cân nhắc trước khi đổi.
- **Buộc đổi mật khẩu chỉ đặt cờ**, nhân viên vẫn đăng nhập được nhưng sẽ bị yêu cầu đổi ngay ở lần sau.
- **Lưu ý bảo mật nội bộ:** một chức năng tra cứu tài khoản ngân hàng theo mã thiết bị thanh toán hiện không yêu cầu đăng nhập (chi tiết kỹ thuật ở dev.md) — đã ghi nhận để đội kỹ thuật xử lý.

## Ảnh hưởng tới
- **Thu chi / Kế toán:** dùng tài khoản ngân hàng + tài khoản mặc định để ghi nhận dòng tiền và tạo QR thu tiền.
- **Toàn bộ màn hình theo module:** cấu hình chức năng quyết định cách hiển thị/ràng buộc (đơn hàng, khách hàng, kho, contact…).
- **Đăng nhập của nhân viên:** thiết lập bảo mật (buộc đổi mật khẩu) tác động tới phiên đăng nhập kế tiếp.
- **Báo cáo theo chi nhánh:** thêm/xóa chi nhánh thay đổi phạm vi lọc và báo cáo.
