---
controller: TrangThaiKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/trangthai-khachhang (danh mục trạng thái KH theo đơn vị) · quan-ly-trangthai-khachhang-admin (admin cấu hình trạng thái dùng chung)"
muc_dich: "CRUD danh mục TRẠNG THÁI khách hàng (do đơn vị tự định nghĩa) + cập nhật hàng loạt; có cache theo đơn vị, clear cache khi thêm/xóa"
alias: [TrangThaiKhachHang, "Trạng thái khách hàng", "Trạng thái KH", "Customer status"]
auth: mixed
state_machine: []   # trạng thái KH là danh mục động do đơn vị tự định nghĩa, KHÔNG phải tập enum cố định
deprecated: false
entities: [TrangThaiKhachHang]
stored_procs: []
depends: [ITrangThaiKhachHangService, ICommonCacheService, BaseController]
cross_module_in: ["(private) service khác gọi GetAllTrangThaiKhachHangByDonViPrivate để map tên trạng thái theo đơn vị"]
cross_module_out: ["Cache trạng thái KH theo đơn vị (ICommonCacheService — clear khi thêm/xóa)"]
---

# TrangThaiKhachHang — Danh mục trạng thái khách hàng

> Lưu ý: đây là **danh mục trạng thái KH do từng đơn vị tự định nghĩa** (mỗi đơn vị một bộ), KHÔNG phải vòng đời/state machine cố định của 1 khách hàng → `state_machine` để rỗng. Có **cache theo đơn vị**, clear cache khi thêm/xóa.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm trạng thái theo điều kiện | Lưới "Trạng thái KH" | `POST TimTheoDieuKien` | TrangThaiKhachHang_TimTheoDieuKien | public | — | `TimTheoDieuKien(gridInfo, CurrentUser)` theo scope đơn vị | EF | — |
| 2 | Lấy theo id | (mở chi tiết) | `GET TimTheoId/{id}` | TrangThaiKhachHang_TimTheoId | public | — | `GetByIdAsync(id)`; nếu có `Data` trả Data, ngược lại trả nguyên result | EF | Trả 2 shape khác nhau tùy `item.Data` null hay không → FE phải xử lý cả 2 |
| 3 | Lấy tất cả trạng thái theo đơn vị | (dropdown chọn trạng thái KH) | `GET GetAllTrangThaiKhachHangByDonVi` | TrangThaiKhachHang_GetAllTrangThaiKhachHangByDonVi (rất nhiều role) | public | — | `GetAllTrangThaiKhachHangByDonVi()` — danh sách dùng cho dropdown/lọc | EF (+ cache) | — |
| 4 | Thêm trạng thái | Nút "Thêm mới" | `POST ThemMoi` | TrangThaiKhachHang_ThemMoi (DV-AD-TONG, DV-QLDV) | public | — | `InsertAsync(model)` → **clear cache** trạng thái theo `CurrentUser.DonViId` | EF + clear cache | — |
| 5 | Cập nhật trạng thái | Nút "Lưu" (sửa) | `POST CapNhatThongTin` | TrangThaiKhachHang_CapNhatThongTin (DV-AD-TONG, DV-QLDV) | public | — | `UpdateAsync(model)` | EF | Không clear cache sau update (chỉ Thêm/Xóa clear) → cache có thể lệch tên |
| 6 | Cập nhật nhiều bản ghi | Lưu hàng loạt (sắp xếp/bật-tắt) | `POST UpdateMany` | TrangThaiKhachHang_UpdateMany (DV-AD-TONG) | public | — | ModelState invalid → MESS_Model_UnValidate; hợp lệ → `UpdateMany(gridInfo, CurrentUser)` | EF | Không clear cache sau UpdateMany |
| 7 | Xóa trạng thái | Nút "Xóa" | `POST Xoa/{id}` | TrangThaiKhachHang_Xoa (DV-AD-TONG, DV-QLDV) | public | — | Nếu `GetByIdAsync(id).Data == null` → trả result (không xóa); ngược lại `DeleteAsync(id)` → **clear cache** theo đơn vị → "Xóa thành công" | EF + clear cache | — |

## Endpoint nội bộ (private / service-to-service)

- [internal] `GET GetAllTrangThaiKhachHangByDonViPrivate/{idDonVi}` (`[PrivateSBAuthorize]`) — service khác lấy danh mục trạng thái KH theo idDonVi (để map id → tên trạng thái), không dùng CurrentUser.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)

- **Cache theo đơn vị:** danh mục trạng thái KH được cache qua `ICommonCacheService`. **Clear cache** (`ClearCache_TrangThaiKhachHang_DonVi(CurrentUser.DonViId)`) khi **Thêm** (`ThemMoi`) và **Xóa** (`Xoa`). **KHÔNG** clear khi `CapNhatThongTin`/`UpdateMany` → đổi tên trạng thái có thể không phản ánh ngay tới nơi đọc từ cache.
- **Xóa an toàn:** trước khi `DeleteAsync`, kiểm tra bản ghi tồn tại (`GetByIdAsync.Data == null` → bỏ qua, không xóa).

## Quan hệ
- **Gọi sang:** `ITrangThaiKhachHangService` (CRUD danh mục) · `ICommonCacheService` (cache theo đơn vị).
- **Dữ liệu (entities):** `TrangThaiKhachHang` (danh mục, theo đơn vị).
- **Ripple / đồng bộ:** thay đổi danh mục → clear cache đơn vị (chỉ ở Thêm/Xóa); `KhachHang` tham chiếu trạng thái này khi hiển thị/lọc KH (cần xác nhận FK).

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `TrangThaiKhachHangController.cs:59-66` (`CapNhatThongTinNhan`) & `:36-46` (`UpdateMany`) — **không gọi clear cache** sau khi sửa, trong khi `ThemMoi` (`:55`) và `Xoa` (`:94`) đều clear → cache đơn vị có thể giữ tên/cấu hình cũ sau khi sửa.
- `TrangThaiKhachHangController.cs:73-78` (`GetById`) — trả về 2 cấu trúc khác nhau (`item.Data` vs nguyên `item`) tùy null → client cần handle cả 2 shape.
- `TrangThaiKhachHangController.cs:18` — controller **không** có `[SBAuthorize]` mức class; mỗi action tự khai key; riêng endpoint private dùng `[PrivateSBAuthorize]`.
- Trạng thái KH là **danh mục động theo đơn vị** (không phải enum cố định) → `state_machine` để rỗng có chủ ý.
