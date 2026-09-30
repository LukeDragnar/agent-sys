---
controller: LiveController
module: ke-toan
service: AccountingReceive
man_hinh_ui: "nội bộ — không có UI (healthcheck + trigger thủ công job dọn dữ liệu)"
muc_dich: "Healthcheck service (test) và endpoint trigger thủ công job dọn dữ liệu phiếu cũ trên Mongo (JobClearDataReceivePhieuMongo)."
alias: ["healthcheck", "live", "test job", "ping service", "dọn data mongo"]
auth: anonymous
state_machine: []
deprecated: false
entities: [DataReceiveCrm, DataReceiveResultPartner]
stored_procs: []
depends: ["IJobDataService", "MongoDB (AccountingReceivePhieuCrm)"]
cross_module_in: ["Healthcheck/monitor gọi /test", "Vận hành gọi /test_job để chạy dọn data thủ công"]
cross_module_out: []
---

# LiveController (AccountingReceive) — Healthcheck + trigger job dọn data

Controller hạ tầng vận hành: kế thừa `ControllerBase` (không phải BaseController), không có auth. `test` trả chuỗi sống; `test_job` gọi `JobClearDataReceivePhieuMongo` để xóa thủ công các phiếu cũ (>7 ngày) trên collection Mongo `AccountingReceivePhieuCrm`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Healthcheck | (monitor) | GET `api/Live/test` | — | anonymous | — | Trả `"Service is running"` (kiểm tra service còn sống) | — | — |
| 2 | Trigger job dọn phiếu Mongo | (vận hành) | GET `api/Live/test_job` | — | anonymous | — | `JobClearDataReceivePhieuMongo`: xóa theo batch 200 các document Mongo `NgayTao < now-7d`; nếu không có data → trả lỗi "Không có dữ liệu cần xóa" | MongoDB | Xóa thật DB qua GET anonymous → ai gọi cũng kích hoạt dọn dữ liệu |

## Quan hệ
- **Gọi sang:** `IJobDataService.JobClearDataReceivePhieuMongo` (dọn Mongo). Service còn job dọn SQL (`JobXoaDataReceiveCrm`, `JobXoaDataReceiveResultPartner` qua stored proc `PRC_SCRM_AccoungtingReceive_*_Job_Clear`) nhưng KHÔNG expose ở controller này (chạy theo lịch).
- **Dữ liệu (entities):** `DataReceiveCrm`, `DataReceiveResultPartner` (SQL) + collection Mongo `AccountingReceivePhieuCrm`.
- **Ripple / đồng bộ:** dọn dữ liệu cũ, không phát event.

## Lỗi đã biết / lưu ý kỹ thuật
- `LiveController.cs:23-28` — `test_job` xóa dữ liệu Mongo qua GET không auth (anonymous) → rủi ro bị gọi nhầm/lạm dụng; nên đặt POST + bảo vệ hoặc chỉ để chạy nội bộ.
- `JobDataService.cs:49 & :77` — job SQL (`JobXoaDataReceiveCrm/ResultPartner`) lặp `count % 100` lần gọi stored proc với `Task.Delay(1000)` mỗi vòng → logic số vòng `% 100` lạ (không tỉ lệ thuận số bản ghi), cần xem lại; các job này không gọi từ LiveController.
