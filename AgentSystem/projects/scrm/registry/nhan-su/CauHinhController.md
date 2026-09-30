---
controller: CauHinhController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Hoa hồng CTV > Cấu hình (cài đặt chung của service hoa hồng cho đơn vị)"
muc_dich: "Quản lý cấu hình khởi tạo của service CommissionFee theo đơn vị: các cờ điều khiển sinh dữ liệu hoa hồng (chỉ khi có mã CTV, tạo cho sale/marketing/thu ngân). Có endpoint private cho service khác đọc cấu hình."
alias: [CauHinh, "cấu hình hoa hồng", "CauHinhSetting CommissionFee"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhSetting]
stored_procs: []
depends: []
cross_module_in: [CommissionFeeUpdateOrder (đọc cờ qua CommonCacheService)]
cross_module_out: []
---

# CauHinh — Cấu hình service hoa hồng (theo đơn vị)

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình | (load) | GET `Get` | `[SBAuthorize()]` rỗng | mixed | Trả cấu hình hoa hồng của đơn vị (login). | EF | Chỉ cần login |
| 2 | Lưu cấu hình khởi tạo | Lưu | POST `SaveCauHinhInit` | `CauHinh_CommissionFee_LuuCauHinhKhachHang` (AD-TONG) | public | Validate + lưu cấu hình init (các cờ điều khiển sinh HH). | EF | — |
| 3 | Lấy cấu hình init | (load) | GET `GetCauHinhInit` | `[SBAuthorize()]` rỗng | mixed | Trả cấu hình init. | EF | Chỉ cần login |
| 4 | Lấy cấu hình (private) | — | POST `PrivateGetConfig` | `[PrivateSBAuthorize]` | internal | Service khác đọc cấu hình đơn vị (gọi `GetAsync`). | EF | — |

## Endpoint nội bộ (service-to-service)
- [internal] Service khác / CommissionFeeUpdateOrder lấy cấu hình đơn vị: `PrivateGetConfig`; runtime thực tế đọc qua `CommonCacheService.GetOrSetCache_CauHinhSetting_DonVi` (cache Redis) chứ không gọi HTTP mỗi đơn.

## Quy tắc nghiệp vụ (cờ cấu hình ảnh hưởng sinh hoa hồng)
Các cờ trong `CauHinhSettingDto` (default = true nếu cache miss):
- `IsDongBoThongTinChiKhiCoMaCTV` — true → chỉ sinh dữ liệu HH khi đơn có CTV; đơn không CTV bị bỏ qua hoàn toàn.
- `IsTaoThongTinDonHangChoSale` — true → sinh dữ liệu HH cho nhân viên Sale.
- `IsTaoThongTinDonHangChoMarketing` — true → sinh cho nhân viên Marketing.
- `IsTaoThongTinDonHangChoThuNgan` — true → sinh cho thu ngân (`IdQlDichVuHoTro ?? IdQlDichVu`).

## Quan hệ
- **Gọi sang:** EF.
- **Dữ liệu (entities):** `CauHinhSetting` (1 bản/đơn vị).
- **Ripple/đồng bộ:** đổi cấu hình → cache `CauHinhSetting_DonVi` cần được làm mới; `CommissionFeeUpdateOrder` đọc cache này quyết định có/không sinh dữ liệu HH cho từng vai trò.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs:24,44` — `Get` / `GetCauHinhInit` dùng `[SBAuthorize()]` rỗng (chỉ login) trong khi `SaveCauHinhInit` yêu cầu AD-TONG.
- Pipeline tính HH đọc cấu hình qua **cache Redis** (CommonCacheService), không gọi `PrivateGetConfig` trực tiếp → sau khi lưu cấu hình mới phải đảm bảo cache được invalidate, nếu không đơn mới vẫn theo cấu hình cũ.
