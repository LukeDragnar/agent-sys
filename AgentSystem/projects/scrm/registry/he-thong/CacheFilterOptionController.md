---
controller: CacheFilterOptionController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI riêng; phục vụ cấu hình tùy chọn bộ lọc cache"
muc_dich: "CRUD tùy chọn bộ lọc (filter option) được cache lại để tái dùng trên các màn search/filter."
alias: ["CacheFilterOption", "cache filter", "tùy chọn bộ lọc cache"]
auth: public
state_machine: []
deprecated: false
entities: [CacheFilterOption]
stored_procs: []
depends: ["ICacheFilterOptionService"]
cross_module_in: ["FE màn quản lý tùy chọn bộ lọc gọi ThemMoi/GetAll/Update/UpdateActive/Xoa"]
cross_module_out: []
---

# CacheFilterOptionController — Quản lý tùy chọn bộ lọc cache

Controller CRUD đơn giản cho tùy chọn bộ lọc được cache. Mọi endpoint `[SBAuthorize()]` rỗng (chỉ cần login, không gắn permission key). Validate `ModelState` trước khi gọi service.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới | Thêm | POST `api/CacheFilterOption/ThemMoi` | — (login) | public | — | Validate model → `_service.Create(req, CurrentUser)` tạo 1 tùy chọn bộ lọc. | EF | Không gắn permission key |
| 2 | Lấy danh sách | (load) | POST `api/CacheFilterOption/GetAll` | — (login) | public | — | Trả danh sách theo `CacheFilterOptionSearchDto`. | EF | — |
| 3 | Bật/tắt active | toggle | POST `api/CacheFilterOption/UpdateActive` | — (login) | public | — | `_service.UpdateActive` đổi trạng thái active theo search dto. | EF | Nhận `SearchDto` (không phải DTO item) cho update active |
| 4 | Cập nhật | Sửa | POST `api/CacheFilterOption/Update` | — (login) | public | — | `_service.Update(req, CurrentUser)`. | EF | — |
| 5 | Xóa | Xóa | GET `api/CacheFilterOption/Xoa/{id}` | — (login) | public | — | `_service.Delete(id, CurrentUser)`. | EF | HTTP GET cho hành động xóa |

## Quan hệ
- **Gọi sang:** `ICacheFilterOptionService`.
- **Dữ liệu (entities):** `CacheFilterOption`.
- **Ripple / đồng bộ:** không có cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `CacheFilterOptionController.cs:18-71` — toàn bộ endpoint dùng `[SBAuthorize()]` rỗng (không permission key) → chỉ chặn ở mức login, không phân quyền chi tiết.
- `CacheFilterOptionController.cs:63` `Xoa` dùng HTTP GET cho xóa (không idempotent-safe).
