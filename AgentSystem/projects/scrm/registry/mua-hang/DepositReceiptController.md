---
controller: DepositReceiptController
module: mua-hang
service: Deposit
man_hinh_ui: "Mua hàng/Đặt cọc > màn 'Quản lý đặt cọc' (/deposit-receipt/deposit-receipt-manager). Lưới phiếu cọc + bộ lọc (chi nhánh, sản phẩm, TT thanh toán, TT giao, TT hoàn cọc, TT gửi SMS/Zalo, nhiều mốc ngày). Nút/menu dòng: Thay sản phẩm, Cập nhật TT thanh toán, Hoàn cọc → Xác nhận hoàn cọc, Xác nhận sẽ đợi đợt tiếp, Xem lịch sử, Xóa khỏi đợt trả cọc, Xuất Excel."
muc_dich: "Quản lý vòng đời phiếu đặt cọc của 1 khách cho 1 sản phẩm: thanh toán tiền cọc, thay sản phẩm đã cọc, hoàn cọc (2 bước yêu cầu→xác nhận), xác nhận đợi đợt sau, gỡ khỏi đợt trả cọc. Mỗi thao tác bắn SMS/Zalo theo action."
alias: ["đặt cọc", "dat coc", "phiếu cọc", "phieu coc", "hoàn cọc", "hoan coc", "thay sản phẩm đặt cọc", "tiền cọc", "deposit receipt", "quản lý đặt cọc"]
auth: public
state_machine: [ChuaThanhToan, DaThanhToan, IsDepositRefund(yêu-cầu-hoàn), IsConfirmRefund(đã-hoàn), IsConfirmWaitNext(đợi-đợt-sau), IsDelivered(đã-giao)]
deprecated: false
entities: [DepositReceipt, DepositOrderDeliveryBatch, HistoryModifyDepositReceipt(MongoDB)]
stored_procs: []
depends: [CommonService/GetChiNhanh, CommonService/GetSanPhamByListMa, CommonService/GetSanPhamByListId, CommonService/FindOrCreatedPrivate, MassTransit/RabbitMQ (publish SMS+voucher), MongoDB (audit), EFCore.BulkExtensions (BulkUpdate), GemBox.Spreadsheet (export)]
cross_module_in: [Landing-page (publish MessageCreateDepositReceiptFromLandingDto → tạo phiếu cọc), DeliveryConnect/Order (publish MessageUpdateStatusDeliveriedByVoucherCode → IsDelivered=true)]
cross_module_out: [SMS/Zalo service (MessageDeposit_EventSendSMSByAction + MessageDepositReceiptSendSmsFirstTime), Marketing/Voucher (MessageRemoveVoucherCode khi hoàn cọc)]
---

# DepositReceiptController — Quản lý đặt cọc (vòng đời phiếu cọc: thanh toán → thay SP / hoàn cọc / đợi đợt sau)

