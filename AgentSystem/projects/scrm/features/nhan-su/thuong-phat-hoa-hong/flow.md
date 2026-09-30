# Luồng — Thưởng phạt & hoa hồng nhân viên

## Tiền điều kiện
- Người thao tác đã đăng nhập với vai phù hợp: ghi thưởng/phạt cần key tương ứng; lưu cấu hình hoa hồng cần AD/QLDV (không cho KT); danh mục thưởng phạt ghi cần AD-TONG.
- **Danh mục thưởng phạt** (DMThuongPhat) đã khai báo để có loại chọn khi ghi thưởng/phạt.
- Có **nhân viên**, **nhóm hoa hồng** (nếu dùng HH theo nhóm), và **đối tượng** (dịch vụ/SP/thẻ/combo) để gắn thưởng doanh số.

## Luồng dùng (ghi thưởng/phạt)
1. Vào hồ sơ nhân viên → tab Thưởng phạt → `ThemMoiThuongPhatNv` (chọn loại từ DMThuongPhat, nhập số tiền, đánh dấu thưởng/phạt/khấu trừ) → lưu.
2. Xem: `GetThuongPhatByNhanVien?idNhanVien` (danh sách), `GetSumThuongPhatByNhanVien?idNhanVien` (tổng thưởng/phạt/khấu trừ).
3. Xóa khoản: `XoaTheoId/{id}`.

## Luồng cấu hình (hoa hồng)
1. Vào Cấu hình → Hoa hồng nhân viên, chọn tầng cấu hình:
   - HH đối tượng-NV: `LayDSHHDoiTuongTheoNhanVien` / `LayHHDoiTuongTheoNhanVien` → `LuuHoaHongNhanVien`.
   - HH tối đa (trần): `LayDSHHNhanVienToiDa` / `LuuHHNhanVienToiDa`.
   - HH mặc định theo đối tượng: `LayDSHHDoiTuongMacDinh` / `LuuHHDoiTuongMacDinh` (IdNhanVien = Guid.Empty).
   - HH theo nhóm hoa hồng: `LayDSHHDoiTuongTheoNhomHoaHong` / `LuuHHDoiTuongTheoNhomHoaHong`.
2. Áp mặc định cho NV chưa có: `TaoHHDoiTuongNhanVienVoiGiaTriMacDinh` (lấy giá trị từ HH mặc định).
3. Xem lịch sử: `GetListHoaHongLichSuByNhanVien`. Xóa: `XoaHH/{id}` (GET).

## Luồng cấu hình (thưởng doanh số)
1. Vào Cấu hình → Thưởng doanh số. Đặt tên, chọn panel (NhanVien/DichVu/SanPham/The/Combo), nhập các bậc (DoanhSoTu → DoanhSoDen → GiaTriThuong + đơn vị VND/%).
2. Bấm Lưu (`ThemMoiAsync`): validate bậc → trong transaction **xóa toàn bộ** `ThuongDoanhSo` + `ThuongDoanhSoChiTiet` của (đơn vị, chi nhánh) → tạo mới list.
3. Xem: `GetListThuongDoanhSo` (denormalize tên NV/dịch vụ/SP/thẻ/combo qua Core + WarehouseProduct).

## Các trường hợp / case biên
- **Thưởng doanh số — replace-all:** lưu = xóa hết cấu hình cũ chi nhánh rồi ghi mới; gửi list rỗng → xóa sạch.
- **Validate bậc doanh số:** model null → "Dữ liệu không được phép null"; tên rỗng → "Tên cấu hình tiền thưởng không được phép bỏ trống"; bậc có DoanhSoTu/DoanhSoDen/GiaTriThuong null hoặc = 0 → "Dữ liệu cài đặt doanh số không hợp lệ"; DoanhSoDen <= DoanhSoTu → "Giá trị doanh thu từ không thể lớn hơn giá trị doanh thu đến"; %>100 → "Giá trị thưởng không được > 100%".
- **Hoa hồng — chọn cấu hình:** ưu tiên HH theo NV (IdNhanVien == NV, đúng LoaiObject/IdLoaiObject); fallback HH mặc định (IdNhanVien == Guid.Empty) khi NV chưa có cấu hình riêng.
- **Hoa hồng — quyền xem trong đơn:** QLDV/Admin (chi nhánh/nhiều chi nhánh/tổng/hệ thống) lấy HH tất cả NV; còn lại chỉ HH của chính `userLogin.Id`.
- **Nạp thẻ → thẻ:** LoaiDoiTuong == NapThe được quy về The (nạp thẻ ăn chung HH thẻ).
- **Denormalize lỗi cross-service:** đọc thưởng doanh số nếu Core/WarehouseProduct lỗi → panel tương ứng để trống nhưng vẫn trả cấu hình.
- **Danh mục thưởng phạt — quyền:** ghi (ThemMoi/CapNhat/Xoa) chỉ AD-TONG; đọc mở cho KT/QLDV.

## Ví dụ cụ thể có số
Chi nhánh C, chính sách thưởng doanh số dịch vụ spa:
- Cấu hình "Thưởng DS spa": bậc 1 = 10.000.000 → 20.000.000 thưởng 500.000 VND; bậc 2 = 20.000.001 → 50.000.000 thưởng 1.500.000 VND. Đơn vị VND. Lưu → xóa cấu hình cũ chi nhánh C, ghi 2 bậc mới.
- Sửa lại bậc 2 thành "đến" = 15.000.000 (nhỏ hơn "từ" 20.000.001) → báo lỗi "Giá trị doanh thu từ không thể lớn hơn giá trị doanh thu đến", không lưu.
- Hoa hồng: đặt HH mặc định dịch vụ = 5% (IdNhanVien rỗng). Nhân viên Mai có HH riêng dịch vụ = 8% → đơn của Mai tính 8%; nhân viên Lan chưa cấu hình riêng → đơn của Lan tính 5% (mặc định).
- Quản lý mở đơn có 3 nhân viên phục vụ → thấy hoa hồng cả 3; nhân viên Lan mở cùng đơn → chỉ thấy hoa hồng của Lan.
- Ghi phạt cho nhân viên Hùng: loại "Đi muộn" 100.000 + loại "Quên đồng phục" 50.000 → `GetSumThuongPhatByNhanVien` trả tổng phạt 150.000.

## Hậu điều kiện
- Lưu hoa hồng / thưởng doanh số: chỉ ghi DB Branch, **không phát event**.
- Tổng thưởng/phạt + hoa hồng → đầu vào tính lương / tổng hợp công.
- Cấu hình hoa hồng → đơn hàng (Order) gọi vào để tính HH từng NV khi xếp nhân viên; đối soát qua `GetHoaHongApDungByListMaDon`.
