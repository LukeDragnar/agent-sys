# Tự động hóa đơn & đối soát chạy nền — Luồng

> 3 mạch nền độc lập, không UI thao tác: (A) consumer tự phát hành/tạo nháp HĐĐT, (B) job đối soát đơn hoàn → tạo phiếu bán trả lại, (C) job đối soát giao dịch ngân hàng MB.

## Tiền điều kiện
- **(A) Tự phát hành HĐĐT:** đã bật tiện ích HĐĐT + cấu hình tài khoản/mẫu MISA/Viettel; cấu hình mốc trạng thái `IsAutoPublishInvoice` + loại HĐ tự động + độ trễ (xem feature `tao-hoa-don-tu-dong`). Hệ thống chạy consumer hàng đợi (MassTransit/RabbitMQ).
- **(B) Đối soát đơn hoàn:** đơn hoàn đã được đối soát (có lịch sử đối soát) và đã được **xếp vào hàng đợi** `DoiSoatDonHoanTaoPhieu`; service OrderLogisticConsumer chạy (job Quartz đăng ký).
- **(C) Đối soát MB:** service Payment (WebhookBank) chạy; có **file đối soát MB** `0G_yyyyMMdd_TC.txt` ngân hàng đặt vào thư mục `mbbank/DoiSoat_In`; thời điểm ≥ 8h sáng.

## Luồng nền (A) — Tự phát hành / tạo nháp HĐĐT
1. Đơn online (OrderLogistic) đổi trạng thái giao hàng → `CommonService.SendMessageCreateAutoPublishInvoice` publish sự kiện `CreateAutoPulishInvoiceMesssage` (qua đường đổi trạng thái trong DonHangService).
2. `EventAutoPublishInvoiceConsumer.Consume` nhận → gọi `QueueGetInfoOrderToAutoPublishInvoice(item)` → `TimeToPublish_Create`:
   - Lấy danh sách trạng thái `IsAutoPublishInvoice = true`. Nếu rỗng → ghi log Fail "Không lấy được cấu hình trạng thái giao hàng".
   - Nhánh **NHÁP** (`LoaiHoaDonTuDong == 2`): độ trễ `TaoNhapTuDongSauSoPhut ?? 2` phút; skip đơn sai trạng thái / đã có HĐ / đã trong bảng draft / đã ReSend → insert `DonHangTimeToCreateDraft`.
   - Nhánh **PHÁT HÀNH**: độ trễ `XuatHoaDonTuDongSauSoPhut ?? 5` phút; skip đơn sai trạng thái; đơn đã có HĐ → log Fail "Đơn hàng đã phát hành trước đó"; enqueue `DonHangTimeToPublishInvoice`.
3. Sau độ trễ, luồng phát hành chung (Invoice service) phát hành thật qua MISA/Viettel.
4. Lỗi phát hành lại → `EventAutoPublishInvoiceReSendConsumer` → `QueueGetInfoOrderToAutoPublishInvoiceReSend`.
- *Kết quả mong đợi:* đơn đạt mốc → sau độ trễ có hóa đơn (hoặc nháp); đơn đã có HĐ bị skip; lỗi cấu hình → log Fail.

