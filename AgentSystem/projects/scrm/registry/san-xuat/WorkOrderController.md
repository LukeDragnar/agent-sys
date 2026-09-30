---
controller: WorkOrderController
module: san-xuat
service: SanXuat
man_hinh_ui: "Sản xuất > Lệnh sản xuất (Work Order) — màn điều hành WO: xếp lịch, bắt đầu/tạm dừng, chạy từng bước quy trình (step), tạo phiếu nhập/xuất kho cho từng step, hoàn thành step/WO"
muc_dich: "Điều hành toàn bộ vòng đời Lệnh sản xuất (Work Order): tách WO từ Production Request, xếp lịch, vật chất hóa quy trình từ BOM thành các bước (WorkOrderStep) + sản phẩm đầu vào/ra, tạo phiếu xuất NVL và phiếu nhập thành phẩm qua kho cho từng bước, đẩy số lượng đầu ra sang bước kế, hoàn thành bước/WO."
alias: ["lệnh sản xuất", "work order", "WO", "WorkOrder", "điều hành sản xuất", "chạy quy trình sản xuất", "phiếu nhập/xuất sản xuất theo bước"]
auth: public                                   # controller [SBAuthorize]; phần lớn endpoint có key riêng, một số [SBAuthorize()] rỗng (chỉ cần đăng nhập)
state_machine: [TrangThai(ChuaBatDau=0,ChuaXepLich=1,DaXepLich=2), TrangThaiSanXuat(ChuaBatDau=0,DangSanXuat=1,TamDung=2,Huy=3,HoanThanh=4|null), WorkOrderStep(ChuaXuLy=0,DangXuLy=1,HoanThanh=2)]
deprecated: false
entities: [WorkOrder, WorkOrderStep, WorkOrderStepProduct]
stored_procs: []
depends: [SanXuat, IWarehouseProductService (HTTP → Warehouse/Kho), IGenCodeService, SaleOrderProduction, BomSetup, BomStep, BomProduct]
cross_module_in: [SaleOrderProductionController (InsertAsyncV2 tiêu thụ Production Request để tách WO), InPhieuController (gọi cùng IWorkOrderService.InsertAsyncV2)]
cross_module_out: [WarehouseProduct/Kho (POST api/PhieuXuat/ThemMoi + api/PhieuNhap/ThemMoi — tạo phiếu xuất NVL & nhập thành phẩm; chiều ngược KHÔNG hoàn/void khi xóa WO)]
---

