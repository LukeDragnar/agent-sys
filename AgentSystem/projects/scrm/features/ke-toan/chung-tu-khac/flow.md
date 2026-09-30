# Luồng — Chứng từ khác (Chứng từ nghiệp vụ khác)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Để xem danh sách: cần quyền `ChungTuKhac_TimTheoDieuKien`. Lập / chi tiết / xóa / xuất Excel chỉ cần đăng nhập.
- Đã có danh mục tài khoản kế toán (TkNo/TkCo) và (nếu cần đẩy MISA) cấu hình kết nối kế toán.

## Luồng dùng — Lập chứng từ
1. Mở **Kế toán → Chứng từ nghiệp vụ khác**, bấm lập chứng từ.
2. Nhập số chứng từ (hoặc để trống), ngày chứng từ, diễn giải; thêm ≥ 1 dòng hạch toán (mỗi dòng: TkNo, TkCo, đối tượng nợ/có, số tiền).
3. Lưu (`InsertChungTu`):
   - `ValidateRequest`. Số chứng từ trống → tự sinh (`SinhMaPhieu(PhieuNghiepVuKhac)`); có giá trị → chống trùng (đơn vị × chi nhánh × số chứng từ).
   - Tạo `ChungTuKhac` + N `ChungTuKhacChiTiet` trong một giao dịch (EF transaction).
4. Sau khi lưu xong (commit):
   - Với mỗi chi tiết → đồng bộ đối tượng Nợ & Có sang MISA (`CheckDongBoDoiTuong`).
   - Nếu tìm thấy `CauHinhKetNoiKeToan` mặc định của chi nhánh (fallback đơn vị) → insert `DataReceiveTaoPhieu(Status=Received)` (vào hàng đợi đẩy MISA).
   - Sinh bút toán `ButToan_PhieuChungTuKhac_Save`.

## Luồng dùng — Xem / xóa / xuất
1. **Danh sách** (`TimTheoDieuKien`): non-admin ép `IdChiNhanh = chi nhánh user`. Lọc đơn vị + CN + keyword (SoChungTu/DienGiai) + khoảng ngày `NgayChungTu` + trạng thái đồng bộ. Trả kèm cờ `StatusSendPartner`, `SinhButToanSuccess`, `SinhButToanMessErr`.
2. **Chi tiết** (`ViewChiTiet`): lấy phiếu + chi tiết theo id + đơn vị.
3. **Xóa** (`XoaPhieu`): tìm theo id + đơn vị + CN; nếu `IdHoaDon` có giá trị (chứng từ tạo tự động từ màn thanh toán) → chặn; hợp lệ → ghi history + xóa qua `PhieuKeToanHistoryService.DeletePhieuKeToan`.
4. **Xuất Excel** (`ExportExcelChungTuKhac`): lọc giống danh sách, build file; rỗng → NotFound.

## Các trường hợp / case biên
- **Lập chứng từ không có dòng nào / dòng không hợp lệ:** `ValidateRequest` chặn.
- **Số chứng từ trùng (nhập tay):** chặn theo (đơn vị × chi nhánh × số chứng từ).
- **Đơn vị không có cấu hình kết nối kế toán:** chứng từ vẫn lưu + sinh bút toán, nhưng KHÔNG tự vào hàng đợi đẩy MISA (đẩy thủ công sau).
- **Xóa chứng từ tạo tự động (`IdHoaDon != null`):** bị chặn "Phiếu được tạo tự động, không thể xóa".
- **Thông báo xóa sai ngữ cảnh:** validate trả "Id phiếu bù trừ không hợp lệ" (copy nhầm từ BuTru) — chỉ ảnh hưởng text (lỗi đã biết).
- **Xuất Excel rỗng:** NotFound (không có dữ liệu).

## Ví dụ cụ thể có số
Kế toán cần kết chuyển 1.000.000đ giữa hai tài khoản:
- Lập chứng từ, để trống số → hệ thống sinh số tự động.
- Dòng 1: TkNo = 642, TkCo = 111, số tiền 1.000.000đ.
- Lưu → tạo `ChungTuKhac` + 1 chi tiết → đồng bộ đối tượng MISA → (nếu có cấu hình kết nối) đưa vào hàng đợi đẩy MISA → sinh bút toán Nợ 642 / Có 111 = 1.000.000đ.
- Lập thêm chứng từ với cùng số vừa nhập tay → chặn trùng.

## Hậu điều kiện
- **Lập:** chứng từ + chi tiết lưu DB; đối tượng MISA đồng bộ; (có cấu hình) hàng đợi đẩy MISA; bút toán sinh.
- **Xóa:** ghi lịch sử + xóa chứng từ (chỉ với chứng từ lập tay không gắn hóa đơn).
- **Danh sách / chi tiết / xuất Excel:** read-only.
