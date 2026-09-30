# Dev — Gửi SMS & Zalo OA

> Service: **SMS** (BE .NET) · module FE Marketing. Gửi tin **bất đồng bộ qua RabbitMQ** (controller chỉ publish, consumer gửi thật). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Mục đích |
|---|---|
| Gửi tin SMS/Zalo | soạn & gửi chiến dịch SMS/Zalo; báo cáo danh sách tin đã gửi |
| Mẫu SMS/Zalo | CRUD mẫu, đổi trạng thái, đồng bộ mẫu duyệt từ ESMS |
| Quản lý tài khoản SMS (Admin/CSKH) | CRUD tài khoản, nạp tiền, lịch sử nạp |

> Route FE cụ thể chưa trace trong registry — cần product/dev xác nhận khi cần.

## Endpoint / Controller (trace registry)

**SmsSendController** (`module marketing` · service `SMS`) — cửa gửi tin, publish RabbitMQ:
| MaTinhNang/Endpoint | Route | Mục đích | Quyền |
|---|---|---|---|
| `TimTheoDieuKien` | POST `api/SmsSend/TimTheoDieuKien` | danh sách tin đã gửi | chỉ `[SBAuthorize]` (không key) |
| `SaveSendSms` / `SaveSendZaloSms` / `SaveSendMessageV2` | POST `api/SmsSend/...` | gửi tin user (publish *ConsumerDto) | chỉ `[SBAuthorize]` |
| `SendSmsByIdPatternSms` / `SendZaloByIdPatternSms` | POST | gửi theo mẫu | chỉ `[SBAuthorize]` |
| `SaveSendSmsSystem` / `SaveSendZaloSystem` / `SaveSendMessageSystemV2` | POST (private) | gửi tin tự động hệ thống | `[PrivateSBAuthorize]` |
| `ReceiveDataQuery` / `ReceiveDataQueryZalo` | GET | callback ESMS cập nhật kết quả | `[AllowAnonymous]` |
| `GetListBrandnameSms{,Default}` / `GetListZOASms{,Default}` | GET | lấy brandname / Zalo OA | chỉ `[SBAuthorize]` |
| `ImportUpdatePrice` | POST (FromForm + file) | import bảng giá tin | check thủ công: super user hoặc `HT-KT` |
| `DownloadFileTemplateImportUpdatePrice` / `DownloadDataWrongImportUpdatePrice/{id}` | GET | tải mẫu / dòng lỗi import | chỉ `[SBAuthorize]` |

**PatternSmsController** (v1 mẫu tin):
| Endpoint | Route | Quyền (key) |
|---|---|---|
| `TimTheoDieuKien` | POST `api/PatternSms/TimTheoDieuKien` | `PatternSms_TimTheoDieuKien` (HT-AD, DV-AD-TONG, HT-CSKH, HT-KT, DV-QL-MKT, DV-MKT-LEAD, DV-MKT, DV-AD-CN) |
| `TimTheoId/{id}` | GET | `PatternSms_TimTheoId` (HT-AD, DV-AD-TONG, HT-CSKH) |
| `ThemMoi` / `CapNhatThongTin` / `Xoa/{id}` | POST | `PatternSms_ThemMoi` / `_CapNhatThongTin` / `_Xoa` (HT-AD, DV-AD-TONG, HT-CSKH) |
| `ThemMoiMauCoSan` | GET | `PatternSms_ThemMoiMauCoSanAsync` |
| `CapNhatStatusAsync` | POST | `PatternSms_CapNhatStatusAsync` |
| `GetTemplateSmsFromEsms` / `GetTemplateZaloFromEsms` | GET | `PatternSms_GetTemplateSmsFromEsms` / `_GetTemplateZaloFromEsms` |

> Bản nâng cao **PatternSmsV2Controller** (route `api/PatternSmsV2`) — mẫu gắn theo `idAccountSms`, dùng khi đơn vị bật cấu hình nâng cao (`SuDungCauHinhNangCao`). CRUD song hành với v1.

**AccountSmsController** (tài khoản SMS cấp hệ thống, gate `TI_HeThong`):
| Endpoint | Route | Quyền (key) |
|---|---|---|
| `TimTheoDieuKien` | POST `api/AccountSms/TimTheoDieuKien` | `AccountSms_TimTheoDonVi` (HT-AD, HT-CSKH) |
| `TimTheoId/{id}` | GET | `AccountSms_TimTheoId` |
| `ThemMoi` / `CapNhatThongTin` / `Xoa/{id}` | POST | `AccountSms_ThemMoi` / `_CapNhatThongTin` / `_Xoa` (HT-AD, HT-CSKH) |
| `DepositMoneyIntoAccount` | POST | `AccountSms_DepositMoneyIntoAccount` |
| `RechargeHistoryGetByAccountSmsId/{id}` | GET | `AccountSms_RechargeHistoryGetByAccountSmsId` |
| `GetAccountSmsByToken` | GET | chỉ `[SBAuthorize]` |
| `TimTheoListId` | POST | `[AllowAnonymous]` (service batch) |
| `GetDonViSuDungCauHinhSms` / `GetDonViSuDungCauHinhCmsnSms` / `IsUsingSms` | — | `[SBAuthorize]` rỗng / nội bộ |

