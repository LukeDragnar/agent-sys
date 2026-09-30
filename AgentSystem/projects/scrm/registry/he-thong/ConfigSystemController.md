---
controller: ConfigSystemController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI riêng; phục vụ app/user thường đọc config hệ thống (vd màn QlThuNgan đọc TK ngân hàng, app đọc Google Map key)"
muc_dich: "Đọc cấu hình hệ thống cho user thường / app công khai (biến thể non-Area): lấy config app (Google Map key, anonymous), lấy địa chỉ bank SB, tìm config theo mã — không cần quyền super admin như QuanTriArea."
alias: ["ConfigSystem", "config app", "cấu hình app", "đọc config", "Google Map key"]
auth: mixed
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: ["IConfigSystemService"]
cross_module_in: ["App/FE gọi GetConfigApp (anonymous) lấy Google Map key", "Màn user (QlThuNgan...) gọi TimTheoMa/GetDiaChiBankSB đọc config"]
cross_module_out: ["Module thanh toán/thu ngân (đọc TK ngân hàng HT_TK_NGANHANG)"]
---

# ConfigSystemController (non-Area) — Đọc cấu hình hệ thống cho user thường / app

Biến thể **non-Area** `Core.API.Controllers`, route `[Route("api/[controller]")]` → path gốc `/api/ConfigSystem/...`. Kế thừa `BaseController` (không phải `SBBaseController`). KHÔNG có `[SBAuthorize]` ở controller-level — auth đặt theo từng action: `GetConfigApp` là `[AllowAnonymous]`, còn lại `[SBAuthorize]`. Đây là biến thể đọc-only dành cho user thường/app; biến thể quản trị đầy đủ (CRUD, super admin) ở `ConfigSystemController_Core.md` (route `api/QuanTriArea/ConfigSystem`).

File: `Core/Core.API/Controllers/ConfigSystemController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config app | (không UI — app/FE load) | GET `/api/ConfigSystem/GetConfigApp` | — | anonymous (`[AllowAnonymous]`) | — | `GetGoogleMapKey()` → trả config app (Google Map key...) cho app công khai | EF | Anonymous → key map lộ public (chấp nhận vì là client key) |
| 2 | Lấy địa chỉ/TK ngân hàng SB | (màn thu ngân/thanh toán) | GET `/api/ConfigSystem/GetDiaChiBankSB` | — (`[SBAuthorize]` trống) | internal | — | `GetValueByCode(ConfigCode.HT_TK_NGANHANG)` → string TK ngân hàng | EF | — |
| 3 | Tìm config theo mã | (màn user đọc config) | GET `/api/ConfigSystem/TimTheoMa?ma=` | — (`[SBAuthorize]` trống) | internal | — | `TimTheoMaAsync(ma)` → 1 config theo mã, cho user thường (không cần quyền super admin như QuanTriArea) | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [anonymous] `GetConfigApp` → app/FE load Google Map key trước khi đăng nhập.
- [internal] `TimTheoMa` / `GetDiaChiBankSB` → màn user (vd QlThuNgan) đọc config hệ thống không qua quyền super admin.

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (GetGoogleMapKey, GetValueByCode, TimTheoMaAsync).
- **Dữ liệu (entities):** `ConfigSystem`.
- **Ripple / đồng bộ:** chỉ đọc; không ghi → không sinh side-effect dữ liệu. Giá trị config (vd TK ngân hàng) được màn thanh toán/thu ngân dùng.

## Lỗi đã biết / lưu ý kỹ thuật
- Trùng tên class `ConfigSystemController` với biến thể QuanTriArea (`Areas/SanboxArea/ConfigSystemController.cs`) → 2 controller cùng tên ở 2 namespace, route khác nhau (`api/ConfigSystem` vs `api/QuanTriArea/ConfigSystem`); dễ nhầm khi tra cứu/route.
- `ConfigSystemController.cs:20-25` (non-Area) — `GetConfigApp` `[AllowAnonymous]`: chỉ nên trả client-side key, không trả secret server-side.
- Phân tách trách nhiệm: biến thể này đọc-only (3 GET), CRUD nằm ở biến thể Area.
