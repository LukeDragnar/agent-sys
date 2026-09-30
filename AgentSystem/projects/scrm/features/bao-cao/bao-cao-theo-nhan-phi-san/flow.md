# Luồng — Báo cáo theo nhãn / phí sàn

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- **Báo cáo theo nhãn:** có khóa quyền `BaoCaoCuaHangController_ReportNhan`.
- **Báo cáo phí sàn:** có vai trò trong danh sách (DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, HT-CSKH, DV-QLDV).
- Đã có dữ liệu: nhãn được gắn cho contact/khách (báo cáo nhãn); đơn logistic sàn đã đồng bộ kèm phí (báo cáo phí sàn).
- Cấu hình giới hạn khoảng ngày (nếu bật) chặn khoảng quá rộng.

## Luồng dùng (user)
**A. Báo cáo theo nhãn (cửa hàng):**
1. Mở **Báo cáo → Báo cáo cửa hàng → Báo cáo theo nhãn**.
2. Chọn chi nhánh, Từ/Đến ngày.
3. Gate khoảng ngày; user thường + chưa chọn chi nhánh → ép về chi nhánh user.
4. Chạy báo cáo (stored proc nhãn offline), trả bảng gom theo nhãn.

**B. Báo cáo phí sàn TMĐT:**
1. Mở **Báo cáo → Phí sàn TMĐT**.
2. Lọc chi nhánh, sàn (EcommerceId), gian hàng, trạng thái giao hàng, Từ/Đến ngày, từ khóa, paging.
3. Gate khoảng ngày; **xin ticket lock** (chống truy cập đồng thời).
4. Nếu được cấp lượt → chạy stored proc, trả lưới phí sàn + tổng số bản ghi; `finally` luôn nhả ticket.
5. Bấm 1 dòng → chi tiết phí của đơn (`..._ChiTiet`) hoặc thông tin đơn SCRM gốc (`..._ChiTiet_Sandbox`).

## Luồng cấu hình (admin)
- Phân quyền: gán khóa quyền tính năng / vai trò cho người được xem.
- Bật/đổi giới hạn khoảng ngày tra báo cáo.
- Đảm bảo kết nối sàn (Shopee/TikTok/Lazada) đồng bộ đơn + phí về (cho báo cáo phí sàn).

## Các trường hợp / case biên
- **Phí sàn — quá lượt truy cập đồng thời** → trả status 250 "Chưa đến lượt truy cập"; ticket được nhả ở `finally` để người sau vào.
- **Khoảng ngày vượt giới hạn** → chặn "Chỉ được xem dữ liệu trong khoảng N ngày" (cả 2 báo cáo).
- **Phí sàn — chi tiết không truyền DonHangLogisticId** → lỗi "Id chi tiết là bắt buộc".
- **Báo cáo nhãn — chưa gắn nhãn nào** → bảng không có dòng theo nhãn.
- **Phí sàn — đơn chưa đồng bộ/chưa có phí** → không nằm trong báo cáo.
- **User thường (báo cáo nhãn) không chọn chi nhánh** → ép về chi nhánh user.

## Ví dụ cụ thể có số
**Báo cáo theo nhãn:** Shop F gắn 3 nhãn cho khách: "Khách VIP" (120 đơn, 90tr), "Khách mới" (300 đơn, 60tr), "Khách quay lại" (80 đơn, 40tr).
- Quản lý lọc "01/06–30/06" → bảng 3 dòng nhãn với số đơn/doanh thu tương ứng; khách chưa gắn nhãn không hiện.

**Báo cáo phí sàn:** Shop F bán qua Shopee + TikTok, tháng 6 có 400 đơn sàn, tổng phí sàn 25tr.
- Kế toán lọc "Shopee, 01/06–30/06" → lưới hiện các đơn Shopee + tổng phí (ví dụ 250 đơn / 16tr); bấm 1 đơn → chi tiết phí hoa hồng/dịch vụ/thanh toán của đơn đó + thông tin đơn SCRM gốc.
- 5 kế toán cùng mở báo cáo phí sàn một lúc, vượt số lượt cho phép → người thứ 5 nhận "Chưa đến lượt truy cập"; chờ rồi mở lại.

## Hậu điều kiện
- Truy vấn **không thay đổi dữ liệu** (read-only); đọc qua stored proc, không bắn message/event.
- Báo cáo phí sàn nhả ticket lock sau mỗi lần chạy (kể cả khi lỗi).
