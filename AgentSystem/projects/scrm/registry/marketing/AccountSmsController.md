---
controller: AccountSmsController
module: marketing
service: SMS
man_hinh_ui: "Quản trị hệ thống > Quản lý tài khoản SMS (màn CSKH/Admin cấu hình tài khoản gửi SMS cho các đơn vị); nạp tiền vào tài khoản; xem lịch sử nạp."
muc_dich: "CRUD tài khoản SMS (AccountSms) cấp hệ thống: tạo/sửa/xóa tài khoản gửi tin của đơn vị, nạp tiền, tra số dư, và cung cấp API nội bộ để service khác lấy cấu hình tài khoản SMS đang dùng."
alias: ["AccountSms", "tài khoản sms", "tài khoản gửi tin", "nạp tiền sms", "cấu hình sms đơn vị", "apikey sms"]
auth: mixed
state_machine: []
deprecated: false
entities: [AccountSms, RechargeHistory]
stored_procs: []
depends:
  - "IAccountSmsService (SMS.Services.Generated)"
  - "MaTienIchSCRM.TI_HeThong — tiện ích gate (controller-level, param thứ 4 = true)"
cross_module_in:
  - "[anonymous] `TimTheoListId` — service khác lấy nhiều AccountSms theo list id (không auth)"
  - "[internal] `GetDonViSuDungCauHinhSms` / `GetDonViSuDungCauHinhCmsnSms` / `IsUsingSms` — kiểm tra đơn vị nào đang bật SMS"
cross_module_out:
  - "Consumer gửi SMS đọc AccountSms để lấy apikey/brandname + trừ tiền (số dư)"
---

# AccountSmsController — Quản lý tài khoản SMS (cấp hệ thống)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm tài khoản | Tìm kiếm | POST `api/AccountSms/TimTheoDieuKien` | `AccountSms_TimTheoDonVi` (HT-AD, HT-CSKH) | public | — | Lọc danh sách `AccountSms` theo filter. | EF | — |
| 2 | Xem chi tiết | Chi tiết | GET `api/AccountSms/TimTheoId/{id}` | `AccountSms_TimTheoId` (HT-AD, HT-CSKH) | public | — | Lấy 1 tài khoản theo id. | EF | — |
| 3 | Thêm tài khoản | Thêm mới | POST `api/AccountSms/ThemMoi` | `AccountSms_ThemMoi` (HT-AD, HT-CSKH) | public | — | Tạo AccountSms mới (apikey/secretkey/brandname). | EF | — |
| 4 | Sửa tài khoản | Sửa | POST `api/AccountSms/CapNhatThongTin` | `AccountSms_CapNhatThongTin` (HT-AD, HT-CSKH) | public | — | Cập nhật thông tin tài khoản. | EF | — |
| 5 | Xóa tài khoản | Xóa | POST `api/AccountSms/Xoa/{id}` | `AccountSms_Xoa` (HT-AD, HT-CSKH) | public | — | Xóa tài khoản. | EF | — |
| 6 | Nạp tiền | Nạp tiền | POST `api/AccountSms/DepositMoneyIntoAccount` | `AccountSms_DepositMoneyIntoAccount` (HT-AD, HT-CSKH) | public | — | Cộng số dư vào tài khoản SMS. | EF | — |
| 7 | Lịch sử nạp tiền | Lịch sử nạp | GET `api/AccountSms/RechargeHistoryGetByAccountSmsId/{accountSmsId}` | `AccountSms_RechargeHistoryGetByAccountSmsId` (HT-AD, HT-CSKH) | public | — | Lấy `RechargeHistory` của tài khoản. | EF | — |
| 8 | Tài khoản theo token | (nội bộ UI) | GET `api/AccountSms/GetAccountSmsByToken` | (chỉ `[SBAuthorize]`) | public | — | Lấy AccountSms của đơn vị đang đăng nhập. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- **[anonymous] `TimTheoListId`** (POST, `List<Guid?>`) — lấy nhiều AccountSms 1 lần, KHÔNG auth → dùng cho service khác batch tra cứu.
- **[internal] cờ sử dụng SMS:** `GetDonViSuDungCauHinhSms`, `GetDonViSuDungCauHinhCmsnSms` (chỉ `[SBAuthorize()]` rỗng), `IsUsingSms` — trả danh sách/đơn vị đang bật cấu hình SMS; consumer/service khác dùng để quyết định có gửi tin không.

## Quan hệ
- **Gọi sang:** `IAccountSmsService`.
- **Dữ liệu (entities):** `AccountSms` (apikey, brandname, số dư), `RechargeHistory` (log nạp).
- **Ripple / đồng bộ:** consumer gửi SMS đọc AccountSms để lấy credential + trừ số dư khi gửi.

## Lỗi đã biết / lưu ý kỹ thuật
- `TimTheoListId` `[AllowAnonymous]` (`AccountSmsController.cs:98-103`) — trả AccountSms (gồm thông tin nhạy cảm) không xác thực.
- Controller gate `TI_HeThong` (`cs:12`, param4=true) → khác AccountSmsAgency (gate `TI_SMS`): đây là bản admin hệ thống, không phải bản agency theo đơn vị.
