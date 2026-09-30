# Luồng — Người giới thiệu

## Tiền điều kiện
- Người dùng đã đăng nhập với vai trò phù hợp:
  - Nguồn giới thiệu (shop): admin đơn vị (DV-AD-TONG/CN/MCN); đơn vị có tiện ích Hoa hồng giới thiệu.
  - Mã giới thiệu (hệ thống): admin/kế toán/CSKH hệ thống (HT-AD/HT-KT/HT-CSKH) hoặc người giới thiệu (HT-NGT).
- Để phát sinh hoa hồng từ nguồn: nguồn có **cấu hình hoa hồng giới thiệu** và đơn được gắn nguồn.

## Luồng cấu hình (admin)

**Nguồn giới thiệu:**
1. Mở **Marketing → Nguồn giới thiệu** → grid `TimTheoDieuKien` (phân trang theo điều kiện + CurrentUser).
2. Thêm (`ThemMoi`/`Create`) / Sửa (`CapNhat`/`Update`) / Xóa (`Xoa`/`Delete`); tìm nhanh (`SearchByKeyword`); kiểm tra hợp lệ (`CheckById/{id}` trước khi gắn vào đơn).

**Mã giới thiệu:**
1. Admin mở **Hệ thống → Mã giới thiệu** → lọc Admin (`TimTheoDieuKienAdmin`) hoặc NGT lọc của mình (`TimTheoDieuKienUser`).
2. Thêm mã (`ThemMoi`/`Create`) — chỉ Admin/KT/CSKH (NGT không tự tạo).
3. Tìm theo đơn vị/hợp đồng (4 biến thể User vs Admin); bật/tắt sử dụng (`ChangeStatusOfUse`).
4. Duyệt thanh toán hoa hồng: `DuyetThanhToanHoaHong/{id}` (1 hợp đồng) hoặc `DuyetThanhToanHoaHongMulti` (nhiều) — chỉ HT-AD/HT-KT.

## Luồng dùng

**Phát sinh hoa hồng từ nguồn giới thiệu (xuyên module):**
1. Đơn hàng được gắn `IdNguonGioiThieu` khi tạo/xử lý.
2. Order/OrderProcess gọi `TimTheoId_Private/{id}` hoặc `GetByIds` lấy thông tin nguồn.
3. Khi thanh toán → nếu nguồn có cấu hình hoa hồng → `PromotionUpdateWithBillService` sinh `HoaHongGioiThieu`.

**Khách nhập mã giới thiệu khi đăng ký (public):**
1. Kênh đăng ký gọi `TimTheoMaGioiThieu` (`[AllowAnonymous]`) kiểm tra mã hợp lệ.
2. Mã hợp lệ + đang bật → gắn người giới thiệu cho đơn vị/hợp đồng mới.

## Case biên
- **Search nhanh nguồn (`SearchByKeyword`)** để `[SBAuthorize]` rỗng → mọi user login tìm được (rò danh sách nguồn — known-issue).
- **`Xoa` nguồn dùng HTTP GET** → không idempotent-safe.
- **Kiểm tra mã `TimTheoMaGioiThieu` `[AllowAnonymous]`** → `CurrentUser` có thể null → service phải null-safe; mã sai/tắt → trả không hợp lệ.
- **Duyệt hoa hồng** chỉ HT-AD/HT-KT — vai trò khác bị chặn (đúng ý đồ siết tiền).
- **Đơn không gắn nguồn / nguồn không cấu hình hoa hồng** → không phát sinh hoa hồng dù đã bán.
- **Mã tắt sử dụng** → không nhận giới thiệu mới.

## Ví dụ cụ thể có số
**Nguồn giới thiệu (shop E):** khai báo nguồn "Chị Hoa - CTV", cấu hình hoa hồng 5% giá trị đơn.
- Đơn khách 2.000.000đ gắn nguồn "Chị Hoa" → khi thanh toán phát sinh hoa hồng `2.000.000 × 5% = 100.000đ` cho nguồn này.
- Đơn 1.500.000đ **không gắn nguồn** → không phát sinh hoa hồng.

**Mã giới thiệu (hệ thống):** đại lý NGT-001 có mã `GT001`.
- 3 shop mới đăng ký nhập mã `GT001` → 3 hợp đồng gắn NGT-001.
- Kế toán hệ thống duyệt hoa hồng hàng loạt cho cả 3 hợp đồng → trạng thái thanh toán hoa hồng cập nhật theo từng hợp đồng.
- Nếu admin tắt `GT001` → shop thứ 4 nhập mã sẽ báo không hợp lệ.

## Hậu điều kiện
- CRUD nguồn/mã → ghi DB.
- Đơn gắn nguồn + có cấu hình → sinh `HoaHongGioiThieu` khi thanh toán.
- Duyệt hoa hồng → cập nhật trạng thái thanh toán theo hợp đồng (ripple sang nghiệp vụ hợp đồng/kế toán hoa hồng).
- Kiểm tra mã khi đăng ký → gắn người giới thiệu cho đơn vị/hợp đồng mới.
