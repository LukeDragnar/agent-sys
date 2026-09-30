---
controller: ConfigSMSController
module: mua-hang
service: Deposit
man_hinh_ui: "Mua hàng/Đặt cọc > màn 'Cấu hình gửi tin nhắn tự động' (/deposit-receipt/config-sms). Lưới cấu hình + nút Tạo/Sửa/Xóa (popup config-sms-edit). Mỗi cấu hình = 1 mẫu SMS/Zalo + tham số dùng cho gửi tin tự động."
muc_dich: "CRUD cấu hình mẫu gửi tin nhắn tự động (template SMS/Zalo + JsonParam) cho đặt cọc. Là 'master config' tái sử dụng (khác đợt-trả-cọc 1-lần): mẫu, voucher, tham số marketing."
alias: ["cấu hình gửi tin nhắn tự động", "cau hinh gui tin nhan tu dong", "config sms", "cấu hình SMS tự động", "mẫu SMS tự động", "auto sms config"]
auth: public
state_machine: [IsSuDung=true(đang-dùng), IsSuDung=false(ngừng)]
deprecated: false
entities: [ConfigSMS, DepositSmsSend, HistoryModifyDepositOrderDeliveryBatch(MongoDB)]
stored_procs: []
depends: [MongoDB (audit), CommonCacheService/ClearCache_ConfigSMS]
cross_module_in: []
cross_module_out: []
---

# ConfigSMSController — Cấu hình gửi tin nhắn tự động (master template SMS/Zalo cho đặt cọc)

> CRUD cấu hình `ConfigSMS` — mẫu gửi tin tự động tái sử dụng (khác `BatchSendSmsCustom` là đợt 1-lần có khóa IsSended). Cờ trạng thái là `IsSuDung` (đang dùng / ngừng), KHÔNG có lock kiểu SetSended. Lưu `IdSmsPattern`/`IdZaloPattern`/`JsonParam`/`IsUseJsonParam`/`IdVoucher`. Mã (Code) unique theo đơn vị, set khi Create, không sửa khi Update. Mọi thay đổi audit MongoDB + clear cache. **Lưu ý chia sẻ DTO**: `SearchWithConditions` dùng `BatchSendSmsCustomSearchWithConditionPayload` (dùng chung payload với BatchSendSmsCustom). Controller hằng `[SBAuthorize(key,..., "DV-AD-TONG, DV-QL-KHO-TONG")]` (riêng AutoComplete = `[SBAuthorize]` trống).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo cấu hình | Nút "Tạo" (popup edit) | `POST api/ConfigSMS/Create` | `ConfigSMSController_Create` | public | màn config SMS | Insert ConfigSMS; validate Code unique theo đơn vị + phải có mẫu SMS hoặc Zalo. Clear cache ConfigSMS. | **EF** | nhãn nội bộ "Tạo đợt gửi tin nhắn" (sao chép từ batch — gây nhầm; đây là cấu hình, không phải đợt). |
| 2 | Sửa cấu hình | Nút "Sửa" | `POST api/ConfigSMS/Update` | `ConfigSMSController_Update` | public | dòng cấu hình | Sửa cấu hình (Code không đổi); log MongoDB; clear cache. | **EF** + MongoDB | — |
| 3 | Tìm kiếm / lưới | Lưới cấu hình + lọc keyword | `POST api/ConfigSMS/SearchWithConditions` | `ConfigSMSController_SearchWithConditions` | public | màn config SMS | Paging tìm theo keyword (Name/Code). **Payload dùng chung `BatchSendSmsCustomSearchWithConditionPayload`**. | **EF** | — |
| 4 | Autocomplete | (combobox chọn cấu hình) | `POST api/ConfigSMS/ConfigSMSAutoComplete` | `[SBAuthorize]` (any login) | public | nơi cần chọn cấu hình | Trả list (Id/Code/Name) sort NgayTao DESC. | **EF** | `[SBAuthorize]` trống — chỉ cần đăng nhập. |
| 5 | Lấy chi tiết | (mở popup edit) | `POST api/ConfigSMS/GetById/{id:Guid}` | `ConfigSMSController_GetById` | public | dòng cấu hình | Trả bản ghi; deserialize JsonParam → List<ParamMarketingCampaignDto>. | **EF** | — |
| 6 | Xóa cấu hình | Nút "Xóa" | `POST api/ConfigSMS/Delete/{id:Guid}` | `ConfigSMSController_Delete` | public | cấu hình chưa dùng gửi | Cấm nếu còn bản ghi `DepositSmsSend` tham chiếu. | **EF** | L81-82: check `IdDepositOrderDeliveryBatch == data.Id` (so Id cấu hình với khóa đợt) → quan hệ tham chiếu nhập nhằng, dễ check sai. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Bắt buộc ≥1 mẫu** SMS hoặc Zalo.
- **Code** unique theo đơn vị; required khi Create, không sửa khi Update.
- **IsSuDung** không null.
- **Xóa** chỉ khi không còn DepositSmsSend tham chiếu.
- **Multi-tenant:** lọc theo đơn vị.

## Quan hệ
- **Gọi sang:** MongoDB (audit `HistoryModifyDepositOrderDeliveryBatch`), CommonCacheService.ClearCache_ConfigSMS.
- **Dữ liệu (entities):** `ConfigSMS`; tham chiếu `DepositSmsSend` (chặn xóa).
- **Ripple / đồng bộ:** KHÔNG publish RabbitMQ. Cấu hình được dùng làm mẫu khi sinh/gửi SMS tự động.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Deposit/Deposit.API/Controllers/ConfigSMSController.cs` — role `DV-AD-TONG, DV-QL-KHO-TONG`; AutoComplete `[SBAuthorize]` trống.
- BE `Deposit/Deposit.Service/Generated/ConfigSMSService.cs`
  - L81-82: Delete check `IdDepositOrderDeliveryBatch == data.Id` — so Id cấu hình với khóa đợt-trả-cọc → quan hệ tham chiếu nhập nhằng/khả nghi.
  - Trùng lặp logic với `BatchSendSmsCustomService` (cùng lưu mẫu SMS/Zalo/JsonParam/IdVoucher, cùng AutoComplete/Search, cùng audit MongoDB) → cơ hội gộp; phân biệt: ConfigSMS = master tái dùng (IsSuDung), BatchSendSmsCustom = đợt 1-lần (IsSended).
  - Audit ghi vào collection `HistoryModifyDepositOrderDeliveryBatch` (tên collection dùng chung, không có collection riêng cho ConfigSMS).
- FE `scrm_frontend/src/app/pages/deposit-receipt/deposit-config-sms/` (manager + edit); route `deposit-receipt/config-sms`.
