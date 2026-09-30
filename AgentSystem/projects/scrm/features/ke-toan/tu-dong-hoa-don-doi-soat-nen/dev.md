# Tự động hóa đơn & đối soát chạy nền — Kỹ thuật

> 3 mạch nền (không UI thao tác): (A) consumer tự phát hành/tạo nháp HĐĐT — **OrderLogistic + Order**; (B) job Quartz đối soát đơn hoàn → publish Accounting tạo phiếu bán trả lại — **OrderLogisticConsumer.API**; (C) IHostedService đối soát giao dịch ngân hàng MB — **WebhookBank/Payment.API**.
>
> Cross-link: cơ chế cấu hình + hàng đợi tự phát hành xem `features/ke-toan/tao-hoa-don-tu-dong/dev.md`; màn tác nghiệp + đối soát đơn online xem `features/ke-toan/doi-soat-don-online/dev.md` + `registry/kho/KeToanTacNghiepController.md`; đối soát đơn (đánh dấu ngày + ripple lương) xem `registry/ke-toan/DoiSoatController.md`.

## Màn hình & route
- **Không có màn riêng.** Toàn bộ là consumer/job nền. Đầu vào cấu hình nằm ở: cấu hình trạng thái giao hàng (`IsAutoPublishInvoice`), cấu hình đơn vị (`LoaiHoaDonTuDong`/`XuatHoaDonTuDongSauSoPhut`/`TaoNhapTuDongSauSoPhut`), và thao tác đối soát đơn hoàn ở màn Kế toán tác nghiệp.

## Consumer / Job (đăng ký)
**(A) Consumer tự phát hành (OrderLogistic + Order):**
- `EventAutoPublishInvoiceConsumer : IConsumer<InputDataJobAutoPublish_ToOrderLogistic>` → `IDonHangService.QueueGetInfoOrderToAutoPublishInvoice`.
- `EventAutoPublishInvoiceReSendConsumer : IConsumer<InputDataJobAutoPublishInvoiceReSend_ToOrderLogistic>` → `QueueGetInfoOrderToAutoPublishInvoiceReSend`.
- `EventCreateAutoPublishInvoiceConsumer` (OrderLogisticConsumer.API) — biến thể create.
- Bản Order tương ứng: `Order/Order.API/Consumer/EventAutoPublishInvoiceConsumer.cs` + `EventAutoPublishInvoiceReSendConsumer.cs`.

**(B) Job đối soát đơn hoàn (Quartz):**
- `JobDoiSoatDonTaoPhieu : IJob` — đăng ký Quartz, trigger lặp **10 giây** (`JobExtention.cs:70-78`); cờ tĩnh `IsRunning` chống chạy chồng.
- Service: `IDonHangDoiSoatLogService.ProcessDoiSoatDonHoanTaoPhieuJob`.

**(C) Job đối soát MB (IHostedService):**
- `JobDoiSoatMbBank : IHostedService` (`AddHostedService<JobDoiSoatMbBank>` — `ServicesExtention.cs:42`) → `XuLyPhanDoiSoatBankService.Run` (vòng lặp tự chờ theo giờ).
- Service: `IDoiSoatService.DoiSoatBankMB(DateTime)` + `FileDoiSoatMBBankExited` + `FileDoiSoatSandboxExited` (WebhookBank/BankReceive.Service).

## Endpoint/Controller liên quan (trace registry)
- `registry/kho/KeToanTacNghiepController.md` — màn tác nghiệp đơn online (nguồn thao tác đối soát).
- `registry/ke-toan/DoiSoatController.md` — đối soát đơn (đánh dấu ngày đối soát, ripple lương) — **internal**.
- `registry/ke-toan/DoiSoatBankController.md` — đối soát bank qua webhook — **DEPRECATED (toàn bộ comment)**, KHÔNG nhầm với job MB đang chạy.
- `registry/ke-toan/MerchantOrderController.md` — webhook báo có thanh toán (nguồn `MerchantOrder` mà job MB đối soát).
- `registry/ke-toan/InvoiceController.md` — phát hành HĐĐT chung (đích của mạch A).

