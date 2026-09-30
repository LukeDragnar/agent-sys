---
controller: MarketingDashboardController
module: sales
service: Contact
man_hinh_ui: "Marketing > Dashboard (marketing-dashboard.component / marketing-dashboard-20250609)"
muc_dich: "Cung cấp dữ liệu cho dashboard Marketing: lịch sử thao tác dữ liệu của user (user activities log) và danh mục loại hoạt động để lọc."
alias: [MarketingDashboard, "dashboard MKT", "dashboard marketing"]
auth: public
state_machine: []
deprecated: false
entities: [UserActivitiesLog, DMActivityLog]
stored_procs: []
depends: [MarketingDashboardService]
cross_module_in: []
cross_module_out: []
---

# MarketingDashboard — Dashboard Marketing

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Lịch sử thao tác dữ liệu | Lọc / Danh sách | POST `GetUserActivitiesLog` | `MarketingDashboardController_GetUserActivitiesLog` (DV-AD-TONG/CN/MCN, DV-QL-MKT, DV-MKT-LEAD, DV-MKT) | public | màn dashboard MKT | Trả log thao tác user theo filter (đơn vị/khoảng ngày/loại hoạt động). | EF | — |
| 2 | Danh mục loại hoạt động | (load combo) | GET `GetDMActivityLog` | `[SBAuthorize]` (login) | public | bộ lọc dashboard | Trả danh mục loại activity để lọc. | EF | — |

## Quan hệ
- **Gọi sang:** `MarketingDashboardService`.
- **Dữ liệu (entities):** UserActivitiesLog (lịch sử thao tác), DMActivityLog (danh mục loại hoạt động).
- **Ripple / đồng bộ:** chỉ đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- `MarketingDashboardController.cs:17-18` — DI khai báo `IDataReceiveService`, `IContactStoreService` nhưng **không inject qua constructor và không dùng** → field thừa, dễ gây nhầm.
- 2 method dashboard (`TimTheoDieuKien`, `GetStatisticsLast7DayOfLanding`) đã bị comment → dashboard chart hiện không expose ở controller này (có thể đã chuyển sang component/version mới `marketing-dashboard-20250609`).
