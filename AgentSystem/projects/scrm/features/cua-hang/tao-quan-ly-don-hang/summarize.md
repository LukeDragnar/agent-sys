---
alias: ["bán hàng", "tạo đơn", "lên đơn", "POS", "thu ngân", "tính tiền", "thanh toán đơn", "order", "in hóa đơn", "in bếp", "đang phục vụ", "chuyển bàn"]
module: cua-hang
trang_thai: Core
goi: cần product xác nhận
quyen: "cần product xác nhận — phần lớn thao tác đơn (tạo đơn / thanh toán / xóa…) có phân quyền riêng; màn nhân viên phục vụ yêu cầu vai nhân viên (DV-NV); phát hành hóa đơn điện tử cần bật tiện ích Misa/Viettel. Một số chức năng chỉ yêu cầu đăng nhập (không gắn quyền riêng — xem dev.md)."
phu_thuoc: ["Sản phẩm / Dịch vụ / Combo / Thẻ (chọn vào đơn, trừ tồn)", "Khuyến mãi & Voucher (áp giảm giá)", "Cấu hình mặt hàng mặc định + Khu vực/Chỗ ngồi (POS theo bàn)", "Xếp nhân viên phục vụ (hoa hồng)", "Khách hàng (gắn khách, ghi nợ, ảnh)", "Thu chi / Báo cáo doanh số (chi nhánh)", "Hóa đơn điện tử Misa/Viettel/MeInvoice/SInvoice", "Giao vận (đơn logistic)"]
link_flow: ./flow.md
---

# Tạo & quản lý đơn hàng (bán hàng tại quầy)

## Là gì
Đây là **màn bán hàng tại quầy (POS thu ngân)** — nơi nhân viên/thu ngân tạo một đơn hàng và xử lý trọn vòng đời của nó: chọn bàn và khách → thêm sản phẩm/dịch vụ/combo/thẻ → áp khuyến mãi, voucher, chiết khấu → xếp nhân viên phục vụ → tính tiền → thanh toán → in hóa đơn, in phiếu bếp, phát hành hóa đơn điện tử. Đây là tính năng trung tâm của cả nghiệp vụ cửa hàng.

Một đơn hàng đi qua 2 trạng thái chính: **Chưa thanh toán** (đang phục vụ, còn sửa được) và **Đã thanh toán** (đã chốt tiền). Ngoài thu ngân, màn này còn phục vụ các giao diện "Đang phục vụ" (theo dõi bàn), "Danh sách đơn hàng" (tra cứu/lọc đơn) và màn thao tác của nhân viên phục vụ.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tạo đơn mới | ✅ | quyền tạo đơn; đơn mở ở trạng thái Chưa thanh toán |
| Thêm / sửa / xóa món trong đơn (SP/DV/combo/thẻ) | ✅ | khi đơn còn Chưa thanh toán; trừ/cộng tồn theo cấu hình |
| Áp / xóa voucher, áp khuyến mãi, chiết khấu theo bill | ✅ | khi đơn còn Chưa thanh toán, đủ điều kiện chương trình |
| Xếp nhân viên phục vụ | ✅ | tùy quyền; gán NV hưởng hoa hồng cho từng món có tiền |
| Tính toán lại tiền | ✅ | tự động hoặc bấm nút trước khi thanh toán |
| Thanh toán | ✅ | đơn đang Chưa thanh toán → chuyển sang Đã thanh toán |
| Hủy hóa đơn (hủy thanh toán) | ✅ | đơn Đã thanh toán → quay về Chưa thanh toán |
| **Xóa đơn** | ⚠️ | chỉ khi **Chưa thanh toán**, **không** tạo từ phiếu mua hàng, và đơn vị **không** bật khóa "không cho xóa đơn" |
| Chuyển bàn | ✅ | đơn đang phục vụ |
| In hóa đơn / in bán lẻ / in trực tiếp | ✅ | tự tính lại tiền trước khi in |
| In phiếu bếp / bar (in chế biến) | ✅ | màn nhân viên phục vụ (role DV-NV) |
| Phát hành hóa đơn điện tử | ⚠️ | cần bật tiện ích Misa **hoặc** Viettel (MeInvoice/SInvoice) |
| Nhập đơn từ Excel | ✅ | quyền import đơn |
| Xuất Excel hóa đơn / danh sách đơn | ✅ | tùy quyền |
| Đối soát đơn hàng | ✅ | tùy quyền |

