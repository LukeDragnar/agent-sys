---
controller: NodeController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Cấu hình > Node (cây node phòng ban/nhóm; dùng làm nguồn cho field select source='node' và phân nhóm thành viên)"
muc_dich: "CRUD node phân cấp (Node — cây qua IdNodeCha); làm nguồn dữ liệu cho field select kiểu node + gắn thành viên (NodeMember)."
alias: [Node, "node phòng ban", "cây node", "nhóm node", org node]
auth: internal
state_machine: []
deprecated: false
entities: [Node]
stored_procs: []
depends: [INodeService]
cross_module_in: [NodeMemberController (gắn thành viên vào node), CongViecController (field select source=node)]
cross_module_out: []
---

# Node — Cây node (nhóm/phòng ban phân cấp)

> CRUD node cây (Ten + IdNodeCha). Filter keyword(Ten) + IdNodeCha. Dùng làm nguồn option cho field mở rộng `select` cấu hình `source='node'` và để gắn NodeMember.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm node (cây/lưới) | Cây node | `POST api/Node/TimTheoDieuKien` | `[SBAuthorize]` | internal | màn cấu hình | Filter keyword(Ten), IdNodeCha; sort NgayTao, phân trang. | EF | — |
| 2 | Chi tiết node | Mở form node | `POST api/Node/TimTheoId/{id}` | `[SBAuthorize]` | internal | form | Trả node. | EF | — |
| 3 | Lưu node | Nút "Lưu" | `POST api/Node/Luu` | `[SBAuthorize]` | internal | form | Tạo/sửa node (gắn cha qua IdNodeCha). | EF | — |
| 4 | Xóa node | Nút "Xóa" | `POST api/Node/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Xóa node. | EF | — |

## Quan hệ
- **Gọi sang:** `INodeService`.
- **Dữ liệu (entities):** `Node` (tự tham chiếu IdNodeCha).
- **Ripple/đồng bộ:** NodeMember gắn user vào node; field select(source=node) của CongViec tham chiếu nodeIds.

## Lỗi đã biết / lưu ý kỹ thuật
- Xóa node không kiểm tra node con / NodeMember / field đang tham chiếu → có thể để lại dangling reference.
