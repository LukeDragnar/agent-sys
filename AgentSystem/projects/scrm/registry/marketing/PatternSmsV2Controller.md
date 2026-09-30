---
controller: PatternSmsV2Controller
module: marketing
service: SMS
man_hinh_ui: "Marketing > Mẫu SMS/Zalo (nâng cao) — dùng khi đơn vị bật cấu hình nâng cao: mẫu gắn theo tài khoản SMS (idAccountSms), CRUD + đồng bộ mẫu từ ESMS theo đối tác."
muc_dich: "CRUD mẫu tin SMS/Zalo bản V2 (nâng cao): mẫu gắn với từng tài khoản SMS (idAccountSms), hỗ trợ lấy template từ ESMS theo request đối tác. Song hành với PatternSmsController v1."
alias: ["PatternSmsV2", "mẫu nâng cao", "mẫu theo tài khoản sms", "template sms v2", "đồng bộ mẫu esms v2"]
auth: public
state_machine: [trạng thái mẫu theo PatternSmsUpdateStatusDto]
deprecated: false
entities: [PatternSms, AccountSms]
stored_procs: []
depends:
  - "IPatternSmsV2Service (SMS.Services.Generated)"
  - "ESMS — GetTemplateSmsFromEsmsV2 / GetTemplateZaloFromEsmsV2 (theo ReqGetTemplateMessageFromPartner)"
  - "MaTienIchSCRM.TI_SMS — tiện ích gate controller-level"
cross_module_in: []
cross_module_out:
  - "SmsSend V2 (SendMessageV2) dùng mẫu V2 khi đơn vị bật cấu hình nâng cao"
---

# PatternSmsV2Controller — Quản lý mẫu SMS/Zalo (V2 nâng cao)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách mẫu | Tìm kiếm | POST `api/PatternSmsV2/PatternSmsV2_DanhSach` | `PatternSms_PatternSmsV2_DanhSach` (8 role MKT/AD/KT) | public | — | Lọc mẫu V2 theo điều kiện. | EF | — |
| 2 | Mẫu theo tài khoản SMS | (chọn theo account) | POST `api/PatternSmsV2/LayDanhSachPatternSmsByIdAccountSms` | `PatternSms_LayDanhSachPatternSmsByIdAccountSms` (8 role) | public | — | Lấy mẫu gắn với 1 `idAccountSms`. | EF | — |
| 3 | Chi tiết mẫu | Xem | GET `api/PatternSmsV2/TimTheoIdV2/{id}` | `PatternSms_TimTheoIdV2` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lấy 1 mẫu V2 theo id. | EF | — |
| 4 | Thêm / Sửa mẫu | Thêm / Sửa | POST `api/PatternSmsV2/ThemMoiV2` · `CapNhatThongTinV2` | `PatternSms_ThemMoiV2` · `PatternSms_CapNhatThongTin` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Tạo/cập nhật mẫu V2. | EF | Key sửa V2 dùng lại key v1 `PatternSms_CapNhatThongTin` (`cs:72`). |
| 5 | Đổi trạng thái | Đổi trạng thái | POST `api/PatternSmsV2/CapNhatStatusV2Async` | `PatternSms_CapNhatStatusV2Async` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Cập nhật trạng thái mẫu V2. | EF | — |
| 6 | Xóa mẫu | Xóa | POST `api/PatternSmsV2/XoaV2/{id}` | `PatternSms_XoaV2` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Xóa mẫu V2. | EF | — |
| 7 | Thêm mẫu có sẵn | Thêm mẫu mặc định | POST `api/PatternSmsV2/ThemMoiMauCoSanV2` | `PatternSms_ThemMoiMauCoSanV2` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Tạo mẫu mặc định bản V2. | EF | — |
| 8 | Lấy mẫu từ ESMS | Đồng bộ mẫu | POST `api/PatternSmsV2/GetTemplateSmsFromEsmsV2` · `GetTemplateZaloFromEsmsV2` | `PatternSms_GetTemplateSmsFromEsmsV2` · `_GetTemplateZaloFromEsmsV2` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Gọi ESMS lấy template theo `ReqGetTemplateMessageFromPartner` (đối tác). | HTTP (ESMS) | Nhãn key 2 endpoint đều ghi "Lấy mẫu zalo từ esms" (`cs:115,121`). |

## Quan hệ
- **Gọi sang:** `IPatternSmsV2Service`; ESMS.
- **Dữ liệu (entities):** `PatternSms` (V2, gắn `idAccountSms`), `AccountSms`.
- **Ripple / đồng bộ:** mẫu V2 dùng cho luồng gửi nâng cao (`SmsSend.SaveSendMessageV2`).

## Lỗi đã biết / lưu ý kỹ thuật
- Sửa mẫu V2 (`CapNhatThongTinV2`) dùng lại permission key v1 `PatternSms_CapNhatThongTin` (`PatternSmsV2Controller.cs:72`) → khó phân quyền tách riêng v1/v2.
- Khác v1: mẫu V2 gắn theo `idAccountSms`, lấy template ESMS qua POST body đối tác thay vì GET.
