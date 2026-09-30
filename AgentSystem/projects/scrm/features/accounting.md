# Accounting — Kế Toán

> Tạo chứng từ kế toán, bút toán, thu chi, bù trừ công nợ. Nhận data từ các service khác qua message broker.

---

## Vai trò trong hệ thống

- ← **Order/OrderLogistic**: thanh toán → PhieuBanHang + ThuChi
- ← **WarehouseProduct**: nhập/xuất kho → PhieuNhap/PhieuXuat
- ← **Customer**: bù trừ công nợ
- → **MISA** (đối tác kế toán): đồng bộ chứng từ
- Toàn bộ nhận qua **DongBoPhieuCrmConsumer (RabbitMQ)**

---

## Entities chính

### PhieuBanHang
| Field | Ý nghĩa |
|-------|---------|
| `TongTienThu` | Tổng tiền phải thu |
| `IdTinhTrangHoaDon` | Chưa xuất / Đã xuất / Bị hủy |
| `IdTinhTrangThanhToan` | Chưa / Đã / Một phần |
| `SinhButToanSuccess/MessErr` | Kết quả sinh bút toán |
| `StatusSendPartner` | Trạng thái gửi MISA |
| `IdDoiTuongNo` | Đối tượng công nợ phải thu |
| `IdDoiTuongThuCod` | Thu hộ (GHN, Shopee...) |

### ThuChi — xem [branch.md](./branch.md)
(Cùng entity nhưng Accounting có bản riêng với thêm trường bút toán)

### ButToan (bút toán kép)
| Field | Ý nghĩa |
|-------|---------|
| `MaLoaiChungTu` | PT=PhiếuThu, PC=PhiếuChi... |
| `TaiKhoanNo/Co` | Tài khoản Nợ/Có |
| `SoTien/DienGiai` | Số tiền + diễn giải |
| `MaCot` | Cột sinh giá trị (ThanhTien/VAT/ChietKhau...) |

### BuTruCongNo
Khi cùng đối tượng có cả phải thu lẫn phải trả:
→ Tạo bút toán Debit TK phải thu ↔ Credit TK phải trả

### DataReceiveTaoPhieu (tracking sync)
`RECEIVED → INPROCESS → SUCCESS / ERROR`

---

## Luồng

```
Service khác publish DongBoPhieuCrmMessage1
  {Type: "PhieuBanHang"/"ThuChi"/"PhieuNhap"...}
  ↓ RabbitMQ
DongBoPhieuCrmConsumer
  ├→ Save phiếu vào DB
  ├→ ButToanService.SinhButToan() — dựa trên TaiKhoanNgamDinh
  ├→ Save DataReceiveTaoPhieu (tracking)
  └→ Nếu cấu hình MISA → publish AccountingReceiveMessage
```

---

## Checklist fix bug

- [ ] Bút toán không sinh → xem `SinhButToanMessErr`, kiểm tra `TaiKhoanNgamDinh`
- [ ] Không sync MISA → xem `DataReceiveTaoPhieu.TrangThai`, kiểm tra `CauHinhKetNoiKeToan`
- [ ] Phiếu trùng → kiểm tra `IdTransaction` idempotency trong `DataReceiveTaoPhieu`
