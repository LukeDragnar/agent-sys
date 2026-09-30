---
controller: VoucherLichSuController
module: marketing
service: Promotion
man_hinh_ui: "nội bộ — không có UI (toàn bộ endpoint đã comment; lịch sử voucher hiện xem qua VoucherController.LichSuSuDungVoucher)"
muc_dich: "Lịch sử sử dụng voucher (tra cứu theo voucher/khách hàng, download Excel) — HIỆN TOÀN BỘ ACTION ĐÃ BỊ COMMENT, controller rỗng."
alias: ["lịch sử voucher", "VoucherLichSu", "lịch sử sử dụng voucher"]
auth: internal
state_machine: []
deprecated: true
entities: [VoucherLichSu]
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# VoucherLichSu — Lịch sử sử dụng voucher (rỗng / vô hiệu)

## Hành động (UI / công khai)
> ⚠️ **Controller hiện KHÔNG có endpoint hoạt động** — cả 3 action (`TimTheoDieuKien_IdVoucher`, `TimTheoDieuKien_IdKhachHang`, `DownloadLichSu/{id}`) đều bị **comment** trong source. Class chỉ giữ DI `IVoucherLichSuService`. `[TienIchAttribute]` cũng comment.

| # | Hành động (đã comment) | Endpoint dự kiến | Hành vi (theo code cũ) | Trạng thái |
|---|---|---|---|---|
| 1 | Lịch sử theo voucher | POST `TimTheoDieuKien_IdVoucher` | `TimTheoDieuKien_IdVoucher(payload)` | commented |
| 2 | Lịch sử theo khách hàng | POST `TimTheoDieuKien_IdKhachHang` | `TimTheoDieuKien_IdKhachHang(payload)` | commented |
| 3 | Download lịch sử (Excel) | GET `DownloadLichSu/{id}` | Xuất `LichSuDungVoucher.xlsx` | commented |

## Quan hệ
- **Dữ liệu (entities):** `VoucherLichSu` (mỗi lần dùng voucher).
- **Thay thế hiện hành:** chức năng lịch sử voucher đang phục vụ qua `VoucherController.LichSuSuDungVoucher` (`Voucher_LichSuSuDungVoucher`).

## Lỗi đã biết / lưu ý kỹ thuật
- `VoucherLichSuController.cs:17-39` — toàn bộ action commented ⇒ **AI/triage bỏ qua nhanh** (deprecated). Nếu cần lịch sử voucher → dùng `VoucherController`.
- Service `IVoucherLichSuService` vẫn được inject nhưng không endpoint nào dùng → dead injection.
