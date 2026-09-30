# Dev — Quản lý ca làm việc (Biên bản bàn giao ca)

> Service: **WarehouseProduct** (BE .NET) · Tiện ích nhóm `TI_Order` ("Đơn hàng post"). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Danh sách biên bản giao ca | `bien-ban-giao-ca` | `bien-ban-giao-ca.component` |
| Popup sửa biên bản | (modal) | `bien-ban-giao-ca-edit` |
| Nhập đầu ca | (modal) | `bien-ban-giao-ca-nhan-dau-ca` |
| Nhập cuối ca | (modal) | `bien-ban-giao-ca-nhan-cuoi-ca` |

## Endpoint / Controller (trace registry)
**`BienBanBanGiaoCaController`** (service WarehouseProduct, `[TienIchAttribute(..., "Đơn hàng post", MaTienIchSCRM.TI_Order)]`). Role gồm DV-AD-TONG/DV-AD-CN/HT-KT/DV-KT/DV-KT-TONG/DV-QLDV.

| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tìm theo điều kiện (grid) | POST `api/BienBanBanGiaoCa/TimTheoDieuKien` | `BienBanBanGiaoCaController_TimTheoDieuKienAsync` |
| Thêm mới | POST `api/BienBanBanGiaoCa/ThemMoi` | `BienBanBanGiaoCa_ThemMoi` |
| Tìm theo Id | GET `api/BienBanBanGiaoCa/TimTheoId/{id}` | `BienBanBanGiaoCa_TimTheoId` |
| Tìm chi tiết theo Id | POST `api/BienBanBanGiaoCa/TimChiTietTheoId` | `BienBanBanGiaoCa_TimChiTietTheoId` |
| Cập nhật thông tin | POST `api/BienBanBanGiaoCa/CapNhatThongTin` | `BienBanBanGiaoCa_CapNhatThongTin` |
| Cập nhật người nhận đầu ca | POST `.../CapNhatThongTinNguoiNhanDauCa` | `BienBanBanGiaoCa_CapNhatThongTinNguoiNhanDauCa` |
| Cập nhật người nhận cuối ca | POST `.../CapNhatThongTinNguoiNhanCuoiCa` | `BienBanBanGiaoCa_CapNhatThongTinNguoiNhanCuoiCa` |
| Thêm chi tiết (danh sách) | POST `.../ThemMoiChiTietAsync` | `BienBanBanGiaoCa_ThemMoiChiTietAsync` |
| Xác nhận đầu ca | POST `.../UpdateIsXacNhanDauCa` | `BienBanBanGiaoCa_UpdateIsXacNhanDauCa` |
| Xác nhận cuối ca | POST `.../UpdateIsXacNhanCuoiCa` | `BienBanBanGiaoCa_UpdateIsXacNhanCuoiCa` |
| Set tổng SP nhận theo kiểu dữ liệu | POST `.../SetTongSanPhamNhanTheoIdVaKieuDuLieu` | `BienBanBanGiaoCa_SetTongSanPhamNhanTheoIdVaKieuDuLieu` |
| Cập nhật tiền thiếu cuối ca | POST `.../CapNhatTienThieuCuoiCa` | `BienBanBanGiaoCa_CapNhatTienThieuCuoiCa` |
| Xóa | POST `.../Xoa/{id}` | `BienBanBanGiaoCa_Xoa` |

**`BienBanBanGiaoCaChiTietController`** (CRUD dòng chi tiết, admin):
| Hành động | Endpoint | Quyền | Lưu ý |
|---|---|---|---|
| Tìm theo điều kiện | POST `api/BienBanBanGiaoCaChiTiet/TimTheoDieuKien` | `..._TimTheoDieuKien` (DV-AD-TONG) | - |
| Thêm mới | POST `.../ThemMoi` | `..._ThemMoi` (HT-AD) | **chặn cứng `!IsSuperUser`** |
| Tìm theo Id | GET `.../TimTheoId/{id}` | `..._TimTheoId` (DV-AD-TONG) | guard tenant |
| Cập nhật | POST `.../CapNhatThongTin` | `..._CapNhatThongTin` (HT-AD, DV-AD-TONG) | KHÔNG chặn IsSuperUser |
| Xóa | POST `.../Xoa/{id}` | `..._Xoa` (HT-AD) | **chặn cứng `!IsSuperUser`** |

