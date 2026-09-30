---
controller: PhongBanNhanVienController
module: he-thong
service: Core
man_hinh_ui: "Đơn vị > Cơ cấu tổ chức > (chi tiết phòng ban): thêm/xóa nhân viên vào phòng ban/nhóm, xem danh sách nhân viên theo nhóm; tách thao tác Unit vs Branch"
muc_dich: "Quản lý nhân viên trong phòng ban/nhóm: thêm nhiều user vào nhóm, xóa user khỏi nhóm, lấy danh sách/ID nhân viên theo nhóm (tách Unit/Branch). Có endpoint private kiểm tra quan hệ lead/quản lý sale."
alias: ["nhân viên phòng ban", "user nhóm nhân viên", "thành viên phòng ban", "PhongBanNhanVien"]
auth: mixed
state_machine: []
deprecated: false
entities: [UserNhomNhanVien]
stored_procs: []
depends: ["IUserNhomNhanVienService", "BaseController"]
cross_module_in: ["FE chi tiết phòng ban gọi ThemMoi_Unit/_Branch, Xoa_Unit/_Branch, GetAllIdUser, GetDanhSachNhanVien", "Service sale gọi KiemTraLeadSaleOrQuanLySale để check quan hệ lead/quản lý"]
cross_module_out: ["Thay đổi thành viên nhóm → ảnh hưởng phân công sale/CSKH & phân cấp lead/quản lý"]
---

# PhongBanNhanVienController — Nhân viên trong phòng ban/nhóm

Kế thừa `BaseController`, controller-level `[TienIch(TI_Chung)]`. Dùng `IUserNhomNhanVienService`. Thao tác thêm/xóa tách **_Unit** (DV-AD-TONG) / **_Branch** (DV-AD-CN, ép IdChiNhanh). Có 1 endpoint private check quan hệ lead/quản lý sale.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm nhân viên vào nhóm | Thêm NV | POST `api/PhongBanNhanVien/ThemMoi_Unit` / `ThemMoi_Branch` | `PhongBanNhanVien_ThemMoi_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `AddMultiUser(payload, CurrentUser)` thêm nhiều user vào nhóm; Branch ép IdChiNhanh. | EF | — |
| 2 | Xóa nhân viên khỏi nhóm | Xóa NV | POST `api/PhongBanNhanVien/Xoa_Unit` / `Xoa_Branch` | `PhongBanNhanVien_Xoa_Unit` (DV-AD-TONG) / `_Branch` (DV-AD-CN) | public | — | `DeleteUserNhomNhanVien(payload, CurrentUser)`; Branch ép IdChiNhanh. | EF | — |
| 3 | Lấy tất cả idUser theo nhóm | (load checkbox) | POST `api/PhongBanNhanVien/GetAllIdUser` | `PhongBanNhanVien_GetAllIdUser` (DV-AD-CN, DV-AD-TONG) | public | — | `ShowListUserNhomNhanVien(id, CurrentUser)` trả danh sách idUser thuộc nhóm. | EF | — |
| 4 | Lấy danh sách nhân viên | (load lưới NV) | POST `api/PhongBanNhanVien/GetDanhSachNhanVien` | `PhongBanNhanVien_GetDanhSachNhanVien` (DV-AD-CN, DV-AD-TONG) | public | — | `GetDanhSachNhanVien(payload, CurrentUser)` trả nhân viên theo `PhongBanNhanVienFilter`. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] **POST `KiemTraLeadSaleOrQuanLySale`** `[PrivateSBAuthorize]` — service sale gọi để kiểm tra `LeadOrQlSaleId` CÓ phải lead/quản lý của `SaleId` không (phân cấp quản lý sale).

## Quan hệ
- **Gọi sang:** IUserNhomNhanVienService (EF).
- **Dữ liệu (entities):** `UserNhomNhanVien` (map user↔nhóm/phòng ban).
- **Ripple / đồng bộ:** Đổi thành viên nhóm → ảnh hưởng phân công sale/CSKH & quan hệ lead/quản lý.

## Lỗi đã biết / lưu ý kỹ thuật
- Thao tác Branch chỉ ép `IdChiNhanh` từ CurrentUser; payload Unit dùng nguyên giá trị client gửi — cần đảm bảo service validate IdDonVi/IdChiNhanh hợp lệ với CurrentUser.
