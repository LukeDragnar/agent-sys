---
controller: SMSLichSuNapTienController
module: marketing
service: SMS
man_hinh_ui: "Quản trị hệ thống (Area QuanTri) > Duyệt nạp tiền SMS (admin/kế toán duyệt hoặc hủy yêu cầu nạp tiền của đơn vị). Route api/QuanTriArea/SMSLichSuNapTien."
muc_dich: "Phía ADMIN: tra cứu mọi yêu cầu nạp tiền SMS, duyệt/hủy duyệt yêu cầu (duyệt → cộng số dư tài khoản), xóa yêu cầu chưa duyệt. Đối ứng với SmsLichSuNapTienController (phía đơn vị)."
alias: ["SMSLichSuNapTien admin", "duyệt nạp tiền sms", "hủy duyệt nạp tiền", "quản trị nạp tiền sms"]
auth: mixed
state_machine: [ChuaDuyet, DaDuyet, Huy]
deprecated: false
entities: [SMSLichSuNapTien, AccountSms]
stored_procs: []
depends:
  - "ISmsLichSuNapTienService (SMS.Services.Generated) — TimTheoDieuKienAdminAsync, Duyet/HuyDuyet"
  - "MaTienIchSCRM.TI_HeThong — tiện ích gate (Area QuanTri, param4=true)"
cross_module_in:
  - "[internal] phía đơn vị (`api/SMSLichSuNapTien/NapTienSMS`) tạo yêu cầu → admin duyệt ở đây"
cross_module_out:
  - "Duyệt nạp tiền → cộng số dư AccountSms (đơn vị có tiền gửi SMS)"
---

# SMSLichSuNapTienController — Duyệt nạp tiền SMS (Area QuanTri / admin)

> Route `api/QuanTriArea/SMSLichSuNapTien`. TRÙNG TÊN class với bản phía đơn vị (route `api/SMSLichSuNapTien`, node `SmsLichSuNapTienController.DonVi.md`).
> File node này đặt tên `...QuanTri.md` để tránh va chạm case-insensitive trên Windows.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách yêu cầu (admin) | Tìm kiếm | POST `api/QuanTriArea/SMSLichSuNapTien/TimTheoDieuKien` | (chỉ `[SBAuthorize]`) | public | — | Lọc TẤT CẢ yêu cầu nạp tiền (`TimTheoDieuKienAdminAsync`). | EF | — |
| 2 | Duyệt nạp tiền | Duyệt | POST `api/QuanTriArea/SMSLichSuNapTien/DuyetNapTienSMS/{id}` | `SMSLichSuNapTien_DuyetNapTienSMS` (HT-AD, HT-KT) | public | state ChuaDuyet → DaDuyet | Duyệt yêu cầu → cộng số dư AccountSms. | EF | — |
| 3 | Hủy duyệt nạp tiền | Hủy duyệt | POST `api/QuanTriArea/SMSLichSuNapTien/HuyDuyetNapTienSMS/{id}` | `SMSLichSuNapTien_HuyDuyetNapTienSMS` (HT-AD, HT-KT) | public | state DaDuyet → Huy | Hủy duyệt yêu cầu nạp tiền. | EF | — |
| 4 | Xóa yêu cầu | Xóa | POST `api/QuanTriArea/SMSLichSuNapTien/Xoa/{id}` | `SMSLichSuNapTien_Xoa` (HT-AD, HT-KT) | public | Chỉ khi `IsDuyet == false` | Xóa yêu cầu chưa duyệt; đã duyệt → trả lỗi "Yêu cầu đã duyệt không thể xóa". | EF | — |

## Quy tắc nghiệp vụ (gating)
1. **Xóa:** chặn nếu `data.Data.IsDuyet == true` → đã duyệt không xóa được (`cs:73-76`).
2. **Duyệt:** chuyển trạng thái + cộng số dư AccountSms (side-effect quan trọng).

## Quan hệ
- **Gọi sang:** `ISmsLichSuNapTienService`.
- **Dữ liệu (entities):** `SMSLichSuNapTien`, `AccountSms` (số dư).
- **Ripple / đồng bộ:** duyệt là điểm cộng tiền vào tài khoản → đơn vị mới gửi SMS được.

## Lỗi đã biết / lưu ý kỹ thuật
- **Trùng tên class** với bản phía đơn vị — phân biệt bằng namespace `SanboxArea` + route `api/QuanTriArea/`. Khi đặt tên file/ref phải rõ Area.
- `TimTheoDieuKien` chỉ `[SBAuthorize]` (không key) nhưng trả dữ liệu admin toàn hệ thống (`SMSLichSuNapTienController.cs:33-38` trong SanboxArea).
