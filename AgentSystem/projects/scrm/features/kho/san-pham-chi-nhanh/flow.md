# Quản lý sản phẩm chi nhánh — Luồng

> Tóm tắt: [summarize.md](./summarize.md) · Module: [../README.md](../README.md) · Kỹ thuật: [dev.md](./dev.md)

## Tiền điều kiện
- Đơn vị đã có **chi nhánh** và người dùng đang đăng nhập **gắn 1 chi nhánh** (phiên đăng nhập có `IdChiNhanh`).
- Có **nhóm sản phẩm / nhóm dịch vụ** để lọc danh sách.
- Người dùng có **quyền** truy cập màn quản lý kho/dịch vụ của chi nhánh *(danh sách quyền cụ thể: cần product xác nhận)*.

## Luồng dùng (user)
1. Vào module **Kho** → chọn màn hình: Sản phẩm / Dịch vụ / Combo / Thẻ chi nhánh.
2. (SP & DV) Chọn **nhóm** ở cột trái → hệ thống tải danh sách món của chi nhánh đang đăng nhập.
3. Có thể **tìm theo từ khóa**, **lọc theo chi nhánh** (với tài khoản có nhiều chi nhánh), bật/tắt cột hiển thị.
4. **Thêm mới**: nhập thông tin món (tên, mã, giá bán, nhóm…) → lưu → món gắn `IdChiNhanhTao` = chi nhánh hiện tại và được gán cho chi nhánh.
5. **Sửa / Xóa / Bật-tắt sử dụng** từng món.
6. (SP) **Import Excel** danh sách sản phẩm chi nhánh.
- **Kết quả mong đợi:** danh mục riêng của chi nhánh được cập nhật; chỉ chi nhánh đó nhìn thấy & dùng.

## Các trường hợp / nhánh (case biên)
| Tình huống | Kết quả |
|---|---|
| Mở danh sách nhưng **chưa chọn nhóm** | Trả về rỗng / báo "Hãy truyền nhóm" — không có dữ liệu |
| Đứng ở **chi nhánh A** xem món của **chi nhánh B** | Không thấy (mỗi phiên chỉ thấy món do chi nhánh hiện tại tạo + được gán) |
| Món do đơn vị tạo (cấp đơn vị) nhưng **chưa gán** cho chi nhánh | Không hiện ở màn chi nhánh (cần có bản ghi gán chi nhánh) |
| Combo cấu hình **giới hạn số lượng theo chi nhánh** | Mỗi chi nhánh có hạn mức riêng; có thể đặt "không giới hạn" |
| Tắt "sử dụng" một món | Ngừng cho chọn khi lên đơn/đặt lịch, không xóa dữ liệu |
| Thẻ chi nhánh: loại thẻ tiền vs thẻ số lần | Cấu hình `GiaTri` (thẻ tiền) hoặc `SoLanSuDung` (thẻ số lần) theo loại |

## Ví dụ cụ thể có số
- Chi nhánh **Cầu Giấy** tạo dịch vụ **"Massage 60 phút" giá 300.000đ**, gán cho chi nhánh Cầu Giấy.
  - Nhân viên đứng ở **Cầu Giấy** → thấy & bán được dịch vụ này.
  - Nhân viên đứng ở **chi nhánh Hà Đông** → **không** thấy dịch vụ này trong màn dịch vụ chi nhánh của mình.
- Combo **"Gói 5 buổi chăm sóc da"** cấu hình **giới hạn 100 suất tại chi nhánh Cầu Giấy**, **không giới hạn** tại chi nhánh Hà Đông → hai chi nhánh có hạn mức bán khác nhau cho cùng tên combo.

## Hậu điều kiện
- Danh mục SP/DV/Combo/Thẻ của chi nhánh được cập nhật, sẵn sàng cho bán hàng / đặt lịch tại chi nhánh đó.
- Món mới gắn `IdChiNhanhTao` = chi nhánh tạo và có bản ghi gán chi nhánh (junction `*ChiNhanh`).
