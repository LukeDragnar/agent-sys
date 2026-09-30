---
controller: BranchReportController
module: bao-cao
service: Branch
man_hinh_ui: "Báo cáo > Biên bản bàn giao ca (báo cáo sản phẩm major khi giao ca)"
muc_dich: "Báo cáo brand phía service Branch: biên bản bàn giao ca theo sản phẩm major (report giao ca)."
alias: [BranchReport, "báo cáo brand", "báo cáo giao ca", "biên bản bàn giao ca major"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [Order (OrderReportProductGiaoCaFilter), NhanVien/User (Core)]
cross_module_in: []
cross_module_out: []
---

# BranchReport (Branch) — Báo cáo bàn giao ca (brand)

> Trùng tên với `BranchReportController` của service **Report** (`bao-cao/BranchReportController.md`) → node này dùng hậu tố `_Branch`. Đây là bản partial bên service Branch (`Branch.API`), `[SBAuthorize]` controller-level. Hiện chỉ 1 action giao ca major; là báo cáo (view) nên chú ý cột Nguồn.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo SP major khi giao ca | Báo cáo giao ca | POST `ReportMajorProductGiaoCa` | `Branch_ReportMajorProductGiaoCa` | public | Trả báo cáo sản phẩm major theo bộ lọc giao ca (`OrderReportProductGiaoCaFilter`) + CurrentUser. | `IBranchReportService` (EF/aggregate) | — |

## Quan hệ
- **Gọi sang:** Core/User (CurrentUser), dùng filter chia sẻ với Order (`OrderReportProductGiaoCaFilter`).
- **Dữ liệu (entities):** không map entity riêng; tổng hợp dữ liệu giao ca/sản phẩm qua `IBranchReportService`.
- **Ripple/đồng bộ:** chỉ đọc (report), không phát event.

## Lỗi đã biết / lưu ý kỹ thuật
- `BranchReportController.cs:16` — `partial class` nhưng hiện chỉ 1 part (1 action); để mở rộng thêm report giao ca sau.
- `BranchReportController.cs:21` — inject `IServiceProvider inject` nhưng không dùng (dead injection).
- Trùng tên controller với service Report → đặt `_Branch` để tách node trong graph.
