# Luồng — Báo cáo Marketing

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị/chi nhánh, có vai được phép xem báo cáo MKT (lọc theo user/nhóm MKT trong quyền).
- Đã có dữ liệu vận hành để báo cáo: lead/contact, đơn hàng, đặt lịch, số liệu landing, ngân sách quảng cáo.
- Cấu hình đơn vị về khoảng thời gian cho phép tìm kiếm + cơ cấu tổ chức MKT (phòng ban/nhóm) đã thiết lập.
- Còn "slot" đọc báo cáo đồng thời (hệ thống giới hạn số người đọc báo cáo online/marketing cùng lúc).

## Luồng dùng (Báo cáo lead theo nguồn — Dashboard)
1. Người dùng mở **Marketing → Dashboard**, đặt bộ lọc (chi nhánh, phòng ban/nhóm/user MKT, từ–đến ngày, nguồn/landing/campaign, điều kiện contact).
2. Hệ thống **kiểm tra quyền multi-user MKT** (`CheckPermissionViewrReportContactWithMultiUser`) → set danh sách user/nhóm được xem; nếu fail → trả lỗi xác thực quyền.
3. Hệ thống **lấy 1 slot đọc đồng thời** (`ReleaseLockedTicket`) → nếu hết slot → báo lỗi (bận).
4. Chạy báo cáo: fan-out 4 stored proc nguồn (Contact / OrderLogistic / DonHang / DatLich) + SP ngân sách landing → **merge trong code** → lọc theo loại contact → sắp xếp (số contact / tỷ lệ chốt / doanh số) → **phân trang trong API** (không phân trang ở SP).
5. Hiển thị lưới + biểu đồ landing 7 ngày (`GetStatisticsLast7DayOfLanding` → 1 SP riêng).
6. Bấm một dòng → **chi tiết campaign** (`GetCampaingStatisticDetails`) → dùng bộ 4 SP chi tiết campaign, merge + phân trang tương tự.
7. Bấm **Xuất Excel** → chạy lại báo cáo lead theo nguồn rồi build file `BaoCaoLeadTheoNguon.xlsx`.

## Luồng dùng (Báo cáo Marketing theo sản phẩm)
1. Mở màn **Báo cáo MKT theo sản phẩm**, lọc (chi nhánh, từ–đến ngày, sản phẩm/nhóm SP, nguồn landing, nhân sự MKT).
2. Hệ thống validate khoảng ngày + ép chi nhánh user nếu không phải admin → chạy bộ SP (`PRC_SCRM_Report_BaoCaoMktTheoSanPham` + `_SoContact` + `_AllLanding` + ngân sách landing) → gộp → trả lưới.
3. **Xuất Excel** theo 2 endpoint: `...NhomTheoSanPham` (gom theo sản phẩm) hoặc `...NhomTheoNguonDuLieu` (gom theo nguồn) → build lại cùng bộ SP → xuất `.xlsx`.

## Case biên
- **Hết slot đọc đồng thời:** `ReleaseLockedTicket` trả null → báo "Lỗi ReleaseLockedTicket Step1/Step2" → user phải thử lại sau.
- **Không có quyền multi-user MKT:** check fail → trả lỗi xác thực; user không thấy số liệu user/nhóm khác.
- **Khoảng ngày vượt giới hạn:** `CheckCauHinhKhoangThoiGianChoPhepTimKiem ≠ -1` → chặn tìm.
- **Tác nghiệp xếp hạng:** các tác nghiệp ở mức xếp hạng (GetDropdownDMTacNghiepByXepHang(3)) bị loại khỏi thống kê khi không có cấu hình → tránh đếm trùng.
- **Xuất Excel quá tải:** dịch vụ trả `Service_Unavailable` → HTTP **503** (FE báo "chưa xuất được"); lỗi khác → 500.
- **Báo cáo lead theo nguồn — phân trang in-memory:** vì merge 4-5 SP rồi phân trang trên API, tổng số dòng phải đếm sau merge → khoảng ngày rộng tốn RAM/CPU, dễ chậm.
- **Chi tiết campaign không gắn key:** endpoint chi tiết campaign + xuất Excel chỉ `[SBAuthorize]` (login), không permission key riêng → ai login + có tiện ích báo cáo đều gọi được (xem dev).

## Ví dụ cụ thể có số
Tháng 6, đơn vị X chi **30.000.000đ** quảng cáo cho 2 nguồn landing:
- Lọc tháng 6, báo cáo lead theo nguồn trả:
  - Nguồn "Landing A": **300 lead**, 210 contact, tỷ lệ chốt 18% → 38 đơn, doanh số **76.000.000đ**, ngân sách 18.000.000đ.
  - Nguồn "Landing B": **120 lead**, 80 contact, tỷ lệ chốt 10% → 8 đơn, doanh số 12.000.000đ, ngân sách 12.000.000đ.
- Sắp xếp theo doanh số → Landing A lên đầu. Quản lý kết luận Landing B kém hiệu quả (chi phí/lead cao, chốt thấp) → giảm ngân sách B.
- Drill-down chi tiết campaign của Landing A: 2 campaign con, campaign "Hè giảm giá" đóng góp 30/38 đơn.
- Báo cáo MKT theo sản phẩm tháng 6: sản phẩm "Combo X" có **250 lead**, 40 đơn; xuất Excel gom theo sản phẩm → mỗi dòng một SP.
- Khi 6 người cùng mở dashboard và giới hạn là 5 slot → người thứ 6 bị báo bận, thử lại sau khi có người đóng.

## Hậu điều kiện
- Toàn bộ **read-only** — không ghi/sửa dữ liệu, không bắn message/event.
- Mở báo cáo **chiếm 1 slot đọc** trong thời gian xem (giải phóng sau).
- Có thể ghi log query (chậm) khi user là super/CSKH hoặc bật cấu hình show query.
