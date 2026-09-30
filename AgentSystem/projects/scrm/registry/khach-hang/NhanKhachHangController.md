---
controller: NhanKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/nhan-khachhang (danh mục Nhãn khách hàng — danh sách + thêm/sửa/xóa nhãn) · nhãn được gắn lên KH ở các màn hồ sơ KH360"
muc_dich: "CRUD danh mục Nhãn khách hàng theo đơn vị (kế thừa BaseDanhMucController) + cấp dropdown nhãn cho các màn khác; có cache nhãn theo đơn vị."
alias: ["Nhãn khách hàng", "Nhãn KH", "Tag khách hàng", "Customer label", "NhanKhachHang"]
auth: mixed
state_machine: []
deprecated: false
entities: [NhanKhachHang]
stored_procs: []
depends:
  - INhanKhachHangService
  - ICommonCacheService            # ClearCache_NhanKhachHang_DonVi
  - BaseDanhMucController          # TimTheoId / LayTatCa / ThemMoi / CapNhatThongTin kế thừa
cross_module_in:
  - "Service khác gọi GetAllNhanByDonViPrivate (PrivateSBAuthorize) để lấy dropdown nhãn"
cross_module_out: []
---

# NhanKhachHangController — Danh mục nhãn khách hàng

> Kế thừa `BaseDanhMucController<NhanKhachHang, NhanKhachHangDto>` → có sẵn 4 endpoint cơ bản (`TimTheoId`, `LayTatCa`, `ThemMoi`, `CapNhatThongTin`) ở mức `[SBAuthorize]` (login-only). Bảng dưới liệt kê các action **override/bổ sung** của controller này. Đây là CRUD danh mục đơn giản — không pad.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm nhãn theo điều kiện (list) | Lưới danh mục nhãn | `POST TimTheoDieuKien` | NhanKhachHang_TimTheoDieuKien (AD-TONG/AD-CN/AD-MCN/QLDV) | public | — | `_service.TimTheoDieuKienAsync(gridInfo)`; **KHÔNG check ModelState** (khác các action khác). | EF (`NhanKhachHang`) | — |
| 2 | Thêm mới nhãn | Nút "Thêm mới" (nhan-khachhang-edit) | `POST ThemMoiNhan` | NhanKhachHang_ThemMoiNhan (AD-TONG/QLDV/AD-CN/AD-MCN) | public | — | `InsertAsync(model)` rồi `ClearCache_NhanKhachHang_DonVi(CurrentUser.DonViId)`. | EF | — |
| 3 | Cập nhật nhãn | Nút "Lưu" (sửa) | `POST CapNhatThongTinNhan` | NhanKhachHang_CapNhatThongTinNhan (AD-TONG/QLDV/AD-CN/AD-MCN) | public | — | `UpdateAsync(model)`. **KHÔNG xóa cache** sau update (khác Thêm/Xóa). | EF | Quên clear cache → dropdown nhãn có thể hiển thị tên cũ tới khi cache hết hạn |
| 4 | Cập nhật nhiều nhãn cùng lúc | (bulk save) | `POST UpdateMany` | NhanKhachHang_UpdateMany (AD-TONG/AD-CN/AD-MCN) | public | — | ModelState invalid → lỗi; `UpdateMany(gridInfo, CurrentUser)`. **KHÔNG clear cache.** | EF | như #3 — không clear cache |
| 5 | Xóa nhãn | Nút "Xóa" | `POST XoaNhan/{id:Guid}` | NhanKhachHang_XoaNhan (AD-TONG/AD-CN/AD-MCN/QLDV) | public | — | `DeleteAsync(id)` rồi `ClearCache_NhanKhachHang_DonVi(CurrentUser.DonViId)`. | EF | Không kiểm tra nhãn còn gắn KH nào trước khi xóa (cần xác nhận ràng buộc) |
| 6 | Lấy tất cả nhãn theo đơn vị | Dropdown chọn nhãn | `GET GetAllNhanByDonVi` | NhanKhachHang_GetAllNhanByDonVi (full role rộng: AD/QLDV/SALE/KHO/KT/MKT...) | public | — | `GetAllNhanByDonVi()` — đọc theo đơn vị hiện tại, dùng cache. | EF/cache | — |
| 7 | Lấy chi tiết nhãn theo id | (mở form sửa) | `GET GetById/{id:Guid}` | NhanKhachHang_GetById (AD-TONG/AD-CN/AD-MCN/QLDV) | public | — | `GetByIdAsync(id)`; trả `item.Data` nếu có, ngược lại trả nguyên `item`. | EF | — |
| 8 | (Kế thừa Base) TimTheoId / LayTatCa / ThemMoi / CapNhatThongTin | — | `GET TimTheoId/{id}` · `GET LayTatCa` · `POST ThemMoi` · `POST CapNhatThongTin` | `[SBAuthorize]` (login-only, không key riêng) | public | — | Hành vi mặc định BaseDanhMucController; FE chủ yếu dùng các endpoint riêng (#2/#3) thay cho ThemMoi/CapNhatThongTin Base. | EF | Endpoint Base không có permission key riêng → chỉ chặn ở mức đăng nhập |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GET GetAllNhanByDonViPrivate/{id:Guid}` và `GET GetAllNhanByDonViPrivate` (`[PrivateSBAuthorize]`) — service khác lấy danh sách nhãn theo đơn vị (có/không truyền id đơn vị) để render dropdown/enrich tên nhãn.

## Quan hệ
- **Gọi sang:** `INhanKhachHangService` (CRUD), `ICommonCacheService.ClearCache_NhanKhachHang_DonVi` (invalidate cache nhãn theo đơn vị).
- **Dữ liệu (entities):** `NhanKhachHang` (EF). Nhãn được gắn lên KH ở chỗ khác (vd `UpdateKhachHangNhanPrivate` trong KhachHangController).
- **Ripple / đồng bộ:** cache nhãn theo đơn vị — chỉ Thêm (#2) & Xóa (#5) clear cache; Update (#3/#4) KHÔNG.

## Lỗi đã biết / lưu ý kỹ thuật
- `NhanKhachHangController.cs:117-123` (`CapNhatThongTinNhan`) — update nhãn nhưng **không gọi `ClearCache_NhanKhachHang_DonVi`** → tên nhãn cũ còn nằm trong cache dropdown.
- `NhanKhachHangController.cs:43-53` (`UpdateMany`) — bulk update cũng **không clear cache**.
- `NhanKhachHangController.cs:37-41` (`TimTheoDieuKien`) — bỏ check `ModelState.IsValid` (khác convention các action còn lại trong file).
- `NhanKhachHangController.cs:56-64` (`XoaNhan`) — xóa nhãn không thấy kiểm tra nhãn đang gắn KH (cần xác nhận service có chặn hay xóa cứng).
