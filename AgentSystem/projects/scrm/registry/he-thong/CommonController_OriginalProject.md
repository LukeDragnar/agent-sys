---
controller: CommonController
module: he-thong
service: OriginalProject
man_hinh_ui: "nội bộ — không có UI"
muc_dich: "Controller dùng chung của OriginalProject — hiện là vỏ rỗng (không action), chỉ inject ICommonService; giữ chỗ cho các API common tương lai."
alias: ["Common OriginalProject"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["ICommonService", "BaseController"]
cross_module_in: []
cross_module_out: []
---

# CommonController (OriginalProject) — API dùng chung (rỗng)

Controller placeholder. Route `api/Common`, kế thừa `BaseController`, inject `ICommonService` nhưng KHÔNG có action nào. Không có `[SBAuthorize]`. Hiện không có endpoint để gọi.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có action) | — | `api/Common` (không endpoint) | — | anonymous | — | Controller rỗng, không phục vụ request nào dù đã inject ICommonService. | — | — |

## Quan hệ
- **Gọi sang:** `ICommonService` (đã inject, chưa dùng).
- **Dữ liệu (entities):** —.
- **Ripple / đồng bộ:** —.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonController.cs:9-17` — inject `ICommonService` nhưng không có action nào dùng tới → dependency thừa/placeholder. Có thể xóa hoặc bổ sung endpoint khi cần.
