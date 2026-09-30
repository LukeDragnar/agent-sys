---
alias: ["mã giới thiệu", "referral", "code giới thiệu", "người giới thiệu", "NGT", "hoa hồng giới thiệu", "đại lý giới thiệu", "mã đại lý"]
module: he-thong
trang_thai: Core (nội bộ super-admin + người giới thiệu)
quyen: "Góc Admin: HT-AD (admin hệ thống), HT-KT (kế toán), HT-CSKH — tạo/sửa/xóa/lọc/đổi trạng thái/duyệt hoa hồng. Góc NGT (người giới thiệu): HT-NGT — chỉ xem mã của chính mình. Duyệt thanh toán hoa hồng siết hơn: chỉ HT-AD, HT-KT. Có 1 endpoint public (anonymous) để kiểm tra mã khi đăng ký."
goi: cần product xác nhận
phu_thuoc: ["Đơn vị & hợp đồng — mã giới thiệu gắn vào đơn vị/hợp đồng đã nhập mã", "Kế toán hoa hồng — duyệt thanh toán hoa hồng cho người giới thiệu"]
link_flow: ./flow.md
---

# Mã giới thiệu đại lý

## Là gì
Đây là nơi **quản lý mã giới thiệu (referral)** dùng cho chương trình cộng tác viên/đại lý giới thiệu khách hàng mới cho SaaS. Mỗi **người giới thiệu (NGT)** có một hoặc nhiều **mã giới thiệu**; khi một đơn vị/hợp đồng mới đăng ký và **nhập mã** này, NGT được hưởng **chiết khấu/hoa hồng** theo cấu hình của mã (phần trăm chiết khấu, phần trăm hoa hồng, số tháng cộng thêm, số lần áp dụng…). Admin hệ thống/kế toán theo dõi mã, tra theo đơn vị/hợp đồng đã dùng mã, bật/tắt mã và **duyệt thanh toán hoa hồng** cho NGT.

Có **2 góc nhìn**:
- **Admin** (HT-AD): quản lý toàn bộ mã, duyệt hoa hồng.
- **Người giới thiệu (NGT)**: chỉ xem các mã của chính mình và đơn vị/hợp đồng đã dùng mã đó.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm mã giới thiệu | ✅ | HT-AD, HT-KT, HT-CSKH (NGT **không** tự tạo mã) |
| Sửa mã giới thiệu | ✅ | HT-AD, HT-KT, HT-CSKH |
| Xóa mã giới thiệu | ✅ | HT-AD, HT-KT, HT-CSKH |
| Lọc/tìm mã (góc Admin) | ✅ | HT-AD, HT-KT, HT-CSKH |
| Xem mã của mình (góc NGT) | ✅ | HT-NGT |
| Tra đơn vị / hợp đồng đã dùng mã | ✅ | có biến thể User (HT-NGT) và Admin (HT-AD) |
| Bật/tắt trạng thái sử dụng mã | ✅ | HT-AD, HT-KT, HT-CSKH |
| **Duyệt thanh toán hoa hồng** (1 hợp đồng) | ✅ | chỉ HT-AD, HT-KT (siết hơn) |
| **Duyệt hoa hồng hàng loạt** | ✅ | chỉ HT-AD, HT-KT |
| Kiểm tra mã giới thiệu khi nhập (public) | ✅ | không cần đăng nhập (anonymous) |

## Để làm gì / cho ai
- **Admin hệ thống / kế toán:** vận hành chương trình giới thiệu — cấp mã, theo dõi hiệu quả (số đơn vị/hợp đồng, số tiền hoa hồng), duyệt chi hoa hồng.
- **Người giới thiệu (đại lý/CTV):** xem mã của mình, theo dõi đơn vị/hợp đồng đã dùng mã và hoa hồng phát sinh.
- **Khách mới đăng ký:** nhập mã giới thiệu để được ưu đãi và ghi nhận nguồn giới thiệu.

## Cách dùng
**Góc Admin:**
1. Vào **Quản trị → Mã giới thiệu** (góc Admin).
2. Lọc theo từ khóa / người giới thiệu / trạng thái sử dụng → **Tìm kiếm**.
3. **Thêm mới** mã (chọn NGT, đặt % chiết khấu, % hoa hồng, số tháng cộng thêm, số lần áp dụng cho đơn vị/NGT…), **Sửa**, **Xóa**.
4. Bật/tắt **Trạng thái sử dụng** mã ngay trên dòng.
5. Vào tra **đơn vị/hợp đồng** đã dùng mã; với hợp đồng đủ điều kiện → **Duyệt thanh toán hoa hồng** (đơn lẻ hoặc hàng loạt).

**Góc NGT:**
1. Vào màn Mã giới thiệu (góc NGT) → xem danh sách mã của mình.
2. Xem đơn vị/hợp đồng đã dùng mã và hoa hồng tương ứng.

## Cần chú ý
- **Phân quyền không đối xứng:** hầu hết thao tác mở cho HT-AD/HT-KT/HT-CSKH, riêng **duyệt thanh toán hoa hồng** chỉ HT-AD/HT-KT (đúng ý đồ siết khâu chi tiền).
- **NGT không tự tạo mã** — chỉ Admin/KT/CSKH tạo; NGT chỉ xem.
- Có **endpoint public** để kiểm tra mã khi đăng ký (không cần đăng nhập) — phục vụ luồng nhập mã của khách mới.
- Số tiền hoa hồng, số tiền hợp đồng (ban đầu/sau chiết khấu) hiển thị để theo dõi — duyệt hoa hồng ảnh hưởng tới chi trả thực tế cho NGT.

## Ảnh hưởng tới
- **Đơn vị & hợp đồng:** mã giới thiệu được tham chiếu khi đăng ký; chiết khấu/ưu đãi áp theo cấu hình mã.
- **Kế toán hoa hồng:** duyệt thanh toán hoa hồng cập nhật trạng thái thanh toán theo hợp đồng.
