# Luồng nghiệp vụ — Điểm thưởng & đổi thưởng

## Tiền điều kiện

- Đơn vị đã có **bộ cấu hình đổi điểm**. Nếu chưa, lần đầu mở màn hình hệ thống tự khởi tạo mặc định: bật cả tích và đổi điểm, `Số tiền đổi được 1 điểm = 10.000`, `1 điểm đổi được số tiền = 10.000`, tất cả loại hàng đều được tích.
- (Nếu dùng tỷ lệ riêng) Đã khai báo **Hạng khách hàng** / **Nhóm khách hàng** trong danh mục.
- Khách hàng tồn tại trong hệ thống và có tài khoản điểm (số điểm hiện tại, số điểm tích lũy).

## Luồng cấu hình (set tỷ lệ tích / đổi)

1. Quản lý mở **Khách hàng → Cấu hình đổi điểm**.
2. Hệ thống gọi lấy cấu hình hiện có (`GetCauHinhDoiDiem`). Nếu chưa có → tự khởi tạo rồi trả về bản mặc định.
3. Quản lý bật/tắt **Cho phép tích điểm**, **Cho phép đổi điểm**.
4. Nhập **Số tiền đổi được 1 điểm** (tích) và **1 điểm đổi được số tiền** (đổi) — chỉ hiện khi công tắc tương ứng đang bật.
5. Tick các loại hàng được tích: Dịch vụ / Sản phẩm / Combo / Thẻ / Thu khác.
6. Lưu → cập nhật cấu hình chung, ghi lịch sử tác động và xóa cache cấu hình của đơn vị.
7. (Tùy chọn) Thêm **Quy đổi chi tiết**: chọn hạng hoặc nhóm khách hàng + tỷ lệ riêng + tên → lưu. Có thể sửa/xóa từng dòng. Mỗi thao tác đều xóa cache cấu hình.

## Luồng dùng (mua → tích điểm; đổi điểm)

**Tích điểm khi mua:**
1. Khách mua hàng, thu ngân thanh toán đơn.
2. Hệ thống đọc cấu hình đổi điểm của đơn vị (qua cache).
3. Với mỗi dòng đơn hàng, cộng dồn số tiền của các loại hàng **được bật tích điểm** (bỏ phần trả bằng điểm và thẻ tiền mặt). Tổng tiền tích điểm không vượt tổng thanh toán của đơn.
4. Quy ra điểm = `floor(tổng tiền tích điểm / tỷ lệ tiền→điểm)`. Nếu khách thuộc hạng/nhóm có quy đổi chi tiết → dùng tỷ lệ riêng.
5. Cộng điểm vào **số điểm hiện tại** và **số điểm tích lũy** của khách, ghi lịch sử (loại TichDiem).

**Đổi điểm (thanh toán bằng điểm):**
1. Khách chọn trả một phần/toàn bộ bằng phương thức **Điểm**.
2. Hệ thống lấy số tiền trả bằng điểm; kiểm tra không vượt giá trị đơn.
3. Quy ra điểm cần trừ = `số tiền trả bằng điểm / tỷ lệ điểm→tiền`.
4. Kiểm tra khách đủ điểm; nếu đủ → trừ vào **số điểm hiện tại**, ghi lịch sử (loại DoiDiem).

## Case biên

- **Chưa có cấu hình khi thanh toán** → báo "Chưa cấu hình đổi điểm!" (không tích/đổi được).
- **Bật đổi điểm nhưng chưa nhập tỷ lệ điểm→tiền (hoặc < 1)** → báo "Chưa cấu hình một điểm đổi được số tiền!".
- **Khách đổi điểm nhưng không đủ điểm** → báo "Số điểm hiện tại của khách hàng không đủ!".
- **Số tiền trả bằng điểm > giá trị đơn** → báo "Số điểm sử dụng không được vượt quá giá trị đơn hàng!".
- **Tỷ lệ < 1 hoặc để trống khi đang bật** → chặn ở cả FE và BE.
- **Thêm quy đổi chi tiết trùng** (cùng hạng + nhóm đã tồn tại) → báo trùng, không cho lưu.
- **Combo có quà tặng trừ điểm tích lũy**: nếu điểm tích được của đơn không đủ bù phần trừ, phần thiếu bị trừ trực tiếp vào số điểm hiện tại của khách (clamp tại 0).

## Ví dụ có số

Cấu hình: `Số tiền đổi được 1 điểm = 10.000`, `1 điểm đổi được số tiền = 10.000`, bật tích cho Dịch vụ và Sản phẩm.

- Khách A mua dịch vụ 300.000₫ + sản phẩm 200.000₫ = 500.000₫, trả hết bằng tiền mặt.
  → Tích `floor(500.000 / 10.000)` = **50 điểm**. Số điểm hiện tại và tích lũy +50.
- Khách A sau đó có 50 điểm, mua đơn 350.000₫ và chọn trả 100.000₫ bằng điểm.
  → Cần trừ `100.000 / 10.000` = **10 điểm**. Khách đủ (50 ≥ 10) → trừ 10, còn 40 điểm. Đồng thời phần còn lại 250.000₫ trả tiền mặt vẫn tích thêm `floor(250.000/10.000)` = 25 điểm (nếu đơn thuộc loại được tích).

## Hậu điều kiện

- Tài khoản điểm khách được cập nhật: số điểm hiện tại (+ tích, − đổi), số điểm tích lũy (+ tích).
- Lịch sử điểm thưởng của khách ghi nhận từng đơn (tích/đổi, gắn IdDonHang).
- Cache cấu hình đổi điểm của đơn vị bị xóa sau mỗi lần sửa cấu hình → lần dùng kế tiếp đọc lại bản mới nhất.
- Cấu hình mới chỉ tác động đơn phát sinh sau thời điểm lưu.
