---
controller: RolePermisionLogController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Phân quyền > Quản lý Quyền Custom: lưới log/danh sách quyền custom đã cấp riêng (override role mặc định) + nút Xóa"
muc_dich: "Tra cứu (danh sách) và xóa các bản ghi quyền custom — quyền cấp riêng cho user/role ngoài role mặc định (audit/override phân quyền)."
alias: ["RolePermisionLog", "quyền custom", "quản lý quyền custom", "log phân quyền", "override quyền"]
auth: mixed
state_machine: []
deprecated: false
entities: [RolePermisionLog]
stored_procs: []
depends: ["IRolePermisionLogService", "BaseController"]
cross_module_in: ["FE Phân quyền/Quyền Custom gọi TimTheoDieuKien (load lưới) + Xoa"]
cross_module_out: ["Xóa bản ghi quyền custom → thu hồi quyền override đã cấp cho user/role (ảnh hưởng phân quyền runtime)"]
---

# RolePermisionLogController — Quản lý Quyền Custom (override phân quyền)

Controller `partial` (route `api/QuanTriArea/RolePermisionLog`, Area SanboxArea) chỉ phơi 2 endpoint: liệt kê và xóa bản ghi quyền custom. Không có Thêm/Sửa qua UI (quyền custom được sinh ở luồng khác). Controller-level `[SBAuthorize]` rỗng + `[TienIch(..., TI_HeThong, true)]` → auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/QuanTriArea/RolePermisionLog/TimTheoDieuKien` | `RolePermisionLog_TimTheoDieuKien` (HT-AD, HT-CSKH) | mixed | — | Lọc danh sách quyền custom theo `RolePermisionLogFilterDto`. | EF | — |
| 2 | Xóa | Xóa | POST `api/QuanTriArea/RolePermisionLog/Xoa/{id:Guid}` | `RolePermisionLog_Xoa` (HT-AD) | mixed | — | Kiểm tra tồn tại (GetById) → `DeleteAsync` → MESS_Delete_Success. | EF | Xóa hard-delete, không log lý do thu hồi quyền |

## Quan hệ
- **Gọi sang:** IRolePermisionLogService (EF).
- **Dữ liệu (entities):** `RolePermisionLog` (quyền custom override role mặc định).
- **Ripple / đồng bộ:** xóa → thu hồi quyền custom đã cấp → ảnh hưởng phân quyền user runtime; không bắn event.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller `partial` — chỉ thấy 2 method trong file này; phần sinh/cấp quyền custom nằm ở luồng/partial khác (chưa lộ ở file `RolePermisionLogController.cs`).
- Không có endpoint ThemMoi/CapNhatThongTin → màn này chỉ đọc + xóa.
