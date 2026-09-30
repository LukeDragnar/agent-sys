# Quản lý vận đơn — Dev

> CHỈ dev. Không đưa vào summarize/faq.
> BE: `OrderLogistic` (đăng đơn, cấu hình, mẫu đóng hàng, trạng thái giao hàng) + `WarehouseProduct` (lượt nhặt).

## Màn hình & route (FE)
FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`

| Màn hình | Route | Component |
|---|---|---|
| Đăng đơn (Thủ kho tác nghiệp) | `/marketing/thu-kho-tac-nghiep` | `ThuKhoTacNghiepComponent` |
| Cấu hình vận đơn | `/marketing/cauhinh-vandon` | `ConfigOrderLogisticComponent` |
| Cấu hình trạng thái giao hàng | `/marketing/cauhinh-trangthai-giaohang` | `CauhinhTrangthaiGiaohangComponent` |
| Mẫu kích thước đóng hàng | `/marketing/mau-kich-thuoc-dong-hang` | `MauKichthuocDonghangComponent` |
| Danh sách lượt nhặt | `/warehouse-product/quan-ly-luot-nhat` | `LuotnhatDanhsachComponent` |

- Route con khai báo:
  - `landing-page-routing.module.ts:34-53` (thu-kho-tac-nghiep, cauhinh-vandon, cauhinh-trangthai-giaohang, mau-kich-thuoc-dong-hang)
  - `WarehouseProduct/warehouseProduct-routing.module.ts:184-187` (quan-ly-luot-nhat)
- Prefix parent: `app-routing.module.ts` → `marketing` (LandingPageModule) `:172-180`, `warehouse-product` (WarehouseProductModule) `:84-92`.

## MaTinhNang / Controller (trace endpoints)
BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm`

| Trace endpoint | Controller | MaTienIch | File:line |
|---|---|---|---|
| `ThuKhoTacNghiep_TimTheoDieuKien` | `ThuKhoTacNghiepController` | `TI_KhoLogistic` | `OrderLogistic/OrderLogistic.API/Controllers/KhoTacNghiepController.cs:30` |
| `CauHinh_Order_LuuCauHinhOrder` | `CauHinhController` (OrderLogistic) | `TI_OrderLogistic` | `OrderLogistic/OrderLogistic.API/Controllers/CauHinhController.cs:11` |
| `MauKichThuocDongHang_TimTheoDieuKien` | `MauKichThuocDongHangController` | `TI_OrderLogistic` | `OrderLogistic/OrderLogistic.API/Controllers/MauKichThuocDongHangController.cs:97` |
| `LuotNhatKho_TimTheoDieuKien` | `LuotNhatKhoController` | `TI_Kho` | `WarehouseProduct/WarehouseProduct.API/Controllers/LuotNhatKhoController.cs:22` |

> Lưu ý đặt tên: class file `KhoTacNghiepController.cs` nhưng controller bên trong tên `ThuKhoTacNghiepController` (TienIch `"ThuKhoTacNghiepController"`).
> Còn 1 `CauHinhController` trùng tên ở service `Order` — endpoint trace `CauHinh_Order_*` ở đây map về OrderLogistic.

### Endpoints chính

**ThuKhoTacNghiepController** (`KhoTacNghiepController.cs`):
- `POST api/ThuKhoTacNghiep/TimTheoDieuKien` — danh sách đăng đơn (`:31`)
- `POST .../DangDonSearch`, `InsertDonHangDangDon`, `ProcessDonHangDangDon`, `DeleteDonHangDangDonByStatus` — luồng đăng đơn (`:206-254`)
- `POST .../AdminDangDon`, `AdminHuyDangDon`, `RollbackChoPartnerDangDon` (hủy chờ partner + hoàn kho, `:111`)
- `POST .../CapNhatTrangThaiGiaoHangTheoDon`, `ThayDoiTrangThaiDonHang`, `LayThongTinTheoMaDonHang`
- `POST .../GetDanhSachDonHangTheoLuotNhat` (`:608`) — đơn theo lượt nhặt
- `POST .../ExportExcelKhoTacNghiepKieu1..4` (dùng bản `_V2`, `:324-476`)
- Quyền điển hình: `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QL-KHO, DV-QL-KHO-TONG, DV-KHO-KIEM-TRUNG, HT-CSKH`

