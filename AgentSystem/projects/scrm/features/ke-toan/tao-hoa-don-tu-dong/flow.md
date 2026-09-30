# Tạo hóa đơn tự động — Luồng

## Tiền điều kiện
- Đã bật **tiện ích hóa đơn điện tử** (MISA `TI_Invocie_Misa` và/hoặc Viettel `TI_Invocie_Viettel`) + cấu hình **tài khoản + mẫu mặc định** (xem feature "Hóa đơn điện tử").
- **Cấu hình trạng thái giao hàng:** ít nhất 1 trạng thái được đánh dấu cờ tự phát hành `IsAutoPublishInvoice = true`.
- **Cấu hình đơn vị:** `LoaiHoaDonTuDong` (2 = tự tạo nháp; khác = tự phát hành), `XuatHoaDonTuDongSauSoPhut` (mặc định 5), `TaoNhapTuDongSauSoPhut` (mặc định 2).
- Có đơn (cửa hàng/POS qua `DonHang_TaoDon`, hoặc đơn online logistic) sẽ chuyển trạng thái giao hàng.

## Luồng dùng (tự động — không thao tác tay)
1. Đơn chuyển trạng thái giao hàng (ví dụ qua Kế toán tác nghiệp / cập trạng thái giao hàng, hoặc luồng đơn cửa hàng).
2. Service gọi `SendMessageCreateAutoPublishInvoice(...)` → publish message `CreateAutoPulishInvoiceMesssage` (RabbitMQ).
3. Consumer `EventAutoPublishInvoiceConsumer` nhận → `QueueGetInfoOrderToAutoPublishInvoice` → `TimeToPublish_Create(...)`.
4. `TimeToPublish_Create` (CommonService) xử lý:
   - Lấy danh sách trạng thái giao hàng được phép tự phát hành: `cauHinhTrangThaiGiaoHang.Where(IsAutoPublishInvoice == true).Select(Ma)`.
   - **Nếu `LoaiHoaDonTuDong == 2` (chế độ NHÁP):** với từng đơn ở trạng thái cho phép + chưa có HĐ → enqueue bảng tạo nháp `DonHangTimeToCreateDraft` với `TimeToPublish = now + TaoNhapTuDongSauSoPhut` (mặc định 2 phút). KHÔNG phát hành thật.
   - **Ngược lại (chế độ PHÁT HÀNH):** với từng đơn hợp lệ → enqueue bảng `DonHangTimeToPublishInvoice` với `TimeToPublish = now + XuatHoaDonTuDongSauSoPhut` (mặc định 5 phút), `ActionCode = Publish`.
5. Job theo lịch quét bảng hàng đợi tới hạn → gọi luồng phát hành HĐĐT (qua InvoiceController/HoaDonDienTuService) như phát hành thủ công, đánh dấu nguồn thực hiện = tự động.
6. Kết quả ghi vào trạng thái hóa đơn của đơn + log hóa đơn tự động (`LogHoaDonDienTuTuDong`, MongoDB).

## Luồng cấu hình (admin/kế toán)
1. Cấu hình tài khoản + mẫu mặc định HĐĐT.
2. Đánh dấu trạng thái giao hàng được tự phát hành (`IsAutoPublishInvoice`).
3. Chọn chế độ tự động (nháp/phát hành) + số phút trễ.

## Các trường hợp / case biên
- **Không lấy được cấu hình trạng thái giao hàng** → ghi log `LogHoaDonDienTuTuDong` status Fail "Không lấy được cấu hình trạng thái giao hàng", không phát hành.
- **Đơn không ở trạng thái cho phép** (`GiaoHangTrangThaiMa` không nằm trong danh sách `IsAutoPublishInvoice`) → bỏ qua (skip), không enqueue.
- **Đơn đã có hóa đơn** (`InvCode`/`InvNo`/`InvDate` khác null) → chế độ phát hành ghi log Fail "Đơn hàng đã phát hành trước đó"; chế độ nháp thì skip.
- **Đơn đã nằm trong hàng đợi** (bảng `DonHangTimeToPublishInvoice` / `DonHangTimeToCreateDraft` / bảng ReSend) → bỏ qua, chống trùng (dedupe).
- **Chế độ nháp:** chỉ tạo nháp, không tự phát hành — cần đồng bộ nháp → phát hành thủ công.
- **Nhà cung cấp ≠ MISA/Viettel:** không hỗ trợ tự phát hành (luồng phát hành chung chặn).
- **Hủy phát hành tự động:** có luồng hủy cho đơn đang chờ (`HuyPhatHanhTuDong` / consumer ReSend) — thao tác UI cần product xác nhận.

## Ví dụ cụ thể có số
Đơn vị D cấu hình: trạng thái "Đã giao hàng" có `IsAutoPublishInvoice = true`, `LoaiHoaDonTuDong` = phát hành thật, `XuatHoaDonTuDongSauSoPhut` = 5, nhà cung cấp Viettel (mẫu mặc định V1).
- 10:00 đơn `OL-2001` chuyển sang "Đã giao hàng" → hệ thống enqueue `DonHangTimeToPublishInvoice` với `TimeToPublish = 10:05`.
- 10:05 job quét tới hạn → phát hành qua Viettel → đơn có số HĐ, log tự động status thành công.
- Cùng lúc đơn `OL-2002` đã có số HĐ từ trước → bị bỏ qua, ghi log "Đơn hàng đã phát hành trước đó".
- Nếu đổi cấu hình sang chế độ nháp (`LoaiHoaDonTuDong = 2`): đơn `OL-2003` "Đã giao hàng" lúc 11:00 → enqueue `DonHangTimeToCreateDraft` với `TimeToPublish = 11:02` → chỉ tạo nháp; kế toán soát rồi đồng bộ nháp → phát hành.

## Hậu điều kiện
- **Chế độ phát hành:** đơn có số hóa đơn (sau độ trễ + job xử lý); ghi trạng thái HĐ + log tự động; (tùy cấu hình) đẩy SMS/ZNS.
- **Chế độ nháp:** đơn có hóa đơn nháp chờ kế toán đồng bộ.
- **Đơn bị skip/lỗi:** không có hóa đơn, có bản ghi log lý do trong `LogHoaDonDienTuTuDong`.
