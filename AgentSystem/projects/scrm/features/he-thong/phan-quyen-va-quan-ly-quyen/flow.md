# Luồng — Phân quyền & quản lý quyền

## Tiền điều kiện
- Người thao tác đã đăng nhập, là **admin hệ thống (HT-AD)** hoặc **super-admin (IsSuperUser)** tùy thao tác. Một số thao tác cho HT-CSKH, HT-KT.
- Phân quyền **mặc định toàn hệ thống** chỉ chạy khi `CurrentUser.IsSuperUser == true`.
- Phân quyền **theo đơn vị** yêu cầu đơn vị hợp lệ (DonViId khác null cho thao tác cập nhật quyền đơn vị theo key).
- Danh sách tiện ích đã có (chức năng được nhóm theo tiện ích) để dựng bảng ma trận.

## Luồng dùng (admin — quản lý Role)
1. Mở **Hệ thống → Phân quyền → Quản lý quyền (Role)**.
2. Hệ thống gọi `Role/TimTheoDieuKien` (hoặc `LayTatCa`) → lưới role sắp theo `ThuTu`.
3. **Thêm** role: nhập tên/thứ tự/cờ "role hệ thống" → hệ thống set người tạo = user hiện tại → Insert.
4. **Sửa** role: kiểm tra tồn tại → set người cập nhật → Update.
5. **Xóa** role: kiểm tra tồn tại → xóa. (KHÔNG kiểm tra role đang gán cho user.)

## Luồng dùng (admin — phân quyền chức năng)
**Cấp đơn vị (admin đơn vị):**
1. Mở màn phân quyền đơn vị → gọi `Permision/LayDanhSachTienIch` (user thường lấy tiện ích theo đơn vị đã mua) + `Permision/LayDanhSachQuyenTheoRoleDonVi` → ma trận quyền role của đơn vị.
2. Tích/bỏ tích → **Lưu** → `Permision/PhanQuyenChucNangDonVi` (`UpdateDonViRolePermision`).

**Cấp hệ thống (super-admin):**
1. Mở màn phân quyền hệ thống (QuanTriArea) → `Permision/LayDanhSachQuyenTheoRole`. Nếu `IsSuperUser != true` → **BadRequest** (chặn).
2. Tích/bỏ tích → **Lưu** → `Permision/PhanQuyenChucNang` (lại check IsSuperUser).
3. **Copy quyền**: `GetInitFromCopy` (init form) + `GetDonViTheoDieuKien` (tìm đơn vị đích) → `DonViQuyenCopy`/`DonViQuyenCopyV2` (V2 chỉ super-admin).
4. **Tự động phân quyền**: `AutoPhanQuyenChucNang` (`AutoSetRolePermision`).
5. **Permission handmade**: `CreateHandmadePermision`/`UpdateHandmadePermision`/`DeleteHandmadePermision/{keycode}`.
6. **Gán role theo key**: `GetByKey?keycode=` → `UpdateByKey` (toàn hệ thống) hoặc `UpdateByKeyDonVi` (yêu cầu DonViId khác null).

## Luồng dùng (admin — quyền custom)
1. Mở **Quản lý quyền custom** → `RolePermisionLog/TimTheoDieuKien` → lưới quyền override.
2. **Xóa**: `RolePermisionLog/Xoa/{id}` → kiểm tra tồn tại → xóa cứng → thu hồi quyền override runtime.

## Case biên
- **Không phải super-admin gọi phân quyền/copy V2 cấp hệ thống** → trả BadRequest, không thực hiện.
- **`UpdateByKeyDonVi` mà DonViId = null** → từ chối (thiếu đơn vị đích).
- **Xóa role còn đang gán cho user** → vẫn xóa (không chặn) → user trở thành "mồ côi" role (known-issue).
- **Xóa quyền custom của bản ghi không tồn tại** → trả NotFound.
- **`RunJobCopyDonVi`** là endpoint `[AllowAnonymous]` → bất kỳ ai gọi cũng bật được cờ job copy đơn vị (rủi ro, xem dev.md).
- **Đổi quyền role nhưng user vẫn thấy như cũ** → cần đăng nhập lại để nạp lại ma trận quyền.

## Ví dụ cụ thể có số
Hệ thống có 12 role chuẩn. Super-admin mở phân quyền mặc định, tích thêm quyền "Xuất Excel khách hàng" cho role DV-KT.
- Sau khi lưu, cả 30 user mang role DV-KT trong mọi đơn vị đều có nút Xuất Excel khách (lần đăng nhập kế tiếp).
- Có đơn vị mới "Shop B" cần quyền giống "Shop A": super-admin chọn nguồn = Shop A, đích = Shop B → Copy quyền V2 → toàn bộ ~200 ô quyền của Shop A áp vào Shop B trong 1 thao tác.
- Admin hệ thống xóa role thừa "Sale-tạm" đang gán cho 3 user → role bị xóa, 3 user đó mất role (không có cảnh báo) → 3 user mất quyền sale cho tới khi được gán lại.

## Hậu điều kiện
- Thay đổi quyền role (hệ thống/đơn vị) → cập nhật bảng `RolePermision` → đổi quyền truy cập của user mang role đó.
- Copy/tự động/handmade → ghi vào `Permision`/`RolePermision` diện rộng.
- Xóa quyền custom → bản ghi `RolePermisionLog` bị xóa cứng → thu hồi override runtime.
- Các thao tác này **không bắn event** sang service khác (ảnh hưởng nội bộ phân quyền).
