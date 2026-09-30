# Luồng — ChatHub Sale (Phân quyền Fanpage)

## Tiền điều kiện
- Đơn vị có tiện ích **ChatHub** (`MaTienIchSCRM.TI_ChatHub`) — gate ở cấp controller.
- Đã kết nối ít nhất 1 **fanpage** (FBFanpage có FanPageId, token).
- Đã có **user** trong đơn vị để gán.
- Cơ cấu tổ chức (phòng ban / nhóm) khai báo đúng để lọc fanpage theo cấp quản lý.
- Phân quyền (thêm mới): người thao tác là admin hoặc admin chi nhánh.

## Luồng dùng (gán & lọc)
1. Admin mở **Phân quyền Fanpage**; lấy danh sách user (`LayTatCaUser` → `IUserService.GetAllUser`).
2. **Thêm mới phân quyền** (`ThemMoi` → `PhanQuyenTuongTacFanpage`): chặn trùng (cùng IdFBFanpage + IdUser + DonVi) → check role admin/admin chi nhánh → set DonVi/ChiNhanh + tự suy ra PhongBan/Nhom của user → insert `FBUserFanpage`.
3. **Cập nhật quyền** (`CapNhatThongTin` → `UpdateAsync`): mark dirty 9 cờ quyền (IsCreatePost, IsEditPost, IsDeletePost, IsReplyComment, IsEditComment, IsDeleteComment, IsInbox, IsEditInbox, IsDeleteInbox).
4. **Lọc fanpage** (`TimTheoDieuKienAsync`): rẽ nhánh theo role → gọi stored proc tương ứng (Admin/Manager/Leader/User) với keyword + PreferredPageIds + phân trang.

## Luồng lọc fanpage theo cấp (rẽ nhánh role, thứ tự ưu tiên)
1. Admin tổng / admin chi nhánh / admin nhiều CN / DV-TRUC-PAGE / SuperUser / CSKH → `PRC_..._FilterFanpage_Admin` (toàn bộ page đơn vị).
2. QL-SALE hoặc QL-MKT → `..._FilterFanpage_Manager` (kèm `StrIdPhongNV`, `StrIdNhomNV`).
3. SALE-LEAD (không QL-SALE) hoặc MKT-LEAD (không QL-MKT) → `..._FilterFanpage_Leader` (kèm `StrIdNhomNV`).
4. SALE thuần / MKT thuần → `..._FilterFanpage_User` (chỉ page được gán).
5. Không khớp role nào → "bạn không có quyền xem".
- Tham số chung: `@IdDonVi, @IdChiNhanh, @KeyWord, @PreferredPageIds, @PageIndex, @PageSize` (+ `@IdUser` cho manager/leader/user).

## Các trường hợp / case biên
- **Gán trùng:** user đã được phân quyền page đó → chặn trùng (không tạo bản ghi mới).
- **Người gán không phải admin/admin chi nhánh:** `PhanQuyenTuongTacFanpage` từ chối.
- **Admin xóa fanpage:** nhánh `AdminXoaFanpage` đã bị comment → trả message "Liên hệ kỹ thuật..." (admin không xóa được qua UI).
- **Nhân viên rời page:** `XoaFanPage` xóa các bản ghi FBUserFanpage của chính user cho page đó (`IdUser = CurrentUser.Id`). Lưu ý `id` ở endpoint `Xoa/{id}` là **FanPageId (long)**, không phải Guid record.
- **Lọc page sai định dạng id:** `LocUserManagerPage` dùng `Guid.Parse(fanpageId)` không try-parse → 500 nếu fanpageId không phải Guid.
- **Role không khớp khi lọc:** trả "bạn không có quyền xem".
- **Cập nhật quyền:** chỉ lưu 9 cờ; đổi field khác (IdUser, IdFBFanpage) không có tác dụng.

## Ví dụ cụ thể có số
Đơn vị D có tiện ích ChatHub, đã kết nối 3 fanpage (P1, P2, P3); phòng Sale có 5 nhân viên.
- Admin gán nhân viên An vào P1 với quyền: tạo bài ✅, xóa bài ❌, đọc inbox ✅, trả comment ✅. Lưu → An được vào P1 với đúng 4 cờ này.
- Admin gán An vào P1 lần nữa → chặn trùng (đã có bản ghi An + P1).
- QL-Sale Bình lọc fanpage → hệ thống dùng `_FilterFanpage_Manager` với phòng/nhóm của Bình → thấy P1, P2 (page thuộc phòng Bình quản lý), không thấy P3.
- Nhân viên An (sale thuần) lọc fanpage → `_FilterFanpage_User` → chỉ thấy P1 (page được gán).
- Admin muốn xóa P3 trong màn phân quyền → hệ thống báo "Liên hệ kỹ thuật" (nhánh admin xóa bị vô hiệu hóa).

## Hậu điều kiện
- Thêm phân quyền → bản ghi `FBUserFanpage` mới (user gắn page + 9 cờ quyền + phòng ban/nhóm).
- Cập nhật quyền → cập nhật 9 cờ trên bản ghi.
- Nhân viên rời page → xóa các bản ghi FBUserFanpage của user đó cho page.
- (Admin xóa fanpage — nếu bật) → clear cache fanpage theo FanPageId + DonVi.
