---
controller: UserDeviceController
module: he-thong
service: Core
man_hinh_ui: "Quản lý thiết bị login: lưới thiết bị đăng nhập của nhân viên + nút Phê duyệt / Hủy duyệt / Xóa thiết bị"
muc_dich: "Quản lý các thiết bị đăng nhập của nhân viên trong đơn vị: liệt kê, phê duyệt, hủy duyệt, xóa thiết bị (kiểm soát đăng nhập đa thiết bị)."
alias: ["UserDevice", "thiết bị login", "quản lý thiết bị đăng nhập", "duyệt thiết bị", "device login"]
auth: internal
state_machine: [ChoDuyet, DaDuyet, HuyDuyet]
deprecated: false
entities: [UserDevice]
stored_procs: []
depends: ["IUserDeviceService", "BaseController"]
cross_module_in: ["FE Quản lý thiết bị login gọi TimTheoDieuKien/DuyetThietBi/HuyDuyetThietBi/Xoa"]
cross_module_out: ["Phê duyệt/hủy thiết bị → ảnh hưởng quyền đăng nhập của nhân viên trên thiết bị đó; scope theo đơn vị (IdDonVi = CurrentUser.DonViId)"]
---

# UserDeviceController — Quản lý thiết bị login

Controller route `api/UserDevice`, `[TienIch(..., TI_Chung)]`. Quản lý thiết bị đăng nhập của nhân viên, scope theo đơn vị của người thao tác. Key `UserDevice_*` (DV-AD-TONG/CN/MCN) → auth = internal. (Có `TimTheoId` nhưng bị comment.)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/UserDevice/TimTheoDieuKien` | `UserDevice_TimTheoDieuKien` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | internal | — | Lọc thiết bị login theo `UserDeviceFilterDto` + CurrentUser. | EF | — |
| 2 | Phê duyệt thiết bị | Duyệt | POST `api/UserDevice/DuyetThietBi` | `UserDevice_DuyetThietBi` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | internal | Thiết bị chờ duyệt | `DuyetThietBi(req, CurrentUser)` → cho phép thiết bị đăng nhập. | EF | — |
| 3 | Hủy duyệt thiết bị | Hủy duyệt | POST `api/UserDevice/HuyDuyetThietBi` | `UserDevice_HuyDuyetThietBi` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | internal | Thiết bị đã duyệt | `HuyDuyetThietBi(req, CurrentUser)` → thu hồi quyền đăng nhập thiết bị. | EF | — |
| 4 | Xóa thiết bị | Xóa | POST `api/UserDevice/Xoa/{id:Guid}` | `UserDevice_Xoa` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | internal | Thiết bị thuộc đơn vị hiện tại | Lấy thiết bị; nếu `IdDonVi != CurrentUser.DonViId` → "Không tìm thấy thiết bị"; ngược lại `DeleteAsync`. | EF | — |

## Quy tắc nghiệp vụ (gating)
1. Xóa: `data.Data.IdDonVi != CurrentUser.DonViId` → trả "Không tìm thấy thiết bị" (chặn xóa chéo đơn vị). (UserDeviceController.cs:83)

## Quan hệ
- **Gọi sang:** IUserDeviceService (EF).
- **Dữ liệu (entities):** `UserDevice` (IdDonVi, trạng thái duyệt).
- **Ripple / đồng bộ:** duyệt/hủy/xóa thiết bị → ảnh hưởng đăng nhập của nhân viên; không bắn event.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserDeviceController.cs:29-41` endpoint `TimTheoId` bị comment (map sai `Role`→`RoleDto` trong code cũ).
- Xóa kiểm tra scope đơn vị thủ công thay vì chặn từ query → cần CurrentUser.DonViId đúng.
