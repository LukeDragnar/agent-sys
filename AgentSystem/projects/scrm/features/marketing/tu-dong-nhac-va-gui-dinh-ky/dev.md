# Dev — Tác vụ tự động nhắc & gửi tin định kỳ

> Feature **nền (Pass B — không menu)**: tập hợp các Quartz job / hosted-service / worker rải ở nhiều service (Booking, MarketingCampaign, Deposit, Customer, SMS). Không có màn hình riêng — cấu hình nằm ở các màn của từng nghiệp vụ. Chỉ dành cho dev.

## Màn hình & route (FE)
- Không có route riêng cho "job". Cấu hình liên quan ở:
  - Marketing → Chiến dịch chăm sóc KH (`CampaignController`).
  - Cấu hình thông báo SMS/Zalo (`NotificationConfig*`).
  - Đặt lịch (`DatLichController`) — cờ gửi nhắc trước ngày hẹn.
  - Đặt cọc (`deposit-receipt/sms-manager` — `DepositSmsSendController`).

## Job / worker (đăng ký + lịch chạy)
| Job | Loại | Lịch chạy | Đăng ký (path:line) | Hành vi |
|---|---|---|---|---|
| `JobSendSMSTruocNgayDatLich` | IJob (cờ `isRunning` static) | mỗi **5 giây** (`WithIntervalInSeconds(5).RepeatForever()`) | `Booking/Booking.API/Service/JobExtention.cs:28-37` | Gọi `IDatLichService.SendSmsTruocNgayDatLich()` |
| `JobClearFileImportFail` | IJob | cron `0 30 3 * * ?` (3:30) | `Booking/Booking.API/Service/JobExtention.cs:41-51` | Dọn file import lỗi (không liên quan gửi tin) |
| `JobCampaign` | IJob `[DisallowConcurrentExecution]` | mỗi **1 phút** | `MarketingCampaign/MarketingCampaign.API/Service/JobExtention.cs:20-34` | Gọi `ICampaignService.Job_CampaignRun()` |
| `JobUpdateCampaignHasHang` / `JobUpdateCampaignRunningHasHang` | IJob | mỗi **8 phút** | `.../JobExtention.cs:36-64` | Refresh cờ "có hàng" của chiến dịch |
| `JobUpdateCustomerConditionFilterHasHang` | IJob | mỗi **15 phút** | `.../JobExtention.cs:66-78` | Refresh cờ "có hàng" bộ lọc điều kiện |
| `JobScanKhachHangChuaThanhToan` | **IHostedService** (vòng while + Semaphore) | vòng lặp, delay **300000ms (5 phút)**, chỉ trong khung giờ `CheckTimeRun` | `Deposit/Deposit.API/Jobs/JobScanKhachHangChuaThanhToan.cs:39-99` | Gọi `IDepositSmsSendService.SendSMSToNotPayment()` |
| `CustomerScheduler` (gửi SMS sinh nhật) | IJob (cờ `isRunning`) | (đăng ký ở Customer JobExtention) | `Customer/Customer.API/Schedulers/KhachHangScheduler.cs:16` | **`WorkerXuLy` đang comment toàn bộ** → KHÔNG gọi `ScheduleSendSmsBirthDayCustomer()` (job rỗng) — xem Bug-prone |
| `ProcessSendSms` (worker hàng đợi) | endpoint GET (gọi định kỳ ngoài) | bên ngoài kích | `ScheduleSmsController` (Customer) | Quét tối đa 100 `ScheduleSms` đến hạn → gửi → Done/Error |

> Các `JobExtention.AddJobConfig` đều có `if (IsDebug) return` (môi trường `*Debug` không chạy job).

## Endpoint/Controller liên quan (trace registry)
- `marketing/CampaignController.md` — chiến dịch + `Job_CampaignRun` (state `Campaign.StatusJob`: Unprocess/Proccessing/Done/Error).
- `marketing/SmsSendController.md` — cửa publish SMS/Zalo qua RabbitMQ (consumer gửi thật + trừ tiền `AccountSms`).
- `cua-hang/DatLichController.md` — `SendSmsTruocNgayDatLich` (job nhắc lịch), cờ `IsGuiTruocNgayHen`, `NgayGui`.
- `khach-hang/ScheduleSmsController.md` — `ProcessSendSms` worker hàng đợi `ScheduleSms` (Waiting/Pending/Done/Error).
- `mua-hang/DepositSmsSendController.md` — engine gửi tin đặt cọc + nhắc thanh toán (`SendSMSToNotPayment`).
- `marketing/CustomerConditionFilterController.md`, `marketing/CustomerInfoCampaignController.md` — bộ lọc điều kiện + lịch sử gửi từng khách.
- `marketing/NotificationConfigController.md` / `NotificationConfigV2Controller.md` — bật/tắt + cấu hình nhắc theo nghiệp vụ (ActionCode, ví dụ `NLHDL`).

