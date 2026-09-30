# Luồng — Mã giới thiệu đại lý

## Tiền điều kiện
- Góc Admin: role HT-AD (hoặc HT-KT/HT-CSKH cho phần lớn thao tác). Duyệt hoa hồng chỉ HT-AD/HT-KT.
- Góc NGT: role HT-NGT — chỉ xem mã của chính mình.
- Có người giới thiệu (NGT) trong hệ thống để gán mã.

## Luồng dùng (Admin — quản lý mã)
1. Mở **Quản trị → Mã giới thiệu** (route `quantri/ma-gioi-thieu-admin`).
2. Lọc theo `keyword` / `idUser` (NGT) / `isSuDung` → grid gọi `TimTheoDieuKienAdmin` trả danh sách mã + thống kê (số đơn vị, số hợp đồng, số tiền hoa hồng, số tiền hợp đồng ban đầu/sau CK).
3. **Thêm mới** (`onAddNew`): popup nhập NGT + % chiết khấu + % hoa hồng + số tháng cộng thêm + số lần áp dụng → `ThemMoi` (validate ModelState).
4. **Sửa** (`onEdit`): popup nạp sẵn theo id → lưu.
5. **Đổi trạng thái sử dụng** (`changeStatusOfUse`): toggle ngay trên dòng → `ChangeStatusOfUse/{id}`; nếu lỗi, FE revert lại cờ `isSuDung` của dòng.
6. **Xóa** (`onDelete`): confirm → `Xoa/{id}`.

## Luồng dùng (NGT — xem mã của mình)
1. Mở màn Mã giới thiệu góc NGT (route `quantri/ma-gioi-thieu-ngt`).
2. `TimTheoDieuKienUser` trả mã giới hạn theo `CurrentUser`.
3. Tra đơn vị/hợp đồng đã dùng mã qua `UserTimKiemTheoDonVi` / `UserTimKiemTheoHopDong`.

## Luồng duyệt hoa hồng (Admin/KT)
1. Tra hợp đồng đã dùng mã (`AdminTimKiemTheoHopDong`).
2. Duyệt 1 hợp đồng: `DuyetThanhToanHoaHong/{id}` (id = id hợp đồng).
3. Duyệt nhiều: `DuyetThanhToanHoaHongMulti` (danh sách id hợp đồng).
4. Ripple: cập nhật trạng thái thanh toán hoa hồng theo hợp đồng (nghiệp vụ kế toán hoa hồng).

## Luồng public (khách nhập mã)
- Khi đăng ký/nhập mã: `TimTheoMaGioiThieu` (`[AllowAnonymous]`) kiểm tra mã hợp lệ → áp ưu đãi. `CurrentUser` có thể null → service phải null-safe.

## Các trường hợp / case biên
- **NGT cố tạo mã:** không có endpoint cho HT-NGT ở `ThemMoi` → bị chặn quyền.
- **Toggle trạng thái lỗi:** `ChangeStatusOfUse` fail → FE tự revert cờ `isSuDung` của dòng + báo lỗi (không để UI lệch state thật).
- **Duyệt hoa hồng bởi HT-CSKH:** không được — chỉ HT-AD/HT-KT (siết hơn các action khác).
- **Mã anonymous với CurrentUser null:** endpoint public truyền `CurrentUser` (có thể null) xuống service → service null-safe, nếu không sẽ NRE.
- **TimTheoId dùng POST:** thao tác đọc 1 bản ghi nhưng dùng POST (lệch REST) — không ảnh hưởng nghiệp vụ.
- **Đầu vào rỗng (filter trống):** Admin lọc không nhập gì → trả toàn bộ mã trong phạm vi quyền.

## Ví dụ cụ thể có số
NGT "Anh Phúc" có mã `REF-PHUC` cấu hình: chiết khấu 5%, hoa hồng 10%, số tháng cộng thêm 1, số lần áp dụng cho đơn vị = 3.
- Đơn vị mới "Shop Hoa Tươi" đăng ký hợp đồng 12.000.000đ, nhập mã `REF-PHUC`:
  - Kiểm tra mã (anonymous) hợp lệ → áp chiết khấu 5% → số tiền hợp đồng sau CK = 11.400.000đ, cộng thêm 1 tháng dùng.
  - Hoa hồng cho NGT = 10% × (số tiền theo cấu hình) → ghi nhận số tiền hoa hồng cho `REF-PHUC`.
- Admin mở góc Admin, lọc NGT "Anh Phúc" → thấy mã `REF-PHUC`: số đơn vị 1, số hợp đồng 1, số tiền hoa hồng tương ứng.
- Kế toán (HT-KT) vào hợp đồng đó → **Duyệt thanh toán hoa hồng** → trạng thái thanh toán hoa hồng của hợp đồng = đã duyệt.
- "Anh Phúc" (HT-NGT) đăng nhập góc NGT → thấy mã của mình và "Shop Hoa Tươi" trong danh sách đơn vị đã dùng mã.

## Hậu điều kiện
- Mã giới thiệu được thêm/sửa/xóa/đổi trạng thái trong DB.
- Đơn vị/hợp đồng dùng mã được áp ưu đãi; số liệu thống kê (số đơn vị/hợp đồng/hoa hồng) cập nhật.
- Duyệt hoa hồng cập nhật trạng thái thanh toán hoa hồng theo hợp đồng.
