# AccountingReceive — Cầu Nối CRM → Kế Toán

> Trung gian nhận phiếu từ CRM, forward sang Accounting service xử lý, nhận callback kết quả từ partner (MISA). Lưu trạng thái để đảm bảo idempotency.

---

## Vai trò

- ← **Order / CRM services**: gửi `AccountingReceiveMessage1` qua MassTransit
- → **Accounting**: publish `DongBoPhieuCrmMessage1`
- ← **Partner (MISA)**: gọi callback endpoint với kết quả xử lý

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `DataReceiveCrm` | Phiếu nhận từ CRM: `PartnerName`, `Data`, `Status`, `ResendNo`, `InProcess`, `IdTransaction` |
| `DataReceiveResultPartner` | Kết quả partner gửi lại: tương tự DataReceiveCrm |
| `LogData` | Log lỗi: `Type`, `ProcessName`, `DataInfo`, `DataError`, `ServerCode`, `PartnerName`, `CrmUnitId`, `OrderCode`, `IdTransaction` |

**Trạng thái DataReceiveCrm:** `RECEIVED → INPROCESS → SUCCESS / ERROR`

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/DataReceive/{partnerName}` | Partner (MISA) gọi để gửi kết quả xử lý phiếu |

---

## Luồng

```
[Nhận phiếu từ CRM]
Order/Service publish AccountingReceiveMessage1
  ↓ (MassTransit)
DataReceiveCrmConsummer
  ↓
DataReceiveCrmService.HandlePhieuCrm()
  ├→ Ghi log MongoDB (collection: AccountingReceivePhieuCrm)
  ├→ Lưu DataReceiveCrm vào SQL (status: INPROCESS)
  └→ Publish DongBoPhieuCrmMessage1 → Accounting service

[Nhận kết quả từ Partner]
MISA callback POST /api/DataReceive/MISA
  ↓
DataReceiveCrmService.ResHandlePhieuCrm()
  ├→ Cập nhật DataReceiveResultPartner
  └→ Cập nhật DataReceiveCrm.Status = SUCCESS/ERROR
```

---

## Đặc điểm quan trọng

- **Idempotency**: dùng `IdTransaction` để chống xử lý trùng lặp
- **Retry**: `ResendNo` đếm số lần gửi lại
- **Multi-partner**: `PartnerName` phân biệt MISA / các partner khác
- Log lỗi ghi cả MongoDB + SQL để tra cứu

---

## Checklist fix bug

- [ ] Phiếu không sang Accounting → xem `DataReceiveCrm.Status`, kiểm tra consumer `DataReceiveCrmConsummer`
- [ ] MISA không nhận được → kiểm tra `DataReceiveCrm.InProcess`, xem log MongoDB
- [ ] Phiếu trùng → tìm theo `IdTransaction` trong `DataReceiveCrm`
- [ ] Lỗi callback → xem `DataReceiveResultPartner` + `LogData`
