# MarketingCampaign — Chiến Dịch Marketing

> Tạo và chạy chiến dịch SMS/Zalo tự động đến tệp khách hàng theo điều kiện lọc. Hỗ trợ gửi ngay, lên lịch, và lặp lại theo chu kỳ.

---

## Vai trò

- ← **Customer / Order / OrderLogistic / Contact**: sync dữ liệu KH vào module
- → **SMS service**: gửi SMS/Zalo cho từng KH trong chiến dịch
- → **Promotion**: kiểm tra voucher đính kèm
- → **MongoDB**: log lỗi gửi
- → **Redis**: cache + lock distributed

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `Campaign` | Chiến dịch: `Ma`, `Ten`, `KieuGui` (Immediate/Schedule/Repeat), `KieuLapLai` (Daily/Weekly/Monthly), `NgayGioGui`, `IsSendSMS`, `IsSendZalo`, `IdVoucher`, `StatusJob` |
| `Campaign` (filter) | `IdCustomerConditionFilter`, `IdSMSTemplate`, `IdZaloTemplate`, `JsonParams`, `IsGetLinkDanhGia` |
| `CampaignRunning` | Theo dõi từng lần chạy: số KH thành công/thất bại, trạng thái |
| `CustomerInfoCampaign` | KH đủ điều kiện: `IdCampaign`, `IdCustomer`, `IsSendSMS`, `IsSendZalo`, trạng thái gửi |
| `CustomerConditionFilter` | Bộ lọc KH: điều kiện động (ngày sinh, hạng, doanh số...) |
| `DonViSync` | Đơn vị đã sync dữ liệu KH |

**StatusJob:** Unprocess → Processing → Done / Error

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/Campaign/Insert` | Tạo chiến dịch |
| `POST /api/Campaign/Update` | Cập nhật |
| `POST /api/Campaign/Delete/{id}` | Xóa |
| `POST /api/Campaign/SearchByCondition` | Tìm kiếm |
| `POST /api/Campaign/SearchHistoryByCondition` | Lịch sử chạy |
| `GET /api/Campaign/GetAllCampaignByIdDonVi/{id}` | Tất cả chiến dịch của đơn vị |

---

## Luồng

```
[Tạo chiến dịch]
Admin tạo Campaign với CustomerConditionFilter + Template SMS/Zalo + KieuGui

[Quét KH đủ điều kiện]
JobScanCustomerInfoCampaign (scheduler)
  └→ CampaignService.Job_Campaign_QuetKhachHangDuDieuKien()
       ├→ Lọc từ bảng KH sync (CustomerInfoCampaign)
       └→ Đánh dấu KH đủ điều kiện

[Chạy chiến dịch]
JobCampaign (scheduler)
  └→ ICampaignService.Job_CampaignRun()
       ├→ Lấy danh sách CustomerInfoCampaign chưa gửi
       ├→ Với mỗi KH: publish SMS/Zalo message → SMS service
       └→ Cập nhật StatusJob = Done / Error

[Nhận phản hồi trạng thái]
SMS service gửi → EventUpdateStatusSMSSendMessage
  ↓
EventUpdateStatusSendSMSConsumer
  ├→ Update CustomerInfoCampaign.IsSendSMS/IsSendZalo
  └→ Log lỗi MongoDB nếu thất bại

[Sync dữ liệu KH]
Customer/Order/Contact thay đổi → publish event
  ↓
EventSyncCustomerInfoConsumer (Redis lock + retry 3 lần)
  └→ SyncCustomerService.SyncCustomerInfo()
       └→ Cập nhật bảng KH local (CustomerInfoCampaign)
```

---

## Đặc điểm quan trọng

- **KieuLapLai**: Daily/Weekly/Monthly — chiến dịch tự chạy định kỳ
- **NgayTrongTuan / NgayTrongThang**: chọn ngày cụ thể cho repeat
- **IsGetLinkDanhGia**: tự động đính kèm link đánh giá vào tin nhắn
- **IdVoucher**: tặng voucher kèm theo campaign
- **Redis distributed lock**: tránh race condition khi nhiều pod chạy sync cùng lúc
- **StartRecordNumber / EndRecordNumber**: phân trang khi chạy campaign lớn

---

## Checklist fix bug

- [ ] Campaign không chạy → kiểm tra `Campaign.StatusJob`, `JobCampaign`
- [ ] KH không đủ điều kiện → kiểm tra `CustomerConditionFilter`, `JobScanCustomerInfoCampaign`
- [ ] SMS/Zalo không gửi → kiểm tra `CustomerInfoCampaign.IsSendSMS`, log MongoDB `UpdateStatusSendMessageLog`
- [ ] Voucher không đính kèm → kiểm tra `Campaign.IdVoucher`, gọi `CheckVoucherValid()`
- [ ] Sync KH không cập nhật → kiểm tra consumer `EventSyncCustomerInfoConsumer`, Redis lock có bị kẹt không
