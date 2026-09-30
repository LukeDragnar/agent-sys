---
controller: SearchMapController
module: he-thong
service: TrackMap
man_hinh_ui: "nội bộ — autocomplete địa chỉ/bản đồ (gợi ý khi nhập địa chỉ trong các form TrackMap)"
muc_dich: "Tìm gợi ý địa chỉ qua MapAsia (autocomplete) phục vụ nhập liệu địa chỉ/bản đồ."
alias: ["Tìm bản đồ", "MapAsia autocomplete", "gợi ý địa chỉ", "search map"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IMapAsiaService", "MapAsia API (external)"]
cross_module_in: ["Các form nhập địa chỉ TrackMap gọi autocomplete"]
cross_module_out: ["MapAsia external service"]
---

# SearchMapController — Tìm/gợi ý địa chỉ (MapAsia)

Autocomplete địa chỉ qua `IMapAsiaService`. `[SBAuthorize]` cấp class (không key cụ thể) → yêu cầu đăng nhập → `internal`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Autocomplete địa chỉ | gợi ý khi gõ | GET `api/SearchMap/TimMapAsiaAutoComplete?text=&isAddressLv2=` | — (chỉ `[SBAuthorize]` class) | internal | — | Gọi MapAsia trả danh sách gợi ý theo `text`; `isAddressLv2` lọc cấp địa chỉ. | External (MapAsia) | `Order=int.MaxValue` → route ưu tiên thấp nhất. |

## Quan hệ
- **Gọi sang:** `IMapAsiaService.TimMapAsiaAutoComplete` → MapAsia external API.
- **Dữ liệu (entities):** — (DTO Map từ Models.Core.DTO.TrackMap.Client.Map).
- **Ripple / đồng bộ:** —.

## Lỗi đã biết / lưu ý kỹ thuật
- `SearchMapController.cs:11` — chỉ `[SBAuthorize]` cấp class, không có permission key riêng → mọi user đăng nhập đều gọi được (không phân quyền chi tiết).
