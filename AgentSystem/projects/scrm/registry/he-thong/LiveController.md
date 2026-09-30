---
controller: LiveController
module: he-thong
service: Authen
man_hinh_ui: "nội bộ — không có UI (health-check / smoke test cache)"
muc_dich: "Endpoint kiểm tra service Authen còn sống và Redis/HybridCache hoạt động (set thử 1 key)."
alias: ["health check", "live check", "test cache authen", "ping authen"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IRedisCacheService (Redis)", "IHybridCache (HybridRedisCache)"]
cross_module_in: ["Hệ thống giám sát / load balancer / dev → TestCache (smoke test)"]
cross_module_out: []
---

# LiveController — Health-check & smoke test cache

Controller nội bộ phục vụ kiểm tra liveness của service Authen và khả năng ghi cache. Chỉ còn 1 endpoint hoạt động (`TestCache`); các endpoint khác (`CheckLive` chạy background task) đã bị comment. Không gắn auth → anonymous.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Smoke test cache | (gọi giám sát) | GET `api/Live/TestCache` | — | anonymous | — | Set Redis key `TestCache` (TTL 10s) + HybridCache key `TestCache` (TTL 1 phút) → trả `Ok("Service Ok")`. Nếu Redis/Cache lỗi → exception → 500 (dùng để phát hiện cache hỏng). | Redis + HybridCache | Không có try/catch — lỗi cache làm vỡ response (đây cũng là chủ ý để báo "không sống") |

## Quan hệ
- **Gọi sang:** `IRedisCacheService.Set`, `IHybridCache.Set`.
- **Dữ liệu (entities):** không có.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `LiveController.cs:38-51` — `CheckLive` (background task queue) đã bị comment, chỉ còn `TestCache`.
- `LiveController.cs:56-59` — key `TestCache` Redis TTL 10s, HybridCache TTL 1 phút (không xóa) — vô hại nhưng ghi đè key cùng tên giữa các lần gọi.