## Entities
- `DatLich` — `IsGuiTruocNgayHen` (bật nhắc), `NgayGui` (mốc đã gửi, null = chưa gửi), `Ngay` (ngày hẹn), `KhachHang` (JSON `List<KhachHangDatLich>`).
- `NotificationConfig` — `ActionCode` (`NLHDL` = nhắc lịch hẹn đặt lịch), `SmsSendBeforMinute`, `IdDonVi`.
- `AccountSms` / `AccountSmsAgency` — `IsUse`, `IsUseZalo` (gate gửi).
- `Campaign` — `IsSuDung`, `KieuGui` (GuiMotLan/GuiNhieuLanDinhKy), `KieuLapLai` (HangNgay/HangTuan/HangThang), `NgayBatDauGui`/`ThoiGianGui`/`NgayTrongTuan`/`NgayTrongThang`, `IdCustomerConditionFilter`, `StatusJob`. `CampaignRunning` — mỗi lần chạy.
- `CustomerInfoCampaign` (MongoDB) — mỗi khách × lần chạy chiến dịch.
- `ScheduleSms` — `Status` (Waiting/Pending/Done/Error), `SmsType` (2=SMS, 4=Zalo), `DataSend` (JSON `SaveSendSmsDto`), `NgayTao`/`NgayCapNhat`.
- `DepositSmsSend` — `StatusSendSms`/`StatusSendZalo` (`EnumDepositSendSmsStatus` 0..5), `IdDepositOrderDeliveryBatch`/`IdBatchSendCustom`, `TypeSend`.

## Luồng code
**A. Nhắc lịch** — `DatLichService.SendSmsTruocNgayDatLich` (`Booking/Booking.Service/Generated/DatLichService.cs:4857`):
1. `GetDsNotiConfigTheoDieuKien(ActionCode="NLHDL")` → distinct theo `IdDonVi` (`:4871-4878`).
2. `AccountSms/TimTheoListId` cho các đơn vị (`:4880-4885`).
3. Mỗi đơn vị: `numberOfDays = ceil(SmsSendBeforMinute / 1440)` (`:4891-4898`).
4. Query `tblDatLich` (`TableWithNolock`): `IsDelete=false && IsGuiTruocNgayHen=true && NgayGui==null && Ngay.Date == DateTime.Now.AddDays(numberOfDays).Date` → `.Take(50)` (`:4903-4910`).
5. Lấy đơn vị/chi nhánh/khách/dịch vụ → dựng nội dung → gửi; set `NgayGui = DateTime.Now` (`:5044-5045`).

**B. Chiến dịch** — `JobCampaign.Execute` → `ICampaignService.Job_CampaignRun()` (`MarketingCampaign/MarketingCampaign.API/Jobs/JobCampaign.cs:16-25`). Job phụ: `JobCampaignRunScan`, `JobScanCustomerInfoCampaign`, `JobScanCustomerInfoWithCondition` (cùng folder `MarketingCampaign.API/Jobs/`); static variables `StaticVariableJobScanCampaignRunning`/`StaticVariableJobScanCustomerInfo*`.

**C. Nhắc thanh toán** — `JobScanKhachHangChuaThanhToan.JobRun.Run` (`Deposit/Deposit.API/Jobs/JobScanKhachHangChuaThanhToan.cs:39`): `CheckTimeRun` (`StaticVariableJobScanKhachHangChuaThanhToan`) → `SetDonViToJobScanKhachHangChuaThanhToan` (nạp đơn vị khi < 50) → `SendSMSToNotPayment` trong `Task.Run` có `Semaphore.Wait(0)` chống chạy chồng.

**D. Hàng đợi SMS** — `ScheduleSmsService.ProcessSendSms` (`Customer/Customer.Service/Generated/ScheduleSmsService.cs:564+`): lấy `Status=Waiting` OR (`Pending` & `NgayCapNhat ≤ now-10p`) → `.Take(100)` → set Pending → gửi theo `SmsType` (`:590-597`) → Done/Error.

**Gửi thật** — mọi job đẩy tin qua publish RabbitMQ (`SendSms*ConsumerDto`/`SendZalo*ConsumerDto`/`SendMessageV2ConsumerDto`) → SMS-service consumer gửi ESMS + trừ `AccountSms`; callback `SmsSendController.ReceiveDataQuery[Zalo]` cập nhật kết quả. Xem skill `projects/scrm/skills/send-notification.md`, `quartz-job.md`.

