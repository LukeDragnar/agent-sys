# Shopee — Quản lý yêu cầu Hủy đơn (Seller Guide)

> Nguồn: Shopee Uni Help Center — `https://help.shopee.vn/portal/4/article/...` (article ngày 26-11-2024)
> Đã copy verbatim từ user 2026-06-01 để team SCRM tham khảo design parity.

## 1. Truy cập trang Đơn hủy

- **Kênh Người Bán (Web)**: trang `Trả hàng/Hoàn tiền/Hủy` → mục **Tất cả** hoặc **Đơn Hủy**
- **Ứng dụng Shopee (Mobile)**: trang `Đã hủy` dưới phần **Đơn hàng**

## 2. Phân loại các yêu cầu hủy đơn

### Trên Kênh Người Bán (Web) — **3 mục**:
1. **Tất cả**
2. **Shopee đang xem xét**
3. **Đã hoàn tiền cho Người mua**

### Trên Ứng dụng Shopee (Mobile) — **4 mục**:
1. **Tất cả**
2. **Chờ phản hồi**
3. **Giữ lại kiện hàng**
4. **Đã hủy**

### Nghĩa nghiệp vụ mỗi mục:

| Mục | Đơn hàng nào |
|---|---|
| **Tất cả** | Bao gồm tất cả các đơn hàng đã bị hủy vì bất kỳ lý do gì |
| **Thương lượng với Người mua / Chờ phản hồi** | Đơn cần Người bán phản hồi — sort theo deadline (sắp hết hạn lên đầu) |
| **Giữ lại kiện hàng** | Đơn KHÔNG cần Người bán xử lý yêu cầu hủy đơn |
| **Đã hoàn tiền cho Người mua / Đã hủy** | Đơn đã hủy thành công + bị hủy tự động bởi hệ thống Shopee |

## 3. Logic ĐVVC quyết định flow

### Đơn vận chuyển bởi **SPX Express** (Shopee Express):
- Buyer hủy **TRƯỚC** khi đơn được bàn giao cho ĐVVC:
  → Yêu cầu hủy đơn xử lý **ngay lập tức**
  → Hiển thị trong `Đơn hủy > Đã hoàn tiền cho Người mua`
- Đơn không cần Người bán xử lý:
  → Hành động quan trọng = **Giữ lại kiện hàng**

### Đơn vận chuyển bởi **ĐVVC khác SPX Express**:
- Buyer hủy **TRƯỚC** khi Người bán nhấn `Chuẩn bị hàng`:
  → Auto hủy ngay lập tức
  → Hiển thị `Đơn hủy > Đã hoàn tiền cho Người mua`
- Buyer hủy **SAU** khi Người bán nhấn `Chuẩn bị hàng`:
  → Hiển thị `Đơn hủy > Thương lượng với Người mua`
  → Là yêu cầu Người bán cần xử lý

## 4. Hành động quan trọng tại tab Đơn Hủy

Theo screenshot Kênh Người Bán hiện tại (2026-06-01):
- **Thương lượng với Người mua** — đơn cần phản hồi
- **Giữ lại kiện hàng (N)** — đơn không cần seller xử lý, chỉ cần giữ kiện

## 5. Lưu ý quan trọng (banner cảnh báo)

Khi đơn bị hủy sau khi Người bán đã nhấn `Chuẩn bị hàng`:
> ⚠️ "1 đơn hàng bị hủy sau khi bạn nhấn Chuẩn bị hàng. Vui lòng giữ lại kiện hàng và không bàn giao cho đơn vị vận chuyển."

→ FE SCRM hiển thị banner đỏ tương tự khi count `GiuLaiKienHang > 0`.

## 6. References

- Quản lý yêu cầu hủy đơn TRƯỚC khi đơn được bàn giao cho ĐVVC: https://help.shopee.vn (link "TẠI ĐÂY" trong guide gốc)
- Quản lý yêu cầu hủy đơn khi hàng đang trong quá trình vận chuyển: https://help.shopee.vn (link "TẠI ĐÂY" trong guide gốc)
- Lọc theo Đơn vị vận chuyển trên Mobile App: filter built-in

## 7. Mapping sang SCRM (đa kênh — generic)

| Shopee mục | SCRM sub-tab cấp 2 | Status code tham chiếu |
|---|---|---|
| Tất cả | TatCa | (no filter) |
| Shopee đang xem xét | SanDangXemXet | `JUDGING` |
| Đã hoàn tiền cho Người mua | DaHoanTienChoNguoiMua | `CLOSED` |

| Shopee action | SCRM action | Logic |
|---|---|---|
| Thương lượng với Người mua | `ThuongLuongVoiNguoiMua` | status JUDGING + chưa response |
| Giữ lại kiện hàng (N) | `GiuLaiKienHang` | đơn hủy + đã có vận đơn (MaVanDonHoan not null) |

**SCRM phải generic** — không hardcode "Shopee/SPX Express". Logic ĐVVC quyết định flow là partner-specific, ECOMM layer xử lý + forward enum status. SCRM UI generic display.
