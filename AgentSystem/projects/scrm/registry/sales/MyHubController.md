---
controller: MyHubController
module: sales
service: Switchboard
man_hinh_ui: "nội bộ — webhook nhận data từ MyHub (ReceiveDataHotline, ReceiveData) + ClickToCall từ UI tổng đài MyHub"
muc_dich: "Tích hợp tổng đài MyHub: nhận webhook data cuộc gọi/hotline từ partner MyHub (anonymous), lưu data nhận từ partner, và thực hiện cuộc gọi (ClickToCall) từ UI. (LƯU Ý: tồn tại 2 MyHubController — bản này ở folder Controllers_MyHub, route `api/MyHub`; còn bản trong Controllers đã rỗng)."
alias: ["MyHub", "tổng đài MyHub", "click to call MyHub", "webhook MyHub"]
auth: mixed
state_machine: []
deprecated: false
entities: [LogHotline, LogData, DataReceived]
stored_procs: []
depends: [IMyHubService, ISwitchboardService, "MyHub partner (external webhook)"]
cross_module_in: ["MyHub partner (ReceiveDataHotline, ReceiveData — webhook đẩy data cuộc gọi)"]
cross_module_out: ["MyHub partner (ClickToCall — thực hiện cuộc gọi)"]
---

# MyHub — Tích hợp tổng đài MyHub (webhook + click-to-call)

> ⚠️ Trùng tên với `Controllers\MyHubController` (đã rỗng). Bản ACTIVE này nằm ở `Controllers_MyHub\MyHubController.cs`, `TI_TongDai_SimSo`. Vì cùng route `api/MyHub` nên chỉ 1 bản được map — bản này có endpoint hoạt động.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận webhook data hotline | (webhook) | POST `ReceiveDataHotline` | (không attribute → anonymous) | anonymous | partner đẩy về | Deserialize body dynamic → `IMyHubService.ReceiveDataHotline`. | EF | Endpoint webhook KHÔNG auth → cần validate signature ở tầng khác |
| 2 | Nhận data cuộc gọi từ partner | (webhook) | POST `ReceiveData` | (không attribute → anonymous) | anonymous | partner đẩy về | Lưu data nhận từ partner MyHub (`SaveDataReceivedFromPartner`). | EF | Endpoint webhook KHÔNG auth |
| 3 | Thực hiện cuộc gọi (click to call) | Gọi | POST `ClickToCall` | `MyHub_ClickToCall` (nhiều role DV) | public | nút gọi trên UI | ModelState invalid → lỗi; hợp lệ → `IMyHubService.MyHubClickToCall`. | partner | — |

## Endpoint nội bộ (private / service-to-service)
- `ReceiveDataHotline`, `ReceiveData` là **webhook inbound** (không `[SBAuthorize]`) — partner MyHub đẩy data về. (Có helper `GenerateCurlCommand` để log curl, hiện comment).

## Quan hệ
- **Gọi sang:** `IMyHubService` (xử lý data hotline + click-to-call), `ISwitchboardService` (lưu data từ partner).
- **Dữ liệu (entities):** log hotline/data (đa phần comment), data nhận từ partner.
- **Ripple / đồng bộ:** webhook nhận cuộc gọi từ MyHub → lưu vào hệ thống tổng đài.

## Lỗi đã biết / lưu ý kỹ thuật
- `MyHubController.cs (Controllers_MyHub):37,116` — `ReceiveDataHotline`/`ReceiveData` không có auth attribute → anonymous webhook. Bảo mật phải dựa vào validate ở service/IP whitelist.
- Trùng tên với `Controllers\MyHubController.cs` (bản đó toàn bộ comment, rỗng). Cùng route → lưu ý conflict khi build/map controller.
