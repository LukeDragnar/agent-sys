---
controller: NodeMemberController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Cấu hình > Node — khu 'Thành viên' của 1 node (gán user vào node)"
muc_dich: "CRUD thành viên node (NodeMember): gán/bỏ user khỏi node để phục vụ phân nhóm + nguồn field select theo node."
alias: [NodeMember, "thành viên node", "member node", "gán user vào node"]
auth: internal
state_machine: []
deprecated: false
entities: [NodeMember]
stored_procs: []
depends: [INodeMemberService]
cross_module_in: [NodeController (node cha của thành viên)]
cross_module_out: []
---

# NodeMember — Thành viên node (gán user vào node)

> CRUD đơn giản gán user vào node.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm thành viên (lưới) | Lưới thành viên node | `POST api/NodeMember/TimTheoDieuKien` | `[SBAuthorize]` | internal | khu thành viên | Filter + phân trang theo node. | EF | — |
| 2 | Chi tiết thành viên | (xem) | `POST api/NodeMember/TimTheoId/{id}` | `[SBAuthorize]` | internal | — | Trả bản ghi. | EF | — |
| 3 | Lưu thành viên | Nút "Thêm thành viên" | `POST api/NodeMember/Luu` | `[SBAuthorize]` | internal | — | Gán/cập nhật user vào node. | EF | — |
| 4 | Xóa thành viên | Nút "Bỏ thành viên" | `POST api/NodeMember/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Gỡ user khỏi node. | EF | — |

## Quan hệ
- **Gọi sang:** `INodeMemberService`.
- **Dữ liệu (entities):** `NodeMember` (FK Node + User).
- **Ripple/đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- (không phát hiện lỗi đáng chú ý.)
