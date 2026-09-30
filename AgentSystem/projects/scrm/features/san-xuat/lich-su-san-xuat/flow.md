# Luồng — Lịch sử sản xuất

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Đã có dữ liệu sản xuất để tra (đơn đặt hàng / PR / WO đã tạo ở Quản lý sản xuất).
- Endpoint lịch sử (`TimTheoDieuKienLichSu`) hiện để quyền rỗng → chỉ cần đăng nhập.

## Luồng dùng (3 màn tra cứu)
1. **Lịch sử đơn đặt hàng** (`/sanxuat/lich-su-donhang`): tìm các đơn đặt hàng sản xuất theo từ khóa/khoảng thời gian → `SaleOrderDonHang/TimTheoDieuKienLichSu`.
2. **Lịch sử sản xuất / Production Order** (`/sanxuat/lich-su-sanxuat`): tìm yêu cầu sản xuất (PR) theo mã PR + khoảng thời gian; cột Trạng thái = Đã tách / Chưa tách (`isTachDon`). Nút **Xóa** chỉ hiện khi `!isTachDon`. Endpoint `SaleOrderProduction/TimTheoDieuKienLichSu`.
3. **Lịch sử lệnh công việc / Work Order** (`/sanxuat/lich-su-congviec`): tìm theo mã lệnh / mã SP / tên SP; lọc theo **Trạng thái** (ChuaBatDau/ChuaXepLich/DaXepLich) + **Trạng thái sản xuất** (ChuaBatDau/DangSanXuat/TamDung/Huy/HoanThanh); cột "Tên bước hiện tại"; nút **Xem chi tiết** mở modal nguyên vật liệu cần; nút **Xóa** (dropdown).

## Luồng cấu hình (admin)
- Không có cấu hình riêng cho các màn lịch sử (chỉ tra cứu).

## Case biên
- **Tìm rỗng / không khớp** → bảng rỗng, phân trang 0.
- **Xóa PR đã tách** → nút Xóa không hiện (chặn từ FE theo `isTachDon`).
- **Xóa WO từ lịch sử** → cascade delete step/product nhưng **KHÔNG hoàn phiếu kho** (giống Quản lý sản xuất) → tồn kho lệch.
- **Lọc theo trạng thái nhưng dữ liệu có TrangThaiSanXuat=null** (khi un-schedule) → cần để ý khi đối chiếu (null khác với 0/ChuaBatDau).

## Ví dụ cụ thể có số
Quản lý cần tra các lệnh đang sản xuất trong tháng 6:
- Vào **Lịch sử lệnh công việc**, chọn khoảng 01/06–30/06, lọc Trạng thái sản xuất = "Đang sản xuất" → ra ví dụ 5 lệnh.
- Bấm **Xem chi tiết** lệnh `WO-2026-0012` → modal hiện 3 nguyên vật liệu cần (Cồn 28 lít, Glycerin 4 lít, Chai rỗng 140 cái).
- Một PR `PR-2026-0030` chưa tách còn sót → vào **Lịch sử sản xuất**, lọc thấy "Chưa tách" → bấm **Xóa** để dọn (dòng đơn liên quan reset TrangThai=0).

## Hậu điều kiện
- Tra cứu **không đổi dữ liệu** (read-only).
- Thao tác Xóa trên màn lịch sử ghi DB (xóa PR/WO) và có ripple như mô tả (PR reset dòng đơn; WO không hoàn phiếu kho).
