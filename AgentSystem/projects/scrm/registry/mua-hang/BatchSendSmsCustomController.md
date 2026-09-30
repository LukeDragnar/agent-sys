---
controller: BatchSendSmsCustomController
module: mua-hang
service: Deposit
man_hinh_ui: "Mua hàng/Đặt cọc > màn 'Đợt gửi tin nhắn tùy chọn' (/deposit-receipt/batch-send-sms-custom). Lưới đợt custom + nút Tạo/Sửa/Xóa (popup batch-custom-insert) + Khóa đợt (SetSended). Mỗi đợt = tệp phiếu cọc chọn tay để gửi SMS/Zalo."
muc_dich: "Quản lý 'đợt gửi tin nhắn tùy chọn' = tệp phiếu cọc gom thủ công (không qua đợt trả cọc) để gửi SMS/Zalo hàng loạt. CRUD đợt + gắn mẫu SMS/Zalo/voucher + khóa đợt khi chốt."
alias: ["đợt gửi tin nhắn tùy chọn", "dot gui tin nhan tuy chon", "đợt SMS custom", "batch send sms custom", "đợt gửi tin tùy chọn", "tệp gửi SMS tùy chọn"]
auth: public
state_machine: [IsSended=false(nháp/sửa-được), IsSended=true(đã-khóa/SetSended)]
deprecated: false
entities: [BatchSendSmsCustom, DepositSmsSend, HistoryModifyDepositOrderDeliveryBatch(MongoDB)]
stored_procs: []
depends: [MongoDB (audit)]
cross_module_in: []
cross_module_out: []
---

# BatchSendSmsCustomController — Đợt gửi tin nhắn tùy chọn (tệp phiếu cọc chọn tay → gửi SMS/Zalo)

> CRUD `BatchSendSmsCustom` — đợt gửi tin 1-lần gom phiếu cọc thủ công (qua DepositSmsSend.InsertBatchCustomFromPickList / FromAnotherBatch), KHÁC đợt-trả-cọc (gom tự động theo SP/ngày). 2 trạng thái `IsSended`: false (nháp — sửa/xóa được) / true (đã khóa qua SetSended — KHÓA mọi sửa). Mã (Code) tự sinh prefix "BAT" (vòng retry ≤20) nếu không truyền. Lưu mẫu SMS/Zalo/JsonParam/IdVoucher. Việc tạo bản ghi SMS & gửi thật nằm ở `DepositSmsSendController` (Insert*/SendSmsByBatchCustom). Audit MongoDB. Controller hằng `[SBAuthorize(key,..., "DV-AD-TONG, DV-QL-KHO-TONG")]` (riêng AutoComplete `[SBAuthorize]` trống).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo đợt | Nút "Tạo" (popup insert) | `POST api/BatchSendSmsCustom/Create` | `BatchSendSmsCustomController_Create` | public | màn đợt custom | Insert đợt; mã tự sinh prefix "BAT" (retry ≤20) nếu trống; `IsSended=false`. | **EF** | code-gen 20-retry mong manh nếu va chạm nhiều. |
| 2 | Sửa đợt | Nút "Sửa" | `POST api/BatchSendSmsCustom/Update` | `BatchSendSmsCustomController_Update` | public | chỉ khi IsSended=false | Sửa đợt; cấm nếu IsSended=true; log MongoDB. | **EF** + MongoDB | L210: audit gán `IdVoucher` từ `data` (mới) thay vì `dataOld` (cũ) → lịch sử không bắt được voucher cũ. |
| 3 | Tìm kiếm / lưới | Lưới đợt + lọc (keyword, đã khóa) | `POST api/BatchSendSmsCustom/SearchWithConditions` | `BatchSendSmsCustomController_SearchWithConditions` | public | màn đợt custom | Paging tìm theo keyword (NameDeliveryBatch/Code) + lọc IsSended. | **EF** | L149-152: log query-string chỉ cho superuser/CSKH → audit không đồng nhất. |
| 4 | Autocomplete | (combobox chọn đợt) | `POST api/BatchSendSmsCustom/BatchSendSmsCustomAutoComplete` | `[SBAuthorize]` (any login) | public | nơi cần chọn đợt | Trả list (Id/Code/NameDeliveryBatch) sort NgayTao DESC. | **EF** | `[SBAuthorize]` trống. |
| 5 | Lấy chi tiết | (mở popup) | `POST api/BatchSendSmsCustom/GetById/{id:Guid}` | `BatchSendSmsCustomController_GetById` | public | dòng đợt | Trả bản ghi; deserialize JsonParam → List<ParamMarketingCampaignDto>. | **EF** | — |
| 6 | Xóa đợt | Nút "Xóa" | `POST api/BatchSendSmsCustom/Delete/{id:Guid}` | `BatchSendSmsCustomController_Delete` | public | IsSended=false & chưa có SMS | Cấm nếu IsSended=true hoặc còn `DepositSmsSend` tham chiếu. | **EF** | — |
| 7 | Khóa đợt | Nút "Khóa" (SetSended) | `POST api/BatchSendSmsCustom/SetSended/{id:Guid}` | `BatchSendSmsCustomController_SetSended` | public | đợt đã cấu hình mẫu | Set `IsSended=true`; bắt buộc đã có ≥1 mẫu SMS hoặc Zalo. Sau khóa không sửa/xóa được. | **EF** | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Khóa cứng khi IsSended=true:** chặn Update / Delete.
- **Delete:** chỉ khi IsSended=false AND chưa có DepositSmsSend tham chiếu.
- **SetSended:** bắt buộc ≥1 mẫu SMS/Zalo.
- **Code:** tự sinh prefix "BAT" (retry ≤20) nếu không truyền.
- **Multi-tenant:** lọc theo đơn vị.

## Quan hệ
- **Gọi sang:** MongoDB (audit `HistoryModifyDepositOrderDeliveryBatch`).
- **Dữ liệu (entities):** `BatchSendSmsCustom`; tham chiếu `DepositSmsSend` (chặn xóa).
- **Ripple / đồng bộ:** KHÔNG publish RabbitMQ. Đợt là input cho DepositSmsSend.InsertBatchCustom*/SendSmsByBatchCustom (gửi thật).

## Luồng xuyên module
- Tạo đợt custom → gắn mẫu SMS/Zalo → DepositSmsSend.InsertBatchCustomFromPickList/FromAnotherBatch (đưa phiếu vào đợt) → SetSended (khóa) → SendSmsByBatchCustom (gửi + sinh voucher).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Deposit/Deposit.API/Controllers/BatchSendSmsCustomController.cs` — role `DV-AD-TONG, DV-QL-KHO-TONG`; AutoComplete `[SBAuthorize]` trống.
- BE `Deposit/Deposit.Service/Generated/BatchSendSmsCustomService.cs`
  - L210: audit Update gán `IdVoucher` từ bản mới thay vì bản cũ → lịch sử thay đổi voucher sai.
  - L149-152: chỉ log query-string cho superuser/CSKH → audit search không đồng nhất.
  - L255-268: code-gen prefix "BAT" với vòng retry 20 — mong manh khi va chạm cao.
  - Trùng lặp đáng kể với `ConfigSMSService` (xem node ConfigSMSController). Phân biệt: BatchSendSmsCustom = đợt 1-lần (IsSended), ConfigSMS = master tái dùng (IsSuDung).
- FE `scrm_frontend/src/app/pages/deposit-receipt/deposit-batch-custom/` (manager + insert); route `deposit-receipt/batch-send-sms-custom`.
