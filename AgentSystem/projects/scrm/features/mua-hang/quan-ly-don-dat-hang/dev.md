# Dev — Quản lý đơn đặt hàng

> Service BE: **Accounting** (controller `DonDatHangController` — TienIch gắn **TI_Kho**, menu Kho). FE module **WarehouseProduct** (Angular 15 + ng-zorro). Chỉ dành cho dev.
> Registry: `projects/scrm/registry/ke-toan/DonDatHangController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Quản lý đơn đặt hàng (lưới + form) | `warehouse-product/quan-ly-don-dat-hang` | `QuanLyDonDatHangComponent` |
| Import đơn đặt hàng (popup) | (popup trong màn trên) | `import-don-dat-hang.component.ts` |
| Tạo đơn đặt hàng từ dự báo tồn | (popup trong baocao-tonkho-donvi) | `baocao-tonkho-donvi/tao-don-dat-hang.component.ts` |

- Route: `WarehouseProduct/warehouseProduct-routing.module.ts:241` (quan-ly-don-dat-hang).
- Model/service FE: `src/app/models/don-dat-hang.model.ts`, `src/app/services/warehouseproduct/don-dat-hang.service.ts`.

## Endpoint / Controller (trace registry)
**`DonDatHangController`** (service Accounting, auth **mixed**). State machine `TrangThaiDonDatHangEnum`: Nhap=1, ChoDuyet=2, DaDuyet=3 (auto→ChoXuLy), KhongDuyet=4, TrinhLai=5, ChoXuLy=6, DangXuLy=7, TamDungXuLy=8, DaHoanThanh=9, DaHuy=10.

| MaTinhNang (key) | Route | Mục đích |
|---|---|---|
| `DonDatHang_TimTheoDieuKien` | `POST api/DonDatHang/TimTheoDieuKien` | Lưới + lọc + phân quyền CN. |
| `DonDatHang_ThemMoi` | `POST .../ThemMoi` | Thêm đơn + dòng SP (transaction). |
| `DonDatHang_TimTheoId` | `GET .../TimTheoId/{id}` | Chi tiết đơn. |
| `DonDatHang_CapNhat` | `POST .../CapNhat` | Sửa (chỉ {Nhap, TrinhLai}); replace dòng SP. |
| `DonDatHang_Xoa` | `DELETE .../Xoa/{id}` | Hard delete (chỉ Nhap). |
| `DonDatHang_CapNhatThanhToan` | `POST .../CapNhatThanhToan` | Set cờ DaThanhToan. |
| `DonDatHang_ChuyenTrangThai` | `POST .../ChuyenTrangThai` | Chuyển trạng thái 2 pha; cascade hủy PMH nếu →DaHuy. |
| `DonDatHang_CapNhatSoLuongDaNhan` | `POST .../CapNhatSoLuongDaNhan` | Cộng SoLuongDaNhan. |
| `DonDatHang_TaoPhieuMuaHang` | `POST .../TaoPhieuMuaHang` | Tạo PMH (ChoXuLy/DangXuLy/TamDungXuLy). |
| `DonDatHang_LayDanhSachPhieuMuaHang` | `GET .../LayDanhSachPhieuMuaHang/{idDonDatHang}` | DS PMH theo đơn. |
| `DonDatHang_ThemMoi` | `GET .../KiemTraQuyenTaoDon` | Check quyền tạo đơn tại CN. |

**Endpoint nội bộ / import:**
- `POST CapNhatDaTaoPhieuMuaHang` — **KHÔNG có `[SBAuthorize]`/`[PrivateSBAuthorize]`** (gọi từ PhieuMuaHangService). ⚠️ thiếu phân quyền (xem Bug-prone).
- `POST KiemTraVaCapNhatHoanThanh` — `[PrivateSBAuthorize]`; nhận đủ hàng → set DaHoanThanh.
- Import (`DonDatHangController.Import.cs`, tất cả `[SBAuthorize()]` trống): `DownloadTemplateDonDatHang`, `ImportTemplateDonDatHang`, `GetDanhSachDonDatHangImport`, `DeleteTmpDonDatHangImport`, `InsertTmpDonDatHangToDonDatHang`.

## Entities
- `DonDatHang`, `DonDatHangSanPham` (dòng SP, order theo SoThuTu).
- `TmpDonDatHangImport`, `TmpDonDatHangSpImport` (bảng tạm import).
- 100% EF, không stored proc, không message bus.

## Luồng code
- `ThemMoi`: validate NCC/CN/SP, check quyền tạo theo CN (config WarehouseProduct), validate quyền duyệt nếu trạng thái=ChoDuyet, sinh mã, `CanKiemDuyet=false`→ChoDuyet auto ChoXuLy, insert transaction.
- `ChuyenTrangThai`: validate quyền theo pha (pha 1 DaDuyet/KhongDuyet/TrinhLai cần canDuyet; pha 2 DangXuLy/TamDungXuLy/DaHoanThanh/DaHuy cần canTao), validate luồng hợp lệ, DaDuyet auto→ChoXuLy; →DaHuy + `HuyPhieuMuaHangLienQuan` → cascade `HuyPhieuMuaHang`.
- `TaoPhieuMuaHang` → `PhieuMuaHangService.ThemMoiAsync` (DangXuLy, MuaHangVaoKho), set `DaTaoPhieuMuaHang=true`.
- Chiều ngược: `PhieuMuaHangService` gọi `CapNhatSoLuongDaNhan` + `CapNhatDaTaoPhieuMuaHang` + `KiemTraVaCapNhatHoanThanh` về đơn.

## Bug-prone / lưu ý (path:line)
> Chỉ ghi nhận — KHÔNG sửa code ở session này.
- **`CapNhatDaTaoPhieuMuaHang` thiếu phân quyền hoàn toàn** (`DonDatHangController.cs:92-97`): không có `[SBAuthorize]`/`[PrivateSBAuthorize]` → bất kỳ ai gọi được để bật cờ `DaTaoPhieuMuaHang` cho đơn bất kỳ (rủi ro sai lệch dữ liệu). Là internal call → nên thêm `[PrivateSBAuthorize]`.
- **Cascade hủy PMH không atomic** (`DonDatHangService.cs:≈803-830`): PMH-A hủy xong rồi PMH-B fail → A đã HuyBo, đơn chưa DaHuy → trạng thái lệch, không tự rollback (code thừa nhận semi-atomic).
- **`XuLyTrangThaiBanDau` (`≈909-922`)**: 2 nhánh ChoDuyet→ChoXuLy (khi !CanKiemDuyet) và DaDuyet→ChoXuLy khác nhau → dễ nhầm khi đọc/sửa.
- **Import endpoints** dùng `[SBAuthorize()]` trống; batch size 50 + stuck timeout 30 phút **hardcode** (không lấy từ config).
- **Hard delete** (Xoa): không soft-delete.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Accounting\Accounting.API\Controllers\DonDatHangController.cs` (`:92-97` CapNhatDaTaoPhieuMuaHang).
- BE controller import: `...\Accounting\Accounting.API\Controllers\DonDatHangController.Import.cs`.
- BE service: `...\Accounting\Accounting.Service\Generated\DonDatHangService.cs` (`:≈803-830` cascade, `:≈909-922` XuLyTrangThaiBanDau).
- FE list: `...\scrm_frontend\src\app\pages\WarehouseProduct\quan-ly-don-dat-hang\quan-ly-don-dat-hang.component.ts` (+ `import-don-dat-hang`).
- FE tạo từ dự báo: `...\WarehouseProduct\quanly-kho\kiemtra-tonkho\baocao-tonkho-donvi\tao-don-dat-hang\tao-don-dat-hang.component.ts`.
- FE model/service: `...\src\app\models\don-dat-hang.model.ts`, `...\src\app\services\warehouseproduct\don-dat-hang.service.ts`.
- FE routing: `...\WarehouseProduct\warehouseProduct-routing.module.ts:241`.
