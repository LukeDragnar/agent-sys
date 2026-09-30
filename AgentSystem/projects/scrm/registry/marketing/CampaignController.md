---
controller: CampaignController
module: marketing
service: MarketingCampaign
man_hinh_ui: "Marketing > Chiến dịch chăm sóc khách hàng — list chiến dịch + nút Thêm/Sửa/Xóa, xem lịch sử chạy Job"
muc_dich: "Quản lý chiến dịch CSKH: cấu hình gửi SMS/Zalo/voucher theo tập điều kiện khách hàng (IdCustomerConditionFilter), kiểu gửi 1 lần / định kỳ; Job nền quét khách đủ điều kiện rồi gửi."
alias: ["chiến dịch", "campaign", "chiến dịch marketing", "gửi SMS chiến dịch", "chiến dịch chăm sóc khách hàng", "gửi zalo định kỳ"]
auth: public
state_machine: [Unprocess, Proccessing, Done, Error]   # Campaign.StatusJob (EnumStatusJob)
deprecated: false
entities: [Campaign, CampaignRunning]
stored_procs: []
depends: [Customer, Voucher/Promotion, SMS/Zalo template service, MongoDB (CustomerInfo), RabbitMQ]
cross_module_in: [Customer (sync khách hàng), Landing/SMS gateway (update trạng thái gửi)]
cross_module_out: [ContactSendSMS (log SMS landing), CustomerInfoCampaign (lịch sử gửi/khách hàng theo job), Voucher (áp voucher chiến dịch)]
---

# CampaignController — Chiến dịch chăm sóc khách hàng

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm chiến dịch theo điều kiện | "Tìm kiếm" | POST `api/Campaign/SearchByCondition` | `CampaignController_SearchByCondition` | public | màn list | List chiến dịch của đơn vị, lọc keyword (Ma/Ten) + IdCustomerConditionFilter; **scope phân quyền**: admin đơn vị thấy all, admin chi nhánh lọc theo IdChiNhanh, user thường chỉ thấy bản ghi mình tạo (GetListIdUserGuidByAuthor) | EF `Campaign` | - |
| 2 | Lịch sử chiến dịch chạy Job | "Lịch sử" | POST `api/Campaign/SearchHistoryByCondition` | `CampaignController_SearchHistoryByCondition` | public | tab lịch sử | List lần chạy Job (CampaignRunning) theo điều kiện | EF `CampaignRunning` | - |
| 3 | Lấy tất cả chiến dịch theo đơn vị | (gọi nội bộ/màn liên quan) | GET `api/Campaign/GetAllCampaignByIdDonVi/{id}` | `CampaignController_GetAllCampaignByIdDonVi` | public | - | List toàn bộ chiến dịch theo idDonVi | EF `Campaign` | Quyền role khác hẳn các action còn lại (HT-AD, HT-CSKH, HT-KT) — endpoint mang tính hệ thống/CSKH |
| 4 | Chi tiết chiến dịch | "Xem" | GET `api/Campaign/GetById/{id}` | `CampaignController_GetById` | public | - | Trả full chi tiết chiến dịch (CampaignDetailRes) | EF `Campaign` | - |
| 5 | Tạo chiến dịch | "Thêm mới" | POST `api/Campaign/Insert` | `CampaignController_Insert` | public | - | ValidateData(CREATE) → map → set IdDonVi/IdChiNhanh theo currentUser; serialize ListParams nếu IsUseParams. Mặc định StatusJob=Unprocess | EF `Campaign` | - |
| 6 | Cập nhật chiến dịch | "Sửa" | POST `api/Campaign/Update` | `CampaignController_Update` | public | - | Bắt buộc có Id; check tồn tại trong scope phân quyền; Validate(UPDATE); KieuGui=GuiMotLan → cắt giờ NgayBatDauGui về .Date; MarkDirty các field rồi update | EF `Campaign` | - |
| 7 | Xóa chiến dịch | "Xóa" | POST `api/Campaign/Delete/{id}` | `CampaignController_Delete` | public | - | Xóa chiến dịch theo id | EF `Campaign` | - |
| 8 | Lấy SĐT khách theo idKhachHang | (màn chi tiết) | POST `api/Campaign/GetSoDienThoaiByIdKhachHang/{idKhachHang}` | `CampaignController_GetSoDienThoaiByIdKhachHang` | public | - | Trả số điện thoại khách hàng để preview/gửi | EF | - |

