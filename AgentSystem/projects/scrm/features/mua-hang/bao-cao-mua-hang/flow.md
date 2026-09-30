# Luồng — Báo cáo mua hàng

> ⚠️ Chưa có màn "Báo cáo mua hàng" tổng hợp riêng. Luồng dưới đây mô tả **báo cáo đặt cọc** (báo cáo có gate rõ trong mảng Mua hàng) và trỏ sang báo cáo tồn kho đơn vị. Phạm vi báo cáo mua hàng hợp nhất: **cần product xác nhận**.

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId).
- Báo cáo đặt cọc: role `DV-AD-TONG, DV-QL-KHO-TONG`.
- Đã có dữ liệu đặt cọc / tồn kho để báo cáo.

## Luồng dùng (Báo cáo đặt cọc)
1. Mở **Đặt cọc → Báo cáo đặt cọc**.
2. Chọn bộ lọc: chi nhánh nhận, sản phẩm, đợt trả cọc, khoảng ngày; tùy chọn gộp theo chi nhánh.
3. Hệ thống gọi **gating `CheckGioiHanTruyCap`** (giới hạn lượt truy cập); nếu chưa đến lượt → trả "Chưa đến lượt truy cập" (statusCode 250).
4. Nếu được phép → gọi stored proc `PRC_SCRM_Deposit_Report` (chi tiết, phân trang) + `PRC_SCRM_Deposit_ReportTotal` (dòng tổng) → hiển thị lưới + dòng tổng. Ticket truy cập được nhả trong finally.
5. Bấm **Xuất Excel** → gộp chi tiết + tổng → file `ReportDoanhSoTongQuan.xlsx`.

## Luồng cấu hình (admin)
- Cấu hình số lượng user đọc đồng thời (SO_USER_READ_QUAN_LY_DAT_COC / SO_USER_READ_REPORT_DAT_COC...) quyết định ngưỡng gating.

## Các trường hợp / case biên
- **Vượt giới hạn truy cập** → statusCode 250 "Chưa đến lượt truy cập"; thử lại sau.
- **Chuẩn hóa ngày**: TuNgay→00:00:00, DenNgay→23:59:998.
- **Lọc kho**: MaKho (từ cấu hình) giao với chi nhánh nhận nếu cả hai cùng có.
- **Service xuất bận** → HTTP 503; lỗi khác → 500.
- **Lỗi báo cáo bung exception** thay vì trả lỗi êm do `throw;` sau catch (xem dev).
- **Báo cáo mua hàng tổng hợp** (đơn đặt hàng PO) → chưa xác định luồng; cần product xác nhận.

## Ví dụ cụ thể có số
- Quản lý kho lọc Báo cáo đặt cọc: chi nhánh Kho Hà Nội, sản phẩm "Tủ lạnh Y", khoảng ngày 01/06–30/06, gộp theo chi nhánh.
- Kết quả: 30 phiếu cọc; đã thanh toán 28, chưa thanh toán 2, đã giao 10, hoàn cọc 1. Dòng tổng cộng các số này.
- Bấm Xuất Excel → file `ReportDoanhSoTongQuan.xlsx` (lưu ý đối chiếu cột vì có ghi nhận lệch ánh xạ cột — xem dev).
- Nếu 6 người cùng mở báo cáo mà cấu hình giới hạn 5 → người thứ 6 nhận "Chưa đến lượt truy cập".

## Hậu điều kiện
- Báo cáo thuần đọc — không thay đổi dữ liệu nghiệp vụ; chỉ truy vấn SP và nhả ticket truy cập.