## Luồng nền (B) — Job đối soát đơn hoàn → tạo phiếu bán trả lại
1. Tại bước đối soát đơn hoàn (OrderLogistic), hệ thống insert bản ghi vào hàng đợi `DoiSoatDonHoanTaoPhieu` (`IsProcessing=false`, `Status=null`).
2. Job Quartz **`JobDoiSoatDonTaoPhieu`** chạy lặp **mỗi 10 giây**; có cờ tĩnh `IsRunning` chống chạy chồng (đang chạy thì bỏ lượt).
3. `ProcessDoiSoatDonHoanTaoPhieuJob`: lấy **tối đa 100** bản ghi `!IsProcessing && Status rỗng`, sắp theo `NgayTao` → đánh dấu `IsProcessing=true` (lock) → update.
4. Với từng job: dựng `PhieuBanTraLaiDoiSoatMessage` (IdDonVi, IdDonHangLichSuDoiSoat, IdDonHangLogistic, MaDon/MaDonGiaoVan, phiếu bán hàng, hình thức hoàn tiền, kho, phương thức/ tài khoản thanh toán…) → **publish sang Accounting** → `SetSuccess` (Status=SUCCESS) hoặc lỗi → `SetFailed` (Status=FAILED + message). Mỗi bản ghi delay 200ms.
5. Accounting nhận qua **`PhieuBanTraLaiDoiSoatConsummer`** → `HandleCreatePhieuTraLaiFromOrderLogistic(message)` tạo phiếu bán trả lại; lỗi → `throw` để MassTransit retry.
- *Kết quả mong đợi:* mỗi đơn hoàn đã đối soát → 1 message → 1 phiếu bán trả lại bên kế toán; bản ghi hàng đợi chuyển SUCCESS/FAILED.

## Luồng nền (C) — Job đối soát giao dịch ngân hàng MB
1. `JobDoiSoatMbBank` (IHostedService) khởi vòng lặp `XuLyPhanDoiSoatBankService.Run`.
2. Mỗi vòng: nếu giờ < 8h → chờ 1 giờ; nếu đã đối soát xong trong ngày → chờ 30 phút; sang ngày mới → reset cờ.
3. Lấy ngày `d = hôm nay - 1`. Kiểm tra file `0G_{yyyyMMdd}_TC.txt` (`FileDoiSoatMBBankExited`): chưa có → chờ 30 phút, thử lại. Đã có file kết quả `_TH.txt` (`FileDoiSoatSandboxExited`) → coi như xong, chờ đến giờ tròn kế tiếp.
4. `DoiSoatBankMB(d)`: đọc file (bỏ dòng tiêu đề), nạp vào bảng tạm `TempDoiSoat` (FtCode + số tiền). So khớp `TempDoiSoat` ↔ `MerchantOrder` (theo `FtCode` + `PayDate`):
   - **Thừa MB:** có ở file, không có trong `MerchantOrder`.
   - **Thừa Sandbox (hệ thống):** có trong `MerchantOrder`, không có ở file.
   - **Lệch:** cùng FtCode nhưng `DebitAmount` khác.
5. Nếu tất cả = 0 → ghi file `_TH.txt` (kết quả = 0, cân khớp). Nếu có lệch → ghi `_TH.txt` (kết quả = 1) + file chi tiết `_CL.txt` liệt kê từng giao dịch thừa/lệch.
6. Đối soát xong → `isDoiSoatDone=true`, chờ đến giờ tròn kế tiếp.
- *Kết quả mong đợi:* mỗi ngày 1 lần khớp số ngân hàng MB ↔ hệ thống, xuất file kết quả khớp hoặc chênh lệch.

