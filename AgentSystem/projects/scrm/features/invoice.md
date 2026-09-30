# Invoice — Hóa Đơn Điện Tử

> Phát hành hóa đơn điện tử qua MISA hoặc Viettel. Nhận trigger từ Order/OrderLogistic.

---

## Vai trò trong hệ thống

- ← **Order/OrderLogistic**: gọi `CreateInvoice` sau khi thanh toán
- → **MISA / Viettel**: gửi HTTP request phát hành HĐ
- → **Accounting**: cập nhật `IsDaXuatHoaDon` trên PhieuBanHang
- Toàn bộ xử lý **async qua MassTransit**

---

## Entities chính

### InvoiceInfo
| Field | Ý nghĩa |
|-------|---------|
| `InvNo/InvCode/InvTemplateCode/InvDate` | Số, ký hiệu, mẫu, ngày HĐ |
| `TransactionID/Uuid` | Mã giao dịch provider |
| `OriginInv*` | HĐ gốc (nếu điều chỉnh/thay thế) |
| `ActionCode/TypeInvoice` | Hành động + loại (MISA/Viettel) |

### InvoiceStatus
| Status | Ý nghĩa |
|--------|---------|
| `pending` | Chờ xử lý |
| `inprocess` | Đang gửi provider |
| `success` | Thành công |
| `error` | Lỗi — xem `Description` |

---

## API & Luồng

```
POST /api/Invoice/CreateInvoice {
  TypeInvoice: MISA / Viettel
  ActionType: 1=Publish, 2=Replace, 3=Adjust
  ListDonHang: [{ BuyerInfo, Items, SummarizeInfo }]
}
  ↓ (retry 3 lần, delay 1s)
Publish message vào RabbitMQ
  ↓ (async)
ProcessInvoiceConsumer
  → Gửi HTTP đến provider
  → Update InvoiceStatus + InvoiceInfo
  → Callback URL nếu có
```

---

## Checklist fix bug

- [ ] HĐ không phát hành → xem `InvoiceStatus.Description`, kiểm tra consumer log
- [ ] HĐ điều chỉnh lỗi → kiểm tra `OriginInvNo/Date` có khớp HĐ gốc không
- [ ] Gửi 2 lần → kiểm tra `IdTransaction` idempotency
