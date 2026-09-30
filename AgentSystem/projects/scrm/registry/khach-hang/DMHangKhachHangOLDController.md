---
controller: DMHangKhachHangOLDController
module: khach-hang
service: Customer
man_hinh_ui: "nội bộ — không có UI (controller đã comment toàn bộ, không build)"
muc_dich: "Bản CŨ của danh mục hạng khách hàng (đơn vị + super admin) — đã thay bằng DMHangKhachHangController"
alias: ["DM Hạng KH (cũ)", "DMHangKhachHangOLD"]
auth: public
state_machine: []
deprecated: true
entities: [DMHangKhachHangOLD]
stored_procs: []
depends: [IDmHangKhachHangOLDService, BaseDanhMucController]
cross_module_in: []
cross_module_out: []
---

# DMHangKhachHangOLDController — Danh mục hạng khách hàng (BẢN CŨ, deprecated)

> **DEPRECATED.** Toàn bộ file đã bị **comment cứng** (mọi dòng prefix `//`) → controller không được compile/đăng ký route, không có endpoint hoạt động. Đã thay bằng `DMHangKhachHangController`. Giữ lại đây để AI nhận diện và bỏ qua nhanh.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | — | — | Controller comment toàn bộ → 0 endpoint live | — | Toàn bộ file commented |

> Tham khảo các endpoint cũ (đã vô hiệu) trong source: `TimTheoDieuKien`, `GetAllRankByDonVi`, `Xoa/{id}`, `TimTheoDieuKienSuperAdminAsync`, `InsertSuperAdmin`, `UpdateSuperAdmin`, `KhoiTaoMacDinh` (super admin). Tất cả không còn hoạt động.

## Quan hệ
- **Gọi sang:** `IDmHangKhachHangOLDService` (kế thừa `BaseDanhMucController<DMHangKhachHangOLD, DmHangKhachHangOLDDto>`) — không còn được wire.
- **Dữ liệu (entities):** `DMHangKhachHangOLD`.
- **Ripple / đồng bộ:** không (deprecated).

## Lỗi đã biết / lưu ý kỹ thuật
- `DMHangKhachHangOLDController.cs:1-107` — **toàn bộ file commented out**; bản hiện hành là `DMHangKhachHangController`. Bỏ qua khi triage.
