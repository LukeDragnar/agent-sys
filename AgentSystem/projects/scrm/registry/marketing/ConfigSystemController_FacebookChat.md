---
controller: ConfigSystemController
module: marketing
service: FacebookChat
man_hinh_ui: "nội bộ — không có UI (controller rỗng, chỉ inject service)"
muc_dich: "Khung controller cấu hình hệ thống của ChatHub. Hiện CHƯA có endpoint nào — chỉ inject IConfigSystemService. Là partial class scaffold, logic CRUD cấu hình hệ thống thực tế nằm ở service (chưa wire) hoặc ở Areas/SanboxArea."
alias: ["config system ChatHub", "cấu hình hệ thống chat", "ConfigSystem Facebook"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - "IConfigSystemService (FacebookChat.Services.Generated.ConfigSystemService : BaseService<ConfigSystem>)"
cross_module_in: []
cross_module_out: []
---

# ConfigSystemController (FacebookChat) — Cấu hình hệ thống (scaffold rỗng)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint) | — | — | — | internal | — | Controller chỉ có constructor inject `IConfigSystemService`, KHÔNG khai báo action nào. Không có `[Route]` action, không phục vụ request nghiệp vụ. | — | **EMPTY CONTROLLER** — `ConfigSystemController.cs:9-17`: partial class, chỉ ctor, 0 endpoint. |

## Quan hệ
- **Gọi sang:** (kỳ vọng) `IConfigSystemService` — service ĐÃ có sẵn CRUD cấu hình hệ thống: `GetValueByCode`, `TimTheoMaAsync`, `Insert`/`Update`/`Delete`, `GetLikeTree`/`GetLikeTreeSort`, `GetGroupCode`, `GetByGroupCode` (`ConfigSystemService.cs:13-24`). Nhưng controller chưa expose method nào.
- **Dữ liệu (entities):** `ConfigSystem` (Ma/Value/GroupCode...). `ConfigSystemService` dùng chung cache với `ICommonService`.
- **Ripple / đồng bộ:** không có (chưa có hành vi).

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:9-17` — controller RỖNG (partial class, chỉ ctor, không action). Chỉ tìm thấy 1 file partial duy nhất cho controller này; các bản `ConfigSystemController.cs` khác nằm ở `Areas/SanboxArea/` (sandbox riêng, ngoài batch).
- Service `IConfigSystemService` đã có đầy đủ CRUD + tra cứu (`ConfigSystemService.cs`), nhưng chưa được nối ra HTTP → muốn quản lý ConfigSystem qua API phải bổ sung action.
- So với `CommonController` (cùng service folder): việc đọc config Facebook hiện đi qua `CommonController.GetConfig*`, còn quản trị `ConfigSystem` (thêm/sửa/xóa) chưa có cửa API ở service FacebookChat này.
