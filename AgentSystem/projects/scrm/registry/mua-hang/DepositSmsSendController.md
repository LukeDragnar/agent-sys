---
controller: DepositSmsSendController
module: mua-hang
service: Deposit
man_hinh_ui: "Mua hàng/Đặt cọc > màn 'Quản lý gửi tin nhắn' (/deposit-receipt/sms-manager). Lưới lịch sử SMS/Zalo + thống kê theo trạng thái + bộ lọc (đợt, loại gửi, SP, keyword, TT SMS/Zalo, ngày). Nút: Tạo lịch sử từ đợt, Gửi theo đợt, Gửi tin chưa gửi, Tạo từ pick-list/đợt khác, Gửi đợt tùy chọn, Xóa, Xuất Excel."
muc_dich: "Sinh & gửi tin nhắn SMS/Zalo cho khách trong đợt trả cọc hoặc đợt tùy chọn. Quản lý vòng đời 1 bản ghi gửi (chưa gửi → đã đẩy service → thành công/thất bại). Gửi qua RabbitMQ/MassTransit (không gọi gateway trực tiếp), có khóa Redis chống gửi trùng."
alias: ["gửi tin nhắn đặt cọc", "gui tin nhan dat coc", "lịch sử gửi SMS", "lich su gui sms", "gửi SMS đợt trả cọc", "gửi Zalo", "SMS đặt cọc", "deposit sms send"]
auth: public
state_machine: [ChuaGui(0), DaGuiSangServiceSMS(1), LoiKhiGuiSangServiceSMS(2), ThanhCong(3), ThatBai(4), ChoXuLy(5)]
deprecated: false
entities: [DepositSmsSend, DepositReceipt, DepositOrderDeliveryBatch, BatchSendSmsCustom, HistoryModifyDepositReceipt(MongoDB)]
stored_procs: []
depends: [MassTransit/RabbitMQ (publish SendSmsByIdPattern/SendZaloByIdPattern), Redis (khóa gửi), CommonService/GenVoucherCodeByBatch, EFCore.BulkExtensions (BulkInsert/BulkUpdate), MongoDB (audit), GemBox.Spreadsheet (export)]
cross_module_in: [Job ScanKhachHangChuaThanhToan (nhắc thanh toán tự động), SMS-service (consume EventDepositUpdateStatusSMSSendMessage → cập nhật TT cuối)]
cross_module_out: [SMS/Zalo service (publish SendSmsByIdPatternConsumerDto type=2 / SendZaloByIdPatternConsumerDto type=4), Marketing/Voucher (GenVoucherCodeByBatch khi gửi đợt tùy chọn)]
---

# DepositSmsSendController — Quản lý gửi tin nhắn đặt cọc (SMS/Zalo theo đợt trả cọc & đợt tùy chọn)