> Core của service Deposit. Khóa nhóm là `DepositCode`/`IdTransaction`: 1 phiếu cọc = 1 khách × 1 sản phẩm. Trạng thái KHÔNG phải 1 enum tuyến tính mà là TẬP cờ độc lập: `DepositPaymentStatus` (0 ChuaThanhToan / 1 DaThanhToan) + `IsDelivered` + `IsDepositRefund` (đã yêu cầu hoàn) + `IsConfirmRefund` (đã xác nhận hoàn) + `IsConfirmWaitNext` (KH chọn đợi đợt sau) + `TypeDepositReceipt` (0 gốc / 1 thay thế / 2 bị thay thế — loại 2 LUÔN bị ẩn khỏi mọi search). Mỗi hành động trạng thái đều publish message gửi SMS/Zalo theo action (REPLACE/REFUND/WAIT/CONFIRM_REFUND/RECEIVED) — **controller này KHÔNG tự gửi SMS, chỉ bắn message qua RabbitMQ**; việc gửi thật do DepositSmsSend/SMS-service xử lý. Controller hằng `[SBAuthorize(key, mô tả, "DV-AD-TONG, DV-QL-KHO-TONG")]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới phiếu cọc + bộ lọc | `POST api/DepositReceipt/SearchWithConditions` | `DepositReceiptController_SearchWithConditions` | public | màn quản lý đặt cọc | Lọc đa điều kiện: chi nhánh, SP, TT thanh toán/giao/hoàn cọc/gửi SMS-Zalo, khoảng ngày theo loại mốc (ngày cọc / thanh toán / vào đợt / thay thế / hoàn / giao). LINQ + `.TableWithNolock()`, paging, **luôn loại `TypeDepositReceipt=2` (bị thay thế)**. | **EF** (không SP) | — |
| 2 | Cập nhật TT thanh toán | Nút "Cập nhật trạng thái thanh toán" | `POST api/DepositReceipt/UpdatePaymentStatusDepositReceipt` | `DepositReceiptController_UpdatePaymentStatusDepositReceipt` | public | dòng phiếu cọc | Đổi TT thanh toán (0↔1); set PaymentDate/PaymentCode/PaymentMethodName/AmountPaid. Nếu phiếu chưa tồn tại → publish tạo mới (MassTransit). Ghi lịch sử MongoDB. Khi chuyển sang DaThanhToan lần đầu → publish `MessageDepositReceiptSendSmsFirstTime`. | **EF** + MongoDB + publish | **avgPaid sai**: `Math.Round(AmountPaid ?? 0 / count, 2)` ưu tiên `0/count` trước → tiền TB chia luôn = 0 (L505, L583-584, L760). |
| 3 | Thay sản phẩm đã đặt cọc | Nút "Thay sản phẩm" | `POST api/DepositReceipt/ReplaceDepositReceipt` | `DepositReceiptController_ReplaceDepositReceipt` | public | chỉ khi DaThanhToan & chưa vào đợt trả cọc & chưa confirm đợi | Đánh dấu phiếu cũ `TypeDepositReceipt=2` (bị thay thế, ẩn khỏi search); tạo phiếu thay thế `Type=1`. Gating: `DepositPaymentStatus=DaThanhToan` AND `IdDepositOrderDeliveryBatch=null` AND `IsConfirmWaitNext!=true`. Publish SMS action REPLACE. | **EF** + publish | — |
| 4 | Hoàn cọc (yêu cầu) | Nút "Hoàn cọc" | `POST api/DepositReceipt/DepositRefund` | `DepositReceiptController_DepositRefund` | public | chỉ khi DaThanhToan & chưa hoàn & chưa giao | Gating: `PaymentStatus=DaThanhToan` AND `IsDepositRefund!=true` AND `IsDelivered!=true`. Set `IsDepositRefund=true` + thông tin NH (số TK validate `RegexHelpers.isMa()` L1812, tên, chi nhánh, lý do) + DepositRefundDate. Publish SMS action REFUND + `MessageRemoveVoucherCode` (hủy voucher). | **EF** + publish | — |
| 5 | Lấy info hoàn cọc | (popup "Thông tin hoàn cọc") | `POST api/DepositReceipt/GetInfoDepositRefund/{id:Guid}` | `DepositReceiptController_GetInfoDepositRefund` | public | dòng đã yêu cầu hoàn | Trả chi tiết hoàn cọc nếu `IsDepositRefund=true` (info NH, số tiền hoàn, mốc xác nhận, lý do). | **EF** | — |
| 6 | Xác nhận hoàn cọc | Nút "Xác nhận hoàn cọc" (popup) | `POST api/DepositReceipt/ConfirmRefund` | `DepositReceiptController_ConfirmRefund` | public | chỉ khi đã yêu cầu hoàn & chưa xác nhận | Gating: `IsDepositRefund=true` AND `IsConfirmRefund!=true`. Set `IsConfirmRefund=true`, DepositRefundAmount, ConfirmRefundDateTime, UserIdConfirmRefund. **Cập nhật MỌI bản ghi cùng `DepositCode`**. Publish SMS action CONFIRM_REFUND. | **EF** + publish | — |
| 7 | Xác nhận sẽ đợi đợt sau | Nút "Xác nhận sẽ đợi đợt tiếp theo" | `POST api/DepositReceipt/ConfirmWait/{depositCode}` | `DepositReceiptController_ConfirmWait` | public | dòng chưa confirm đợi | Gating: `IsConfirmWaitNext!=true` (idempotent — chỉ lần đầu hiệu lực). Set `IsConfirmWaitNext=true`. Publish SMS action WAIT. | **EF** + publish | — |
| 8 | Lấy trạng thái phiếu cọc | (refresh trạng thái dòng) | `POST api/DepositReceipt/GetStatusDepositReceipt/{depositCode}` | `DepositReceiptController_GetStatusDepositReceipt` | public | dòng phiếu cọc | Trả cờ tổng hợp: IsAssignedToBatch, IsDelivered, IsConfirmedWait, IsPaid, IsRefunded, IsReplaced. | **EF** | — |
| 9 | Xem lịch sử thay đổi | Nút "Lịch sử" (popup history) | `POST api/DepositReceipt/GetHistoryModifyDepositReceipt/{idTransaction:Guid}` | `DepositReceiptController_GetHistoryModifyDepositReceipt` | public | dòng phiếu cọc | Lấy entity + toàn bộ `HistoryModifyDepositReceipt` từ MongoDB theo IdTransaction; enrich thuộc tính SP từ CommonService. | **EF** + MongoDB | — |
| 10 | Xóa khỏi đợt trả cọc | Nút "Xóa khỏi đợt trả cọc" | `POST api/DepositReceipt/RemoveFromDeliveryBatch` | `DepositReceiptController_RemoveFromDeliveryBatch` | public | dòng đã vào đợt & chưa giao | Gating: `IdDepositOrderDeliveryBatch!=null` AND `IsDelivered!=true`, **tối đa 1000 phiếu/lần**. Clear `IdDepositOrderDeliveryBatch` + `DepositOrderDeliveryBatchDate` (BulkUpdate). | **EF** BulkUpdate | — |
| 11 | Xuất Excel quản lý | Nút "Xuất Excel" | `POST api/DepositReceipt/ExportExcelDepositManager` | `DepositReceiptController_ExportExcelDepositManager` | public | màn quản lý | Gọi SearchWithConditions nội bộ → 38 cột (cọc/thanh toán/thay thế/hoàn cọc). Trả file `ReportDatCoc.xlsx`. Lỗi `Service_Unavailable`→HTTP 503, lỗi khác→500. | **EF** + GemBox | License GemBox hardcode `"ERDD-..."` (L1366); `throw;` dead-code sau return (L1714). |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Thay sản phẩm:** `DepositPaymentStatus=DaThanhToan` AND `IdDepositOrderDeliveryBatch=null` AND `IsConfirmWaitNext!=true`.
- **Hoàn cọc (yêu cầu):** `DepositPaymentStatus=DaThanhToan` AND `IsDepositRefund!=true` AND `IsDelivered!=true`.
- **Xác nhận hoàn cọc:** `IsDepositRefund=true` AND `IsConfirmRefund!=true` → áp cho mọi bản ghi cùng DepositCode.
- **Xác nhận đợi đợt sau:** `IsConfirmWaitNext!=true` (idempotent).
- **Gỡ khỏi đợt trả cọc:** `IdDepositOrderDeliveryBatch!=null` AND `IsDelivered!=true`, ≤1000 phiếu/lần.
- **Mọi search/export** loại `TypeDepositReceipt=PhieuDatCocBiThayThe(2)`.
- **Luồng hoàn cọc 2 bước:** IsDepositRefund=true (yêu cầu) → IsConfirmRefund=false (chờ) → IsConfirmRefund=true (đã hoàn).

## Quan hệ
- **Gọi sang:** CommonService (chi nhánh, sản phẩm theo mã/Id, find-or-create khách). Publish RabbitMQ tới SMS/Zalo service + Marketing(voucher).
- **Dữ liệu (entities):** ghi `DepositReceipt`; đọc `DepositOrderDeliveryBatch`; audit `HistoryModifyDepositReceipt` (MongoDB, TypeModify ~8 loại thao tác).
- **Ripple / đồng bộ:** mỗi action publish `MessageDeposit_EventSendSMSByAction` (REPLACE/REFUND/WAIT/CONFIRM_REFUND/RECEIVED); thanh toán lần đầu publish `MessageDepositReceiptSendSmsFirstTime`; hoàn cọc publish `MessageRemoveVoucherCode`. Consume `MessageUpdateStatusDeliveriedByVoucherCode` → set IsDelivered=true.

## Luồng xuyên module
- **Tạo từ landing:** Landing-page publish `MessageCreateDepositReceiptFromLandingDto` → service tạo phiếu cọc bất đồng bộ (không qua UI).
- **Hoàn cọc:** UI DepositRefund → set cờ hoàn + publish RemoveVoucherCode (Marketing hủy mã) + SMS REFUND → (sau khi chi tiền) ConfirmRefund → SMS CONFIRM_REFUND.
- **Giao hàng:** Order/DeliveryConnect publish `MessageUpdateStatusDeliveriedByVoucherCode` → phiếu cọc set IsDelivered=true (khóa hoàn cọc/thay SP).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Deposit/Deposit.API/Controllers/DepositReceiptController.cs` — toàn bộ endpoint role `DV-AD-TONG, DV-QL-KHO-TONG`.
- BE `Deposit/Deposit.Service/Generated/DepositReceiptService.cs`
  - **L505 / L583-584 / L760**: `Math.Round(AmountPaid ?? 0 / count, 2)` — sai thứ tự toán tử (`0/count` chạy trước) → tiền cọc TB **luôn = 0**. Cần `(AmountPaid ?? 0) / count`.
  - L737: `CreateDepositReceipt()` trả `null` khi trùng cache-key nhưng caller không null-check trước khi lặp (rủi ro NRE).
  - L1366: license GemBox hardcode trong code (nên đưa vào config).
  - L1712-1714: `ExportExcelDepositManager` đã `return` lỗi qua MethodResult rồi `throw;` → dead code.
  - L973-976: try/catch log lỗi ghi error-log bị nuốt (không re-throw).
- FE `scrm_frontend/src/app/pages/deposit-receipt/deposit-receipt-manager/` (manager + popup deposit-refund-info, deposit-confirm-refund, deposit-receipt-history); route `deposit-receipt/deposit-receipt-manager`.
