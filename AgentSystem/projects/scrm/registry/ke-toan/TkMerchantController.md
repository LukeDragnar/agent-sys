---
controller: TkMerchantController
module: ke-toan
service: WebhookBank
man_hinh_ui: "nội bộ — service-to-service (private). CRUD tài khoản merchant ngân hàng (Payment.API)."
muc_dich: "CRUD tài khoản Merchant (UserName/SecretKey kết nối cổng thanh toán MB) — toàn bộ endpoint dùng PrivateSBAuthorize (gọi nội bộ service-to-service)."
alias: ["tài khoản merchant", "merchant MB", "cấu hình merchant thanh toán"]
auth: internal
state_machine: []
deprecated: false
entities: [TkMerchant]
stored_procs: []
depends: [TkMerchantService]
cross_module_in: [service nội bộ (Payment/QrcodeBank lấy user merchant)]
cross_module_out: []
---

# TkMerchantController — CRUD tài khoản Merchant (private)

> CRUD chuẩn cho `TkMerchant` (chứa `UserName`/`SecretKey` kết nối cổng thanh toán). **Tất cả 5 endpoint dùng `[PrivateSBAuthorize]`** → chỉ gọi được service-to-service bằng token private (`internal`). Có chặn xóa tài khoản hệ thống `UserName=="MB_PSALE"`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo Id | (private) | `GET api/TkMerchant/TimTheoId/{id:Guid}` | (`[PrivateSBAuthorize]`) | internal | — | `GetByIdAsync` → map `TkMerchantDto`. | **EF** `TkMerchant` | — |
| 2 | Thêm mới | (private) | `POST api/TkMerchant/ThemMoi` | (`[PrivateSBAuthorize]`) | internal | ModelState valid | `InsertAsync(model)`. | **EF** `TkMerchant` | — |
| 3 | Cập nhật | (private) | `POST api/TkMerchant/CapNhatThongTin` | (`[PrivateSBAuthorize]`) | internal | Id không rỗng + tồn tại | Kiểm tra tồn tại → `UpdateAsync`. | **EF** `TkMerchant` | — |
| 4 | Xóa | (private) | `POST api/TkMerchant/Xoa/{id:Guid}` | (`[PrivateSBAuthorize]`) | internal | tồn tại & `UserName != "MB_PSALE"` | Tồn tại → xóa; nếu `UserName=="MB_PSALE"` → chặn ("Không thể xóa"). | **EF** `TkMerchant` | Chặn xóa hardcode theo UserName `MB_PSALE` (L107). |
| 5 | Tìm theo điều kiện | (private) | `POST api/TkMerchant/TimTheoDieuKien` | (`[PrivateSBAuthorize]`) | internal | — | `TimTheoDieuKienAsync(gridInfo)`. | **EF** `TkMerchant` | — |

## Quan hệ
- **Gọi sang:** `TkMerchantService` (CRUD).
- **Dữ liệu (entities):** `TkMerchant` (UserName/SecretKey cổng thanh toán MB).
- **Ripple / đồng bộ:** `QrcodeBankController` đọc `TkMerchant` (`GetUserMerchant`) để tạo QR — sửa/xóa merchant ảnh hưởng luồng tạo QR.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/Payment.API/Controllers/TkMerchantController.cs:30,49,68,98,123` — toàn bộ `[PrivateSBAuthorize]` → internal, không phơi ra public.
- L107 — chặn xóa hardcode `UserName=="MB_PSALE"` (TK hệ thống). Entity chứa `SecretKey` → tránh log/expose qua DTO trả về.
