---
controller: CommonController
module: marketing
service: SMS
man_hinh_ui: "nội bộ — cung cấp dropdown cho các màn mẫu SMS (loại pattern, loại mẫu, trạng thái mẫu). Không phải màn riêng."
muc_dich: "Cung cấp dữ liệu dropdown dùng chung cho UI SMS: loại pattern SMS, kiểu mẫu (sms/zalo…), trạng thái mẫu."
alias: ["Common SMS", "dropdown pattern sms", "dropdown loại mẫu", "dropdown trạng thái mẫu"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "ICommonService (SMS.Service.Generated.Common)"
cross_module_in: []
cross_module_out: []
---

# CommonController — Dropdown dùng chung (SMS)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Dropdown loại pattern | (combobox) | GET `api/Common/DropdownPatternSmsType` | (không) | anonymous | — | Trả danh sách loại pattern SMS. | EF/enum | — |
| 2 | Dropdown kiểu mẫu | (combobox) | GET `api/Common/DropdownPatternSmsPatternType` | (không) | anonymous | — | Trả danh sách kiểu mẫu (sms/zalo…). | EF/enum | — |
| 3 | Dropdown trạng thái mẫu | (combobox) | GET `api/Common/DropdownPatternSmsStatus` | (không) | anonymous | — | Trả danh sách trạng thái mẫu. | EF/enum | — |

## Quan hệ
- **Gọi sang:** `ICommonService`.
- **Dữ liệu (entities):** danh mục/enum dropdown cho mẫu SMS (PatternSms).

## Lỗi đã biết / lưu ý kỹ thuật
- `[SBAuthorize()]` bị comment (`CommonController.cs:20`) → các dropdown mở (anonymous); chấp nhận được vì chỉ trả enum tĩnh.
