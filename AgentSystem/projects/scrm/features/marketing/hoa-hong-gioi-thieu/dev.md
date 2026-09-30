# Dev — Hoa hồng giới thiệu

> Service: **CommissionFee** (BE .NET) · registry module `nhan-su` (gom vào feature Marketing theo INDEX). Việc **tính & ghi hoa hồng** nằm ở `CommissionFeeUpdateOrder` (khi đơn được xử lý), 2 controller này chỉ CRUD cấu hình + đọc thống kê. Chỉ dành cho dev.

> ⚠️ **PHÂN BIỆT — tránh nhầm khi tra (2 cơ chế "hoa hồng" khác nhau):**
> - **Feature NÀY = hoa hồng CTV (cộng tác viên).** Entity `CongTacVien` / `NhomHoaHongCTV`, service **`CommissionFee`**, controller `CongTacVienController` / `NhomHoaHongCTVController`. Tính & ghi qua `CommissionFeeUpdateOrder`. Registry: `registry/nhan-su/CongTacVienController.md`, `registry/nhan-su/NhomHoaHongCTVController.md`.
> - **KHÁC với `HoaHongGioiThieuController`** (service **`Promotion`**) — đó là **hoa hồng theo NGUỒN giới thiệu**, thuộc feature [`nguoi-gioi-thieu`](../nguoi-gioi-thieu/), entity `HoaHongGioiThieu` / `NguonGioiThieu` / `CauHinhHoaHongGioiThieu`, sinh tự động qua `PromotionUpdateWithBillService.HoaHongGioiThieuUpdate`. Registry: `registry/marketing/HoaHongGioiThieuController.md`.
> - Hai cái **không liên quan code/entity/service** với nhau dù tên tiếng Việt na ná ("hoa hồng giới thiệu" vs "hoa hồng CTV"). Khi grep theo từ "hoa hồng" sẽ ra cả hai — xác định đúng service (`CommissionFee` = CTV, `Promotion` = nguồn giới thiệu) trước khi sửa.

## Màn hình & route (FE)
| Màn hình | Mục đích |
|---|---|
| Hoa hồng CTV > Cộng tác viên | danh sách + thêm/sửa; tab thống kê HH theo CTV |
| Hoa hồng CTV > Nhóm hoa hồng | danh sách + thêm/sửa cấu hình; nút Sao chép / Áp dụng |

## Endpoint / Controller (trace registry)

**CongTacVienController** (service `CommissionFee`):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Thêm / Cập nhật / Xóa | POST `ThemMoi` / `CapNhat` / `Xoa/{id}` | `CongTacVien_ThemMoi` / `_CapNhat` / `_Xoa` (AD-TONG/CN/MCN) |
| Lấy theo Id | POST `LayTheoId/{id}` | `CongTacVien_LayTheoId` |
| Check tồn tại | POST `CheckById/{id}` | `[SBAuthorize()]` rỗng — **quyền lỏng** |
| Lọc grid | POST `TimTheoDieuKien` | `CongTacVien_TimTheoDieuKien` (non-admin ép chi nhánh) |
| Autocomplete | POST `TimKiemAutoComplete` | `[SBAuthorize()]` rỗng (key cũ comment) — **quyền lỏng** |
| Thống kê HH sản phẩm | POST `GetStats_SanPham_ByCTV` (+`_LienKet`) | `CongTacVien_GetStats_SanPham_ByCTV*` → SP `PRC_CommissionFee_GetStats_CTV[_LienKet]` |
| Thống kê HH doanh số | POST `GetStats_DoanhSo_ByCTV` (+`_LienKet`) | key tương ứng → SP `PRC_CommissionFee_GetStat_DoanhSo_ByIdCTV[_LienKet]` |
| Thống kê HH chiến dịch | POST `GetStats_ChienDich_ByCTV` / `_Detail_ByCTV` | key tương ứng → SP `PRC_CommissionFee_GetStats_CTV_ChienDich` |
| Lấy KM theo CTV | POST `GetListKhuyenMaiTheoCTV` | `[SBAuthorize()]` rỗng — **quyền lỏng** |
| Import | POST `DownloadFileTemplateImport` / `Import` (IFormFile), GET `DownloadDataWrong/{id}` | `CongTacVien_*` (AD-TONG) |

