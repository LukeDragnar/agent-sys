---
controller: CauHinhViewController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc — view Gantt / Lịch (cấu hình view của user theo module; tự seed default view khi mở lần đầu)"
muc_dich: "CRUD cấu hình view của user (CauHinhView): các loại view hợp lệ gantt/lich; lấy-hoặc-tạo default view theo module (auto-seed); cache Redis danh sách view."
alias: [CauHinhView, "cấu hình view", "view gantt", "view lịch", "default view", saved view]
auth: internal
state_machine: []
deprecated: false
entities: [CauHinhView]
stored_procs: []
depends: [ICauHinhViewService, ICongViecService, IRedisCacheService]
cross_module_in: []
cross_module_out: []
---

# CauHinhView — Cấu hình view của user (Gantt / Lịch)

> CRUD view của user. **Whitelist LoaiView** = `gantt`, `lich` (chặn FE gửi loại lạ). `LayHoacTaoDefault` lấy default view của user trong module, **auto-seed nếu chưa có**. Danh sách view cache Redis (TTL 1440 phút), reset khi Luu/Xoa.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm view (list) | Danh sách view | `POST api/CauHinhView/TimTheoDieuKien` | `[SBAuthorize]` | internal | chọn view | Filter + phân trang view của user/module (cache Redis). | EF (cache) | — |
| 2 | Chi tiết view | Mở cấu hình view | `POST api/CauHinhView/TimTheoId/{id}` | `[SBAuthorize]` | internal | — | Trả view config. | EF | — |
| 3 | Lưu view | Nút "Lưu view" | `POST api/CauHinhView/Luu` | `[SBAuthorize]` | internal | — | Tạo/sửa view (LoaiView phải ∈ {gantt, lich}); reset cache. | EF + Redis | LoaiView ngoài whitelist → bị chặn. |
| 4 | Xóa view | Nút "Xóa view" | `POST api/CauHinhView/XoaTheoId/{id}` | `[SBAuthorize]` | internal | — | Xóa view; reset cache. | EF + Redis | — |
| 5 | Lấy/tạo default view | (auto khi mở module) | `POST api/CauHinhView/LayHoacTaoDefault` (body Module) | `[SBAuthorize]` | internal | mở module lần đầu | Trả default view; chưa có → **tự tạo** (seed) rồi trả. | EF | — |

## Quan hệ
- **Gọi sang:** `ICauHinhViewService`; `ICongViecService` (seed default theo cấu trúc CV); `IRedisCacheService`.
- **Dữ liệu (entities):** `CauHinhView` (per user + module).
- **Ripple/đồng bộ:** cache view list (Redis) invalidate khi Luu/Xoa.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhViewService.cs:28-31` — whitelist LoaiView cứng {gantt, lich}; thêm loại view mới phải sửa code service (không cấu hình động).
