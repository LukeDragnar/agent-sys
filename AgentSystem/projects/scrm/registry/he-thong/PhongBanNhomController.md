---
controller: PhongBanNhomController
module: he-thong
service: Core
man_hinh_ui: "Đơn vị > Cơ cấu tổ chức > Nhóm phòng ban: CRUD nhóm trong phòng ban + gán list user vào nhóm; tách thao tác Unit vs Branch"
muc_dich: "Quản lý 'nhóm phòng ban' (đơn vị tổ chức con trong phòng ban): CRUD nhóm (tách Unit/Branch) và thêm nhóm kèm danh sách user (ThemMoiNhomUser)."
alias: ["nhóm phòng ban", "nhóm trong phòng ban", "PhongBanNhom"]
auth: mixed
state_machine: []
deprecated: false
entities: [PhongBanNhom]
stored_procs: []
depends: ["IPhongBanNhomService", "BaseController", "FunctionCommon (CheckUserLoginIsRoleAdmin)"]
cross_module_in: ["FE cơ cấu phòng ban gọi ThemMoi/CapNhat/Xoa/TimTheoId _Unit/_Branch, ThemMoiNhomUser"]
cross_module_out: ["Nhóm phòng ban là khung gom user → ảnh hưởng cơ cấu tổ chức & phân công"]
---

# PhongBanNhomController — Nhóm phòng ban

Kế thừa `BaseController`, controller-level `[TienIch(TI_Chung)]`. CRUD tách **_Unit** (DV-AD-TONG) / **_Branch** (DV-AD-CN, ép IdChiNhanh). Có `ThemMoiNhomUser` (gán list user) chỉ `[SBAuthorize]` rỗng + tự ép phạm vi theo role admin. Auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới nhóm | Thêm | POST `api/PhongBanNhom/ThemMoi_Unit` / `ThemMoi_Branch` | `PhongBanNhom_ThemMoi_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `Create(payload, CurrentUser)`; Branch ép IdChiNhanh. | EF | — |
| 2 | Cập nhật nhóm | Sửa | POST `api/PhongBanNhom/CapNhat_Unit` / `CapNhat_Branch` | `PhongBanNhom_CapNhat_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `Update(payload, CurrentUser)`; Branch ép IdChiNhanh. | EF | — |
| 3 | Xóa nhóm | Xóa | POST `api/PhongBanNhom/Xoa_Unit` / `Xoa_Branch` | `PhongBanNhom_Xoa_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `Delete(payload, CurrentUser)`. | EF | Bản `_Branch` KHÔNG ép IdChiNhanh trước khi Delete (khác ThemMoi/CapNhat) — có thể xóa nhầm ngoài chi nhánh nếu service không tự chặn |
| 4 | Tìm theo Id | (mở chi tiết) | POST `api/PhongBanNhom/TimTheoId_Unit` / `TimTheoId_Branch` | `PhongBanNhom_TimTheoId_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `GetById(payload, CurrentUser)`. | EF | `_Branch` cũng không ép IdChiNhanh trước GetById |
| 5 | Thêm nhóm kèm list user | Lưu nhóm + thành viên | POST `api/PhongBanNhom/ThemMoiNhomUser` | — (login, `[SBAuthorize]` rỗng) | public | — | Ép IdDonVi=CurrentUser.DonViId; nếu KHÔNG phải role admin → ép IdChiNhanh; `ThemMoiNhomListUser(payload, CurrentUser)`. | EF | Chỉ `[SBAuthorize]` rỗng (key admin bị comment dòng 128) → mọi user login gọi được, chỉ giới hạn phạm vi bằng code |

## Quan hệ
- **Gọi sang:** IPhongBanNhomService (EF), FunctionCommon.CheckUserLoginIsRoleAdmin.
- **Dữ liệu (entities):** `PhongBanNhom`.
- **Ripple / đồng bộ:** Nhóm phòng ban là khung gom user → ảnh hưởng cơ cấu tổ chức.

## Lỗi đã biết / lưu ý kỹ thuật
- `Xoa_Branch` (dòng 87) và `TimTheoId_Branch` (dòng 113) KHÔNG ép `IdChiNhanh = CurrentUser.IdChiNhanh` trước khi gọi service (khác với ThemMoi/CapNhat _Branch) → phụ thuộc service tự chặn phạm vi chi nhánh.
- `ThemMoiNhomUser` (dòng 130) key admin bị comment → chỉ còn `[SBAuthorize]` rỗng; phạm vi chi nhánh chỉ được ép trong code khi không phải admin.
