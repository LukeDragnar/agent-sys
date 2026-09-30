---
alias: ["phê duyệt giao dịch tổng đài", "duyệt thanh toán tổng đài", "duyệt nạp tiền tổng đài", "super admin tổng đài", "giao dịch tổng đài SA"]
module: he-thong
trang_thai: cần product xác nhận — dành cho admin/kế toán hệ thống nội bộ SaaS
goi: cần product xác nhận
quyen: "cần product xác nhận — controller trong SanboxArea, TI_HeThong, chỉ role hệ thống HT-AD, HT-KT (super-admin)."
phu_thuoc: ["Tổng đài IP đơn vị (TongDaiIpDonVi) — nơi giao dịch nạp tiền phát sinh và số dư đơn vị được cộng", "Giao dịch tổng đài (GiaoDichTongDai) — bản ghi nạp tiền cần duyệt"]
link_flow: ./flow.md
---

# Phê duyệt giao dịch tổng đài (super-admin)

## Là gì
Đây là màn **nội bộ super-admin** để **duyệt (hoặc hủy duyệt) các giao dịch nạp tiền tổng đài IP của các đơn vị**. Khi một đơn vị nạp tiền để dùng tổng đài (gọi điện qua IP), giao dịch ở trạng thái **chờ thanh toán**; super-admin (admin/kế toán hệ thống) là người **duyệt cuối** — duyệt xong thì giao dịch chuyển sang **đã thanh toán** và **số dư tổng đài của đơn vị được cộng** tương ứng. Màn này cũng cho **xem lịch sử giao dịch** theo đơn vị và **sửa ngày hết hạn** của chi tiết giao dịch.

> Đây KHÔNG phải màn cho khách hàng. Đơn vị tạo yêu cầu nạp tiền ở luồng khác; còn việc **duyệt** là việc của super-admin nhà cung cấp.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem lịch sử giao dịch tổng đài theo đơn vị | ✅ | HT-AD, HT-KT |
| Duyệt thanh toán nạp tiền | ✅ | HT-AD, HT-KT; giao dịch đang chờ thanh toán; phải có Id đơn vị |
| Hủy duyệt thanh toán | ✅ | HT-AD, HT-KT; giao dịch đã duyệt |
| Cập nhật ngày hết hạn chi tiết giao dịch | ✅ | HT-AD, HT-KT |
| Tạo / nhập giao dịch nạp tiền tại màn này | ❌ | màn này chỉ duyệt/quản lý; giao dịch phát sinh từ luồng nạp tiền của đơn vị |

## Để làm gì / cho ai
- **Admin hệ thống (HT-AD):** duyệt giao dịch nạp tiền tổng đài cho các đơn vị, xử lý sai sót bằng hủy duyệt, gia hạn ngày hết hạn.
- **Kế toán hệ thống (HT-KT):** đối chiếu, duyệt thanh toán nạp tiền, theo dõi lịch sử giao dịch của đơn vị.

## Cách dùng
1. Vào **Quản trị hệ thống → Giao dịch tổng đài IP đơn vị** (chỉ super-admin: HT-AD / HT-KT).
2. Chọn đơn vị → xem **lịch sử giao dịch** nạp tiền tổng đài của đơn vị đó.
3. Với giao dịch đang **chờ thanh toán**, bấm **Duyệt** → giao dịch chuyển **đã thanh toán** và **cộng số dư** tổng đài cho đơn vị.
4. Nếu duyệt nhầm, bấm **Hủy duyệt** để chuyển giao dịch về trạng thái **hủy thanh toán**.
5. Mở chi tiết giao dịch để **cập nhật ngày hết hạn** khi cần.

## Cần chú ý
- **Duyệt = cộng tiền thật vào số dư tổng đài của đơn vị** → ảnh hưởng trực tiếp đến quyền dùng dịch vụ của khách. Cần đối chiếu kỹ trước khi duyệt.
- **Duyệt yêu cầu có Id đơn vị:** nếu thiếu sẽ báo "Id đơn vị không được để trống".
- **Hủy duyệt KHÔNG kiểm tra đầu vào chặt như Duyệt:** có thể bị gọi với tham số rỗng (Id đơn vị / Id giao dịch) mà không báo lỗi rõ — đây là known-issue (xem dev.md), cần thao tác cẩn thận.
- **Trạng thái giao dịch là một luồng** (chờ thanh toán → đã thanh toán → hủy thanh toán) — duyệt/hủy duyệt phải đúng trạng thái hiện tại của giao dịch.

## Ảnh hưởng tới
- **Số dư tổng đài của đơn vị:** duyệt → cộng số dư; là cơ sở để đơn vị dùng tổng đài IP.
- **Tổng đài IP đơn vị (TongDaiIpDonVi):** trạng thái thanh toán giao dịch gắn cùng state-machine với luồng nạp tiền của đơn vị.
