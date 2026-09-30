# Luồng — Quản lý tồn (dự báo)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Đã có dữ liệu nền: sản phẩm, nhóm sản phẩm, kho/chi nhánh.
- Để **dự báo hết hàng** chính xác: đã khai báo **Giới hạn tồn kho** cho sản phẩm × chi nhánh; đơn vị đã bật cấu hình **tính giới hạn tồn kho tự động** (nếu dùng dự báo theo khoảng ngày).
- Quyền: xem báo cáo tồn kho đơn vị cần role kho/kế toán/quản lý; xuất Excel báo cáo cần role admin; cấu hình ngưỡng cần admin (non-admin chỉ chi nhánh mình).

## Luồng dùng (xem dự báo & đặt hàng)
1. Người dùng mở **Báo cáo tồn kho đơn vị** (hoặc Kiểm tra tồn kho → tab Báo cáo tồn kho đơn vị).
2. Chọn điều kiện lọc (chi nhánh, nhóm SP, khoảng ngày). Nếu là non-admin, hệ thống **ép chi nhánh = chi nhánh của user** (không xem chéo chi nhánh).
3. Bấm xem → hệ thống gọi báo cáo tồn kho qua stored proc, trả số tồn theo SP và sản lượng bán.
4. Bật **Dự báo hết hàng**: hệ thống đặt cờ dự báo và (nếu bật giới hạn tự động) tính tuNgay/denNgay/soNgay theo **kiểu khoảng ngày** đã chọn → so tồn hiện có với ngưỡng/tốc độ bán để đánh dấu mặt hàng sắp hết.
5. Người dùng xem danh sách mặt hàng thiếu → bấm **Tạo đơn đặt hàng** cho phần cần nhập → chuyển sang quy trình Đơn đặt hàng.
6. (Tùy chọn) Bấm **Xuất Excel** báo cáo (chỉ admin, ≤10.000 dòng).

## Luồng cấu hình (admin — Giới hạn tồn kho)
1. Admin mở **Kho → Giới hạn tồn kho**.
2. Chọn một trong ba cách nhập ngưỡng:
   - **Thêm mới** (≤100 dòng): mỗi dòng = 1 cặp (sản phẩm × chi nhánh) với tồn tối thiểu/tối đa/khẩn cấp, số ngày tồn, SL bán TB, khoảng ngày tính TB. Bật "áp dụng tất cả chi nhánh" (chỉ admin) sẽ nhân bản cho mọi chi nhánh đơn vị.
   - **Khởi tạo**: nhập 1 bộ cấu hình → hệ thống quét toàn bộ SP đơn vị (lọc theo nhóm nếu chọn) và áp.
   - **Nhập Excel** (≤2.000 dòng): tải file mẫu, điền theo cột, tải lên; dòng lỗi xuất ra file lỗi để tải về sửa.
3. Hệ thống validate từng dòng: các trường tồn/số ngày/SL bán TB không null, không âm; tồn tối thiểu ≤ tối đa; số ngày tối thiểu ≤ tối đa; khoảng ngày ∈ [1..13]; sản phẩm bắt buộc.
4. Lưu theo cơ chế upsert theo (IdDonVi, IdSanPham, IdChiNhanh): đã có → cập nhật, chưa có → tạo mới.

## Case biên
- **Non-admin cấu hình/xem** → bị ép `IdChiNhanh = chi nhánh của user`, không được "áp dụng tất cả chi nhánh".
- **Lưới tồn kho không nhập nhóm SP** (ở màn Quản lý tồn kho) → trả rỗng (yêu cầu bắt buộc ListIdNhomSanPham).
- **Dự báo hết hàng nhưng chưa chọn kiểu khoảng ngày** (khi bật giới hạn tự động) → báo lỗi yêu cầu chọn kiểu khoảng ngày.
- **Xuất Excel báo cáo tồn kho đơn vị > 10.000 dòng** → bị chặn (validate OffSet≥0, PageSize>0, PageSize≤10.000); non-admin bị chặn hẳn (chỉ admin xuất được).
- **Khai trùng ngưỡng** (cùng SP × chi nhánh trong 1 batch) → chặn "Trước đó đã có bản ghi trùng sản phẩm và chi nhánh!".
- **Sửa nhanh trên lưới Giới hạn tồn kho** (chỉ gửi tồn tối thiểu/tối đa) → service vẫn validate đủ trường nên payload thiếu có thể bị từ chối (xem dev.md, rủi ro lệch FE↔BE).
- **Xóa ngưỡng của chi nhánh/đơn vị khác** → từ chối "Bạn không có quyền xóa!".
- **Import sai khoảng ngày** (lệch hoa/thường so với template) → dòng đó báo "không hợp lệ", xuất vào file lỗi.

## Ví dụ cụ thể có số
Shop B bán sản phẩm "Sữa rửa mặt X" tại chi nhánh Quận 1.
- Admin cài Giới hạn tồn kho: tồn tối thiểu = 20, tối đa = 200, SL bán TB nhỏ nhất = 5/ngày, khoảng ngày tính TB = "30 ngày trước". Lưu OK (20 ≤ 200, đủ trường).
- Cuối tuần, tồn thực tế còn 18 (< 20). Mở **Báo cáo tồn kho đơn vị → Dự báo hết hàng**, chọn kiểu khoảng ngày → sản phẩm này hiện cảnh báo "dưới tồn tối thiểu".
- Người mua hàng bấm **Tạo đơn đặt hàng**, đặt mua bù lên gần tối đa (ví dụ 200 − 18 = 182) → chuyển sang quy trình Đơn đặt hàng.
- Admin thử Khởi tạo lại với tồn tối thiểu = 250 nhưng tối đa = 200 → báo lỗi (tối thiểu > tối đa).
- Admin xuất Excel báo cáo, dữ liệu 12.000 dòng → bị chặn vì > 10.000; lọc theo nhóm còn 6.500 dòng → xuất ra `BaoCaoTonKho.xlsx` thành công.

## Hậu điều kiện
- Xem báo cáo/dự báo **không thay đổi dữ liệu tồn kho** (read-only); chỉ truy vấn và đánh dấu cảnh báo.
- Thêm/Sửa/Khởi tạo/Import **ghi cấu hình ngưỡng** vào DB (`SanPhamGioiHanTonKho`); xóa là xóa cứng. Không bắn message/event ra ngoài.
- Tạo đơn đặt hàng từ màn dự báo **sinh đơn ở trạng thái nháp/chờ duyệt** trong quy trình Đơn đặt hàng (xem feature Quản lý đơn đặt hàng).
