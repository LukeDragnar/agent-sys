# Tính lương — Luồng

## Tiền điều kiện
- Người dùng có 1 trong các vai: `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, DV-QLDV`.
- Đã khai **cài đặt lương cơ bản** (`CaiDatLuongCoBan`) cho các nhân viên cần tính.
- **Chấm công / giờ làm việc** (`GioLamViecNhanVien`) kỳ đó đã có và (theo quy trình) đã duyệt — đây là nguồn công để tính lương tự động.

## Luồng dùng (kế toán)
1. Vào **Lương > Bảng lương nhân viên**, lọc/tìm bảng lương theo điều kiện (kỳ/chi nhánh) — `TimTheoDieuKien`.
2. **Thêm bảng lương** mới cho kỳ — `ThemMoi` (insert 1 bản ghi `LuongNhanVien`).
3. Mở bảng lương — `GetDetailBangLuong` trả chi tiết toàn bảng + danh sách nhân viên.
4. Bấm **Tạo lương tự động** — `TaoLuongChiTietNhanVienTuDong(idBangLuong)`: hệ thống lấy cài đặt lương cơ bản + công đã duyệt → sinh `LuongChiTietNhanVien` cho từng nhân viên của bảng.
5. Mở 1 nhân viên — `LayThongTinLuongTheoNv(idNhanVien, idBangLuong)` → chỉnh tay → **Lưu** `SaveLuongTheoNhanVien` (`LuuLuongChiTietNhanVien`).
6. Soát xong → **Đổi trạng thái bảng lương** — `CapNhatTrangThaiBangLuong(idBangLuong, type)` để duyệt/chốt.

## Luồng cấu hình (admin)
- Khai **cài đặt lương cơ bản** cho từng nhân viên (màn riêng — `CaiDatLuongCoBan`). Đây là dữ liệu nền cho bước tạo lương tự động.

## Các trường hợp / case biên
- **Tạo lương tự động khi thiếu cài đặt/công:** nhân viên không có cài đặt lương cơ bản hoặc chưa có công duyệt → chi tiết lương người đó trống hoặc thiếu khoản; cần bổ sung dữ liệu nền rồi tạo lại.
- **Sửa công sau khi đã tạo tự động:** bảng lương KHÔNG tự đồng bộ lại; phải chạy lại tạo tự động hoặc chỉnh tay.
- **Đổi trạng thái với `type` không hợp lệ / null:** controller nhận `idBangLuong`/`type` nullable và KHÔNG kiểm tra null trước khi gọi service (`LuongNhanVienController.cs:89`); mapping trạng thái nằm ở service — truyền sai có thể không chuyển trạng thái như mong đợi.
- **Tạo tự động chạy lại trên bảng đã có chi tiết:** cần xác nhận service ghi đè hay nhân đôi (cần product/dev xác nhận hành vi cụ thể).
- **Xóa bảng lương:** xóa cả bảng theo id — kiểm tra kỹ trước khi xóa bảng đã chốt.

## Ví dụ cụ thể có số
Chi nhánh CN1 có 10 nhân viên, kế toán lập bảng lương tháng 6.
- Bấm Thêm mới → tạo bảng `BL-2026-06-CN1`.
- Bấm Tạo lương tự động: 8 nhân viên có đủ cài đặt lương + công duyệt → sinh đủ; 2 nhân viên chưa khai cài đặt lương → chi tiết trống.
- Kế toán khai cài đặt lương cho 2 người còn thiếu, tạo lại tự động → đủ 10 dòng.
- Một nhân viên có thưởng doanh số 500.000đ chưa được tính tự động → mở chi tiết, cộng tay 500.000đ, Lưu.
- Soát xong, bấm Chốt (đổi trạng thái) → bảng lương khóa lại làm cơ sở chi lương.

## Hậu điều kiện
- Bảng lương + chi tiết lương từng nhân viên được ghi DB (`LuongNhanVien` + `LuongChiTietNhanVien`).
- Bảng lương chốt là kết quả cuối của chuỗi **chấm công → cài đặt lương → tính lương**; không phát event ra ngoài (theo registry node).
