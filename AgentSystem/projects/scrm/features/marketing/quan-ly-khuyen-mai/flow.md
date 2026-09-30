# Luồng — Quản lý khuyến mãi

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Để áp KM khi thanh toán: đơn vị đã **mua tiện ích Khuyến mãi (TI_KhuyenMai)**.
- Đã có dữ liệu nền để chọn đối tượng: hạng/nhóm khách hàng, sản phẩm/kho (để quy đổi đơn vị tính khi so số lượng).
- Chương trình KM còn trong **thời gian áp dụng**, đã được **kích hoạt**, còn **lượt**.

## Luồng cấu hình (admin — tạo chương trình KM)
1. Admin tổng (DV-AD-TONG) mở **Marketing → Khuyến mãi → Thêm mới**.
2. Chọn **loại KM** (`LoaiKhuyenMai`: KM sản phẩm — mặc định, hoặc KM đơn hàng).
3. Nhập thời gian, khung giờ vàng, ngưỡng số lượng/giá trị (Tu/Den), tổng số lượt áp dụng, số lần mỗi khách, đối tượng (hạng/nhóm KH).
4. Lưu → `Create(payload)` validate điều kiện → insert → **xóa cache KM (Redis)** của đơn vị.
5. Sửa (`Update`) / Xóa (`Delete`) tương tự, đều xóa cache KM.

## Luồng dùng (xem danh sách + áp KM khi thanh toán)

**Xem danh sách:**
1. Mở grid Khuyến mãi → `TimTheoDieuKien`.
2. Nếu người dùng **không phải admin** → hệ thống ép `IdChiNhanh = CurrentUser.IdChiNhanh` (chỉ thấy KM chi nhánh mình).

**Áp KM khi thanh toán (luồng xuyên module):**
1. Khi thanh toán đơn, Order/OrderProcess gọi `PromotionUpdateWithBill/ThanhToanDonHang`.
2. Hệ thống check tiện ích đơn vị (TI_KhuyenMai/TI_Voucher/TI_HoaHongGioiThieu) — không có thì bỏ qua nhánh tương ứng.
3. Trong **1 transaction (có retry)**: ghi lịch sử áp (`KhuyenMaiKhachHangLichSu`), đánh dấu KM của khách đã dùng (`IsSuDung=true`, gán IdDonHang/NgaySuDung), **tăng `SoLuongDaDung`** (theo khách) và **`SoLuotDaApDung`** (toàn chương trình).
4. Sau commit: xóa cache KM; ghi `LogTuongTacKhachHang` (KieuLoai = TangKhuyenMai).

## Case biên
- **Non-admin xem danh sách** → bị ép chỉ thấy KM chi nhánh mình.
- **Bật "giới hạn số lượt" nhưng để trống số lượt** → kiểm tra global limit dùng `(SoLuotDaApDung ?? 0) >= (SoLuotApDung ?? 0)` → `0 >= 0` = **hết lượt ngay** → KM không áp. (Known-issue — phải nhập số lượt khi bật giới hạn.)
- **Số lần mỗi khách = null** → coi như đã quá số lần (chặn áp).
- **Khách vãng lai (IdKhachHang rỗng)** → các gate hạng/nhóm KH coi như fail; chỉ KM/voucher không giới hạn mới áp.
- **Hết tồn / tạm ngừng / ngoài khung giờ vàng** → batch check KM sản phẩm trả `IsXoaKmSanPham + LyDoXoa` → loại sản phẩm khỏi KM.
- **Hủy/rollback đơn** → hoàn lượt: `SoLuotDaApDung -= 1` (guard `> 0`), set `IsSuDung=false`, xóa lịch sử. Nếu rollback không chạy → lượt bị "khóa"; nếu rollback chạy 2 lần (không idempotent ở vài nhánh) → có thể trừ âm.
- **Sửa/Xóa KM mà không xóa cache** → cache Redis lệch, áp sai (hệ thống đã xóa cache ở mỗi Create/Update/Delete).

## Ví dụ cụ thể có số
Shop C tạo KM sản phẩm "Mua 3 tặng 1 sữa rửa mặt", thời gian 1–30/6, tổng số lượt = 100, mỗi khách 1 lần.
- Khách An mua 3 sản phẩm trong khung thời gian → đủ điều kiện → áp KM, `SoLuotDaApDung` từ 0 → **1**, KM của An `SoLuongDaDung` = 1.
- Khách An quay lại mua tiếp 3 cái nữa → vì "mỗi khách 1 lần" và An đã dùng → **không áp** lần 2.
- Sau 100 lượt → chương trình hết lượt, khách tiếp theo không được áp dù còn trong thời gian.
- Khách An hủy đơn đầu → hệ thống hoàn: `SoLuotDaApDung` 1 → **0**, KM của An `IsSuDung=false` → An có thể được áp lại.
- Nếu admin tạo KM bật "giới hạn lượt" nhưng **quên nhập số lượt** → KM không bao giờ áp được (0 ≥ 0).

## Hậu điều kiện
- Tạo/Sửa/Xóa KM → ghi DB + **xóa cache KM Redis** của đơn vị.
- Áp KM khi thanh toán → tăng `SoLuotDaApDung` (chương trình) + `SoLuongDaDung` (khách), ghi `KhuyenMaiKhachHangLichSu` + `LogTuongTacKhachHang`.
- Hủy đơn → hoàn lượt, reset cờ sử dụng, xóa lịch sử áp (rollback-safe trong transaction).