# WorkOrderController — Lệnh sản xuất (Work Order) & điều hành quy trình

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tách WO từ PR | Thêm mới / Tách đơn | POST `api/WorkOrder/ThemMoi` | `WorkOrder_ThemMoi` (AD-TONG/AD-CN/SANXUAT) | public | từ Production Request chưa tách | Tách `SaleOrderProduction` → các WO; nếu `IsTachDon==true` → lỗi "Đơn đã được tách"; set PR `IsTachDon=true`, copy LotSize/SoLuongSanXuat/IdBomSetup; WO mới `TrangThai=ChuaBatDau`, `IsPlan=false`; gọi Warehouse `PrivateGetSanPhamByIds` lấy mã SP | EF + HTTP Warehouse | InsertAsyncV2 (WorkOrderService.cs:190) |
| 2 | Chuyển sang Planner | Chuyển sang Plan | POST `api/WorkOrder/ChuyenWOPlanner` | `WorkOrder_ChuyenWOPlanner` | public | WO `ChuaBatDau`, có IdBomSetup | Vật chất hóa quy trình: BomStep→WorkOrderStep, BomProduct→WorkOrderStepProduct, `SoLuongDinhMuc` scale theo `SoLuong/SoLuongTheoCongThuc`; set WO `TrangThai=ChuaXepLich`; WO không có IdBomSetup → lỗi "đơn WO cũ"; gọi Warehouse `DropdownSanPhamSanXuat` (IsSanPhamLoDate). 1 transaction | EF + HTTP Warehouse | ChuyenWOPlanner (WorkOrderService.cs:262-421) |
| 3 | Xếp lịch (bắt đầu kế hoạch) | Bắt đầu / Xếp lịch | POST `api/WorkOrder/BatDauWorkOrder` | `WorkOrder_BatDauWorkOrder` | public | WO `ChuaXepLich` | `TrangThai=DaXepLich`, `BuocIndex=1`, `TrangThaiSanXuat=ChuaBatDau`, set NgayBatDau; step ThuTu=1 → `DangXuLy`. Đã `DangSanXuat` → chặn | EF | WOPlanStart (WorkOrderService.cs:1809) |
| 4 | Tạm dừng / huỷ xếp lịch | Tạm dừng | POST `api/WorkOrder/TamDungWorkOrder` | `SBAuthorize()` (chỉ đăng nhập) | public | WO `DaXepLich` & `TrangThaiSanXuat==ChuaBatDau` | Reset `TrangThai=ChuaXepLich`, `BuocIndex=0`, `TrangThaiSanXuat=null`, NgayBatDau=null. Sai state → chặn | EF | WOPlanStop (:1882); permission key bị comment, để rỗng |
| 5 | Chuyển về chưa xếp lịch | Chuyển về chưa xếp lịch | POST `api/WorkOrder/ChuyenVeChuaXepLich` | `SBAuthorize()` (chỉ đăng nhập) | public | bất kỳ state | Reset `TrangThai=ChuaXepLich`, NgayBatDau=null, BuocIndex=0, `TrangThaiSanXuat=null`. **Không guard state** | EF | ChuyenVeChuaXepLich (:2651); key comment, để rỗng |
| 6 | Bắt đầu sản xuất | Bắt đầu sản xuất | POST `api/WorkOrder/BatDauSanXuat/{id}` | `SBAuthorize` (chỉ đăng nhập) | public | chưa DangSanXuat/HoanThanh | `TrangThaiSanXuat=DangSanXuat`. Đã DangSanXuat/HoanThanh → chặn | EF | BatDauSanXuat (:2678); không có key riêng |
| 7 | Huỷ sản xuất | Huỷ sản xuất | POST `api/WorkOrder/HuySanXuat/{id}` | `SBAuthorize` (chỉ đăng nhập) | public | `TrangThaiSanXuat==DangSanXuat` | `TrangThaiSanXuat=Huy`. Yêu cầu state hiện = DangSanXuat | EF | HuySanXuat (:2729) |
| 8 | Cập nhật trạng thái SX (raw) | (set state) | POST `api/WorkOrder/CapNhatTrangThaiSanXuat` | `SBAuthorize` (chỉ đăng nhập) | public | - | Set `TrangThaiSanXuat = trangThai` (int) thô. **KHÔNG validate/guard** — nhận mọi giá trị | EF | CapNhatTrangThaiSanXuat (:2776) — rủi ro nhảy state tuỳ ý |
| 9 | Tạo phiếu xuất NVL theo bước | Tạo phiếu xuất | POST `api/WorkOrder/LuuVaTaoPhieuXuatByStep` | `WorkOrder_TaoPhieuXuatByStep` | public | step chưa HoanThanh | Lưu chỉnh sửa SP đầu vào (bỏ row đã có IdPhieu), validate GroupType + không trùng IdSanPham/group; set `woStep.NgayXuat`; gọi Warehouse tạo phiếu xuất (KieuLoaiXuat=2) → stamp IdPhieu/MaPhieu về step product. Step HoanThanh → chặn | EF + HTTP Warehouse | LuuVaTaoPhieuXuatByStep (:1090) → TaoPhieuXuatByStep (:893) |
| 10 | Tạo phiếu nhập thành phẩm theo bước | Tạo phiếu nhập | POST `api/WorkOrder/LuuVaTaoPhieuNhapByStep` | `WorkOrder_TaoPhieuNhapByStep` | public | step chưa HoanThanh | Lưu SP đầu ra; auto-gen lô `LSX` cho SP lô-date; GiaNhap=ΣTienXuat/ΣSoLuongNhap; gọi Warehouse tạo phiếu nhập (MaDmPhanLoaiNhap=10) → stamp IdPhieu. **Đẩy số lượng đầu ra sang SP đầu vào của bước KẾ** (scale theo SP chính) | EF + HTTP Warehouse | LuuVaTaoPhieuNhapByStep (:1517) → TaoPhieuNhapByStep (:1276) |
| 11 | Hoàn thành bước quy trình | Hoàn thành bước | POST `api/WorkOrder/HoanThanhWorkOrderStep/{id}` | `WorkOrder_HoanThanhWorkOrderStep` | public | mọi SP của step đã có phiếu | Chặn nếu còn SP `IdPhieu==null` ("Vui lòng tạo các phiếu"); có bước kế → bước kế `DangXuLy` + `wo.BuocIndex=next.ThuTu`; bước cuối → `wo.TrangThaiSanXuat=HoanThanh` + NgayHoanThanh; step này `HoanThanh`. Transaction | EF | HoanThanhWorkOrderStep (:1701) |
| 12 | In phiếu nhập / xuất | In phiếu | POST `api/WorkOrder/PrintPhieuNhap` · `PrintPhieuXuat` | `SBAuthorize` (chỉ đăng nhập) | public | có phiếu | Lấy dữ liệu phiếu (GetPhieuNhapById/GetPhieuXuatById qua Warehouse) để in | EF + HTTP Warehouse | PrintPhieuNhap/PrintPhieuXuat (:1809+) |
| 13 | Quản lý SP trong bước | thêm/sửa/xoá SP | POST `XoaSanPham/{id}` · `CapNhatSanPham` · `LuuDanhSachSanPham` · `LuuTatCaSanPham` | `SBAuthorize` (chỉ đăng nhập) | public | step chưa HoanThanh | CRUD WorkOrderStepProduct trong 1 bước (đầu vào/ra) | EF | nhiều endpoint không có permission key riêng |
| 14 | Xoá nhóm SP | xoá nhóm | POST `api/WorkOrder/XoaNhom/{idWorkOrderStep}/{groupType}` | `SBAuthorize` (chỉ đăng nhập) | public | group≠0, step chưa HoanThanh, SP chưa có phiếu | Xoá nhóm SP (GroupType) của 1 step; chặn GroupType=0 (nhóm mặc định), step HoanThanh, hoặc SP đã có phiếu | EF | XoaNhom (:2819) |
| 15 | Lấy SP cần (BOM vs tồn kho) | tính NVL cần | POST `api/WorkOrder/GetWOSanPhamCan` | `WorkOrder_GetWOSanPhamCan` | public | - | Report-only: join BomSetup+BomProduct (Loai=0) theo IdBomSetup/IdSanPham; gọi Warehouse `GetTonKhoVaDonViTinhByListSanPham`; quy đổi đơn vị; `SoLuongThieu=max(0, can−tonKho)` | EF + HTTP Warehouse | GetWOSanPhamCan (:584) |
| 16 | Chi tiết WO theo step | xem chi tiết | GET `api/WorkOrder/GetWOStepById/{id}` | `WorkOrder_GetWOStepById` | public | - | Read-only: load step + step-product, nhóm DauVao/DauRa theo step, SP chính (IsSanPhamChinh) đứng đầu | EF | GetWOStepById (:788) |
| 17 | Tìm theo điều kiện | grid lọc | POST `api/WorkOrder/TimTheoDieuKien` | `WorkOrder_TimTheoDieuKien` | public | màn list | Phân trang WO theo filter + CurrentUser | EF | - |
| 18 | Lịch sử / trạng thái | lịch sử, dropdown TT | POST `GetLichSu` · GET `GetTrangThaiSanPhamList` | `SBAuthorize()` / `SBAuthorize` | public | - | GetLichSu: log WO; GetTrangThaiSanPhamList: enum `WorkOrderStepProductTrangThaiEnums` (LoiNguyenLieu=1, LoiPhePham=2) | EF + in-memory enum | - |
| 19 | Xoá WO | Xoá | POST `api/WorkOrder/Xoa/{id}` | `SBAuthorize()` (chỉ đăng nhập) | public | - | Cascade delete WorkOrderStepProduct → WorkOrderStep → WorkOrder (1 transaction). **KHÔNG gọi Warehouse** → không hoàn/void các phiếu nhập/xuất đã tạo | EF | DeleteAsyncV2 (:2220); để rỗng key |

