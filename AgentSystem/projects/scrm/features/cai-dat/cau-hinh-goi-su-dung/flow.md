# Luồng — Cấu hình gói sử dụng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId).
- Nâng cấp / xem gói: quyền `NangCapGoiSuDung_*` (DV-AD-TONG, DV-AD-CN/MCN, HT-CSKH).
- Nạp tiền SMS: quyền `SMSLichSuNapTien_NapTienSMS` / `_TimTheoDieuKien` (DV-AD-TONG, HT-CSKH).
- Để có hiệu lực: cần admin hệ thống (SuperUser/HT-KT/HT-AD) duyệt (yêu cầu nâng cấp + yêu cầu nạp tiền).

## Luồng dùng (đơn vị)
**A. Nâng cấp / gia hạn gói:**
1. Mở `caidat/nang-cap-goi-su-dung` → `GetGoiSuDungHienTai` (gói hiện tại) + `GetListGoiSuDung` (gói có thể lên) + `GetThoiHanGoi` (mốc thời hạn).
2. Chọn gói + thời hạn; (tùy chọn) nhập mã giới thiệu → `CheckSoLuotApDungMaGioiThieu` kiểm tra số lượt còn lại.
3. Bấm Xác nhận → `NangCapSuDung(model, CurrentUser)` tạo yêu cầu (trạng thái chờ duyệt — `NangCapGoiSuDung`).

**B. Nạp tiền SMS:**
1. Mở `caidat/lich-su-nap-tien-sms` → `TimTheoDieuKien` (lịch sử nạp).
2. Bấm Nạp tiền → `NapTienSMS` (`SMNapTienDto`) tạo yêu cầu trạng thái `ChuaDuyet`.

## Luồng cấu hình (admin hệ thống — QuanTriArea, ngoài cẩm nang khách)
**Duyệt nâng cấp gói** (`GoiSuDungController`, route `api/QuanTriArea/GoiSuDung`):
1. `DanhSachNangCap` liệt kê yêu cầu chờ.
2. `DuyetYeuCauNangCap` — chỉ chạy nếu `IsSuperUser || IsHeThongKeToan() || IsAdminHeThong()`, ngược lại "Bạn không có quyền sử dụng chức năng này". Duyệt → cập nhật gói áp cho đơn vị.
3. `HuyDuyetNangCap?idNangCap=` — hủy duyệt (cùng điều kiện quyền).

**Duyệt nạp tiền SMS** (Area QuanTri `SMSLichSuNapTien`):
1. Admin duyệt/hủy yêu cầu nạp → cộng số dư `AccountSms`.

## Case biên
- **Mã giới thiệu hết lượt / không hợp lệ** → `CheckSoLuotApDungMaGioiThieu` trả không áp được → chặn nâng cấp với mã đó.
- **ModelState invalid khi NangCapSuDung** → báo lỗi model, không tạo yêu cầu.
- **Duyệt khi thiếu quyền** → trả lỗi "Bạn không có quyền sử dụng chức năng này" (gate runtime, không chỉ ở attribute).
- **`GetThoiHanGoi`** không gắn permission key → mọi user login gọi được (lệch so với các action khác cần role admin — xem dev.md).
- **Nạp SMS chưa duyệt** → số dư AccountSms chưa cộng; chỉ ở trạng thái chờ duyệt.
- **Xóa gói (`GoiSuDung_Xoa`)** override kiểm tra tồn tại trước (`GetByIdAsync ≠ null`), không có → `MESS_NotFound`.

## Ví dụ cụ thể có số
Đơn vị Z đang dùng gói "Standard", hết hạn còn 10 ngày.
- Admin Z mở màn nâng cấp → gói hiện tại "Standard". Chọn gói "Pro", thời hạn 12 tháng, nhập mã giới thiệu `GT2026`.
- `CheckSoLuotApDungMaGioiThieu("GT2026")` còn 3 lượt → áp được → bấm Xác nhận → tạo yêu cầu nâng cấp "Pro 12 tháng" trạng thái chờ duyệt.
- CSKH hệ thống mở `DanhSachNangCap` → thấy yêu cầu của Z → `DuyetYeuCauNangCap` (đủ quyền) → gói Z chuyển "Pro", hạn mức tính năng tăng theo gói Pro.
- Admin Z gửi yêu cầu nạp 500.000đ vào tài khoản SMS → trạng thái chờ duyệt; sau khi admin hệ thống duyệt → số dư AccountSms của Z +500.000đ tương ứng số tin.

## Hậu điều kiện
- Yêu cầu nâng cấp được duyệt → gói/quota đơn vị cập nhật → ảnh hưởng quyền & giới hạn module toàn đơn vị.
- Yêu cầu nạp SMS được duyệt → số dư `AccountSms` tăng, dùng cho gửi tin.
- Mã giới thiệu được áp → giảm số lượt còn lại của mã (tracking đại lý).
