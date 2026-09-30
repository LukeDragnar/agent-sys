# Luồng — RollBack & xử lý đơn

## Tiền điều kiện
- Có một đơn hàng cần hoàn tác (bị hủy, hủy thanh toán, hoặc trả khách).
- Các phân hệ liên quan (Kho, Khách, Booking, Khuyến mãi, Thu chi, Logistic) đang hoạt động để nhận lệnh hoàn tác.
- `ConfigSystem` (bảng cấu hình của service OrderProcess) có tham số điều khiển job rollback.
- Để **xem** trên UI: admin hệ thống có quyền nội bộ `OrderProcess_TimTheoDieuKien` (màn "Danh sách đơn hàng lỗi").

## Luồng dùng (admin theo dõi — màn "Danh sách đơn hàng lỗi")
1. Admin mở **Quản trị → Danh sách đơn hàng lỗi** (route `quantri/ds-donhang-loi`).
2. Nhập bộ lọc: từ khóa (tên/mã), kiểu lỗi (1 = Hủy đơn, 2 = Thanh toán đơn), đơn vị, chi nhánh.
3. Bấm **Tìm kiếm** → gọi `OrderProcess/TimTheoDieuKien` (read-only) trả bảng bản ghi rollback có phân trang.
4. Mỗi dòng hiển thị: mã đơn, tên lỗi, đơn vị, chi nhánh, các cờ `isBookingMajorSuccess / isCustomerSuccess / isPromotionSuccess / isWarehouseProductSuccess`, ngày tạo.
5. Admin đọc cờ để biết phân hệ nào đã hoàn tác xong (✔) / chưa (trống).

## Luồng hệ thống (rollback tự động — không UI)
1. **Phát sinh:** Order/OrderLogistic gọi `CreateRollBackAll` (hoặc sàn TMĐT bắn `TMDT_CreateRollBackLogistic`, hoặc RabbitMQ `OrderRollBackMassage` → `OrderRollBackSyncConsumer`).
2. **Lưu bản ghi:** `LuuOrderRollBack` ghi `OrderRollBack` với tất cả cờ `IsXxxSuccess = false`, `IsProcessing = false`, `SoLanDaGui = 0`.
3. **Job nền xử lý:** job rollback (có static lock `isRunning` chống chạy song song) đọc `ConfigSystem`, lấy danh sách bản ghi chưa hoàn tất, lần lượt gọi:
   - `CallRollBackWareHouse` → API WarehouseProduct (hoàn tồn)
   - `CallRollBackCustomer` → API Customer (hoàn điểm/hạng/công nợ)
   - `CallRollBackBooking` → API Booking (hủy lịch hẹn)
   - `CallRollBackPromotion` → API Promotion (gỡ voucher)
   - `CallRollBackThuChi` → API Branch (hoàn phiếu thu/chi)
4. **Cập nhật cờ:** mỗi phân hệ chạy xong set cờ `IsXxxSuccess = true`; lỗi thì lưu message lỗi tương ứng, tăng `SoLanDaGui`.
5. **Dọn dẹp:** job `JobXoaBanGhiOrderRollBack` xóa bản ghi đã thành công cũ; `JobXoaBanGhiOrderRollBackLoi` xóa bản ghi lỗi cũ.

## Các trường hợp / case biên
- **Rollback từng phần:** 1 phân hệ con lỗi (sau khi đã lưu bản ghi) chỉ log, các phân hệ sau **vẫn chạy** → đơn có thể hoàn xong kho nhưng chưa hoàn công nợ. Cần job dọn / đối soát lại.
- **Retry vô hạn:** `SoLanDaGui` đếm số lần gửi để chặn vòng lặp vô hạn; vượt ngưỡng thì cần điều tra API phân hệ con thay vì retry mãi.
- **Job bị kẹt lock:** static `isRunning` không reset (do exception bất thường) → job ngừng xử lý, các đơn mới không được rollback. Triệu chứng: cờ tất cả đơn mới đều trống.
- **API public TMĐT:** `TMDT_CreateRollBackLogistic` là `[AllowAnonymous]` — sàn bắn vào không cần xác thực; cần whitelist/HMAC ở gateway (xem dev.md).
- **Đổi trạng thái ngược:** đơn đã hoàn tác mà lại có thao tác mới → cần đảm bảo không double-rollback (mỗi phân hệ check cờ `IsXxxSuccess` trước khi chạy lại).

## Ví dụ cụ thể có số
Đơn `DH-00123` (Shop A, chi nhánh CN-01) bị hủy → hệ thống sinh 1 bản ghi rollback, kiểu lỗi = 1 (Hủy đơn), tất cả 4 cờ = trống.
- Job chạy lần 1: hoàn kho OK (`WarehouseProductSuccess = ✔`), hoàn khách OK (`CustomerSuccess = ✔`), gỡ KM OK (`PromotionSuccess = ✔`), nhưng API Booking timeout → `BookingMajorSuccess` vẫn trống, `SoLanDaGui = 1`, lưu message lỗi booking.
- Admin mở "Danh sách đơn hàng lỗi", lọc mã `DH-00123` → thấy 3/4 cột ✔, cột Booking trống.
- Job chạy lần 2 (Booking đã sống lại): `BookingMajorSuccess = ✔`, `SoLanDaGui = 2`. Bản ghi đủ 4 cờ ✔ → đợt dọn `JobXoaBanGhiOrderRollBack` sẽ xóa bản ghi này → đơn biến mất khỏi danh sách (đã hoàn tác trọn vẹn).

## Hậu điều kiện
- Tồn kho, công nợ/điểm khách, voucher, phiếu thu/chi, lịch hẹn của đơn đã được **trả về đúng** ở tất cả phân hệ.
- Bản ghi rollback đủ cờ thành công → bị job dọn xóa. Bản ghi còn lỗi → giữ lại để retry/đối soát.
- Màn "Danh sách đơn hàng lỗi" **không ghi dữ liệu** (chỉ đọc).
