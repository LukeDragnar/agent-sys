---
controller: UngVienController
module: nhan-su
service: HumanResource
man_hinh_ui: "Tuyển dụng > Ứng viên (danh sách + thêm/sửa/xóa hồ sơ ứng viên)"
muc_dich: "CRUD ứng viên tuyển dụng (hồ sơ ứng viên, gắn chiến dịch tuyển dụng + file giấy tờ)."
alias: [UngVien, "ứng viên", "hồ sơ ứng viên", "candidate"]
auth: mixed
state_machine: []
deprecated: false
entities: [UngVien, FileNhanSu]
stored_procs: []
depends: []
cross_module_in: [ChienDich (ứng viên thuộc chiến dịch tuyển dụng)]
cross_module_out: []
---

# UngVien — Quản lý ứng viên tuyển dụng

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | Danh sách/Lọc | POST `GetAllAsync` | `[SBAuthorize]` rỗng | internal | — | Phân trang + lọc ứng viên theo filter. | EF | Quyền chỉ login |
| 2 | Lấy tất cả ứng viên | (combo/dropdown) | POST `GetAll` | `[SBAuthorize]` rỗng | internal | — | Trả toàn bộ ứng viên (không phân trang). | EF | Quyền chỉ login; không paging |
| 3 | Lấy theo Id | Mở edit | GET `GetById/{id}` | `[SBAuthorize]` rỗng | internal | — | Trả chi tiết 1 ứng viên. | EF | Quyền chỉ login |
| 4 | Thêm mới | Lưu | POST `ThemMoi` | (không attribute) | anonymous | — | Insert ứng viên mới (kèm giấy tờ FileNhanSu nếu có). | EF | **Không có `[SBAuthorize]`** → không chặn quyền |
| 5 | Cập nhật | Lưu | POST `CapNhat` | `[SBAuthorize]` rỗng | internal | — | Update ứng viên theo Id. | EF | Quyền chỉ login |
| 6 | Xóa | Xóa | POST `Xoa/{id}` | `[SBAuthorize]` rỗng | internal | — | Xóa ứng viên theo Id. | EF | Quyền chỉ login |

## Quan hệ
- **Gọi sang:** thuần EF qua repository (`UngVien`, `FileNhanSu`).
- **Dữ liệu (entities):** `UngVien` (hồ sơ ứng viên), `FileNhanSu` (giấy tờ đính kèm). Ứng viên gắn `ChienDich` tuyển dụng.
- **Ripple / đồng bộ:** không có side-effect cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `UngVienController.cs:42` — `ThemMoi` **không có** attribute `[SBAuthorize]` → endpoint thêm mới không bị chặn quyền.
- `GetAll` trả toàn bộ ứng viên không phân trang — cần lưu ý hiệu năng nếu dữ liệu lớn.
- Toàn controller dùng `[SBAuthorize]` rỗng (chỉ login), không phân quyền key cụ thể.
