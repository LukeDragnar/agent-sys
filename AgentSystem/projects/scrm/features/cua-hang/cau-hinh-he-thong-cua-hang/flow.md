# Luồng — Cấu hình hệ thống cửa hàng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) + chi nhánh (IdChiNhanh).
- Lưu cấu hình Order init cần quyền DV-AD-TONG; cấu hình SuperAdmin cần HT-AD/HT-CSKH.
- Danh mục máy in & Khu vực: thao tác CRUD có gắn quyền (key `DMMayInController_*`, `KhuVuc_*`).

## Luồng cấu hình (admin)

### A. Cấu hình Order (cấu hình đơn vị)
1. Admin mở **Cài đặt hệ thống → Cấu hình Order** → hệ thống gọi `Get` / `GetCauHinhInit` lấy cấu hình hiện tại của đơn vị.
2. Chỉnh tham số (vd cờ `IsKhongChoPhepXoaDonHang`) → bấm **Lưu** (`SaveCauHinhInit`).
3. Cấu hình SuperAdmin: màn quản trị đơn vị (HT-AD/HT-CSKH) → `Save_SuperAdmin`.
4. Service Invoice đẩy cấu hình hóa đơn sang qua `CapNhatInvoice`.

### B. Danh mục máy in
1. Mở **Order → Danh mục máy in** → list theo điều kiện.
2. **Thêm mới / Sửa** (modal) → lưu; **bật/tắt** sử dụng (`ChangeSuDung`); **Xóa** (`Delete`).
3. Dropdown máy in được dùng ở các màn in đơn/POS.

### C. Quản lý khu vực (sơ đồ bàn)
1. Mở màn **Quản lý khu vực** → list khu vực kèm chỗ ngồi (theo cây).
2. **Thêm mới** khu vực: chặn trùng tên (cùng đơn vị+chi nhánh, đang sử dụng) → insert → làm mới cache khu vực + chỗ ngồi.
3. **Sửa**: chặn trùng tên (khác Id) → update → làm mới cache.
4. **Xóa**: nếu khu vực đang sử dụng → chặn; ngược lại xóa toàn bộ chỗ ngồi thuộc khu vực rồi xóa khu vực (trong 1 transaction) → làm mới cache.

## Các trường hợp / case biên
- **Cờ `IsKhongChoPhepXoaDonHang` bật** → khi xóa đơn ở màn bán hàng, hệ thống chặn: "Đơn vị không cho phép xóa đơn hàng!".
- **Thêm/Sửa khu vực trùng tên** (cùng đơn vị+chi nhánh, `SuDung=true`) → chặn trùng tên.
- **Xóa khu vực đang sử dụng** (`IsDangSuDung=true`) → "Khu vực đang sử dụng không thể xóa!".
- **Xóa khu vực không dùng** → xóa cascade toàn bộ chỗ ngồi thuộc khu vực (trong transaction).
- **Máy in tắt sử dụng** → không hiện trong dropdown chọn máy in khi in.
- **Cấu hình `Get`/`GetCauHinhInit`/`CheckQuyenXemReportOrder`** chỉ cần đăng nhập (không gắn quyền riêng).

## Ví dụ cụ thể có số
Cửa hàng D, chi nhánh Quận 1:
- Admin (DV-AD-TONG) vào Cấu hình Order, bật cờ "không cho phép xóa đơn hàng" → Lưu. Sau đó thu ngân thử xóa 1 đơn → bị chặn "Đơn vị không cho phép xóa đơn hàng!".
- Admin thêm 2 máy in: "Máy bếp 1" (bật sử dụng) và "Máy bar" (tắt) → khi in đơn bếp chỉ thấy "Máy bếp 1" trong dropdown.
- Admin tạo khu vực "Tầng 1" có 10 bàn, "Tầng 2" có 8 bàn. Thử tạo thêm khu vực tên "Tầng 1" lần nữa → chặn trùng tên.
- Bàn số 3 ở "Tầng 1" đang phục vụ khách → admin xóa khu vực "Tầng 1" → chặn "Khu vực đang sử dụng không thể xóa!". Sau khi bàn 3 trống, xóa "Tầng 1" → xóa luôn cả 10 bàn của khu vực.

## Hậu điều kiện
- **Lưu cấu hình Order:** ghi DB; cờ áp cho luồng đơn hàng/báo cáo về sau.
- **Thêm/Sửa/Xóa khu vực:** ghi DB + **làm mới cache KHU_VUC + CHO_NGOI** theo đơn vị+chi nhánh → POS/dropdown lần sau lấy dữ liệu mới.
- **Xóa khu vực:** chỗ ngồi thuộc khu vực bị xóa theo (cascade trong transaction).
- **CRUD máy in:** ghi DB; dropdown máy in cập nhật.
