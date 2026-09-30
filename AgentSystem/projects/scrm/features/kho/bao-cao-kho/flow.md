# Flow — Báo cáo kho

Mô tả luồng dùng chung cho cả nhóm báo cáo kho. Các loại báo cáo chỉ khác nhau ở bộ lọc và cột số liệu; khung xử lý giống nhau.

## Tiền điều kiện
- Người dùng đã đăng nhập, có 1 trong các role được phép (xem `quyen` trong summarize.md). Backend kiểm bằng `[SBAuthorize(...)]` trên từng endpoint.
- Đơn vị đã có dữ liệu kho: sản phẩm, kho, và các phiếu nhập/xuất hoặc đơn hàng phát sinh trong kỳ.
- Với báo cáo có khoảng thời gian: khoảng từ ngày – đến ngày phải nằm trong giới hạn cấu hình của đơn vị (`CheckCauHinhKhoangThoiGianChoPhepTimKiem`).
- Chưa bị chặn bởi cơ chế giới hạn truy cập đồng thời (`CheckGioiHanTruyCap` — trả về cho phép).

## Luồng dùng (chọn loại → lọc → xem/xuất)
1. **Chọn loại báo cáo** → mở màn tương ứng (nhập / nhập-xuất / tồn / xuất / xuất theo nhân viên / sản phẩm bán / sản phẩm bán theo ngày / quay vòng).
2. **Đặt bộ lọc:** chi nhánh, kho, nhóm sản phẩm, sản phẩm cha, sản phẩm, nhãn sản phẩm (ListIdNhanSanPham), từ khóa, khoảng thời gian, kiểu gộp (GroupType: theo sản phẩm hay theo kho–sản phẩm).
3. **Bấm xem** → FE gọi API tương ứng (POST tới `WarehouseReport` hoặc `OrderReport`).
4. **Backend xử lý (chung):**
   a. Kiểm giới hạn truy cập đồng thời → nếu chưa tới lượt, trả statusCode 250 "Chưa đến lượt truy cập".
   b. Kiểm khoảng thời gian cho phép → vượt giới hạn thì trả lỗi.
   c. Nếu lọc theo nhãn sản phẩm: gọi SP `PRC_SCRM_WarehouseProduct_NhanSanPham` lấy danh sách IdSanPham trước.
   d. Ép quyền chi nhánh: user không phải admin/super/test → IdChiNhanh bị thay bằng chi nhánh của user.
   e. Gọi stored procedure chính (xem dev.md), nhận về nhiều bảng: **dữ liệu chi tiết + dòng tổng (summary)** (báo cáo quay vòng có thêm bảng total).
   f. Làm giàu dữ liệu: gắn tên nhóm sản phẩm, tên đơn vị tính hiện tại (quy đổi theo đơn vị lô-date hoặc đơn vị nhỏ nhất).
   g. Trả về kèm tổng số bản ghi để FE phân trang.
5. **Xem kết quả:** bảng chi tiết + dòng tổng, phân trang, sắp xếp theo cột (OrderBy/Dir).
6. **Xuất Excel (tùy chọn):** bấm nút xuất → gọi endpoint Export tương ứng → nhận file `.xlsx` (XuatNhapKho.xlsx / Baocaotonkho.xlsx / SanPhamTheoNgay.xlsx…).

## Case biên
- **Vượt khoảng thời gian cho phép:** trả lỗi "Chỉ được xem dữ liệu trong khoảng 'N ngày'", không có dữ liệu.
- **Đang quá tải:** trả "Chưa đến lượt truy cập" (statusCode 250) — không phải lỗi, chờ và thử lại.
- **Lọc nhãn sản phẩm không khớp sản phẩm nào:** `strListIdSanPhambyNhanSP = null` → SP coi như không lọc theo nhãn (cẩn thận: không phải "ra rỗng").
- **Không có bản ghi summary:** trả lỗi "Không có bản ghi nào!".
- **Tồn kho:** không kiểm khoảng thời gian (đã comment out) — luôn xem tại thời điểm hiện tại.
- **User không phải admin chọn chi nhánh khác:** bị ép về chi nhánh của chính user.

## Ví dụ có số
Báo cáo nhập–xuất kho, kho A, sản phẩm "Sữa rửa mặt", kỳ 01/06–30/06, GroupType = 1 (theo sản phẩm):
- Tồn đầu kỳ (SoLuongNhapDauKy): 120
- Tổng nhập trong kỳ (TongNhap): 200, giá nhập bình quân 45.000đ
- Tổng xuất trong kỳ (TongXuat): 260
- Tồn cuối kỳ (SoLuongNhapCuoiKy) = 120 + 200 − 260 = **60**
- Dòng tổng (summary) cộng tất cả sản phẩm trong trang lọc; TotalRows dùng để phân trang.

Báo cáo quay vòng: nếu TongXuat = 260 và TonKhoTrungBinh = 65 → VongQuaySanPham ≈ 4 vòng trong kỳ; sản phẩm được phân loại "bán chạy".

## Hậu điều kiện
- **Không thay đổi dữ liệu** — báo cáo chỉ đọc, không tạo/sửa phiếu hay tồn.
- Có thể ghi log câu truy vấn (LogCommandV2) khi user là super/CSKH hoặc đơn vị bật chế độ show query — phục vụ debug, không ảnh hưởng nghiệp vụ.
- Lượt truy cập đồng thời được giải phóng ở khối `finally` (ReleaseLockedTicket) để người sau dùng được.
