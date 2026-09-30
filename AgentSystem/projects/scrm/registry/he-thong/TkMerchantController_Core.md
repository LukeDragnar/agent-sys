---
controller: TkMerchantBaseController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Quản lý Merchant (admin): CRUD tài khoản merchant ngân hàng (UserName/SecretKey cổng thanh toán)"
muc_dich: "CRUD tài khoản Merchant ngân hàng (cấu hình kết nối cổng thanh toán) — quản trị cấp hệ thống (HT-AD)."
alias: ["TkMerchant", "TkMerchantBase", "quản lý merchant", "tài khoản merchant", "merchant ngân hàng"]
auth: internal
state_machine: []
deprecated: false
entities: [TkMerchant]
stored_procs: []
depends: ["ITkMerchantBaseService", "BaseController"]
cross_module_in: ["FE Hệ thống/Quản lý Merchant (admin) gọi TimTheoDieuKien/TimTheoId/ThemMoi/CapNhatThongTin/Xoa"]
cross_module_out: ["Cấu hình merchant dùng để kết nối cổng thanh toán; trùng tên với TkMerchantController (service WebhookBank/Payment.API) ở module ke-toan — KHÁC controller"]
---

# TkMerchantBaseController — Quản lý Merchant (Core)

⚠️ File `TkMerchantController.cs` nhưng **class thật là `TkMerchantBaseController`** (route `api/QuanTriArea/TkMerchantBase`, Area SanboxArea). Đây là controller service **Core** — KHÁC với `TkMerchantController` (service WebhookBank/Payment.API) ở `registry/ke-toan/TkMerchantController.md`. Vì vậy node này dùng suffix `_Core`.

CRUD merchant kế thừa `BaseController`, mọi method truyền `CurrentUser` xuống service (scope theo user). Controller-level KHÔNG có `[SBAuthorize]` rỗng (chỉ `[TienIch(..., TI_HeThong, true)]`); từng method gắn key `TkMerchantBase_*` (HT-AD) → auth = internal/admin.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/QuanTriArea/TkMerchantBase/TimTheoDieuKien` | `TkMerchantBase_TimTheoDieuKien` (HT-AD) | internal | — | Lọc merchant theo `TkMerchantFilterDto` + CurrentUser. | EF | — |
| 2 | Xem chi tiết | (mở form) | GET `api/QuanTriArea/TkMerchantBase/TimTheoId/{id:Guid}` | `TkMerchantBase_TimTheoId` (HT-AD) | internal | — | `GetByIdAsync(id, CurrentUser)`. | EF | Cả nhánh data!=null và null đều trả `ResultWithData(item)` (không trả NotFound rõ ràng) |
| 3 | Thêm mới | Thêm | POST `api/QuanTriArea/TkMerchantBase/ThemMoi` | `TkMerchantBase_TimTheoId` (HT-AD) | internal | — | ModelState valid → `InsertAsync(model, CurrentUser)`. | EF | Key trùng `TimTheoId` (gắn nhầm `TkMerchantBase_TimTheoId` thay vì `_ThemMoi`) — dòng 49 |
| 4 | Cập nhật | Sửa | POST `api/QuanTriArea/TkMerchantBase/CapNhatThongTin` | `TkMerchantBase_CapNhatThongTin` (HT-AD) | internal | — | Valid + `Id` không rỗng + tồn tại → `UpdateAsync(model, CurrentUser)`. | EF | — |
| 5 | Xóa | Xóa | POST `api/QuanTriArea/TkMerchantBase/Xoa/{id:Guid}` | `TkMerchantBase_Xoa` (HT-AD) | internal | — | Kiểm tra tồn tại → `DeleteAsync(id, CurrentUser)`. | EF | — |

## Quan hệ
- **Gọi sang:** ITkMerchantBaseService (EF, mọi method nhận CurrentUser).
- **Dữ liệu (entities):** `TkMerchant` (DTO `TkMerchantDto` từ `Models.Core.DTO.BankReceive.Client`).
- **Ripple / đồng bộ:** cấu hình merchant phục vụ kết nối cổng thanh toán; không bắn event.

## Lỗi đã biết / lưu ý kỹ thuật
- `TkMerchantController.cs:49` ThemMoi gắn nhầm key `TkMerchantBase_TimTheoId` (đáng lẽ `_ThemMoi`) → phân quyền/audit có thể sai.
- `TkMerchantController.cs:36-40` TimTheoId cả hai nhánh đều `ResultWithData(item)` → không phân biệt found/not-found rõ.
- Tên file ≠ tên class (`TkMerchantController.cs` → `TkMerchantBaseController`); và trùng tên file với controller WebhookBank ở ke-toan → node này suffix `_Core`.
