---
controller: CauHinhChiNhanhController
module: cua-hang
service: Order
man_hinh_ui: "Cài đặt hệ thống / Cấu hình Order theo chi nhánh (config-order-chi-nhanh, ConfigOrderChiNhanhComponent) — đọc/lưu cấu hình Order ở mức chi nhánh. Cũng được đọc ở POS thu ngân (thungan-edit)."
muc_dich: "Quản lý cấu hình Order theo từng chi nhánh (CauHinhChiNhanhSettingDto): lấy cấu hình init cho màn cài đặt + lưu lại; cung cấp endpoint nội bộ cho service khác lấy cấu hình theo chi nhánh."
alias: ["cấu hình chi nhánh order", "cấu hình order theo chi nhánh", "config order chi nhánh"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhChiNhanh]
stored_procs: []
depends: [Order]
cross_module_in: [Order/thungan (thungan-edit, don-hang-offline-quet-imei) gọi GetCauHinhInit/Get; service-to-service gọi GetCauHinhChiNhanhOrder/{idChiNhanh}]
cross_module_out: [Cấu hình chi nhánh áp dụng cho luồng tạo đơn POS theo chi nhánh]
---

# CauHinhChiNhanhController — Cấu hình Order theo chi nhánh

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình (init) | load màn cấu hình | GET `api/CauHinhChiNhanh/Get` | (rỗng — login) | public | màn cài đặt | `GetCauHinhInit(CurrentUser)` — trả cấu hình init chi nhánh của user | EF | `Get` và `GetCauHinhInit` gọi cùng `_service.GetCauHinhInit` → trùng hành vi (2 route 1 logic) |
| 2 | Lấy cấu hình init | load | GET `api/CauHinhChiNhanh/GetCauHinhInit` | (rỗng — login) | public | màn cài đặt | Như #1 — `GetCauHinhInit(CurrentUser)` | EF | trùng với `Get` |
| 3 | Lưu cấu hình init | "Lưu" | POST `api/CauHinhChiNhanh/SaveCauHinhInit` | (rỗng — login) | public | nút Lưu | `SaveCauHinhInit(inSetting, CurrentUser)` — lưu CauHinhChiNhanhSettingDto cho chi nhánh | EF | permission key đã comment (`...SaveCauHinhChiNhanhInit`/DV-AD-TONG) → chỉ còn login |

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize]`] **`GetCauHinhChiNhanhOrder/{idChiNhanh}`** (GET) — service khác lấy `CauHinhChiNhanhSettingDto` theo `idChiNhanh` (`GetAsync(idChiNhanh)`), wrap `ResultWithData`. Không qua token user.

## Quan hệ
- **Gọi sang:** `ICauHinhChiNhanhService` (GetCauHinhInit / GetAsync / SaveCauHinhInit).
- **Dữ liệu (entities):** `CauHinhChiNhanh` (CauHinhChiNhanhSettingDto).
- **Ripple / đồng bộ:** cấu hình chi nhánh dùng ở POS thu ngân theo chi nhánh.

## Lỗi đã biết / lưu ý kỹ thuật
- `Get` và `GetCauHinhInit` cùng gọi `GetCauHinhInit` → 2 endpoint trùng logic (FE service map cả hai).
- `SaveCauHinhInit` mất permission key (đã comment) → chỉ còn yêu cầu đăng nhập, bất kỳ role nào login đều lưu được cấu hình chi nhánh.
- Code thật: BE `Order/Order.API/Controllers/CauHinhChiNhanhController.cs`; FE service `services/order/cau-hinh-chi-nhanh.service.ts`, page `pages/caidat-hethong/configsystem/config-order-chi-nhanh/`.
