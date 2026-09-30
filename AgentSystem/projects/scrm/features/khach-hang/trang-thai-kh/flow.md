# Flow — Quản lý trạng thái khách hàng

Màn hình: **Trạng thái khách hàng** (module Khách hàng). Trace nghiệp vụ chính: `TrangThaiKhachHang_TimTheoDieuKien`.

## Tiền điều kiện
- Đã đăng nhập, tài khoản thuộc một **đơn vị (shop)**; đơn vị gắn với một **ngành nghề**.
- Để **xem** danh sách trạng thái: cần 1 trong các role `DV-AD-TONG`, `DV-AD-CN`, `DV-AD-MCN`, `DV-QLDV`.
- Để **thêm / sửa / xóa**: cần `DV-AD-TONG` hoặc `DV-QLDV`.
- Thường khi đơn vị được khởi tạo, hệ thống đã sinh sẵn bộ trạng thái mẫu theo ngành nghề (qua consumer khởi tạo đơn vị).

## Luồng dùng (chính)
1. Người dùng vào **Khách hàng → Trạng thái khách hàng**.
2. Hệ thống lấy danh sách ngành nghề; chọn ngành nghề đầu tiên mặc định.
3. Màn gọi **Tìm theo điều kiện** (`TimTheoDieuKien`) với từ khóa rỗng → trả về toàn bộ trạng thái của **đơn vị hiện tại**, sắp xếp theo **Số thứ tự** tăng dần.
4. Bảng hiển thị cột: Tên, Số thứ tự, Màu nền, Ngày cập nhật.
5. Tìm kiếm: gõ từ khóa → lọc các trạng thái có Tên *chứa* từ khóa.
6. Thêm mới: bấm **Thêm mới** → một dòng trống được chèn lên đầu bảng (số thứ tự gợi ý = số dòng hiện có + 1) → nhập Tên, Màu, Số thứ tự.
7. Lưu: **Lưu tất cả** gọi `UpdateMany` (tạo dòng mới + cập nhật dòng đã sửa trong một lần), hoặc lưu từng dòng qua `ThemMoi` / `CapNhatThongTin`.
8. Sau khi lưu/xóa thành công → **xóa cache** danh sách trạng thái theo đơn vị và load lại bảng.

## Case biên
- **Tên trùng:** thêm/sửa một trạng thái có Tên đã tồn tại trong đơn vị → báo lỗi "đã bị trùng tên", không lưu. Trùng được kiểm tra cả với dòng đã có DB lẫn giữa các dòng đang nhập một lần.
- **Bỏ trống bắt buộc:** Tên / Số thứ tự / Màu nền để trống khi lưu hàng loạt → báo lỗi tương ứng, dừng cả lô (không lưu dòng nào).
- **Khoảng trắng thừa:** Tên được `Trim()` trước khi kiểm tra trùng và lưu → "Khách mới " và "Khách mới" coi là trùng.
- **Đơn vị khác:** dữ liệu lọc theo `IdDonVi` của người đăng nhập → không thấy/không sửa được trạng thái của shop khác.
- **Khởi tạo lại:** đơn vị đã có trạng thái mà chạy khởi tạo lại → bỏ qua, báo "Đơn vị đã được khởi tạo trạng thái khách hàng" (không tạo trùng).
- **Không có dữ liệu mẫu cho ngành nghề:** khởi tạo không tạo gì, trả về thành công (bảng rỗng, người dùng tự thêm).
- **Xóa id không tồn tại:** kiểm tra theo Id trước; nếu không tìm thấy bản ghi thì trả về luôn, không xóa.

## Ví dụ có số
Shop A (đơn vị `DV-A`, ngành "Spa"), người dùng có role `DV-QLDV`:
- Khởi tạo đơn vị sinh sẵn 3 trạng thái mẫu: `Khách mới` (Stt 1), `Đang chăm sóc` (Stt 2), `Đã chốt` (Stt 3).
- Người dùng mở màn → `TimTheoDieuKien` trả về 3 dòng, sắp theo Stt: 1, 2, 3.
- Thêm 2 dòng: `Đang tư vấn` (Stt 2, màu vàng) và `Ngừng quan tâm` (Stt 5, màu xám) → bấm **Lưu tất cả** (`UpdateMany`).
  - Hợp lệ → DB còn 5 trạng thái. Hiển thị lại theo Stt: 1 Khách mới, 2 Đang tư vấn, 2 Đang chăm sóc, 3 Đã chốt, 5 Ngừng quan tâm.
- Thử thêm thêm `Đã chốt` (trùng tên) → báo "'Đã chốt' đã bị trùng tên", lô không được lưu.
- Sửa `Ngừng quan tâm` → Stt 4 và lưu → bảng sắp lại đúng thứ tự 1,2,2,3,4.

## Hậu điều kiện
- Bảng `TrangThaiKhachHang` của đơn vị phản ánh đúng các trạng thái vừa thêm/sửa/xóa.
- **Cache** danh sách trạng thái theo đơn vị đã bị xóa → lần lấy tiếp theo (ví dụ ở hồ sơ khách) đọc lại từ DB và cache mới.
- Các trạng thái này sẵn sàng để **gắn cho từng khách** ở màn hồ sơ khách hàng (lưu vào bảng liên kết `KhachHangTrangThaiKhachHang`).
