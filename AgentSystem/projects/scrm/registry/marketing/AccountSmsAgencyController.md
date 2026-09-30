---
controller: AccountSmsAgencyController
module: marketing
service: SMS
man_hinh_ui: "Marketing/Cấu hình > Cấu hình SMS-Zalo theo đơn vị (đơn vị tự khai báo tài khoản gửi SMS/Zalo); CSKH duyệt yêu cầu dùng cấu hình hệ thống."
muc_dich: "Quản lý tài khoản/cấu hình SMS-Zalo ở góc nhìn ĐƠN VỊ (agency): đơn vị tự thêm/sửa tài khoản gửi tin của mình (V2), tra số dư, và CSKH duyệt yêu cầu dùng cấu hình mặc định hệ thống."
alias: ["AccountSmsAgency", "cấu hình sms zalo đơn vị", "tài khoản sms agency", "duyệt cấu hình sms", "số dư sms", "dùng cấu hình hệ thống"]
auth: mixed
state_machine: [ChuaDuyet, DaDuyet]
deprecated: false
entities: [AccountSms]
stored_procs: []
depends:
  - "IAccountSmsService (bản v1) — TimTheoIdDonVi, số dư, thêm/sửa theo đơn vị"
  - "IAccountSmsV2Service — CRUD cấu hình SMS/Zalo nâng cao theo id + duyệt accept"
  - "MaTienIchSCRM.TI_SMS — tiện ích gate controller-level"
cross_module_in:
  - "[internal] `TimTheoIdDonViAsync_Private/{idDonVi}` & `GetCauHinhDefaultTheoListId` (PrivateSBAuthorize) — service khác lấy cấu hình SMS theo đơn vị"
cross_module_out:
  - "Consumer/SmsSend dùng cấu hình đơn vị (apikey/ZOA) để gửi tin"
---

# AccountSmsAgencyController — Cấu hình SMS/Zalo theo đơn vị (agency)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách cấu hình SMS/Zalo | Bảng cấu hình | POST `api/AccountSmsAgency/TimTheoDieuKienAsync` | `AccountSmsV2_TimTheoDieuKienAsync` (HT-AD, DV-AD-TONG, HT-CSKH, HT-KT, DV-SALE) | public | — | Lọc cấu hình SMS/Zalo theo đơn vị (V2). | EF (`_serviceV2`) | — |
| 2 | Chi tiết theo đơn vị | (mở form) | GET `api/AccountSmsAgency/TimTheoIdDonViAsync` | `AccountSmsV2_TimTheoIdDonViAsync` (HT-AD, DV-AD-TONG, HT-CSKH, HT-KT, DV-SALE) | public | — | Lấy cấu hình SMS của đơn vị đang đăng nhập (v1 service). | EF | — |
| 3 | Chi tiết theo id | Xem | POST `api/AccountSmsAgency/GetAccountSmsById` | `AccountSmsV2_GetAccountSmsById` (5 role MKT/AD/KT) | public | — | Lấy 1 cấu hình theo id (V2). | EF | — |
| 4 | Thêm/Sửa theo đơn vị | Thêm / Sửa | POST `api/AccountSmsAgency/ThemMoiTheoDonViAsync` · `CapNhatTheoDonViAsync` · `UpdateAccountSmsById` | `AccountSmsAgency_ThemMoiTheoDonViAsync` · `_CapNhatTheoDonViAsync` (HT-AD, HT-CSKH, HT-KT) · `AccountSmsV2_UpdateAccountSmsById` (5 role) | public | — | Tạo/cập nhật cấu hình SMS/Zalo của đơn vị. | EF | — |
| 5 | Xóa cấu hình | Xóa | POST `api/AccountSmsAgency/DeleteAccountSmsById` | `AccountSmsV2_DeleteAccountSmsById` (5 role) | public | — | Xóa cấu hình theo id (V2). | EF | — |
| 6 | Số dư tài khoản | Số dư | GET `api/AccountSmsAgency/GetBalanceAccountSms/{ApiKey}/{SecretKey}` | `AccountSmsAgency_GetBalanceAccountSms` (HT-AD, DV-AD-TONG, HT-CSKH, HT-KT) | public | — | Gọi ESMS lấy số dư theo apikey/secretkey. | HTTP (ESMS) | ApiKey/SecretKey trên URL path. |
| 7 | Cấu hình SMS mặc định | (lấy mẫu HT) | GET `api/AccountSmsAgency/GetAcountSmsDefaultAsync` | `AccountSmsV2_GetAcountSmsDefaultAsync` (HT-AD, HT-CSKH, HT-KT) | public | Chỉ SuperUser / CSKH / Kế toán (check trong code) | Lấy tài khoản SMS mặc định hệ thống. | EF | Check quyền thủ công thêm (`cs:46-49`). |
| 8 | Danh sách yêu cầu dùng cấu hình HT | (CSKH duyệt) | POST `api/AccountSmsAgency/ListAccountSmsUseDefault` | `AccountSmsV2_ListAccountSmsUseDefault` (HT-CSKH) | public | — | Lấy đơn vị yêu cầu dùng cấu hình mặc định. | EF | — |
| 9 | Duyệt/đổi trạng thái tài khoản | Duyệt | POST `api/AccountSmsAgency/ChangeAcceptAccount` | `AccountSmsV2_ChangeAcceptAccount` (HT-CSKH) | public | state ChuaDuyet → DaDuyet | CSKH chấp nhận/đổi trạng thái cấu hình SMS của đơn vị. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- **[internal] `TimTheoIdDonViAsync_Private/{idDonVi}`** — lấy cấu hình SMS theo đơn vị (không cần user login).
- **[internal] `GetCauHinhDefaultTheoListId`** (POST `List<Guid>`) — batch lấy cấu hình mặc định cho nhiều đơn vị; consumer/SmsSend dùng để biết apikey/ZOA khi gửi.

## Quy tắc nghiệp vụ (gating)
1. **Trộn 2 service:** `_service` (IAccountSmsService, v1) cho thao tác theo đơn vị-đăng-nhập; `_serviceV2` (IAccountSmsV2Service) cho CRUD theo id + duyệt. Khi sửa nghiệp vụ phải xác định đúng service.
2. **Duyệt cấu hình:** đơn vị tạo cấu hình → trạng thái chờ → CSKH `ChangeAcceptAccount` mới active.

## Quan hệ
- **Gọi sang:** `IAccountSmsService`, `IAccountSmsV2Service`.
- **Dữ liệu (entities):** `AccountSms` (cấu hình SMS/Zalo của đơn vị).
- **Ripple / đồng bộ:** cấu hình đã duyệt là nguồn apikey/ZOA cho `SmsSendController` + consumer khi gửi tin theo đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller dùng SONG SONG v1 (`_service`) và v2 (`_serviceV2`) service (`AccountSmsAgencyController.cs:19-28`) — dễ nhầm khi maintain; endpoint `TimTheoIdDonViAsync`/`GetBalance` dùng v1, còn CRUD by-id dùng v2.
- `GetAcountSmsDefaultAsync` vừa có `[SBAuthorize(key,...)]` vừa check `IsSuperUser/IsCSKH/IsHeThongKeToan()` thủ công (`cs:46-49`) → 2 lớp quyền.
- Khác `AccountSmsController` (gate `TI_HeThong`): controller này gate `TI_SMS` và là góc nhìn đơn vị (agency).
