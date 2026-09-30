---
controller: SmsSendController
module: marketing
service: SMS
man_hinh_ui: "Marketing > SMS/Zalo > Gửi tin (soạn & gửi chiến dịch SMS/Zalo); báo cáo danh sách tin đã gửi; Import cập nhật giá tin. Endpoint Receive/System là webhook/nội bộ — không có UI."
muc_dich: "Cửa gửi SMS/Zalo: nhận yêu cầu gửi từ UI/hệ thống rồi PUBLISH message lên RabbitMQ cho consumer xử lý (không gửi đồng bộ); nhận callback kết quả gửi từ ESMS; quản lý brandname/ZOA và import giá."
alias: ["SmsSend", "gửi sms", "gửi zalo", "gửi tin", "soạn tin sms", "campaign sms", "brandname", "ZOA", "callback esms", "import giá sms"]
auth: mixed
state_machine: []
deprecated: false
entities: [SmsSend, AccountSms, CauHinhSms]
stored_procs: []
depends:
  - "ISmsSendService (SMS.Services.Generated) — query danh sách, ReceiveData_Query, brandname/ZOA, import giá"
  - "ICauHinhService.GetAsync(idDonVi) — đọc cờ SuDungCauHinhNangCao để rẽ V1/V2"
  - "RabbitMQ qua IPublishEndpoint (MassTransit) — publish các *ConsumerDto cho SMS consumer gửi thật"
  - "MaTienIchSCRM.TI_SMS — tiện ích gate controller; CurrentUser.ListTienIch_BE kiểm tra hạn dùng"
  - "ESMS (đối tác gửi SMS/Zalo) — callback ReceiveDataQuery/ReceiveDataQueryZalo"
cross_module_in:
  - "[internal] Các service hệ thống gọi SaveSendSmsSystem / SaveSendMessageSystemV2 / SaveSendZaloSystem (PrivateSBAuthorize) để gửi tin tự động (thông báo đơn hàng, OTP, tác nghiệp)"
  - "[anonymous] ESMS callback ReceiveDataQuery / ReceiveDataQueryZalo cập nhật kết quả gửi"
cross_module_out:
  - "RabbitMQ SMS consumer (SendSms/SendZalo/SendMessageV2/...ConsumerDto) — nơi gửi thật tới ESMS + trừ tiền AccountSms"
  - "pushsale — ReceiveData_Query đẩy kết quả gửi (xem comment: 'send data to pushsale by IdServer')"
---

# SmsSendController — Gửi tin SMS/Zalo (qua consumer RabbitMQ)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách tin đã gửi | Bảng SMS/Zalo | POST `api/SmsSend/TimTheoDieuKien` | (chỉ `[SBAuthorize]`, không key) | public | — | Lọc danh sách `SmsSend` theo filter. | EF (`ISmsSendService.TimTheoDieuKienAsync`) | — |
| 2 | Gửi tin (user) | Gửi | POST `api/SmsSend/SaveSendSms` · `SaveSendZaloSms` · `SaveSendMessageV2` · `SendSmsByIdPatternSms` · `SendZaloByIdPatternSms` | (chỉ `[SBAuthorize]`) | public | Đơn vị có `TI_SMS` còn hạn (check trong code) | Đọc `CauHinhSms` của đơn vị → nếu `SuDungCauHinhNangCao=true` publish **SendMessageV2ConsumerDto** (gộp SMS+Zalo), ngược lại publish **SendSmsConsumerDto** / **SendZaloConsumerDto** (v1). Luôn trả "Thành công" ngay (gửi bất đồng bộ). | RabbitMQ publish | **Nuốt lỗi:** try/catch rỗng → publish fail vẫn trả "Thành công" (SmsSendController.cs:112-116, 181-185, 235-240). |
| 3 | Lấy brandname / ZOA | Chọn brandname / ZOA | GET `api/SmsSend/GetListBrandnameSms{,Default}` · `GetListZOASms{,Default}` | (chỉ `[SBAuthorize]`) | public | — | Lấy danh sách brandname / Zalo OA (mặc định hệ thống hoặc theo apikey/secretkey đơn vị) từ ESMS. | EF/HTTP service | apikey/secretkey truyền qua query GET (nhạy cảm) |
| 4 | Import cập nhật giá tin | Import giá | POST `api/SmsSend/ImportUpdatePrice` (FromForm + file) | (key đã comment) `[SBAuthorize]` + check role thủ công | public | Chỉ `IsSuperUser` hoặc role `HT-KT` (check trong code) | Parse Excel cập nhật bảng giá tin; ai không đủ quyền trả lỗi "Bạn không có quyền". | EF | Quyền check thủ công thay vì `[SBAuthorize(key)]` (SmsSendController.cs:372-375); key gốc bị comment. |
| 5 | Tải template / file lỗi import | Tải mẫu / Tải dòng lỗi | GET `api/SmsSend/DownloadFileTemplateImportUpdatePrice` · `DownloadDataWrongImportUpdatePrice/{id}` | (chỉ `[SBAuthorize]`) | public | — | Trả file `.xlsx` (GemBox) mẫu import / các dòng import sai. | EF → Excel stream | — |

