# Deposit — Quản Lý Đặt Cọc

> Quản lý phiếu đặt cọc sản phẩm từ khách hàng (thường qua Landing Page). Hỗ trợ thay đổi sản phẩm, hoàn cọc, gửi SMS/Zalo thông báo theo lô và từng action.

---

## Vai trò

- ← **Landing Page**: publish `MessageCreateDepositReceiptFromLandingDto` khi KH đặt cọc
- ← **Promotion**: cập nhật voucher status
- → **SMS service**: gửi SMS/Zalo thông báo
- → **Redis**: lock concurrent SMS sends

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `DepositReceipt` | Phiếu cọc: `CustomerName`, `PhoneNumber`, `ProductCode`, `ProductPrice`, `DepositAmount`, `DepositCode`, `DepositPaymentStatus` (0=chưa TT, 1=đã TT), `IsDelivered` |
| `DepositReceipt` (hoàn cọc) | `IsDepositRefund`, `DepositRefundDate`, `DepositRefundReason`, `DepositRefundBankName`, `DepositRefundBankAccount`, `DepositRefundAmount` |
| `DepositOrderDeliveryBatch` | Đợt trả cọc: `NameDeliveryBatch`, `Code`, `FromDepositDate`, `ToDepositDate`, `ListIdReceivingBranch`, `IsSended` |
| `DepositSmsSend` | Log gửi SMS: `IdDepositReceipt`, `PhoneNumber`, `StatusSendSms`, `TypeSend`, `IsSendSMSFirstTime` |
| `BatchSendSmsCustom` | Batch gửi SMS tùy chỉnh |
| `ConfigSMS` | Mẫu SMS/Zalo |
| `DepositReceiptCreationErrorLogs` | Log lỗi tạo phiếu cọc |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/DepositReceipt/SearchWithConditions` | Tìm kiếm phiếu cọc |
| `POST /api/DepositReceipt/ReplaceDepositReceipt` | Thay đổi sản phẩm đã đặt cọc |
| `POST /api/DepositReceipt/UpdatePaymentStatusDepositReceipt` | Cập nhật trạng thái thanh toán |
| `POST /api/DepositReceipt/DepositRefund` | Hoàn tiền cọc |
| `POST /api/DepositReceipt/ConfirmRefund` | Xác nhận hoàn tiền |
| `POST /api/DepositReceipt/GetHistoryModifyDepositReceipt/{idTransaction}` | Lịch sử thay đổi (MongoDB) |
| `POST /api/DepositReceipt/ExportExcelDepositManager` | Export Excel |
| `POST /api/DepositReceipt/RemoveFromDeliveryBatch` | Xóa khỏi đợt trả cọc |
| CRUD `/api/DepositOrderDeliveryBatch/...` | Quản lý đợt trả cọc |

---

## Luồng

```
[Tạo phiếu cọc từ Landing Page]
Landing Page publish MessageCreateDepositReceiptFromLandingDto
  ↓
EventCreateDepositReceiptConsumer
  ↓
DepositReceiptService.CreateDepositReceiptFromLanding()
  ├→ Tạo DepositReceipt
  └→ Trigger EventDepositReceiptSendSmsFirstTimeConsumer
       → Gửi SMS lần đầu cho KH

[Gửi SMS/Zalo]
EventDepositSmsSendConsumer
  ├→ Lock Redis (chống gửi trùng)
  ├→ Gọi SMS service
  └→ EventDepositUpdateStatusSendSMSConsumer → cập nhật StatusSendSms

[Job scan KH chưa thanh toán]
JobScanKhachHangChuaThanhToan (scheduler)
  └→ Tìm phiếu DepositPaymentStatus=0 quá hạn
       → Gửi SMS nhắc nhở

[Hoàn cọc]
Admin: POST /api/DepositReceipt/DepositRefund
  └→ Cập nhật IsDepositRefund, DepositRefundAmount, thông tin TK ngân hàng

[Gán đợt trả cọc]
DepositOrderDeliveryBatch.GanDotTraCoc()
  └→ Gán danh sách phiếu vào đợt trả cọc theo chi nhánh nhận
```

---

## Đặc điểm quan trọng

- History thay đổi phiếu lưu trong **MongoDB** (tra cứu theo `IdTransaction`)
- SMS retry tối đa 3 lần với **Redis semaphore** (tránh gửi trùng)
- `JsonParam` cho phép tùy chỉnh nội dung SMS/Zalo
- Hỗ trợ thay đổi sản phẩm ngay cả khi đã thanh toán cọc

---

## Checklist fix bug

- [ ] Phiếu cọc không tạo → xem `DepositReceiptCreationErrorLogs`, kiểm tra `EventCreateDepositReceiptConsumer`
- [ ] SMS không gửi → xem `DepositSmsSend.StatusSendSms`, kiểm tra Redis lock
- [ ] KH chưa nhận SMS nhắc → kiểm tra `JobScanKhachHangChuaThanhToan`
- [ ] Lịch sử thay đổi → tìm theo `IdTransaction` trong MongoDB
