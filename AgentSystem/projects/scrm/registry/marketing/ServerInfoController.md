---
controller: ServerInfoController
module: marketing
service: SMS
man_hinh_ui: "Quản trị/nội bộ — danh mục thông tin server gửi tin (ServerInfo). Thường không có UI người dùng cuối."
muc_dich: "Tra cứu danh sách ServerInfo (thông tin server/gateway gửi tin) theo điều kiện lọc."
alias: ["ServerInfo", "thông tin server sms", "server gửi tin"]
auth: anonymous
state_machine: []
deprecated: false
entities: [ServerInfo]
stored_procs: []
depends:
  - "IServerInfoService (SMS.Services.Generated)"
cross_module_in: []
cross_module_out: []
---

# ServerInfoController — Thông tin server gửi tin

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách server | Tìm kiếm | POST `api/ServerInfo/TimTheoDieuKien` | (không) | anonymous | — | Lọc danh sách `ServerInfo` theo filter. | EF | KHÔNG có `[SBAuthorize]` (`cs:26`). |

## Quan hệ
- **Gọi sang:** `IServerInfoService`.
- **Dữ liệu (entities):** `ServerInfo` (cấu hình server/gateway gửi tin — liên quan IdServer trong callback ESMS).

## Lỗi đã biết / lưu ý kỹ thuật
- Endpoint không có auth (`ServerInfoController.cs:26-31`).
- Controller scaffold tối thiểu (1 list endpoint).