## Entities / bảng
- **(A)** `DonHangTimeToPublishInvoice` (hàng đợi phát hành, `TimeToPublish`), `DonHangTimeToCreateDraft` (+ ReSend) (hàng đợi nháp); `LogHoaDonDienTuTuDong` (MongoDB) log kết quả/skip/fail. Cấu hình: `CauHinhTrangThaiGiaoHangDonViItem.IsAutoPublishInvoice`, `CauHinhSettingDto.LoaiHoaDonTuDong/.XuatHoaDonTuDongSauSoPhut/.TaoNhapTuDongSauSoPhut`.
- **(B)** `DoiSoatDonHoanTaoPhieu` (`OrderLogistic.DataContext/Entity/DoiSoatDonHoanTaoPhieu.cs`) — hàng đợi tạo phiếu: `IsProcessing`, `Status` (enum `EnumDonHangDoiSoatTrangThaiTaoPhieu`: SUCCESS/FAILED), `Message`, `NgayTao`, + dữ liệu đơn hoàn (IdDonHangLogistic, MaDon/MaDonGiaoVan, IdPhieuBanHang, HinhThucHoanTien, IdKho, IdPhuongThucThanhToan/IdTaiKhoan…). `DonHangLichSuDoiSoat` / `DonHangLichSuDoiSoatDetail` (lịch sử đối soát). Message: `PhieuBanTraLaiDoiSoatMessage`.
- **(C)** `MerchantOrder` (giao dịch thanh toán QR — `FtCode`, `DebitAmount`, `PayDate`, `IsDelete`), `TempDoiSoat` (bảng tạm), DTO `DoiSoatMBInfo`. File: `mbbank/DoiSoat_In/0G_yyyyMMdd_TC.txt` (vào), `mbbank/DoiSoat_Out/0G_yyyyMMdd_TH.txt` (tổng hợp) + `_CL.txt` (chênh lệch).

## Luồng code (path:line — verified)
**(A) Tự phát hành (xem chi tiết ở `tao-hoa-don-tu-dong/dev.md`):**
- Consumer: `OrderLogistic/OrderLogisticConsumer.API/Consumer/EventAutoPublishInvoiceConsumer.cs:18-31` (try/catch nuốt lỗi `:25-28`).
- Re-send: `.../EventAutoPublishInvoiceReSendConsumer.cs:18-31` (catch nuốt lỗi `:25-28`).
- Publish + xếp hàng đợi: `OrderLogistic/OrderLogistic.Service/Generated/Common/CommonService.cs:3772` (SendMessage), `:3796` (TimeToPublish_Create), `:3827` lọc trạng thái, `:3876` độ trễ phát hành.

**(B) Job đối soát đơn hoàn:**
1. `JobDoiSoatDonTaoPhieu.Execute` (`OrderLogistic/OrderLogisticConsumer.API/Jobs/JobDoiSoatDonTaoPhieu.cs:21`) — cờ `IsRunning` (`:9,:23,:27,:29,:35`); `WorkerXuLy` (`:40`) → `ProcessDoiSoatDonHoanTaoPhieuJob` (`:44`).
2. `ProcessDoiSoatDonHoanTaoPhieuJob` (`OrderLogistic/OrderLogistic.Service/Generated/DonHangDoiSoatLogService.cs:3036`):
   - `:3040-3045` lấy batch 100 (`!IsProcessing && Status rỗng`, OrderBy `NgayTao`).
   - `:3051-3057` lock `IsProcessing=true` + update.
   - `:3064-3084` dựng `PhieuBanTraLaiDoiSoatMessage` → `_publishEndpointDonHang.Publish` sang Accounting; `:3088` delay 200ms.
   - `:3086` `SetSuccess` / `:3092` `SetFailed`; `:3099-3102` batch update.
   - `SetSuccess` (`:3105`) / `SetFailed` (`:3116`).
