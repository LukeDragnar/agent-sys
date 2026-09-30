# Danh mục khác (gốc — cấp hệ thống) — Luồng

## Tiền điều kiện
- Người dùng là **quản trị viên hệ thống**, chủ yếu role **HT-AD**.
- Nguồn đặt lịch (hệ thống): thao tác trên dữ liệu `IdDonVi == Guid.Empty` (danh mục gốc dùng chung).
- Ngành nghề / Lĩnh vực: khu QuanTriArea, tiện ích `TI_HeThong`.

## Luồng dùng (Nguồn đặt lịch — hệ thống)
1. Mở màn → `POST api/SanboxArea/NguonDatLich/TimTheoDieuKien_SA` (lọc `IdDonVi == Guid.Empty`, keyword theo Tên, phân trang).
2. **Thêm mới** (`ThemMoi_SA`): validate Tên (trim); nếu `MacDinh=true` → bỏ mặc định bản hệ thống cũ; chống trùng Tên; gán `IdDonVi=Guid.Empty`; insert.
3. **Cập nhật** (`CapNhat_SA`): validate Tên + chống trùng + guard mặc-định-duy-nhất → update.
4. **Đặt mặc định** (`SetMacDinh_SA`): transaction bỏ mặc định bản cũ → set bản này (lỗi → rollback).
5. **Xóa** (`Xoa_SA`, HTTP GET): chặn nếu `IdDonVi != Guid.Empty`; chặn nếu `Ma == TRUCTIEP/WEBSITE`; xóa cứng.

## Luồng dùng (Ngành nghề / Lĩnh vực)
1. **Ngành nghề** (`api/QuanTriArea/NganhNghe`): `TimTheoDieuKien`, `TimTheoId/{id}`, `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` (HT-AD). Service nội bộ gọi `LayTatCaNganhNghe` (private) để build dropdown / khởi tạo dữ liệu mẫu.
2. **Lĩnh vực** (`api/QuanTriArea/LinhVuc`): CRUD (HT-AD) + `GetTienIchTheoLinhVuc/{id}` + `CauHinhTienIchTheoLinhVuc` (gán bộ tiện ích cho lĩnh vực).

## Các trường hợp / case biên
- **Xóa nguồn lõi** (Ma = TRUCTIEP/WEBSITE) → bị chặn.
- **Xóa nguồn của đơn vị qua màn hệ thống** → chặn nếu `IdDonVi != Guid.Empty` ("không có quyền xóa").
- **ThemMoi_SA với MacDinh=true rồi trùng tên** → lỗi side-effect: code bỏ mặc-định bản cũ **TRƯỚC** khi check trùng tên → nếu trùng tên trả lỗi nhưng mặc-định cũ đã bị set false (data lệch, không rollback) — *xem dev.md*.
- **Xóa lĩnh vực đang có đơn vị tham chiếu** → không kiểm tra ràng buộc → đơn vị có thể mồ côi lĩnh vực.
- **Nhóm ngành nghề**: CRUD kế thừa không gắn key → mọi request gọi được (không gate role).

## Ví dụ cụ thể có số
Hệ thống có 3 nguồn đặt lịch gốc: "Trực tiếp" (TRUCTIEP), "Website" (WEBSITE), "Facebook"; mặc định = "Trực tiếp".
- Admin thêm nguồn "Zalo OA" và đặt mặc định → "Trực tiếp" bị bỏ cờ mặc định, "Zalo OA" thành mặc định. Hệ thống còn 4 nguồn.
- Admin thử xóa "Website" → bị chặn (Ma=WEBSITE là nguồn lõi).
- Admin xóa "Facebook" (`GET NguonDatLich/Xoa_SA/{id}`) → xóa cứng → còn 3 nguồn.
- Admin thêm ngành nghề "Nha khoa" (`NganhNghe/ThemMoi`, HT-AD) → đơn vị mới chọn ngành "Nha khoa" sẽ được khởi tạo dữ liệu mẫu theo ngành.
- Admin gán tiện ích "Đặt lịch" + "Tổng đài" cho lĩnh vực "Spa" (`CauHinhTienIchTheoLinhVuc`) → đơn vị thuộc lĩnh vực Spa được bật 2 tiện ích này.

## Hậu điều kiện
- Nguồn đặt lịch: thêm/sửa/xóa/đặt-mặc-định ghi DB (`NguonDatLich`, IdDonVi=Guid.Empty). KHÔNG tự đồng bộ ngược về đơn vị đã copy.
- Ngành nghề / Lĩnh vực: ghi DB (`NganhNghe` / `LinhVuc` + `LinhVucTienIch`); cấu hình tiện ích theo lĩnh vực ảnh hưởng bộ tính năng đơn vị thuộc lĩnh vực.
