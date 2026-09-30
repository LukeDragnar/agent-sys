---
alias: ["đặt trước", "pre-order", "QR đặt món", "mã QR bàn", "khách quét QR gọi món", "đặt trước qua QR", "danh sách đặt trước", "gọi món tại bàn"]
module: cua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — quản lý QR & danh sách đặt trước có phân quyền riêng; danh sách/xóa đặt trước giới hạn theo chi nhánh (chỉ admin tổng đơn vị DV-AD-TONG xem/xóa được mọi chi nhánh). Trang khách quét QR là công khai (không cần đăng nhập)."
phu_thuoc: ["Khu vực & Chỗ ngồi / Bàn (gắn QR)", "Sản phẩm cho Pre-Order (menu khách thấy)", "Khách hàng (tự tạo/tìm khi khách nhập tên+SĐT)", "Đơn hàng / POS thu ngân (nhập đơn đặt trước thành đơn thật)"]
link_flow: ./flow.md
---

# Pre-Order (đặt trước qua mã QR)

## Là gì
Pre-Order cho phép **khách tự quét mã QR dán ở bàn** để **xem menu và gọi/đặt món trước** mà không cần đăng nhập. Mỗi bàn/chỗ ngồi có một mã QR riêng. Khách quét QR → xem menu chi nhánh → chọn món → (tùy chọn) nhập tên + số điện thoại → gửi đơn đặt trước. Đơn này hiện trong **Danh sách đặt trước** để thu ngân/admin xử lý và **nhập thành đơn hàng thật** ở màn bán hàng.

Tính năng gồm các phần:
- **Quản lý mã QR** (gắn QR cho từng bàn, xem danh sách, xóa QR).
- **Trang khách quét QR** (menu + gửi đơn — công khai).
- **Danh sách đặt trước** (admin xem/lọc/xóa đơn khách đã đặt).
- **Nhập đơn đặt trước vào đơn hàng** (thu ngân kéo đơn pre-order thành đơn bán thật).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tạo / lấy mã QR cho một bàn | ✅ | quyền QR (idempotent theo đơn vị+chi nhánh+chỗ ngồi) |
| Xem danh sách mã QR theo chi nhánh | ✅ | quyền QR |
| Xóa mã QR | ⚠️ | xóa **cứng** — QR đã in/dán sẽ chết ngay, không khôi phục |
| (Khách) Quét QR xem menu | ✅ | công khai, không cần đăng nhập |
| (Khách) Gửi đơn đặt trước | ✅ | công khai; chọn ≥1 món; nếu nhập tên thì phải nhập cả SĐT (và ngược lại) |
| (Khách) Tra lại đơn theo mã PreOrder | ✅ | công khai, biết mã PO-xxxxxx |
| Xem danh sách đặt trước (admin) | ✅ | nếu không phải DV-AD-TONG → chỉ thấy chi nhánh của mình |
| Xem chi tiết đơn đặt trước | ✅ | quyền xem chi tiết |
| Xóa đơn đặt trước | ⚠️ | xóa **mềm**; không phải DV-AD-TONG → chỉ xóa được đơn chi nhánh mình |
| Nhập đơn đặt trước thành đơn hàng | ✅ | thu ngân ở POS; chỉ đơn **chưa xử lý** mới nhập được |

## Để làm gì / cho ai
- **Khách hàng:** tự gọi món tại bàn qua điện thoại, không cần chờ nhân viên; có thể đặt ẩn danh hoặc để lại tên + SĐT.
- **Thu ngân / nhân viên:** nhận sẵn đơn khách đặt, kéo vào đơn hàng để phục vụ/thanh toán nhanh.
- **Quản lý chi nhánh:** theo dõi danh sách đặt trước, dọn đơn rác.

## Cách dùng
**Thiết lập QR cho bàn (admin):**
1. Vào **Cửa hàng → PreOrder**, chọn chi nhánh.
2. Gán QR cho từng bàn/chỗ ngồi (tạo hoặc lấy mã QR sẵn có) → in QR ra dán tại bàn.

**Khách đặt trước:**
1. Khách quét QR ở bàn → trang menu của chi nhánh hiện ra.
2. Chọn món (kèm sản phẩm đính kèm nếu có), tùy chọn nhập tên + SĐT.
3. Gửi đơn → nhận **mã PreOrder (PO-xxxxxx)** để tra lại.

**Thu ngân xử lý:**
1. Vào **Danh sách đặt trước**, lọc theo ngày/trạng thái/bàn/từ khóa.
2. Mở chi tiết đơn; ở POS, nhập mã PreOrder để kéo món vào đơn hàng → tạo đơn bán thật → đơn pre-order chuyển sang "đã nhập".

## Cần chú ý
- **Xóa mã QR là xóa cứng.** QR đã in/dán ở bàn sẽ mất hiệu lực ngay; khách quét sẽ báo "mã đã bị xóa". Cân nhắc trước khi xóa.
- **Mỗi bàn chỉ nên có một QR** — hệ thống tạo-hoặc-lấy theo (đơn vị + chi nhánh + chỗ ngồi), tránh trùng.
- **Khách nhập tên phải nhập cả SĐT** (và ngược lại); để trống cả hai = đặt ẩn danh. Nếu có tên + SĐT, hệ thống tự tạo/tìm hồ sơ khách.
- **Đơn đặt trước đã nhập (hoặc đã thanh toán) không nhập lại được** — báo "Mã PreOrder này đã được sử dụng!".
- **Đơn đặt trước chưa xử lý quá hạn sẽ bị dọn tự động** (job định kỳ, mặc định sau ~30 ngày) — chỉ xóa đơn còn ở trạng thái chưa xử lý.
- **Trang khách là công khai**: ai biết mã PreOrder đều tra được đơn (lộ tên/SĐT). Hiện không còn giới hạn chống spam quét theo IP (xem `dev.md`).
- **Xem danh sách/xóa đặt trước bị giới hạn theo chi nhánh** nếu bạn không phải admin tổng đơn vị.

## Ảnh hưởng tới
- **Đơn hàng / POS thu ngân** — đơn đặt trước được nhập thành đơn bán thật (liên kết mã đơn).
- **Khách hàng** — có thể sinh hồ sơ khách mới khi khách để lại tên + SĐT (người tạo = "PREORDER").
- **Bàn / chỗ ngồi** — QR gắn theo bàn để resolve đúng menu + vị trí.