## Các trường hợp / case biên
- **Publish/consumer nuốt lỗi (A):** `EventAutoPublishInvoiceConsumer.cs:25-28` và `EventAutoPublishInvoiceReSendConsumer.cs:25-28` là `catch (Exception) {}` rỗng → đơn **không vào hàng đợi mà không cảnh báo** (mất tự phát hành im lặng). `SendMessageCreateAutoPublishInvoice` cũng catch rỗng.
- **Đơn đã có HĐ (A):** skip theo `InvCode/InvNo/InvDate`; nếu 3 trường này không set đúng sau phát hành → rủi ro **trùng hóa đơn**.
- **Chế độ nháp (A):** `LoaiHoaDonTuDong==2` chỉ tạo nháp, KHÔNG phát hành thật.
- **Job đối soát đơn hoàn chạy chồng (B):** chặn bằng cờ tĩnh `IsRunning`; nếu 1 lượt treo → các lượt sau bị bỏ qua đến khi `IsRunning` được reset (chỉ reset trong try/catch của `Execute`).
- **Lock theo batch (B):** đánh dấu `IsProcessing=true` trước khi xử lý; nếu publish lỗi → `Status=FAILED`, `IsProcessing=false` (được xử lý lại lần sau? KHÔNG — vì điều kiện lấy job là `Status rỗng`, đã set FAILED nên **không tự thử lại** — cần can thiệp tay).
- **Accounting tạo phiếu lỗi (B):** consumer `throw` → MassTransit retry theo cấu hình; nhưng bản ghi hàng đợi OrderLogistic đã `SUCCESS` (chỉ tính publish thành công, không phản ánh tạo phiếu lỗi) → **lệch trạng thái giữa 2 bên**.
- **File đối soát MB chưa về (C):** job chờ 30 phút thử lại, không bịa số.
- **File MB sai định dạng (C):** code `Split("|")` + parse cứng theo cột (rowData[0..20], `decimal.Parse(rowData[8])`) → file thiếu cột/ sai số → ném lỗi, job log lỗi và đợi giờ tròn (không xuất file kết quả).
- **Đối soát bank qua webhook (`DoiSoatBankController`):** ĐANG TẮT (toàn bộ comment) — không phải mạch chạy.

## Ví dụ cụ thể có số
**(A)** Shop B cấu hình: mốc "Đã giao hàng" `IsAutoPublishInvoice=true`, `LoaiHoaDonTuDong=1` (phát hành), độ trễ mặc định 5 phút. Đơn `DH001` chuyển "Đã giao hàng" lúc 10:00 → consumer enqueue `DonHangTimeToPublishInvoice` với `TimeToPublish=10:05`. Lúc 10:05 luồng phát hành ra hóa đơn. Đơn `DH002` đã có `InvNo` từ trước → log Fail "Đơn hàng đã phát hành trước đó", không tạo lại.

**(B)** Kế toán đối soát 250 đơn hoàn → 250 bản ghi vào `DoiSoatDonHoanTaoPhieu`. Job chạy mỗi 10s, mỗi lượt lấy 100 bản ghi: lượt 1 xử lý 100, lượt 2 xử lý 100, lượt 3 xử lý 50 → publish 250 message sang Accounting → 250 phiếu bán trả lại. Nếu 3 đơn publish lỗi → 3 bản ghi `FAILED` (kèm message), 247 `SUCCESS`; 3 đơn FAILED không tự chạy lại.

**(C)** Ngày 06/12, file `0G_20241206_TC.txt` MB gửi có 120 giao dịch tổng 50.000.000đ. Hệ thống `MerchantOrder` ngày 06/12 có 118 giao dịch. Đối soát: 2 giao dịch "thừa MB" (15.000đ), 0 thừa hệ thống, 1 lệch số tiền → xuất `0G_20241206_TH.txt` (kết quả=1: cần khớp 117, lệch 3) + `0G_20241206_CL.txt` liệt kê 3 dòng. Nếu khớp đủ → `_TH.txt` kết quả=0, không có file `_CL`.

## Hậu điều kiện
- **(A)** Đơn có hóa đơn (hoặc nháp); bảng hàng đợi `DonHangTimeToPublishInvoice`/`DonHangTimeToCreateDraft` cập nhật; log `LogHoaDonDienTuTuDong` (MongoDB) ghi kết quả/skip/fail.
- **(B)** Bản ghi `DoiSoatDonHoanTaoPhieu` chuyển SUCCESS/FAILED; bên Accounting có phiếu bán trả lại tương ứng (nếu consumer xử lý xong).
- **(C)** File kết quả `_TH.txt` (và `_CL.txt` nếu lệch) được ghi vào `mbbank/DoiSoat_Out`; bảng tạm `TempDoiSoat` được tái sử dụng (xóa đầu mỗi lần chạy).