## Quy tắc nghiệp vụ (state machine — blueprint để trace)
1. **TrangThai (xếp lịch):** `ChuaBatDau(0)` → ChuyenWOPlanner → `ChuaXepLich(1)` → BatDauWorkOrder/WOPlanStart → `DaXepLich(2)`. TamDung/ChuyenVeChuaXepLich đưa về `ChuaXepLich`.
2. **TrangThaiSanXuat (sản xuất):** `ChuaBatDau(0)` → BatDauSanXuat → `DangSanXuat(1)`; TamDung→`TamDung(2)`; HuySanXuat (chỉ từ DangSanXuat) → `Huy(3)`; hoàn thành step cuối → `HoanThanh(4)`; khi un-schedule = `null`.
3. **WorkOrderStep.TrangThai:** `ChuaXuLy(0)` → `DangXuLy(1)` (khi tới lượt bước) → `HoanThanh(2)`. Bước kế chỉ `DangXuLy` khi bước trước `HoanThanh`.
4. **Gate tạo phiếu:** chỉ tạo phiếu cho SP **chưa có IdPhieu**; step `HoanThanh` không cho tạo thêm; hoàn thành step yêu cầu **mọi SP đã có phiếu**.
5. **Loai SP:** `DauVao=0` (xuất NVL), `DauRa=1` (nhập thành phẩm). Nhập đầu ra của bước → cộng vào đầu vào bước kế (scale theo SP chính).
6. **CapNhatTrangThaiSanXuat** không guard → có thể phá state machine (lưu ý khi trace/triage).

