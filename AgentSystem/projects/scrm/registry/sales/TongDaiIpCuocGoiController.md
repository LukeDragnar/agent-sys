---
controller: TongDaiIpCuocGoiController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài IP > Lịch sử/Báo cáo cuộc gọi (danh sách cuộc gọi, báo cáo tổng đài, xuất Excel lịch sử cuộc gọi omicall)"
muc_dich: "Tra cứu cuộc gọi tổng đài IP (Omicall): lấy danh sách lịch sử cuộc gọi, báo cáo/thống kê cuộc gọi, và xuất Excel lịch sử cuộc gọi."
alias: ["cuộc gọi tổng đài IP", "call history omicall", "báo cáo cuộc gọi", "xuất excel cuộc gọi"]
auth: public
state_machine: []
deprecated: false
entities: [TongDaiLichSuCuocGoi, CallHistoryOmicall]
stored_procs: []
depends: [ITongDaiIpCuocGoiService, "Omicall (nguồn data cuộc gọi)", "GemBox.Spreadsheet (xuất Excel)"]
cross_module_in: []
cross_module_out: []
---

# TongDaiIpCuocGoi — Cuộc gọi tổng đài IP (Omicall)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy danh sách cuộc gọi | (load màn) | POST `LayDanhSachCuocGoi` | `TongDaiIpCuocGoi_LayDanhSachCuocGoi` (nhiều role DV) | public | màn lịch sử cuộc gọi IP | Trả danh sách call history theo điều kiện + `CurrentUser`. | EF/Omicall | — |
| 2 | Lấy báo cáo cuộc gọi | Báo cáo | POST `LayDanhSachBaoCaoCuocGoi` | `TongDaiIpCuocGoi_LayDanhSachBaoCaoCuocGoi` (nhiều role DV) | public | tab báo cáo tổng đài | Trả số liệu báo cáo/thống kê cuộc gọi. | EF/Omicall | — |
| 3 | Xuất Excel lịch sử cuộc gọi | Xuất Excel | POST `ExportLichSuCuocGoi` | `TongDaiIpCuocGoi_ExportLichSuCuocGoi` (nhiều role DV) | public | nút xuất Excel | Sinh file `data_callhistory.xlsx`; nếu service lỗi → trả **503** `EXPORT_ERROR` (không phải 500). | GemBox/Omicall | Lỗi export trả HTTP 503 (Service Unavailable) thay vì 500 — FE cần handle 503 |

## Quan hệ
- **Gọi sang:** `ITongDaiIpCuocGoiService` (đọc data cuộc gọi từ Omicall + dựng báo cáo/Excel).
- **Dữ liệu (entities):** lịch sử cuộc gọi tổng đài IP (nguồn Omicall).
- **Ripple / đồng bộ:** không có side-effect ghi; chỉ đọc + export.

## Lỗi đã biết / lưu ý kỹ thuật
- `TongDaiIpCuocGoiController.cs:69-77` — `ExportLichSuCuocGoi` khi `result.Success == false` trả `StatusCode(503)` với body `{ Status, Message, Code = "EXPORT_ERROR" }`. FE phải bắt 503 chứ không phải 500.
- Class-level `[SBAuthorize]` + key per-action → mọi endpoint cần login + đúng role.
