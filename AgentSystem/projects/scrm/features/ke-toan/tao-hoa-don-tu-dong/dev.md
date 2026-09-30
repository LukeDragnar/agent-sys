# Tạo hóa đơn tự động — Kỹ thuật

> Luồng nền (background, không UI trực tiếp): trạng thái giao hàng đạt mốc cấu hình → publish RabbitMQ → consumer xếp đơn vào bảng hàng đợi (`DonHangTimeToPublishInvoice` / `DonHangTimeToCreateDraft`) với độ trễ → job phát hành qua luồng HĐĐT chung. Gắn 2 nguồn đơn: cửa hàng (`DonHang_TaoDon` — `registry/cua-hang/DonHangController.md`) và đơn online logistic (`KeToanTacNghiep` — `registry/kho/KeToanTacNghiepController.md`). Phát hành chung qua Invoice service (`registry/ke-toan/InvoiceController.md`).

## Màn hình & route
- Không có màn riêng để "tạo hóa đơn tự động". Cấu hình nằm ở: cấu hình trạng thái giao hàng (cờ `IsAutoPublishInvoice`) + cấu hình đơn vị (`LoaiHoaDonTuDong`, `XuatHoaDonTuDongSauSoPhut`, `TaoNhapTuDongSauSoPhut`). Route FE cụ thể cần dev xác nhận.

## Endpoint/Controller liên quan
- **Trigger (đổi trạng thái → tự phát hành):** `DonHang_TaoDon`/vòng đời đơn (cửa hàng) hoặc cập trạng thái giao hàng (Kế toán tác nghiệp). `DonHang_TaoDon` = MaTinhNang `POST api/DonHang/TaoDon` trong `DonHangController` (Order) — `registry/cua-hang/DonHangController.md`.
- **Publish auto-publish:** `CommonService.SendMessageCreateAutoPublishInvoice(...)` → publish `CreateAutoPulishInvoiceMesssage`.
- **Consumer:** `EventAutoPublishInvoiceConsumer : IConsumer<InputDataJobAutoPublish_ToOrderLogistic>` → `IDonHangService.QueueGetInfoOrderToAutoPublishInvoice(item)`.
- **Phát hành thật:** qua Invoice service `InvoiceController.CreateInvoice` (PrivateSBAuthorize) → publish queue NCC (retry 3 lần) → consumer phát hành (xem feature "Hóa đơn điện tử").
- **Hủy phát hành tự động / gửi lại:** `EventAutoPublishInvoiceReSendConsumer` (Order + OrderLogistic), `HuyPhatHanhTuDong` (DonHangController).

## Entities / bảng hàng đợi
- `DonHangTimeToPublishInvoice` — hàng đợi đơn chờ tự **phát hành** (`Status=Pending`, `TimeToPublish`, `ActionCode=Publish`).
- `DonHangTimeToCreateDraft` (+ bảng ReSend) — hàng đợi đơn chờ tự **tạo nháp**.
- `LogHoaDonDienTuTuDong` (MongoDB collection `LogHoaDonDienTuTuDong`) — log kết quả/skip/fail từng đơn.
- `DonHangLogistic` (đơn online) / `DonHang` (đơn cửa hàng) — nguồn đơn. `InvCode`/`InvNo`/`InvDate` quyết định đơn đã có HĐ.
- Cấu hình: `CauHinhTrangThaiGiaoHangDonViItem.IsAutoPublishInvoice`; `CauHinhSettingDto.LoaiHoaDonTuDong` / `.XuatHoaDonTuDongSauSoPhut` / `.TaoNhapTuDongSauSoPhut`.

## Luồng code (path:line — verified)
1. `CommonService.SendMessageCreateAutoPublishInvoice` (`...\OrderLogistic\OrderLogistic.Service\Generated\Common\CommonService.cs:3772`) — publish `CreateAutoPulishInvoiceMesssage`.
   - Được gọi từ `DonHangService.cs` (OrderLogistic) tại `:6891`, `:7483`, `:7682` (sau khi đổi trạng thái đơn).
