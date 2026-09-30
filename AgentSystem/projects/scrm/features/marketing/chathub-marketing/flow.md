# Luồng — ChatHub Marketing (Phân quyền Fanpage)

## Tiền điều kiện
- Đơn vị đã **bật tiện ích ChatHub (TI_ChatHub)** và đã **kết nối fanpage Facebook** (có page + token).
- Người dùng đã đăng nhập, thuộc đơn vị/chi nhánh.
- Đã thiết lập **cơ cấu tổ chức** (phòng ban/nhóm) để phân quyền lọc fanpage theo cấp.
- Để gán quyền: caller phải là admin / admin chi nhánh.

## Luồng dùng (admin gán quyền)
1. Mở **ChatHub → Phân quyền Fanpage**.
2. Chọn fanpage → **Thêm mới** (`ThemMoi` → `PhanQuyenTuongTacFanpage`): hệ thống **chặn trùng** (user đã được phân quyền page đó) → check vai admin/admin chi nhánh → set DonVi/ChiNhanh + tự suy ra phòng ban/nhóm của user → insert.
3. **Cập nhật quyền** (`CapNhatThongTin`): validate → check tồn tại → `UpdateAsync` chỉ mark dirty **9 cờ quyền** (IsCreatePost, IsEditPost, IsDeletePost, IsReplyComment, IsEditComment, IsDeleteComment, IsInbox, IsEditInbox, IsDeleteInbox).

## Luồng dùng (nhân viên)
1. **Xem page của tôi** (`LayTatCaFanpageCuaUser`) → join User→FBUserFanpage→FBFanpage theo IdUser + IdDonVi.
2. **Rời page** (`Xoa/{id}` → `XoaFanPage`): nếu là admin/super/admin-CN → bị chặn (báo "liên hệ kỹ thuật"); nhân viên thường → xóa các FBUserFanpage của chính mình cho page đó (id ở đây là **FanPageId (long)**).

## Luồng lọc fanpage (rẽ theo vai)
`TimTheoDieuKienAsync` rẽ nhánh theo vai (thứ tự ưu tiên), gọi stored proc tương ứng:
1. Admin tổng / admin CN / admin nhiều CN / DV-TRUC-PAGE / SuperUser / CSKH → `..._FilterFanpage_Admin` (toàn bộ page đơn vị).
2. QL-SALE hoặc QL-MKT → `..._FilterFanpage_Manager` (kèm phòng/nhóm).
3. SALE-LEAD (không QL-SALE) / MKT-LEAD (không QL-MKT) → `..._FilterFanpage_Leader` (kèm nhóm).
4. SALE / MKT thuần → `..._FilterFanpage_User` (chỉ page được gán).
5. Không khớp vai nào → "bạn không có quyền xem".

## Case biên
- **Gán trùng:** user đã có quyền trên page đó → `PhanQuyenTuongTacFanpage` chặn, không tạo trùng.
- **Vai không phải admin gán quyền:** dù `SBAuthorize` cho nhiều vai, service chỉ cho admin/admin-CN → vai thường bị từ chối ở tầng service.
- **Admin rời/xóa page:** nhánh `AdminXoaFanpage` đã bị comment → admin bị chặn, báo "liên hệ kỹ thuật".
- **fanpageId không phải Guid:** `LocUserManagerPage` dùng `Guid.Parse(fanpageId)` không try-parse → **500** nếu truyền sai định dạng.
- **Update field ngoài 9 cờ:** `UpdateAsync` chỉ lưu 9 cờ quyền → đổi IdUser/IdFBFanpage không có tác dụng.
- **Xóa bản ghi phân quyền (`XoaUF`):** `[SBAuthorize]` không key + không verify chủ sở hữu/đơn vị → ai có TI_ChatHub đều xóa được bản ghi bất kỳ (lỗ hổng — xem dev).
- **Xem chi tiết (`TimTheoId`):** không lọc đơn vị/chi nhánh → rò rỉ ngang tenant (lỗ hổng — xem dev).
- **Nhân viên chưa xếp phòng/nhóm:** lọc fanpage Manager/Leader dựa cơ cấu tổ chức → thiếu phòng/nhóm thì page lọc ra có thể thiếu.

## Ví dụ cụ thể có số
Đơn vị X kết nối **3 fanpage**, có 1 phòng MKT gồm 1 quản lý + 1 lead + 4 nhân viên.
- Admin gán: Page A cho NV1 (đủ 9 quyền), Page B cho NV2 (chỉ inbox), Page C cho cả phòng.
- NV2 mở ChatHub → lọc fanpage trả nhánh User → thấy **2 page** (Page B + Page C), trên Page B chỉ làm được inbox.
- Lead mở → nhánh Leader (theo nhóm) → thấy các page của nhóm mình.
- Quản lý MKT mở → nhánh Manager → thấy page theo phòng/nhóm.
- Admin mở → nhánh Admin → thấy cả **3 page**.
- NV1 nghỉ chăm Page A → "rời page" → các bản ghi FBUserFanpage của NV1 trên Page A bị xóa, NV1 không còn thấy Page A.

## Hậu điều kiện
- Gán/cập nhật/gỡ quyền → ghi/xóa bản ghi `FBUserFanpage` (9 cờ quyền + IdUser/IdFBFanpage…).
- `AdminXoaFanpage` (nếu bật) clear cache fanpage theo FanPageId + DonVi — hiện đã vô hiệu.
- Lọc fanpage chạy hoàn toàn qua stored proc (4 PRC theo vai), không EF.
