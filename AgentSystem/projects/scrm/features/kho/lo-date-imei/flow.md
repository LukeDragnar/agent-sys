# Luồng — Quản lý lô date & Imei

## Tiền điều kiện
- Sản phẩm đã được bật cờ phù hợp trong **Quản lý sản phẩm**:
  - `Sản phẩm quản lý IMEI` (IsSanPhamImei) → để theo dõi theo số seri.
  - `Sản phẩm quản lý lô date` (IsSanPhamLoDate) → để theo dõi theo lô/hạn sử dụng.
- Đã có **kho** và **chi nhánh** hợp lệ.
- Người dùng có quyền nhóm kho (DV-QL-KHO / DV-KT / DV-AD...).

---

## Luồng dùng (nghiệp vụ chính)

### A. Nhập kho hàng IMEI / lô date
1. Tạo **phiếu nhập** cho sản phẩm có cờ IMEI/lô date.
2. Với mỗi dòng sản phẩm:
   - **IMEI**: nhập tay từng mã IMEI, **hoặc** tick "sinh IMEI tự động" để hệ thống tạo mã.
   - **Lô date**: nhập lô sản xuất + ngày SX + hạn dùng, **hoặc** tick "sinh lô date tự động".
3. **Hoàn thành phiếu nhập** → hệ thống tạo bản ghi IMEI (trạng thái *Chờ nhập kho* → *Đã nhập kho*) / lô date tương ứng, tăng tồn kho.

### B. Tra cứu IMEI (màn Danh sách / Quản lý IMEI)
1. Mở màn IMEI, đặt bộ lọc: khoảng ngày (mặc định 7 ngày gần nhất), kho, nhóm/cha/con sản phẩm, nhà cung cấp, trạng thái IMEI, từ khóa.
2. Bấm tìm → lưới hiển thị: sản phẩm, mã SP, loại/mã phiếu, IMEI, kho, NCC, kiểu nhập, trạng thái nhập kho, trạng thái SP, ngày tạo, lô SX, số đăng ký, ngày SX, hạn dùng, tình trạng, giá bán.
3. Có thể: **quét/kiểm tra một IMEI** cụ thể, xem **lịch sử thao tác** của IMEI, in tem, xuất Excel.

### C. Báo cáo / Quản lý sản phẩm lô date
1. Mở màn Báo cáo SP lô date, chọn **kiểu ngày** (Ngày tạo / Ngày hết hạn SP), khoảng ngày, kho, trạng thái lô, từ khóa, sản phẩm.
2. Lưới hiển thị: mã lô date, số lượng (in), sản phẩm, đơn vị, số lượng tồn, kho, kiểu nhập, trạng thái nhập kho, ngày tạo, lô SX, số ĐK, ngày SX, hạn dùng.
3. Dùng để **ưu tiên bán lô sắp hết hạn**, in tem lô date.

### D. Xuất / chuyển kho
- Khi bán hoặc xuất: chọn đúng **IMEI** hoặc đúng **lô date** để trừ kho → IMEI chuyển trạng thái *Đã xuất kho*; lô date giảm tồn.
- Chuyển kho IMEI: IMEI ở trạng thái *Đang chuyển kho*, kho nhận **quét IMEI** để xác nhận.

---

## Luồng cấu hình

### Sinh IMEI tự động (màn "Sinh IMEI")
1. Bấm Thêm mới → nhập: **tiền tố** (số, tối đa 2 ký tự), số lượng, lô sản phẩm, số đăng ký, ngày SX, hạn dùng, giá bán.
2. Hệ thống sinh mã dạng `{tiền tố}{ddMMyy}{số thứ tự 3 chữ số tự tăng}`.
3. Chọn các mã → **In tem** (mã chuyển "Đã in"), hoặc **Xuất Excel gán sản phẩm**.

### Sinh lô date tự động (màn "Sinh lô date")
- Tương tự sinh IMEI: nhập tiền tố + thông tin lô (lô SX, số ĐK, ngày SX, hạn dùng, giá bán). Mã lô được sinh và có thể in.

### Cấu hình mẫu in
- Mỗi đơn vị có thể lưu cấu hình mẫu in tem (DsCauHinh / CapNhatCauHinhIn) để in IMEI/lô date theo bố cục riêng.

---

## Case biên
- **Tiền tố sai**: rỗng / có chữ / > 2 ký tự → chặn, báo lỗi.
- **Hạn dùng ≤ ngày SX** → chặn "Ngày hết hạn phải lớn hơn ngày sản xuất".
- **Lô sản xuất trùng** trong đơn vị → chặn "Lô sản xuất đã tồn tại" (trừ khi tick bỏ qua check trùng).
- **Lô sản phẩm rỗng** hoặc > 255 ký tự; **số đăng ký** > 255 ký tự → chặn.
- **Xóa lô date khi phiếu nhập đã hoàn thành** → chặn "Phiếu nhập đã hoàn thành. Không được xóa lô date".
- **Nhập kho IMEI không ở trạng thái "Chờ nhập kho"** → chặn "Imei không ở trạng thái chờ nhập kho".
- **IMEI không tồn tại** khi nhập kho/tra cứu → "Không tìm thấy imei".
- **IMEI chứa ký tự đặc biệt** (vd "/"): dùng tra cứu bản POST (KiemTraImeiV2) vì truyền qua URL bị lỗi.

---

## Ví dụ có số
- Sinh IMEI: tiền tố `88`, ngày hôm nay 20/06/2026 → mã sinh `88200626001`, `88200626002`, ... Số thứ tự tự tăng theo ngày, ≤ 999 thì pad 3 chữ số (`001`), > 999 thì để nguyên số (`1000`).
- Lô date: nhập 100 hộp mỹ phẩm lô `LOT-A`, ngày SX 01/01/2026, hạn 01/01/2028, giá bán 250.000đ → tạo 1 bản ghi lô date với số lượng 100; báo cáo lọc theo hạn dùng sẽ thấy lô này hết hạn 01/01/2028.
- Tra IMEI `88200626001`: kết quả trạng thái *Đã nhập kho*, kho "Kho HN", SP "Tai nghe X" → biết máy đang còn trong kho.

---

## Hậu điều kiện
- Sau nhập kho hoàn thành: tồn kho tăng, mỗi IMEI có 1 bản ghi trạng thái *Đã nhập kho*; mỗi lô date có thông tin hạn dùng tra cứu được.
- Sau xuất/bán: IMEI → *Đã xuất kho*; lô date giảm tồn.
- Mã đã in → trạng thái *Đã in*.
- Mọi thao tác IMEI được ghi vào **lịch sử thao tác IMEI** để truy vết.

## Link
→ [summarize.md](./summarize.md) · [faq.md](./faq.md) · Kỹ thuật: [./dev.md](./dev.md)
