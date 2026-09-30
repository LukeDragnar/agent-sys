# Luồng — Lịch sử hoạt động

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị.
- Có quyền truy cập màn lịch sử tương ứng (mỗi action có SBAuthorize key nội bộ).
- Đã có dữ liệu log phát sinh: các thao tác Thêm/Sửa/Xóa/Lọc/Xuất Excel ở những màn nghiệp vụ khác mới sinh bản ghi để xem ở đây.

## Luồng dùng (Lịch sử hoạt động / tác động)
1. Người dùng mở **Cài đặt → Lịch sử hoạt động** (`/caidat/lich-su-tac-dong`).
2. Hệ thống tải dropdown **Bảng** (table) và **Thao tác** (activity) để lọc.
3. Người dùng chọn Bảng, Thao tác (ADD / UPDATE / DELETE / SELECT=Lọc tìm kiếm / ExportExcel), khoảng ngày, từ khóa → bấm Tìm.
4. Hệ thống gọi `TimTheoDieuKien` (lọc theo điều kiện + CurrentUser) → trả danh sách phân trang.
5. Mỗi dòng hiển thị: loại thao tác (màu: thêm xanh lá, cập nhật xanh dương, xóa đỏ), đối tượng + id, người tạo, thời điểm; với Cập nhật còn liệt kê từng thuộc tính (tên, giá trị cũ, giá trị mới); với Lọc tìm kiếm hiển thị các điều kiện bộ lọc đã dùng. Nếu > 3 dòng chi tiết → nút "Xem thêm/Thu gọn".
6. Nếu là **SuperUser**: cột Hành động hiện nút **Xóa** từng dòng (`Xoa/{id}`).

## Luồng dùng (Lịch sử bộ lọc / tìm kiếm)
1. Mở `/caidat/lich-su-bo-loc` (bộ lọc) hoặc `/caidat/lich-su-tim-kiem` (tìm kiếm).
2. Lọc theo điều kiện → xem danh sách.
3. Bấm **Xuất Excel** → tải file (bộ lọc: `ExportExcelLichSuBoLoc`; tìm kiếm: `ExportExcelLichSuTimKiem`).

## Luồng dùng (Lịch sử xem thông tin)
1. Mở `/caidat/lich-su-xem-thong-tin`.
2. Lọc → xem danh sách + thống kê top (GetTopCount), drilldown chi tiết theo contact/user.
3. Bấm **Xuất Excel** (`ExportExcelLichSuXemThongTin`).

## Case biên
- **Không có dữ liệu log khớp điều kiện** → bảng rỗng.
- **Người dùng không phải SuperUser** → không thấy nút Xóa dòng lịch sử tác động (cột Hành động ẩn).
- **Xuất Excel khi rs.Success=false** → BadRequest (FE báo lỗi, không tải file).
- **Trùng tên file Excel** → cả 3 màn (bộ lọc / tìm kiếm / xem thông tin) cùng xuất file tên `LichSuBoLoc.xlsx` → người dùng dễ ghi đè/nhầm khi tải nhiều file (lỗi đặt tên — xem dev.md).
- **Lịch sử là chỉ-đọc** → không có thao tác Sửa; chỉ Xem (và SuperUser Xóa dòng tác động).

## Ví dụ cụ thể có số
Admin nghi một khách bị đổi số điện thoại sai.
1. Vào Lịch sử hoạt động → Bảng = "Khách hàng", Thao tác = "Cập nhật", ngày = hôm qua → Tìm.
2. Ra 4 dòng cập nhật. Dòng lúc 14:30 do nhân viên "Lan" thực hiện hiển thị: `SoDienThoai: 0901234567 → 0907654321`.
3. Admin đối chiếu → xác định Lan đã sửa nhầm; yêu cầu Lan sửa lại.
4. (Nếu admin là SuperUser và muốn dọn 1 bản ghi rác) bấm Xóa dòng đó.
5. Sang Lịch sử xem thông tin: lọc khách này → thấy 3 nhân viên đã mở xem hồ sơ trong tuần, top 1 là "Hùng" với 12 lần.

## Hậu điều kiện
- Mọi thao tác xem/lọc/xuất ở các màn lịch sử **không thay đổi dữ liệu nghiệp vụ**.
- Xóa 1 dòng lịch sử tác động (SuperUser) → xóa đúng bản ghi đó khỏi nhật ký.
- Xuất Excel → tạo file để tải về, không sửa dữ liệu.
