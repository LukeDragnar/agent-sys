---
controller: DaPhuongTienController
module: he-thong
service: Core
man_hinh_ui: "Quản trị > danh mục đa phương tiện (tiện ích TI_Chung) — màn quản lý/đính kèm media; best-effort, cần xác nhận"
muc_dich: "Quản lý 'đa phương tiện' (media/file đính kèm theo nhóm): lưu, xóa, tìm theo điều kiện, tìm theo Id, lấy danh sách nhóm."
alias: ["Đa phương tiện", "DaPhuongTien", "media", "đính kèm đa phương tiện"]
auth: mixed
state_machine: []
deprecated: false
entities: [DaPhuongTien]
stored_procs: []
depends: ["IDaPhuongTienService"]
cross_module_in: ["[UI] màn quản lý đa phương tiện gọi Luu/Xoa/TimTheoDieuKien/TimTheoId/LayNhom"]
cross_module_out: []
---

# DaPhuongTienController — Quản lý đa phương tiện (media theo nhóm)

Controller kế thừa `BaseController` (không phải SBBaseController) — tự khai báo đủ action, không có CRUD chuẩn kế thừa. Khai báo `[TienIch("DaPhuongTienController", "Quản lý đơn vị", MaTienIchSCRM.TI_Chung)]`, route `api/DaPhuongTien`. Toàn bộ logic ủy quyền cho `IDaPhuongTienService`. Auth **mixed**: `Luu` và `Xoa` yêu cầu `[SBAuthorize(key)]`; còn `TimTheoDieuKien`, `TimTheoId`, `LayNhom` **không có `[SBAuthorize]`** (chỉ cần token đăng nhập, không gắn permission key).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu đa phương tiện | Lưu | POST `api/DaPhuongTien/Luu` | `HT_Luu_DaPhuongTien` | public | — | Nhận `DaPhuongTienView`, gọi `service.Luu`, trả `ResponseResult`. | EF qua service | — |
| 2 | Xóa đa phương tiện | Xóa | GET `api/DaPhuongTien/Xoa/{id:Guid}` | `HT_Xoa_DaPhuongTien` | public | — | Xóa theo `id`, trả `ResponseResult`. **Dùng HTTP GET để xóa** (lệch convention, các controller khác Xóa thường POST). | EF qua service | Xóa bằng GET — không idempotent-safe, dễ bị prefetch/crawler trigger |
| 3 | Tìm theo điều kiện | (lưới/tìm kiếm) | POST `api/DaPhuongTien/TimTheoDieuKien` | — | public (không `[SBAuthorize]`) | — | Nhận `ReqTimKiemTheoDieuKien`, gọi `service.TimTheoDieuKien`, trả danh sách. | EF qua service | Không gắn permission key |
| 4 | Tìm theo Id | (chi tiết) | GET `api/DaPhuongTien/TimTheoId/{id:Guid}` | — | public (không `[SBAuthorize]`) | — | Lấy 1 bản ghi theo `id`. | EF qua service | Không gắn permission key |
| 5 | Lấy nhóm | (dropdown nhóm) | GET `api/DaPhuongTien/LayNhom` | — | public (không `[SBAuthorize]`) | — | Trả tất cả nhóm có trong bảng DaPhuongTien (dựng dropdown lọc theo nhóm). | EF qua service | Không gắn permission key |

## Quan hệ
- **Gọi sang:** `IDaPhuongTienService` (toàn bộ nghiệp vụ).
- **Dữ liệu (entities):** `DaPhuongTien` ↔ `DaPhuongTienView` / `ReqTimKiemTheoDieuKien`.
- **Ripple / đồng bộ:** không thấy ripple cross-service ở tầng controller.

## Lỗi đã biết / lưu ý kỹ thuật
- `DaPhuongTienController.cs:46` — `Xoa` dùng `[HttpGet]` để xóa dữ liệu → lệch convention (xóa nên POST), rủi ro side-effect qua GET.
- `DaPhuongTienController.cs:58-83` — `TimTheoDieuKien`, `TimTheoId`, `LayNhom` thiếu `[SBAuthorize]` → chỉ chặn ở mức token, không phân quyền chi tiết.
- `DaPhuongTienController.cs:33` — còn dòng `//[AllowAnonymous]` đã comment trên `Luu` (di tích, không ảnh hưởng).
- `DaPhuongTienController.cs:12` — `[TienIch]` mô tả "Quản lý đơn vị" trong khi controller là đa phương tiện → label tiện ích có thể gây nhầm khi tra cứu phân quyền.