## Quy tắc nghiệp vụ (gating / scope)
- **Scope dữ liệu 3 tầng** (lặp ở SearchByCondition + Update, là invariant của module marketing):
  1. Luôn lọc `IdDonVi == currentUser.DonViId`.
  2. KHÔNG phải admin đơn vị (`CheckUserLoginIsRoleAdmin`=false) → thêm `IdChiNhanh == currentUser.IdChiNhanh`.
  3. KHÔNG phải admin chi nhánh (`CheckUserLoginIsRoleAdminChiNhanh`=false) → chỉ thấy bản ghi `NguoiTaoUserId ∈ GetListIdUserGuidByAuthor(currentUser)` (cây user mình quản lý).
- **KieuGui** (EnumKieuGui): GuiMotLan / GuiNhieuLanDinhKy. GuiMotLan → cắt NgayBatDauGui về ngày (bỏ giờ).
- **KieuLapLai** (EnumKieuLapLai): HangNgay / HangTuan / HangThang → kết hợp NgayTrongTuan / NgayTrongThang / ThoiGianGui khi định kỳ.
- **StatusJob** (EnumStatusJob): Unprocess(0) → Proccessing(1) → Done(2) / Error(3). Job nền chuyển trạng thái.

## Quan hệ
- **Gọi sang:** Customer (lấy SĐT, dữ liệu khách hàng), Voucher (CheckVoucherValid — hiện comment), template SMS/Zalo (IdSMSTemplate/IdZaloTemplate), Landing (IdLanding).
- **Dữ liệu (entities):** `Campaign` (cấu hình chiến dịch), `CampaignRunning` (mỗi lần Job chạy). Liên kết `IdCustomerConditionFilter` → MongoDB CustomerConditionFilter (controller CustomerConditionFilter).
- **Ripple / đồng bộ:** Job quét khách đủ điều kiện → sinh bản ghi CustomerInfoCampaign + gửi SMS/Zalo → log ContactSendSMS; cập nhật trạng thái gửi qua consumer (EventUpdateStatusSendSMS / EventLandingCampaignUpdateStatusSMSSend).

## Luồng xuyên module (Job chạy chiến dịch)
```
Job_CampaignRun (JobCampaign) → đọc Campaign IsSuDung + đến lịch (NgayGioGui/KieuLapLai)
  → Job_Campaign_QuetKhachHangDuDieuKien: lọc CustomerInfo theo IdCustomerConditionFilter
  → sinh CustomerInfoCampaign (mỗi khách 1 bản ghi gửi)
  → publish message gửi SMS/Zalo (+ voucher/landing nếu cấu hình)
  → consumer cập nhật StatusJob + trạng thái gửi từng khách
```

## Lỗi đã biết / lưu ý kỹ thuật
- `CheckVoucherValid` đã bị comment ở controller (CampaignController.cs:78-83) — interface ICampaignService vẫn còn khai báo nhưng không expose endpoint. Áp voucher hiện đi qua luồng Job, không qua API trực tiếp.
- Endpoint `GetAllCampaignByIdDonVi` gắn role hệ thống (HT-AD/HT-CSKH/HT-KT) khác hẳn nhóm role marketing (DV-*-MKT) của các action còn lại — dùng cho cross-service/CSKH, không phải UI marketing thường.
- Scope phân quyền 3 tầng bị lặp thủ công ở từng method (Search + Update) — sửa rule scope phải đồng bộ mọi nơi.
