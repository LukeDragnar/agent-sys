---
controller: CauHinhController
module: cua-hang
service: PreOrder
man_hinh_ui: "Cửa hàng / PreOrder — màn 'Cấu hình PreOrder' của khách hàng (đơn vị): lưu/đọc cấu hình khởi tạo PreOrder cho đơn vị"
muc_dich: "Quản lý cấu hình service PreOrder theo đơn vị: lấy cấu hình chung, lưu/đọc cấu hình khởi tạo (init), và endpoint private cho service khác đọc config."
alias: ["cấu hình preorder", "cấu hình đặt trước", "cài đặt PreOrder", "config preorder đơn vị"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [PreOrder]
cross_module_in: [service khác gọi PrivateGetConfig để đọc cấu hình PreOrder của đơn vị]
cross_module_out: [cấu hình init PreOrder dùng cho luồng đặt trước của đơn vị]
---

# CauHinhController — Cấu hình service PreOrder theo đơn vị

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình (chung) | load cấu hình | GET `api/CauHinh/Get` | `[SBAuthorize]` rỗng (chỉ cần đăng nhập) | public | màn cấu hình | `GetAsync(CurrentUser)` — trả cấu hình PreOrder theo đơn vị user | EF | - |
| 2 | Lưu cấu hình khởi tạo | "Lưu cấu hình" | POST `api/CauHinh/SaveCauHinhInit` | `CauHinh_PreOrder_LuuCauHinhKhachHang` | public | nút Lưu | ModelState invalid → trả lỗi model; ok → `SaveCauHinhInit(inSetting, CurrentUser)` lưu cấu hình init cho đơn vị | EF | - |
| 3 | Lấy cấu hình khởi tạo | load init | GET `api/CauHinh/GetCauHinhInit` | `[SBAuthorize]` rỗng | public | màn cấu hình | `GetCauHinhInit(CurrentUser)` — trả cấu hình init theo đơn vị | EF | - |

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize()]`] **`PrivateGetConfig`** (POST) — service khác đọc cấu hình PreOrder; gọi cùng `GetAsync(CurrentUser)` như endpoint `Get` (CurrentUser lấy từ token private). Trả cấu hình theo đơn vị.

## Quan hệ
- **Gọi sang:** không (thuần `ICauHinhService` → EF).
- **Dữ liệu (entities):** `CauHinh` (cấu hình service PreOrder + cấu hình init theo đơn vị).
- **Ripple / đồng bộ:** cấu hình init điều khiển hành vi khởi tạo PreOrder của đơn vị; service khác đọc qua `PrivateGetConfig`.

## Lỗi đã biết / lưu ý kỹ thuật
- **`Get` và `PrivateGetConfig` gọi cùng `GetAsync(CurrentUser)`** — chỉ khác mức auth (`[SBAuthorize]` rỗng vs `[PrivateSBAuthorize]`).
- **`Get`/`GetCauHinhInit` dùng `[SBAuthorize]` rỗng** (không permission key) → bất kỳ user đăng nhập của đơn vị đọc được; chỉ `SaveCauHinhInit` có key `CauHinh_PreOrder_LuuCauHinhKhachHang` (role `DV-AD-TONG`).
- Code thật: BE `PreOrder/PreOrder.API/Controllers/CauHinhController.cs`, service `PreOrder/PreOrder.Service/Generated/CauHinhService.cs`.
