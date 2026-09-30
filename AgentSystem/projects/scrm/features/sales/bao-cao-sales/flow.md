# Luồng — Báo cáo Sales

## Tiền điều kiện
- Người dùng đã đăng nhập, có quyền xem báo cáo sale (key `BaoCaoOnlineController_*` / `ContactReportController_BaoCaoChiaSo`).
- Dữ liệu nghiệp vụ đã phát sinh (contact đã chia/tác nghiệp, đơn đã chốt, cuộc gọi đã ghi) — báo cáo chỉ đọc lại.
- Đơn vị có thể đặt **giới hạn khoảng ngày** tra cứu.

## Luồng dùng (user)
1. Mở **Báo cáo** → chọn báo cáo sale.
2. Đặt **bộ lọc**: chi nhánh, Từ/Đến ngày, cơ cấu tổ chức (phòng ban/nhóm/user sale), sản phẩm/nguồn (tùy báo cáo).
3. Hệ thống **validate khoảng ngày** (`CheckCauHinhKhoangThoiGianChoPhepTimKiem`); nếu user thường + chưa chọn chi nhánh → **ép chi nhánh user**; lọc cơ cấu tổ chức theo quyền (`GetCheckPermissionViewDropdownListNhomVaUser`).
4. Chạy **stored proc** tương ứng (DB SCRM_Report) → trả lưới số liệu.
5. (Tùy báo cáo) bấm **Xuất Excel** → endpoint Export build lại cùng SP → file .xlsx.

## Các báo cáo sale chính
- **Chia số contact theo ngày** (`ContactReport/BaoCaoChiaSo` → SP `PRC_SCRM_ReportBaoCaoChiaSo`).
- **Công việc sale** (`BaoCaoOnline/ReportCongViecSale` + Export).
- **Tối ưu sale** (`ReportToiUuSale` + enrich SP cuộc gọi MyHub & Omicall).
- **Bảng xếp hạng sale** (`ReportBangXepHangSale` — ép LoaiNhanVien=1, dùng chung SP với MKT).
- **Phong thần bảng sale / nhóm sale** (`ReportPhongThanBangSale` / `...NhomSale` — mở connection riêng).
- **Tỉ lệ chốt đơn sản phẩm** (`ReportTyLeChotDonSanPham` + SP số contact).
- **KH cũ/mới** (`ReportKhachHangCuMoi`), **theo nguồn landing** (`ReportSaleTheoNguonLanding`).
- **Doanh số tổng quan / theo ngày / theo giao vận** (`ReportDoanhSoTongQuan`...).

## Các trường hợp / case biên
- **Khoảng ngày vượt giới hạn** → chặn "Chỉ được xem dữ liệu trong khoảng N ngày" (khi cấu hình ≠ -1).
- **User thường chưa chọn chi nhánh** → tự ép chi nhánh của user (không phải admin/super/test/CSKH).
- **User ngoài quyền xem nhóm/nhân viên** → lọc cơ cấu tổ chức cắt bớt → thấy ít dòng hơn.
- **Báo cáo theo ngày (`ReportDoanhSoTheoNgay`)** dùng `[SBAuthorize]` không key riêng (key cũ bị comment) → chỉ chặn theo auth chung.
- **Export khi service bận** → 503 (`Service_Unavailable`); lỗi khác → 500. Riêng **Export Combo SP** trả `BadRequest` (khác chuẩn), **Export Combo (thường)** build từ dữ liệu FE gửi lên (không re-query SP).
- **Widget đếm đơn/contact theo đơn vị** (`GetCountDonHangLogisticByDonVi`, `GetCountContactByDonVi`) là `[AllowAnonymous]` — service khác gọi, không phải UI sale.

## Ví dụ cụ thể có số
Quản lý sale xem báo cáo chia số tháng 6 (đơn vị giới hạn xem tối đa 90 ngày).
- Chọn Từ 01/06 Đến 30/06 (30 ngày < 90) → OK. Lưới hiện: An được chia 100 số, Bình 120, Chi 90 → tổng 310.
- Chuyển sang **Tỉ lệ chốt đơn sản phẩm** cùng kỳ: An 100 contact → 8 chốt (8%), Bình 120 → 6 chốt (5%).
- Mở **Tối ưu sale**: ghép số cuộc gọi tổng đài — An 220 cuộc/100 contact (2,2 cuộc/contact).
- Bấm Xuất Excel "Công việc sale" → file .xlsx. Nếu service xuất bận → 503, thử lại.
- Thử chọn Từ 01/01 Đến 30/06 (>90 ngày) → bị chặn "Chỉ được xem dữ liệu trong khoảng 90 ngày".

## Hậu điều kiện
- Báo cáo **read-only**: không sửa dữ liệu, **không bắn event/ripple** sang module khác.
- File Excel sinh ra phía client (không lưu server).
- Số liệu phản ánh dữ liệu gốc tại thời điểm chạy SP; sai số liệu → kiểm tra dữ liệu nguồn (contact/đơn/cuộc gọi).
