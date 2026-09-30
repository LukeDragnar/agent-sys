# OrderProcess — Rollback Đơn Hàng

> Điều phối và thực thi rollback đơn hàng khi gặp lỗi hoặc bị hủy. Đảm bảo đồng bộ trạng thái với tất cả các hệ thống liên quan: kho, khách hàng, booking, khuyến mãi, thu chi, logistics.

---

## Vai trò

- ← **Order / TMĐT**: gửi message khi đơn cần rollback
- → **WarehouseProduct**: rollback xuất kho
- → **Customer**: rollback điểm/hạng/công nợ
- → **Booking**: rollback lịch hẹn
- → **Promotion**: rollback voucher
- → **Branch (ThuChi)**: rollback phiếu thu/chi
- → **OrderLogistic**: rollback đơn logistics

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `OrderRollBack` | Bản ghi rollback: `IdDonHang`, `MaDonHang`, `Data`, cờ thành công từng hệ thống (`IsWarehouseProductSuccess`, `IsCustomerSuccess`, `IsBookingMajorSuccess`, `IsPromotionSuccess`, `IsThuChiSuccess`, `IsPhieuBanHangSuccess`), message lỗi tương ứng |
| `OrderRollBack` (meta) | `IsRollBackLuon`, `IsProcessing`, `SoLanDaGui` |
| `OrderRollBackLogistic` | Rollback logistics: trạng thái theo từng đơn vị vận chuyển |
| `OrderRollBackLogisticDoiSoat` | Rollback đối soát logistics |
| `OrderRollBackOnline` | Rollback đơn online (TMĐT) |
| `OrderRollBackThuNgan` | Rollback thu ngân |
| `NhapKhoRollBackLogistic` | Nhập kho trả khách sau rollback logistics |
| `OrderRollBackLog` | Log chi tiết quá trình rollback |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/OrderRollBack/CreateRollBackAll` | Tạo rollback cho toàn bộ hệ thống |
| `POST /api/OrderRollBack/UpdateRollBack` | Cập nhật trạng thái rollback |
| `GET /api/OrderRollBack/GetListByIdDonHang` | Xem trạng thái rollback của đơn |
| `POST /api/OrderRollBack/CreateRollBackLogistic` | Tạo rollback logistics |
| `POST /api/OrderRollBack/TMDT_CreateRollBackLogistic` | Tạo rollback logistics từ TMĐT (public) |
| `POST /api/OrderRollBack/CreateRollBackPhieuNhapTraKhach` | Tạo nhập kho trả khách |
| `POST /api/OrderRollBack/CreateRollBackLogisticDoiSoat` | Tạo rollback đối soát |

---

## Luồng

```
[Tạo rollback]
Order service gọi CreateRollBackAll() hoặc publish message
  ↓
OrderRollBackSyncConsumer / API trực tiếp
  ↓
IOrderRollBackService.CreateRollBackAll()
  └→ Lưu OrderRollBack với IsProcessing=false, tất cả cờ=false

[Xử lý rollback (Job định kỳ)]
JobRollBack (scheduler, có static lock isRunning)
  ↓
CallRollBack()
  ├→ LayDanhSachOrderRollBack_XuLyRollBack() — lấy chưa xử lý
  ├→ CallRollBackWareHouse()    → gọi API WarehouseProduct
  ├→ CallRollBackCustomer()     → gọi API Customer
  ├→ CallRollBackBooking()      → gọi API Booking
  ├→ CallRollBackPromotion()    → gọi API Promotion
  ├→ CallRollBackThuChi()       → gọi API Branch
  └→ Cập nhật cờ IsXxxSuccess / message lỗi

[Cleanup]
JobXoaBanGhiOrderRollBack     → xóa bản ghi thành công cũ
JobXoaBanGhiOrderRollBackLoi  → xóa bản ghi lỗi cũ
```

---

## Jobs định kỳ

| Job | Mục đích |
|-----|---------|
| `JobRollBack` | Xử lý rollback chờ (main job, có static lock) |
| `JobRollBackWarehouse` | Rollback riêng cho kho |
| `JobRollBackCustomer` | Rollback riêng cho khách hàng |
| `JobRollBackLogistic` | Rollback riêng cho logistics |
| `JobRollBackPromotion` | Rollback riêng cho khuyến mãi |
| `JobRollBackNhapKhoTraKhach` | Nhập kho trả khách |
| `JobXoaBanGhiOrderRollBack` | Cleanup bản ghi thành công |
| `JobXoaBanGhiOrderRollBackLoi` | Cleanup bản ghi lỗi |

---

## Đặc điểm quan trọng

- **SoLanDaGui**: tracking số lần retry, tránh vòng lặp vô hạn
- **Cờ từng hệ thống**: rollback từng phần — nếu Warehouse fail không ảnh hưởng Customer
- **static isRunning**: tránh chạy song song job cùng loại
- TMĐT endpoints (`TMDT_*`) là **public** (không cần auth) để TMĐT platform gọi

---

## Checklist fix bug

- [ ] Rollback không chạy → kiểm tra `JobRollBack` có đang lock không (isRunning stuck)
- [ ] 1 hệ thống con fail → xem cờ `Is[X]Success` + `[X]MessErr` trong `OrderRollBack`
- [ ] SoLanDaGui quá nhiều → điều tra API hệ thống con đang bị lỗi gì
- [ ] Rollback logistics → xem `OrderRollBackLogistic` + `OrderRollBackLog`
