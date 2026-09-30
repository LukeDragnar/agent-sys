---
controller: CauHinhController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > Cấu hình order (cấu hình đơn hàng của service OrderLogistic): cấu hình order chung, cấu hình SuperAdmin (HT-AD/CSKH), cấu hình đơn hàng sale, cấu hình init, cấu hình nghiệp vụ"
muc_dich: "Quản lý cấu hình của service OrderLogistic: cấu hình order cấp đơn vị (kho tổng, chia số care đơn...), cấu hình SuperAdmin, cấu hình đơn hàng sale, cấu hình init, cấu hình nghiệp vụ; kèm các endpoint private để service khác lấy/đồng bộ cấu hình + đồng bộ cấu hình từ Contact và invoice."
alias: ["cấu hình order logistic", "cấu hình đơn hàng kho", "cấu hình kho tổng", "cấu hình superadmin order", "cấu hình nghiệp vụ order", "cấu hình đơn hàng sale"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh, CauHinhSetting, CauHinhDonHangSale, CauHinhNghiepVu]
stored_procs: []
depends:
  - "ICauHinhService (OrderLogistic.Services.Generated)"
cross_module_in:
  - "Contact (DongBoCauHinhDonHangTuContact — Contact đồng bộ cấu hình đơn hàng sang OrderLogistic) [PrivateSBAuthorize]"
  - "Service nội bộ khác gọi GetCauHinhOrder / GetAllCauHinhSuperAdmin / GetAllAsyncPrivate để lấy cấu hình OrderLogistic [PrivateSBAuthorize]"
  - "Invoice (CapNhatInvoice — payload CauHinhInvoiceSyncToAnotherService đồng bộ cấu hình invoice sang OrderLogistic)"
cross_module_out:
  - "ChiaSoCareDonController (OrderLogistic) — đọc cấu hình kho tổng (IsSuDungKhoTong / IsSuDungChiaSoCareDon...) khi phân bổ đơn"
---

# CauHinhController (OrderLogistic) — Cấu hình order/service kho