## Endpoint nội bộ (private / service-to-service)
- **[internal] Gửi tin hệ thống (PrivateSBAuthorize):** `SaveSendSmsSystem`, `SaveSendZaloSystem`, `SaveSendMessageSystemV2` — cùng pattern rẽ V1/V2 theo `SuDungCauHinhNangCao`, publish consumer KHÔNG kèm `UserLogin`. Dùng cho thông báo tự động (đơn hàng, tác nghiệp, OTP) do service khác gọi sang.
- **[anonymous] Callback ESMS:** `ReceiveDataQuery` (SMS) & `ReceiveDataQueryZalo` (Zalo) — GET, ESMS gọi về với `RequestId` (Guid), `SMSID`, số gửi thành công/thất bại, `TotalPrice` → `ISmsSendService.ReceiveData_Query[_Zalo]` cập nhật trạng thái gửi + (theo comment) đẩy sang pushsale theo IdServer.

## Quy tắc nghiệp vụ (gating / rẽ nhánh V1 ↔ V2)
1. **Chọn cấu hình:** đọc `cauHinhService.GetAsync(input.idDonVi)`.
   - `SuDungCauHinhNangCao == true` → nhánh **V2**: publish `SendMessageV2ConsumerDto` (1 message gộp SMS+Zalo). LƯU Ý: ở V2, `SaveSendZaloSms`/`SaveSendZaloSystem` **không publish gì** (comment: "đã xử lý gộp trong api SMS").
   - ngược lại → nhánh **V1**: publish riêng `SendSmsConsumerDto` / `SendZaloConsumerDto` (hoặc *System variant).
2. **Gate tiện ích (chỉ ở endpoint user):** nếu `CurrentUser.ListTienIch_BE` có item `MaTienIch == TI_SMS && NgayHetHan >= now` → publish; **nếu list rỗng/null → vẫn publish** (comment: "không lấy được tiện ích thì cứ gửi vào xử lý sau"). Endpoint *System (private) bỏ qua check này.

## Quan hệ
- **Gọi sang:** `ISmsSendService`, `ICauHinhService`; publish RabbitMQ (`IPublishEndpoint`).
- **Dữ liệu (entities):** `SmsSend` (log tin), `AccountSms` (tài khoản trừ tiền — do consumer xử lý), `CauHinhSms` (cờ nâng cao).
- **Ripple / đồng bộ:** việc gửi thật + trừ tiền nằm ở **consumer** (không ở controller); callback ESMS quay lại cập nhật kết quả; kết quả còn đẩy sang pushsale.

## Luồng xuyên module (gửi SMS end-to-end)
1. UI soạn tin → `SaveSendSms`/`SaveSendMessageV2` → controller đọc cấu hình đơn vị → publish *ConsumerDto lên RabbitMQ → trả "Thành công" ngay.
2. SMS consumer nhận message → gọi ESMS gửi thật → trừ tiền `AccountSms` → ghi log `SmsSend`.
3. ESMS xử lý xong → callback `api/SmsSend/ReceiveDataQuery[Zalo]` → `ReceiveData_Query` cập nhật số gửi thành công/thất bại + `TotalPrice`, đẩy kết quả sang pushsale.

## Lỗi đã biết / lưu ý kỹ thuật
- **try/catch nuốt lỗi toàn bộ endpoint gửi** — `SmsSendController.cs:112-116,146-149,181-185,203-206,235-240,266-269,433-436,453-456`: mọi exception (kể cả publish RabbitMQ fail) bị nuốt, luôn trả "Thành công" → UI báo thành công dù tin không vào queue.
- **`ImportUpdatePrice` check quyền thủ công** (`cs:372-375`) thay vì attribute; key `SmsSendController_ImportUpdatePrice` bị comment (`cs:367`).
- **V2 bỏ qua endpoint Zalo riêng** — `SaveSendZaloSms`/`SaveSendZaloSystem` khi `SuDungCauHinhNangCao=true` không publish (`cs:222-224,253-255`); phải đảm bảo client V2 chỉ gọi `SaveSendMessageV2`, tránh mất tin Zalo nếu gọi nhầm endpoint v1.
- **Callback là `[AllowAnonymous]` GET** với apikey/số tiền trên query — không xác thực nguồn gọi (`cs:297-330`).
- `ReceiveData_Query` parse `Guid.Parse(RequestId)` không bọc try → callback sai format RequestId sẽ ném 500.
