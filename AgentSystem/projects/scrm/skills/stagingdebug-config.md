# Skill: Config StagingDebug — FE + BE

## Mục đích
Chạy FE local kết nối staging DB/auth, nhưng trỏ một số service về localhost để debug.

---

## FE — `src/environments/environment.stagingdebug.ts`

### 1. Trỏ service cần debug về localhost

Mỗi service có 2 dòng: 1 staging URL (comment), 1 localhost URL (uncomment khi debug).

```ts
// Ví dụ: debug WorkFlow.API (port 50071)
// SERVICE_API_WORK: 'https://stagingapi.sandbox.com.vn/workflow/',
SERVICE_API_WORK: 'http://localhost:50071/',
```

Khi xong debug → đảo lại (comment localhost, bỏ comment staging).

### 2. Đổi `domainDebug` về domain khách đang test

```ts
domainDebug: 'https://kimcucspa.sandbox.com.vn/',   // domain khách hàng cụ thể
// domainDebug: 'https://mrlip2.sandbox.com.vn/',   // domain khác (comment lại)
```

`domainDebug` = subdomain của khách hàng trên staging (`{tenant}.sandbox.com.vn`).  
Mỗi lần debug cho khách khác → đổi dòng này.

### Port map các service hay debug:

| Service | Port |
|---------|------|
| WorkFlow.API | 50071 |
| WorkFlowReceiver.API | 50082 |
| WorkFlowConsumer.API | 50084 |
| OrderLogistic.API | 50024 |

*(Xem launchSettings.json của từng service để lấy port chính xác.)*

### ⚠️ Cross-service khi F5 1 service local (bài học 2026-07, đừng đào lại)
Chỉ chạy service ĐÃ SỬA local, **các service khác (Core/WarehouseProduct/DeliveryConnect...) trỏ STAGING** (JWT chung key staging → hợp lệ). Riêng **DeliveryConnect** hay bị: URL/section config có thể **comment hoặc thiếu** trong `appsettings.StagingDebug.json` → luồng cần nó (vd order-form load `PartnerIsSignPart` giao vận) **fail câm**. DeliveryConnect staging = `http://10.0.161.16:50027` (không chạy local nếu không sửa code nó). Thiếu section → bổ sung trỏ staging.

---

## BE — chạy service với ASPNETCORE_ENVIRONMENT=StagingDebug

> 🔴 **BẮT BUỘC trước khi F5 để debug**: mở `Properties/launchSettings.json` của **MỌI service sẽ F5**, đổi
> `"ASPNETCORE_ENVIRONMENT": "Staging"` → `"StagingDebug"`. **Mặc định phần lớn service để `Staging`, KHÔNG phải `StagingDebug`** → F5 nguyên trạng sẽ chạy env Staging (đầy đủ, **có hút RabbitMQ**), không phải môi trường debug mình muốn.

### Checklist đổi launchSettings sang StagingDebug (mỗi service F5)
1. `{Service}\{Service}.API\Properties\launchSettings.json` → đổi cả 2 profile (`{Service}.API` + `IIS Express`): `Staging` → `StagingDebug`.
2. Có sẵn template trong repo: `DebugLaunchSettings\StagingDebug\{Service}\...\launchSettings.json` (copy đè, hoặc chỉ sửa tay giá trị env). ⚠️ **Không phải service nào cũng có** trong folder template (vd PartnerConnect thiếu) → thiếu thì **sửa tay**.
3. Xác nhận có `appsettings.StagingDebug.json` trong `{Service}.API` (thiếu → F5 lỗi load config).
4. ⚠️ **`StagingDebug` trong `launchSettings.json` KHÔNG phải "hack cần revert".** Đo trên bản HEAD:
   `Development` 193 chỗ · **`StagingDebug` 67 chỗ** · `Staging` 55 chỗ → nó là giá trị **đã commit
   bình thường** cho chạy local, tuỳ service. Đừng bắt người khác trả lại `Staging`, cũng đừng tạo
   diff ồn ào nếu service đó vốn đã là `StagingDebug`. (Đúc kết 2026-07-30, sau khi ghi TODO revert
   sai và bị dev chỉnh: *"debug thì phải chạy staging debug"*.)
   Thứ **thật sự** phải trả lại là những chỗ trỏ về **localhost** hoặc bật thêm hành vi
   (xem mục "Nhãn TODO" dưới).

