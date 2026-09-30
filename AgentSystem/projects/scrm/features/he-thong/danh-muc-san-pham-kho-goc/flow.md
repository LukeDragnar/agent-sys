# Danh mục sản phẩm & kho (gốc — cấp hệ thống) — Luồng

## Tiền điều kiện
- Người dùng là **quản trị viên hệ thống**, đăng nhập khu quản trị (SandBoxArea), có role **HT-AD**.
- Tiện ích `TI_HeThong` được bật.
- Với màn lọc theo ngành nghề: đã có dữ liệu **Ngành nghề** (idNganhNghe) để chọn.

## Luồng dùng (chung cho các danh mục: SP / Nhóm SP / DMSanPham / Dịch vụ / Nhóm DV / DMKho)
1. Admin mở màn quản trị tương ứng.
2. (Nếu màn lọc theo ngành nghề) Chọn ngành nghề → gọi tìm theo điều kiện (`TimTheoDieuKien_SA` / `TimTheoDieuKien`).
3. Bấm **Thêm mới** → nhập thông tin → lưu (`ThemMoi_SA` / `ThemMoiDMSanPham_SA` / `ThemMoi`).
4. Mở chi tiết theo Id (`TimTheoId_SA` / `TimTheoId`) → sửa (`CapNhat_SA` / `CapNhatDMSanPham_SA` / `CapNhat`) → lưu.
5. Xóa mục không dùng (`Xoa_SA` / `XoaDMSanPham_SA` / `Xoa`).
6. Các màn nhóm/danh mục cung cấp **dropdown** (`DropdownSanPham_SA`, `DropdownNhomSanPham_SA`, `DropdownNhomDichVu_SA`…) để chọn khi gắn vào SP/DV.

## Các trường hợp / case biên
- **Combo / Thẻ dịch vụ cấp hệ thống** → **không có màn** (không tìm thấy controller SandBoxArea) → chỉ quản lý ở cấp đơn vị.
- **Danh mục kho (DMKho)** → dùng đúng bản **SandBoxArea** (`api/SanboxArea/DMKho`); bản trong `Controllers/` là stub rỗng (không endpoint) → nếu gọi nhầm sẽ không có chức năng.
- **Xóa DMSanPham / DMKho** → endpoint dùng HTTP **GET** (`XoaDMSanPham_SA`, `Xoa`), lệch convention POST nhưng khớp FE.
- **Không chọn ngành nghề** (với màn lọc theo ngành) → danh sách rỗng / không đúng tập danh mục mong đợi.

## Ví dụ cụ thể có số
Ngành "Spa & Làm đẹp" (idNganhNghe = G1) đang có 3 nhóm sản phẩm và 12 sản phẩm hệ thống.
- Admin chọn ngành "Spa & Làm đẹp" → màn nhóm SP hiển thị 3 nhóm (`LayTatCa_SA/{idNganhNghe}`).
- Admin thêm nhóm "Mỹ phẩm cao cấp" (`ThemMoi_SA`) → còn 4 nhóm.
- Admin thêm 2 sản phẩm hệ thống vào nhóm mới (`SanPham/ThemMoi_SA`) → ngành này còn 14 SP.
- Admin thêm 1 danh mục kho "Kho tổng - mẫu" (`DMKho/ThemMoi`) → lưu OK; bấm xóa danh mục kho test → gọi `DMKho/Xoa/{id}` (HTTP GET) → xóa.
- Admin tìm Combo cấp hệ thống → **không có màn** → phải làm ở cấp đơn vị.

## Hậu điều kiện
- Thêm/sửa/xóa danh mục → ghi vào các entity tương ứng (`SanPham`, `NhomSanPham`, `DMSanPham`, `DichVu`, `NhomDichVu`, `DMKho`).
- Các đơn vị tham chiếu/khởi tạo dữ liệu từ danh mục gốc này theo ngành nghề.
