---
alias: ["phân quyền", "phân quyền vai trò", "quản lý quyền", "role hệ thống", "chức danh", "phân quyền chức năng", "quyền custom", "copy quyền"]
module: he-thong
trang_thai: cần product xác nhận — phần lớn dành cho admin hệ thống / nội bộ SaaS
goi: cần product xác nhận
quyen: "cần product xác nhận — đa số thao tác chỉ admin hệ thống (HT-AD), một số chỉ super-admin (IsSuperUser). Phân quyền cấp đơn vị (admin đơn vị) chạy ở màn riêng của đơn vị."
phu_thuoc: ["Quản lý tài khoản & người dùng (user gắn role)", "Quản lý trang & giao diện (Page/PageMobile tham chiếu role để hiện màn)", "Quản lý đơn vị (copy quyền giữa các đơn vị)", "Quản lý gói & tiện ích (tiện ích bật/tắt chức năng)"]
link_flow: ./flow.md
---

# Phân quyền & quản lý quyền

## Là gì
Đây là phần **nội bộ của nhà cung cấp SCRM (admin hệ thống / super-admin)** dùng để định nghĩa **ai được làm gì** trong toàn hệ thống. Nó gồm ba mảng:
- **Quản lý Role (chức danh / nhóm quyền):** khai báo các vai trò như admin đơn vị, kế toán, sale, marketing… Mỗi user sẽ được gán một hoặc nhiều role; role quyết định thấy menu nào, bấm được nút nào.
- **Phân quyền chức năng:** bảng ma trận "role × chức năng" — tích vào ô để cho phép một role dùng một chức năng. Có hai cấp: **mặc định toàn hệ thống** (super-admin đặt) và **theo từng đơn vị** (admin đơn vị tinh chỉnh cho shop của mình). Còn có công cụ **copy quyền** từ đơn vị này sang đơn vị khác và **tự động phân quyền** theo mẫu.
- **Quyền custom (override):** các quyền cấp riêng cho một user/role ngoài role mặc định — màn này chỉ để xem và thu hồi (xóa).

> Đây KHÔNG phải màn cho khách hàng cuối thao tác hằng ngày. Khách chỉ "thấy hậu quả" của phân quyền (vào được/không vào được màn nào).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm / Sửa Role (chức danh) | ✅ | admin hệ thống (HT-AD) |
| Xóa Role | ⚠️ | admin hệ thống; **không kiểm tra role đang gán cho user** trước khi xóa → có thể để user "mồ côi" role (known-issue) |
| Phân quyền chức năng mặc định (toàn hệ thống) | ✅ | **chỉ super-admin** |
| Phân quyền chức năng theo đơn vị | ✅ | admin đơn vị (màn cấp đơn vị) |
| Copy quyền giữa các đơn vị | ✅ | bản V2 chỉ super-admin |
| Tự động phân quyền theo mẫu | ✅ | admin hệ thống (HT-AD, HT-CSKH) |
| Tạo / Sửa / Xóa permission "handmade" (theo keycode) | ✅ | admin hệ thống |
| Gán role cho từng key chức năng | ✅ | admin hệ thống |
| Xem danh sách quyền custom (override) | ✅ | admin hệ thống (HT-AD, HT-CSKH) |
| Xóa (thu hồi) quyền custom | ✅ | admin hệ thống; xóa cứng, không lưu lý do thu hồi |
| Tự tạo / sửa quyền custom qua màn này | ❌ | màn quyền custom chỉ xem + xóa; quyền custom được sinh ở luồng khác |

## Để làm gì / cho ai
- **Super-admin (nhà cung cấp SCRM):** thiết lập "khuôn" quyền mặc định cho toàn bộ hệ thống, copy nhanh sang đơn vị mới, tự động phân quyền để khỏi tích tay từng ô.
- **Admin hệ thống / CSKH nội bộ:** tạo/sửa role, xử lý quyền custom, hỗ trợ khách khi họ "không vào được màn nào đó".
- **Admin đơn vị (chủ shop được cấp quyền):** tinh chỉnh quyền chức năng cho nhân viên trong shop của mình ở màn cấp đơn vị.

## Cách dùng
**Quản lý Role:**
1. Vào **Hệ thống → Phân quyền → Quản lý quyền (Role)**.
2. Bảng liệt kê các role; bấm **Thêm** để tạo chức danh mới (tên, thứ tự, đánh dấu có phải role hệ thống không).
3. Bấm vào một dòng để **Sửa**, hoặc **Xóa** role không dùng nữa.

**Phân quyền chức năng:**
1. Mở màn phân quyền, hệ thống tải **danh sách tiện ích** và **bảng ma trận quyền theo role**.
2. Tích/bỏ tích các ô (role được dùng chức năng nào) rồi **Lưu phân quyền**.
3. Muốn nhân bản cấu hình: dùng **Copy quyền** chọn đơn vị nguồn → đơn vị đích; hoặc **Tự động phân quyền** để hệ thống gán theo mẫu.

**Quyền custom:**
1. Mở màn **Quản lý quyền custom**, bảng liệt kê các quyền cấp riêng.
2. Cần thu hồi quyền nào thì bấm **Xóa**.

## Cần chú ý
- **Phân quyền mặc định toàn hệ thống chỉ super-admin làm được.** Người không phải super-admin bấm vào sẽ bị từ chối — đây là chặn cố ý, không phải lỗi.
- **Xóa Role không an toàn:** hệ thống không kiểm tra role còn đang gán cho user trước khi xóa, dễ khiến user mất role đang dùng. Cân nhắc kỹ trước khi xóa role đã có người gán.
- **Phân quyền theo đơn vị và mặc định hệ thống là hai cấp khác nhau.** Sửa ở cấp này không tự lan sang cấp kia.
- **Quyền custom xóa là xóa cứng**, không lưu lý do — thu hồi xong khó truy vết.
- Đổi quyền của một role sẽ **ảnh hưởng ngay tới mọi user đang mang role đó** (vào được/mất quyền vào màn).

## Ảnh hưởng tới
- **Tài khoản & người dùng:** user gắn role nào thì hưởng quyền của role đó.
- **Trang & menu (Page/PageMobile):** mỗi trang gán cho role nào thì role đó mới thấy.
- **Đơn vị:** copy quyền sang đơn vị khác → đổi toàn bộ quyền của đơn vị đích.
- **Gói & tiện ích:** chức năng còn phải được **bật tiện ích** thì role có quyền mới dùng được (hai lớp khóa: tiện ích + phân quyền).
