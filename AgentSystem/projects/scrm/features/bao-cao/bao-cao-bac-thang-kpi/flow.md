# Báo cáo bậc thang & KPI — Luồng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Có quyền xem báo cáo (đa số public ở mức controller).
- Đơn vị đã khai báo cơ cấu tổ chức (phòng ban/nhóm/nhân viên Sale-MKT) và có dữ liệu thành tích (đơn/contact).
- Nếu bật giới hạn khoảng ngày, khoảng Từ–Đến phải nằm trong giới hạn.

## Luồng dùng (user)
1. Mở module **Báo cáo** → chọn Bảng xếp hạng Sale / Bảng xếp hạng MKT / Phong thần bảng (Sale/MKT/Nhóm sale).
2. Chọn **chi nhánh** + **khoảng ngày** + lọc **cơ cấu tổ chức**.
3. Hệ thống validate khoảng ngày → ép chi nhánh user (nếu thường) → lọc cơ cấu tổ chức.
4. **Ép LoaiNhanVien theo báo cáo:** BXH Sale = 1, BXH MKT = 3 (cùng gọi `ReportBangXepHang` chung 1 SP, chỉ khác LoaiNhanVien).
5. Chạy stored proc tương ứng (read-only). Phong thần bảng mở connection riêng (`OpenNewConnectionOrDefault`).
6. Sắp xếp kết quả theo `TruongSapXep`/`KieuSapXep` tại ứng dụng.
7. Trả về bảng xếp hạng; user xem, sắp xếp lại theo cột.

## Luồng cấu hình (admin)
- Khai báo cơ cấu tổ chức (phòng ban/nhóm/nhân viên) và phân loại Sale/MKT — quyết định ai vào bảng nào.
- Cấu hình giới hạn khoảng ngày xem báo cáo.

## Các trường hợp / case biên
- **Vượt giới hạn khoảng ngày** → lỗi "Chỉ được xem dữ liệu trong khoảng N ngày".
- **User thường không chọn chi nhánh** → ép về chi nhánh user; chỉ thấy phạm vi cơ cấu tổ chức được phép.
- **BXH Sale vs MKT** → cùng SP `SCRM_Report_ReportBangXepHang`, khác `LoaiNhanVien` (1 vs 3) → nếu truyền sai loại, danh sách nhân viên sai nhóm.
- **Phong thần bảng** → 3 biến thể (Sale/MKT/Nhóm sale), mỗi cái 1 SP riêng, mở connection riêng cho store nặng.
- **Không có thành tích trong kỳ** → bảng rỗng/0 điểm, không lỗi.
- **Phong thần bảng bản gốc đã bị comment** trong controller (xem dev.md) — chỉ các biến thể MKT/Sale/NhomSale còn dùng.

## Ví dụ cụ thể có số
Shop E có 12 Sale (2 nhóm) và 5 MKT, kỳ 01–30/06.
- Quản lý mở **BXH Sale** → hệ thống ép LoaiNhanVien=1, xếp 12 Sale theo doanh số: Hùng #1 (150tr), Lan #2 (120tr)…
- Mở **BXH MKT** → ép LoaiNhanVien=3, xếp 5 MKT theo chỉ số MKT.
- Mở **Phong thần bảng Nhóm sale** → Nhóm A tổng 320tr đứng trên Nhóm B 280tr.
- Trưởng nhóm B (chỉ xem nhóm B) mở BXH Sale → chỉ thấy thành viên nhóm B, không thấy nhóm A.
- Chọn khoảng 200 ngày nhưng đơn vị giới hạn 90 ngày → báo lỗi giới hạn ngày.

## Hậu điều kiện
- Truy vấn báo cáo **không thay đổi dữ liệu** (read-only), không bắn message/event.
- Log query chậm khi super/CSKH hoặc đơn vị bật "show query".
