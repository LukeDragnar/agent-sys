---
controller: DonHangPreController
module: cua-hang
service: PreOrder
man_hinh_ui: "Cửa hàng / PreOrder — màn 'Danh sách đặt trước' (admin chi nhánh xem các đơn khách đặt qua QR: lọc theo ngày/trạng thái/chỗ ngồi/keyword, xem chi tiết, xóa)"
muc_dich: "Quản lý phía admin danh sách đơn đặt trước (PreOrder) do khách tạo qua QR: tìm theo bộ lọc, xem chi tiết theo mã, xóa mềm theo danh sách id."
alias: ["danh sách đặt trước", "đơn pre-order", "quản lý preorder", "đơn khách đặt qua QR", "danh sách PreOrder"]
auth: public
state_machine: [TrangThai_0_ChuaXuLy, TrangThai_1_DaTaoDonHang, TrangThai_2_DaThanhToan]
deprecated: false
entities: [DonHangPre, DonHangPreSanPham, DonHangPreSanPhamDinhKem]
stored_procs: []
depends: [PreOrder]
cross_module_in: [PrivateDonHangPreController — Order/thungan gọi GetFullByMaPreOrder/MarkImported khi nhập preorder vào đơn; PublicPreOrderController — khách Save/GetByMaPreOrder]
cross_module_out: [Order/thungan (nhập preorder thành đơn hàng → MarkImported set TrangThai=1); JobClearDonHangPreExpired (xóa đơn TrangThai=0 quá hạn)]
---

# DonHangPreController — Danh sách đơn đặt trước (PreOrder) phía admin

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (list) | "Tìm kiếm" / load grid | POST `api/DonHangPre/TimTheoDieuKien` | `DonHangPre_TimTheoDieuKien` | public | màn list | Lọc theo DonViId user + khoảng `TuNgay..DenNgay`; **role gating chi nhánh**: nếu user KHÔNG phải `DV-AD-TONG` → ép `IdChiNhanh == userLogin.IdChiNhanh` (chỉ thấy CN mình); nếu AD-TONG và filter.IdChiNhanh có → lọc theo đó; thêm lọc Keyword (MaPreOrder.Contains), TrangThai, IdChoNgoi; sort theo Sorts hoặc NgayTao; phân trang (Skip/Take); trả total | EF | - |
| 2 | Xem chi tiết theo mã | mở chi tiết đơn | GET `api/DonHangPre/GetByMaPreOrder?maPreOrder=` | `DonHangPre_GetChiTiet` | public | per-row | Trả `DonHangPreFullDto` (đơn + list sản phẩm + đính kèm) theo MaPreOrder; không thấy → "Không tìm thấy mã PreOrder!". **Không filter theo đơn vị/chi nhánh** (chỉ theo mã) | EF (BuildFullDto: join SanPham + DinhKem) | Không check DonVi/ChiNhánh → biết mã là xem được đơn bất kỳ |
| 3 | Xóa (mềm) theo danh sách id | "Xóa" (chọn nhiều + confirm) | POST `api/DonHangPre/Delete` | `DonHangPre_Xoa` | public | chọn ≥1 dòng | Ids rỗng → lỗi; lấy đơn theo (Ids + DonViId user); nếu KHÔNG `DV-AD-TONG` → lọc thêm `IdChiNhanh == user.IdChiNhanh`; set `IsDelete=true` (xóa **mềm**, MarkDirty); update batch | EF | Xóa mềm — khác BangQRCode (xóa cứng) |

## Endpoint nội bộ (private / service-to-service)
- Các thao tác private (GetFullByMaPreOrder, MarkImported) nằm ở **PrivateDonHangPreController** (node riêng) — dùng `IDonHangPreService` chung. Public (Save/GetByMaPreOrder cho khách) nằm ở **PublicPreOrderController**.

## Quy tắc nghiệp vụ (role gating chi nhánh — TimTheoDieuKien & Delete)
1. Tính `isAdTong = RoleCodes chứa DV_AD_TONG`.
2. **TimTheoDieuKien**: `isAdTong=false` → BẮT BUỘC `IdChiNhanh == user.IdChiNhanh` (bỏ qua filter.IdChiNhanh). `isAdTong=true` → optional filter.IdChiNhanh.
3. **Delete**: sau khi lấy theo (Ids + DonViId), nếu `isAdTong=false` → lọc tiếp `IdChiNhanh == user.IdChiNhanh` ở memory (đơn CN khác bị bỏ qua, không xóa).
- **Trạng thái** (TrangThai): `0`=chưa xử lý (đơn khách mới đặt) · `1`=đã nhập thành đơn hàng (MarkImported) · `2`=đã thanh toán (MarkThanhToan). Job clear chỉ xóa `TrangThai=0` quá hạn.

## Quan hệ
- **Gọi sang:** không gọi service ngoài (controller này thuần đọc/ghi EF qua `IDonHangPreService`).
- **Dữ liệu (entities):** `DonHangPre` (MaPreOrder, TrangThai, IdDonVi, IdChiNhanh, IdChoNgoi, TenKhachHang, SoDienThoai, IdKhachHang, IdDonHang, MaDonHang, IsDelete), `DonHangPreSanPham`, `DonHangPreSanPhamDinhKem`.
- **Ripple / đồng bộ:** đơn ở đây do **PublicPreOrderController.Save** tạo (khách quét QR); Order/thungan nhập đơn → `MarkImported` set TrangThai=1; `JobClearDonHangPreExpired` (cron 2h sáng) xóa cứng đơn TrangThai=0 quá `SoNgayLuuDonHangPre` ngày (mặc định 30).

## Luồng xuyên module (đặt trước → nhập thành đơn)
```
Khách quét QR → public/Save (PublicPreOrderController) → DonHangPre TrangThai=0
  → Admin xem ở DonHangPre/TimTheoDieuKien (list theo CN)
  → Order/thungan gọi PrivateDonHangPre/GetFullByMaPreOrder (chặn nếu TrangThai 1/2)
      → nhập sản phẩm vào đơn hàng → MarkImported → TrangThai=1 (+ IdDonHang/MaDonHang)
  → đơn TrangThai=0 không dùng → Job clear sau N ngày
```

## Lỗi đã biết / lưu ý kỹ thuật
- **GetByMaPreOrder (UI) không filter đơn vị/chi nhánh** (`DonHangPreService:199`) — chỉ tra theo MaPreOrder; ai có quyền `DonHangPre_GetChiTiet` + biết mã đều xem được (khác bản Private có check idDonVi/idChiNhanh).
- **Tất cả query `TableWithNolock()`** (đọc dirty) — list/chi tiết có thể đọc bản chưa commit.
- **Lọc chi nhánh khi Delete làm ở memory** (sau khi đã `ToListAsync`) — đúng kết quả nhưng kéo về cả đơn CN khác rồi mới loại.
- Sort qua `OrderByExtension(filter.Sorts)` (dynamic) — cần Sorts hợp lệ, mặc định OrderBy NgayTao tăng dần.
- Code thật: BE `PreOrder/PreOrder.API/Controllers/DonHangPreController.cs`, service `PreOrder/PreOrder.Service/Generated/DonHangPreService.cs`, entity `PreOrder.DataContext/Entity/DonHangPre.cs`.