- StagingDebug: kết nối DB staging (10.0.161.13), **KHÔNG hút RabbitMQ** (consumer tắt message khi isDebug=true).
- Service WorkFlow đã set sẵn `StagingDebug` → F5 luôn; các service khác (WarehouseProduct, OrderLogistic, PartnerConnect...) mặc định `Staging` → **phải tự đổi**.

---

## 🔴 "Multiple Startup Projects" → Visual Studio ẨN dropdown launch profile
> Đúc kết 2026-07-30.

Debug nhiều service cùng lúc thì solution để **Multiple Startup Projects** — lúc đó cạnh nút ▶ chỉ
còn **"Start"** + "Web Browser", **KHÔNG có** dropdown chọn launch profile. Profile chỉ chọn được
khi đặt **MỘT** project làm startup project.

**Hệ quả khi thiết kế cờ bật/tắt cho lúc debug:**
- ❌ **ĐỪNG** làm cờ qua **launch profile riêng** / biến môi trường trong `launchSettings.json` —
  người debug nhiều service sẽ không với tới được.
- ❌ **ĐỪNG tự nghĩ ra cờ mới** (`RUN_JOBS_IN_DEBUG`, `"RunJobsInDebug": true`...). Hệ thống đã có
  khuôn riêng cho từng loại, **đi đọc màn/service khác trước**:
  - Job Quartz → **"đưa job cần chạy lên TRƯỚC `if (IsDebug) return`"**, xem
    [quartz-job.md](quartz-job.md) và `Contact/Contact.API/Service/JobExtention.cs:18`.
  - Consumer RabbitMQ → `if (!IsDebug) { cfg.ReceiveEndpoint(...) }`, xem
    `OrderLogistic/OrderLogisticConsumer.API/Service/MassTransitExtensions.cs:55`.
  - Cần bật/tắt thật sự theo cấu hình → `appsettings.{Env}.json` (file này **cho phép comment `//`**),
    vì nó ăn ở mọi cách chạy.

⚠️ **`launchSettings.json` KHÔNG nhận comment `//`** (không phải cú pháp chính thức, VS có thể báo
"không đọc được launch profile"). Cần ghi chú trong file đó thì dùng **property** —
`"_TODO-TRUOC-KHI-DAY": "..."` — VS bỏ qua key lạ. `appsettings.*.json` thì comment được bình thường.

## 🏷️ Nhãn `TODO-TRUOC-KHI-DAY` — chống quên trả lại config debug
Mọi thay đổi **chỉ dùng ở máy local** phải dán đúng một nhãn này ngay dòng đó:
```ts
// TODO-TRUOC-KHI-DAY: tra moi URL ve stagingapi (bo cac dong localhost)
```
Trước khi đẩy: `rg "TODO-TRUOC-KHI-DAY"` → ra hết trong 1 lệnh, không phải nhớ từng file.
Áp cho: URL localhost trong `environment.stagingdebug.ts` · service URL localhost trong
`appsettings.StagingDebug.json` · job/consumer được đưa lên trước cửa chặn `IsDebug` · `domainDebug`
đổi sang tenant cụ thể.
**KHÔNG** áp cho `ASPNETCORE_ENVIRONMENT: StagingDebug` (giá trị bình thường, xem mục trên).

## Quy trình debug một feature mới (ví dụ: ticketManager)

1. BE: F5 các service cần thiết (`WorkFlow.API`, `WorkFlowReceiver.API`, `WorkFlowConsumer.API`)
2. FE: sửa `environment.stagingdebug.ts` → trỏ đúng service + domain khách
3. FE: `ng serve --configuration=stagingdebug`
4. Xong → revert `environment.stagingdebug.ts` (không commit file này)
