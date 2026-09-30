---
controller: LichSuXemThongTinController
module: he-thong
service: LogService
man_hinh_ui: "Lịch sử > Lịch sử xem thông tin (KH/contact) — màn tra cứu, thống kê top + xuất Excel"
muc_dich: "Tra cứu lịch sử xem thông tin khách hàng/contact, thống kê top count theo user/contact, và xuất Excel."
alias: ["lịch sử xem thông tin", "ai đã xem", "top xem thông tin"]
auth: internal
state_machine: []
deprecated: false
entities: [LichSuXemThongTin]
stored_procs: []
depends: [ILichSuXemThongTinService, GemBox.Spreadsheet]
cross_module_in: ["FE màn lịch sử xem thông tin gọi Tim/GetTopCount*/Export"]
cross_module_out: []
---

# LichSuXemThongTinController — Lịch sử xem thông tin

TienIch: `LichSuXemThongTinController` "Quản lý lịch sử xem thông tin" (TI_Chung). Route `api/LichSuXemThongTin`. Mọi action có `[SBAuthorize]` key riêng → internal.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | Tìm | POST `api/LichSuXemThongTin/TimTheoDieuKien` | LichSuXemThongTinController_TimTheoDieuKien | internal | — | Lọc lịch sử xem + CurrentUser | EF | — |
| 2 | Top count tổng | (thống kê) | POST `api/LichSuXemThongTin/GetTopCount` | LichSuXemThongTinController_GetTopCount | internal | — | Bộ đếm top theo điều kiện | EF | — |
| 3 | Top count chi tiết theo contact | (drilldown) | POST `api/LichSuXemThongTin/GetTopCountDetailByIdContact` | LichSuXemThongTinController_GetTopCountDetailByIdContact | internal | — | Top count chi tiết theo IdContact | EF | — |
| 4 | List chi tiết theo user | (drilldown) | POST `api/LichSuXemThongTin/GetlstDetailByIdUser` | LichSuXemThongTinController_GetlstDetailByIdUser | internal | — | Chi tiết log theo IdUser | EF | — |
| 5 | Lịch sử chi tiết theo contact | (drilldown) | POST `api/LichSuXemThongTin/GetHistoryDetailByIdContact` | LichSuXemThongTinController_GetHistoryDetailByIdContact | internal | — | Lịch sử chi tiết theo IdContact | EF | — |
| 6 | Xuất Excel | Xuất Excel | POST `api/LichSuXemThongTin/ExportExcelLichSuXemThongTin` | LichSuXemThongTinController_ExportExcelLichSuXemThongTin | internal | — | Trả file Excel; rs.Success=false → BadRequest | EF + GemBox | Tên file Excel là `LichSuBoLoc.xlsx` (copy-paste sai) |

## Quan hệ
- **Gọi sang:** `ILichSuXemThongTinService` (Tim/GetTopCount*/Export), GemBox.Spreadsheet.
- **Dữ liệu (entities):** `LichSuXemThongTin`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `LichSuXemThongTinController.cs:77` — file Excel export đặt tên `LichSuBoLoc.xlsx` (sai, nên là `LichSuXemThongTin.xlsx`).
