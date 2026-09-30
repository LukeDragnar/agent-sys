# Website — Bán Hàng Online

> Website bán hàng cho khách hàng cuối (B2C). Bán combo spa, sản phẩm, thẻ dịch vụ.

---

## Vai trò

- ← Khách hàng truy cập website đặt hàng
- → **WarehouseProduct**: đọc sản phẩm, combo, thẻ dịch vụ
- → **Promotion**: áp khuyến mãi, voucher
- → **Order/OrderLogistic**: tạo đơn sau khi KH đặt thành công
- → **AuthenCustomer**: xác thực khách hàng đăng nhập

---

## Entities chính

### WebsiteDonHang
| Field | Ý nghĩa |
|-------|---------|
| `SoDienThoaiKhach` | SĐT định danh KH |
| `ThanhTien` | Tổng tiền sau giảm giá |
| `LoaiThanhToan` | 1=chuyển khoản, 2=quét thẻ |
| `MaThanhToan` | Mã reference để track thanh toán |
| `IdDonHang` | Liên kết đơn hàng sau khi confirm |
| UTM fields | Tracking nguồn truy cập |

### WebsiteDonHangCombo / SanPham / The
Chi tiết từng item trong đơn — tương tự DonHang bên Order nhưng từ website.

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/GetDataByDomain` | Lấy toàn bộ data theo domain (SP, combo, KM...) |
| `GET /ComboSpa/{id}` | Chi tiết combo spa |
| `GET /TimTheoDieuKien` | Tìm đơn hàng website |

---

## Luồng

```
Khách vào website (domain riêng của đơn vị)
  ↓
HomeController.GetDataByDomain() — load SP/combo/KM
  ↓
Khách chọn + đặt hàng
  ↓
Tạo WebsiteDonHang + chi tiết
  ↓
Thanh toán → WebhookBank xác nhận
  ↓
EventUserSyncDataConsumer → sync sang Order/OrderLogistic
```

---

## Checklist fix bug

- [ ] Website không load data → kiểm tra `Domain` config của đơn vị
- [ ] Giá sai → kiểm tra `WebsiteDonHangCombo.TypeGiamGia/ValueGiamGia`
- [ ] Đơn không tạo sau thanh toán → xem `EventUserSyncDataConsumer` log
