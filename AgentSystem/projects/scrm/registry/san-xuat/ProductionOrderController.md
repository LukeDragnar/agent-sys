---
controller: ProductionOrderController
module: san-xuat
service: SanXuat
man_hinh_ui: "Sản xuất > Quản lý danh mục > Production Order — CRUD lệnh/yêu cầu sản xuất (entity ProductionOrder); có dấu hiệu là entity legacy/song song với SaleOrderProduction"
muc_dich: "CRUD ProductionOrder (Ma, Ten, SoLuongYeuCau, SoLuongSanXuat, HanXuLy, IsTachDon). Là entity sản xuất riêng, có vẻ legacy/song song với luồng SaleOrderProduction (Production Request)."
alias: ["production order", "ProductionOrder", "lệnh sản xuất (entity cũ)", "đơn sản xuất"]
auth: public                                   # controller [ApiController]; CRUD đều có key riêng
state_machine: []
deprecated: false                              # không khóa cứng, nhưng nghi legacy — cần xác nhận còn dùng
entities: [ProductionOrder]
stored_procs: []
depends: [SanXuat]
cross_module_in: []
cross_module_out: []
---

# ProductionOrderController — Production Order (CRUD, nghi legacy)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm | Thêm ProductionOrder | POST `api/ProductionOrder/ThemMoi` | `ProductionOrder_ThemMoi` (AD-TONG/AD-CN/SANXUAT) | public | - | ModelState lỗi → validate; map DTO→entity; InsertAsync | EF | - |
| 2 | Sửa | Chỉnh sửa thông tin | POST `api/ProductionOrder/CapNhatThongTin` | `ProductionOrder_CapNhatThongTin` | public | tồn tại | Map DTO→entity; GetById null → "Không tìm thấy"; UpdateAsync | EF | ProductionOrderService.cs:31 |
| 3 | Xóa | Xóa | POST `api/ProductionOrder/Xoa/{id}` | `ProductionOrder_Xoa` | public | - | GetById null → "Không tìm thấy"; DeleteAsync → true | EF | - |
| 4 | Xem chi tiết | mở | GET `api/ProductionOrder/TimTheoId/{id}` | `ProductionOrder_TimTheoId` | public | - | Trả 1 ProductionOrder theo id | EF | - |
| 5 | Tìm theo điều kiện | grid lọc | POST `api/ProductionOrder/TimTheoDieuKien` | `ProductionOrder_TimTheoDieuKien` | public | màn list | Phân trang ProductionOrder theo filter | EF | ProductionOrderService.cs:42 |

## Quan hệ
- **Gọi sang:** không (CRUD thuần, không gọi Warehouse, không messaging).
- **Dữ liệu (entities):** `ProductionOrder {Ma, Ten, SoLuongYeuCau, SoLuongSanXuat, HanXuLy, IsTachDon}`.
- **Ripple / đồng bộ:** không. Không tham gia luồng WorkOrder hiện hành (WorkOrder tách từ `SaleOrderProduction`, không từ `ProductionOrder`).

## Lỗi đã biết / lưu ý kỹ thuật
- Cấu trúc field gần trùng `SaleOrderProduction` (SoLuongYeuCau/SoLuongSanXuat/HanXuLy/IsTachDon) nhưng **không được luồng WO tiêu thụ** → nghi entity legacy/song song. Cần xác nhận màn này còn dùng thực tế không trước khi sửa/triage.
