# Luồng — Quản lý đơn đặt hàng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId) + chi nhánh (IdChiNhanh).
- Có nhà cung cấp, sản phẩm, kho/chi nhánh đã khai báo.
- Có quyền tạo đơn tại chi nhánh của mình (cấu hình từ module Kho/WarehouseProduct). Pha duyệt cần quyền duyệt; pha xử lý mua cần quyền tạo.
- Đơn vị cấu hình `CanKiemDuyet` quyết định có bước duyệt hay không.

## Luồng dùng (user)
1. Người dùng mở **Kho → Quản lý đơn đặt hàng**, bấm **Thêm mới**.
2. Chọn nhà cung cấp, chi nhánh nhận; thêm các dòng sản phẩm (SL > 0). Hệ thống validate NCC + chi nhánh + ≥1 SP, kiểm tra quyền tạo theo chi nhánh.
3. Sinh mã đơn nếu để trống; nếu gửi ở trạng thái Chờ duyệt thì validate quyền duyệt. Nếu `CanKiemDuyet=false` → Chờ duyệt tự chuyển Chờ xử lý. Insert đơn + dòng SP trong 1 transaction.
4. (Pha duyệt) Người có quyền duyệt mở đơn → **Duyệt** (Đã duyệt → auto Chờ xử lý), **Không duyệt** (terminal), hoặc **Trình lại** (về Chờ duyệt qua người tạo sửa).
5. (Pha xử lý) Người mua hàng chuyển **Đang xử lý**, bấm **Tạo phiếu mua hàng** → sinh phiếu mua (loại MuaHangVaoKho, trạng thái Đang xử lý) và set cờ `DaTaoPhieuMuaHang`.
6. Phiếu mua hàng nhận hàng về → cập nhật `SoLuongDaNhan` ngược về đơn; khi mọi SP nhận đủ → đơn **tự Hoàn thành** (terminal).
7. Có thể **Tạm dừng xử lý** (Đang xử lý ↔ Tạm dừng) hoặc **Hủy** đơn.

## Luồng cấu hình (admin)
- Bật/tắt duyệt: cấu hình `CanKiemDuyet` của đơn vị.
- Cấu hình quyền tạo đơn theo chi nhánh: lấy từ module WarehouseProduct.

## Các trường hợp / case biên
- **State machine** (`KiemTraLuongChuyenTrangThai`): Nhap→ChoDuyet; ChoDuyet→{DaDuyet(auto→ChoXuLy) | KhongDuyet | TrinhLai}; TrinhLai→ChoDuyet; ChoXuLy→DangXuLy; DangXuLy→{DaHoanThanh | TamDungXuLy}; TamDungXuLy→DangXuLy; mọi trạng thái non-terminal →DaHuy. Chuyển sai luồng → bị chặn.
- **Đã duyệt không bao giờ persist** → luôn auto chuyển Chờ xử lý.
- **Sửa**: chỉ {Nhap, TrinhLai} — sửa là replace toàn bộ dòng SP (xóa cũ → insert mới, transaction). **Xóa**: chỉ Nhap, hard delete đơn + dòng SP.
- **Thiếu quyền pha**: chuyển trạng thái pha duyệt mà không có quyền duyệt → từ chối; pha xử lý mà không có quyền tạo → từ chối.
- **Hủy kèm phiếu mua hàng** (`HuyPhieuMuaHangLienQuan=true`): cascade hủy mọi phiếu mua hàng chưa terminal. Cascade **không atomic** — nếu hủy phiếu A xong rồi phiếu B lỗi thì A đã hủy nhưng đơn chưa về Đã hủy → trạng thái lệch (xem dev).
- **Nhận đủ hàng** → `KiemTraVaCapNhatHoanThanh` set Hoàn thành; nếu payload thiếu IdDonVi thì fallback theo userLogin.
- **Import**: claim batch race-safe (TokenXuLy, UPDATE TOP); dòng OK tạo đơn thật + xóa tmp, dòng lỗi giữ lại trong bảng tmp.

## Ví dụ cụ thể có số
Shop C đặt mua từ NCC "Công ty ABC", chi nhánh Kho Trung Tâm.
- Tạo đơn DDH-0001 gồm: Sản phẩm M ×100, Sản phẩm N ×50. Đơn vị bật duyệt → đơn ở **Chờ duyệt**.
- Quản lý bấm **Duyệt** → đơn auto sang **Chờ xử lý** (không dừng ở Đã duyệt).
- Người mua **Xử lý** → **Tạo phiếu mua hàng** PMH-0001 cho cả 2 SP (Đang xử lý), cờ `DaTaoPhieuMuaHang=true`.
- Hàng về đợt 1: nhận M ×100, N ×30 → `SoLuongDaNhan` cập nhật; N còn thiếu 20 → đơn **chưa** hoàn thành.
- Đợt 2 nhận N ×20 → đủ cả 2 SP → đơn **tự Hoàn thành**.
- Trường hợp khác: đơn ở Nháp, người tạo bấm Xóa → xóa hẳn đơn + 2 dòng SP. Nếu đơn đã ở Chờ xử lý, nút Xóa không khả dụng (chỉ Hủy).

## Hậu điều kiện
- Tạo/Sửa/Xóa đơn ghi vào `DonDatHang` + `DonDatHangSanPham` (EF, transaction). Không pub/sub — đồng bộ qua internal service call (HTTP nội bộ).
- Tạo phiếu mua hàng sinh bản ghi PMH ở module Accounting, back-ref `IdDonDatHang`.
- Hủy đơn (kèm tùy chọn) cascade hủy phiếu mua hàng liên quan.
- Nhận đủ hàng → đơn về Hoàn thành (terminal).
