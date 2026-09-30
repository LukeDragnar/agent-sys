---
controller: PatternSmsController
module: marketing
service: SMS
man_hinh_ui: "Marketing > Mẫu SMS/Zalo (danh sách mẫu tin, thêm/sửa/xóa, đổi trạng thái, lấy mẫu có sẵn, đồng bộ mẫu duyệt từ ESMS)."
muc_dich: "CRUD mẫu tin SMS/Zalo (PatternSms) bản v1: quản lý nội dung mẫu, trạng thái duyệt, lấy mẫu mặc định có sẵn và đồng bộ mẫu (template) SMS/Zalo đã duyệt từ ESMS."
alias: ["PatternSms", "mẫu sms", "mẫu zalo", "template sms", "mẫu tin nhắn", "đồng bộ mẫu esms"]
auth: public
state_machine: [trạng thái mẫu theo PatternSmsUpdateStatusDto]
deprecated: false
entities: [PatternSms]
stored_procs: []
depends:
  - "IPatternSmsService (SMS.Services.Generated)"
  - "ESMS — GetTemplateSmsFromEsms / GetTemplateZaloFromEsms (lấy mẫu đã duyệt)"
  - "MaTienIchSCRM.TI_SMS — tiện ích gate controller-level"
cross_module_in: []
cross_module_out:
  - "SmsSend `SendSmsByIdPatternSms` / `SendZaloByIdPatternSms` gửi tin theo idPattern → consumer đọc nội dung mẫu này"
---

# PatternSmsController — Quản lý mẫu SMS/Zalo (v1)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm mẫu | Tìm kiếm | POST `api/PatternSms/TimTheoDieuKien` | `PatternSms_TimTheoDieuKien` (HT-AD, DV-AD-TONG, HT-CSKH, HT-KT, DV-QL-MKT, DV-MKT-LEAD, DV-MKT, DV-AD-CN) | public | — | Lọc danh sách mẫu theo điều kiện. | EF | — |
| 2 | Chi tiết mẫu | Xem | GET `api/PatternSms/TimTheoId/{id}` | `PatternSms_TimTheoId` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Lấy 1 mẫu theo id. | EF | — |
| 3 | Thêm / Sửa mẫu | Thêm / Sửa | POST `api/PatternSms/ThemMoi` · `CapNhatThongTin` | `PatternSms_ThemMoi` · `PatternSms_CapNhatThongTin` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Tạo/cập nhật nội dung mẫu. | EF | — |
| 4 | Xóa mẫu | Xóa | POST `api/PatternSms/Xoa/{id}` | `PatternSms_Xoa` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Xóa mẫu theo id. | EF | — |
| 5 | Thêm mẫu có sẵn | Thêm mẫu mặc định | GET `api/PatternSms/ThemMoiMauCoSan` | `PatternSms_ThemMoiMauCoSanAsync` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Tạo loạt mẫu mặc định cho đơn vị. | EF | — |
| 6 | Đổi trạng thái mẫu | Đổi trạng thái | POST `api/PatternSms/CapNhatStatusAsync` | `PatternSms_CapNhatStatusAsync` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Cập nhật trạng thái mẫu (duyệt/khóa…). | EF | — |
| 7 | Lấy mẫu SMS/Zalo từ ESMS | Đồng bộ mẫu | GET `api/PatternSms/GetTemplateSmsFromEsms` · `GetTemplateZaloFromEsms` | `PatternSms_GetTemplateSmsFromEsms` · `_GetTemplateZaloFromEsms` (HT-AD, DV-AD-TONG, HT-CSKH) | public | — | Gọi ESMS lấy template đã duyệt để đồng bộ vào mẫu đơn vị. | HTTP (ESMS) | Nhãn key 2 endpoint đều ghi "Lấy mẫu zalo từ esms" (`cs:106,113`). |

## Quan hệ
- **Gọi sang:** `IPatternSmsService`; ESMS (lấy template).
- **Dữ liệu (entities):** `PatternSms` (mẫu tin + trạng thái).
- **Ripple / đồng bộ:** mẫu được tham chiếu khi gửi qua `SmsSend.SendSmsByIdPatternSms` / `SendZaloByIdPatternSms`.

## Lỗi đã biết / lưu ý kỹ thuật
- Mô tả quyền `GetTemplateSmsFromEsms` ghi nhầm "Lấy mẫu zalo từ esms" (`PatternSmsController.cs:106`).
- Đây là bản v1; bản nâng cao (đa tài khoản) ở `PatternSmsV2Controller`.
