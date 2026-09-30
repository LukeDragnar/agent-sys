---
name: project-orderlogistic-giaohang-ghichu-fix
description: Fix GiaoHangTrangThaiGhiChu bị overwrite mỗi lần DVVC push — chuyển sang log LichSuTacNghiep.GhiChu
metadata:
  type: project
---

Fix ngày 2026-07-01 trong `ReceiveStatusOrder` — `DeliveryUnitConnect/OrderService.cs`.

**Vấn đề:** `DonHangLogistic.GiaoHangTrangThaiGhiChu` là field đơn lẻ, mỗi lần DVVC webhook push trạng thái lại overwrite → chỉ giữ được ghi chú của trạng thái cuối cùng, các trạng thái trước mất hết. Cột Ghi chú trong màn "Lịch sử cập nhật trạng thái giao hàng" luôn trống vì `AutoGenerateGhiChu` overwrite thành empty (`tuDongSinhGhiChu=true` + block `CapNhatTrangThaiGiaoHang` rỗng).

**Fix:**
1. Bỏ set `donHangDb.GiaoHangTrangThaiGhiChu` + bỏ `MarkDirty` → không persist vào DB nữa
2. `ghiChuThayDoi = reqBody.ReasonUpdate` (cắt 500 ký tự) → truyền vào `LichSuTacNghiepLog`
3. `tuDongSinhGhiChu: false` → giữ nguyên `ghiChuThayDoi`, không bị AutoGenerate overwrite

**Why:** Ghi chú theo trạng thái DVVC là per-status (nhiều lần), không phải single field trên đơn.

**How to apply:** Mọi field dạng "ghi chú/note theo sự kiện" — nếu 1 đơn có nhiều event → không save vào entity field, chỉ log vào `LichSuTacNghiep.GhiChu`. `AutoGenerateGhiChu` block `CapNhatTrangThaiGiaoHang` rỗng → safe để `tuDongSinhGhiChu=false` cho action này.

**Còn mở:** 2 TODO cũ trong method (phí VC + GiaoHangMotPhanCOD), chưa cherry-pick sang staging.
