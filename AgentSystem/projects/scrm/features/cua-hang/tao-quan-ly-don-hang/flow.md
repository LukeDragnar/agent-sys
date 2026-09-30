# Luồng — Tạo & quản lý đơn hàng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) + chi nhánh (IdChiNhanh); mọi thao tác đơn lọc theo đơn vị + chi nhánh của user.
- Đã có dữ liệu nền: sản phẩm/dịch vụ/combo/thẻ, cấu hình mặt hàng mặc định + khu vực/chỗ ngồi (nếu bán theo bàn).
- Có quyền tương ứng cho từng thao tác (tạo đơn, thanh toán, xóa…). Màn nhân viên phục vụ cần role DV-NV.
- Phát hành hóa đơn điện tử cần đơn vị đã bật tiện ích Misa hoặc Viettel/MeInvoice/SInvoice.

## Luồng dùng (thu ngân — vòng đời 1 đơn)
1. **Tạo đơn** (`TaoDon`): sinh đơn trạng thái **Chưa thanh toán**, mở tab POS (lưu Redis), gán bàn/khách/chi nhánh.
2. **Thêm chi tiết** (`ThemChiTietDon`): thêm SP/DV/combo/thẻ; trừ/cộng tồn theo cấu hình. Sửa/xóa dòng tùy ý khi đơn còn Chưa thanh toán.
3. **Áp ưu đãi**: `ApDungVoucher` (validate + áp voucher qua Promotion), khuyến mãi tự động, `CheckChietKhauTheoBill` (kiểm tra đủ điều kiện chiết khấu theo bill).
4. **Xếp nhân viên** (`LuuXepNv`): gán NV phục vụ cho từng DV/SP **có tiền** (combo đã mua không tính) để chia hoa hồng.
5. **Tính toán lại** (`TinhToanLai`): tính lại tổng tiền/giảm giá/KM/thuế. Cờ `IsDungKhiYcThanhToan` quyết định có dừng dịch vụ tính giờ hay không.
6. **Gửi yêu cầu thanh toán** (`GuiYeuCauThanhToan`): chốt yêu cầu → dừng dịch vụ tính giờ, sinh QR/thanh toán online nếu có.
7. **Thanh toán** (`ThanhToan`): ghi nhận tiền → trạng thái **Đã thanh toán**; trừ tồn chốt; sinh bản ghi thanh toán + phương thức thanh toán; kích hoạt ripple (đồng bộ KM/CTV, ghi thu chi, báo cáo doanh số, hoa hồng, giao vận).
8. **In & hóa đơn**: `InDon`/`InDonBanLe`/`InDonTrucTiep` (tự tính lại trước khi in); `PhatHanhHoaDon` (cần tiện ích Misa/Viettel); in phiếu bếp `InDonBepNv` cho khu chế biến.

## Luồng nhân viên phục vụ (role DV-NV)
- Màn nhân viên có bộ thao tác riêng: thêm/xóa món của NV, sửa số lượng/giá, bắt đầu/kết thúc dịch vụ tính giờ (`StopStartDv`), hoàn thành món (`HoanThanhSanPham`), ghi chú, xếp NV, in phiếu bếp.

## Luồng quản lý đơn
- **Đang phục vụ** (`DsDangPhucVu` / theo bàn / theo NV): theo dõi bàn đang có khách; **Chuyển bàn** (`ChuyenBan`) đổi chỗ ngồi (+ đèn bàn nếu có thiết bị BI_A).
- **Danh sách đơn hàng** (`TimTheoDieuKien`): lọc/tra cứu đơn (đi qua store service mới), mở chi tiết, xuất Excel, đối soát (`DoiSoatDonHang`).
- **Đóng/mở tab** (`CloseDon`/`OpenDon`/`CloseAllDon`): ẩn/hiện đơn khỏi danh sách đang mở.

## Các trường hợp / case biên
- **Xóa đơn — chuỗi guard** (dừng ở lỗi đầu tiên):
  1. Không tìm thấy đơn → báo không tồn tại.
  2. Đơn khác đơn vị / khác chi nhánh của user → báo không tồn tại (chặn cross-tenant).
  3. Đơn **Đã thanh toán** → "Đơn hàng đã thanh toán, không được xóa".
  4. Đơn tạo từ **phiếu mua hàng** (thu cũ đổi mới) → không cho xóa.
  5. Không phải Admin/AdminChiNhánh + đơn vị bật `IsKhongChoPhepXoaDonHang` → "Đơn vị không cho phép xóa đơn hàng!".
  6. Qua guard → xóa đơn + dọn dữ liệu liên quan + cập nhật Redis + đồng bộ MKT + **hoàn cọc** (nếu TienCoc>0, gọi Branch; lỗi thì dừng) + tắt đèn bàn + xóa ảnh KH (gọi Customer; lỗi thì dừng) + đồng bộ contact.
- **Hủy hóa đơn** (`HuyHoaDon`): đơn Đã thanh toán → rollback thanh toán → quay về **Chưa thanh toán** (không phải xóa).
- **Đổi trạng thái ngược**: không thể "xóa" thẳng đơn đã thanh toán — bắt buộc hủy thanh toán trước.
- **Thêm món khi tồn = 0**: phụ thuộc cấu hình kho (cho âm tồn hay chặn) — cần product xác nhận hành vi chính xác theo cấu hình.
- **Phát hành hóa đơn khi chưa bật tiện ích**: mọi method hóa đơn điện tử kiểm tra `CheckTienIch(Misa || Viettel)` — chưa bật → từ chối.
- **Tính lại tổng tiền không token** (`TinhToanLaiTongTienTra`): endpoint dùng cho mobile **không gắn auth** — xem mục Cần chú ý / dev.

## Ví dụ cụ thể có số
Quán cà phê, bàn B3:
- Thu ngân tạo đơn cho bàn B3 → đơn `Chưa thanh toán`.
- Thêm 2 cà phê (35.000đ/ly) + 1 bánh (45.000đ) → tạm tính 115.000đ.
- Áp voucher giảm 10% → giảm 11.500đ → còn **103.500đ**.
- Xếp nhân viên An phục vụ (để chia hoa hồng phần dịch vụ có tiền).
- Tính toán lại → Yêu cầu thanh toán → Thanh toán bằng chuyển khoản 103.500đ → đơn chuyển **Đã thanh toán**; doanh số 103.500đ vào báo cáo, tồn cà phê -2, bánh -1.
- Khách báo tính nhầm: thu ngân **Hủy hóa đơn** → đơn về `Chưa thanh toán` → bỏ bánh → tính lại còn 70.000đ - 7.000đ (10%) = **63.000đ** → Thanh toán lại.

## Hậu điều kiện
- Đơn ở trạng thái cuối (`Chưa thanh toán` hoặc `Đã thanh toán`); tab POS + cache đơn cập nhật trên Redis.
- Khi **thanh toán**: tồn trừ chốt; phát sinh bản ghi thanh toán/PTTT; đồng bộ MKT/CTV/điểm; ghi thu chi + báo cáo doanh số + hoa hồng (Branch); (tùy chọn) phát hành hóa đơn điện tử → callback cập nhật trạng thái.
- Khi **xóa**: dữ liệu đơn + chi tiết bị xóa, cọc được hoàn, ảnh KH bị xóa, contact đồng bộ; chiến dịch MKT được cập nhật (Type=2).
- Khi **hủy hóa đơn**: thanh toán bị rollback, đơn quay lại Chưa thanh toán (không mất chi tiết đơn).
