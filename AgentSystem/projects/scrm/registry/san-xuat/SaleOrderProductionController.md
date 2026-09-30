---
controller: SaleOrderProductionController
module: san-xuat
service: SanXuat
man_hinh_ui: "Sản xuất > Yêu cầu sản xuất (Production Request / PR) — tạo yêu cầu sản xuất từ dòng đơn đặt hàng, là đầu vào để tách thành Work Order"
muc_dich: "Quản lý Yêu cầu sản xuất (Production Request = entity SaleOrderProduction): tạo PR từ các dòng đơn đặt hàng (set chi tiết đơn TrangThai=2), cập nhật SoLuongSanXuat/LotSize, xóa PR (reset trạng thái dòng đơn). PR là bước trung gian trước khi WorkOrderController tách thành các Work Order."
alias: ["yêu cầu sản xuất", "production request", "PR", "SaleOrderProduction", "tạo product request", "lệnh yêu cầu sản xuất"]
auth: public                                   # controller [SBAuthorize]; phần lớn endpoint có key riêng; ThemMoi/TimTheoId bị comment (disable), LichSu để rỗng
state_machine: []                              # PR không có state riêng; điều khiển TrangThaiPR của đơn + TrangThai dòng đơn (0↔2)
deprecated: false
entities: [SaleOrderProduction, SaleOrderProductionDonHang]
stored_procs: []
depends: [SanXuat, IGenCodeService, BomSetup (đọc LotSize/KichThuoc), SaleOrderDonHangChiTiet]
cross_module_in: []
cross_module_out: [WorkOrderController (tiêu thụ SaleOrderProduction qua InsertAsyncV2 để tách WO)]
---

# SaleOrderProductionController — Yêu cầu sản xuất (Production Request)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo PR (Product Request) | Tạo Product Request | POST `api/SaleOrderProduction/InsertProductRequest` | `SaleOrderProduction_InsertProductRequest` (AD-TONG/AD-CN/SANXUAT) | public | từ dòng đơn đặt hàng | Sinh Ma; tạo `SaleOrderProduction` từ dòng đơn (SoLuongYeuCau, SoLuongSanXuat, HanXuLy, IsTachDon, IdDonViTinhSanXuat); LotSize/KichThuoc lấy từ **BomSetup**; link `SaleOrderProductionDonHang`; set dòng đơn `SaleOrderDonHangChiTiet.TrangThai=2`; transaction | EF | InsertProductRequest (SaleOrderProductionService.cs:222) |
| 2 | Sửa PR | Lưu | POST `api/SaleOrderProduction/CapNhatThongTin` | `SaleOrderProduction_CapNhatThongTin` | public | PR tồn tại | Update `SoLuongSanXuat`, `LotSize` | EF | UpdateAsync (:111) |
| 3 | Xóa PR | Xóa | POST `api/SaleOrderProduction/Xoa/{id}` | `SaleOrderProduction_Xoa` | public | - | Xóa PR + các link `SaleOrderProductionDonHang`; **reset dòng đơn `TrangThai` về 0** | EF | DeleteAsync (:49) |
| 4 | Tìm theo điều kiện | grid lọc | POST `api/SaleOrderProduction/TimTheoDieuKien` | `SaleOrderProduction_TimTheoDieuKien` | public | màn list | Phân trang PR theo filter + CurrentUser | EF | TimTheoDieuKienAsync |
| 5 | Lịch sử theo điều kiện | lịch sử | POST `api/SaleOrderProduction/TimTheoDieuKienLichSu` | `SBAuthorize()` (chỉ đăng nhập) | public | - | Tìm lịch sử PR theo filter | EF | key comment, để rỗng |
| — | (disabled) Thêm/Xem chi tiết | — | ThemMoi · TimTheoId/{id} | — | — | — | Code controller bị **comment toàn bộ** (không expose) | — | controller L33-55 — không phải endpoint hoạt động |

## Quan hệ
- **Gọi sang:** IGenCodeService (sinh Ma PR); đọc `BomSetup` để lấy LotSize/KichThuoc; đọc `SaleOrderDonHangChiTiet`.
- **Dữ liệu (entities):** `SaleOrderProduction {Ma, SoLuongYeuCau, SoLuongSanXuat, LotSize, KichThuoc, HanXuLy, IsTachDon, IdDonViTinhSanXuat}`, link `SaleOrderProductionDonHang`.
- **Ripple / đồng bộ:** tạo PR → set dòng đơn `TrangThai=2` (đang sản xuất); xóa PR → reset về `0`. KHÔNG tự sinh WorkOrder — WO tạo sau bởi `WorkOrderController.ThemMoi` (InsertAsyncV2) tiêu thụ PR này (set `IsTachDon=true`). Không RabbitMQ.

## Luồng xuyên module
SaleOrderDonHang (dòng đơn) → `InsertProductRequest` tạo PR (dòng `TrangThai=2`, đơn `TrangThaiPR=1`) → `WorkOrderController.ThemMoi` tách PR thành các WorkOrder → vòng đời WO.

## Lỗi đã biết / lưu ý kỹ thuật
- `ThemMoi`/`TimTheoId` bị comment hoàn toàn (controller L33-55) → chỉ còn `InsertProductRequest` là đường tạo PR thật.
- `using MassTransit.Internals;` thừa (SaleOrderProductionService.cs:10) — KHÔNG có publish/consume RabbitMQ thực.
- Xóa PR reset dòng đơn về TrangThai=0 (SaleOrderProductionService.cs:74) — đảm bảo dòng đơn quay lại trạng thái có thể tạo PR lại.