## Endpoint nội bộ (private / service-to-service)
- [internal] `InsertAsyncV2` cũng được `InPhieuController.InPhieuNhapSX/InPhieuXuatSX` gọi (cùng IWorkOrderService).
- [internal→Warehouse] mọi tạo phiếu/đọc tồn kho đi **HTTP đồng bộ** qua `IWarehouseProductService` (`Orther/WarehouseProductService.cs`): `TaoPhieuXuat`→POST `api/PhieuXuat/ThemMoi`; `TaoPhieuNhap`→POST `api/PhieuNhap/ThemMoi`; `GetTonKho*`, `DropdownSanPhamSanXuat`, `PrivateGetSanPhamByIds`.

## Quan hệ
- **Gọi sang:** Warehouse/Kho service (phiếu nhập/xuất + tồn kho) qua HTTP; IGenCodeService (sinh mã lô `LSX`); đọc BomSetup/BomStep/BomProduct và SaleOrderProduction.
- **Dữ liệu (entities):** `WorkOrder {TrangThai, TrangThaiSanXuat, BuocIndex, IdBomSetup, IsTachDon, IsPlan}`, `WorkOrderStep {ThuTu, TrangThai, NgayXuat, NgayHoanThanh}`, `WorkOrderStepProduct {Loai, GroupType, IdPhieu, MaPhieu, TrangThai, SoLuongDinhMuc, IsSanPhamChinh, IsBanThanhPham}`.
- **Ripple / đồng bộ:** Tạo phiếu xuất/nhập → tạo chuyển động tồn kho thật ở Warehouse service. **Không có RabbitMQ/MassTransit** trong toàn SanXuat (chỉ `using MassTransit.Internals;` thừa ở SaleOrderProductionService.cs:10).

## Luồng xuyên module
Production Request (SaleOrderProduction) → `ThemMoi` tách WO → `ChuyenWOPlanner` vật chất hóa BOM thành step → `BatDauWorkOrder` xếp lịch → từng step: `LuuVaTaoPhieuXuatByStep` (Warehouse xuất NVL) → `LuuVaTaoPhieuNhapByStep` (Warehouse nhập thành phẩm, đẩy qty sang bước kế) → `HoanThanhWorkOrderStep` → bước cuối → WO `HoanThanh`.

## Lỗi đã biết / lưu ý kỹ thuật
- `DeleteAsyncV2` (WorkOrderService.cs:2220) xoá cascade nhưng **không hoàn/void phiếu nhập/xuất** đã đẩy sang Warehouse → tồn kho lệch nếu xóa WO sau khi đã tạo phiếu.
- `CapNhatTrangThaiSanXuat` (:2776) set `TrangThaiSanXuat` raw, không guard → nhảy state tuỳ ý.
- Nhiều endbpoint để `[SBAuthorize()]`/`[SBAuthorize]` rỗng (Xoa, TamDung, ChuyenVeChuaXepLich, BatDau/HuySanXuat, CapNhat*SanPham, GetLichSu) — chỉ cần đăng nhập, key thật bị comment → cần xác nhận có chủ ý.
- Coupling liên-bước: nhập đầu ra cộng vào đầu vào bước kế (WorkOrderService.cs:1447) — sai SP chính/tỉ lệ sẽ lệch định mức bước sau.