## Để làm gì / cho ai
- **Thu ngân:** tạo đơn, tính tiền, nhận thanh toán, in hóa đơn, phát hành hóa đơn điện tử, đối soát ca.
- **Nhân viên phục vụ (DV-NV):** thêm món cho khách, ghi chú, bắt đầu/kết thúc dịch vụ tính giờ, hoàn thành món, in phiếu bếp.
- **Quản lý / chủ shop:** theo dõi "Đang phục vụ" (bàn nào đang có khách), tra danh sách đơn, kiểm soát hủy/xóa đơn qua cấu hình.

## Cách dùng
**Tạo và xử lý một đơn (thu ngân):**
1. Vào module **Cửa hàng → Thu ngân (POS)**.
2. Bấm **Tạo đơn**: chọn bàn/chỗ ngồi và khách (nếu có) — đơn mở ở trạng thái Chưa thanh toán.
3. **Thêm món**: chọn sản phẩm/dịch vụ/combo/thẻ vào đơn. Có thể sửa số lượng, sửa giá (tùy quyền), xóa dòng.
4. **Áp ưu đãi**: áp voucher, để hệ thống áp khuyến mãi, hoặc kiểm tra chiết khấu theo bill.
5. **Xếp nhân viên** phục vụ cho các món có tiền (để chia hoa hồng).
6. Bấm **Tính toán lại** để chốt tổng tiền, rồi **Yêu cầu thanh toán** (dừng dịch vụ tính giờ, sinh mã QR/thanh toán online nếu có).
7. Bấm **Thanh toán**, chọn phương thức (tiền mặt, chuyển khoản, ví, thẻ…) → đơn chuyển sang **Đã thanh toán**.
8. **In hóa đơn**, và (nếu cần) **Phát hành hóa đơn điện tử**; **in phiếu bếp** cho khu chế biến.

**Theo dõi & quản lý đơn:**
- Màn **Đang phục vụ**: xem các bàn đang có khách, chuyển bàn.
- Màn **Danh sách đơn hàng**: lọc theo điều kiện, mở chi tiết, xuất Excel, đối soát.

## Cần chú ý
- **Đơn đã thanh toán thì không xóa được** — phải **Hủy hóa đơn** trước để đưa về Chưa thanh toán, rồi mới xóa (nếu đủ điều kiện).
- **Một số đơn vị khóa quyền xóa đơn** (cấu hình "không cho phép xóa đơn hàng"). Khi đó kể cả đơn Chưa thanh toán cũng không xóa được. Đây là cấu hình, không phải lỗi.
- **Đơn tạo từ phiếu mua hàng (thu cũ đổi mới) không được xóa.**
- **Phát hành hóa đơn điện tử** chỉ làm được khi đơn vị đã bật tiện ích Misa hoặc Viettel; nếu chưa bật, nút sẽ báo lỗi/không khả dụng.
- **Trừ tồn kho**: thêm món có thể giữ/trừ tồn ngay hoặc chỉ trừ khi thanh toán — tùy cấu hình đơn vị.
- Khi **xóa đơn có đặt cọc**, hệ thống sẽ hoàn lại khoản thu chi (cọc); nếu bước hoàn cọc lỗi thì việc xóa cũng dừng lại.
- Màn hiển thị bàn công khai và link đánh giá đơn là **công khai** (không cần đăng nhập) — xem mục Cần chú ý ở `flow.md`.

## Ảnh hưởng tới
- **Tồn kho** (trừ/cộng khi thêm món, thanh toán, xóa đơn).
- **Khuyến mãi / Voucher / CTV / Điểm thưởng** — thanh toán đơn sẽ đồng bộ sang chiến dịch marketing (tích điểm, hoa hồng CTV).
- **Thu chi & Báo cáo doanh số / hoa hồng nhân viên** (chi nhánh) — đơn đã thanh toán mới vào báo cáo.
- **Hóa đơn điện tử** (Misa/Viettel/MeInvoice/SInvoice) khi phát hành.
- **Giao vận** (đơn logistic), **bàn/đèn bàn** (đặt lịch/đặt bàn), **khách hàng** (ghi nợ, ảnh, contact).
