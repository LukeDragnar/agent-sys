---
controller: AccountSmsUnitUsedController
module: marketing
service: SMS
man_hinh_ui: "Quản trị > Cấu hình SMS / Duyệt đại lý dùng tài khoản SMS (đơn vị đăng ký dùng tài khoản SMS của đại lý/tổng, DV-AD-TONG hoặc kế toán duyệt)."
muc_dich: "Quản lý quan hệ 'đơn vị sử dụng tài khoản SMS' (AccountSmsUnitUsed): đơn vị đăng ký dùng tài khoản SMS, được duyệt/từ chối; tra cứu đơn vị đã được chấp nhận."
alias: ["AccountSmsUnitUsed", "đơn vị dùng tài khoản sms", "duyệt đại lý sms", "đăng ký dùng sms"]
auth: mixed
state_machine: [ChuaDuyet, DaDuyet]
deprecated: false
entities: [AccountSmsUnitUsed]
stored_procs: []
depends:
  - "IAccountSmsUnitUsedService (SMS.Services.Generated)"
  - "AutoMapperConfiguration.Mapper — map entity ↔ Dto"
  - "MaTienIchSCRM.TI_HeThong — tiện ích gate controller-level"
cross_module_in: []
cross_module_out:
  - "Consumer/SmsSend kiểm tra đơn vị đã được duyệt dùng tài khoản trước khi gửi"
---

# AccountSmsUnitUsedController — Đơn vị sử dụng tài khoản SMS (duyệt đại lý)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | Tìm kiếm | POST `api/AccountSmsUnitUsed/TimTheoDieuKien` | `AccountSmsUnitUsed_TimTheoDieuKien` (DV-AD-TONG, HT-KT) | public | — | Lọc danh sách đơn vị đăng ký dùng tài khoản SMS. | EF | — |
| 2 | Thêm mới | Thêm | POST `api/AccountSmsUnitUsed/ThemMoi` | `AccountSmsUnitUsed_ThemMoi` (DV-AD-TONG, HT-KT) | public | — | Tạo bản ghi đơn vị-dùng-tài-khoản. | EF | — |
| 3 | Chi tiết | Xem | GET `api/AccountSmsUnitUsed/TimTheoId/{id}` | `AccountSmsUnitUsed_TimTheoId` (DV-AD-TONG, HT-KT) | public | Bản ghi phải thuộc đơn vị đang đăng nhập (`IdDonVi == DonViId`) | Lấy bản ghi; nếu không thuộc đơn vị → trả "Không tìm thấy". | EF | Thông báo lỗi ghi nhầm "Không tìm thấy Q&A!" (`cs:53`). |
| 4 | Duyệt đại lý | Duyệt | POST `api/AccountSmsUnitUsed/ChangeAccept/{id}` | `AccountSmsUnitUsed_ChangeAccept` (DV-AD-TONG, HT-KT) | public | state ChuaDuyet → DaDuyet | Duyệt đơn vị được dùng tài khoản SMS. | EF | — |
| 5 | Xóa | Xóa | POST `api/AccountSmsUnitUsed/Xoa/{id}` | `AccountSmsUnitUsed_Xoa` (DV-AD-TONG, HT-KT) | public | Bản ghi tồn tại | Xóa bản ghi (kiểm tra tồn tại trước). | EF | — |
| 6 | Kiểm tra đã duyệt | (nội bộ UI) | POST `api/AccountSmsUnitUsed/IsAccepted` | (chỉ `[SBAuthorize()]`) | public | — | Trả đơn vị đăng nhập đã được duyệt dùng tài khoản SMS hay chưa. | EF | Nhận param `id` nhưng service chỉ dùng `CurrentUser` (`cs:74-77`). |

## Quan hệ
- **Gọi sang:** `IAccountSmsUnitUsedService`.
- **Dữ liệu (entities):** `AccountSmsUnitUsed` (đơn vị ↔ tài khoản SMS + trạng thái duyệt).
- **Ripple / đồng bộ:** trạng thái duyệt được dùng để gate quyền gửi tin của đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `LayTheoIdAsync` filter theo `CurrentUser.DonViId` (`cs:51`) — chỉ xem được bản ghi của đơn vị mình; thông báo lỗi copy nhầm domain Q&A (`cs:53`).
- `IsAccepted` khai báo param `Guid id` nhưng không dùng — chỉ check `CurrentUser` (`cs:74-77`).
