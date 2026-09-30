# Dev — Cấu hình hệ thống cửa hàng

> Feature gộp 3 controller: **CauHinhController (Order)**, **DMMayInController** (service Order), **KhuVucController** (service Booking). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route / vị trí | Component |
|---|---|---|
| Cấu hình Order | `config-order` | `ConfigOrderComponent` |
| Cấu hình SuperAdmin (quản trị đơn vị) | `quantri/cauhinh-donvi-super-admin` | - |
| Danh mục máy in | `pages/Order/dm-may-in` | `DmMayInComponent` (+ `DmMayInEditComponent`) |
| Quản lý khu vực (sơ đồ bàn) | màn Booking/POS "Quản lý khu vực" | (view khu vực + chỗ ngồi theo cây) |

## Endpoint / Controller (trace registry)

### CauHinhController (service Order) — `CauHinhController.Order.md`
| Hành động | Endpoint | Quyền |
|---|---|---|
| Lấy cấu hình order | GET `api/CauHinh/Get` | (rỗng — login; key `...LayCauHinhOrder` đã comment) |
| Lưu cấu hình SuperAdmin | POST `api/CauHinh/Save_SuperAdmin` | `CauHinh_Order_LuuCauHinhOrderSuperAdmin` (HT-AD/HT-CSKH) |
| Lấy cấu hình SuperAdmin | POST `api/CauHinh/GetCauHinhSuperAdmin` | `CauHinh_Order_GetCauHinhSuperAdmin` |
| Lấy cấu hình init | GET `api/CauHinh/GetCauHinhInit` | (rỗng — login) |
| Lưu cấu hình init | POST `api/CauHinh/SaveCauHinhInit` | `CauHinh_Order_SaveCauHinhInit` (DV-AD-TONG) |
| Cập nhật cấu hình Invoice (đồng bộ) | POST `api/CauHinh/CapNhatInvoice` | (rỗng — login) |
| Kiểm quyền xem báo cáo đơn | GET `api/CauHinh/CheckQuyenXemReportOrder` | (rỗng — login) |
- Internal (`[PrivateSBAuthorize]`): `GetCauHinhOrder/{idDonVi}` (GET), `GetAllCauHinhSuperAdmin` (GET).
- **Deprecated:** `Save` (POST `api/CauHinh/Save`, key `CauHinh_Order_LuuCauHinhOrder`) — comment toàn bộ (controller:24-30); dùng `SaveCauHinhInit`.

### DMMayInController (service Order)
| Hành động | Endpoint | Quyền |
|---|---|---|
| Thêm mới | POST `api/DMMayIn/Create` | `DMMayInController_Create` |
| Cập nhật | POST `api/DMMayIn/Update` | `DMMayInController_Update` |
| Tìm theo Id | POST `api/DMMayIn/TimTheoId/{id}` | `UMMayInController_TimTheoId` (typo prefix) |
| Xóa | POST `api/DMMayIn/Delete/{id}` | `UMMayInController_Delete` (typo) |
| Bật/tắt sử dụng | POST `api/DMMayIn/ChangeSuDung/{id}` | `UMMayInController_ChangeSuDung` (typo) |
| Tìm theo điều kiện | POST `api/DMMayIn/TimTheoDieuKien` | `DMMayInController_TimTheoDieuKien` |
| Dropdown máy in | POST `api/DMMayIn/DropdownDMMayIn` | (rỗng — login; key đã comment) |

### KhuVucController (service Booking)
| Hành động | Endpoint | Quyền |
|---|---|---|
| Tìm theo điều kiện (khu vực + chỗ ngồi) | POST `api/KhuVuc/TimTheoDieuKien` (alias `TimChoNgoiTheoDonVi`) | `KhuVuc_TimTheoDieuKien` (alias chỉ login) |
| Thêm mới khu vực | POST `api/KhuVuc/ThemMoi` | `KhuVuc_ThemMoi` |
| Cập nhật khu vực | POST `api/KhuVuc/CapNhatThongTin` | `KhuVuc_CapNhatThongTin` |
| Xóa khu vực | POST `api/KhuVuc/Xoa/{id}` | `KhuVuc_Xoa` |
| DS bàn bi-a theo đơn vị | POST `api/KhuVuc/DanhSachBanBiaDonVi` | (rỗng — login) |
- Public rỗng: `TimChoNgoiDangDung`, `TimChoNgoiTheoIds`, `TimAllChoNgoiTheoDonVi[Tree]`, `TimAllChoNgoiTheoChiNhanhTree/{id}`, `GetAllKhuVucThuocChiNhanh`.
- Internal (`[PrivateSBAuthorize]`): `GetDSChiNhanhConfigSmartDevicePrivate`, `SaveChoNgoiSmartDevice`.
- Anonymous (`[AllowAnonymous]`): `GetAllKhuVucThuocChiNhanhAllowAll` (màn quảng cáo bi-a, body có IdDonVi/IdChiNhanh).

