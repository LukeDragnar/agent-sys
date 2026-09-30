---
controller: ChienDichController
module: nhan-su
service: HumanResource
man_hinh_ui: "Tuyển dụng > Chiến dịch tuyển dụng (danh sách + thêm/sửa/xóa)"
muc_dich: "CRUD chiến dịch tuyển dụng nhân sự (đợt/campaign tuyển dụng gom ứng viên)."
alias: [ChienDich, "chiến dịch tuyển dụng", "đợt tuyển dụng", "campaign tuyển dụng"]
auth: mixed
state_machine: []
deprecated: false
entities: [ChienDich, UngVien, User]
stored_procs: []
depends: [User (Core)]
cross_module_in: []
cross_module_out: []
---

# ChienDich — Quản lý chiến dịch tuyển dụng

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | Danh sách/Lọc | POST `GetAllAsync` | `[SBAuthorize]` rỗng | internal | — | Phân trang + lọc chiến dịch theo filter. | EF | Quyền chỉ login (key rỗng) |
| 2 | Lấy theo Id | Mở edit | GET `GetById/{id}` | `[SBAuthorize]` rỗng | internal | — | Trả chi tiết 1 chiến dịch. | EF | Quyền chỉ login |
| 3 | Thêm mới | Lưu | POST `ThemMoi` | (không attribute) | anonymous | — | Insert chiến dịch mới. | EF | **Không có `[SBAuthorize]`** → ai cũng gọi được |
| 4 | Cập nhật | Lưu | POST `CapNhat` | `[SBAuthorize]` rỗng | internal | — | Update chiến dịch theo Id. | EF | Quyền chỉ login |
| 5 | Xóa | Xóa | POST `Xoa/{id}` | `[SBAuthorize]` rỗng | internal | — | Xóa chiến dịch theo Id. | EF | Quyền chỉ login |

## Quan hệ
- **Gọi sang:** thuần EF qua repository; service inject thêm `UngVien`, `User` (chiến dịch gắn ứng viên).
- **Dữ liệu (entities):** `ChienDich` (đợt tuyển dụng), liên quan `UngVien` (ứng viên thuộc chiến dịch).
- **Ripple / đồng bộ:** không có side-effect cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `ChienDichController.cs:36` — `ThemMoi` **không có** attribute `[SBAuthorize]` (các method khác chỉ `[SBAuthorize]` rỗng = chỉ login) → endpoint thêm mới không bị chặn quyền.
- Toàn controller chỉ dùng `[SBAuthorize]` rỗng (không key cụ thể) → không phân quyền chi tiết.
