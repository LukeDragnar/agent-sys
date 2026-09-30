# Luồng — Công nợ (Bù trừ công nợ + Báo cáo công nợ)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Bù trừ: đối tượng cần có công nợ phải thu (bên Khách hàng) và/hoặc phải trả (bên Kho/NCC); đã chọn tài khoản phải thu/phải trả + ngày bù trừ.
- Báo cáo công nợ: người dùng phải có quyền xem báo cáo đơn hàng (gate cấu hình bên Đơn hàng). Danh sách phiếu bù trừ cần quyền `PhieuBuTruCongNo_TimTheoDieuKien`.

## Luồng dùng — Lập phiếu bù trừ
1. Mở **Kế toán → Phiếu bù trừ công nợ**, bấm lập phiếu.
2. Chọn đối tượng (MaDoiTuong), tài khoản phải thu / phải trả, ngày bù trừ → bấm **Lấy chứng từ** (`GetChungTu`).
3. Hệ thống gọi đồng thời: Khách hàng `GetCongNoPhaiThu` (gắn thêm thông tin phiếu bán theo IdDonHang) + Kho/NCC `GetCongNoPhaiTra`. Trả về danh sách gộp các chứng từ (mỗi dòng = 1 chứng từ thu hoặc trả).
4. Người dùng chọn dòng + nhập số tiền bù trừ (chỉ dòng số tiền > 0 mới ghi nhận), bấm **Lưu phiếu bù trừ** (`InsertPhieuBuTru`).
5. Hệ thống sinh số chứng từ (hoặc chống trùng nếu nhập tay), tạo phiếu `BuTruCongNo` + chi tiết + bản ghi rollback. Trong một giao dịch: insert phiếu → giảm công nợ KH (Khách hàng) **và** giảm công nợ NCC (Kho) — cả hai OK → commit; bất kỳ bên fail → hủy toàn bộ + đánh dấu để job khôi phục.
6. Thành công → đồng bộ danh mục MISA + đưa vào hàng đợi đẩy MISA; nếu là phiếu tạo tự động thì gắn ngược IdPhieuLienQuan vào phiếu mua/bán gốc. Bút toán Nợ/Có sinh riêng.

## Luồng dùng — Báo cáo công nợ tổng hợp
1. Mở **Kế toán → Báo cáo → Báo cáo công nợ tổng hợp**.
2. Hệ thống gọi cổng kiểm quyền xem báo cáo đơn hàng — không có quyền → chặn.
3. Chọn tab loại tài khoản (131 phải thu / 331 phải trả), lọc chi nhánh / khoảng ngày / nhóm đối tượng / từ khóa.
4. Nếu chọn nhóm đối tượng → hệ thống quy đổi ra danh sách mã KH + NCC; nếu rỗng thì truyền giá trị "không có dữ liệu" để không trả gì.
5. Chạy báo cáo (stored proc), làm giàu **tên đối tượng** từ Khách hàng + NCC.
6. Bấm một đối tượng → xem **chi tiết** từng chứng từ công nợ.

## Các trường hợp / case biên
- **Lấy chứng từ, một service lỗi:** nếu Khách hàng trả lỗi → fail luôn; nếu một task của Kho lỗi → trả lỗi (kiểm từng task).
- **Lập phiếu, một bên ghi nhận fail:** transaction không commit, bản ghi rollback đánh dấu để job khôi phục; không có phiếu nào được tạo.
- **Số chứng từ trùng (nhập tay):** chống trùng theo (đơn vị × chi nhánh × số chứng từ).
- **Xóa phiếu tạo tự động:** bị chặn "được tạo tự động, không thể xóa".
- **Xóa phiếu lập tay:** ghi lịch sử xóa → xóa vĩnh viễn phiếu + chi tiết → hoàn (rollback) công nợ từng dòng (phải trả → Kho, phải thu → Khách hàng); mọi rollback OK → commit.
- **Báo cáo, loại tài khoản lạ (vd "999"):** điều kiện validate hiện viết lỏng — giá trị lạ có thể lọt xuống stored proc (lỗi đã biết, xem dev.md).
- **Báo cáo, nhóm đối tượng rỗng:** truyền sentinel "KHONG_CO_DU_LIEU" → báo cáo không trả dòng nào.

## Ví dụ cụ thể có số
Đối tác X vừa là khách hàng vừa là nhà cung cấp:
- Phải thu (X nợ mình): 3.000.000đ (TK 131).
- Phải trả (mình nợ X): 2.000.000đ (TK 331).
- Lập phiếu bù trừ 2.000.000đ → giảm phải thu còn 1.000.000đ, giảm phải trả còn 0đ. Sinh bút toán Nợ 331 / Có 131 = 2.000.000đ, đẩy MISA.
- Báo cáo công nợ 131 sau bù trừ: X còn phải thu 1.000.000đ. Báo cáo 331: X = 0đ.
- Xóa phiếu bù trừ (nếu lập tay) → hoàn lại: phải thu về 3.000.000đ, phải trả về 2.000.000đ.

## Hậu điều kiện
- **Lập phiếu bù trừ:** công nợ phải thu KH ↓ + phải trả NCC ↓ + bút toán + đẩy MISA; phiếu lưu DB.
- **Xóa phiếu bù trừ:** công nợ 2 bên được hoàn lại + ghi lịch sử xóa + xóa cứng phiếu.
- **Báo cáo công nợ:** chỉ đọc (read-only), không thay đổi dữ liệu.
