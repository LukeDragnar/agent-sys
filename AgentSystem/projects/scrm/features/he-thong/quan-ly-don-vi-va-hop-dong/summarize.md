---
alias: ["quản lý đơn vị", "chi nhánh", "tenant", "hợp đồng", "đơn vị tiện ích", "hợp đồng tiện ích", "merchant", "duyệt mã đơn vị"]
module: he-thong
trang_thai: cần product xác nhận — dành cho admin hệ thống / kế toán / CSKH nội bộ SaaS
goi: cần product xác nhận
quyen: "cần product xác nhận — CRUD đơn vị & hợp đồng chủ yếu admin hệ thống (HT-AD), kế toán hệ thống (HT-KT), một số HT-CSKH. Đăng ký đơn vị mới (bản app) là [AllowAnonymous] (known-issue)."
phu_thuoc: ["Quản lý gói & tiện ích (đơn vị gắn gói + tiện ích)", "Quản lý tài khoản & người dùng (user thuộc đơn vị)", "Domain / ngành nghề (gán theo đơn vị)"]
link_flow: ./flow.md
---

# Quản lý đơn vị & hợp đồng

## Là gì
Đây là phần **nội bộ của nhà cung cấp SCRM** để quản trị **các đơn vị (tenant — mỗi khách hàng/shop là một đơn vị)** và **hợp đồng** kèm theo. Gồm:
- **Quản lý đơn vị (DonVi):** tạo/sửa/xóa đơn vị toàn hệ thống, xuất danh sách đơn vị ra Excel, **sinh mã & duyệt mã đơn vị**, xem **gói sử dụng + user** của đơn vị, **yêu cầu đổi gói**, và quản lý **merchant** (cấu hình cổng thanh toán) của đơn vị. Có hai mặt: bản **app** (đơn vị tự xem/sửa thông tin của chính mình, và **đăng ký đơn vị mới**) và bản **quản trị** (admin hệ thống quản lý mọi đơn vị).
- **Quản lý hợp đồng (HopDong):** CRUD hợp đồng kèm với đơn vị.
- **Đơn vị tiện ích (DonViTienIch):** tra cứu **tiện ích nào đang gắn cho đơn vị nào**.
- **Hợp đồng tiện ích (HopDongTienIch):** quản lý **yêu cầu mua tiện ích** của đơn vị — duyệt/hủy duyệt/xem chi tiết/xóa; duyệt thì kích hoạt tiện ích cho đơn vị.

> Đây KHÔNG phải màn cho khách thao tác hằng ngày (trừ việc đăng ký đơn vị mới và tự xem/sửa thông tin đơn vị của chính họ). Quản trị toàn hệ thống là việc của admin nhà cung cấp.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Đăng ký đơn vị mới (đơn vị tự tạo) | ⚠️ | luồng đăng ký (bản app) hiện **không bắt buộc đăng nhập** — điểm lưu ý cần xác minh chống lạm dụng |
| Thêm / Sửa / Xóa đơn vị (quản trị) | ✅ | admin hệ thống (HT-AD); kế toán/CSKH một số thao tác |
| Tìm / Lấy tất cả đơn vị toàn hệ thống | ✅ | HT-AD, HT-KT, HT-CSKH |
| Xuất danh sách đơn vị ra Excel | ⚠️ | chỉ cần đăng nhập, **không siết quyền riêng** — điểm lưu ý khi soát quyền |
| Sinh mã / Duyệt mã đơn vị | ✅ | HT-AD (GenCode, ApproveCode) |
| Xem gói + user của đơn vị | ✅ | tùy quyền |
| Yêu cầu đổi gói sử dụng của đơn vị | ✅ | tùy quyền |
| Xem / Đổi merchant (cổng thanh toán) đơn vị | ✅ | tùy quyền |
| CRUD hợp đồng | ✅ | HT-AD, HT-KT |
| Tra đơn vị nào đang dùng tiện ích nào | ✅ | HT-AD, HT-KT |
| Duyệt / Hủy duyệt / Xóa hợp đồng tiện ích | ✅ | HT-AD, HT-KT |

## Để làm gì / cho ai
- **Admin hệ thống:** quản trị toàn bộ đơn vị, duyệt mã, xử lý hợp đồng, kích hoạt tiện ích cho khách.
- **Kế toán hệ thống (HT-KT):** quản lý hợp đồng, hợp đồng tiện ích, theo dõi gói/merchant.
- **CSKH nội bộ:** tra cứu, hỗ trợ khách về thông tin đơn vị.
- **Khách (đơn vị):** đăng ký mới, tự xem/sửa thông tin đơn vị của mình, gửi yêu cầu đổi gói.

## Cách dùng
**Quản trị đơn vị:**
1. Vào **Quản trị → Đơn vị**.
2. Lọc/tìm đơn vị; mở chi tiết để sửa (chi tiết lấy bản mới nhất, không qua cache).
3. **Thêm** đơn vị → hệ thống khởi tạo dữ liệu cho tenant mới. **Sinh mã** và **Duyệt mã** khi cần.
4. Xem **gói + user**, gửi **yêu cầu đổi gói**, cấu hình **merchant**. **Xuất Excel** danh sách đơn vị.

**Hợp đồng & hợp đồng tiện ích:**
1. Vào **Quản trị → Hợp đồng** để CRUD hợp đồng.
2. Vào **Hợp đồng tiện ích / Yêu cầu mua tiện ích** để **Duyệt / Hủy duyệt** yêu cầu mua tiện ích; duyệt xong tiện ích được bật cho đơn vị.
3. Vào **Đơn vị tiện ích** để tra đơn vị nào đang dùng tiện ích gì.

## Cần chú ý
- **Đăng ký đơn vị mới (bản app) không cần đăng nhập** → cần biện pháp chống lạm dụng/đăng ký rác. Đây là điểm lưu ý về an toàn.
- **Xuất Excel đơn vị không có quyền riêng** (chỉ cần đăng nhập) — ai đã đăng nhập đều có thể xuất; cân nhắc khi soát quyền.
- **Chi tiết đơn vị để sửa lấy bản không-cache** (đảm bảo dữ liệu mới nhất); thông tin hiển thị thông thường có thể qua cache.
- **Hợp đồng tiện ích**: thao tác **Duyệt** dùng **mã hợp đồng**, còn Hủy duyệt/Chi tiết/Xóa dùng **id** — chú ý truyền đúng (known-issue không đồng nhất).
- **Duyệt hợp đồng tiện ích = kích hoạt tiện ích** cho đơn vị → đụng tới quyền dùng tính năng của khách.

## Ảnh hưởng tới
- **Tài khoản & người dùng:** user thuộc đơn vị nào theo dữ liệu đơn vị ở đây.
- **Gói & tiện ích:** duyệt hợp đồng tiện ích bật tính năng; đổi gói đổi phạm vi sử dụng.
- **Khởi tạo tenant:** tạo đơn vị mới sinh dữ liệu nền cho tenant.
