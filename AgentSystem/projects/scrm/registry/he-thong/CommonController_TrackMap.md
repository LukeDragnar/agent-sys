---
controller: CommonController
module: he-thong
service: TrackMap
man_hinh_ui: "nội bộ — không có UI"
muc_dich: "Controller dùng chung của TrackMap — hiện là vỏ rỗng (không có action), kế thừa BaseController; chỉ giữ chỗ cho các API common tương lai."
alias: ["Common TrackMap", "Quản lý chung"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["BaseController"]
cross_module_in: []
cross_module_out: []
---

# CommonController (TrackMap) — API dùng chung (rỗng)

Controller placeholder. Route `api/Common`, kế thừa `BaseController`, KHÔNG có action nào. Attribute `[SBAuthorize("Quản lý chung")]` ở cấp class đã bị **comment** → không áp quyền. Hiện không có endpoint nào để gọi.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có action) | — | `api/Common` (không endpoint) | — | anonymous | — | Controller rỗng, không phục vụ request nào. | — | — |

## Quan hệ
- **Gọi sang:** — (không có service inject).
- **Dữ liệu (entities):** —.
- **Ripple / đồng bộ:** —.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonController.cs:9` — `[SBAuthorize("Quản lý chung")]` bị comment; controller rỗng (placeholder). Có thể xóa hoặc bổ sung action khi cần.