3. Enqueue nguồn: `DonHangDoiSoatLogService.cs:2607` (`lstInsertQueue`), `:2647` (`new DoiSoatDonHoanTaoPhieu`), `:2787` (`InsertAsync(lstInsertQueue)`).
4. Accounting consumer: `Accounting/Accounting.API/Consumer/PhieuBanTraLaiDoiSoatConsummer.cs:26-48` → `HandleCreatePhieuTraLaiFromOrderLogistic` (`Accounting/Accounting.Service/Generated/PhieuBanTraLaiService.cs`); lỗi `:44` `throw` để MassTransit retry.

**(C) Job đối soát MB:**
1. `JobDoiSoatMbBank.StartAsync` (`WebhookBank/Payment.API/Jobs/JobDoiSoatMbBank.cs:14`) → `Task.Run` → `XuLyPhanDoiSoatBankService.Run` (`:41`).
2. Vòng lặp `Run`: giờ < 8 → chờ 1h (`:72-76`); đã xong → chờ 30 phút (`:77-81`); `d = today.AddDays(-1)` (`:83`); chưa có file TC → chờ 30 phút (`:85-90`); đã có TH → done, chờ giờ tròn (`:97-104`); `DoiSoatBankMB(d)` (`:110`).
3. `DoiSoatBankMB` (`WebhookBank/BankReceive.Service/Generated/DoiSoatService.cs:44`): init/clear `TempDoiSoat` (`:53-54`), đọc file `Split("|")` parse cứng cột (`:62-99`), insert temp (`:104`), query thừa MB (`:107-115`), thừa Sandbox (`:118-124`), lệch (`GetGiaoDichLechMBVaSandBox` `:177`); khớp đủ → `DoiSoatHopLeTH` (`:186`), lệch → `DoiSoatSaiLechTH` (`:215`) + `DoiSoatSaiLechCL` (`:244`).

## Bug-prone / lưu ý (path:line)
- **Consumer nuốt lỗi (A):** `EventAutoPublishInvoiceConsumer.cs:25-28` + `EventAutoPublishInvoiceReSendConsumer.cs:25-28` — `catch (Exception) {}` rỗng → lỗi xử lý làm đơn **không vào hàng đợi mà không cảnh báo, không retry, không log**. (Cùng kiểu với `SendMessageCreateAutoPublishInvoice` `CommonService.cs:3792-3794`.)
- **Job (B) không tự retry bản ghi FAILED:** `SetFailed` set `Status=FAILED, IsProcessing=false` (`DonHangDoiSoatLogService.cs:3116-3125`), nhưng `ProcessDoiSoatDonHoanTaoPhieuJob` chỉ lấy bản ghi `Status rỗng` (`:3041`) → bản ghi FAILED **không bao giờ được job chạy lại**, phải reset tay.
- **Lệch trạng thái 2 bên (B):** OrderLogistic set `SUCCESS` ngay khi **publish thành công** (`:3086`), không phản ánh việc Accounting tạo phiếu lỗi (Accounting `throw` để retry — `PhieuBanTraLaiDoiSoatConsummer.cs:44`). Hàng đợi báo SUCCESS nhưng phiếu trả lại có thể chưa/không lên.
- **`IsRunning` là biến tĩnh in-process (B):** `JobDoiSoatDonTaoPhieu.cs:9` — chống chạy chồng trong 1 instance; nếu chạy nhiều instance (scale-out) thì **không chống chồng cross-instance** → 2 instance có thể cùng lock/publish (lock DB `IsProcessing` giảm rủi ro nhưng có khe race giữa đọc batch và update).
- **Job (B) `Execute` nuốt lỗi cấp ngoài:** `JobDoiSoatDonTaoPhieu.cs:32-36` `catch (Exception ex) {}` chỉ reset `IsRunning`, KHÔNG log (chỉ `WorkerXuLy` mới `LogHelper.Error` — `:48`). Lỗi ngoài `WorkerXuLy` mất dấu.
- **Job (C) parse file cứng:** `DoiSoatService.cs:64-98` `Split("|")` + index cố định `rowData[0..20]`, `decimal.Parse(rowData[8])` không try-catch theo dòng → file sai cột/sai số làm cả lần đối soát ném lỗi (job log + đợi giờ tròn, không xuất kết quả).
- **Job (C) phụ thuộc filesystem + working directory:** đường dẫn `Path.Combine(Directory.GetCurrentDirectory(), "mbbank/...")` — phụ thuộc CWD process; chỉ làm cho kênh `0G` (MB), hardcode `ChanelName="0G"` (`:21`).
- **Job (C) `Run` là `async void`** (`:41`) — exception ngoài try lớn có thể không bắt được (đã bọc try/catch trong nhưng `async void` vẫn rủi ro nếu lỗi trước khi vào try).
- **`DoiSoatBankController` (webhook đối soát MB) CHẾT:** toàn bộ comment (`WebhookBank/Payment.API/Controllers/DoiSoatBankController.cs:7-28`, ngày hardcode `2022,08,03`) — chỉ job (C) là mạch đối soát MB sống.
- **`MerchantOrderController.PaymentReceive` anonymous nhận tiền Production** (`registry/ke-toan/MerchantOrderController.md`) — nguồn `MerchantOrder` mà job (C) đối soát; bảo mật phụ thuộc service tự verify.
- **Skip đơn đã có HĐ theo `InvCode/InvNo/InvDate` (A):** nếu sau phát hành 3 trường không set đúng → rủi ro **trùng hóa đơn** (kế thừa từ `tao-hoa-don-tu-dong`).

