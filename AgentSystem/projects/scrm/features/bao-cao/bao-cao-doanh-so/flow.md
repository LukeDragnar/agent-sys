# Báo cáo doanh số — Luồng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Có quyền xem báo cáo tương ứng (mỗi báo cáo có key quyền riêng — đa số public ở mức controller).
- Đơn vị đã có dữ liệu đơn hàng trong khoảng ngày cần xem.
- Nếu đơn vị bật cấu hình giới hạn khoảng ngày báo cáo thì khoảng Từ–Đến phải nằm trong giới hạn.

## Luồng dùng (user)
1. Người dùng mở module **Báo cáo** → chọn báo cáo doanh số (ví dụ: Doanh thu theo ngày, Theo chi nhánh, Theo sản phẩm, Doanh số tổng quan, Theo giao vận, Tổng hợp bán hàng).
2. Chọn **chi nhánh** + **khoảng ngày Từ–Đến**; tùy báo cáo có thêm lọc sản phẩm/nhân viên/nguồn/cơ cấu tổ chức.
3. Hệ thống kiểm tra giới hạn khoảng ngày; nếu vượt → trả lỗi và không truy vấn.
4. Hệ thống ép phạm vi chi nhánh nếu user không phải admin/super/test/CSKH và chưa chọn chi nhánh → mặc định chi nhánh của user.
5. Hệ thống chạy stored proc báo cáo tương ứng (read-only), một số báo cáo gọi phụ để bổ sung tên sản phẩm.
6. Trả về bảng (lưới) + tổng số bản ghi; người dùng phân trang, sắp xếp.
7. Bấm **Xuất Excel** (nếu có) → hệ thống build lại dữ liệu và xuất file `.xlsx`.

## Luồng cấu hình (admin)
- Admin cấu hình **giới hạn khoảng ngày được phép xem** trong cấu hình báo cáo (CheckCauHinhKhoangThoiGianChoPhepTimKiem). Đặt = -1 nghĩa là không giới hạn.
- Cấu hình **cho phép xem toàn đơn vị / phạm vi chi nhánh** ảnh hưởng tới việc ép chi nhánh.

## Các trường hợp / case biên
- **Vượt giới hạn khoảng ngày** → trả lỗi "Chỉ được xem dữ liệu trong khoảng N ngày", không chạy báo cáo.
- **User thường không chọn chi nhánh** → bị ép về chi nhánh của user (IdChiNhanh rỗng → gán chi nhánh user).
- **Không có dữ liệu trong khoảng ngày** → bảng rỗng, tổng = 0 (không lỗi).
- **Xuất Excel khi service bận** → trả mã 503 (Service_Unavailable); FE báo tạm thời chưa xuất được.
- **Khoảng ngày quá rộng / nhiều chi nhánh** → báo cáo chạy lâu (SP nặng, mở connection riêng cho báo cáo nặng như giao vận, phong thần bảng).
- **Báo cáo doanh số theo ngày (online)** dùng quyền theo auth chung (không có key riêng) — vẫn yêu cầu đăng nhập hợp lệ.

## Ví dụ cụ thể có số
Shop B có 3 chi nhánh, tháng 6 bán được 1.250 đơn, tổng doanh thu 480.000.000đ.
- Anh Nam (quản lý đơn vị) mở **Doanh thu theo ngày**, chọn 01–30/06, tất cả chi nhánh → bảng 30 dòng (mỗi ngày 1 dòng), tổng 480.000.000đ.
- Chị Hoa (thu ngân CN1) mở cùng báo cáo, không chọn chi nhánh → hệ thống ép về CN1, chỉ thấy doanh thu CN1 (ví dụ 180.000.000đ).
- Anh Nam mở **Doanh thu theo sản phẩm**, lọc còn 1 ngành hàng → bảng hiện từng SP với số lượng + doanh thu, kèm tên SP được map từ kho.
- Anh Nam chọn khoảng 01/01–30/06 (180 ngày) nhưng đơn vị giới hạn 90 ngày → báo lỗi "Chỉ được xem dữ liệu trong khoảng 90 ngày".

## Hậu điều kiện
- Truy vấn báo cáo **không thay đổi dữ liệu** (read-only), không bắn message/event.
- Khi user là super/CSKH hoặc đơn vị bật "show query", hệ thống ghi log câu truy vấn chậm để theo dõi hiệu năng (không ảnh hưởng dữ liệu nghiệp vụ).
