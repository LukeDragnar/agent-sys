---
controller: MyHubUserSwitchboardController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài MyHub (sim số) > User tổng đài (danh sách tổng đài sim số, tìm theo điều kiện, xuất Excel, đổi user, chi tiết)"
muc_dich: "Quản lý user tổng đài MyHub (sim số): tìm user tổng đài theo điều kiện, lấy thông tin tổng đài sim số, xuất Excel tổng đài sim số, hoán đổi user (fullbooking), lấy chi tiết user — auto-scope đơn vị nếu không super/kế-toán."
alias: ["user tổng đài MyHub", "tổng đài sim số", "xuất excel sim số", "đổi user fullbooking"]
auth: public
state_machine: []
deprecated: false
entities: [UserSwitchboard, TongDaiSimSo]
stored_procs: []
depends: [IUserSwitchboardService, "GemBox.Spreadsheet (xuất Excel)"]
cross_module_in: []
cross_module_out: []
---

# MyHubUserSwitchboard — User tổng đài MyHub (sim số)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm user tổng đài theo điều kiện | Tìm | POST `TimTheoDieuKien` | `MyHubUserSwitchboard_TimTheoDieuKien` (HT-KT, HT-AD, DV-AD-TONG) | public | màn user tổng đài | Không super/kế-toán → ép `IdDonVi=DonViId`; trả danh sách user tổng đài. | EF | — |
| 2 | Thông tin tổng đài sim số | (load) | POST `GetTongDaiSimSo` | `MyHubUserSwitchboard_GetTongDaiSimSo` (nhiều role DV) | public | màn sim số | Auto-scope `IdDonVi`; trả thông tin tổng đài sim số (V2). | EF | — |
| 3 | Xuất Excel tổng đài sim số | Xuất Excel | POST `ExportExcelTongDaiSimSo` | `MyHubUserSwitchboard_ExportExcelTongDaiSimSo` (nhiều role DV) | public | nút xuất Excel | Sinh `TongDaiSimSo.xlsx`; lỗi `Service_Unavailable` → **503**, lỗi khác → **500** (cùng code `EXPORT_ERROR`). | GemBox | FE cần handle 503 vs 500 riêng |
| 4 | Hoán đổi user (fullbooking) | Đổi user | POST `ChangeUser` | `MyHubUserSwitchboard_ChangeUser` (HT-KT, HT-AD, HT-CSKH, DV-AD-TONG) | public | user đã map | Invalid → lỗi; auto-scope `IdDonVi`; hoán đổi user fullbooking. | EF | — |
| 5 | Chi tiết user tổng đài | (load) | GET `GetById/{id}` | `MyHubUserSwitchboard_GetById` (HT-KT, HT-AD, HT-CSKH, DV-AD-TONG) | public | mở chi tiết | super/kế-toán → toàn hệ thống, ngược lại scope `IdDonVi`; trả chi tiết user. | EF | — |

## Quan hệ
- **Gọi sang:** `IUserSwitchboardService` (tìm/get/export/đổi user), GemBox (xuất Excel).
- **Dữ liệu (entities):** user tổng đài sim số (MyHub).
- **Ripple / đồng bộ:** đổi user fullbooking ảnh hưởng mapping user ↔ tổng đài.

## Lỗi đã biết / lưu ý kỹ thuật
- `MyHubUserSwitchboardController.cs:48-78` — `ExportExcelTongDaiSimSo` phân biệt 503 (`Service_Unavailable`) vs 500, cùng code `EXPORT_ERROR`. FE phải handle cả 2.
- Toàn bộ action auto-scope `IdDonVi` khi không super/kế-toán.
