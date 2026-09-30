---
controller: TongDaiUserController
module: sales
service: Switchboard
man_hinh_ui: "Quản lý tổng đài (cấp hệ thống) > User tổng đài (tạo/sửa/xóa user kết nối, đổi user, lấy user partner)"
muc_dich: "Quản lý user tổng đài (user connective) — chủ yếu role hệ thống HT-AD/HT-KT: tạo/sửa/xóa/đổi user kết nối tổng đài, lấy danh sách user partner và chi tiết user."
alias: ["user tổng đài", "user connective", "đổi user tổng đài", "tạo user tổng đài"]
auth: public
state_machine: []
deprecated: false
entities: [TongDaiUser, UserConnective]
stored_procs: []
depends: [ITongDaiUserService, "Partner tổng đài (external)"]
cross_module_in: []
cross_module_out: ["Partner tổng đài (tạo/đổi/xóa user kết nối)"]
---

# TongDaiUser — Quản lý user tổng đài (user connective)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy all user partner | (dropdown) | POST `TongDaiUserLayAllUserPartner` | `[SBAuthorize()]` (login) | public | màn user tổng đài | Trả toàn bộ user của partner để chọn. | EF + partner | Key `TongDaiUser_TongDaiUserLayAllUserPartner` đã comment → chỉ `[SBAuthorize]` rỗng |
| 2 | Tạo mới user tổng đài | Thêm | POST `TongDaiUserTaoMoi` | `TongDaiUser_TongDaiUserTaoMoi` (HT-AD, HT-KT) | public | màn user | Tạo user connective mới. | EF + partner | — |
| 3 | Danh sách user connective | (load) | POST `GetListUserConnective` | `TongDaiUser_GetListUserConnective` (HT-AD, HT-KT) | public | màn danh sách | Trả danh sách user connective. | EF | — |
| 4 | Xóa / cập nhật user connective | Xóa / Lưu | POST `DeleteUserConnective`, POST `UpdateUserConnective` | `_DeleteUserConnective` / `_UpdateUserConnective` (HT-AD, HT-KT) | public | dòng user | Xóa / cập nhật user connective. | EF + partner | — |
| 5 | Chi tiết user tổng đài | (load) | POST `TongDaiUserChiTiet` | `TongDaiUser_TongDaiUserChiTiet` (HT-AD, HT-KT, DV-AD-TONG) | public | mở chi tiết | Trả chi tiết user tổng đài. | EF | — |
| 6 | Thay đổi user tổng đài | Đổi user | POST `ChangeUserConnective` | `TongDaiUser_ChangeUserConnective` (HT-AD, HT-KT, DV-AD-TONG) | public | user đã map | Hoán đổi user connective. | EF + partner | — |

## Quan hệ
- **Gọi sang:** `ITongDaiUserService` → partner tổng đài (CRUD user connective).
- **Dữ liệu (entities):** user tổng đài / user connective.
- **Ripple / đồng bộ:** tạo/đổi/xóa user đẩy sang partner tổng đài.

## Lỗi đã biết / lưu ý kỹ thuật
- `TongDaiUserController.cs:27-39` — endpoint `TongDaiUserDanhSach` (có logic scope `CrmUnitId` theo đơn vị) đã bị comment toàn bộ → không expose.
- `TongDaiUserController.cs:41-43` — `TongDaiUserLayAllUserPartner` key role đã comment, còn `[SBAuthorize()]` rỗng → user login bất kỳ gọi được.
