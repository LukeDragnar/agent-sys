# Quản lý vận đơn — Luồng

> Tóm tắt: [summarize.md](./summarize.md) · Hỏi đáp: [faq.md](./faq.md) · Module: [../README.md](../README.md)

## Tiền điều kiện
- Đã **kết nối ít nhất 1 hãng vận chuyển** (xem "Kết nối đơn vị vận chuyển").
- Có đơn giao hàng (OrderLogistic) ở trạng thái chờ giao.
- Có kho và tồn kho cho sản phẩm trong đơn.
- (Cho lượt nhặt) đã cấu hình **số SKU tối đa mỗi phiếu nhặt** trong cấu hình vận đơn.

## Luồng dùng (nhân viên kho)

### A. Đăng đơn (Thủ kho tác nghiệp)
1. Vào màn **Đăng đơn**, lọc danh sách đơn theo trạng thái giao hàng / kho / ngày / số điện thoại.
2. Mở đơn → kiểm tra sản phẩm, soạn hàng (có thể quét IMEI / lô date nếu sản phẩm yêu cầu).
3. Bấm **đăng đơn** → hệ thống đẩy thông tin sang hãng vận chuyển → nhận về **mã vận đơn**.
4. **In phiếu giao**, dán lên kiện, bàn giao cho shipper.
5. Theo dõi đơn: trạng thái giao hàng cập nhật dần (đang giao, đã giao, hoàn...).
6. Khi cần: cập nhật trạng thái thủ công, ghi chú nội bộ, đổi ngày muốn giao, yêu cầu giao lại.

### B. Gom hàng theo lượt nhặt
1. Vào **Danh sách lượt nhặt** → tạo lượt mới, chọn nhiều đơn cần đi lấy hàng.
2. Hệ thống kiểm tra: đơn nào đã nằm trong lượt khác → chặn; số SKU vượt cấu hình → tách phiếu.
3. Hệ thống sinh **mã lượt nhặt** (dạng `LN` + ngày) và phiếu nhặt liệt kê sản phẩm **theo vị trí trong kho**.
4. In phiếu nhặt → nhân viên cầm đi gom hàng 1 lượt cho cả nhóm đơn.
- **Kết quả mong đợi:** 1 lượt nhặt + các phiếu nhặt; hàng được gom đủ cho nhóm đơn.

## Luồng cấu hình (admin / chủ shop)

### Cấu hình vận đơn
- Cài đặt mặc định cho service vận đơn: quy định nghiệp vụ, **số SKU tối đa mỗi phiếu nhặt**, cấu hình in/đơn hàng sale... Lưu xong áp dụng cho cả đơn vị.

### Cấu hình trạng thái giao hàng
1. Mở màn → danh sách trạng thái giao hàng hiện ra theo thứ tự (stt).
2. Chỉnh **thứ tự (stt)** kéo/sửa số + chọn **màu** cho từng trạng thái.
3. Bấm lưu → áp dụng cách hiển thị cho toàn bộ danh sách đơn.

### Mẫu kích thước đóng hàng
1. Thêm mẫu: nhập **Tên** + dài / rộng / cao / cân nặng.
2. (Tùy chọn) đặt 1 mẫu làm **mặc định**.
3. Khi đăng đơn → chọn mẫu để điền nhanh kích thước kiện hàng.

## Case biên
| Tình huống | Kết quả |
|---|---|
| Chưa kết nối hãng vận chuyển | Đăng đơn lỗi / không đẩy đi được |
| Tạo lượt nhặt khi chưa cấu hình SKU tối đa | Báo lỗi "Vui lòng cấu hình số lượng tối đa SKU trong một phiếu nhặt!" |
| Chọn đơn đã nằm trong lượt nhặt khác | Báo "Đơn hàng ... đã được tạo phiếu nhặt, vui lòng chọn đơn hàng khác!" |
| Lượt nhặt nhưng không tìm thấy đơn | Báo "Không tìm thấy đơn hàng!" |
| Thêm mẫu đóng hàng trùng tên (cùng chi nhánh) | Báo "Tên mẫu đã tồn tại" |
| Tên mẫu đóng hàng > 255 ký tự | Tự cắt còn 255 ký tự |
| Đăng đơn rồi hãng treo lâu | Dùng "hủy chờ đối tác đăng đơn" → đưa đơn về chưa đăng + hoàn kho |

## Ví dụ cụ thể có số
- Kho có **30 đơn** chờ giao, cấu hình **SKU tối đa = 20**. Nhân viên chọn cả 30 đơn tạo lượt nhặt → hệ thống gom thành lượt mã `LN24062026`, tách phiếu nhặt sao cho mỗi phiếu ≤ 20 loại sản phẩm, liệt kê theo vị trí kho. Nhân viên in phiếu, đi gom 1 vòng.
- Đơn 500.000đ COD, đăng sang GHN → nhận mã vận đơn, in phiếu giao; khi GHN giao xong + chuyển 500.000đ về, đối soát COD khớp theo trạng thái "đã giao".

## Hậu điều kiện
- Đơn đã đăng → có mã vận đơn, trạng thái chuyển sang đang giao; tồn kho được trừ theo soạn hàng.
- Lượt nhặt đã tạo → các đơn trong lượt được đánh dấu đã có phiếu nhặt (không gom lại được).
- Cấu hình đã lưu → áp dụng cho các thao tác đăng đơn / hiển thị sau đó.