## Entities
- **`BienBanBanGiaoCa`** (header) — trường: `IsXacNhanDauCa`, `IsXacNhanCuoiCa` (2 cờ vòng trạng thái), `NguoiTaoBienBanUserId` (dùng cho guard xóa), `IdDonVi`, tiền thiếu cuối ca.
- **`BienBanBanGiaoCaChiTiet`** — mỗi dòng = 1 sản phẩm/khoản nhận trong ca; thuộc về 1 `BienBanBanGiaoCa`; có `IdDonVi`.

## Luồng code
- **Vòng trạng thái:** `Nhap → UpdateIsXacNhanDauCa (IsXacNhanDauCa=true) → UpdateIsXacNhanCuoiCa (IsXacNhanCuoiCa=true)`. Đã xác nhận → khóa xóa (và service khóa sửa các field tương ứng).
- **Xóa** (controller:92-110), thứ tự kiểm tra:
  1. `data == null || IdDonVi != CurrentUser.DonViId` → NotFound (guard tenant).
  2. `IsXacNhanDauCa == true || IsXacNhanCuoiCa == true` → "Không thể xóa biên bản đã được xác nhận!".
  3. `NguoiTaoBienBanUserId == CurrentUser.Id || CurrentUser.IsSuperUser` → cho xóa; ngược lại "Người tạo phiếu mới có thể xóa!".
- Chi tiết được thêm chủ yếu qua `BienBanBanGiaoCaController.ThemMoiChiTietAsync` + `SetTongSanPhamNhanTheoIdVaKieuDuLieu`; `BienBanBanGiaoCaChiTietController` là CRUD trực tiếp cho admin hệ thống.

## Bug-prone / lưu ý
- **Block guard `IsSuperUser` ở đầu `Xoa` bị comment** (`BienBanBanGiaoCaController.cs:96-99`) — chỉ còn 3 điều kiện guard sau đó. Lưu ý nếu cần khôi phục.
- **Nhãn quyền copy-paste sai (chỉ label, không ảnh hưởng logic):**
  - `CapNhatTienThieuCuoiCa` (controller:132) nhãn = "Thêm mới người nhận cuối ca".
  - `CapNhatThongTinNguoiNhanCuoiCa` (controller:54) cũng dùng "Thêm mới người nhận cuối ca".
  - `BienBanBanGiaoCaChiTietController.TimTheoId` (controller:58) message lỗi ghi "Không tìm thấy Q&A!" (copy từ controller khác).
- **Bất đối xứng quyền chi tiết:** Thêm/Xóa dòng chi tiết chặn cứng `!IsSuperUser` (HT-AD) nhưng Cập nhật thì DV-AD-TONG cũng được — *cần xác nhận* có chủ ý.
- **Tiện ích gắn nhóm `TI_Order` ("Đơn hàng post")** dù entity nằm ở service WarehouseProduct — lưu ý khi map menu.
- `partial class` — *cần xác nhận* không có file partial bổ sung.

## File source (path:line)
- BE controller header: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\WarehouseProduct\WarehouseProduct.API\Controllers\BienBanBanGiaoCaController.cs` (Xóa: `:92-110`, guard comment `:96-99`, nhãn `:54`/`:132`).
- BE controller chi tiết: `...\WarehouseProduct.API\Controllers\BienBanBanGiaoCaChiTietController.cs` (ThemMoi `:44-47`, Xóa `:71-74`, guard tenant `:56`/`:76`, message Q&A `:58`).
- Service: `IBienBanBanGiaoCaService`, `IBienBanBanGiaoCaChiTietService` (namespace WarehouseProduct).
