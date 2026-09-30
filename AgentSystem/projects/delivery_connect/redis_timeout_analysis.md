# Redis Timeout — Phân tích & Giải pháp

**Ngày bắt đầu:** 2026-06-05
**Container quan sát:** `df708373100a`, `5d0aa54fd444`, `2bf48001ee97`
**Source path:** `D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\`
**Stack tech:** .NET 6, MassTransit, StackExchange.Redis v2.7.10.12442, Redis server `10.0.63.32:6379`

---

## 1. Triệu chứng

`DeliveryConnect.API.Consumer.CreateOrderConsumer.Consume` ném timeout exception khi gọi Redis (qua `RedisCacheService`). Mỗi tháng có ~300-340 timeout, tập trung trong giờ làm việc.

## 1.1. Sample log thực tế — full message từng loại

Mỗi sample dưới là **nguyên văn** từ log production (single line, không reformat).

---

### Loại 1 — Timeout awaiting response (EVAL, `SetNotExistsNew`)

Phổ biến nhất (~50%). Xảy ra ở `CrmOrderService.CreateOrder:178` — check trùng đơn.

**Ngày:** 2026-04-10 13:49:47.107

```
:Timeout awaiting response (outbound=5KiB, inbound=0KiB, 5005ms elapsed, timeout is 5000ms), command=EVAL, next: GET DeliveryConnect_Production:DELIVERYPROPERTY:VTP, inst: 0, qu: 0, qs: 49, aw: False, bw: SpinningDown, rs: ReadAsync, ws: Idle, in: 230092, in-pipe: 0, out-pipe: 292, last-in: 35, cur-in: 0, sync-ops: 17167595, async-ops: 875163, serverEndpoint: 10.0.63.32:6379, conn-sec: 2686427.5, aoc: 1, mc: 1/1/0, mgr: 9 of 10 available, clientName: 2bf48001ee97(SE.Redis-v2.7.10.12442), IOCP: (Busy=0,Free=1000,Min=4,Max=1000), WORKER: (Busy=35,Free=32732,Min=4,Max=32767), POOL: (Threads=35,QueuedItems=127,CompletedItems=432129935,Timers=103), v: 2.7.10.12442 (Please take a look at this article for some common client-side issues that can cause timeouts: https://stackexchange.github.io/StackExchange.Redis/Timeouts)|   at StackExchange.Redis.RedisDatabase.ScriptEvaluateAsync(String script, RedisKey[] keys, RedisValue[] values, CommandFlags flags) in /_/src/StackExchange.Redis/RedisDatabase.cs:line 1551    at DeliveryConnect.Service.Generated.RedisCache.RedisCacheService.SetNotExistsNew(String key, String value, Int32 cacheTimeInSeconds) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\RedisCache\RedisCacheService.cs:line 267    at DeliveryConnect.API.Services.CrmOrderService.CreateOrder(ReqCreateOrder reqData) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\Crm\CrmOrderService.cs:line 178    at DeliveryConnect.API.Consumer.CreateOrderConsumer.Consume(ConsumeContext`1 context) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.API\Consumer\CreateOrderConsumer.cs:line 53|
```

**Đọc:** `qs:49` (49 lệnh chờ response), `WORKER Busy=35=Threads` (pool đầy), `QueuedItems=127` (work backlog), `mc:1/1/0` (1 connection), `in:230KB` (data response Redis chờ thread đọc).

---

### Loại 2 — Timeout performing UNLINK (đã gửi, chờ response)

~30%. Xảy ra ở `CrmOrderService.CreateOrder:402` — xóa cache `CreateOrderDuplicate` ở label `end:`.

**Ngày:** 2026-04-13 08:23:26.987 — đơn `DHLOG2604086VB5V001SB` (VTP)

```
:Timeout performing UNLINK (5000ms), next: SETEX DeliveryConnect_Production:CONFIGDELIVERY:VNPOST_API_URL, inst: 66, qu: 0, qs: 66, aw: False, bw: SpinningDown, rs: ReadAsync, ws: Idle, in: 307851, in-pipe: 0, out-pipe: 0, last-in: 2, cur-in: 0, sync-ops: 18713702, async-ops: 951034, serverEndpoint: 10.0.63.32:6379, conn-sec: 2926047.38, aoc: 1, mc: 1/1/0, mgr: 10 of 10 available, clientName: 2bf48001ee97(SE.Redis-v2.7.10.12442), IOCP: (Busy=0,Free=1000,Min=4,Max=1000), WORKER: (Busy=66,Free=32701,Min=4,Max=32767), POOL: (Threads=66,QueuedItems=247,CompletedItems=471011086,Timers=193), v: 2.7.10.12442 (Please take a look at this article for some common client-side issues that can cause timeouts: https://stackexchange.github.io/StackExchange.Redis/Timeouts)|   at StackExchange.Redis.ConnectionMultiplexer.ExecuteSyncImpl[T](Message message, ResultProcessor`1 processor, ServerEndPoint server, T defaultValue) in /_/src/StackExchange.Redis/ConnectionMultiplexer.cs:line 2108    at StackExchange.Redis.RedisDatabase.KeyDelete(RedisKey key, CommandFlags flags) in /_/src/StackExchange.Redis/RedisDatabase.cs:line 739    at DeliveryConnect.Service.Generated.RedisCache.RedisCacheService.Remove(String key) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\RedisCache\RedisCacheService.cs:line 190    at DeliveryConnect.Service.Generated.CacheCommon.DelCache(String key) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\Common\CacheCommon.cs:line 58    at DeliveryConnect.API.Services.CrmOrderService.CreateOrder(ReqCreateOrder reqData) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\Crm\CrmOrderService.cs:line 402    at DeliveryConnect.API.Consumer.CreateOrderConsumer.Consume(ConsumeContext`1 context) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.API\Consumer\CreateOrderConsumer.cs:line 53|
```

**Đọc:**
- Stack `ExecuteSyncImpl` → `KeyDelete` (sync) → root cause `Ping().Result` + sync `KeyDelete` ở `RedisCacheService.Remove:190`
- `inst:66` — 66 lệnh mới đến trong tick này (load tăng đột biến)
- `WORKER Busy=66=Threads, QueuedItems=247` — pool full, backlog dày
- `in:307KB` — Redis đang gửi reply, không thread đọc

---

### Loại 3 — Message timed out in the backlog (CHƯA gửi)

~15%. Cùng caller như Loại 2 nhưng cơ chế **khác hẳn** — lệnh kẹt outbound queue, chưa bao giờ được gửi đi.

**Ngày:** 2026-04-11 14:18:06.853 — đơn `AVN001004177` (GHN)

```
:The message timed out in the backlog attempting to send because no connection became available, command=UNLINK, timeout: 5000, outbound: 0KiB, inbound: 168KiB, inst: 253, qu: 41, qs: 0, aw: True, bw: CheckingForTimeoutComplete, rs: ReadAsync, ws: Idle, in: 0, in-pipe: 0, out-pipe: 0, last-in: 6, cur-in: 0, sync-ops: 17874178, async-ops: 910950, serverEndpoint: 10.0.63.32:6379, conn-sec: 2774527.24, aoc: 1, mc: 1/1/0, mgr: 10 of 10 available, clientName: 2bf48001ee97(SE.Redis-v2.7.10.12442), IOCP: (Busy=0,Free=1000,Min=4,Max=1000), WORKER: (Busy=41,Free=32726,Min=4,Max=32767), POOL: (Threads=52,QueuedItems=153,CompletedItems=447814280,Timers=106), v: 2.7.10.12442 (Please take a look at this article for some common client-side issues that can cause timeouts: https://stackexchange.github.io/StackExchange.Redis/Timeouts)|   at StackExchange.Redis.ConnectionMultiplexer.ExecuteSyncImpl[T](Message message, ResultProcessor`1 processor, ServerEndPoint server, T defaultValue) in /_/src/StackExchange.Redis/ConnectionMultiplexer.cs:line 2108    at StackExchange.Redis.RedisDatabase.KeyDelete(RedisKey key, CommandFlags flags) in /_/src/StackExchange.Redis/RedisDatabase.cs:line 739    at DeliveryConnect.Service.Generated.RedisCache.RedisCacheService.Remove(String key) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\RedisCache\RedisCacheService.cs:line 190    at DeliveryConnect.Service.Generated.CacheCommon.DelCache(String key) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\Common\CacheCommon.cs:line 58    at DeliveryConnect.API.Services.CrmOrderService.CreateOrder(ReqCreateOrder reqData) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\Crm\CrmOrderService.cs:line 402    at DeliveryConnect.API.Consumer.CreateOrderConsumer.Consume(ConsumeContext`1 context) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.API\Consumer\CreateOrderConsumer.cs:line 53|
```

**Đọc:**
- `outbound: 0KiB` — lệnh **chưa từng** được gửi đi
- `qu: 41` (41 lệnh kẹt outbound queue) + `qs: 0` — đợi connection rảnh
- `aw: True, bw: CheckingForTimeoutComplete` — writer đang dọn dẹp timeout
- Pattern này thường xảy ra khi connection broken/reconnecting hoặc writer thread không quay vòng kịp

---

### Loại 4 — HttpClient operation canceled (gọi API hãng vận chuyển)

~5%. **Không phải Redis** — gọi API GHN/JNT/VTP/VNPOST/BEST. Trong stack có thể đi kèm thread starvation gián tiếp từ Redis chain.

```
:The operation was canceled.|   at System.Net.Http.HttpClient.HandleFailure(Exception e, Boolean telemetryStarted, HttpResponseMessage response, CancellationTokenSource cts, CancellationToken cancellationToken, CancellationTokenSource pendingRequestsCts)    at System.Net.Http.HttpClient.<SendAsync>g__Core|83_0(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationTokenSource cts, Boolean disposeCts, CancellationTokenSource pendingRequestsCts, CancellationToken originalCancellationToken)    at DeliveryConnect.Service.ClientHelper.ApiPost[T](String url, Object content, Dictionary`2 lstHeader, Int32 timeoutMilliseconds, String contentType) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\Helpers\ClientHelper.cs:line 65|System.IO.IOException: Unable to read data from the transport connection: Operation canceled.  ---> System.Net.Sockets.SocketException (125): Operation canceled    --- End of inner exception stack trace ---    at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.System.Threading.Tasks.Sources.IValueTaskSource<System.Int32>.GetResult(Int16 token)    at System.Net.Http.HttpConnection.<CheckUsabilityOnScavenge>g__ReadAheadWithZeroByteReadAsync|45_0()    at System.Net.Http.HttpConnection.SendAsyncCore(HttpRequestMessage request, Boolean async, CancellationToken cancellationToken)
```

**Đọc:**
- `SocketException (125)` = ECANCELED (Linux)
- Stack có `CheckUsabilityOnScavenge` → connection bị scavenge bởi `PooledConnectionLifetime: 5 phút` đang được test
- Có thể đến từ:
  - CTS timeout của caller (`timeoutMilliseconds`)
  - PooledConnectionLifetime scavenge connection
  - Thread block kéo theo HTTP timeout

---

### Case đặc biệt — Server-side stall (rare, sustained)

~5%. Sustained 15-20 phút (vd cụm 2026-04-13 08:23, 2026-06-03 07:41-08:01). Đặc trưng `last-in` rất cao + `in:` buffer đầy 1MB.

**Ngày:** 2026-04-13 08:23:45.980 — cụm 5 EVAL timeout trong vài ms

```
:Timeout awaiting response (outbound=30KiB, inbound=66KiB, 8845ms elapsed, timeout is 5000ms), command=EVAL, next: EVAL, inst: 0, qu: 0, qs: 162, aw: False, bw: SpinningDown, rs: ReadAsync, ws: Idle, in: 1017385, in-pipe: 0, out-pipe: 0, last-in: 16929, cur-in: 0, sync-ops: 18714239, async-ops: 951153, serverEndpoint: 10.0.63.32:6379, conn-sec: 2926066.37, aoc: 1, mc: 1/1/0, mgr: 10 of 10 available, clientName: 2bf48001ee97(SE.Redis-v2.7.10.12442), IOCP: (Busy=0,Free=1000,Min=4,Max=1000), WORKER: (Busy=81,Free=32686,Min=4,Max=32767), POOL: (Threads=99,QueuedItems=281,CompletedItems=471013312,Timers=225), v: 2.7.10.12442 (Please take a look at this article for some common client-side issues that can cause timeouts: https://stackexchange.github.io/StackExchange.Redis/Timeouts)|   at StackExchange.Redis.RedisDatabase.ScriptEvaluateAsync(String script, RedisKey[] keys, RedisValue[] values, CommandFlags flags) in /_/src/StackExchange.Redis/RedisDatabase.cs:line 1551    at DeliveryConnect.Service.Generated.RedisCache.RedisCacheService.SetNotExistsNew(String key, String value, Int32 cacheTimeInSeconds) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\RedisCache\RedisCacheService.cs:line 267    at DeliveryConnect.API.Services.CrmOrderService.CreateOrder(ReqCreateOrder reqData) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.Service\Generated\Crm\CrmOrderService.cs:line 178    at DeliveryConnect.API.Consumer.CreateOrderConsumer.Consume(ConsumeContext`1 context) in D:\PROJECT\Git\_SourceForBuild\build_delivery_connect\DeliveryConnect.API\Consumer\CreateOrderConsumer.cs:line 53|
```

**Đọc:**
- `elapsed: 8845ms` (cao hơn timeout 5s — heartbeat phát hiện chậm vì pool đã chết)
- `last-in: 16929ms` — **17 giây không nhận byte nào từ Redis!**
- `in: 1017385` — sau khi Redis dậy, **~1MB response dồn về cùng lúc**, thread starve không đọc kịp
- `qs: 162` (162 lệnh kẹt chờ response)
- `Threads=99, QueuedItems=281` — pool đã grow lên 99 threads, 281 work còn xếp hàng

**Nguyên nhân khả nghi (cần verify Phase 3):**
- Redis BGSAVE / RDB fork (dataset lớn → fork process block server vài giây)
- AOF rewrite
- Lua script runaway của client khác
- Network glitch giữa app container và Redis (10.0.63.32)

---

### Quan sát quan trọng từ `conn-sec`

| Container | conn-sec | Tương đương |
|---|---|---|
| `2bf48001ee97` | 2,686,427s | ~31 ngày |
| `2bf48001ee97` | 2,774,527s | ~32 ngày |
| `2bf48001ee97` | 2,926,047s | ~33.8 ngày |

App chạy liên tục 31+ ngày không restart. **Loại bỏ giả thuyết "burst sáng do cold start + Job chạy ngay khi up"**.

---

---

## 2. Phân loại 4 loại lỗi quan sát được

| # | Loại | Command | Mechanism | Stack | % |
|---|---|---|---|---|---|
| 1 | `Timeout awaiting response` | EVAL | Đã gửi, chờ Redis trả lời quá 5s | `SetNotExistsNew` → `CreateOrder:178` | ~50% |
| 2 | `Timeout performing UNLINK` | UNLINK | Đã gửi, chờ Redis trả lời quá 5s | `Remove` → `DelCache` → `CreateOrder:402` | ~30% |
| 3 | `Message timed out in the backlog` | UNLINK | **Chưa gửi đi**, kẹt outbound queue | `Remove` → `DelCache` → `CreateOrder:402` | ~15% |
| 4 | `HttpClient operation canceled` | HTTP POST | Socket cancel khi gọi API hãng vận chuyển | `ClientHelper.ApiPost:65` | ~5% |

3 loại đầu là Redis. Loại 4 là HttpClient nhưng cùng root cause (thread starvation gián tiếp).

---

## 3. Phân tích pattern theo thời gian

Dataset 5 tuần (2026-04-10 → 2026-06-05), ~340 events. Phân bố theo giờ:

```
07:xx  ████████████████████████  ~50
08:xx  ████████████████████████████████████████  ~110
09:xx  ███████████████████████████████  ~85
10:xx  ████████  ~18
11:xx  ████  ~10
12:xx          0   ← nghỉ trưa, KHÔNG có lỗi
13:xx  ████████  ~22
14:xx  ████████  ~22
15:xx  ██  ~6
16:xx+         0   ← hết giờ
```

**Đặc trưng:**
- Hai đỉnh giờ làm việc (8h-10h, 13h-15h)
- 12h-13h trắng hoàn toàn → vấn đề bám traffic, **không phải cron**
- Sau 16h không có lỗi

**Hai dạng cụm:**
- **Dạng A (phổ biến):** 8-10 lỗi rớt trong dưới 20ms → 1 connection bị stall, cả pipeline ăn timeout
- **Dạng B (hiếm, sustained):** 60+ lỗi trong 20 phút (vd 2026-06-03 07:41-08:01, 2026-04-13 08:23) → server-side stall

**Quan trọng:** `conn-sec: 2,686,427s` (~31 ngày) trong các log cùng container → app KHÔNG restart trong tháng đó. Loại bỏ giả thuyết "burst do cold start".

---

## 4. Root cause map

```
              ┌─────────────────────────────────────────┐
              │  ROOT 1: ThreadPool Min=4 + grow chậm   │
              │  ROOT 2: Sync-over-async Redis          │
              │  ROOT 3: 1 ConnectionMultiplexer (mc:1/1/0) │
              └────────────────────┬────────────────────┘
                                   │
        ┌───────────┬──────────────┼──────────────┬──────────┐
        ▼           ▼              ▼              ▼          ▼
    Type 1       Type 2         Type 3         Type 4      Bão B
    EVAL         UNLINK reply   UNLINK send    HTTP        sustained
    50%          30%            15%            5%          5%
                                                            │
                                          ┌─────────────────┴─────┐
                                          │ Server-side stall:    │
                                          │ RDB/AOF/Lua/Network   │
                                          └───────────────────────┘
```

### Bằng chứng cụ thể trong code

**A. Sync-over-async** — `RedisCacheService.cs` (bản gốc):
- Line 190 (`Remove`): `if (!Ping().Result) return;` — block thread chờ async PingAsync + extra round-trip
- Line 215 (`Set`): tương tự
- Line 332 (`GetOrSet`): tương tự
- 16 caller `DelCache` (qua `Remove`) trong hot path `CreateOrder`

**B. ThreadPool Min=4** — `Program.cs` (bản gốc):
- Không có `ThreadPool.SetMinThreads`
- Default min = ProcessorCount → khi peak, pool grow ~1 thread/0.5s → request timeout trước khi có thread

**C. 1 multiplexer** — `RedisCacheService.cs` (bản gốc):
- `private static IConnectionMultiplexer _connMultiplexer;` — singleton 1 connection
- `mc: 1/1/0` trong mọi log timeout → confirm
- Burst dạng A (8-10 lệnh trong 20ms) chính là head-of-line trên 1 socket

### Nguyên nhân phụ

**D. Job quét DEL prefix mỗi giờ** — `JobSyncAddressPs.cs`:
- Timer mỗi 1 giờ (`TimeSpan.FromHours(1)`)
- Gọi `DelCacheByPrefix("ADDRESS")` + `DelCacheByPrefix("ADDRESSDELIVERY")`
- `RemoveByPrefix` dùng `server.Keys()` + sync `KeyDelete` từng key
- `DoWork`: `async void` + `.GetAwaiter().GetResult()` → block Timer thread
- Đóng góp ~5-10% timeout vào đỉnh giờ

**E. N+1 GET config** — Trong `next:` của log timeout:
```
next: GET CONFIGDELIVERY:JNT_API_URL
next: GET CONFIGDELIVERY:VTP
next: GET DELIVERYPROPERTY:VTP
next: GET CONFIGDELIVERY:BEST_API_URL_PRICE
```
Mỗi `CreateOrder` đọc 5-10 config key → pipeline dày → dễ head-of-line

**F. Bug đơn vị thời gian** — `RedisCacheService.SetNotExists` line 248:
```csharp
var expiresIn = TimeSpan.FromMilliseconds(cacheTimeInSeconds);  // SAI: param tên seconds, dùng FromMilliseconds
```
Không gây timeout nhưng correctness bug — TTL thực 60ms thay vì 60s.

---

## 5. Hệ quả nghiệp vụ của UNLINK fail

Mỗi UNLINK fail tại `CreateOrder:402` (label `end:`) → key `CreateOrderDuplicate:{ShippingUnitCode}_{OrderCode}` còn nguyên trong Redis với TTL 60s. Trong 60s đó:

- Nếu CRM retry cùng đơn → `SetNotExistsNew` returns false
- `CreateOrder` báo: **"Đơn hàng đang xử lý bởi 1 tiến trình khác. Nếu cần hãy thao tác lại sau 60s."**
- → Đơn báo **trùng giả** dù không có tiến trình nào đang xử lý

Ước tính ~70-100 đơn báo trùng giả mỗi tháng.

---

## 6. Patches đã áp dụng — Phase 1 (2026-06-05)

### Patch 1.1 — ThreadPool min threads

**File:** `DeliveryConnect.API/Program.cs`

Thêm trước `WebApplication.CreateBuilder`:
```csharp
System.Threading.ThreadPool.SetMinThreads(workerThreads: 200, completionPortThreads: 200);
```

**Tại sao 200:** Log peak quan sát `WORKER Busy=108`, đặt 200 để cover peak + buffer.

### Patch 1.2 — Pool 4 ConnectionMultiplexer

**File:** `DeliveryConnect.Service/Generated/RedisCache/RedisCacheService.cs`

Thay đổi:
- `_connMultiplexer` static field → `_connPool[]` array 4 multiplexer
- `_db` instance field → property `Db` round-robin pick mỗi call
- `NextConn()` dùng `Interlocked.Increment` để round-robin atomic
- Bỏ anti-pattern `!IsConnected` re-Connect (multiplexer tự reconnect)
- `AbortOnConnectFail = false` để không crash khi Redis tạm down
- `Ping()` và `GetServers()` cũng dùng `NextConn()`
- `GetConnectionRedisMultiplexer()` giữ signature → backward compat

Verify: `dotnet build` 0 errors, 1590 pre-existing XML warnings.

### Kỳ vọng Phase 1

| Loại lỗi | % giảm |
|---|---|
| Type 1 EVAL | ~70-80% |
| Type 2 UNLINK timeout | ~65-75% |
| Type 3 UNLINK backlog | ~70-80% |
| Type 4 HTTP cancel | ~30-40% (gián tiếp) |

Tổng kỳ vọng **-60-70%** timeout/tháng.

---

## 7. Còn lại — Phase 2 đến 5

### Phase 2 (next) — Bỏ sync-over-async
- Bỏ `Ping().Result` trong `Remove`, `Set`, `GetOrSet`
- `Remove` chuyển sang `KeyDeleteAsync` fire-and-forget (cache fail không critical, TTL handle)
- `Set` chuyển sang `StringSetAsync` fire-and-forget
- `SetNotExists` fix `FromMilliseconds` → `FromSeconds`
- `JobSyncAddressPs` sửa async/await pattern, không `GetAwaiter().GetResult()`
- `RemoveByPrefix` async + batch + `UNLINK` (multi-key)

### Phase 3 — Verify server-side
Khi vẫn còn `last-in > 3s` trong log sau Phase 1+2:
```bash
redis-cli -h 10.0.63.32 CONFIG GET save        # persistence policy
redis-cli -h 10.0.63.32 CONFIG GET appendonly  # AOF
redis-cli -h 10.0.63.32 INFO persistence       # last RDB/AOF time
redis-cli -h 10.0.63.32 SLOWLOG GET 100        # commands chậm
redis-cli -h 10.0.63.32 INFO commandstats      # peak EVAL/UNLINK rate
```
Nếu RDB fork là thủ phạm → đổi sang AOF everysec, hoặc dùng replica để snapshot.

### Phase 4 — Read pattern
- Gộp `CONFIGDELIVERY:{HÃNG}_*` thành 1 `HASH CONFIGDELIVERY:{HÃNG}` → 1 round-trip
- Cache config in-memory (LRU 1-5 phút) thay vì đập Redis mỗi `CreateOrder`

### Phase 5 — Architectural
- Cache versioning: bump version key thay vì quét DEL → loại bỏ `RemoveByPrefix` hot path
- Hoặc dùng Redis `UNLINK` (async server-side) thay `DEL` cho mass-delete

---

## 8. Verify deploy

Sau khi deploy Phase 1, monitor 1-2 ngày:

| Metric | Trước (baseline) | Sau (kỳ vọng) |
|---|---|---|
| Timeout/tháng (Type 1) | ~170 | <50 |
| Timeout/tháng (Type 2+3) | ~150 | <50 |
| `WORKER Busy` peak (trong log timeout còn lại) | 108 | <40 |
| Cụm burst Dạng A (8-10 lệnh trong 20ms) | thường xuyên | hiếm, ≤2-3 lệnh |
| Đơn báo "trùng giả" CRM | ~70-100/tháng | <20/tháng |

Nếu vẫn có timeout với `last-in > 3000ms`:
- Nguyên nhân server-side (RDB fork / AOF / Lua / network) — đi Phase 3

---

## 9. Phương pháp đọc SE.Redis timeout message

Lưu lại để team tự diagnose lần sau. Các field quan trọng nhất:

| Field | Ý nghĩa | Cảnh báo |
|---|---|---|
| `sync-ops` vs `async-ops` | Ratio sync/async | Sync >> async → sync-over-async anti-pattern |
| `qu:` | Outbound queue (chưa gửi) | >0 → backlog gửi |
| `qs:` | Sent queue (chờ response) | >50 → head-of-line trên connection |
| `in:` | Buffer TCP receive | >100KB → thread starvation, không đọc kịp |
| `last-in:` (ms) | Lâu rồi không nhận data | >3000ms → server-side stall |
| `WORKER Busy` vs `Min` | Threadpool stress | Busy ≈ Threads → pool đã full grow |
| `POOL QueuedItems` | Work item chờ thread | >100 → continuation backlog |
| `mc:` | Multiplexer connections | `1/1/0` → single connection, dễ head-of-line |
| `aoc:` | Async ops completed trong sync wait | >5 → sync-over-async đang xảy ra |
| `aw:` `bw:` | Active writer / backlog writer | `True/CheckingForTimeout` → writer thread đang dọn |

Ví dụ đọc:
```
WORKER: Busy=108, Min=4 → starvation cứng
qs: 66, in: 240KB     → 240KB reply chờ đọc, 66 lệnh chờ response
last-in: 3231         → 3s không có data dù Redis vẫn đang gửi
```
→ **ThreadPool dead, không phải Redis dead**.

```
WORKER: Busy=8, Min=4
qs: 24, in: 0
last-in: 268
```
→ Threadpool ok, **head-of-line block trên 1 connection** (1 lệnh nặng chặn 24 lệnh khác).

```
qs: 162, in: 1MB, last-in: 16929
```
→ **Server-side stall** ~17s. Check Redis RDB/AOF/Lua.

---

## 10. References

- Stack trace gốc: `CreateOrderConsumer.Consume` → `CrmOrderService.CreateOrder` (line 178 EVAL, line 402 UNLINK)
- Files chính:
  - `DeliveryConnect.Service/Generated/RedisCache/RedisCacheService.cs`
  - `DeliveryConnect.Service/Generated/Common/CacheCommon.cs`
  - `DeliveryConnect.Service/Generated/Schedule/JobSyncAddressPs.cs`
  - `DeliveryConnect.Service/Generated/SyncAddress/SyncAddressService.cs`
  - `DeliveryConnect.API/Program.cs`
  - `DeliveryConnect.API/Consumer/CreateOrderConsumer.cs`
- SE.Redis timeout doc: https://stackexchange.github.io/StackExchange.Redis/Timeouts
