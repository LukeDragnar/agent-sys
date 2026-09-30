# Dev — Quản lý sản xuất

> Service: **SanXuat** (BE .NET, `SanXuat.API` / `SanXuat.Service`) · FE module **sanxuat** (Angular 15 + ng-zorro). Chỉ dành cho dev.
> Trace registry: `registry/san-xuat/SaleOrderDonHangController.md`, `SaleOrderDonHangChiTietController.md`, `SaleOrderProductionController.md`, `WorkOrderController.md`, `InPhieuController.md`, `ProductionOrderController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component / folder |
|---|---|---|
| Đơn đặt hàng sản xuất | `/sanxuat/lenh-dathang` | `saleorder-donhang/` (list `saleorder-donhang.component`, edit `saleorder-donhang-edit`) |
| Lệnh sản xuất (Product Orders / PR split) | `/sanxuat/lenh-sanxuat` | `saleorder-production/production-order.component` |
| Lệnh công việc (Work Order) | `/sanxuat/lenh-lamviec` | `workorder/workorder.component` |
| Planner (điều hành bước) | `/sanxuat/lenh-plan` | `workorder-planner/` (`workorder-planner.component`, `workorder-planner-step`) |

- Route gốc module: `app-routing.module.ts:465` (`path: 'sanxuat'` → lazy `SanXuatModule`).
- Route con: `sanxuat-routing.module.ts` (lenh-dathang :7, lenh-sanxuat :11, lenh-lamviec :17, lenh-plan :21).
- Enum FE: `sanxuat/models/sanxuat-enum.ts` — `WOTrangThaiEnums {ChuaBatDau=0, ChuaXepLich=1, DaXepLich=2}`, `WOTrangThaiSanXuatEnums {ChuaBatDau=0, DangSanXuat=1, TamDung=2, Huy=3, HoanThanh=4}`.

## Endpoint / Controller (trace registry)
**Đơn đặt hàng** — `SaleOrderDonHangController` (controller `[SBAuthorize]`):
| Endpoint | Quyền | Mục đích |
|---|---|---|
| `POST api/SaleOrderDonHang/ThemMoi` | `SaleOrderDonHang_ThemMoi` (AD-TONG/AD-CN/SANXUAT) | tạo header+chi tiết, tính TongSoLuong |
| `POST .../CapNhatThongTin` | `_CapNhatThongTin` | update; **delete-all-reinsert chi tiết** |
| `POST .../Xoa/{id}` | `_Xoa` | id là **chi tiết**; hết chi tiết → xóa đơn |
| `POST .../LockSanPham` | `_LockSanPham` | set IsLock/SoLuongLock → upsert SanPhamLock |
| `POST .../DanhSach` · `.../TimTheoDieuKien` | `_DanhSach` / `_TimTheoDieuKien` | list (TimTheoDieuKien gọi Warehouse đọc tồn) |
| `POST .../TimTheoDieuKienLichSu` | `[SBAuthorize()]` rỗng | lịch sử đơn |

**Yêu cầu sản xuất (PR)** — `SaleOrderProductionController` (`[SBAuthorize]`):
| Endpoint | Quyền | Mục đích |
|---|---|---|
| `POST api/SaleOrderProduction/InsertProductRequest` | `_InsertProductRequest` (AD-TONG/AD-CN/SANXUAT) | tạo PR từ dòng đơn; LotSize/KichThuoc từ BomSetup; set ChiTiet.TrangThai=2 |
| `POST .../CapNhatThongTin` | `_CapNhatThongTin` | update SoLuongSanXuat, LotSize |
| `POST .../Xoa/{id}` | `_Xoa` | xóa PR + link; reset dòng đơn TrangThai=0 |
| `POST .../TimTheoDieuKien` · `.../TimTheoDieuKienLichSu` | `_TimTheoDieuKien` / `[SBAuthorize()]` rỗng | list / lịch sử |
| ~~`ThemMoi` · `TimTheoId`~~ | — | **comment toàn bộ** (disable, controller L33-55) |

**Lệnh công việc (WO)** — `WorkOrderController` (`[SBAuthorize]`): xem chi tiết 19 hành động ở `registry/san-xuat/WorkOrderController.md`. Trọng tâm:
| Endpoint | Quyền | Mục đích |
|---|---|---|
| `POST api/WorkOrder/ThemMoi` | `WorkOrder_ThemMoi` | tách PR → WO (InsertAsyncV2) |
| `POST .../ChuyenWOPlanner` | `_ChuyenWOPlanner` | vật chất hóa BOM → WorkOrderStep/Product |
| `POST .../BatDauWorkOrder` | `_BatDauWorkOrder` | xếp lịch |
| `POST .../TamDungWorkOrder` · `.../ChuyenVeChuaXepLich` · `.../BatDauSanXuat/{id}` · `.../HuySanXuat/{id}` · `.../CapNhatTrangThaiSanXuat` | **rỗng / chỉ đăng nhập** | điều khiển state |
| `POST .../LuuVaTaoPhieuXuatByStep` | `_TaoPhieuXuatByStep` | xuất NVL → Warehouse PhieuXuat (KieuLoaiXuat=2) |
| `POST .../LuuVaTaoPhieuNhapByStep` | `_TaoPhieuNhapByStep` | nhập thành phẩm → Warehouse PhieuNhap (MaDmPhanLoaiNhap=10); đẩy đầu ra sang bước kế |
| `POST .../HoanThanhWorkOrderStep/{id}` | `_HoanThanhWorkOrderStep` | hoàn thành bước; bước cuối → WO HoanThanh |
| `POST .../GetWOSanPhamCan` | `_GetWOSanPhamCan` | report NVL cần vs tồn kho |
| `POST .../Xoa/{id}` | `[SBAuthorize()]` rỗng | cascade delete; **KHÔNG hoàn phiếu kho** |

**In phiếu SX** — `InPhieuController` (`[SBAuthorize]`):
- `POST api/InPhieu/InPhieuNhapSX` (`InPhieu_PhieuNhapSX`) và `POST .../InPhieuXuatSX` (`InPhieu_PhieuXuatSX`) — **cả hai gọi cùng `IWorkOrderService.InsertAsyncV2`** (không tách nhánh nhập/xuất).

**Production Order (nghi legacy)** — `ProductionOrderController`: CRUD entity `ProductionOrder` riêng (`ThemMoi`/`CapNhatThongTin`/`Xoa`/`TimTheoId`/`TimTheoDieuKien`, đều có key `ProductionOrder_*`). Không tham gia luồng WO hiện hành.

## Entities
- `SaleOrderDonHang {Ma, TongSoLuong, TrangThaiPR(0/1)}` + `SaleOrderDonHangChiTiet {TrangThai(0/2), IsLock, SoLuongLock, IdSanPham, SoLuong}` + `SanPhamLock`.
- `SaleOrderProduction {Ma, SoLuongYeuCau, SoLuongSanXuat, LotSize, KichThuoc, HanXuLy, IsTachDon, IdDonViTinhSanXuat}` + link `SaleOrderProductionDonHang`.
- `WorkOrder {TrangThai, TrangThaiSanXuat, BuocIndex, IdBomSetup, IsTachDon, IsPlan}` + `WorkOrderStep {ThuTu, TrangThai, NgayXuat, NgayHoanThanh}` + `WorkOrderStepProduct {Loai(0=DauVao/1=DauRa), GroupType, IdPhieu, MaPhieu, TrangThai, SoLuongDinhMuc, IsSanPhamChinh, IsBanThanhPham}`.
- `ProductionOrder {Ma, Ten, SoLuongYeuCau, SoLuongSanXuat, HanXuLy, IsTachDon}` (nghi legacy).

## Luồng code
SaleOrderDonHang → `InsertProductRequest` (`SaleOrderProductionService.cs:222`) → `WorkOrder.ThemMoi`/`InsertAsyncV2` (`WorkOrderService.cs:190`) → `ChuyenWOPlanner` (`WorkOrderService.cs:262-421`, scale `SoLuong/SoLuongTheoCongThuc`) → `WOPlanStart` (`:1809`) → per-step `TaoPhieuXuatByStep` (`:893`) / `TaoPhieuNhapByStep` (`:1276`, đẩy qty bước kế `:1447`) → `HoanThanhWorkOrderStep` (`:1701`). Mọi tạo phiếu/đọc tồn đi HTTP qua `IWarehouseProductService` (`SanXuat.Service/Orther/WarehouseProductService.cs`).

## Bug-prone / lưu ý (ghi nhận, KHÔNG sửa — để session security)
- **`WorkOrderService.DeleteAsyncV2` (`:2220`) xóa WO cascade nhưng KHÔNG hoàn/void phiếu nhập/xuất** đã đẩy sang Warehouse → tồn kho lệch. (lỗ hổng nghiệp vụ chính)
- **`CapNhatTrangThaiSanXuat` (`:2776`)** set TrangThaiSanXuat raw, **không guard** → nhảy state tùy ý, phá state machine.
- **Nhiều endpoint điều hành WO để `[SBAuthorize()]`/`[SBAuthorize]` rỗng** (Xoa, TamDung, ChuyenVeChuaXepLich, BatDau/HuySanXuat, CapNhat*SanPham, GetLichSu) — key thật bị comment → chỉ cần đăng nhập. Tương tự `SaleOrderDonHangChiTietController` và `DmCongDoanController` comment toàn bộ key.
- **InPhieuController: InPhieuNhapSX và InPhieuXuatSX gọi cùng `InsertAsyncV2`** (`InPhieuController.cs:40,57`) — tên endpoint gợi ý khác nhưng service không tách nhánh nhập/xuất → có thể gây hiểu nhầm.
- **Thiếu tenant guard** ở `SaleOrderDonHangChiTietController` / `DmCongDoanController` (khác `BomSetupController` chỉ guard ở `CapNhatThongTin`) → rủi ro thao tác chéo đơn vị/chi nhánh.
- **`CapNhatThongTin` (đơn) + `CapNhatQuyTrinh` (BOM) là delete-all-reinsert** → mọi Id con đổi; nơi khác giữ id cũ sẽ gãy.
- **Không có RabbitMQ/MassTransit** trong toàn SanXuat (chỉ `using MassTransit.Internals;` thừa, `SaleOrderProductionService.cs:10`); ripple kho qua HTTP đồng bộ → lỗi giữa chừng không có cơ chế retry/compensate rõ ràng.
- **ProductionOrder** nghi legacy/song song với SaleOrderProduction (field gần trùng) → cần product xác nhận trước khi triage/sửa.

## File source (path:line)
- BE controllers: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\SanXuat\SanXuat.API\Controllers\` (`WorkOrderController.cs`, `SaleOrderDonHangController.cs`, `SaleOrderProductionController.cs`, `InPhieuController.cs`, `ProductionOrderController.cs`, `SaleOrderDonHangChiTietController.cs`, `DmCongDoanController.cs`).
- BE services: `...\SanXuat\SanXuat.Service\` — `WorkOrderService.cs` (:190/:262/:584/:893/:1090/:1276/:1517/:1701/:1809/:2220/:2776), `SaleOrderProductionService.cs` (:49/:74/:111/:222), `SaleOrderDonHangService.cs` (:70/:126/:186/:273/:339/:500/:578), `Orther/WarehouseProductService.cs`.
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\sanxuat\` — `sanxuat-routing.module.ts`, `saleorder-donhang/`, `saleorder-production/`, `workorder/`, `workorder-planner/`, `models/sanxuat-enum.ts`.
