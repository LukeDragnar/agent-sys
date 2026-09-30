# Luồng — Quản lý Website

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId); đơn vị có **tiện ích Website (TI_Website)**.
- Thao tác ghi (thêm/sửa/xóa) phần lớn yêu cầu DV-AD-TONG.
- Để website chạy: đã khai báo **domain** và gán **theme** cho link website.

## Luồng cấu hình (admin)

**Tin tức:**
1. `TimTheoDieuKien` (list, loại DV-NV) / `LayTheoNhomTinTuc` (lọc theo nhóm).
2. `ThemMoi` / `CapNhatThongTin` / `Xoa/{id}` (scope DV-AD-TONG).
3. Storefront render qua endpoint Private (`LayTatCa_Private`, `PrivateGetTinTucByIds`).

**Banner:**
1. `TimTheoDieuKien` (lọc theo đơn vị + KeyWord/ViTri/IsSuDung; sort ViTri→Stt→NgayCapNhat).
2. `ThemMoi` (validate Ten/ViTri/NgayBatDau bắt buộc; chặn NgayKetThuc < NgayBatDau) / `TimTheoId/{id}` + `CapNhatThongTin` / `Xoa/{id}`.

**Domain:**
1. `TimTheoDieuKien` (list theo đơn vị) / `GetDomainToSetLinkWebsite` (domain `IsSuDung` của đơn vị HOẶC `IdDonVi = Guid.Empty` dùng chung).
2. `ThemMoi` (validate Ten + **check trùng tên toàn hệ thống**) / `TimTheoId/{id}` / `CapNhatThongTin` (tìm theo Id + đúng đơn vị) / `Xoa/{id}` (đúng đơn vị).

**Theme:**
1. `TimTheoDieuKien` (grid theme) / `GetThemeToSetLinkWebsite` (gán theme cho link website).
2. CRUD theme **đã comment** ở controller đơn vị → thực hiện ở bản superadmin (route `api/QuanTriArea/Theme`).

**Chuyên gia:**
1. `TimTheoDieuKien` (set `IdDonVi = CurrentUser.DonViId`) / `TimTheoId/{id}`.
2. `ThemMoi` / `CapNhatThongTin` / `Xoa/{id}`.
3. Storefront render qua `TimTheoDieuKienPrivate` / `TimTheoIdPrivate/{id}` (Private).

## Luồng dùng (storefront — khách xem web)
1. Trang web công khai gọi các endpoint Private (`LayTatCa_Private` tin tức, `TimTheoDieuKienPrivate` chuyên gia, theme/banner theo cấu hình) để render nội dung cho khách.

## Case biên
- **Domain — check trùng tên không lọc đơn vị** (`AnyAsync(x => x.Ten == model.Ten)`) → 2 đơn vị không tạo cùng tên domain. (cần product xác nhận có đúng chủ ý "tên miền duy nhất toàn cục".)
- **Domain dùng chung (`IdDonVi = Guid.Empty`)** → vẫn hiện khi gán link website.
- **GetDomainToSetLinkWebsite không phân trang** (đoạn skip/take comment) → đơn vị nhiều domain → dropdown nặng.
- **CapNhat domain chỉ map `Ten` + `IsSuDung`** — các field liên hệ (NguoiLienHe/Sdt/DiaChi/Email) đã comment → UI gửi lên cũng không lưu.
- **Banner NgayKetThuc < NgayBatDau** → chặn, nhưng message diễn đạt ngược ("Ngày bắt đầu không được lớn hơn ngày kết thúc").
- **Sửa/xóa banner/domain/chuyên gia sai đơn vị** → trả NotFound (message một số chỗ ghi nhầm "Không tìm thấy nhà cung cấp!").
- **Theme: cố sửa/xóa ở controller đơn vị** → không có (đã comment) → phải dùng bản superadmin.
- **ChuyenGia Private không ép IdDonVi từ token** → caller phải tự truyền `IdDonVi`, thiếu có thể trả chéo đơn vị.

## Ví dụ cụ thể có số
Shop G (có tiện ích Website) cấu hình trang web:
- Khai báo domain `shopg.vn` → hệ thống check trùng toàn hệ thống, chưa ai dùng → tạo OK. Nếu shop khác đã có `shopg.vn` → báo "đã tồn tại".
- Gán domain `shopg.vn` + theme "Spa01" cho link website → storefront chạy với giao diện Spa01.
- Thêm 3 banner: banner "Sale 6/6" vị trí Top, hiển thị 1–6/6. Ngày 7/6 banner tự ẩn (ngoài khoảng thời gian).
- Thêm 5 chuyên gia → hiển thị trên trang "Đội ngũ" của web.
- Thử thêm banner với NgayBatDau 10/6, NgayKetThuc 5/6 → bị chặn (kết thúc < bắt đầu).

## Hậu điều kiện
- CRUD tin tức/banner/domain/chuyên gia → ghi DB (lọc theo đơn vị).
- Nội dung được storefront render qua endpoint Private.
- Đổi/xóa domain hoặc theme đang gán → ảnh hưởng cấu hình website đang chạy.
