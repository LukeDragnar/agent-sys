# Shopee — Xử lý đơn Giao không thành công (Seller Guide)

> Nguồn: Shopee Uni Help Center (article 15-08-2025).
> Copy verbatim từ user 2026-06-01.

## A. Giao không thành công là gì?

Giao không thành công xảy ra khi ĐVVC không thể giao hàng cho Người mua, với lý do:
- **Đơn giao không thành công**: Người mua từ chối nhận hàng, địa chỉ không chính xác, hoặc nhiều lần liên hệ giao hàng không được
- **Đơn hủy trên đường giao**: Người mua hủy đơn khi đơn đang trong quá trình vận chuyển

Với ĐVVC **SPX Express**, khi shipper đến hoàn hàng, Người bán sẽ ký xác nhận trên app sau khi kiểm tra đủ số lượng đơn hoàn, biên bản hoàn hàng điện tử (e-receipt) gửi về email Người bán đã đăng ký.

## B. Theo dõi đơn giao không thành công

### Kênh Người Bán (Web):
- Trang **Trả hàng/Hoàn tiền/Hủy**:
  - **Đơn giao không thành công** → mục `Đơn Giao hàng không thành công`
  - **Đơn hủy trên đường giao** → mục `Đơn Trả hàng/Hoàn tiền`

### Ứng dụng Shopee (Mobile):
- Màn hình **Đơn Hàng**:
  - **Đơn giao không thành công** → mục `Giao không thành công`
  - **Đơn hủy trên đường giao** → mục `Trả hàng/Hoàn tiền`

## C. Quy trình xử lý

### 1. Trường hợp Người bán ĐÃ nhận được hàng hoàn

Hệ thống cập nhật trong **24h** kể từ khi nhận hàng. Sau thời gian này nếu chưa update → liên hệ CSKH Shopee.

#### 1.1. Hàng hoàn nguyên vẹn:
- Kiểm tra hàng + bấm xác nhận **"Nhận hàng trong tình trạng nguyên vẹn"** (nếu không có vấn đề khiếu nại).

#### 1.2. Hàng hoàn hư hỏng/bể vỡ/sai/thiếu/rỗng:

| Kênh vận chuyển | Deadline khiếu nại | Yêu cầu bằng chứng |
|---|---|---|
| **Hỏa Tốc / Hỏa Tốc Ưu Tiên** (SP có hạn SD ngắn) | 24h kể từ khi nhận hàng hoàn | Hình ảnh/video thể hiện rõ ngày/tháng trong vòng 24h |
| **Các trường hợp khác** | 7 ngày kể từ khi nhận hàng hoàn | Theo hướng dẫn standard |

**Bước khiếu nại (Đơn giao không thành công):**
1. Trang `Trả hàng/Hoàn tiền/Hủy` → chọn `Đơn Giao hàng không thành công`
2. Chọn đơn → bấm **"Khiếu nại"**
3. Điền đầy đủ thông tin + bấm **"Gửi"**
4. Bấm **"Xem chi tiết"** theo dõi kết quả (Shopee xét **3-5 ngày làm việc**)

**Đơn hủy trên đường giao**: vào `Đơn Trả hàng/Hoàn tiền` → các bước tương tự.

### 2. Trường hợp Người bán CHƯA nhận được hàng hoàn

Hàng được hoàn về trong **4 ngày làm việc** kể từ khi ĐVVC ghi nhận hoàn về. Sau thời gian này nếu chưa nhận → liên hệ CSKH.

#### 2.1. Hệ thống cập nhật ĐÃ hoàn thành công nhưng Người bán chưa nhận hàng:
- Bấm **"Khiếu nại"** + điền thông tin trong vòng **7 ngày** kể từ khi hệ thống update.

#### 2.2. Hoàn hàng KHÔNG thành công:

**Kiện hàng không thể hoàn về**:
- Lý do: không có người nhận, địa chỉ trả hàng không chính xác, nhiều lần liên hệ không được
- Người bán liên hệ ĐVVC **30 ngày** (Hỏa tốc/Hỏa Tốc Ưu Tiên: **21 ngày**) để lên lịch nhận lại
- Sau thời gian này → kiện hàng bị **tiêu hủy**

**Kiện hàng thất lạc khi đang hoàn về**:
- Kiểm tra thông báo + theo dõi đền bù từ Shopee
- Nếu đủ điều kiện bồi thường → tiền bồi thường chuyển vào **Số dư TK Shopee** của Người bán

## D. Mapping sang SCRM (đa kênh — generic)

| Shopee tab/sub-tab | SCRM cấp 1 | SCRM sub-tab cấp 2 |
|---|---|---|
| Đơn Giao hàng không thành công | `GiaoKhongThanhCong` | `TatCa` |
| Đơn Giao hàng không thành công > Đang trả hàng | `GiaoKhongThanhCong` | `DangTraHangChoNguoiBan` |
| Đơn Giao hàng không thành công > Đã trả hàng | `GiaoKhongThanhCong` | `DaHoanTienChoNguoiMua` (đã trả hoàn tiền) |
| Đơn Trả hàng/Hoàn tiền (đơn hủy trên đường giao) | `TraHangHoanTien` | Logic riêng — vẫn ở tab TraHangHoanTien |

| Shopee action | SCRM action |
|---|---|
| Khiếu nại (hư hỏng/sai/thiếu/rỗng) | Dispute API → `Returns/Dispute` |
| Xác nhận "Nhận hàng trong tình trạng nguyên vẹn" | Confirm → `Returns/Confirm` |
| Kiểm tra hàng hoàn | `KiemTraHangHoan` filter (status = `LOGISTICS_DELIVERY_DONE`) |

## E. Deadline rules (timer business)

Field `DeadlineKhieuNai` trên `DonHangHoan`:
- **24h** từ ngày nhận hàng hoàn (Hỏa tốc / Ưu Tiên với SP hạn SD ngắn)
- **7 ngày** từ ngày nhận hàng hoàn (các trường hợp khác)
- **7 ngày** từ ngày hệ thống cập nhật "đã hoàn thành công" (case chưa nhận hàng)

→ FE hiển thị badge "Còn N ngày" theo `getDeadlineDisplay()` đã implement.

## F. References

- Article gốc Shopee Uni: `https://help.shopee.vn/portal/...` (15-08-2025)
- "Quản lý đơn giao không thành công" — link "TẠI ĐÂY" trong guide gốc
