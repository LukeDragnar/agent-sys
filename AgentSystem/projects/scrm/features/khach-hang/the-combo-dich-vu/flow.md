# Thẻ & combo dịch vụ — Luồng nghiệp vụ

> Dùng cho test / đào tạo. Mô tả luồng end-to-end cấp thẻ → dùng/trừ dần → hết buổi/hết hạn, kèm ví dụ có số.

---

## Tiền điều kiện
- Đơn vị đã tạo **loại thẻ dịch vụ** trong module Kho (định nghĩa: loại thẻ số lần/số tiền, giá bán, số buổi hoặc số tiền, thời hạn, tiền khuyến mãi nếu có).
- Hoặc đã tạo **combo gốc** (gói gồm các dịch vụ và số buổi từng dịch vụ).
- Khách hàng đã có hồ sơ trong hệ thống.
- Tài khoản thao tác có quyền cấp thẻ / bán combo (Admin tổng, Admin chi nhánh, Quản lý dịch vụ...).

---

## Luồng chính — Thẻ số lần (thẻ tích buổi)

### A. Cấp thẻ
1. Người dùng vào **Thẻ khách hàng** → Thêm mới.
2. Chọn loại thẻ + (một hoặc nhiều) khách hàng + ngày cấp + số lượng thẻ cấp.
3. Hệ thống kiểm tra: nếu nhập "số lần đã dùng ban đầu" > số lần của loại thẻ → **báo lỗi, dừng**.
4. Với mỗi khách × mỗi thẻ: sinh **mã thẻ** ngẫu nhiên (prefix THE), tính **ngày hết hạn**, đặt **số buổi còn lại = tổng buổi − buổi đã dùng ban đầu**, trạng thái = **Sử dụng**.
5. Ghi **lịch sử tương tác** (cấp thẻ) vào hồ sơ khách + log Mongo (sự kiện ThemMoi).

### B. Dùng / trừ buổi
1. Lễ tân/thu ngân **lên đơn hàng** cho khách, chọn dịch vụ và **dùng thẻ** của khách để thanh toán.
2. Đơn hoàn tất → hệ thống **trừ số buổi còn lại** của thẻ theo số buổi sử dụng.
3. Ghi **giao dịch dùng thẻ lần** (loại "Dùng thẻ lần"): lưu mã đơn, số buổi trừ, thời điểm.
4. Số buổi còn lại giảm dần qua từng lần dùng.

### C. Kết thúc
- Khi **hết buổi** (số buổi còn lại = 0): thẻ coi như dùng xong (chuyển trạng thái Hoàn thành theo cấu hình).
- Khi **tới ngày hết hạn**: thẻ chuyển Hết hạn, không dùng được nữa.

---

## Luồng chính — Thẻ số tiền (trả trước)
1. Cấp thẻ tương tự, nhưng thẻ mang **số tiền** (và có thể kèm **tiền khuyến mãi**). Kiểm tra: "tiền đã dùng ban đầu" không được > giá trị thẻ; "tiền khuyến mãi đã dùng ban đầu" không được > tiền khuyến mãi.
2. Khi khách mua hàng/dịch vụ và thanh toán bằng thẻ → hệ thống **trừ tiền còn lại** (và trừ phần tiền khuyến mãi nếu áp dụng) theo số tiền của đơn.
3. Ghi giao dịch "Dùng thẻ tiền". Có thể **nạp thêm tiền** vào thẻ (nạp trực tiếp không qua đơn) → ghi giao dịch "Nạp tiền".

---

## Luồng chính — Combo (gói liệu trình spa)
1. **Bán combo** cho khách (Khách hàng-combo): chọn khách + combo gốc → tạo combo-khách, **mỗi dịch vụ trong gói** được ghi nhận với **số buổi (SoLan)** và **số buổi còn lại (SoLanConLai)** riêng.
2. **Dùng:** mỗi lần khách làm 1 dịch vụ trong gói → trừ buổi của **đúng dịch vụ đó** (SoLanConLai của dòng dịch vụ giảm). Có thể tác động 1 dịch vụ hoặc nhiều dịch vụ cùng lúc.
3. Lọc nhanh combo theo **số buổi còn lại** (=, ≥, ≤ một mức) để biết khách nào sắp hết liệu trình.

---

## Case biên

| Tình huống | Hệ thống xử lý |
|---|---|
| Nhập "đã dùng ban đầu" > số buổi/tiền/tiền KM của loại thẻ | Báo lỗi, **không cấp thẻ** |
| Cấp thẻ nhưng chưa chọn khách / chưa chọn loại thẻ / chưa có ngày cấp | Báo lỗi từng trường, dừng |
| Thẻ **hết buổi** (còn lại = 0) nhưng còn hạn | Không trừ thêm được; coi như dùng xong |
| Thẻ **còn buổi/tiền** nhưng **quá hạn** | Không dùng được; trạng thái Hết hạn |
| Thẻ đang **Khóa** | Tạm thời không dùng được tới khi mở khóa |
| Thẻ bị **Thu hồi** | Hết hiệu lực |
| Combo: trừ buổi làm số còn lại **< 0** | Chặn, báo lỗi (không cho âm) |
| Combo: cộng trả buổi vượt **quá tổng buổi ban đầu** | Chặn ở mức tổng buổi của dịch vụ |
| **Hủy/hoàn đơn** đã dùng thẻ/combo | Cộng trả lại số buổi/tiền đã trừ vào thẻ/combo |

---

## Ví dụ có số

**Ví dụ 1 — Thẻ số lần:**
- Loại thẻ "Massage 10 buổi", thời hạn 6 tháng tính từ ngày cấp.
- Cấp cho chị A ngày 01/06/2026, đã dùng ban đầu = 0 → thẻ có **10 buổi**, ngày hết hạn **01/12/2026**.
- Chị A đến làm 3 lần (mỗi lần 1 buổi) → còn **7 buổi**.
- Đến 01/12/2026 chị A còn 2 buổi nhưng quá hạn → thẻ **Hết hạn**, 2 buổi không dùng được (trừ khi gia hạn).

**Ví dụ 2 — Thẻ số tiền:**
- Thẻ nạp 5.000.000đ, tặng khuyến mãi 500.000đ → tiền còn lại 5.000.000đ, tiền KM còn lại 500.000đ.
- Khách mua đơn 1.200.000đ thanh toán bằng thẻ → tiền còn lại **3.800.000đ** (KM trừ theo cấu hình tiền khuyến mãi của thẻ).

**Ví dụ 3 — Combo liệu trình:**
- Gói "Trắng da": Soi da 6 buổi + Đắp mặt nạ 6 buổi. Bán cho chị B → mỗi dịch vụ 6 buổi còn lại.
- Buổi 1 chị B làm Soi da + Đắp mặt nạ → Soi da còn **5**, Đắp mặt nạ còn **5**.
- Buổi 2 chị B chỉ làm Đắp mặt nạ → Soi da vẫn **5**, Đắp mặt nạ còn **4**.

---

## Hậu điều kiện
- Số buổi/tiền còn lại của thẻ-combo phản ánh đúng số đã dùng.
- Mỗi lần cấp/dùng/gia hạn/thu hồi đều có **bản ghi lịch sử** (lịch sử tương tác khách + lịch sử giao dịch thẻ + log Mongo).
- Báo cáo **Lịch sử dùng thẻ** thống kê được từng lần trừ theo đơn hàng, theo khoảng thời gian, theo chi nhánh.
- Đơn hủy/hoàn → số dư thẻ/combo được khôi phục đúng giá trị đã trừ.