> Engine gửi tin của Deposit. 1 bản ghi `DepositSmsSend` = 1 tin của 1 phiếu cọc, có 2 trạng thái song song `StatusSendSms` & `StatusSendZalo` (enum `EnumDepositSendSmsStatus`: 0 ChuaGui → 5 ChoXuLy → 1 DaGuiSangServiceSMS → 3 ThanhCong / 4 ThatBai; 2 LoiKhiGuiSangServiceSMS khi publish lỗi). **Gửi tin = publish message qua MassTransit/RabbitMQ, KHÔNG gọi gateway HTTP trực tiếp**; SMS-service xử lý rồi consume `EventDepositUpdateStatusSMSSendMessage` để chốt TT cuối (ThanhCong/ThatBai). 2 luồng song song: **theo đợt trả cọc** (IdDepositOrderDeliveryBatch) và **đợt tùy chọn** (IdBatchSendCustom, TypeSend=BatchCustom). Gửi theo lô 50 bản ghi, có khóa Redis (TTL 30s) chống gửi đồng thời. Controller hằng `[SBAuthorize(key,..., "DV-AD-TONG, DV-QL-KHO-TONG")]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / lưới | Lưới lịch sử SMS + lọc | `POST api/DepositSmsSend/SearchWithConditions` | `DepositSmsSendController_SearchWithConditions` | public | màn gửi tin nhắn | LINQ lọc theo đơn vị, đợt, TypeSend, SP, keyword (DepositCode/tên KH/SĐT/voucher), TT SMS/Zalo, ngày; paging. | **EF** (không SP) | — |
| 2 | Thống kê gửi tin | (panel thống kê theo TT) | `POST api/DepositSmsSend/GetStats` | `DepositSmsSendController_GetStats` | public | màn gửi tin nhắn | GroupBy đếm tổng + breakdown theo TT (ChuaGui/ChoXuLy/DaGui/ThanhCong/ThatBai) cho SMS & Zalo. | **EF** | — |
| 3 | Tạo lịch sử từ đợt trả cọc | Nút "Tạo lịch sử SMS" | `POST api/DepositSmsSend/InsertFromBatch/{id:Guid}` | `DepositSmsSendController_InsertFromBatch` | public | đợt đã khóa (IsSended) & có mẫu | Tạo bản ghi SMS cho phiếu cọc trong đợt chưa có SMS (chunk 1000); resolve param theo cấu hình đợt; TT khởi tạo ChuaGui. Dedup left-join chống tạo trùng. KHÔNG gửi ngay. | **EF** BulkInsert | — |
| 4 | Gửi theo đợt | Nút "Gửi tin nhắn" | `POST api/DepositSmsSend/SendSmsByBatch/{id:Guid}` | `DepositSmsSendController_SendSmsByBatch` | public | có bản ghi ChuaGui | Khóa Redis; lấy 50 bản ghi ChuaGui → ChoXuLy; publish Sms(type=2)/Zalo(type=4) qua MassTransit; OK → DaGuiSangServiceSMS, lỗi publish → LoiKhiGuiSangServiceSMS. Kiểm tra khung giờ Zalo (`ZaloCheckTime`) — ngoài giờ thì reset về ChuaGui. Cập nhật DepositReceipt + log MongoDB. | **EF** + publish | Exception trong vòng gửi bị nuốt (chỉ set TT, không log — L926-930/975-979) → khó debug. |
| 5 | Gửi tin chưa gửi | Nút "Gửi tin chưa gửi" | `POST api/DepositSmsSend/SendSmsToWaitingStatus` | `DepositSmsSendController_SendSmsToWaitingStatus` | public | có bản ghi ChuaGui khớp lọc | Gửi lại các bản ghi đang ChuaGui khớp điều kiện lọc (manual retry). Lấy 50; cùng logic publish/TT như #4. Check khung giờ Zalo 1 lần. | **EF** + publish | — |
| 6 | Tạo từ pick-list | (chọn phiếu thủ công → tạo đợt tùy chọn) | `POST api/DepositSmsSend/InsertBatchCustomFromPickList` | `DepositSmsSendController_InsertBatchCustomFromPickList` | public | đợt tùy chọn | Tạo bản ghi SMS từ phiếu cọc chọn tay vào BatchCustom (tối đa 100 phiếu/lần). Dedup. Set IdBatchSendCustom, TypeSend=BatchCustom. | **EF** BulkInsert | — |
| 7 | Tạo từ đợt khác | (sao chép từ tệp khác) | `POST api/DepositSmsSend/InsertBatchCustomFromAnotherBatch` | `DepositSmsSendController_InsertBatchCustomFromAnotherBatch` | public | đợt tùy chọn | Clone bản ghi từ đợt nguồn (TypeRoot=1 đợt-trả-cọc / 2 đợt-custom) sang BatchCustom đích; dedup triple-join; chunk 1000. | **EF** BulkInsert | — |
| 8 | Gửi đợt tùy chọn | Nút "Gửi" (đợt custom) | `POST api/DepositSmsSend/SendSmsByBatchCustom/{id:Guid}` | `DepositSmsSendController_SendSmsByBatchCustom` | public | đợt custom có ChuaGui | Như #4 nhưng cho BatchSendSmsCustom; có sinh voucher (`GenVoucherCodeByBatch`, ≤50 mã) merge vào JsonParam khóa `VOUCHER_CODE_CUSTOM` rồi publish. | **EF** + publish | sinh voucher lỗi không return → vẫn gửi không có voucher. |
| 9 | Xóa 1 bản ghi | Nút "Xóa" (dòng) | `POST api/DepositSmsSend/DeleteSmsSendById/{id:Guid}` | `DepositSmsSendController_DeleteSmsSendById` | public | chỉ BatchCustom & ChuaGui | Xóa 1 bản ghi nếu `TypeSend=BatchCustom` AND TT=ChuaGui (hard delete). | **EF** | — |
| 10 | Xóa theo đợt custom | Nút "Xóa danh sách" | `POST api/DepositSmsSend/DeleteSmsSendByIdBachCustom/{id:Guid}` | `DepositSmsSendController_DeleteSmsSendByIdBachCustom` | public | đợt custom | Xóa hàng loạt bản ghi ChuaGui của 1 BatchCustom (soft delete IsDelete/NgayXoa/NguoiXoa, chunk 1000, lặp ≤50); chạm 50k → báo "làm lại". | **EF** BulkUpdate | — |
| 11 | Xuất Excel | Nút "Xuất Excel" | `POST api/DepositSmsSend/ExportExcelDepositSMSSend` | `DepositSmsSendController_ExportExcelDepositSMSSend` | public | màn gửi tin nhắn | Join DepositSmsSend + DepositReceipt → 22 cột (TT SMS/Zalo text, ngày, voucher, SP, TT thanh toán). File `ReportGuiTin.xlsx`. Lỗi `Service_Unavailable`→503, khác→500. | **EF** + GemBox | export theo PageSize, không có safeguard full-export. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Tạo SMS từ đợt:** đợt phải `IsSended=true` và có mẫu SMS hoặc Zalo.
- **Gửi:** chỉ bản ghi `ChuaGui`; chuyển `ChoXuLy` (atomic) trước khi publish; publish OK → `DaGuiSangServiceSMS`, publish lỗi → `LoiKhiGuiSangServiceSMS`. TT cuối (ThanhCong/ThatBai) do SMS-service callback.
- **Khung giờ Zalo:** `ZaloCheckTime()=false` → không gửi Zalo, reset về ChuaGui.
- **Dedup:** left-join chống tạo SMS trùng cho cùng phiếu cọc.
- **Khóa Redis:** SetNotExists TTL 30s chống gửi đồng thời cùng đợt.
- **Giới hạn:** InsertBatchCustomFromPickList ≤100 phiếu/lần; gửi 50 bản ghi/lượt; xóa lô ≤50k/lần.
- **Voucher đợt custom:** sinh nếu `IdVoucher!=null && VoucherCodeCustom==null` → merge `VOUCHER_CODE_CUSTOM`.
- **Xóa:** chỉ TypeSend=BatchCustom & ChuaGui.

## Quan hệ
- **Gọi sang:** MassTransit publish SMS/Zalo; Redis (khóa); CommonService.GenVoucherCodeByBatch; EFCore.BulkExtensions.
- **Dữ liệu (entities):** ghi `DepositSmsSend`; đọc `DepositOrderDeliveryBatch` / `BatchSendSmsCustom` (cấu hình mẫu); cập nhật `DepositReceipt` (cờ đã gửi SMS); audit MongoDB.
- **Ripple / đồng bộ:** publish `SendSmsByIdPatternConsumerDto`(type=2) / `SendZaloByIdPatternConsumerDto`(type=4) → SMS-service. Consume `EventDepositUpdateStatusSMSSendMessage` (`UpdateStatusSmsSend`) → DaGui→ThanhCong/ThatBai.

## Luồng xuyên module
- **Đợt trả cọc:** SetSended đợt → InsertFromBatch (tạo bản ghi ChuaGui) → SendSmsByBatch (publish RabbitMQ) → SMS-service gửi gateway → callback EventDepositUpdateStatusSMSSendMessage chốt TT.
- **Nhắc thanh toán tự động:** Job `StaticVariableJobScanKhachHangChuaThanhToan` quét phiếu chưa thanh toán quá `SoPhutGuiTinNhanKhiChuaThanhToan` phút (nếu `CauHinh.IsGuiTinNhanKhiChuaThanhToan=true`) → tự sinh + gửi nhắc (set lock PaymentReminderSmsStatus=INPROCESSING).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Deposit/Deposit.API/Controllers/DepositSmsSendController.cs` — role `DV-AD-TONG, DV-QL-KHO-TONG`.
- BE `Deposit/Deposit.Service/Generated/DepositSmsSendService.cs`
  - **L186**: `if (dataVoucher == null && dataVoucher.Success != true ...)` — dereference `dataVoucher.Success` khi `dataVoucher==null` → **NRE chắc chắn nếu null** (rủi ro cao).
  - L926-930 / 975-979 / 1312-1316 / 1361-1365 / 1687-1691 / 1736-1740: nuốt exception trong vòng gửi (set TT nhưng không log) → khó debug gửi lỗi.
  - L140-144 / 1098-1100 / 1498-1503 / 1847-1850: nuốt exception parse JsonParam (4 chỗ).
  - L2113-2117: `_ = SendSmsByAction()` fire-and-forget trong finally (không await) → SMS sau-thao-tác có thể chưa chạy xong khi method thoát.
  - L2377-2387: lỗi sinh voucher (SendSMSToNotPayment) không return → gửi không có voucher.
  - License GemBox hardcode trong export.
- FE `scrm_frontend/src/app/pages/deposit-receipt/deposit-sms-send/` (view + manager) + `deposit-batch-custom/` (đợt tùy chọn) + `deposit-show-param/`, `deposit-note-code-sms/`; route `deposit-receipt/sms-manager`.
