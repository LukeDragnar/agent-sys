---
controller: PrivateDonHangPreController
module: cua-hang
service: PreOrder
man_hinh_ui: "nội bộ — không có UI (service-to-service: Order/thungan nhập đơn đặt trước thành đơn hàng)"
muc_dich: "Endpoint private cho Order/thungan: lấy đầy đủ đơn PreOrder theo mã (có gating theo ĐV/CN + trạng thái) và đánh dấu đã nhập thành đơn hàng."
alias: ["nhập preorder vào đơn", "lấy đơn đặt trước cho thu ngân", "mark imported preorder", "private preorder"]
auth: internal
state_machine: [TrangThai_0_ChuaXuLy, TrangThai_1_DaTaoDonHang, TrangThai_2_DaThanhToan]
deprecated: false
entities: [DonHangPre, DonHangPreSanPham, DonHangPreSanPhamDinhKem]
stored_procs: []
depends: [PreOrder]
cross_module_in: [Order/thungan — gọi GetFullByMaPreOrder khi thu ngân nhập mã preorder; gọi MarkImported sau khi tạo đơn hàng]
cross_module_out: [DonHangPre.TrangThai=1 + gắn IdDonHang/MaDonHang (liên kết đơn đặt trước ↔ đơn hàng thực)]
---

# PrivateDonHangPreController — Nhập đơn đặt trước thành đơn hàng (service-to-service)

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize]`] **`GetFullByMaPreOrder`** (GET, query `maPreOrder`,`idDonVi`,`idChiNhanh`) — Order/thungan gọi để load full đơn đặt trước trước khi nhập vào đơn hàng. Tra theo (MaPreOrder + idDonVi + idChiNhanh); không thấy → "Không tìm thấy mã PreOrder!"; **gating trạng thái**: nếu `TrangThai==1 || ==2` → "Mã PreOrder này đã được sử dụng!" (chặn nhập lại đơn đã dùng); ok → trả `DonHangPreFullDto` (đơn + SP + đính kèm).
- [internal — `[PrivateSBAuthorize]`] **`MarkImported`** (POST, body `DonHangPreMarkImportedDto`) — sau khi tạo đơn hàng từ preorder: tra theo MaPreOrder, set `TrangThai=1`, gắn `IdDonHang` + `MaDonHang` (MarkDirty từng field), update. Không thấy → "Không tìm thấy mã PreOrder!".

## Quy tắc nghiệp vụ (gating khi nhập)
1. **GetFull** filter chặt theo cả idDonVi + idChiNhanh (khác bản UI/public chỉ theo mã) → đúng tenant mới đọc được.
2. **Chặn dùng lại**: chỉ TrangThai=0 mới nhập được; 1 (đã tạo đơn) hoặc 2 (đã thanh toán) bị từ chối.
3. **MarkImported chỉ tra theo MaPreOrder** (không filter ĐV/CN) — khác GetFull; rủi ro nếu mã bị đoán (mức internal nên thấp).

## Quan hệ
- **Gọi sang:** không (thuần đọc/ghi EF qua `IDonHangPreService` — cùng service với `DonHangPreController`).
- **Dữ liệu (entities):** `DonHangPre` (TrangThai, IdDonHang, MaDonHang) + SP/đính kèm khi build full.
- **Ripple / đồng bộ:** `MarkImported` là cạnh ra quan trọng — liên kết đơn đặt trước với đơn hàng thực (IdDonHang/MaDonHang) và khóa preorder khỏi bị nhập lại; cũng tránh Job clear xóa (chỉ xóa TrangThai=0).

## Luồng xuyên module
```
Order/thungan: thu ngân nhập mã PreOrder 
  → GetFullByMaPreOrder(ma, idDonVi, idChiNhanh) [chặn nếu TrangThai 1/2]
  → tạo đơn hàng từ list SP đặt trước
  → MarkImported(ma, idDonHang, maDonHang) → DonHangPre.TrangThai=1
```

## Lỗi đã biết / lưu ý kỹ thuật
- **`[PrivateSBAuthorize]` cấp method**, controller không có `[TienIch]` (endpoint hạ tầng, không phải tiện ích UI) → không sinh permission key UI.
- **MarkImported không idempotent guard**: nếu gọi 2 lần với mã khác idDonHang sẽ ghi đè IdDonHang/MaDonHang (không kiểm tra đã imported chưa) — nhưng GetFull đã chặn nhập lại ở bước trước.
- **Query `TableWithNolock()`** (đọc dirty) ở cả GetFull lẫn MarkImported.
- `MarkThanhToan` (set TrangThai 2 hoặc 1 khi hủy) tồn tại trong service nhưng **không expose qua controller này** — gọi nội bộ từ luồng thanh toán/hủy khác.
- Code thật: BE `PreOrder/PreOrder.API/Controllers/Private/PrivateDonHangPreController.cs`, service `PreOrder/PreOrder.Service/Generated/DonHangPreService.cs` (GetFullByMaPreOrder/MarkImported).
