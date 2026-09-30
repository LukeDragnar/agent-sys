# Luồng — Pre-Order (đặt trước qua QR)

## Tiền điều kiện
- Đã khai báo khu vực + chỗ ngồi (bàn) và menu sản phẩm cho Pre-Order của chi nhánh.
- Admin đã gán mã QR cho bàn và in/dán tại bàn.
- Trang khách (quét QR) là **công khai** — không cần đăng nhập. Màn quản lý QR + danh sách đặt trước cần đăng nhập + quyền.

## Luồng cấu hình (admin — mã QR)
1. Mở **Cửa hàng → PreOrder**, chọn chi nhánh.
2. **Tạo/lấy QR** (`GetOrCreate`): tìm QR theo (đơn vị + chi nhánh + chỗ ngồi); có sẵn → trả bản cũ; chưa có → sinh `MaQR` unique 8 ký tự + enrich tên đơn vị/chi nhánh → insert. (Idempotent theo bàn.)
3. **Xem danh sách QR** (`GetByChiNhanh`) theo chi nhánh.
4. **Xóa QR** (`Delete/{id}`): xóa **cứng** theo (Id + đơn vị user).

## Luồng dùng (khách — quét QR đặt trước)
1. Khách quét QR → `public/GetSanPham?maQR=`: validate mã QR, resolve đơn vị/chi nhánh/chỗ ngồi, trả menu (nhóm SP, SP, đính kèm) + info chi nhánh.
2. Khách chọn món + (tùy chọn) tên/SĐT → `public/Save`:
   - Validate list SP không rỗng; **XOR tên/SĐT** (có tên ⇔ có SĐT, hoặc cả hai trống = ẩn danh).
   - Sinh `MaPreOrder` = `PO-` + 6 ký tự, claim qua Redis (TTL 5 phút), trùng thì thử lại (tối đa 10 lần).
   - Nếu có tên + SĐT → gọi Customer `FindOrCreatedCustomResult` (người tạo = "PREORDER") gắn IdKhachHang (lỗi gọi Customer **không** chặn lưu đơn).
   - Insert đơn `DonHangPre` **TrangThai=0 (chưa xử lý)** + từng SP + đính kèm; nhả claim Redis.
3. Khách nhận mã PreOrder; tra lại qua `public/GetByMaPreOrder?maPreOrder=`.

## Luồng dùng (admin — danh sách đặt trước)
1. `TimTheoDieuKien`: lọc theo đơn vị user + khoảng ngày; **role gating chi nhánh** — không phải DV-AD-TONG → ép `IdChiNhanh = chi nhánh user`; thêm lọc từ khóa (mã), trạng thái, chỗ ngồi; phân trang.
2. `GetByMaPreOrder` (UI): xem chi tiết theo mã.
3. `Delete`: xóa **mềm** (`IsDelete=true`) theo (Ids + đơn vị); không phải DV-AD-TONG → chỉ xóa đơn chi nhánh mình.

## Luồng nhập đơn đặt trước → đơn hàng (thu ngân, service-to-service)
1. Thu ngân ở POS nhập mã PreOrder → Order gọi `PrivateDonHangPre/GetFullByMaPreOrder(ma, idDonVi, idChiNhanh)`:
   - Filter chặt theo đơn vị + chi nhánh.
   - **Gating trạng thái**: nếu TrangThai=1 (đã tạo đơn) hoặc =2 (đã thanh toán) → "Mã PreOrder này đã được sử dụng!".
2. Tạo đơn hàng từ list SP đặt trước.
3. `MarkImported(ma, idDonHang, maDonHang)` → set `TrangThai=1` + gắn IdDonHang/MaDonHang → khóa đơn pre-order khỏi nhập lại + tránh job clear xóa.

## Trạng thái đơn đặt trước (TrangThai)
- `0` = chưa xử lý (khách mới đặt).
- `1` = đã nhập thành đơn hàng (MarkImported).
- `2` = đã thanh toán.
- Job `JobClearDonHangPreExpired` (cron ~2h sáng) chỉ xóa cứng đơn `TrangThai=0` quá hạn (`SoNgayLuuDonHangPre`, mặc định 30 ngày).

## Các trường hợp / case biên
- **QR rỗng / không tồn tại / đã xóa** → menu báo "Mã QR không hợp lệ!" / "Mã QR không tồn tại hoặc đã bị xóa!".
- **Đặt với list món rỗng** → "Danh sách sản phẩm không được trống!".
- **Nhập tên thiếu SĐT (hoặc ngược lại)** → "Vui lòng điền đầy đủ tên và số điện thoại!".
- **Gọi Customer lỗi khi đặt** → đơn vẫn lưu, chỉ thiếu IdKhachHang (không rollback).
- **Sinh mã PreOrder trùng > 10 lần** → ném lỗi (cực hiếm).
- **Nhập lại đơn đã dùng** (TrangThai 1/2) → chặn ở GetFull.
- **Xóa QR đang được dùng** → khách quét QR đó sau khi xóa sẽ báo mã không tồn tại (xóa cứng).
- **Sản phẩm đính kèm** → vừa là bản ghi đính kèm vừa là 1 dòng SP (IsSanPhamDinhKem=true); nếu đính kèm trùng SP đã có thì cộng dồn số lượng — lưu ý khi đếm/tính tiền tránh đếm trùng.

## Ví dụ cụ thể có số
Quán trà sữa, bàn T5 (chi nhánh CN1):
- Admin gán QR cho T5 → sinh mã QR 8 ký tự (vd `A1B2C3D4`), in dán bàn.
- Khách quét QR T5 → menu CN1; chọn 2 trà sữa (30.000đ) + topping trân châu (5.000đ, đính kèm) + nhập "Minh / 0901234567" → gửi → nhận mã `PO-7K2M9X`; hệ thống tạo/tìm khách "Minh" (NguoiTao=PREORDER).
- Đơn vào Danh sách đặt trước CN1 với TrangThai=0; quản lý CN2 (không phải AD-TONG) không thấy đơn này.
- Thu ngân CN1 nhập `PO-7K2M9X` ở POS → GetFull OK (TrangThai=0) → tạo đơn hàng 2 trà sữa + topping = 65.000đ → MarkImported → đơn pre-order chuyển TrangThai=1.
- Thu ngân nhập lại `PO-7K2M9X` → "Mã PreOrder này đã được sử dụng!".
- Một đơn khách đặt nhưng không ai xử lý, để TrangThai=0 quá 30 ngày → job dọn xóa cứng.

## Hậu điều kiện
- Khách: đơn `DonHangPre` TrangThai=0 được lưu (+ có thể sinh khách mới ở Customer).
- Sau khi nhập: đơn pre-order TrangThai=1 + liên kết IdDonHang/MaDonHang ↔ đơn bán thật; đơn được phục vụ/thanh toán ở luồng đơn hàng bình thường.
- Xóa QR: bản ghi QR mất, mã không còn resolve được.
