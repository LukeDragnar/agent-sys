---
controller: ConfigHangKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "nội bộ — không có UI (controller đã comment toàn bộ, không build)"
muc_dich: "Bản CŨ cấu hình hạng khách hàng — đã vô hiệu; chức năng config hạng theo KH chuyển sang DMHangKhachHangController (GetCauHinhHangByIdKh)"
alias: ["Config hạng KH (cũ)", "ConfigHangKhachHang"]
auth: public
state_machine: []
deprecated: true
entities: [ConfigHangKhachHang]
stored_procs: []
depends: [IConfigHangKhachHangService, BaseDanhMucController]
cross_module_in: []
cross_module_out: []
---

# ConfigHangKhachHangController — Cấu hình hạng khách hàng (BẢN CŨ, deprecated)

> **DEPRECATED.** Toàn bộ file đã bị **comment cứng** (mọi dòng prefix `//`) → controller không compile/đăng ký route, không có endpoint hoạt động. Chức năng "get cấu hình hạng theo KH" nay nằm ở `DMHangKhachHangController.GetCauHinhHangByIdKh`. Giữ node để AI bỏ qua nhanh.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | — | — | Controller comment toàn bộ → 0 endpoint live | — | Toàn bộ file commented |

> Tham khảo các endpoint cũ (đã vô hiệu) trong source: `TimTheoDieuKien`, `Xoa/{id}`, `GetConfigHangByIdKh/{idKh}` (và 2 method `GetOptionHangKhachHang`/`UpdateHangOptions` cũng đã comment lồng). Tất cả không còn hoạt động.

## Quan hệ
- **Gọi sang:** `IConfigHangKhachHangService` (kế thừa `BaseDanhMucController<ConfigHangKhachHang, ConfigHangKhachHangDto>`) — không còn được wire.
- **Dữ liệu (entities):** `ConfigHangKhachHang`.
- **Ripple / đồng bộ:** không (deprecated).

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigHangKhachHangController.cs:1-81` — **toàn bộ file commented out**; thay thế: `DMHangKhachHangController.GetCauHinhHangByIdKh`. Bỏ qua khi triage.
