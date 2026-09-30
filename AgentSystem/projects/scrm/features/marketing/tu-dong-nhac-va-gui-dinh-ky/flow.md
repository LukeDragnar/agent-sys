# Luồng — Tác vụ tự động nhắc & gửi tin định kỳ

> Đây là nhóm **job/worker chạy nền**: quét điều kiện → gửi SMS/Zalo → ghi log/đánh dấu. Mỗi job có nhịp và điều kiện riêng. Phần "gửi thật" luôn đi qua hàng đợi (RabbitMQ/MassTransit) tới SMS-service, không gọi gateway trực tiếp tại job.

## Tiền điều kiện (chung)
- Đơn vị có **tài khoản SMS/Zalo** còn hiệu lực (`AccountSms`/`AccountSmsAgency`, `IsUse=true` hoặc `IsUseZalo=true`) và còn tiền/quota.
- Có **mẫu tin** (`PatternSms`/`PatternSmsV2`) với placeholder hợp lệ.
- **Cấu hình thông báo** (`NotificationConfig`) cho nghiệp vụ tương ứng đang bật (ví dụ nhắc lịch dùng mã hành động `NLHDL`).
- Môi trường không phải `*Debug` (các `*Debug` không chạy job — `if (IsDebug) return`).

## Luồng dùng (các job chính)

### A. Nhắc trước ngày hẹn (Booking)
1. Job `JobSendSMSTruocNgayDatLich` chạy **mỗi 5 giây**, có cờ `isRunning` chống chạy chồng.
2. Đọc cấu hình nhắc lịch (`NotificationConfig` ActionCode `NLHDL`, distinct theo đơn vị) + tài khoản SMS đơn vị.
3. Với mỗi đơn vị: tính `numberOfDays = ceil(SmsSendBeforMinute / 1440)` (đổi phút → ngày).
4. Quét lịch của đơn vị thỏa: `IsDelete=false` AND `IsGuiTruocNgayHen=true` AND `NgayGui=null` AND `Ngay.Date == hôm nay + numberOfDays` → **lấy tối đa 50 lịch**.
5. Lấy thông tin đơn vị/chi nhánh/khách/dịch vụ, dựng nội dung, gửi SMS/Zalo.
6. Gửi xong set `NgayGui = now` cho từng lịch để **không gửi lại**.

### B. Chiến dịch chăm sóc định kỳ (MarketingCampaign)
1. Job `JobCampaign` (`[DisallowConcurrentExecution]`) chạy **mỗi 1 phút** → gọi `Job_CampaignRun`.
2. Đọc các `Campaign` đang `IsSuDung` và **đến lịch** (theo `KieuGui` GuiMotLan / GuiNhieuLanDinhKy + `KieuLapLai` HangNgay/HangTuan/HangThang + giờ gửi).
3. Quét khách đủ điều kiện theo `IdCustomerConditionFilter` (CustomerInfo trên MongoDB) → sinh `CustomerInfoCampaign` (mỗi khách 1 bản ghi gửi).
4. Publish message gửi SMS/Zalo (+ voucher/landing nếu cấu hình) → consumer cập nhật trạng thái gửi từng khách.
5. `Campaign.StatusJob`: Unprocess(0) → Proccessing(1) → Done(2)/Error(3).
6. Các job phụ refresh "có hàng" chạy nền: `JobUpdateCampaignHasHang` / `JobUpdateCampaignRunningHasHang` (mỗi 8 phút), `JobUpdateCustomerConditionFilterHasHang` (mỗi 15 phút).

### C. Nhắc đóng tiền khi chưa thanh toán (Deposit)
1. `JobScanKhachHangChuaThanhToan` (IHostedService) chạy vòng lặp, **chỉ trong khung giờ cho phép** (kiểm tra `CheckTimeRun`; mốc trong code: ~21:45→06:15), nghỉ `Task.Delay(300000)` (5 phút) mỗi vòng.
2. Nạp danh sách đơn vị vào hàng xử lý (`SetDonViToJobScanKhachHangChuaThanhToan`) khi còn < 50.
3. Gọi `SendSMSToNotPayment`: với đơn vị bật `IsGuiTinNhanKhiChuaThanhToan`, quét phiếu cọc chưa thanh toán quá `SoPhutGuiTinNhanKhiChuaThanhToan` phút → sinh + gửi nhắc, đặt lock `PaymentReminderSmsStatus=INPROCESSING`.

### D. Hàng đợi SMS khách hàng (Customer — ScheduleSms)
1. Bản ghi `ScheduleSms` được tạo (trạng thái Waiting) khi gửi hàng loạt/đặt lịch SMS.
2. Worker `ProcessSendSms` (gọi định kỳ) lấy bản ghi `Waiting` HOẶC (`Pending` quá 10 phút) → tối đa **100 bản/lần** → set Pending → gửi theo `SmsType` (2=SMS, 4=Zalo) → Done/Error.

