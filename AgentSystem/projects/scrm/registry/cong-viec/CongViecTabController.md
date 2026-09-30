---
controller: CongViecTabController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc — thanh tab phía trên lưới (mỗi tab = 1 bộ filter + cấu hình view đã lưu của user)"
muc_dich: "CRUD tab công việc (CongViecTab) — bộ filter + view config tái sử dụng; lưu cấu hình view riêng cho từng tab."
alias: [CongViecTab, "tab công việc", "tab filter", "view công việc", saved view]
auth: internal
state_machine: []
deprecated: false
entities: [CongViecTab]
stored_procs: []
depends: [ICongViecTabService]
cross_module_in: [CongViecController (TimTheoDieuKien áp TabFilter của tab)]
cross_module_out: []
---

# CongViecTab — Tab công việc (filter + view config)

> CRUD tab. Mỗi tab giữ điều kiện filter (TabFilter) + cấu hình view; CongViec.TimTheoDieuKien nhận TabFilter từ tab đang chọn.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm tab (lưới/list) | Danh sách tab | `POST api/CongViecTab/TimTheoDieuKien` | `[SBAuthorize]` | internal | thanh tab | Filter + phân trang tab. | EF | — |
| 2 | Chi tiết tab | Mở cấu hình tab | `POST api/CongViecTab/TimTheoId/{id}` | `[SBAuthorize]` | internal | — | Trả tab + filter + view config. | EF | — |
| 3 | Lưu tab | Nút "Lưu tab" | `POST api/CongViecTab/Luu` | `[SBAuthorize]` | internal | — | Tạo/sửa tab. | EF | — |
| 4 | Xóa tab | Nút "Xóa tab" | `POST api/CongViecTab/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Xóa tab. | EF | — |
| 5 | Lưu view config | (auto-save layout cột/sort) | `POST api/CongViecTab/LuuViewConfig` | `[SBAuthorize]` | internal | — | Lưu cấu hình view (cột/sort/group) cho tab. | EF | — |

## Quan hệ
- **Gọi sang:** `ICongViecTabService`.
- **Dữ liệu (entities):** `CongViecTab`.
- **Ripple/đồng bộ:** CongViec.TimTheoDieuKien tiêu thụ TabFilter của tab.

## Lỗi đã biết / lưu ý kỹ thuật
- (không phát hiện lỗi đáng chú ý — CRUD đơn giản.)
