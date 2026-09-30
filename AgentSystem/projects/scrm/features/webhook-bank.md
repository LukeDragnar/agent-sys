# WebhookBank — Xác Nhận Thanh Toán Ngân Hàng

> Nhận callback từ ngân hàng (MB Bank, Zengi) khi khách thanh toán QR/chuyển khoản → cập nhật trạng thái đơn hàng.

---

## Vai trò

- ← MB Bank / Zengi gửi callback khi có giao dịch
- → **Order/OrderLogistic**: cập nhật trạng thái thanh toán
- → **Accounting**: ghi nhận phiếu thu

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `TkNganHangHeThong` | Tài khoản ngân hàng của hệ thống |
| `TkNganHangKetNoiOnline` | TK kết nối thanh toán online |
| `LogRequestQrcode` | Log request tạo QR + trạng thái thanh toán |
| `LogRequestChiHo` | Log request chi hộ (Zengi) |
| `LogZengiData` | Log response từ Zengi |
| `MerchantOrder` | Đơn hàng merchant (TraceTransfer, DebitAmount, RespCode) |

---

## Luồng

```
[QR Code flow]
FE tạo QR → lưu LogRequestQrcode (TrangThai = 0)
  ↓
Khách quét QR, chuyển tiền
  ↓
MB Bank callback → BankSendController.PaymentUpdate()
  ├→ Update LogRequestQrcode.TrangThai = 1
  └→ Gọi callback API → Order cập nhật thanh toán thành công

[Zengi chi hộ flow]
Zengi gửi callback → ZengiReceiveController
  ↓
ZengiService.UpdateDataReceive()
  ├→ Ghi LogZengiData
  ├→ Update LogRequestChiHo (TransactionStatus, FtCode)
  └→ Publish event → Order/Accounting

[Đối soát định kỳ]
JobDoiSoatMbBank (scheduler) → Đối soát giao dịch ngân hàng
```

---

## Checklist fix bug

- [ ] Thanh toán QR không cập nhật → xem `LogRequestQrcode.TrangThai`, kiểm tra callback URL
- [ ] Zengi chi hộ lỗi → xem `LogZengiData`, kiểm tra `LogRequestChiHo.TransactionStatus`
- [ ] Đối soát sai → chạy lại `JobDoiSoatMbBank` thủ công
