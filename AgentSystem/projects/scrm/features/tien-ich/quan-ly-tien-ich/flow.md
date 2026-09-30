# Luồng — Quản lý tiện ích

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId).
- Để **mua/xóa** tiện ích: phải có role **DV-AD-TONG** (mua thêm có HT-KT); xem danh sách đã mua: DV-AD-TONG/CN/MCN, DV-QLDV, HT-KT.
- Danh mục tiện ích đã được quản trị hệ thống (Admin SaaS) khai báo sẵn (tên, mô tả, giá, nhóm, mặc định/miễn phí/trả phí).
- Có cấu hình tài khoản ngân hàng hệ thống (`HT_TK_NGANHANG`) để sinh QR; nếu chưa cấu hình, hệ thống dùng tài khoản mặc định cứng trong code.

## Luồng dùng (đơn vị mua tiện ích)
1. Người dùng mở **Cài đặt → Đăng ký tiện ích** (`/caidat/dangky-tien-ich`).
2. Hệ thống gọi `GetDanhSachTienIchFilter` → trả tiện ích gom theo nhóm; mỗi tiện ích có cờ `isMacDinh` / `isFree` / `isTraPhi`, `isDaMua` (đã mua), `isChoDuyet` (chờ duyệt), `ngayHetHan`.
3. Người dùng lọc theo nhóm (component `app-filter-nhom-tienich`) hoặc xem tất cả.
4. Bấm chọn tiện ích chưa mua (thẻ tô sáng `isSelect=true`). Tiện ích `isChoDuyet=true` bị khóa chọn.
5. Bấm **"Mua tiện ích"** → mở popup **Thanh toán tiện ích** (`ChonMuaTienIchComponent`): nhập số lượng, xem tổng tiền (miễn phí mặc định số lượng 999, trả phí mặc định 1).
6. Bấm thanh toán → gọi `MuaNgay`. Backend tách 2 loại:
   - **Tiện ích giá = 0 (miễn phí/mặc định):** tạo hợp đồng + **tự động duyệt** ngay (`AutoDuyetHopDong`). Nếu giỏ chỉ toàn miễn phí → trả `Code="02"` → FE báo "Mua tiện ích thành công" và đóng popup.
   - **Tiện ích trả phí (giá > 0):** tạo hợp đồng trạng thái **ChoDuyet** + trả `Code="01"` kèm thông tin ngân hàng/mã giao dịch → FE chuyển bước hiển thị **QR/chuyển khoản**, báo "Gửi yêu cầu mua tiện ích thành công!".
7. Popup đóng → màn chợ load lại danh sách.

## Luồng xóa tiện ích đã đăng ký
1. Tại thẻ tiện ích **đang dùng**, bấm icon thùng rác → xác nhận.
2. Gọi `XoaTiDaDangKy/{idDonViTienIch}` → kiểm tra: bản ghi DonViTienIch thuộc đúng đơn vị; tiện ích tồn tại; **nếu `IsMacDinh=true && IsAllowDelete!=true` → chặn** ("Không được xóa tiện ích mặc định!").
3. Hợp lệ → xóa → load lại danh sách.

## Case biên
- **Giỏ rỗng khi bấm Mua** → FE chặn: "Vui lòng chọn ít nhất một tiện ích để mua".
- **Backend nhận list rỗng** → "Bạn phải mua ít nhất một tiện ích".
- **Số lượng ≤ 0** → FE tự ép về 1; backend: SoLuong < 0 → lỗi "phải >= 1".
- **Số lượng > 100 và giá > 0** → lỗi "phải < 100" (chỉ áp cho tiện ích trả phí).
- **Tiện ích không tồn tại trong DB** → lỗi "Tiện ích ... không tồn tại".
- **Giỏ trộn cả miễn phí + trả phí** → miễn phí được auto-duyệt, trả phí vào hợp đồng chờ duyệt; chỉ trả `Code="02"` (dừng) khi **không có** tiện ích trả phí nào.
- **Xóa tiện ích mặc định bị khóa** → "Không được xóa tiện ích mặc định!".
- **Xóa bản ghi không thuộc đơn vị mình** → "Không tìm thấy data!".
- **Chưa cấu hình tài khoản ngân hàng hệ thống** → dùng tài khoản Techcombank mặc định cứng trong code (cần product xác nhận có muốn vậy không).

## Ví dụ cụ thể có số
Đơn vị X (Admin tổng anh Hùng) vào chợ tiện ích:
- Anh Hùng chọn 1 tiện ích **miễn phí** "Chat đa kênh" (giá 0) → bấm mua → backend auto-duyệt → trả `Code="02"` → báo "Mua tiện ích thành công", dùng được ngay.
- Anh Hùng chọn tiện ích **trả phí** "Tổng đài IP" giá 500.000đ, số lượng 2 → tổng 1.000.000đ → bấm thanh toán → backend tạo hợp đồng mã `HD000123` trạng thái **ChoDuyet**, trả `Code="01"` + QR ngân hàng + nội dung "TT SandboxVN Ma HD000123". Anh Hùng chuyển khoản 1.000.000đ, chờ Admin SaaS duyệt → khi duyệt, tính năng Tổng đài IP mới bật.
- Anh Hùng thử mua "Tổng đài IP" số lượng 150 → backend chặn "Số lượng mua tiện ích 'Tổng đài IP' phải < 100".
- Nhân viên Sale (không phải DV-AD-TONG) vào màn này → bị chặn quyền mua (`TienIch_MuaNgay` yêu cầu DV-AD-TONG/HT-KT).

## Hậu điều kiện
- Mỗi lần mua tạo **HopDongTienIch** (+ chi tiết). Miễn phí: trạng thái đã duyệt (auto). Trả phí: trạng thái **ChoDuyet** → vào hàng chờ duyệt của quản trị hệ thống.
- Khi hợp đồng được duyệt → tiện ích kích hoạt cho đơn vị → các controller gắn `[TienIch("<mã>")]` cho phép đơn vị truy cập.
- Xóa tiện ích đã đăng ký → thu hồi quyền dùng tính năng tương ứng.
- Mọi giao dịch mua đều xem được ở màn **Lịch sử mua tiện ích**.
