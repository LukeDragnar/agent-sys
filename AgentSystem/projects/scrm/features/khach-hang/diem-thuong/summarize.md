---
alias:
  - "tích điểm"
  - "điểm thưởng khách hàng"
  - "đổi điểm lấy quà"
  - "cấu hình đổi điểm"
  - "tích điểm khi mua hàng"
module: khach-hang
man_hinh: "Cấu hình đổi điểm"
trang_thai: "đang dùng"
goi: "cần product xác nhận"
quyen:
  - "Quyền cấu hình: DV-AD-TONG (xem/sửa tỷ lệ tích–đổi)"
  - "Quyền thêm/sửa/xóa quy đổi chi tiết theo hạng/nhóm: DV-AD-TONG, DV-QLDV"
  - "(các vai trò cụ thể khác cần product xác nhận)"
phu_thuoc:
  - "Hạng khách hàng (DM Hạng khách hàng)"
  - "Nhóm khách hàng (DM Nhóm khách hàng)"
  - "Đơn hàng / Thanh toán (nơi điểm được tích và trừ)"
  - "Phương thức thanh toán 'Điểm' (để đổi điểm khi thanh toán)"
---

# Điểm thưởng & đổi thưởng

## Là gì

Là tính năng cho phép mỗi đơn vị (spa/cửa hàng) tự **cấu hình quy tắc tích điểm và đổi điểm** cho khách hàng:

- **Tích điểm**: khi khách mua hàng/dùng dịch vụ và thanh toán, hệ thống quy đổi số tiền đã chi thành điểm cộng vào tài khoản của khách.
- **Đổi điểm**: khách dùng điểm tích lũy để thanh toán (đổi điểm lấy chiết khấu/quà), mỗi điểm tương ứng một số tiền.

Toàn bộ quy tắc nằm ở màn hình **Cấu hình đổi điểm**. Mỗi đơn vị có **một bộ cấu hình chung** (bật/tắt tích điểm, bật/tắt đổi điểm, tỷ lệ tiền↔điểm, loại hàng nào được tích) và có thể thêm **các quy đổi chi tiết** áp riêng cho từng hạng hoặc nhóm khách hàng.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem / sửa cấu hình đổi điểm (tỷ lệ tích–đổi) | ✅ | DV-AD-TONG (yêu cầu bản ghi đã tồn tại; không có thì phải Khởi tạo) |
| Khởi tạo cấu hình mặc định | ✅ | tạo bộ mặc định cho đơn vị (mặc định 10.000₫ = 1 điểm) |
| Thêm / Sửa / Xóa quy đổi chi tiết theo hạng/nhóm | ✅ | DV-AD-TONG, DV-QLDV |
| Xem lịch sử điểm thưởng của 1 khách | ✅ | nhiều role nghiệp vụ (AD/QLDV/KT/NV/SALE/MKT…); chỉ đọc log |
| **Cộng / trừ điểm thủ công ở màn này** | ❌ | điểm chỉ được tích/trừ tự động qua luồng thanh toán, không nhập tay ở đây |
| Sửa điểm đã ghi lịch sử | ❌ | lịch sử điểm thưởng là read-only |

## Để làm gì / cho ai

- **Cho chủ/quản lý đơn vị**: thiết lập chương trình khách hàng thân thiết — thưởng điểm để giữ chân khách, cho khách dùng điểm như một hình thức ưu đãi.
- **Cho thu ngân**: tại quầy thanh toán, hệ thống tự tính điểm tích được và cho phép khách trả bằng điểm theo đúng cấu hình.
- **Cho khách hàng cuối**: tích lũy điểm qua mỗi lần mua, đổi điểm lấy giá trị khi thanh toán.

## Cách dùng

1. Vào **Khách hàng → Cấu hình đổi điểm**.
2. Bật **Cho phép tích điểm** và/hoặc **Cho phép đổi điểm**.
3. Khai báo tỷ lệ:
   - **Số tiền đổi được 1 điểm** (tích điểm): ví dụ 10.000₫ chi tiêu = 1 điểm.
   - **1 điểm đổi được số tiền** (đổi điểm): ví dụ 1 điểm = 10.000₫ khi thanh toán.
4. Chọn **loại hàng nào được tích điểm**: Dịch vụ, Sản phẩm, Combo, Thẻ, Thu khác.
5. (Tùy chọn) Thêm **quy đổi chi tiết** theo hạng/nhóm khách hàng để áp tỷ lệ riêng (ví dụ khách hạng VIP tích điểm với tỷ lệ tốt hơn).
6. Lưu cấu hình. Từ đó mọi đơn thanh toán sẽ tự áp dụng.

## Cần chú ý

- Lần đầu mở màn hình, nếu đơn vị **chưa có cấu hình**, hệ thống tự khởi tạo một bộ mặc định (bật cả tích và đổi điểm, mặc định 10.000₫ = 1 điểm và 1 điểm = 10.000₫). Nên kiểm tra và chỉnh lại đúng chính sách của đơn vị.
- Tỷ lệ phải **≥ 1**; nếu bật tích/đổi điểm thì trường tỷ lệ tương ứng **không được để trống**.
- Quy đổi chi tiết: mỗi cặp hạng/nhóm khách hàng chỉ được khai **một** dòng (không trùng).
- Khi khách thanh toán một phần bằng điểm, **số tiền trả bằng điểm không được vượt quá giá trị đơn hàng**.
- Sửa cấu hình có hiệu lực cho các đơn **sau** thời điểm lưu; đơn đã thanh toán trước đó không bị thay đổi.

## Ảnh hưởng tới

- **Thanh toán đơn hàng**: quyết định khách được tích bao nhiêu điểm và đổi điểm ra bao nhiêu tiền.
- **Tài khoản điểm của khách hàng** (số điểm hiện tại, số điểm tích lũy): bị cộng/trừ theo mỗi giao dịch.
- **Hạng/Nhóm khách hàng**: nếu có quy đổi chi tiết, tỷ lệ tích điểm thay đổi theo hạng/nhóm.
- **Lịch sử điểm thưởng** của khách (tích/đổi từng đơn).
