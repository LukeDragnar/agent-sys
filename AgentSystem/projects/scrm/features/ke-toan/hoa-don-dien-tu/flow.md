# Hóa đơn điện tử — Luồng

## Tiền điều kiện
- Đơn vị đã **bật tiện ích** hóa đơn điện tử: MISA (`TI_Invocie_Misa`) và/hoặc Viettel (`TI_Invocie_Viettel`). Không bật → mọi thao tác phát hành/tra cứu bị chặn ("Bạn không có quyền sử dụng tiện ích này").
- Đã khai báo **tài khoản nhà cung cấp** (MISA hoặc Viettel) và kết nối thành công.
- Đã **đồng bộ mẫu hóa đơn** từ nhà cung cấp và **đặt 1 mẫu mặc định** (mẫu mặc định được dùng khi phát hành).
- Có đơn hàng (cửa hàng/POS hoặc đơn online logistic) làm nguồn dữ liệu xuất hóa đơn; thông tin xuất hóa đơn/thuế của khách hợp lệ.

## Luồng cấu hình (admin/kế toán — Cấu hình HĐĐT)
1. Vào Cấu hình HĐĐT, chọn tab nhà cung cấp (MeInvoice hoặc Viettel).
2. **Thêm/Sửa tài khoản:** nhập thông tin kết nối → lưu. Controller ép loại nhà cung cấp đúng theo tab (MISA luôn ép `MeInovice`; Viettel ép khi xóa, nhưng InsertOrUpdate fallback MISA nếu thiếu — xem case biên).
3. **Đồng bộ mẫu:** gọi sang API nhà cung cấp, kéo danh sách mẫu, upsert vào hệ thống.
4. **Đặt mẫu mặc định:** bật cờ mặc định cho 1 mẫu của tài khoản đó.
5. (Tùy chọn) **Ánh xạ trường tùy biến / ký hiệu / seri** (Viettel có thêm template code, ký hiệu, seri) và **Xem trước**.

## Luồng dùng (kế toán/thu ngân — Phát hành)
1. Mở đơn → bấm **Phát hành HĐĐT** (hoặc Xuất hóa đơn).
2. Hệ thống validate điều kiện đơn + tiện ích, rồi **publish message lên hàng đợi (RabbitMQ)** (có retry 3 lần, delay 1 giây). Trả ngay câu **"Đã nhận yêu cầu xuất hóa đơn điện tử"** — KHÔNG có kết quả phát hành ngay.
3. **Consumer** lấy message, gọi sang nhà cung cấp đúng loại (`TypeInvoice`): MISA → queue MISA, Viettel → queue Viettel. Nhà cung cấp ký + phát hành.
4. Consumer ghi kết quả vào trạng thái hóa đơn của đơn (`InvoiceStatus`) + lịch sử (`HoaDonDienTuHistory` trên MongoDB), rồi (tùy cấu hình) đẩy SMS/ZNS báo khách.
5. FE **poll lại** (mở popup HĐĐT của đơn) để xem trạng thái: đang phát hành / thành công (có số HĐ) / thất bại.
6. Khi thành công → bấm **Tải PDF** (stream byte từ nhà cung cấp).

**Luồng nháp → phát hành:**
1. Bấm **Tạo hóa đơn nháp** (chỉ MISA + Viettel) → ghi trạng thái nháp `Draft`.
2. Kế toán soát nháp.
3. Bấm **Đồng bộ nháp → phát hành**: orchestrator tách nhánh theo nhà cung cấp ghi trên `InvoiceStatus` (KHÔNG theo payload client), gộp kết quả → consumer phát hành → trạng thái nháp `Created`.

## Các trường hợp / case biên
- **Chưa bật tiện ích** → mọi action UI trả lỗi "không có quyền sử dụng tiện ích". Riêng action Xuất Excel trả mã 400 (BadRequest) thay vì body lỗi chuẩn — khác convention.
- **Phiếu xuất kho + nhà cung cấp Viettel** → `CreateInvoice` chặn: "Phiếu xuất kho chỉ hỗ trợ phát hành hóa đơn qua MeInvoice".
- **Tạo nháp với nhà cung cấp ≠ MISA/Viettel** → "Nhà cung cấp hóa đơn không hỗ trợ tạo nháp!".
- **Publish hàng đợi thất bại cả 3 lần retry** → hệ thống **vẫn trả "Đã nhận yêu cầu…"** (không báo lỗi cho người dùng) → đơn có thể "tưởng đã gửi" mà thực chưa publish (xem dev.md — bug-prone).
- **Đồng bộ nháp khi MISA lỗi** → KHÔNG return sớm: vẫn giữ kết quả Viettel đã ghi DB, gắn lỗi riêng từng đơn rồi trả gộp.
- **Đơn chưa có trạng thái hóa đơn** khi đồng bộ nháp → rơi vào nhánh MISA, báo "chưa có HĐ".
- **Tài khoản Viettel thiếu loại nhà cung cấp** → InsertOrUpdate fallback gán MISA → tài khoản "lạc nhà cung cấp".
- **Tải PDF khi đơn chưa có số HĐ** → không tìm thấy lịch sử hóa đơn cho đơn.

## Ví dụ cụ thể có số
Shop B bật cả tiện ích MISA và Viettel, có 1 tài khoản MISA (mẫu mặc định M1) và 1 tài khoản Viettel (mẫu mặc định V1).
- Thu ngân bán đơn `DH-1001` (loại đơn thường, gắn nhà cung cấp Viettel) → bấm Phát hành → hệ thống đẩy lên queue Viettel, trả "Đã nhận yêu cầu". Sau ~3 giây, mở lại popup: trạng thái "Phát hành thành công", số HĐ `00001234`. Bấm Tải → ra file PDF.
- Kế toán xuất HĐ cho **phiếu xuất kho** `PXK-77` chọn nhầm Viettel → hệ thống chặn: "Phiếu xuất kho chỉ hỗ trợ phát hành hóa đơn qua MeInvoice". Đổi sang MISA → phát hành OK.
- Admin tạo tài khoản ở tab Viettel nhưng FE không truyền loại nhà cung cấp → tài khoản lưu xong hiện loại = MISA → admin xóa và tạo lại.
- Kế toán tạo 5 nháp MISA, soát 4 hợp lệ + 1 sai thông tin thuế → đồng bộ nháp → 4 đơn phát hành OK, 1 đơn trả lỗi riêng, không ảnh hưởng 4 đơn còn lại.

## Hậu điều kiện
- **Phát hành thành công:** đơn có số hóa đơn; ghi `InvoiceStatus` (SQL) + append `HoaDonDienTuHistory` (Mongo); (tùy cấu hình) đẩy SMS/ZNS cho khách.
- **Cấu hình tài khoản/mẫu:** lưu DB; mẫu mặc định mới sẽ được áp cho lần phát hành sau.
- **Tra cứu/Xuất Excel:** read-only, không thay đổi dữ liệu hóa đơn.
