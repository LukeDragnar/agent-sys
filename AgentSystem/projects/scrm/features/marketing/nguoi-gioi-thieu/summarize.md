---
alias: ["người giới thiệu", "nguồn giới thiệu", "mã giới thiệu", "referral", "NGT", "CTV giới thiệu", "code giới thiệu"]
module: marketing
trang_thai: cần product xác nhận — nguồn giới thiệu cần tiện ích Hoa hồng giới thiệu; mã giới thiệu thuộc nhóm tính năng Hệ thống
goi: cần product xác nhận
quyen: "cần product xác nhận — Nguồn giới thiệu (CRUD): DV-AD-TONG, DV-AD-CN, DV-AD-MCN (+ QLDV cho check). Mã giới thiệu: Admin (HT-AD, HT-KT, HT-CSKH) quản lý toàn bộ; người giới thiệu (HT-NGT) chỉ xem mã của mình; duyệt thanh toán hoa hồng chỉ HT-AD, HT-KT. Kiểm tra mã khi đăng ký là public"
phu_thuoc: ["Tiện ích Hoa hồng giới thiệu (cho nguồn giới thiệu)", "Đơn hàng (gắn nguồn giới thiệu để tính hoa hồng khi thanh toán)", "Đơn vị / Hợp đồng (mã giới thiệu gắn theo đơn vị/hợp đồng đã đăng ký)", "Cấu hình hoa hồng giới thiệu theo nguồn"]
link_flow: ./flow.md
---

# Người giới thiệu

## Là gì
Đây là nơi quản lý **người/kênh giới thiệu khách hàng** để tính hoa hồng, gồm 2 phần:
- **Nguồn giới thiệu** (cấp đơn vị/shop): khai báo người hoặc kênh giới thiệu khách. Khi đơn hàng gắn nguồn giới thiệu và có cấu hình hoa hồng → hệ thống phát sinh hoa hồng khi thanh toán.
- **Mã giới thiệu** (cấp hệ thống/đại lý): mã referral của **Người giới thiệu (NGT)** — đại lý/cộng tác viên giới thiệu khách đăng ký dùng phần mềm. Quản trị cấp mã, theo dõi đơn vị/hợp đồng đã nhập mã, và **duyệt thanh toán hoa hồng** cho người giới thiệu.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm / Sửa / Xóa nguồn giới thiệu | ✅ | DV-AD-TONG/CN/MCN |
| Xem danh sách / tìm nguồn giới thiệu | ✅ | tùy quyền (search nhanh chỉ cần đăng nhập) |
| Kiểm tra nguồn hợp lệ (trước khi gắn đơn) | ✅ | tùy quyền |
| Thêm / Xóa mã giới thiệu (Admin) | ✅ | HT-AD, HT-KT, HT-CSKH |
| Người giới thiệu xem mã của mình | ✅ | HT-NGT |
| Tìm mã theo đơn vị / hợp đồng | ✅ | Admin hoặc NGT (theo phạm vi) |
| Bật / tắt sử dụng mã | ✅ | HT-AD, HT-KT, HT-CSKH |
| Duyệt thanh toán hoa hồng (1 hoặc nhiều hợp đồng) | ✅ | chỉ HT-AD, HT-KT |
| Kiểm tra mã giới thiệu khi khách nhập | ✅ | công khai (không cần đăng nhập) |

## Để làm gì / cho ai
- **Chủ shop / Marketing:** khai báo nguồn giới thiệu để biết khách đến từ ai, tính hoa hồng cho người giới thiệu.
- **Quản trị hệ thống (CSKH/Kế toán SaaS):** cấp mã giới thiệu cho đại lý/CTV, theo dõi đơn vị nào đăng ký qua mã, duyệt chi hoa hồng.
- **Người giới thiệu (NGT/đại lý):** tra mã của mình, xem đơn vị/hợp đồng đã giới thiệu được, theo dõi hoa hồng.
- **Khách đăng ký mới:** nhập mã giới thiệu khi đăng ký để gắn với người giới thiệu.

## Cách dùng

**Nguồn giới thiệu (trong shop):**
1. Vào **Marketing → Nguồn giới thiệu → Thêm mới**, nhập thông tin nguồn, lưu.
2. Khi tạo/xử lý đơn hàng, gắn nguồn giới thiệu cho đơn → nếu có cấu hình hoa hồng, hệ thống tính hoa hồng khi thanh toán.

**Mã giới thiệu (quản trị/đại lý):**
1. Admin vào **Hệ thống → Mã giới thiệu → Thêm mới** để cấp mã cho người giới thiệu.
2. Người giới thiệu đăng nhập xem **mã của mình** và **đơn vị/hợp đồng** đã nhập mã.
3. Admin/Kế toán **duyệt thanh toán hoa hồng** cho từng hợp đồng hoặc hàng loạt.

## Cần chú ý
- **Nguồn giới thiệu (shop) và Mã giới thiệu (hệ thống) là 2 thứ khác nhau:** nguồn giới thiệu phục vụ hoa hồng nội bộ shop cho khách lẻ; mã giới thiệu phục vụ chương trình đại lý giới thiệu khách dùng phần mềm SaaS.
- **Duyệt hoa hồng siết quyền hơn:** chỉ admin và kế toán hệ thống được duyệt chi tiền hoa hồng (không mở rộng cho CSKH như các thao tác khác).
- **Đơn phải gắn đúng nguồn giới thiệu + có cấu hình hoa hồng** thì mới phát sinh hoa hồng khi thanh toán.
- **Kiểm tra mã khi đăng ký là công khai** (không cần đăng nhập) — nhập sai mã sẽ không gắn được người giới thiệu.

## Ảnh hưởng tới
- **Đơn hàng** — nguồn giới thiệu trên đơn quyết định có phát sinh hoa hồng hay không.
- **Hoa hồng giới thiệu** — nguồn này là gốc cấu hình & phát sinh hoa hồng khi thanh toán.
- **Đơn vị / Hợp đồng** — mã giới thiệu gắn với đơn vị/hợp đồng đã đăng ký; duyệt hoa hồng cập nhật trạng thái thanh toán theo hợp đồng.