2. `EventAutoPublishInvoiceConsumer.Consume` (`...\OrderLogisticConsumer.API\Consumer\EventAutoPublishInvoiceConsumer.cs:18-31`) → `QueueGetInfoOrderToAutoPublishInvoice` (try/catch nuốt lỗi — xem bug-prone).
3. `CommonService.TimeToPublish_Create` (`CommonService.cs:3796`):
   - `:3827` `listTrangThaiDuocPhepPhatHanh = cauHinhTrangThaiGiaoHang.Where(IsAutoPublishInvoice == true).Select(Ma)`.
   - `:3833-3873` nhánh **NHÁP** (`LoaiHoaDonTuDong == 2`): độ trễ `TaoNhapTuDongSauSoPhut ?? 2`; skip đơn không đúng trạng thái (`:3854`), đơn đã có HĐ (`:3856`), đơn đã trong bảng draft (`:3857`) hoặc ReSend (`:3859`); insert `DonHangTimeToCreateDraft`.
   - `:3874-...` nhánh **PHÁT HÀNH**: độ trễ `XuatHoaDonTuDongSauSoPhut ?? 5` (`:3876`); skip đơn không đúng trạng thái (`:3882`); đơn đã có HĐ → log Fail "Đơn hàng đã phát hành trước đó" (`:3887-3903`); enqueue `DonHangTimeToPublishInvoice`.
   - `:3804-3826` nếu cấu hình trạng thái giao hàng rỗng → log Fail "Không lấy được cấu hình trạng thái giao hàng".

## Bug-prone / lưu ý (path:line)
- **Consumer nuốt lỗi:** `EventAutoPublishInvoiceConsumer.cs:25-28` — `catch (Exception) {}` rỗng → nếu `QueueGetInfoOrderToAutoPublishInvoice` ném lỗi, đơn KHÔNG được xếp hàng đợi mà không có cảnh báo (mất tự phát hành im lặng). `SendMessageCreateAutoPublishInvoice` (`CommonService.cs:3792-3794`) cũng `catch {}` rỗng.
- **Gating chỉ dựa trạng thái + LoaiHoaDonTuDong** (OrderLogistic), KHÔNG có cờ bật riêng (comment ghi rõ `:3835`). Cấu hình sai trạng thái → đơn không tự ra HĐ hoặc ra ngoài ý muốn.
- **Skip đơn đã có HĐ theo `InvCode`/`InvNo`/`InvDate`** — nếu 3 trường này không được set đúng sau phát hành, có rủi ro tạo trùng (phụ thuộc consumer phát hành ghi đúng).
- **Độ trễ mặc định:** phát hành 5 phút, nháp 2 phút (`?? 5` / `?? 2`).
- **Chế độ nháp KHÔNG tự phát hành** — chỉ enqueue draft; cần đồng bộ nháp → phát hành thủ công (luồng InvoiceController.DongBoTrangThaiHoaDonNhap).
- **Phát hành chung kế thừa bug HĐĐT:** publish queue fail cả 3 retry vẫn coi như "đã nhận" (xem feature "Hóa đơn điện tử" — `HoaDonDienTuService.cs:91-144`).
- **Verb GET có side-effect ở đồng bộ kế toán liên quan:** `DonHangKeToanController.DonHangKeToan_PhieuChoDonHangOnline_Save` là GET tạo chứng từ (non-idempotent) — luồng song song khi đơn đạt mốc trạng thái (xem `registry/kho/DonHangKeToanController.md`).

## File source (path:line)
- BE publish + xếp hàng đợi: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\OrderLogistic\OrderLogistic.Service\Generated\Common\CommonService.cs` (`:3772` SendMessage..., `:3796` TimeToPublish_Create, `:3827` lọc trạng thái, `:3833` nhánh nháp, `:3876` độ trễ phát hành).
- BE gọi publish: `...\OrderLogistic\OrderLogistic.Service\Generated\DonHangService.cs:6891,7483,7682`.
- BE consumer: `...\OrderLogistic\OrderLogisticConsumer.API\Consumer\EventAutoPublishInvoiceConsumer.cs` · `EventAutoPublishInvoiceReSendConsumer.cs` · `EventCreateAutoPublishInvoiceConsumer.cs`.
- BE consumer Order: `...\Order\Order.API\Consumer\EventAutoPublishInvoiceConsumer.cs` · `EventAutoPublishInvoiceReSendConsumer.cs`.
- BE phát hành chung: `...\Invoice\Invoice.API\Controllers\InvoiceController.cs` + `...\Invoice\Invoice.Service\Generated\HoaDonDienTuService.cs`.
- BE đồng bộ kế toán song song: `...\OrderLogistic\OrderLogistic.API\Controllers\DonHangKeToanController.cs` + `...\DonHang\DonHangKeToanService.cs`.
- BE trigger đơn cửa hàng (DonHang_TaoDon): `...\Order\Order.API\Controllers\DonHangController.cs` (`TaoDon`).
