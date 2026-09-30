# Luồng — Quản lý gói & tiện ích

## Tiền điều kiện
- CRUD danh mục gói/tiện ích: **HT-AD** (chính), **HT-KT**.
- Chợ tiện ích / mua / quản lý tiện ích đã mua: admin đơn vị (**DV-AD-TONG**, DV-AD-CN/MCN, DV-QLDV), **HT-KT**.
- Duyệt/Hủy duyệt nâng cấp gói: `CurrentUser.IsSuperUser || IsHeThongKeToan() || IsAdminHeThong()`.

## Luồng dùng (admin — gói sử dụng)
1. Mở **Quản trị → Gói sử dụng** (route `api/QuanTriArea/GoiSuDung`).
2. CRUD: `ThemMoiGoiSd` / `CapNhatGoiSd` (qua `InsertAsync/UpdateAsync`) + CRUD nền SBBaseController; `GoiSuDung_TimTheoDieuKien` (HT-AD/HT-KT).
3. **Xóa** `Xoa/{id}`: override `XoaAsync` → `GetByIdAsync` ≠ null mới xóa, không có → `MESS_NotFound`.
4. **Yêu cầu nâng cấp**: `DanhSachNangCap` (DV-AD-TONG/HT-KT) → lưới; `GoiPheDuyetInfo/{id}` xem chi tiết.
5. **Duyệt** `DuyetYeuCauNangCap`: nếu đủ quyền → `DuyetNangCapGoi(request)`; không đủ → "Bạn không có quyền sử dụng chức năng này".
6. **Hủy duyệt** `HuyDuyetNangCap?idNangCap=`: cùng gating quyền trong code.

## Luồng dùng (admin — danh mục tiện ích, route api/QuanTriArea/TienIch)
1. `TienIch/TimTheoDieuKien` (HT-AD/HT-KT), `LayTatCa`, `TimTheoId/{id}`.
2. **Thêm/Sửa** `ThemMoi`/`CapNhatThongTin` (HT-AD).
3. **Xóa** `Xoa/{id}`: check tồn tại + `CheckTienIchDaSuDung(id)` → nếu đang có đơn vị dùng → chặn ("Tiện ích đang có đơn vị sử dụng nên không thể xóa").

## Luồng dùng (đơn vị — chợ tiện ích, route api/TienIch)
1. **Chợ** `GetDanhSachTienIchFilter` (DV-AD-TONG/HT-KT) → danh sách tiện ích bán cho đơn vị.
2. **Mua ngay** `MuaNgay` (DV-AD-TONG/HT-KT) → tạo `HopDongTienIch` cho list `MuaNgayTienIchDto` → kích hoạt tính năng cho đơn vị.
3. **Tiện ích đã mua** `GetDanhSachTienIchDaMua/{IsConHan}`: `IsConHan==1` → chỉ còn hạn; else → tất cả.
4. **Lịch sử giao dịch** `LichSuGiaoDich` (login, qua `IHopDongTienIchService`).
5. **Xóa TI đã đăng ký** `XoaTiDaDangKy/{id}` (DV-AD-TONG, HttpGet) → thu hồi/hủy tiện ích.

## Case biên
- **Duyệt/Hủy nâng cấp gói khi không đủ quyền** → trả lỗi "Bạn không có quyền..." (gating runtime trong code, không chỉ attribute).
- **Xóa gói không tồn tại** → `MESS_NotFound`.
- **Xóa danh mục tiện ích đang được đơn vị dùng** → bị chặn (CheckTienIchDaSuDung = true).
- **GetDanhSachTienIchDaMua IsConHan=1** → chỉ trả tiện ích còn hạn; =0 → tất cả (kể cả hết hạn).
- **XoaTiDaDangKy dùng HttpGet** → thao tác xóa nghiệp vụ qua GET, không idempotent về nghiệp vụ (known-issue).
- **Mua tiện ích nhưng user không có quyền role** → tính năng "có mặt" nhưng user vẫn bị chặn ở phân quyền (hai lớp khóa).
- **Endpoint nội bộ `GetDanhSachTienIchDaMua/{idDonVi}/{IsConHan}`** (`[PrivateSBAuthorize]`) → service khác lấy TI đã mua của đơn vị bất kỳ, không cần token user.

## Ví dụ cụ thể có số
Hệ thống có 3 gói (Basic/Pro/Enterprise) và 15 tiện ích.
- Đơn vị "Spa Hoa Mai" (gói Basic) xin nâng lên Pro → tạo yêu cầu nâng cấp → admin mở **Danh sách nâng cấp**, bấm **Duyệt** → gói áp dụng đổi sang Pro.
- Admin đơn vị Spa Hoa Mai vào **Chợ tiện ích**, chọn 2 tiện ích "ChatHub" + "Tổng đài IP", bấm **Mua ngay** → tạo 1 (hoặc nhiều) `HopDongTienIch` → 2 tiện ích kích hoạt.
- Sau 1 tháng, đơn vị xem **Tiện ích đã mua** với `IsConHan=1` → còn 2 tiện ích còn hạn; bấm **Xóa** "Tổng đài IP" → thu hồi, chỉ còn ChatHub.
- Admin hệ thống muốn xóa danh mục tiện ích "Tổng đài IP" khỏi hệ thống nhưng còn 5 đơn vị đang dùng → hệ thống chặn xóa.

## Hậu điều kiện
- Thêm/Sửa/Xóa gói → cập nhật `GoiSuDung`; duyệt nâng cấp → cập nhật gói áp dụng cho đơn vị (qua `NangCapGoiSuDungService`).
- CRUD danh mục tiện ích → cập nhật `TienIch`.
- Mua ngay → tạo `HopDongTienIch` → bật `[TienIch]` cho đơn vị → các controller liên quan cho truy cập.
- Xóa TI đã đăng ký → thu hồi quyền dùng tính năng của đơn vị.
