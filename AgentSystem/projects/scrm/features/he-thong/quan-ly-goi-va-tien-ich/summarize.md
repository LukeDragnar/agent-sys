---
alias: ["quản lý gói", "gói sử dụng", "gói cước", "nâng cấp gói", "quản lý tiện ích", "chợ tiện ích", "mua tiện ích", "tiện ích đã mua", "marketplace tiện ích"]
module: he-thong
trang_thai: cần product xác nhận — admin hệ thống quản danh mục; đơn vị tự mua tiện ích
goi: cần product xác nhận
quyen: "cần product xác nhận — CRUD danh mục gói/tiện ích chủ yếu admin hệ thống (HT-AD, HT-KT). Mua/quản lý tiện ích đã mua là admin đơn vị (DV-AD-TONG...). Duyệt nâng cấp gói chỉ super-admin / kế toán HT / admin HT."
phu_thuoc: ["Quản lý đơn vị & hợp đồng (gói/tiện ích gắn vào đơn vị qua hợp đồng tiện ích)", "Phân quyền & quản lý quyền (tiện ích bật mới + quyền role mới dùng được chức năng)"]
link_flow: ./flow.md
---

# Quản lý gói & tiện ích

## Là gì
Đây là phần định nghĩa **"khách mua gì" trên SCRM** — gồm hai khái niệm:
- **Gói sử dụng (GoiSuDung):** các **gói cước** của hệ thống (ví dụ Basic / Pro…). Admin hệ thống CRUD danh mục gói; còn có luồng **yêu cầu nâng cấp gói** của đơn vị và bước **duyệt/hủy duyệt** nâng cấp.
- **Tiện ích (TienIch):** các **tính năng cộng thêm** bán cho đơn vị (ví dụ ChatHub, tổng đài…). Có hai mặt: **admin CRUD danh mục tiện ích**, và **đơn vị xem/mua/quản lý tiện ích đã mua** (chợ tiện ích, mua ngay, lịch sử giao dịch, xóa tiện ích đã đăng ký). Khi đơn vị **mua tiện ích**, một hợp đồng tiện ích được tạo và tính năng tương ứng được bật cho đơn vị.

> Đây vừa là phần **nội bộ admin** (CRUD danh mục gói/tiện ích, duyệt nâng cấp), vừa là phần **đơn vị tự thao tác** (chợ tiện ích — admin đơn vị mua tiện ích cho shop của mình).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm / Sửa gói sử dụng | ✅ | admin hệ thống (HT-AD/HT-KT) |
| Xóa gói sử dụng | ✅ | check tồn tại trước; không có → báo NotFound |
| Tìm gói theo điều kiện | ✅ | HT-AD, HT-KT |
| Xem danh sách yêu cầu nâng cấp gói | ✅ | DV-AD-TONG, HT-KT |
| Duyệt / Hủy duyệt yêu cầu nâng cấp gói | ✅ | **chỉ super-admin HOẶC kế toán HT HOẶC admin HT** (check trong code) |
| Thêm / Sửa / Xóa danh mục tiện ích | ✅ | admin hệ thống (HT-AD; HT-KT một số) |
| Xóa danh mục tiện ích đang có đơn vị dùng | ❌ | bị chặn: "Tiện ích đang có đơn vị sử dụng nên không thể xóa" |
| Xem chợ tiện ích (đơn vị) | ✅ | DV-AD-TONG, HT-KT |
| Mua tiện ích (mua ngay) | ✅ | DV-AD-TONG, HT-KT |
| Xem tiện ích đã mua | ✅ | DV-AD-TONG/CN/MCN, DV-QLDV, HT-KT |
| Xem lịch sử giao dịch tiện ích | ✅ | đăng nhập |
| Xóa (hủy) tiện ích đã đăng ký | ✅ | DV-AD-TONG (thao tác kỹ thuật kiểu "mở link" — điểm lưu ý nội bộ) |

## Để làm gì / cho ai
- **Admin hệ thống / kế toán HT:** dựng danh mục gói cước và tiện ích, đặt giá/khuyến mãi, duyệt yêu cầu nâng cấp gói của khách.
- **Super-admin:** quyết định cuối cùng việc duyệt/hủy nâng cấp gói.
- **Admin đơn vị (chủ shop):** vào chợ tiện ích, mua tiện ích cần dùng, xem tiện ích đã mua và lịch sử giao dịch, hủy tiện ích không dùng nữa.

## Cách dùng
**Quản lý gói (admin):**
1. Vào **Quản trị → Gói sử dụng**.
2. Lưới gói; **Thêm/Sửa/Xóa** gói. Mở **Danh sách yêu cầu nâng cấp** để xem các đơn vị xin đổi gói.
3. **Duyệt** hoặc **Hủy duyệt** yêu cầu nâng cấp (chỉ super/kế toán HT/admin HT).

**Quản lý tiện ích:**
1. Admin: vào **Hệ thống → Quản lý tiện ích**, CRUD danh mục tiện ích.
2. Đơn vị: vào **Đơn vị → Mua tiện ích / Chợ tiện ích**, xem danh sách, bấm **Mua ngay**; xem **Tiện ích đã mua** và **Lịch sử giao dịch**; **Xóa** tiện ích đã đăng ký nếu không cần.

## Cần chú ý
- **Tiện ích là "công tắc bật/tắt tính năng" cho toàn hệ thống.** Nhiều màn/chức năng chỉ truy cập được nếu đơn vị đã mua tiện ích chứa nó. Mua tiện ích = bật; xóa tiện ích đã đăng ký = thu hồi.
- **Bật tiện ích chưa đủ để dùng** — user còn phải có **quyền (role)** cho chức năng đó. Hai lớp khóa độc lập: tiện ích + phân quyền.
- **Không xóa được danh mục tiện ích đang có đơn vị dùng** — hệ thống chặn để tránh gãy đơn vị đang sử dụng.
- **Duyệt/Hủy duyệt nâng cấp gói** chỉ super-admin/kế toán HT/admin HT; người khác bấm sẽ bị báo không có quyền (gating trong code, không chỉ ở nhãn).
- **Hủy tiện ích đã đăng ký dùng thao tác kỹ thuật kiểu "mở link"** — về nghiệp vụ là xóa; chú ý không gọi nhầm. Đây là điểm lưu ý nội bộ.

## Ảnh hưởng tới
- **Đơn vị & hợp đồng:** mua tiện ích tạo hợp đồng tiện ích; nâng cấp gói đổi gói áp dụng cho đơn vị.
- **Phân quyền & truy cập:** tiện ích quyết định chức năng nào "có mặt" cho đơn vị; phối hợp với role để cho phép dùng.
- **Mọi controller có `[TienIch("<code>")]`:** phụ thuộc tiện ích đã mua để cho truy cập.
