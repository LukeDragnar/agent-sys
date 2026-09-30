# Luồng — Quản lý quyền & tài khoản (thiết bị & đăng nhập)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId).
- Quản lý/duyệt thiết bị: quyền `UserDevice_*` (DV-AD-TONG/CN/MCN).
- Cấp slot đăng nhập đa thiết bị: quyền `UserDeviceLoginExtend_*` (HT-AD, HT-KT) + đơn vị đã cấu hình `DonVi.LoginOneDeviceExtend` > 0.
- Tra log đăng nhập: chỉ cần login; lọc đơn vị bất kỳ cần SuperUser/CSKH.

## Luồng dùng (admin đơn vị)
**A. Duyệt / chặn thiết bị:**
1. Mở `device-login/danh-sach` → `TimTheoDieuKien` nạp lưới thiết bị theo đơn vị.
2. Thiết bị `ChoDuyet` → bấm **Duyệt** (`DuyetThietBi`) → trạng thái `DaDuyet`, cho phép đăng nhập.
3. Thiết bị `DaDuyet` → bấm **Hủy duyệt** (`HuyDuyetThietBi`) → thu hồi quyền.
4. Bấm **Xóa** (`Xoa/{id}`) → nếu `IdDonVi == CurrentUser.DonViId` thì xóa, ngược lại báo "Không tìm thấy thiết bị".

**B. Cấp / sửa slot đăng nhập:**
1. Mở `caidat/login-limited` → `TimTheoDieuKien` nạp lưới.
2. **Thêm** (`ThemMoi`): chọn user + nhập `TongDeviceLogin` (≥1) → Redis lock → kiểm tra `SUM(slot khác) + mới ≤ DonVi.LoginOneDeviceExtend` → Insert.
3. **Sửa** (`CapNhatThongTin`): như thêm; đổi user → user cũ về 0 + logout thiết bị thừa; sau Update gọi `LogOutUserTheoSoLuong`.
4. **Xóa** (`Xoa`): Delete + `LogOutUserTheoSoLuong(IdDonVi, UserId, 0)` → đăng xuất hết thiết bị mở rộng.

**C. Tra log đăng nhập:**
1. Mở `caidat/dangnhap-log` → `TimTheoDieuKien` (UserDeviceLog): lọc KeyWord (UserName/IP/TenDonVi), TuNgay/DenNgay → trả paged, sắp xếp NgayTao desc.

## Luồng cấu hình (admin)
- Toàn bộ A/B/C là thao tác cấu hình do admin thực hiện. Phần phân quyền vai trò + đồng bộ danh mục quyền toàn hệ thống nằm ở khu QuanTriArea (super-admin) — ngoài phạm vi cẩm nang khách.

## Case biên
- **Xóa thiết bị chéo đơn vị** → `data.IdDonVi != CurrentUser.DonViId` → "Không tìm thấy thiết bị" (`UserDeviceController.cs:83`).
- **Slot < 1** → lỗi "Vui lòng nhập giá trị > 0".
- **User không tồn tại** → lỗi "Nhân viên không tồn tại".
- **Trùng user trong đơn vị** → lỗi "Trùng lặp nhân viên. mời chọn nhân viên khác".
- **Không lấy được Redis lock** (`UserDeviceLoginExtend:{IdDonVi}:lock`, 30s) → "Đang có người thao tác vui lòng thử lại".
- **Vượt hạn mức** → "Tổng nhân viên được phép thêm không vượt quá {TongDuocPhep}" + gỡ lock.
- **Hạn mức đơn vị null** → coi như 0 → không cấp được slot nào.
- **Log: user thường** → bị ép `DonViId == userLogin.DonViId`, chỉ thấy log đơn vị mình.
- **Log: tham số Sorts của client** → bị bỏ qua (luôn sort NgayTao desc).
- **Log có độ trễ** → ghi async qua background queue sau login.

## Ví dụ cụ thể có số
Đơn vị Y có hạn mức `LoginOneDeviceExtend = 5`. Hiện đã cấp: NV-A 2 slot, NV-B 1 slot (tổng 3).
- Admin cấp NV-C 2 slot → `3 + 2 = 5 ≤ 5` → **OK**. Tổng giờ là 5/5.
- Admin cấp tiếp NV-D 1 slot → `5 + 1 = 6 > 5` → **bị chặn**, báo "Tổng nhân viên được phép thêm không vượt quá 5".
- Admin sửa NV-A từ 2 xuống 1 slot, lúc đó NV-A đang đăng nhập 2 máy → hệ thống logout 1 máy thừa ngay; tổng còn 4/5 → có thể cấp NV-D 1 slot.
- Một điện thoại lạ login tài khoản NV-B (thiết bị mới, `ChoDuyet`) → admin Hủy duyệt → thiết bị bị chặn đăng nhập. Admin tra `dangnhap-log` lọc UserName=NV-B, TuNgay=hôm nay → thấy bản ghi IP + thời gian login bất thường.

## Hậu điều kiện
- Duyệt/hủy/xóa thiết bị → đổi trạng thái thiết bị, ảnh hưởng đăng nhập (không bắn event).
- Cấp/sửa/xóa slot → cập nhật `UserDeviceLoginExtend` + logout thiết bị thừa ngay lập tức.
- Tra log → chỉ đọc, không ghi.