## Entities
- `CauHinh` / `CauHinhSetting` (CauHinhSettingDto — chứa cờ như `IsKhongChoPhepXoaDonHang`).
- `DMMayIn`.
- `KhuVuc` (Ten, GhiChu, SuDung, Stt, IdDonVi, IdChiNhanh), `ChoNgoi` (Ten, ThuTu, IdDevice, IdNhanVienPhucVu, IdKhuVuc); đọc thêm `DatLich`/`DatLichChiTiet`, `User` (nhân viên phục vụ).

## Luồng code
- **Cờ gate xóa đơn:** Admin set `IsKhongChoPhepXoaDonHang` (SaveCauHinhInit) → `DonHangController.Xoa` đọc cấu hình (GetAsync) → nếu true → "Đơn vị không cho phép xóa đơn hàng!" (`DonHangController.cs:831`).
- **Khu vực Thêm/Sửa/Xóa** → `LoadCache()` ghi đè cache `CacheKey.KHU_VUC` + `CacheKey.CHO_NGOI` theo DonViId+IdChiNhanh.
- **Xóa khu vực** → transaction: xóa toàn bộ ChoNgoi (`IsDelete != true`) thuộc khu vực rồi xóa khu vực; trước đó check `IsDangSuDung` (qua `TimChoNgoiTheoId`).

## Bug-prone / lưu ý
- **`CapNhatInvoice` chỉ login** dù là endpoint đồng bộ từ Invoice — không dùng `[PrivateSBAuthorize]`; cân nhắc chuyển internal.
- **DMMayIn permission key typo:** `TimTheoId`/`Delete`/`ChangeSuDung` dùng prefix `UMMayInController_*` thay vì `DMMayInController_*` (copy-paste typo) — hoạt động nếu key seed đúng nhưng không nhất quán. `DropdownDMMayIn` mất permission key (comment) → chỉ login.
- **KhuVuc `TimTheoDieuKien` join trong memory** — `KhuVucService.cs:220-221` `.AsEnumerable()` trên ChoNgoi + DatLich → kéo toàn bộ về app rồi LINQ-to-Objects → rủi ro hiệu năng khi nhiều bàn/lịch.
- **`IsDangSuDung` suy ra từ `DatLich.KhachHang.Contains(idChoNgoi)`** (`KhuVucService.cs:233`) — so khớp chuỗi JSON `KhachHang` chứa Id chỗ ngồi (không phải khóa ngoại) → dễ sai nếu chuỗi trùng.
- **`SaveChoNgoiSmartDeviceAsync` thiếu null-check thứ tự** (`KhuVucService.cs:604-607`) — DeepClone/Add chạy trước `if (choNgoi == null) continue;`, `entity` có thể null → nguy cơ NRE.
- **`TimChoNgoiTheoId` không null-check `kv`** (`KhuVucService.cs:437-439`) — nếu `FirstOrDefaultAsync()` null thì `kv.lstChoNgoi` ném NRE (luồng Xóa gọi method này trước khi xóa).
- **`TimTheoDieuKien` vs `TimChoNgoiTheoDonVi`** (controller:34/41) trùng logic nhưng endpoint sau chỉ login → khác mức quyền cho cùng dữ liệu.
- **`GetAllKhuVucThuocChiNhanhAllowAll`** `[AllowAnonymous]` — nhận IdDonVi/IdChiNhanh từ body (không token), cho màn quảng cáo bi-a — cần xác nhận không lộ dữ liệu nhạy cảm.

## File source (path:line)
- Cấu hình Order: BE `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Order\Order.API\Controllers\CauHinhController.cs` (Save comment `:24-30`); gate xóa đơn `...\Order.API\Controllers\DonHangController.cs:831`. FE `services/order/cau-hinh.service.ts`, `pages/caidat-hethong/configsystem/config-order/`, `pages/quantri/cauhinh-donvi-super-admin/`.
- Máy in: BE `...\Order\Order.API\Controllers\DMMayInController.cs`. FE `services/order/dm-may-in.service.ts`, `pages/Order/dm-may-in/`.
- Khu vực: BE controller `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Booking\Booking.API\Controllers\KhuVucController.cs`, service `...\Booking\Booking.Service\Generated\KhuVucService.cs` (`:220-221`, `:233`, `:437-439`, `:604-607`).
