---
controller: LichSuBoLocController
module: he-thong
service: LogService
man_hinh_ui: "Lịch sử > Lịch sử bộ lọc / lịch sử tìm kiếm (màn xem + xuất Excel)"
muc_dich: "Ghi nhận và tra cứu lịch sử bộ lọc / tìm kiếm của user; hỗ trợ xuất Excel lịch sử bộ lọc và lịch sử tìm kiếm."
alias: ["lịch sử bộ lọc", "lịch sử tìm kiếm", "bộ lọc đã lưu"]
auth: internal
state_machine: []
deprecated: false
entities: [LichSuBoLoc, EventLichSuBoLoc]
stored_procs: []
depends: [ILichSuBoLocService, GemBox.Spreadsheet]
cross_module_in: ["FE màn lịch sử bộ lọc/tìm kiếm gọi InsertAsync khi user lưu/áp bộ lọc; gọi Tim/Export khi xem"]
cross_module_out: []
---

# LichSuBoLocController — Lịch sử bộ lọc / tìm kiếm

TienIch: `LichSuBoLocController` "Quản lý lịch sử bộ lọc" (MaTienIchSCRM.TI_Chung). Route `api/LichSuBoLoc`. Mọi action có `[SBAuthorize]` key riêng → internal (theo quyền).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới bộ lọc | (lưu bộ lọc) | POST `api/LichSuBoLoc/InsertAsync` | LichSuBoLocController_InsertAsync | internal | — | Ghi 1 EventLichSuBoLoc | EF | — |
| 2 | Tìm theo điều kiện | Tìm | POST `api/LichSuBoLoc/TimTheoDieuKien` | LichSuBoLocController_TimTheoDieuKien | internal | — | Lọc lịch sử theo điều kiện + CurrentUser | EF | — |
| 3 | Lấy theo người tạo | (load list) | POST `api/LichSuBoLoc/GetLstByUserCreate` | LichSuBoLocController_GetLstByUserCreate | internal | — | List bộ lọc của user (phân trang) | EF | — |
| 4 | Xuất Excel lịch sử bộ lọc | Xuất Excel | POST `api/LichSuBoLoc/ExportExcelLichSuBoLoc` | LichSuBoLocController_ExportExcelLichSuBoLoc | internal | — | Trả file `LichSuBoLoc.xlsx`; rs.Success=false → BadRequest | EF + GemBox | — |
| 5 | Xuất Excel lịch sử tìm kiếm | Xuất Excel | POST `api/LichSuBoLoc/ExportExcelLichSuTimKiem` | LichSuBoLocController_ExportExcelLichSuTimKiem | internal | — | Trả file `LichSuBoLoc.xlsx`; rs.Success=false → BadRequest | EF + GemBox | Tên file Excel cũng là `LichSuBoLoc.xlsx` (không phân biệt với #4) |

## Quan hệ
- **Gọi sang:** `ILichSuBoLocService` (Insert/Tim/Export), GemBox.Spreadsheet (build Excel).
- **Dữ liệu (entities):** `LichSuBoLoc`, DTO `EventLichSuBoLoc`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `LichSuBoLocController.cs:60,76` — cả 2 endpoint export dùng cùng tên file `LichSuBoLoc.xlsx`; export "lịch sử tìm kiếm" nên đặt tên khác để tránh nhầm khi tải.
