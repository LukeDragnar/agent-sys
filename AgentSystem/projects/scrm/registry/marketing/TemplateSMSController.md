---
controller: TemplateSMSController
module: marketing
service: SMS
man_hinh_ui: "Marketing > SMS > Template SMS (bảng template tin nhắn dùng chung). Có thể chỉ dùng nội bộ/danh mục."
muc_dich: "Tra cứu danh sách Template SMS (TemplateSMS) theo điều kiện lọc — danh mục template tin nhắn."
alias: ["TemplateSMS", "template sms", "mẫu template sms"]
auth: anonymous
state_machine: []
deprecated: false
entities: [TemplateSMS]
stored_procs: []
depends:
  - "ITemplateSMSService (SMS.Services.Generated)"
cross_module_in: []
cross_module_out: []
---

# TemplateSMSController — Danh mục Template SMS

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách template | Tìm kiếm | POST `api/TemplateSMS/TimTheoDieuKien` | (không) | anonymous | — | Lọc danh sách `TemplateSMS` theo filter. | EF | KHÔNG có `[SBAuthorize]` (`cs:26`). |

## Quan hệ
- **Gọi sang:** `ITemplateSMSService`.
- **Dữ liệu (entities):** `TemplateSMS` (template tin nhắn).

## Lỗi đã biết / lưu ý kỹ thuật
- Endpoint không có auth (`TemplateSMSController.cs:26-31`).
- Controller scaffold tối thiểu (chỉ 1 list endpoint); dễ nhầm với `PatternSms` (mẫu tin nghiệp vụ) — đây là danh mục template thô.
