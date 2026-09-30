---
controller: LiveLumiController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI (health-check / ping tới server Lumi Smarthome)"
muc_dich: "Kiểm tra kết nối (live/health-check) tới server bên ngoài api.smarthome.lumi.vn: gọi HTTP GET và trả về có kết nối được hay không. Endpoint tiện ích, không gắn auth, không chạm DB."
alias: ["LiveLumi", "ping lumi", "health check lumi", "PingLumi"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["HttpClient (gọi https://api.smarthome.lumi.vn)", "System.Net.NetworkInformation.Ping (bản nháp không dùng)"]
cross_module_in: ["Công cụ giám sát / dev gọi GET /api/LiveLumi/PingLumi để kiểm tra kết nối tới Lumi"]
cross_module_out: ["Gọi ra ngoài tới https://api.smarthome.lumi.vn (Lumi Smarthome)"]
---

# LiveLumiController — Health-check kết nối server Lumi

Controller tiện ích 1 endpoint: gọi HTTP GET tới `https://api.smarthome.lumi.vn` rồi báo có/không kết nối được. Kế thừa `ControllerBase` thuần, **không** `BaseController`, **không** `[TienIch]`, **không** `[SBAuthorize]` → **anonymous**. Không chạm DB, không service nghiệp vụ. Có method nháp `PingServerLumi1()` (dùng ICMP Ping) **private, không được route** → dead code.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Ping server Lumi | (không UI) | GET `api/LiveLumi/PingLumi` | — | anonymous | — | `CanConnectToServer()`: `HttpClient.GetAsync(https://api.smarthome.lumi.vn)`; 2xx → `ResultWithData(true, body)`; status khác → `ResultWithError(statusCode)`; exception → log Console + `ResultWithError(message)`. Luôn trả `Ok(...)` (200). | Không DB (gọi HTTP ngoài) | Tạo `new HttpClient()` mỗi request (không dùng IHttpClientFactory) → nguy cơ socket exhaustion |

## Quan hệ
- **Gọi sang:** `HttpClient.GetAsync` tới `https://api.smarthome.lumi.vn`.
- **Dữ liệu (entities):** không có (không chạm DB).
- **Ripple / đồng bộ:** không có side-effect; chỉ đọc trạng thái kết nối ngoài.

## Lỗi đã biết / lưu ý kỹ thuật
- `LiveLumiController.cs:37` — `new HttpClient()` trực tiếp trong mỗi request (không IHttpClientFactory/singleton) → **socket exhaustion** nếu gọi nhiều.
- `LiveLumiController.cs:11` — namespace `Authen.API.Controllers` (lệch service Core — copy từ Authen); không ảnh hưởng route nhưng dễ gây nhầm về service chủ.
- `LiveLumiController.cs:16` — kế thừa `ControllerBase` thuần + không `[SBAuthorize]` → endpoint **anonymous**; mọi người gọi được (chỉ là health-check nên rủi ro thấp, nhưng vẫn lộ dependency Lumi).
- `LiveLumiController.cs:56-88` — `PingServerLumi1()` (ICMP `Ping`) là **dead code**: private, không gắn route; còn `reply.Options.Ttl`/`ex.InnerException.Message` có thể NRE nếu null. Nên xóa hoặc dùng có ý thức.
- Endpoint hard-code URL Lumi → mang tính tạm thời/đặc thù tích hợp; cân nhắc đưa vào config nếu giữ lâu dài.