> Bản theo đơn vị: **AccountSmsAgencyController** (gate `TI_SMS`) và **AccountSmsUnitUsedController** — đừng nhầm với bản hệ thống.

## Entities
- **`SmsSend`** — log tin đã gửi (trạng thái, số thành công/thất bại, tổng tiền).
- **`AccountSms`** — tài khoản gửi tin (apikey, secretkey, brandname/Zalo OA, **số dư**).
- **`RechargeHistory`** — log nạp tiền tài khoản SMS.
- **`PatternSms`** — mẫu tin SMS/Zalo (nội dung + trạng thái duyệt); bản V2 gắn `idAccountSms`.
- **`CauHinhSms`** — cấu hình SMS của đơn vị, cờ `SuDungCauHinhNangCao` quyết định rẽ V1/V2.

## Luồng code
1. UI gọi `SaveSendSms`/`SaveSendMessageV2` → controller đọc `cauHinhService.GetAsync(input.idDonVi)`.
   - `SuDungCauHinhNangCao == true` → publish **`SendMessageV2ConsumerDto`** (gộp SMS+Zalo). Lưu ý: ở V2, `SaveSendZaloSms`/`SaveSendZaloSystem` **không publish** (đã gộp trong message V2).
   - ngược lại → publish **`SendSmsConsumerDto`** / **`SendZaloConsumerDto`** (V1).
2. Gate tiện ích (chỉ endpoint user): nếu `CurrentUser.ListTienIch_BE` có `MaTienIch == TI_SMS && NgayHetHan >= now` → publish; **list rỗng/null → vẫn publish**. Endpoint *System (private) bỏ qua check này.
3. Controller trả "Thành công" ngay (bất đồng bộ).
4. **SMS consumer** (RabbitMQ) nhận message → gọi ESMS gửi thật → trừ tiền `AccountSms` → ghi `SmsSend`.
5. ESMS callback `ReceiveDataQuery[Zalo]` → `ISmsSendService.ReceiveData_Query[_Zalo]` cập nhật kết quả + đẩy sang pushsale.

## Bug-prone / lưu ý
- **try/catch nuốt lỗi toàn bộ endpoint gửi** — `SmsSendController.cs:112-116,146-149,181-185,203-206,235-240,266-269,433-436,453-456`: mọi exception (kể cả publish RabbitMQ fail) bị nuốt, luôn trả "Thành công" → UI báo thành công dù tin **không vào queue**. Đối chiếu bằng `SmsSend` log.
- **V2 bỏ qua endpoint Zalo riêng** — `SaveSendZaloSms`/`SaveSendZaloSystem` khi `SuDungCauHinhNangCao=true` không publish (`cs:222-224,253-255`); client V2 phải gọi `SaveSendMessageV2`, tránh **mất tin Zalo** nếu gọi nhầm endpoint v1.
- **Callback `[AllowAnonymous]` GET** với apikey/số tiền trên query (`cs:297-330`) — không xác thực nguồn gọi (nguy cơ bảo mật).
- **`ReceiveData_Query` parse `Guid.Parse(RequestId)` không bọc try** → callback sai format RequestId ném 500, kết quả tin không cập nhật.
- **`ImportUpdatePrice` check quyền thủ công** (`cs:372-375`) thay vì attribute; key `SmsSendController_ImportUpdatePrice` bị comment (`cs:367`).
- **`AccountSms.TimTheoListId` `[AllowAnonymous]`** (`AccountSmsController.cs:98-103`) — trả AccountSms (gồm apikey/secretkey nhạy cảm) không xác thực.
- **`GetListBrandnameSms`/`GetListZOASms`** — apikey/secretkey truyền qua **query GET** (nhạy cảm).
- **PatternSms quyền mô tả nhầm**: `GetTemplateSmsFromEsms` ghi nhãn "Lấy mẫu zalo từ esms" (`PatternSmsController.cs:106`); V2 `CapNhatThongTinV2` dùng lại key v1 `PatternSms_CapNhatThongTin` (`PatternSmsV2Controller.cs:72`) → khó tách quyền v1/v2.

## File source (path:line)
- `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\SmsSendController.cs` (`:112-116,146-149,181-185,222-224,253-255,297-330,367-375,433-456`).
- `...\AccountSmsController.cs` (`:12` gate TI_HeThong, `:98-103` TimTheoListId anonymous).
- `...\PatternSmsController.cs` (`:106,113`) · `...\PatternSmsV2Controller.cs` (`:72,115,121`).
- Registry: `projects/scrm/registry/marketing/SmsSendController.md`, `AccountSmsController.md`, `PatternSmsController.md`, `PatternSmsV2Controller.md`.
