# Luồng — Quản lý nguồn dữ liệu

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị/chi nhánh.
- Vai phù hợp: CRUD nguồn dữ liệu cần admin + quản lý/lead/nhân viên MKT (gán user MKT chỉ admin); danh mục kênh tiếp nhận cần admin hệ thống/tổng (HT-AD, DV-AD-TONG).
- (Tùy chọn) Đã có cấu hình chia số / chia chat để gắn vào nguồn.

## Luồng dùng (Quản lý nguồn dữ liệu — landing)
1. Mở **Marketing → Quản lý nguồn dữ liệu** → hệ thống gọi `TimTheoDieuKienAsync` trả lưới nguồn theo quyền.
2. **Thêm mới** (`ThemMoi`) → đặt tên nguồn → lưu.
3. **Cấu hình** cho nguồn: `ChangeCauHinhChiaSo` (chia số), `ChangeCauHinhChiaChat` (chia chat), `ChangeKenhBanHang` (kênh bán hàng); bật/tắt sale tạo contact thủ công (`ChangeIsAllowSaleTaoThuCong`).
4. **Gán nhân viên MKT** (`UpdateMarketingUser`) — chỉ admin.
5. **Số liệu landing:** `Sanpham_LandingData_TimTheoDieuKien` / `_Save` / `_Xoa` — CRUD số liệu theo FB Ads/ngày.
6. **Nhập từ Excel:** tải template (`DownloadTemplateNguonDuLieuAsync`) → import (`ImportTemplateNguonDuLieu`) → xem danh sách tạm (`GetDanhSachNguonDuLieuImport`) → insert chính thức (`InsertDataImportToSanPhamLanding`); có thể xóa danh sách import tạm.

## Luồng cấu hình (Danh mục kênh tiếp nhận)
1. Mở **Quản lý kênh tiếp nhận** (admin tổng/hệ thống).
2. **Thêm mới** (`ThemMoi`) → ModelState check → map DTO → lưu.
3. **Sửa** (`CapNhatThongTin`) tương tự.
4. **Xóa** (`Xoa/{id}`) → kiểm tra tồn tại (NotFound nếu null) → xóa → **clear cache** kênh tiếp nhận của đơn vị.

## Luồng xuyên module (cấp dữ liệu cho hệ thống ngoài)
1. **Facebook/Ads** đẩy số liệu landing qua endpoint private (`GetLandingDataPrivate`, `ThemSanPhamLandingDataPrivate`, `CapNhatThongTinLandingDataPrivate`) → cập nhật `SanPhamLandingData`.
2. **Ecommerce** gọi `TimTheoIdWithNoAuthen/{id}` (anonymous) lấy landing không cần auth.
3. **Web booking** gọi `GetSanPhamLandingById/{id}` (anonymous) → đọc landing từ **cache Redis**.

## Case biên
- **Cấu hình chia chat không có key quyền:** `ChangeCauHinhChiaChat` dùng `[SBAuthorize("", ...)]` **key rỗng** → chỉ chặn bằng danh sách vai, không có permission key (lệch convention so với chia số).
- **Thêm/sửa kênh tiếp nhận không clear cache:** chỉ thao tác **Xóa** mới clear cache → sau thêm/sửa, danh sách kênh đơn vị **lệch** tới khi cache hết hạn hoặc có thao tác xóa.
- **Xóa kênh không tồn tại:** `GetByIdAsync` trả null → NotFound.
- **Cập nhật toàn bộ data landing:** `UpdateDataLanding` dùng HTTP **GET** cho thao tác ghi hàng loạt → không idempotent, dễ trigger ngoài ý muốn (xem dev).
- **Landing anonymous:** `GetSanPhamLandingById`, `TimTheoIdWithNoAuthen` trả thông tin chỉ dựa vào id, không qua token → cần đảm bảo không lộ dữ liệu nhạy cảm.
- **Cache landing lệch sau sửa:** `GetSanPhamLandingById` đọc cache Redis → sau Create/Update/Delete nguồn phải đảm bảo cache làm mới, nếu không web booking đọc dữ liệu cũ.

## Ví dụ cụ thể có số
Đơn vị X chạy 3 landing trong tháng:
- Thêm nguồn "Landing Hè" → gán cấu hình chia số "Vòng tròn 3 sale", bật sale tạo contact thủ công = false → lead chỉ tự đổ về, sale không tự tạo.
- Gán 2 nhân viên MKT phụ trách "Landing Hè" (admin thực hiện).
- Nhập Excel 50 nguồn dữ liệu mới: tải template → import → danh sách tạm 50 dòng (3 dòng lỗi định dạng) → insert 47 dòng hợp lệ vào nguồn dữ liệu.
- Facebook đẩy số liệu landing ngày 10/6: chi phí 2.000.000đ, 120 lead → ghi vào `SanPhamLandingData` của "Landing Hè".
- Admin tổng thêm kênh tiếp nhận "Zalo OA" → lưu OK, nhưng danh sách combo kênh ở màn khác chưa thấy ngay (cache) → sau khi xóa 1 kênh test, cache làm mới → "Zalo OA" xuất hiện.

## Hậu điều kiện
- Thêm/sửa/xóa nguồn → ghi `SanPhamLanding`; landing data ghi `SanPhamLandingData`.
- Xóa kênh tiếp nhận → ghi DB + **clear cache** `DMKenhTiepNhan_RemoveByDonVi`.
- Thêm/sửa kênh tiếp nhận → ghi DB nhưng **KHÔNG clear cache** (lệch — xem dev).
- Số liệu landing là input cho Báo cáo Marketing (lead/ngân sách theo nguồn).
