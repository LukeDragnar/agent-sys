# CommissionFee — Hoa Hồng Cộng Tác Viên

> Tính toán và quản lý hoa hồng cho cộng tác viên (CTV) dựa trên đơn hàng. Hỗ trợ nhiều loại hoa hồng: liên kết, sản phẩm, doanh số, chiến dịch.

---

## Vai trò

- ← **Order**: publish `CommissionFeeUpdateOrderDto` khi có đơn mới
- ← **Order**: publish `EventRollbackCommissionFeeOrder` khi đơn bị hủy
- → **MongoDB**: lưu history tính hoa hồng
- → **Redis**: cache cấu hình hoa hồng

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `CongTacVien` | CTV: `MaCTV`, `TenCTV`, `SoDienThoaiCTV`, `IdNhomHoaHongCTV`, `KieuLoai` |
| `NhomHoaHongCTV` | Nhóm HH: cờ loại (LienKet/SanPham/DoanhSo/ChienDich) |
| `ChienDichHoaHongCTV` | Chiến dịch HH: `TuNgay`, `DenNgay`, `IsSuDung` |
| `CauHinhHoaHongSanPhamCTV` | HH theo từng sản phẩm |
| `CauHinhHoaHongDoanhSoCTV` | HH theo doanh số (bậc thang) |
| `CauHinhHoaHongChienDichCTV` | HH theo chiến dịch |
| `DonHang` | Bản sao đơn hàng: `IdCongTacVien`, `IdNhomHoaHongCTV`, `IdTransaction` |
| `DonHangChiTiet` | Chi tiết đơn: giá, số lượng, chiết khấu, VAT |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/CommissionFeeUpdateOrder/CommissionFeeUpdateOrder` | Tính hoa hồng cho đơn hàng |
| `POST /api/CommissionFeeUpdateOrder/CommissionFeeRollBackOrder` | Rollback hoa hồng |
| CRUD `/api/CongTacVien/...` | Quản lý cộng tác viên |
| CRUD `/api/NhomHoaHongCTV/...` | Quản lý nhóm hoa hồng |
| CRUD `/api/ChienDichHoaHongCTV/...` | Quản lý chiến dịch hoa hồng |

---

## Luồng

```
Order tạo đơn có IdCongTacVien
  ↓ publish CommissionFeeUpdateOrderDto
EventSyncCommissionFeeOrderConsumer
  ↓
CommissionFeeUpdateOrderService.CommissionFeeUpdateOrder()
  ├→ Lấy CongTacVien + NhomHoaHongCTV
  ├→ Tính HH theo loại: LienKet / SanPham / DoanhSo / ChienDich
  ├→ Lưu kết quả vào DB + MongoDB (history)
  └→ Cache cấu hình Redis

[Rollback khi đơn bị hủy]
EventRollbackCommissionFeeOrderConsumer
  └→ CommissionFeeRollBackOrder() — đảo ngược HH đã tính
```

---

## Logic hoa hồng

- **LienKet**: HH cố định theo tỷ lệ nhóm
- **SanPham**: HH riêng từng sản phẩm (`CauHinhHoaHongSanPhamCTV`)
- **DoanhSo**: bậc thang theo tổng doanh số trong kỳ
- **ChienDich**: HH theo chiến dịch có thời hạn (`TuNgay` — `DenNgay`)

---

## Checklist fix bug

- [ ] HH không tính → kiểm tra `EventSyncCommissionFeeOrderConsumer`, xem đơn có `IdCongTacVien` chưa
- [ ] HH sai loại → kiểm tra `NhomHoaHongCTV` cờ loại (LienKet/SanPham/DoanhSo/ChienDich)
- [ ] HH chiến dịch không áp → kiểm tra `ChienDichHoaHongCTV.IsSuDung` và ngày hiệu lực
- [ ] Rollback sai → xem history MongoDB
