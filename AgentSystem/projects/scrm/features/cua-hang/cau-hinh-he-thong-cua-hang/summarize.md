---
alias: ["cấu hình hệ thống cửa hàng", "cài đặt cửa hàng", "setup cửa hàng", "cấu hình order", "config order", "danh mục máy in", "máy in", "quản lý khu vực", "sơ đồ bàn", "khu vực POS"]
module: cua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — Cấu hình khởi tạo đơn hàng lưu bởi DV-AD-TONG; cấu hình SuperAdmin bởi HT-AD/HT-CSKH. Danh mục máy in & Khu vực: các thao tác CRUD có gắn quyền riêng; một số dropdown/danh sách chỉ cần đăng nhập."
phu_thuoc: ["Đơn vị / chi nhánh (cấu hình lưu theo đơn vị)", "Đặt lịch & POS (khu vực-chỗ ngồi dựng sơ đồ bàn)", "Đơn hàng POS (cờ cấu hình gate hành vi, máy in để in đơn/hóa đơn)", "Dịch vụ hóa đơn (đồng bộ cấu hình hóa đơn sang đơn hàng)"]
link_flow: ./flow.md
---

# Cấu hình hệ thống cửa hàng

## Là gì
Đây là nhóm **cài đặt nền cho cửa hàng**, gồm 3 phần:
- **Cấu hình Order (cấu hình đơn vị):** các tham số/cờ tổng cho hoạt động bán hàng — ví dụ có cho phép xóa đơn hay không, quyền xem báo cáo đơn, cấu hình hóa đơn…
- **Danh mục máy in:** khai báo các máy in dùng để in đơn bếp / hóa đơn tại quầy.
- **Quản lý khu vực (sơ đồ bàn):** chia cửa hàng thành các khu vực và chỗ ngồi/bàn, dùng cho sơ đồ bàn POS, đặt lịch, màn hình quảng cáo bi-a.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem / lưu cấu hình Order của đơn vị | ✅ | lưu cần quyền (DV-AD-TONG) |
| Lưu cấu hình SuperAdmin | ✅ | chỉ HT-AD / HT-CSKH |
| Thêm / Sửa máy in | ✅ | tùy quyền |
| Bật / tắt sử dụng máy in | ✅ | tùy quyền |
| Xóa máy in | ✅ | tùy quyền |
| Thêm / Sửa khu vực (sơ đồ bàn) | ✅ | tùy quyền; không trùng tên khu vực trong cùng chi nhánh |
| **Xóa khu vực** | ⚠️ | **chặn nếu khu vực đang được sử dụng** ("Khu vực đang sử dụng không thể xóa!"); xóa khu vực sẽ xóa luôn các chỗ ngồi thuộc khu vực |

## Để làm gì / cho ai
- **Chủ shop / admin đơn vị:** thiết lập ban đầu cho cửa hàng — máy in, sơ đồ bàn, các quy tắc bán hàng.
- **Thu ngân / nhân viên POS:** dùng kết quả cấu hình — chọn máy in khi in đơn, thấy sơ đồ bàn để phục vụ.
- **Super admin SaaS (HT-AD):** cấu hình cấp cao (SuperAdmin) áp cho đơn vị.

## Cách dùng
**Cấu hình Order:**
1. Vào **Cài đặt hệ thống → Cấu hình Order**.
2. Chỉnh các tham số/cờ → bấm **Lưu**.

**Danh mục máy in:**
1. Vào **Order → Danh mục máy in**.
2. Bấm **Thêm mới** / **Sửa** để khai báo máy in; **bật/tắt** trạng thái sử dụng; **xóa** máy in không dùng.

**Quản lý khu vực (sơ đồ bàn):**
1. Mở màn **Quản lý khu vực**.
2. **Thêm mới** khu vực (đặt tên, số thứ tự) rồi thêm các chỗ ngồi/bàn.
3. **Sửa** hoặc **xóa** khu vực; lưu ý không xóa được khu vực đang dùng.

## Cần chú ý
- **Cờ "không cho phép xóa đơn hàng"** trong cấu hình Order sẽ **chặn xóa đơn** ở màn bán hàng nếu bật — đây là chủ ý, không phải lỗi.
- **Khu vực đang sử dụng không xóa được.** Phải tách hết bàn đang phục vụ trước khi xóa.
- **Xóa khu vực = xóa luôn chỗ ngồi** thuộc khu vực đó (thực hiện trong một thao tác).
- **Tên khu vực không được trùng** trong cùng đơn vị + chi nhánh.
- Máy in khai báo ở đây mới hiện ra để chọn khi in đơn bếp / hóa đơn ở POS.
- Cấu hình SuperAdmin chỉ dành cho tài khoản cấp hệ thống (HT-AD/HT-CSKH), không phải admin đơn vị thường.

## Ảnh hưởng tới
- **Bán hàng POS:** cờ cấu hình Order gate hành vi (vd chặn xóa đơn); máy in để in đơn/hóa đơn; sơ đồ bàn để phục vụ.
- **Đặt lịch:** khu vực-chỗ ngồi dùng chung với lịch hẹn (chỗ ngồi đang phục vụ tính theo lịch).
- **Hóa đơn:** cấu hình hóa đơn được service Invoice đồng bộ sang Order.
