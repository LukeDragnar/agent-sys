---
controller: CauHinhController
module: cua-hang
service: Order
man_hinh_ui: "Cài đặt hệ thống / Cấu hình Order (config-order, ConfigOrderComponent) — đọc/lưu cấu hình Order theo đơn vị. Cấu hình SuperAdmin ở màn quản trị đơn vị (quantri/cauhinh-donvi-super-admin). Cờ cấu hình cũng được đọc ở POS thu ngân."
muc_dich: "Cấu hình tổng của Order theo đơn vị (CauHinhSettingDto): lấy/lưu cấu hình init, cấu hình SuperAdmin, cập nhật cấu hình Invoice, kiểm quyền xem báo cáo đơn hàng. Các cờ cấu hình (vd IsKhongChoPhepXoaDonHang) gate hành vi ở DonHang."
alias: ["cấu hình order", "cấu hình tổng order", "config order", "cấu hình đơn vị order", "cấu hình superadmin order"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh, CauHinhSetting]
stored_procs: []
depends: [Order, Invoice]
cross_module_in: [Order/config-order + quantri/cauhinh-donvi-super-admin + thungan gọi Get/GetCauHinhInit/Save_SuperAdmin/GetCauHinhSuperAdmin; service-to-service gọi GetCauHinhOrder/{idDonVi} + GetAllCauHinhSuperAdmin; Invoice sync gọi CapNhatInvoice]
cross_module_out: [Cờ CauHinhSettingDto.IsKhongChoPhepXoaDonHang gate DonHangController.Xoa (chặn xóa đơn nếu bật); cấu hình Order áp cho luồng tạo đơn/báo cáo]
---

# CauHinhController (Order) — Cấu hình tổng Order theo đơn vị

> Lưu ý: trùng tên class `CauHinhController` với service PreOrder (node `CauHinhController.md`). Node này là bản của **service Order**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình order | load màn cấu hình | GET `api/CauHinh/Get` | (rỗng — login) | public | màn cài đặt | `GetAsync(CurrentUser)` — cấu hình order của đơn vị user | EF | permission key đã comment (`...LayCauHinhOrder`) → còn login |
| 2 | Lưu cấu hình SuperAdmin | "Lưu" (màn quản trị) | POST `api/CauHinh/Save_SuperAdmin` | `CauHinh_Order_LuuCauHinhOrderSuperAdmin` | public | màn SuperAdmin (HT-AD,HT-CSKH) | `Save_SuperAdminAsync(payload, CurrentUser)` | EF | - |
| 3 | Lấy cấu hình SuperAdmin | load | POST `api/CauHinh/GetCauHinhSuperAdmin` | `CauHinh_Order_GetCauHinhSuperAdmin` | public | màn SuperAdmin | `GetCauHinhSuperAdmin(payload, CurrentUser)` | EF | - |
| 4 | Lấy cấu hình init | load màn cài đặt | GET `api/CauHinh/GetCauHinhInit` | (rỗng — login) | public | màn cài đặt | `GetCauHinhInit(CurrentUser)` | EF | - |
| 5 | Lưu cấu hình init | "Lưu" | POST `api/CauHinh/SaveCauHinhInit` | `CauHinh_Order_SaveCauHinhInit` | public | nút Lưu (DV-AD-TONG) | `SaveCauHinhInit(inSetting, CurrentUser)` | EF | - |
| 6 | Cập nhật cấu hình Invoice | (đồng bộ từ Invoice) | POST `api/CauHinh/CapNhatInvoice` | (rỗng — login) | public | - | `CapNhatInvoice(inSetting, CurrentUser)` — nhận `CauHinhInvoiceSyncToAnotherService` từ Invoice, cập nhật cấu hình invoice trong Order | EF | endpoint sync nhưng chỉ login (không PrivateSBAuthorize) |
| 7 | Kiểm quyền xem báo cáo đơn | (ẩn/hiện menu báo cáo) | GET `api/CauHinh/CheckQuyenXemReportOrder` | (rỗng — login) | public | trước khi vào báo cáo | `CheckQuyenXemReportOrder(CurrentUser)` — trả bool quyền xem report order | EF | - |

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize]`] **`GetCauHinhOrder/{idDonVi}`** (GET) — service khác lấy `CauHinhSettingDto` theo `idDonVi` (`GetAsync(idDonVi).CauHinhSettingDto`).
- [internal — `[PrivateSBAuthorize]`] **`GetAllCauHinhSuperAdmin`** (GET) — service khác lấy toàn bộ cấu hình SuperAdmin (`GetAllCauHinhSuperAdmin()`).

## Deprecated / commented
- **`Save`** (POST `api/CauHinh/Save`, key `CauHinh_Order_LuuCauHinhOrder`) — đã **comment toàn bộ** (controller:24-30); thay bằng `SaveCauHinhInit`. Controller-level `[SBAuthorize("CauHinhController")]` cũng comment.

## Quan hệ
- **Gọi sang:** `ICauHinhService` (GetAsync / Save_SuperAdminAsync / GetCauHinhSuperAdmin / GetAllCauHinhSuperAdmin / GetCauHinhInit / SaveCauHinhInit / CapNhatInvoice / CheckQuyenXemReportOrder).
- **Dữ liệu (entities):** `CauHinh` (CauHinhSettingDto — chứa các cờ như `IsKhongChoPhepXoaDonHang`).
- **Ripple / đồng bộ:** Invoice service đẩy cấu hình invoice sang qua `CapNhatInvoice`; cờ trong CauHinhSettingDto gate hành vi đơn hàng.

## Luồng xuyên module (cờ cấu hình gate xóa đơn)
```
Admin set cờ IsKhongChoPhepXoaDonHang ở cấu hình Order (SaveCauHinhInit)
  → DonHangController.Xoa đọc cấu hình (GetAsync) trước khi xóa
      → nếu IsKhongChoPhepXoaDonHang == true → trả "Đơn vị không cho phép xóa đơn hàng!" (chặn xóa)
```

## Lỗi đã biết / lưu ý kỹ thuật
- `Get`/`GetCauHinhInit`/`CheckQuyenXemReportOrder`/`CapNhatInvoice` chỉ `[SBAuthorize]` rỗng (login) — riêng `CapNhatInvoice` là endpoint đồng bộ từ Invoice nhưng **không** dùng `[PrivateSBAuthorize]`, chỉ cần login → cân nhắc nên là internal.
- `Save` (bản cũ) đã comment hẳn — bỏ qua, dùng `SaveCauHinhInit`.
- Trùng tên class với `CauHinhController` của PreOrder → node tách bằng hậu tố `.Order` (theo precedent `CauHinhController.Booking.md`).
- Code thật: BE `Order/Order.API/Controllers/CauHinhController.cs`; FE service `services/order/cau-hinh.service.ts`, page `pages/caidat-hethong/configsystem/config-order/` + `pages/quantri/cauhinh-donvi-super-admin/`. Cross-module gate ở `Order/Order.API/Controllers/DonHangController.cs:831` (IsKhongChoPhepXoaDonHang).