**CauHinhController** (OrderLogistic):
- `GET api/CauHinh/Get` (`:37`), `POST Save_SuperAdmin` (`:54`), `SaveCauHinhDonHangSale` (`:95`), `SaveCauHinhInit` (`:105`), `Save/GetNghiepVuCauHinh` (`:129-143`)
- Cấu hình lưu trong `CauHinhSettingDto` (gồm `SoLuongSKUMaxPhieuNhat` dùng cho lượt nhặt).

**MauKichThuocDongHangController**:
- `POST ThemMoi` (`:28`, check trùng tên + cắt 255), `CapNhatThongTin` (`:51`), `CapNhatIsDefault` (`:67`), `Xoa/{id}` (`:76`), `TimTheoDieuKien` (`:98`), `GET DropDownMauKichThuoc` (`:106`).
- Cache invalidation: `_commonCacheService.MauKichThuocDongHang_RemoveByIdChiNhanh(...)` sau mỗi write.

**LuotNhatKhoController**:
- `POST TimTheoDieuKien` (`:23`), `TaoLuotNhat` (`:37`), `SyncPhieuNhat` (`:44`), `DeleteLuotNhat/{id}` (`:51`), `GetDsSanPhamByDonHang` (`:30`), `GetDanhSachDonHangTheoLuotNhat/{idLuotNhat}` (Private, `:58`).

**Cấu hình trạng thái giao hàng** (không nằm trong 4 trace gốc):
- FE gọi `api/Common/LayCauHinhTrangThaiGiaoHang` + `api/Common/CapNhatCauHinhTrangThaiGiaoHang` (OrderLogistic `CommonController`).
- FE service: `scrm_frontend/src/app/services/orderLogistic/common.service.ts:59-66`.
- Chỉ lưu `stt` (thứ tự) + `color` mỗi trạng thái; component `cauhinh-trangthai-giaohang.component.ts`.

## Entities
- **`MauKichThuocDongHang`** — `OrderLogistic/OrderLogistic.DataContext/Entity/MauKichThuocDongHang.cs`
  - `IdDonVi`, `IdChiNhanh?`, `Ten` (NVARCHAR 255), `ChieuCao`, `ChieuRong`, `ChieuDai`, `CanNang`, `IsDefault` (default false).
- **`LuotNhatKho`** — `WarehouseProduct/WarehouseProduct.DataContext/Entity/LuotNhatKho.cs`
  - `IdDonVi`, `IdChiNhanh`, `Ma` (VARCHAR 250), `JsonData` (snapshot ds sản phẩm theo đơn), `SoTuTang?` (số tự tăng để gen mã).
- Liên quan: `PhieuNhatKho`, `PhieuNhatKhoDonHang`, `PhieuNhatKhoSanPham`, `KhoChiTiet`, `ViTriKho`, `ViTriSanPham`, `SanPham` (đều trong WarehouseProduct).
- Đơn vận hành thực tế: `DonHangLogistic` (OrderLogistic) — xem `features/order-logistic.md`.

## Luồng code

### Tạo lượt nhặt — `LuotNhatKhoService.TaoLuotNhatAsync` (`WarehouseProduct.Service/Generated/LuotNhatKhoService.cs:186`)
1. Lấy cấu hình `CauHinhSettingDto.SoLuongSKUMaxPhieuNhat` qua `_cauHinhService.GetAsync(DonViId)` — null/≤0 → trả lỗi "Vui lòng cấu hình số lượng tối đa SKU..." (`:189-191`).
2. Check đơn đã có phiếu nhặt: query `PhieuNhatKhoDonHang` theo `LstIdDonHang` → nếu có → lỗi "đã được tạo phiếu nhặt" (`:194-196`).
3. Gọi cross-service `OrderLogistic/api/DonHangLogistic/GetDsDonHangThuongMai` lấy ds sản phẩm trong đơn (`:205-207`); rỗng → "Không tìm thấy đơn hàng!".
4. Join `KhoChiTiet` + `SanPham` + `ViTriSanPham`/`ViTriKho` để dựng danh sách nhặt theo **vị trí kho** (`:232-304`).
5. Gen mã: `GenCode(soTuTangLN, "LN" + ddMMyyyy)`; `SoTuTang` lấy max qua `GetMaxSoTuTang` + cache Redis (`:345-353`, `:759-781`).
6. Tách thành nhiều `PhieuNhatKho` theo giới hạn SKU; lưu `LuotNhatKho` + phiếu; invalidate cache số tự tăng (`:447-465`).