**NhomHoaHongCTVController** (service `CommissionFee`, master cấu hình):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Thêm / Cập nhật / Xóa | POST `ThemMoi` / `CapNhat` / `Xoa/{id}` | `NhomHoaHongCTV_ThemMoi` / `_CapNhat` / `_Xoa` (AD-TONG/CN/MCN); Xóa qua SP `PRC_CommissionFee_DeleteNhomHoaHong_ById` |
| Lấy theo Id | POST `LayTheoId/{id}` | `NhomHoaHongCTV_LayTheoId` |
| Lọc grid | POST `TimTheoDieuKien` | `NhomHoaHongCTV_TimTheoDieuKien` (non-admin ép chi nhánh) |
| Autocomplete | POST `TimKiemAutoComplete` | `NhomHoaHongCTV_TimKiemAutoComplete` |
| Bật/tắt sử dụng | POST `CapNhatTrangThaiSuDung/{id}/{loaiTrangThai}` | `NhomHoaHongCTV_CapNhatTrangThaiSuDung` |
| Sao chép / Áp dụng | POST `Copy` / `Apply` | `NhomHoaHongCTV_Copy` / `_Apply` |
| Import | POST `DownloadFileTemplateImport` / `Import`, GET `DownloadDataWrong/{id}` | `NhomHoaHongCTV_*` |

> Cấu hình con: `NhomHoaHongCTVSanPhamController`, `NhomHoaHongCTVDoanhSoController`, `NhomHoaHongCTVChienDichController`.

## Entities
- **`CongTacVien`** — CTV (+ nhân viên `KieuLoai=NhanVien`); gắn `IdNhomHoaHongCTV`, `IdUser`, `MaCTV`.
- **`NhomHoaHongCTV`** — master; cờ `IsSuDungHoaHongChienDich` quyết định phát sinh hoa hồng chiến dịch.
- Cấu hình con: `CauHinhHoaHongSanPhamCTV`, `CauHinhHoaHongDoanhSoCTV`, cấu hình chiến dịch.
- Báo cáo đọc `DonHang`, `DonHangChiTiet`, `DonHangChiTietChienDich` (do CommissionFeeUpdateOrder ghi).

## Luồng code
1. CRUD cấu hình ở 2 service tương ứng. Xóa nhóm → SP dọn cấu hình con.
2. Đơn gắn `IdCongTacVien` → `CommissionFeeUpdateOrder` tính theo nhóm → ghi DonHangChiTiet(_ChienDich).
3. Thống kê: controller gọi 5 SP `PRC_CommissionFee_*` qua SqlSugar.

## Bug-prone / lưu ý (path:line)
- **Quyền lỏng (chỉ login)** — `CongTacVienController.cs:71,96,186`: `CheckById`, `TimKiemAutoComplete`, `GetListKhuyenMaiTheoCTV` dùng `[SBAuthorize()]` rỗng; `TimKiemAutoComplete` còn để key cũ ở comment (`:95`).
- **GET cho tải file** — `CongTacVienController.cs:218` & `NhomHoaHongCTVController.cs:147`: `DownloadDataWrong` dùng HTTP GET.
- **Thống kê phụ thuộc pipeline** — toàn bộ `GetStats_*` đọc dữ liệu do `CommissionFeeUpdateOrder` ghi; đơn chưa đẩy/đẩy lỗi → báo cáo trống dù đã bán.
- **Xóa nhóm qua SP (không EF cascade)** — `NhomHoaHongCTVController.cs` / SP `PRC_CommissionFee_DeleteNhomHoaHong_ById`: CTV còn gán nhóm bị xóa → cần kiểm tra ràng buộc trước.
- **Cờ `IsSuDungHoaHongChienDich`** — tắt cờ ở nhóm → đơn không sinh `DonHangChiTietChienDich` dù chiến dịch có cấu hình.

## File source (path:line)
- `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\CongTacVienController.cs` (`:71,95,96,186,218`).
- `...\NhomHoaHongCTVController.cs` (`:147`).
- Registry: `projects/scrm/registry/nhan-su/CongTacVienController.md`, `projects/scrm/registry/nhan-su/NhomHoaHongCTVController.md` (+ `NhomHoaHongCTVSanPham/DoanhSo/ChienDichController.md`).
