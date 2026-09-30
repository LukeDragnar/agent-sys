---
controller: SmsLichSuNapTienController
module: marketing
service: SMS
man_hinh_ui: "Marketing/Tài khoản SMS > Nạp tiền SMS (đơn vị gửi yêu cầu nạp tiền vào tài khoản SMS + xem lịch sử nạp). Route api/SMSLichSuNapTien."
muc_dich: "Phía ĐƠN VỊ: tạo yêu cầu nạp tiền vào tài khoản SMS và tra cứu lịch sử nạp. (Phía duyệt nằm ở SMSLichSuNapTienController trong Area QuanTri.)"
alias: ["SMSLichSuNapTien", "nạp tiền sms", "yêu cầu nạp tiền", "lịch sử nạp tiền sms đơn vị"]
auth: mixed
state_machine: [ChuaDuyet, DaDuyet, Huy]
deprecated: false
entities: [SMSLichSuNapTien]
stored_procs: []
depends:
  - "ISmsLichSuNapTienService (SMS.Services.Generated)"
  - "MaTienIchSCRM.TI_Chung — tiện ích gate controller-level"
cross_module_in: []
cross_module_out:
  - "QuanTriArea/SMSLichSuNapTien (admin) duyệt/hủy yêu cầu nạp → cộng số dư AccountSms"
---

# SmsLichSuNapTienController — Yêu cầu nạp tiền SMS (phía đơn vị)

> Route `api/SMSLichSuNapTien`. Có 1 controller TRÙNG TÊN ở Area QuanTri (`api/QuanTriArea/SMSLichSuNapTien`) lo việc DUYỆT — xem node `SMSLichSuNapTienController.QuanTri.md`.
> File node này đặt tên `...DonVi.md` để tránh va chạm case-insensitive trên Windows với bản QuanTri (`SmsLichSuNapTien` vs `SMSLichSuNapTien` chỉ khác hoa/thường).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lịch sử nạp tiền | Tìm kiếm | POST `api/SMSLichSuNapTien/TimTheoDieuKien` | `SMSLichSuNapTien_TimTheoDieuKien` (DV-AD-TONG, HT-CSKH) | public | — | Lọc lịch sử nạp tiền SMS của đơn vị. | EF | — |
| 2 | Tạo yêu cầu nạp tiền | Nạp tiền | POST `api/SMSLichSuNapTien/NapTienSMS` | `SMSLichSuNapTien_NapTienSMS` (DV-AD-TONG, HT-CSKH) | public | — | Tạo yêu cầu nạp tiền (`SMNapTienDto`), trạng thái chờ duyệt. | EF | Nhãn quyền `NapTienSMS` ghi nhầm "Tìm kiếm lịch sử nạp tiền sms" (`cs:40`). |

## Quan hệ
- **Gọi sang:** `ISmsLichSuNapTienService`.
- **Dữ liệu (entities):** `SMSLichSuNapTien` (yêu cầu nạp + trạng thái duyệt).
- **Ripple / đồng bộ:** yêu cầu phải được admin (Area QuanTri) duyệt mới cộng số dư `AccountSms`.

## Lỗi đã biết / lưu ý kỹ thuật
- **Trùng tên class** với `SMS.API.Areas.SanboxArea.SMSLichSuNapTienController` — phân biệt qua namespace + route (`api/` vs `api/QuanTriArea/`); đây là bản đơn vị (gate `TI_Chung`).
- `[SBAuthorize]` controller-level bị comment (`cs:11`), chỉ còn auth ở method.
- Nhãn key `NapTienSMS` copy nhầm mô tả "Tìm kiếm lịch sử nạp tiền sms" (`SmsLichSuNapTienController.cs:40`).
