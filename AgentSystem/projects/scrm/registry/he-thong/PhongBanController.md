---
controller: PhongBanController
module: he-thong
service: Core
man_hinh_ui: "Đơn vị > Cơ cấu tổ chức > Phòng ban: cây cơ cấu phòng ban (kéo-thả sắp xếp) + form Thêm/Sửa/Xóa phòng ban; phân tách thao tác theo cấp Đơn vị (Unit) vs Chi nhánh (Branch)"
muc_dich: "Quản lý cơ cấu phòng ban của đơn vị/chi nhánh: lấy cây cơ cấu, sắp xếp, CRUD phòng ban (tách bản Unit cho ad tổng và Branch cho ad chi nhánh), thêm phòng ban kèm user quản lý."
alias: ["phòng ban", "cơ cấu phòng ban", "cơ cấu tổ chức", "PhongBan"]
auth: mixed
state_machine: []
deprecated: false
entities: [PhongBan]
stored_procs: []
depends: ["IPhongBanService", "BaseController", "IRedisCache (qua service)"]
cross_module_in: ["FE cơ cấu phòng ban gọi GetCoCauPhongBan_Unit/_Branch, SapXepCoCauPhongBan, ThemMoi/CapNhat/Xoa _Unit/_Branch", "User thường gọi UserGetThongTinPhongBan"]
cross_module_out: ["GetAndSetCoCauCache_TheoDonVi build & cache cơ cấu phòng ban đơn vị → phục vụ phân cấp quản lý sale/nhân viên cross-module"]
---

# PhongBanController — Cơ cấu phòng ban

Kế thừa `BaseController`, controller-level `[TienIch(TI_Chung)]`. Đặc trưng: hầu hết thao tác tách đôi **_Unit** (cấp đơn vị, role DV-AD-TONG) và **_Branch** (cấp chi nhánh, role DV-AD-CN; tự gán `IdChiNhanh = CurrentUser.IdChiNhanh`). Có endpoint private build cache cơ cấu. Auth = mixed (key theo từng method + 1 private + 1 SBAuthorize rỗng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cơ cấu phòng ban | (load cây) | POST `api/PhongBan/GetCoCauPhongBan_Unit` / `GetCoCauPhongBan_Branch` | `PhongBan_GetCoCauPhongBan_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | theo cấp Unit/Branch | Trả cây cơ cấu phòng ban; bản Branch ép `IdChiNhanh` theo CurrentUser. | EF | — |
| 2 | Sắp xếp cơ cấu | (kéo-thả lưu) | POST `api/PhongBan/SapXepCoCauPhongBan` | `PhongBan_SapXepCoCauPhongBan` (DV-AD-TONG DV-AD-CN) | public | — | Lưu thứ tự/cấp cha-con cơ cấu theo list `PhongBanDto`. | EF | — |
| 3 | Thêm mới | Thêm | POST `api/PhongBan/ThemMoi_Unit` / `ThemMoi_Branch` | `PhongBan_ThemMoi_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `Create(payload, CurrentUser)`; Branch ép IdChiNhanh. | EF | — |
| 4 | Cập nhật | Sửa | POST `api/PhongBan/CapNhat_Unit` / `CapNhat_Branch` | `PhongBan_CapNhat_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `Update(payload, CurrentUser)`; Branch ép IdChiNhanh. | EF | — |
| 5 | Xóa | Xóa | POST `api/PhongBan/Xoa_Unit` / `Xoa_Branch` | `PhongBan_Xoa_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `Delete(payload, CurrentUser)`; Branch ép IdChiNhanh. | EF | — |
| 6 | Tìm theo Id | (mở chi tiết) | POST `api/PhongBan/TimTheoId_Unit` / `TimTheoId_Branch` | `PhongBan_TimTheoId_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `GetById(payload, CurrentUser)`; Branch ép IdChiNhanh. | EF | — |
| 7 | Thêm phòng ban + user quản lý | Thêm (gán QL) | POST `api/PhongBan/ThemMoi_UnitQuanLy` / `ThemMoi_BranchQuanLy` | `PhongBan_ThemMoi_Unit` (DV-AD-TONG) / `PhongBan_ThemMoi_Branch` (DV-AD-CN) | public | — | `ThemPhongBanUserQuanLy(payload, CurrentUser)`; ép IdDonVi (+ IdChiNhanh cho branch). | EF | Dùng lại key `PhongBan_ThemMoi_*` của thêm thường — không có key riêng cho biến thể QuanLy |
| 8 | Lấy cơ cấu (CurrentUser) | (load cây tổng) | POST `api/PhongBan/GetCoCauPhongBan` | `PhongBan_TimTheoId_GetCoCauPhongBan` (DV-AD-TONG/CN/MCN) | public | — | `GetCoCauPhongBan(CurrentUser)` trả cơ cấu theo user. | EF | — |
| 9 | User xem phòng ban | (màn user) | POST `api/PhongBan/UserGetThongTinPhongBan` | — (login, `[SBAuthorize]` rỗng) | public | — | `UserGetThongTinPhongBan(CurrentUser)` trả thông tin phòng ban của user. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] **POST `GetAndSetCoCauCache_TheoDonVi/{id:Guid}`** `[PrivateSBAuthorize]` — service khác gọi để build & cache cơ cấu phòng ban của 1 đơn vị (phục vụ phân cấp quản lý sale/nhân viên).

## Quan hệ
- **Gọi sang:** IPhongBanService (EF + cache).
- **Dữ liệu (entities):** `PhongBan` (cơ cấu cây).
- **Ripple / đồng bộ:** Cache cơ cấu phòng ban dùng để xác định quản lý/lead của user (liên quan PhongBanNhanVien.KiemTraLeadSaleOrQuanLySale).

## Lỗi đã biết / lưu ý kỹ thuật
- `PhongBan_SapXepCoCauPhongBan` (dòng 33) khai báo role chuỗi `"DV-AD-TONG DV-AD-CN"` (thiếu dấu phẩy giữa 2 role) → cần kiểm tra cách parse role có tách đúng không.
- Biến thể `*_UnitQuanLy`/`*_BranchQuanLy` tái dùng key `PhongBan_ThemMoi_Unit`/`_Branch` của thêm thường → không phân quyền tách riêng được hành vi gán user quản lý.
