---
controller: LeadController
module: he-thong
service: AuthenPartner
man_hinh_ui: "Nội bộ — không có UI. Webhook/API nhận kết quả tác nghiệp lead từ đối tác (Tingtong/Sandbox) đẩy ngược về SCRM."
muc_dich: "Nhận callback từ đối tác (Tingtong) cập nhật kết quả tác nghiệp của 1 lead/contact theo RefId; chuyển sang Contact service và ghi log toàn bộ request/response vào Mongo (PartnerConnect)."
alias: ["update lead result", "callback tingtong", "cập nhật kết quả lead đối tác", "api lead partner", "webhook lead sandbox"]
auth: internal
state_machine: []
entities: [Contact, LogTingTong]
stored_procs: []
deprecated: false
depends: ["SBAuthorizePartner (JWT partner + CauHinh AccessToken/DeviceId + IsSuDungApiGetData)", "Contact service (Contact/UpdateKetQuaTacNghiepContactPartner)", "MongoDB PartnerConnect (collection LogTingTong) — log mọi call"]
cross_module_in: ["Đối tác Tingtong/Sandbox → POST update-lead-result (callback kết quả tác nghiệp)"]
cross_module_out: ["Contact service (cập nhật kết quả tác nghiệp contact)", "MongoDB PartnerConnect (ghi LogTingTong)"]
---

# LeadController — Nhận kết quả tác nghiệp lead từ đối tác (AuthenPartner)

Controller AuthenPartner nhận **callback từ đối tác Tingtong/Sandbox** để cập nhật kết quả tác nghiệp của một lead/contact (theo `RefId`). Kế thừa `BasePartnerController` + `[SBAuthorizePartner]` (mô hình auth đối tác xem node `CommonController_AuthenPartner`). `DonViId` lấy từ token.

Khác các controller chỉ-đọc khác, đây là endpoint **ghi dữ liệu** (cập nhật contact). Mọi request đều được **log đầy đủ vào MongoDB PartnerConnect** (`LogTingTong`: IdDonVi, LinkApi, Method, RequestBody, ResponseBody/ErrorMessage, type `TingTongToSandbox`) — kể cả khi lỗi (log trong cả nhánh success/fail/exception). Logic thật ở `LeadService.UpdateLeadResult`: validate `RefId` là Guid, `Status`/`Result` không rỗng → gọi Contact `UpdateKetQuaTacNghiepContactPartner` (service này kiểm tra RefId tồn tại + cập nhật).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật kết quả lead | (API callback) | POST `api/Lead/update-lead-result` | — | internal `[SBAuthorizePartner]` | — | Validate `RefId` là Guid + `Status`/`Result` không rỗng → gọi Contact `UpdateKetQuaTacNghiepContactPartner` (kèm `IdDonVi` từ token) → ghi `LogTingTong` (request + response/error) vào Mongo PartnerConnect → trả kết quả. | HTTP Contact + Mongo PartnerConnect | RefId không hợp lệ/rỗng → "RefId không hợp lệ"; Status/Result rỗng → "không được để trống" |

## Quy tắc nghiệp vụ (gating)
1. Auth `SBAuthorizePartner` (JWT + CauHinh AccessToken/DeviceId + `IsSuDungApiGetData`).
2. `RefId` null hoặc không parse được Guid → "RefId không hợp lệ. Vui lòng kiểm tra lại."
3. `Status` rỗng HOẶC `Result` rỗng → "không được để trống."
4. Gọi Contact `UpdateKetQuaTacNghiepContactPartner` (Contact tự kiểm tra RefId tồn tại); Contact trả Success=false → trả nguyên `Message` của Contact.
5. Mọi nhánh (success/fail/exception) đều insert 1 bản ghi `LogTingTong` vào Mongo trước khi return.

## Luồng xuyên module (callback lead)
1. Tingtong/Sandbox POST `update-lead-result` (Bearer partner token) → AuthenPartner validate input.
2. Gọi Contact service cập nhật kết quả tác nghiệp contact theo `RefId` + `IdDonVi`.
3. Ghi log `LogTingTong` (type `TingTongToSandbox`) vào MongoDB PartnerConnect.

## Quan hệ
- **Gọi sang:** Contact service (`Contact/UpdateKetQuaTacNghiepContactPartner`).
- **Dữ liệu (entities):** `Contact` (ghi kết quả tác nghiệp qua Contact service), `LogTingTong` (Mongo PartnerConnect — audit).
- **Ripple / đồng bộ:** cập nhật kết quả tác nghiệp contact bên Contact service; lưu vết toàn bộ call partner.

## Lỗi đã biết / lưu ý kỹ thuật
- `LeadController.cs:54-58` — khi `result.Success == false`, set `logTingTong.ErrorMessage = result.Message` rồi vẫn `ResponseResult(result)`; ổn, nhưng nếu `result == null` (đã set ErrorMessage "Xảy ra lỗi hệ thống") thì `ResponseResult(null)` truyền null vào `IMethodResult` → có thể NRE ở `BasePartnerController.ResponseResult` (`!result.Success`).
- `LeadService.cs:56-58` — kiểm tra `dataContact == null` 2 lần (dòng 51 và 56); ở nhánh 56 đã chắc chắn không null nhưng vẫn truy cập `dataContact.Message` sau khi check `==null` (dead branch, không lỗi runtime).
- Endpoint ghi dữ liệu nhưng **không throttle** (khác các controller đọc) → đối tác có thể gọi liên tục; đã có log đầy đủ để audit.