### Mẫu đóng hàng — `MauKichThuocDongHangController`
- ThemMoi: set `IdDonVi/IdChiNhanh` từ `CurrentUser`; `GetMauByTen` để chặn trùng; cắt `Ten` về 255; map DTO→entity; `InsertAsync`; xóa cache theo chi nhánh (`:28-48`).

## Bug-prone
- **Mỗi đơn = 1 lượt nhặt:** chặn bằng query `PhieuNhatKhoDonHang`. Nếu xóa lượt nhặt mà không dọn `PhieuNhatKhoDonHang` → đơn kẹt "đã có phiếu nhặt". Kiểm tra `DeleteLuotNhatAsync` (`:469`) có xóa đủ child không.
- **Số tự tăng mã `LN`/`PN` cache Redis** (`KeyCache_LuotNhatKho_SoTuTangMaxTheoDonVi`): nếu cache lệch DB → trùng mã. Khi nghi trùng mã, clear cache `:447-448, :516-517`.
- **`SoLuongSKUMaxPhieuNhat`** lấy từ cấu hình OrderLogistic CauHinh (không phải WarehouseProduct) — sửa cấu hình sai service sẽ "không có tác dụng".
- **Cache mẫu đóng hàng theo chi nhánh:** quên `MauKichThuocDongHang_RemoveByIdChiNhanh` sau write → dropdown/list không cập nhật.
- **2 `CauHinhController` trùng tên** (Order vs OrderLogistic): cẩn thận khi search/sửa, dễ sửa nhầm service.
- **File vs class mismatch:** `KhoTacNghiepController.cs` chứa `ThuKhoTacNghiepController` — search theo tên file dễ trượt.
- **Export Excel:** controller gọi bản `*_V2` (bản cũ không V2 đã comment); xử lý lỗi `Service_Unavailable` → trả 503, lỗi khác → 500 (`:329-351`).
- **AdminDangDon/AdminHuyDangDon/RollbackChoPartnerDangDon** đụng hoàn kho — sai luồng dễ lệch tồn; verify quyền (Rollback giới hạn `DV-AD-TONG, HT-AD, HT-CSKH, ...`).

## File source (path:line thật)
BE root: `D:\PROJECT\Git\_SourceForCode\Group2\scrm`
- `OrderLogistic/OrderLogistic.API/Controllers/KhoTacNghiepController.cs:30` (ThuKhoTacNghiep_TimTheoDieuKien)
- `OrderLogistic/OrderLogistic.API/Controllers/CauHinhController.cs:11,37,95,105`
- `OrderLogistic/OrderLogistic.API/Controllers/MauKichThuocDongHangController.cs:28,97`
- `OrderLogistic/OrderLogistic.DataContext/Entity/MauKichThuocDongHang.cs:11`
- `WarehouseProduct/WarehouseProduct.API/Controllers/LuotNhatKhoController.cs:22,37`
- `WarehouseProduct/WarehouseProduct.Service/Generated/LuotNhatKhoService.cs:97,186,469,759`
- `WarehouseProduct/WarehouseProduct.DataContext/Entity/LuotNhatKho.cs:7`

FE root: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`
- `src/app/pages/landing-page/landing-page-routing.module.ts:34-53`
- `src/app/pages/WarehouseProduct/warehouseProduct-routing.module.ts:184-187`
- `src/app/app-routing.module.ts:84-92,172-180`
- `src/app/pages/landing-page/cauhinh-trangthai-giaohang/cauhinh-trangthai-giaohang.component.ts`
- `src/app/services/orderLogistic/common.service.ts:59-66`
