# Danh mục hành chính (địa giới — cấp hệ thống) — Luồng

## Tiền điều kiện
- Người dùng là **quản trị viên hệ thống**, có role **HT-AD** hoặc **HT-CSKH**.
- Cây địa giới theo thứ tự: **Quốc gia → Tỉnh/TP → Quận/Huyện → Phường/Xã**. Mỗi cấp con tham chiếu cấp cha qua Id.

## Luồng dùng (CRUD một cấp)
1. Admin mở màn cấp tương ứng → tìm theo điều kiện (`TimTheoDieuKien`, phân trang).
2. **Thêm mới** (`ThemMoi`):
   - Tỉnh: bắt buộc `IdQuocGia`. Quận/Huyện: bắt buộc `IdTinhThanh`. Phường/Xã: bắt buộc `IdTinhThanh` + `IdQuanHuyen`.
   - Validate Mã/Tên không trống, không trùng **trong phạm vi cấp cha** (qua `GetCodeByRequest`), trim khoảng trắng → `base.ThemMoi`.
3. **Cập nhật** (`CapNhatThongTin`): ModelState hợp lệ → map DTO→entity → kiểm tra tồn tại (GetById) → update. *Không re-check trùng Mã/Tên.*
4. **Xóa** (`Xoa/{id}`): kiểm tra tồn tại → `DeleteAsync`. *Không kiểm tra ràng buộc cấp con.*
5. **Dropdown cascade**: `TinhTP/GetTinhByQuocGia/{idQuocGia}`, `QuanHuyen/GetQuanHuyenByTinh/{idTinh}` để load dropdown địa chỉ.
6. **Cây địa chỉ tổng hợp**: `QuocGia/GetThongTinDiaChi` (public, cho form địa chỉ) và `GetThongTinDiaChi_Private` (service nội bộ build cây Quốc gia→Tỉnh→Quận→Xã).

## Các trường hợp / case biên
- **Thêm cấp con thiếu cấp cha** → lỗi (vd "Quốc gia không được bỏ trống" / "Tỉnh/TP không được bỏ trống").
- **Thêm trùng Mã/Tên trong cùng cấp cha** (chỉ khi `Id == null`) → lỗi "Mã, Tên không được phép trùng".
- **Sửa thành trùng Mã/Tên** → KHÔNG bị chặn (update không validate trùng) → có thể tạo trùng.
- **Xóa cấp cha còn cấp con** → vẫn xóa được; con thành mồ côi nếu DB không có FK chặn.
- **(Quốc gia) Thêm thiếu Tên** → do lỗi điều kiện validate (`Ma && Ma`, lặp Ma, bỏ sót Ten) → KHÔNG báo lỗi như mong đợi, có thể tạo quốc gia thiếu Tên.

## Ví dụ cụ thể có số
Hệ thống có 1 Quốc gia "Việt Nam", 63 Tỉnh/TP. Tỉnh "Hà Nội" có 30 Quận/Huyện.
- Admin thêm Quận/Huyện "Quận Test" vào "Hà Nội" (`QuanHuyen/ThemMoi`, chọn IdTinhThanh = Hà Nội) → Hà Nội còn 31 đơn vị cấp huyện.
- Admin thêm lại "Quận Test" lần nữa cùng Hà Nội → báo "Mã, Tên không được phép trùng".
- Admin sửa "Quận Test" thành tên trùng "Quận Hoàn Kiếm" → **không bị chặn** (update không check trùng) → tồn tại 2 mục trùng tên cùng tỉnh.
- Admin xóa "Hà Nội" (đang có 31 huyện) → `TinhTP/Xoa` vẫn cho xóa; 31 huyện con có thể mồ côi nếu DB không ràng buộc.

## Hậu điều kiện
- Thêm/sửa/xóa → ghi vào entity (`DMQuocGia` / `DMTinhThanh` / `DMQuanHuyen` / `DMPhuongXa`).
- Không bắn event; là danh mục nền — thay đổi phản ánh ngay vào dropdown địa chỉ toàn hệ thống.
