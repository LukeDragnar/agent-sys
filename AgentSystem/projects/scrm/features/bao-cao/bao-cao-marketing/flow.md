# Báo cáo marketing — Luồng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Có quyền xem báo cáo (đa số public ở mức controller).
- Đơn vị đã khai báo cơ cấu tổ chức MKT (nhóm/nhân viên) và có dữ liệu lead/contact/công việc.
- Nếu bật giới hạn khoảng ngày, khoảng Từ–Đến phải nằm trong giới hạn.

## Luồng dùng (user)
1. Mở module **Báo cáo** → chọn Báo cáo công việc MKT (tác nghiệp / view sale) hoặc MKT theo nguồn landing.
2. Chọn **chi nhánh** + **khoảng ngày** + lọc **cơ cấu tổ chức MKT**.
3. Hệ thống validate khoảng ngày → ép chi nhánh user (nếu thường) → lọc cơ cấu tổ chức (LoaiNhanVien=3 cho MKT).
4. Chạy stored proc tương ứng (read-only).
5. Trả về bảng + tổng; user phân trang, sắp xếp.
6. Bấm **Xuất Excel** (nếu có) → build lại + xuất `.xlsx`.

## Luồng cấu hình (admin)
- Khai báo cơ cấu tổ chức MKT (nhóm/nhân viên) — quyết định ai vào báo cáo.
- Khai báo nguồn/kênh landing để phân tích MKT theo nguồn.
- Cấu hình giới hạn khoảng ngày xem báo cáo.

## Các trường hợp / case biên
- **Vượt giới hạn khoảng ngày** → lỗi "Chỉ được xem dữ liệu trong khoảng N ngày".
- **User thường không chọn chi nhánh** → ép về chi nhánh user; chỉ thấy phạm vi cơ cấu tổ chức được phép.
- **Hai view công việc MKT** → cùng nguồn dữ liệu, khác SP (`_View_TacNghiep` vs `_View_Sale`); số liệu trình bày khác nhau, không nên cộng dồn nhầm.
- **MKT theo nguồn landing** → dùng SP riêng cho manager (`ReportDoanhSoMktForManager`).
- **Không có hoạt động MKT trong kỳ** → bảng rỗng, không lỗi.
- **Xuất Excel khi service bận** → trả 503.

## Ví dụ cụ thể có số
Shop G có 5 nhân viên MKT (1 nhóm), kỳ 01–30/06.
- Quản lý MKT mở **Công việc MKT view tác nghiệp** → bảng khối lượng: MKT An tạo 500 lead, chăm sóc 320 lượt.
- Mở **Công việc MKT view sale** → cùng dữ liệu nhìn theo chuyển giao: trong 500 lead của An, 180 lead được Sale tiếp nhận, 45 chốt đơn.
- Mở **MKT theo nguồn landing** → nguồn "Facebook Ads" ra 800 lead, "Google" 300 lead trong kỳ.
- Nhân viên MKT thường (chỉ xem mình) → chỉ thấy số liệu của bản thân.
- Chọn khoảng 200 ngày nhưng giới hạn 90 ngày → báo lỗi giới hạn ngày.

## Hậu điều kiện
- Truy vấn báo cáo **không thay đổi dữ liệu** (read-only), không bắn message/event.
- Log query chậm khi super/CSKH hoặc đơn vị bật "show query".