## Bug-prone / lưu ý
- **SMS sinh nhật tự động đang TẮT**: `CustomerScheduler.WorkerXuLy` (`Customer/Customer.API/Schedulers/KhachHangScheduler.cs:45-60`) comment toàn bộ thân hàm, kể cả `await _service.ScheduleSendSmsBirthDayCustomer()` (`:54`) — job chạy nhưng **không làm gì**. Đây là gốc của câu hỏi "khách không nhận SMS sinh nhật". Cần product xác nhận kênh gửi sinh nhật thực tế (có thể chuyển sang chiến dịch). **Không sửa ở session này.**
- **Job nhắc lịch chạy mỗi 5 giây** (`Booking JobExtention.cs:34-36`) — nhịp dày; mỗi lượt `.Take(50)` + nhiều HTTP call sang Core/Customer/SMS → tải mạng/CPU cao nếu nhiều đơn vị. Cờ chống chạy chồng là `static bool isRunning` (`JobSendSMSTruocNgayDatLich.cs:9`) → an toàn trong 1 process nhưng không phân biệt đa instance.
- **`numberOfDays` làm tròn lên ngày** (`DatLichService.cs:4898`): `SmsSendBeforMinute` nhỏ hơn 1 ngày (vd 120 phút) vẫn ra `numberOfDays=1` → thực tế gửi "trước 1 ngày" chứ không phải trước 120 phút. Tham số phút dưới 1 ngày không chính xác như tên gọi.
- **Khách không lấy được vẫn để `NgayGui=null`**: nhánh `continue` khi `FindCustomersById` rỗng (`DatLichService.cs:4944-4947`) không đánh dấu → lượt sau lặp lại query (tốn tài nguyên) cho tới khi qua ngày hẹn.
- **Nhắc cọc dùng khung giờ hardcode** (`JobScanKhachHangChuaThanhToan.cs:55-56` có `start=21:45`, `end=06:15` nhưng quyết định thực tế nằm trong `StaticVariableJobScanKhachHangChuaThanhToan.CheckTimeRun()`) — biến `start`/`end` khai báo trong vòng lặp **không được dùng trực tiếp** (dead var); logic thật ở `CheckTimeRun`. Đổi khung giờ phải sửa trong static variable, không phải 2 dòng này.
- **Nuốt exception**: `JobCampaign.Execute` catch rỗng (`JobCampaign.cs:22-24`); `JobScanKhachHangChuaThanhToan` catch chỉ gán `var a = ex` (`:82-85`) → lỗi gửi/quét bị nuốt, khó debug. `SmsSendController` cũng nuốt lỗi publish → luôn báo "Thành công" (xem `SmsSendController.md`).
- **ScheduleSms `ProcessSendSms` không có key quyền riêng** (`ScheduleSmsController.cs:44-56`) — chỉ `[SBAuthorize]` mức class → bất kỳ user đăng nhập gọi được endpoint kích worker (cần xác nhận chủ ý). Ngưỡng "kẹt Pending 10 phút" + batch 100 hardcode (`ScheduleSmsService.cs:564-572`).
- **Một số `AddQuartz` bị comment** (Booking job lock sale, SMS `Job_LoadToQueues` AddJob bị comment ở `SMS/SMS.API/Jobs/JobExtention.cs:21`) → có trigger nhưng không AddJob → job không chạy. Khi thấy "job không chạy" kiểm tra cả AddJob lẫn AddTrigger.

## File source (path:line)
- BE Booking job: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Booking\Booking.API\Jobs\JobSendSMSTruocNgayDatLich.cs:7`; đăng ký `...\Booking.API\Service\JobExtention.cs:28`.
- BE Booking service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Booking\Booking.Service\Generated\DatLichService.cs:4857` (`SendSmsTruocNgayDatLich`), `:4898` (numberOfDays), `:4903-4910` (query+Take50), `:5044` (set NgayGui).
- BE Campaign job: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\MarketingCampaign\MarketingCampaign.API\Jobs\JobCampaign.cs:8`; đăng ký `...\MarketingCampaign.API\Service\JobExtention.cs:20`; job phụ cùng folder `Jobs\`.
- BE Deposit job: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Deposit\Deposit.API\Jobs\JobScanKhachHangChuaThanhToan.cs:39`; static var `...\Deposit.Service\Generated\StaticVariables\StaticVariableJobScanKhachHangChuaThanhToan.cs`.
- BE Customer scheduler (sinh nhật, đang tắt): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Customer\Customer.API\Schedulers\KhachHangScheduler.cs:16` (`WorkerXuLy` comment `:45-60`); service `...\Customer\Customer.Service\Generated\ScheduleSmsService.cs` (`ProcessSendSms`, `ScheduleSendSmsBirthDayCustomer`).
- BE SMS job: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\SMS\SMS.API\Jobs\JobExtention.cs:17` (AddJob `Job_LoadToQueues` bị comment `:21`); `Job_JobSendSms.cs`, `Job_JobSendFail.cs`, `Job_JobDeleteRecordSendFail.cs`.
- BE Notification job: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Notification\Notification.API\Job\JobSendNotification.cs`, `JobSendRequestNotiToQueue.cs`.
- Registry: `projects/scrm/registry/marketing/CampaignController.md`, `.../SmsSendController.md`, `projects/scrm/registry/cua-hang/DatLichController.md`, `projects/scrm/registry/khach-hang/ScheduleSmsController.md`, `projects/scrm/registry/mua-hang/DepositSmsSendController.md`.
- Skills: `projects/scrm/skills/quartz-job.md`, `projects/scrm/skills/send-notification.md`.