## File source (path:line)
- (A) Consumer OrderLogistic: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\OrderLogistic\OrderLogisticConsumer.API\Consumer\EventAutoPublishInvoiceConsumer.cs` · `EventAutoPublishInvoiceReSendConsumer.cs` · `EventCreateAutoPublishInvoiceConsumer.cs`.
- (A) Consumer Order: `...\Order\Order.API\Consumer\EventAutoPublishInvoiceConsumer.cs` · `EventAutoPublishInvoiceReSendConsumer.cs`.
- (A) Publish + hàng đợi: `...\OrderLogistic\OrderLogistic.Service\Generated\Common\CommonService.cs:3772,3796,3827,3876`.
- (B) Job: `...\OrderLogistic\OrderLogisticConsumer.API\Jobs\JobDoiSoatDonTaoPhieu.cs:7`.
- (B) Đăng ký Quartz: `...\OrderLogistic\OrderLogisticConsumer.API\Service\JobExtention.cs:67-79`.
- (B) Service: `...\OrderLogistic\OrderLogistic.Service\Generated\DonHangDoiSoatLogService.cs:3036` (process), `:2607-2787` (enqueue), `:3105/3116` (Set status).
- (B) Entity: `...\OrderLogistic\OrderLogistic.DataContext\Entity\DoiSoatDonHoanTaoPhieu.cs`.
- (B) Accounting consumer: `...\Accounting\Accounting.API\Consumer\PhieuBanTraLaiDoiSoatConsummer.cs:10`.
- (B) Accounting service: `...\Accounting\Accounting.Service\Generated\PhieuBanTraLaiService.cs` (`HandleCreatePhieuTraLaiFromOrderLogistic`).
- (C) Job: `...\WebhookBank\Payment.API\Jobs\JobDoiSoatMbBank.cs:5` (class), `:41` (Run).
- (C) Đăng ký: `...\WebhookBank\Payment.API\Service\ServicesExtention.cs:42`.
- (C) Service: `...\WebhookBank\BankReceive.Service\Generated\DoiSoatService.cs:44` (DoiSoatBankMB), `:28/36` (kiểm tra file), `:177` (lệch), `:186/215/244` (xuất file TH/CL).
- (C) Controller chết: `...\WebhookBank\Payment.API\Controllers\DoiSoatBankController.cs:7-28`.
