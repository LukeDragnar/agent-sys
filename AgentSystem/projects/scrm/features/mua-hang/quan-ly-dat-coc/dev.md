# Dev — Quản lý đặt cọc

> Service BE: **Deposit** (controller `DepositReceiptController` + `DepositOrderDeliveryBatchController`). FE module **deposit-receipt** (Angular 15 + ng-zorro). Chỉ dành cho dev.
> Registry: `projects/scrm/registry/mua-hang/DepositReceiptController.md`, `projects/scrm/registry/mua-hang/DepositOrderDeliveryBatchController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Quản lý đặt cọc (phiếu cọc) | `deposit-receipt/deposit-receipt-manager` | `DepositReceiptManagerComponent` (popup: deposit-refund-info, deposit-confirm-refund, deposit-receipt-history) |
| Đợt trả cọc | `deposit-receipt/deposit-order-delivery-batch-manager` | `DepositOrderDeliveryBatchManagerComponent` (popup: batch-edit, batch-sms-edit) |
| Báo cáo đặt cọc | `deposit-receipt/deposit-report` | `DepositReportComponent` (gọi `api/DepositReport` — controller riêng) |
| Gửi SMS custom theo đợt | `deposit-receipt/batch-send-sms-custom` | `DepositBatchCustomManagerComponent` |
| Quản lý SMS gửi | `deposit-receipt/sms-manager` | `DepositSmsSendViewComponent` |
| Cấu hình SMS | `deposit-receipt/config-sms` | `DepositConfigSmsManagerComponent` |

- Route module: `deposit-receipt/deposit-receipt-routing.module.ts:11/:16/:24` (manager / batch-manager / deposit-report).
- Liên quan (Kho): `warehouse-product/du-kien-dat-coc` (`DuKienDatCocComponent`, dự kiến đặt cọc — nằm ở module WarehouseProduct routing `:233`).

## Endpoint / Controller (trace registry)

**`DepositReceiptController`** (service Deposit, role `DV-AD-TONG, DV-QL-KHO-TONG`). Trạng thái = TẬP cờ độc lập: `DepositPaymentStatus` (0/1), `IsDelivered`, `IsDepositRefund`, `IsConfirmRefund`, `IsConfirmWaitNext`, `TypeDepositReceipt` (0 gốc / 1 thay thế / 2 bị thay thế — luôn ẩn).
| Key | Route | Mục đích |
|---|---|---|
| `..._SearchWithConditions` | `POST api/DepositReceipt/SearchWithConditions` | Lưới (luôn loại Type=2). EF `.TableWithNolock()`. |
| `..._UpdatePaymentStatusDepositReceipt` | `POST .../UpdatePaymentStatusDepositReceipt` | Đổi TT thanh toán; lần đầu DaThanhToan → SMS first-time. |
| `..._ReplaceDepositReceipt` | `POST .../ReplaceDepositReceipt` | Thay SP (gating đã thanh toán & chưa vào đợt & chưa đợi). |
| `..._DepositRefund` | `POST .../DepositRefund` | Hoàn cọc (yêu cầu) + hủy voucher. |
| `..._GetInfoDepositRefund` | `POST .../GetInfoDepositRefund/{id:Guid}` | Info hoàn cọc. |
| `..._ConfirmRefund` | `POST .../ConfirmRefund` | Xác nhận hoàn (áp mọi bản ghi cùng DepositCode). |
| `..._ConfirmWait` | `POST .../ConfirmWait/{depositCode}` | Đợi đợt sau (idempotent). |
| `..._GetStatusDepositReceipt` | `POST .../GetStatusDepositReceipt/{depositCode}` | Trả cờ tổng hợp. |
| `..._GetHistoryModifyDepositReceipt` | `POST .../GetHistoryModifyDepositReceipt/{idTransaction:Guid}` | Lịch sử MongoDB. |
| `..._RemoveFromDeliveryBatch` | `POST .../RemoveFromDeliveryBatch` | Gỡ khỏi đợt (≤1000). |
| `..._ExportExcelDepositManager` | `POST .../ExportExcelDepositManager` | 38 cột → `ReportDatCoc.xlsx` (503→HTTP 503). |

**`DepositOrderDeliveryBatchController`** (service Deposit, role `DV-AD-TONG, DV-QL-KHO-TONG`). 2 trạng thái: `IsSended=false` (nháp) / `true` (khóa).
| Key | Route | Mục đích |
|---|---|---|
| `..._Create` / `_Update` / `_GetById` / `_Delete` | `POST .../Create`, `/Update`, `/GetById/{id}`, `/Delete/{id}` | CRUD đợt (Update/Delete chỉ khi !IsSended). |
| `..._SearchWithConditions` | `POST .../SearchWithConditions` | Lưới đợt. |
| `..._GanDotTraCoc` | `POST .../GanDotTraCoc/{id:Guid}` | Gán phiếu (SP `PRC_SCRM_Deposit_GetAndUpdateIdBatch`, chunk 1000). |
| `..._XoaDanhSachTraCoc` | `POST .../XoaDanhSachTraCoc/{id:Guid}` | Gỡ phiếu khỏi đợt (BulkUpdate, `.Take(1000)`). |
| `..._GetStatsBatch` | `POST .../GetStatsBatch/{id:Guid}` | SP `PRC_SCRM_Deposit_GetStatsDepositReceiptByBatch`. |
| `..._UpdateSMSInfo` | `POST .../UpdateSMSInfo` | Mẫu SMS/Zalo + JsonParam. |
| `..._SetSended` | `POST .../SetSended/{id:Guid}` | Khóa đợt (cần ≥1 mẫu). |
| `..._DepositReceiptBatchAutoComplete` | `POST .../DepositReceiptBatchAutoComplete` | Autocomplete đợt. |

## Entities
- `DepositReceipt` (khóa nhóm `DepositCode`/`IdTransaction`), `DepositOrderDeliveryBatch` + `DepositOrderDeliveryBatchProduct`, `DepositSmsSend`.
- Audit MongoDB: `HistoryModifyDepositReceipt`, `HistoryModifyDepositOrderDeliveryBatch`.
- Stored procs: `PRC_SCRM_Deposit_GetAndUpdateIdBatch`, `PRC_SCRM_Deposit_GetStatsDepositReceiptByBatch`.

## Luồng code
- Phiếu cọc: mỗi action gating theo tập cờ → cập nhật EF + audit MongoDB + publish RabbitMQ (`MessageDeposit_EventSendSMSByAction` REPLACE/REFUND/WAIT/CONFIRM_REFUND/RECEIVED; `MessageDepositReceiptSendSmsFirstTime`; `MessageRemoveVoucherCode`). **Controller KHÔNG tự gửi SMS** — dịch vụ SMS/Zalo xử lý.
- Consume `MessageUpdateStatusDeliveriedByVoucherCode` → set IsDelivered=true.
- Đợt: gán phiếu qua stored proc (không LINQ), khóa cứng khi IsSended=true; voucher gọi đồng bộ sang Marketing (`DeleteVoucherCodeByIdNguonTao` + `GenVoucherCodeByBatch`).

## Bug-prone / lưu ý (path:line)
> Chỉ ghi nhận — KHÔNG sửa code ở session này.
- **Tiền cọc TB luôn = 0**: `Math.Round(AmountPaid ?? 0 / count, 2)` sai thứ tự toán tử (`0/count` chạy trước) — `DepositReceiptService.cs:505 / 583-584 / 760`. Cần `(AmountPaid ?? 0)/count`.
- **NRE rủi ro**: `CreateDepositReceipt()` trả null khi trùng cache-key nhưng caller không null-check trước khi lặp (`DepositReceiptService.cs:737`).
- **License GemBox hardcode** trong code: `DepositReceiptService.cs:1366`.
- **Dead code**: `ExportExcelDepositManager` `return` lỗi rồi `throw;` (`:1712-1714`); error-log bị nuốt (`:973-976`).
- **`XoaDanhSachTraCoc` `.Take(1000)` cứng** (`DepositOrderDeliveryBatchService.cs:192`) → đợt >1000 phiếu gỡ thiếu.
- **`LockBatch`/`UnLockBatch` no-op** (`:159-160 / :175-176`): gọi UpdateAsync nhưng không đổi field nào (code đổi cờ bị comment).
- **`SetSended` khóa được đợt rỗng** (`:987-1003`): không kiểm tra số phiếu đã gán.
- **`GanDotTraCoc` không bọc transaction** (`:376-407`): SP fail giữa chừng → trạng thái dở; SP trả 0 → break im lặng.
- **`TaoMaVoucher` giới hạn 50×1000** (`:284`) → >50k phiếu dừng sớm.
- **Delete đợt trước SP** (`:514-515`) → có thể orphan product nếu lỗi giữa chừng; mess "Không tìm thấy dữ liệu cần xóa" dùng cả cho path không-xóa (`:492 / :993`).
- **Số TK ngân hàng (hoàn cọc) validate `RegexHelpers.isMa()`** (`DepositReceiptService.cs:1812`) — lưu ý ràng buộc định dạng.

## File source (path:line)
- BE controller phiếu cọc: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Deposit\Deposit.API\Controllers\DepositReceiptController.cs`.
- BE controller đợt: `...\Deposit\Deposit.API\Controllers\DepositOrderDeliveryBatchController.cs`.
- BE service phiếu cọc: `...\Deposit\Deposit.Service\Generated\DepositReceiptService.cs` (`:505/:583-584/:760` avgPaid, `:737`, `:1366`, `:1712-1714`, `:1812`).
- BE service đợt: `...\Deposit\Deposit.Service\Generated\DepositOrderDeliveryBatchService.cs` (`:159-176`, `:192`, `:284`, `:376-407`, `:514-515`, `:987-1003`).
- Stored procs (DB): `PRC_SCRM_Deposit_GetAndUpdateIdBatch`, `PRC_SCRM_Deposit_GetStatsDepositReceiptByBatch`.
- FE phiếu cọc: `...\scrm_frontend\src\app\pages\deposit-receipt\deposit-receipt-manager\` (+ popup refund-info / confirm-refund / history).
- FE đợt: `...\deposit-receipt\deposit-order-delivery-batch-manager\` (+ batch-edit / batch-sms-edit).
- FE báo cáo đặt cọc: `...\deposit-receipt\deposit-report\deposit-report.component.ts`; service `services\deposit-receipt\deposit-report.service.ts` (`api/DepositReport`).
- FE routing: `...\deposit-receipt\deposit-receipt-routing.module.ts`.
