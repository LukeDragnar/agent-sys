# Danh mục khách hàng (gốc — cấp hệ thống) — Luồng

## Tiền điều kiện
- Người dùng là **quản trị viên hệ thống**, đã đăng nhập khu quản trị (SandBoxArea).
- Màn **Hạng khách hàng (admin)**: cần role **HT-AD**.
- Màn **Nhóm khách hàng (admin)**: theo code chỉ cần đăng nhập (attribute không siết role) — *cần product xác nhận*.

## Luồng dùng (Nhóm khách hàng — admin)
1. Admin mở màn Danh mục nhóm khách hàng (admin) → gọi tìm theo điều kiện (phân trang).
2. Bấm **Thêm mới** → nhập tên nhóm → lưu (`ThemMoi_SA`).
3. Bật/tắt **"Sử dụng"** (`ChangeSuDung_SA`) → đảo cờ; nhóm bị tắt không hiển thị/áp cho khách mới.
4. Bật/tắt **"Tự động xét nâng nhóm khi thanh toán"** (`ChangeChoPhepXetNangNhom_SA`) → đảo cờ; khi TRUE hệ thống tự xét nâng nhóm lúc thanh toán.
5. Mở chi tiết (`TimTheoId_SA`) → sửa (`CapNhat_SA`) → lưu.

## Luồng dùng (Hạng khách hàng — admin)
1. Admin mở màn Danh mục hạng khách hàng (admin) → tìm theo điều kiện (`TimTheoDieuKien_SA`, role HT-AD).
2. Thêm mới (`ThemMoi_SA`) hoặc mở chi tiết (`TimTheoId_SA`) rồi cập nhật (`CapNhat_SA`).
3. Có thể lấy toàn bộ hạng theo đơn vị (`GetAllRankByDonVi_SA`) để đổ dropdown/cấu hình.

## Các trường hợp / case biên
- **Xóa nhóm KH** → **không hỗ trợ**: endpoint `Xoa_SA` đã bị comment trong code → không có đường xóa qua API admin này. Muốn ngừng dùng → tắt "Sử dụng".
- **Đổi cơ chế xếp hạng (hạng KH)** → **không hỗ trợ**: endpoint `ChangeCoCheXepHang_SA` đã bị comment → tính năng chưa bật.
- **ModelState không hợp lệ** (thiếu trường bắt buộc khi thêm/sửa hạng KH) → trả lỗi `MESS_Model_UnValidate`, không lưu.
- **Bật cờ tự động xét nâng nhóm** → đây là thao tác có ảnh hưởng ngoài màn (luồng thanh toán) — đổi rồi sẽ tác động lần thanh toán kế tiếp.

## Ví dụ cụ thể có số
Hệ thống đang có 4 nhóm KH gốc: "Khách lẻ", "Khách sỉ", "Khách thân thiết", "Khách VIP" và 4 hạng: Đồng/Bạc/Vàng/Kim cương.
- Admin thêm nhóm thứ 5 "Khách doanh nghiệp" → `ThemMoi_SA` → danh sách còn 5 nhóm.
- Admin bật "Tự động xét nâng nhóm khi thanh toán" cho nhóm "Khách thân thiết" → cờ chuyển TRUE. Lần sau khách thuộc nhóm này thanh toán, hệ thống tự xét điều kiện nâng nhóm.
- Admin muốn xóa nhóm "Khách doanh nghiệp" vừa tạo nhầm → **không có nút/đường xóa** → phải tắt "Sử dụng" để nhóm này không áp cho khách mới.
- Admin thêm hạng "Bạch kim" qua màn Hạng KH (HT-AD) → `ThemMoi_SA` lưu; tài khoản không có role HT-AD bấm cùng nút → bị chặn quyền.

## Hậu điều kiện
- Thêm/sửa nhóm/hạng → ghi vào DB (entity `DMNhomKhachHang` / `DMHangKhachHang`).
- Bật/tắt cờ sử dụng và cờ tự-động-xét-nâng-nhóm → cập nhật trạng thái nhóm; cờ tự-xét ảnh hưởng luồng thanh toán/bán hàng kế tiếp.
- Thay đổi hạng ở cấp hệ thống có thể lan tới logic xếp hạng khách ở màn KH360 (ripple đã ghi trong registry).
