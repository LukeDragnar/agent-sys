---
controller: VanBanQuyDinhController
module: nhan-su
service: HumanResource
man_hinh_ui: "Nhân sự > Văn bản / Quyết định (danh sách + thêm/sửa/xóa văn bản quy định)"
muc_dich: "CRUD văn bản quy định / quyết định nội bộ về nhân sự."
alias: [VanBanQuyDinh, VBQD, "văn bản quy định", "văn bản quyết định", "quyết định nội bộ"]
auth: mixed
state_machine: []
deprecated: false
entities: [VanBanQuyDinh]
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# VanBanQuyDinh — Quản lý văn bản / quyết định nhân sự

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | Danh sách/Lọc | POST `GetAllAsync` | `[SBAuthorize]` rỗng | internal | — | Phân trang + lọc văn bản quy định theo filter. | EF | Quyền chỉ login |
| 2 | Lấy theo Id | Mở edit | GET `GetVBQDById/{id}` | `[SBAuthorize]` rỗng | internal | — | Trả chi tiết 1 văn bản quy định. | EF | Quyền chỉ login |
| 3 | Thêm mới | Lưu | POST `ThemMoi` | (không attribute) | anonymous | — | Insert văn bản quy định mới. | EF | **Không có `[SBAuthorize]`** → không chặn quyền |
| 4 | Cập nhật | Lưu | POST `CapNhat` | `[SBAuthorize]` rỗng | internal | — | Update văn bản theo Id. | EF | Quyền chỉ login |
| 5 | Xóa | Xóa | POST `Xoa/{id}` | `[SBAuthorize]` rỗng | internal | — | Xóa văn bản theo Id. | EF | Quyền chỉ login |

## Quan hệ
- **Gọi sang:** thuần EF qua repository (`VanBanQuyDinh`).
- **Dữ liệu (entities):** `VanBanQuyDinh` (văn bản / quyết định nội bộ nhân sự).
- **Ripple / đồng bộ:** không có side-effect cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `VanBanQuyDinhController.cs:36` — `ThemMoi` **không có** attribute `[SBAuthorize]` → endpoint thêm mới không bị chặn quyền.
- Toàn controller dùng `[SBAuthorize]` rỗng (chỉ login), không phân quyền key cụ thể.