> **Trùng tên** với `CauHinhController` module khác → node này suffix `_OrderLogistic`. Tiện ích `[TienIchAttribute("CauHinh_OrderLogistic_Controller", ..., TI_OrderLogistic)]`. Controller mix `[SBAuthorize]` (UI) và `[PrivateSBAuthorize]` (service-to-service).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình order (của user) | load màn | GET `api/CauHinh/Get` | `[SBAuthorize]` (mở) | public | - | Trả cấu hình order theo CurrentUser/đơn vị | EF | - |
| 2 | Lưu cấu hình SuperAdmin | "Lưu" (SuperAdmin) | POST `api/CauHinh/Save_SuperAdmin` | `CauHinh_OrderLogistic_LuuCauHinhOrderSuperAdmin` | public | role HT-AD/HT-CSKH | Lưu cấu hình order cấp SuperAdmin | EF | - |
| 3 | Lấy cấu hình SuperAdmin | màn SuperAdmin | POST `api/CauHinh/GetCauHinhSuperAdmin` | `CauHinh_OrderLogistic_GetCauHinhSuperAdmin` | public | role HT-AD/HT-CSKH | Trả cấu hình SuperAdmin theo payload | EF | - |
| 4 | Lấy cấu hình đơn hàng sale | màn cấu hình sale | GET `api/CauHinh/GetCauHinhDonHangSale` | `[SBAuthorize]` (mở) | public | - | Trả cấu hình đơn hàng sale | EF | - |
| 5 | Lưu cấu hình đơn hàng sale | "Lưu" (sale) | POST `api/CauHinh/SaveCauHinhDonHangSale` | `CauHinh_OrderLogistic_SaveCauHinhDonHangSale` | public | role DV-AD-TONG | Lưu cấu hình đơn hàng sale | EF | - |
| 6 | Lấy cấu hình init | load init | GET `api/CauHinh/GetCauHinhInit` | `[SBAuthorize]` (mở) | public | - | Trả cấu hình order ban đầu (init) | EF | - |
| 7 | Lưu cấu hình init | "Lưu" (init) | POST `api/CauHinh/SaveCauHinhInit` | `CauHinh_OrderLogistic_SaveCauHinhInit` | public | role DV-AD-TONG | Lưu cấu hình order ban đầu | EF | - |
| 8 | Check quyền xuất Excel | gating nút xuất | GET `api/CauHinh/CheckQuyenXuatExcel` | `[SBAuthorize]` (mở) | public | - | Trả quyền user được xuất Excel hay không | EF | - |
| 9 | Lấy/Lưu cấu hình nghiệp vụ | màn cấu hình nghiệp vụ | GET `api/CauHinh/GetNghiepVuCauHinh/{nghiepVu}` ; POST `api/CauHinh/SaveNghiepVuCauHinh` | `[SBAuthorize]` (mở) | public | - | Lấy/lưu cấu hình theo từng nghiệp vụ (key `nghiepVu`) | EF | Permission key bị comment (TODO debug) → tạm mở `[SBAuthorize]`, CauHinhController.cs:127,137 |
| 10 | Cập nhật cấu hình invoice | đồng bộ invoice | POST `api/CauHinh/CapNhatInvoice` | `[SBAuthorize]` (mở) | public | - | Cập nhật cấu hình invoice (payload `CauHinhInvoiceSyncToAnotherService` từ module Invoice) | EF | - |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GET api/CauHinh/GetCauHinhOrder/{idDonVi}` `[PrivateSBAuthorize]` — service khác lấy `CauHinhSettingDto` theo đơn vị.
- [internal] `GET api/CauHinh/GetAllCauHinhSuperAdmin` `[PrivateSBAuthorize]` — lấy toàn bộ cấu hình SuperAdmin.
- [internal] `GET api/CauHinh/GetAllAsyncPrivate/{idDonVi}` `[PrivateSBAuthorize]` — lấy toàn bộ cấu hình của đơn vị.
- [internal] `POST api/CauHinh/DongBoCauHinhDonHangTuContact` `[PrivateSBAuthorize]` — **Contact gọi sang** đồng bộ cấu hình đơn hàng giữa Contact và OrderLogistic (`DongBoCauHinhGiuaDonHangVaContactReq`).

## Quan hệ
- **Gọi sang:** `ICauHinhService` (GetAsync, Save_SuperAdminAsync, SaveCauHinhDonHangSale, SaveCauHinhInit, GetNghiepVuCauHinh/SaveNghiepVuCauHinh, DongBoCauHinhDonHangTuContact, CapNhatInvoice).
- **Dữ liệu (entities):** `CauHinh`/`CauHinhSetting` (cờ kho tổng `IsSuDungKhoTong`, `IsSuDungChiaSoCareDon`, `IsSuDungChiaSoCareDonKhoTong`, `IdChiNhanhTong`...), cấu hình đơn hàng sale, init, nghiệp vụ.
- **Ripple / đồng bộ:** cấu hình kho tổng/chia số care đơn được `ChiaSoCareDonController` đọc khi phân bổ đơn (quyết view all đơn toàn đơn vị hay theo chi nhánh). Đồng bộ 2 chiều với Contact (DongBoCauHinhDonHangTuContact) và Invoice (CapNhatInvoice).

## Lỗi đã biết / lưu ý kỹ thuật
- Một số action `Save`/`GetNghiepVuCauHinh`/`SaveNghiepVuCauHinh` có permission key bị **comment "TODO comment for debug"** → tạm thời chỉ `[SBAuthorize]` mở (CauHinhController.cs:27,127,136) — nguy cơ thiếu phân quyền nếu lên prod.
- Action `Save` (lưu cấu hình order chung) đã bị comment toàn bộ (CauHinhController.cs:27-33) → không còn endpoint lưu cấu hình order cơ bản; chỉ còn SuperAdmin/sale/init/nghiệp vụ.
- Trùng tên controller với module khác → node dùng suffix `_OrderLogistic`; tiện ích đăng ký tên `CauHinh_OrderLogistic_Controller`.
- 🆕 `CauHinhSettingDto` (OrderLogistic.Client) có thêm cờ feature **xuất HĐ theo đơn** (xem `features/hoa-don-yeu-cau-xuat-logistic`): `IsYeuCauXuatHoaDonMacDinh` (default true — cờ default khi tạo đơn thường mới) + `LoaiDonKhongXuatPhiShip` (List<UnitCode> — blocklist loại đơn KHÔNG xuất phí ship, OR với công tắc global `IsKhongXuatHDDT_PhiGiaoHang`).