## Luồng cấu hình (admin)
- **Nhắc lịch:** bật `NotificationConfig` mã `NLHDL`, đặt `SmsSendBeforMinute`, chọn mẫu; khi tạo lịch bật `IsGuiTruocNgayHen`.
- **Chiến dịch:** Marketing → Chiến dịch → Thêm/Sửa: gắn bộ lọc điều kiện, mẫu SMS/Zalo, voucher, KieuGui/KieuLapLai/giờ gửi, bật `IsSuDung`.
- **Nhắc cọc:** bật `IsGuiTinNhanKhiChuaThanhToan` + đặt `SoPhutGuiTinNhanKhiChuaThanhToan`; tạo mẫu tin đợt.

## Các trường hợp / case biên
- **Hết quota / tài khoản SMS tắt:** job lấy `AccountSms` thấy `IsUse=false && IsUseZalo=false` → `continue` (bỏ qua đơn vị đó, không gửi). Nếu hết tiền thật, lỗi phát sinh ở SMS-service/consumer (job không tự biết).
- **Khách che số / số sai / khách không trong danh sách trả về:** với nhắc lịch, nếu `FindCustomersById` trả rỗng → `continue`, lịch không được gửi (nhưng `NgayGui` vẫn null → lượt sau thử lại nếu còn đúng ngày).
- **Job chạy trùng / chạy chồng:** chống bằng `[DisallowConcurrentExecution]` (Campaign) hoặc cờ tĩnh `isRunning` (Booking) hoặc `Semaphore.Wait(0)` (Deposit) → lượt sau bị bỏ qua nếu lượt trước chưa xong. Lưu ý cờ `isRunning` là **static** → nếu một vòng ném lỗi giữa chừng vẫn được reset trong catch, nhưng nếu process treo cờ có thể kẹt.
- **Ngoài khung giờ (Zalo / nhắc cọc):** Zalo ngoài khung giờ → giữ trạng thái chờ, gửi lại sau; nhắc cọc ngoài khung giờ → job nghỉ (delay 5 phút).
- **Input rỗng / không có bản ghi đến hạn:** nhắc lịch → `continue`; ScheduleSms `ProcessSendSms` → trả mã `HET_TIN_NHAN`.
- **Vượt cap lô:** mỗi lượt giới hạn (nhắc lịch 50, ScheduleSms 100) → phần dư xử lý ở lượt sau, không gửi hết trong 1 nhịp.
- **Gửi 2 lần / đánh dấu:** nhắc lịch set `NgayGui` để không gửi lại; ScheduleSms chuyển Waiting→Pending→Done; Deposit dùng cờ trạng thái + lock Redis 30s.
- **SMS sinh nhật:** đoạn `ScheduleSendSmsBirthDayCustomer` trong `CustomerScheduler.WorkerXuLy` **bị comment** → hiện không tự gửi sinh nhật (cần product xác nhận kênh thay thế).

## Ví dụ cụ thể có số
Đơn vị X bật nhắc lịch với `SmsSendBeforMinute = 1440` (1 ngày). Hôm nay 2026-06-20.
- `numberOfDays = ceil(1440/1440) = 1` → job quét lịch có `Ngay = 2026-06-21`, `IsGuiTruocNgayHen=true`, `NgayGui=null`.
- Có 120 lịch thỏa → job lấy **50 lịch** đầu, gửi SMS/Zalo, set `NgayGui` cho 50 lịch này.
- 5 giây sau job chạy lại → 70 lịch còn lại (`NgayGui` vẫn null) → lấy tiếp 50 → còn 20 → lượt sau gửi nốt. Tổng 3 lượt gửi hết 120 lịch.
- Nếu `AccountSms` của X có `IsUse=false && IsUseZalo=false` → job `continue` ngay, **0 tin gửi** dù 120 lịch thỏa điều kiện.

Chiến dịch Y định kỳ "Hằng tuần, Thứ 2, 09:00", bộ lọc "khách VIP chưa mua 30 ngày" khớp 200 khách:
- Thứ 2 lúc 09:00 (job 1 phút/lần bắt được) → sinh 200 `CustomerInfoCampaign` → publish gửi → `StatusJob` Unprocess→Proccessing→Done; tuần sau lặp lại.

## Hậu điều kiện
- Tin được publish sang SMS-service → gửi thật → **trừ tiền `AccountSms`** + ghi log `SmsSend` (kết quả cập nhật qua callback ESMS).
- Nhắc lịch: lịch được set `NgayGui` (không gửi lại).
- Chiến dịch: mỗi khách có bản ghi `CustomerInfoCampaign` + trạng thái gửi; `Campaign.StatusJob` cập nhật.
- Nhắc cọc: `DepositReceipt`/`DepositSmsSend` cập nhật trạng thái + lock; voucher đợt custom (nếu có) được merge vào tham số tin.
- Không có thao tác nào sửa dữ liệu nghiệp vụ gốc (đơn/khách) ngoài cờ "đã gửi".
