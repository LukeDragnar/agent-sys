# Luồng — Telesales

## Tiền điều kiện
- Nhân viên tele đã đăng nhập, có role sale/MKT phù hợp để thao tác trên contact.
- Đã có **data (contact/lead)** được chia cho nhân viên (qua Chia Lead/số). Chưa chia → danh sách rỗng.
- Để gọi trực tiếp: đơn vị đã **khởi tạo & kết nối tổng đài** (1 trong 3 phiên bản) và map user.

## Luồng dùng (nhân viên telesales)
1. Mở **Sale → Danh sách contact** → lọc data của mình.
2. Với contact còn ẩn thông tin: bấm **Nhận data** (`NhanData`) → hệ thống bỏ ẩn thông tin, bắt đầu đếm thời gian tác nghiệp.
3. **Gọi** khách: click-to-call (qua tổng đài) hoặc gọi ngoài; hệ thống ghi **lịch sử cuộc gọi**.
4. Sau cuộc gọi: **Cập nhật kết quả tác nghiệp** (`CapNhatKetQuaTacNghiep`), **gắn nhãn** (`CapNhatNhanKhachHang`), **ghi chú** (`SaveNote`/`SaveMessage`).
5. Tùy kết quả:
   - Khách hẹn xem/nhận hàng → **Chuyển tác nghiệp tiếp** hoặc **Đặt lịch** (`DatLichContact`).
   - Khách chốt → **Tạo đơn** / **Tạo đơn khách cũ** (`TaoDonHangKhachCu`).
6. Lặp lại với số tiếp theo.

## Luồng cấu hình / quản lý (sale leader)
1. **Chia data** cho nhân viên (phân bổ data sale/nhóm/MKT — `PhanBoLaiData`...).
2. **Duyệt lịch hẹn** do tele đặt: `GetListContactDatLichChoDuyet` → `DuyetContactDeTaoLich` (tạo lịch thật) hoặc `HuyDuyetContactTaoLich` (trả về chờ duyệt).
3. **Thu hồi data** chưa gọi/không hiệu quả (`ThuHoiSaleContact`) để chia lại.

## Các trường hợp / case biên
- **Chưa được chia data** → danh sách contact rỗng, không có số để gọi.
- **Contact ẩn, chưa Nhận data** → thông tin số bị ẩn cho tới khi bấm Nhận data.
- **Đặt lịch khi đơn vị bật duyệt lịch** → lịch ở trạng thái **Chờ duyệt (DatLich_ChoDuyet)**; chỉ thành lịch thật sau khi leader **Duyệt (DatLich_DaDuyet)**; leader **Hủy duyệt (DatLich_HuyDuyet)** → quay lại Chờ duyệt.
- **Gọi trùng số / khách cũ** → màn contact cảnh báo số trùng / khách cũ (đã có đơn) để tránh gọi chồng.
- **Đơn vị chưa bật tổng đài** → không click-to-call được; vẫn gọi ngoài và ghi nhận kết quả thủ công.
- **Xóa hàng loạt contact** → chỉ DV-AD-TONG được phép (siết quyền); nhân viên tele thường không xóa hàng loạt.

## Ví dụ cụ thể có số
Đội tele 5 người, leader chia 500 lead (mỗi người 100).
- NV An mở Danh sách contact → thấy **100 contact** của mình. Bấm Nhận data contact #1 → bắt đầu gọi.
- An gọi 60 số trong ngày: 25 "đã liên hệ", 10 "hẹn gọi lại", 8 "đặt lịch" (đang Chờ duyệt), 5 "chốt đơn" (tổng 5 đơn ~ 7,5 triệu), 12 "không nghe máy".
- Leader vào **Chờ duyệt lịch** thấy 8 lịch → duyệt 7, hủy 1 (sai thông tin) → 7 lịch hẹn chính thức.
- Cuối ngày Báo cáo Sales: An có 60 cuộc gọi, 5 đơn, tỉ lệ chốt 5/60 ≈ 8,3%.

## Hậu điều kiện
- Kết quả tác nghiệp + nhãn + ghi chú lưu lên contact (read sau bởi báo cáo).
- Lịch sử cuộc gọi sinh bản ghi ở tổng đài.
- Lịch hẹn đã duyệt → ripple sang Booking/LichHen.
- Đơn chốt → sinh đơn hàng (ripple OrderLogistic) + gán sale lên KhachHang.
