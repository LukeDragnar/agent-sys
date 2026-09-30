# Luồng — Thêm mới nhà cung cấp & nhóm nhà cung cấp

> Trace: `NhaCungCap_ThemMoi`, `NhomNhaCungCap_ThemMoi`

## Tiền điều kiện
- Đã đăng nhập, có một trong các quyền: Admin tổng / Kế toán / Quản lý kho... (xem frontmatter `summarize.md`). Thêm/sửa **nhóm NCC** yêu cầu quyền **Admin tổng**.
- (Khuyến nghị) đã tạo sẵn **Nhóm nhà cung cấp**, **Nhân viên mua hàng**, danh mục **Tài khoản công nợ phải trả** nếu muốn gán.

## Luồng dùng — Thêm nhà cung cấp
1. Menu Kho → **Nhà cung cấp** → **Thêm mới** (mở popup).
2. Nhập **Tên**, **Mã**, **Số điện thoại** (bắt buộc). Nhập thêm các trường tùy chọn.
3. (Tùy chọn) gõ SĐT rồi rời ô → hệ thống dò trong bảng khách hàng; nếu khớp sẽ tự điền tên/email/địa chỉ và bật "đồng thời là khách hàng".
4. Bấm **Lưu**. Hệ thống kiểm tra hợp lệ → tạo NCC → đóng popup → load lại danh sách.

## Luồng dùng — Thêm nhóm nhà cung cấp
1. Menu Kho → **Nhóm nhà cung cấp** → **Thêm mới**.
2. Nhập **Tên nhóm** (bắt buộc), tùy chọn **STT** để sắp xếp.
3. Bấm **Lưu** → nhóm xuất hiện trong dropdown "Nhóm" ở màn hình NCC.

## Case biên
- **Thiếu Tên / Mã / SĐT** → báo lỗi từng trường, không lưu.
- **SĐT sai định dạng** (theo regex cấu hình đơn vị) → "Số điện thoại không hợp lệ".
- **Email sai định dạng** → báo lỗi (chỉ kiểm khi có nhập).
- **Mã trùng NCC khác** → "Mã nhà cung cấp đã tồn tại".
- **Mã trùng một khách hàng đã có** → trả về yêu cầu **xác nhận** (popup "đồng thời là NCC/khách hàng"); người dùng đồng ý → lưu, từ chối → hủy.
- **Số ngày nợ tối đa < số ngày được nợ** → báo lỗi.
- **Đối tác trả góp** đã gán cho NCC khác → "Đối tác trả góp đã tồn tại".
- **Nhóm NCC**: bỏ trống tên → "Tên nhóm nhà cung cấp không được để trống".

## Ví dụ có số
- Tạo NCC "Công ty Vật Tư An Phát", Mã `NCC001`, SĐT `0901234567`, **Số ngày được nợ = 15**, **Số ngày nợ tối đa = 30** → hợp lệ, lưu thành công.
- Sửa lại **nợ tối đa = 10** (< 15) → bị chặn: "Số ngày nợ tối đa phải lớn hơn hoặc bằng Số ngày được nợ".
- Tạo NCC thứ hai cũng đặt Mã `NCC001` → bị chặn: "Mã nhà cung cấp đã tồn tại".

## Hậu điều kiện
- Bản ghi NCC mới tồn tại trong cửa hàng (gắn `IdDonVi` + `IdNganhNghe`), trạng thái Sử dụng theo lựa chọn.
- Nếu bật "đồng thời là khách hàng" → thông tin được đồng bộ sang module **Khách hàng** dùng chung mã.
- NCC sẵn sàng để chọn trong **phiếu mua hàng / đơn đặt hàng**; nhóm NCC sẵn sàng để lọc/báo cáo.
