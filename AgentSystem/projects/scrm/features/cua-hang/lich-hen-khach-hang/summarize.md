---
alias: ["đặt lịch", "lịch hẹn", "booking", "đặt lịch trước", "lịch hẹn cửa hàng", "đặt lịch online", "nguồn đặt lịch", "kênh đặt lịch", "báo cáo đặt lịch", "thống kê đặt lịch"]
module: cua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — các thao tác lịch hẹn đều có phân quyền riêng; hầu hết thao tác còn kiểm tra 'QLDV được phép xem POS'. Quản lý nguồn đặt lịch có phân quyền riêng. Tạo lịch online là công khai (không cần đăng nhập). Báo cáo đặt lịch HIỆN BỊ VÔ HIỆU HÓA."
phu_thuoc: ["Khách hàng (tạo/tìm KH khi đặt lịch)", "Sản phẩm / Dịch vụ / Combo (nội dung lịch hẹn)", "Khu vực - chỗ ngồi (gán bàn cho lịch)", "Nguồn đặt lịch (kênh khách đặt)", "Contact / Telesale (đồng bộ trạng thái + nhãn)", "SMS / Zalo (nhắc lịch)", "Đơn hàng (đồng bộ trạng thái khi tạo/thanh toán/hủy đơn)"]
link_flow: ./flow.md
---

# Lịch hẹn khách hàng (Đặt lịch)

## Là gì
Đây là nơi **quản lý lịch hẹn / đặt lịch trước** của cửa hàng: khách đặt lịch đến (qua nhân viên, qua website, hoặc đặt hàng loạt), nhân viên xem lịch theo nhiều lát cắt (theo thời gian, vị trí/bàn, nhân viên, ngày), cập nhật trạng thái lịch, và hệ thống nhắc lịch qua SMS/Zalo. Đi kèm là **quản lý nguồn đặt lịch** (khai báo các kênh khách đặt: trực tiếp, website, fanpage, hotline…) và **báo cáo đặt lịch** (hiện đang tạm tắt).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem lịch (theo thời gian / vị trí / nhân viên / ngày) | ✅ | tùy quyền; nhân viên thường chỉ thấy lịch của mình |
| Tạo lịch (nhân viên đơn vị) | ✅ | tùy quyền; QLDV phải được phép dùng POS |
| Tạo lịch online (khách tự đặt trên website) | ✅ | công khai, không cần đăng nhập |
| Đặt lịch hàng loạt | ✅ | tùy quyền |
| Sửa thông tin lịch | ✅ | Sale chỉ sửa lịch của mình |
| Cập nhật trạng thái lịch | ✅ | tùy quyền |
| Xóa lịch hẹn | ⚠️ | có thể bị chặn xóa lịch online theo cấu hình (trừ admin/CSKH/super) |
| Import lịch hẹn từ Excel | ✅ | có file mẫu + file báo lỗi |
| Gắn nhãn khách hàng cho lịch | ✅ | tùy quyền |
| Nhắc lịch qua SMS/Zalo | ✅ | tự động theo cấu hình |
| (Nguồn đặt lịch) Thêm/Sửa/Xóa/Đặt mặc định | ✅ | tùy quyền; không xóa được nguồn lõi (Trực tiếp/Website) |
| (Nguồn đặt lịch) Khởi tạo bộ nguồn mặc định | ✅ | copy nguồn hệ thống về đơn vị |
| **Xem báo cáo đặt lịch** | ❌ | tính năng hiện đang vô hiệu hóa (toàn bộ endpoint bị tắt) |

## Để làm gì / cho ai
- **Nhân viên cửa hàng / lễ tân:** đặt lịch cho khách, xem lịch trong ngày, gán bàn/nhân viên phục vụ.
- **Khách hàng:** tự đặt lịch trên website của shop.
- **Telesale / CSKH:** xem và cập nhật lịch ở bản dành cho sale.
- **Quản lý:** theo dõi lịch hẹn, nguồn khách đặt; (báo cáo thống kê hiện tạm tắt).

## Cách dùng
**Đặt & xem lịch:**
1. Vào màn **Đặt lịch hẹn** → chọn cách xem (theo thời gian / vị trí / nhân viên / ngày).
2. **Thêm mới** lịch: chọn khách, dịch vụ/combo, thời gian, bàn/chỗ ngồi, nhân viên phục vụ → Lưu. Hệ thống có thể gửi SMS/Zalo nhắc lịch.
3. Cập nhật **trạng thái** lịch khi khách đến / phục vụ / hoàn thành / hủy / không đến.

**Quản lý nguồn đặt lịch:**
1. Vào màn **Quản lý nguồn đặt lịch**.
2. **Thêm/Sửa/Xóa** nguồn; **đặt mặc định**; hoặc bấm **Khởi tạo mặc định** để copy bộ nguồn hệ thống về đơn vị.

## Cần chú ý
- **Nhân viên thường chỉ thấy lịch của mình**; quản lý/admin thấy nhiều hơn. Đây là phân quyền, không phải lỗi.
- **Quản lý đơn vị (QLDV) phải được bật quyền dùng POS** mới thao tác được phần lớn chức năng lịch — nếu không sẽ báo "Đơn vị của bạn không cho phép sử dụng tính năng này!".
- **Tạo lịch hàng loạt không có "hoàn tác toàn bộ":** nếu lỗi ở khung giờ thứ N, các lịch trước đó vẫn đã được tạo (tạo một phần).
- **Lịch online có thể bị chặn xóa** theo cấu hình (trừ admin/CSKH/super user).
- **Mỗi đơn vị chỉ có 1 nguồn đặt lịch mặc định**; không xóa được nguồn lõi (Trực tiếp / Website).
- **Báo cáo đặt lịch đang tắt:** các route báo cáo đặt lịch hiện không hoạt động (sẽ trả lỗi không tìm thấy nếu gọi).

## Ảnh hưởng tới
- **Khách hàng:** lịch tạo/tìm khách trong hệ thống.
- **Sơ đồ bàn / khu vực:** chỗ ngồi gắn với lịch; bàn "đang phục vụ" suy ra từ lịch.
- **Contact / Telesale:** lịch có gắn contact thì đồng bộ trạng thái + nhãn sang Contact.
- **Đơn hàng:** khi tạo/thanh toán/hủy đơn từ lịch, trạng thái lịch được đồng bộ.
- **SMS/Zalo, Notification:** gửi nhắc lịch + thông báo cho thu ngân/quản lý.
