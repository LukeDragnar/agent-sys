# Quản lý bảo hành — Luồng

> ⚠️ **Luồng dưới đây là nghiệp vụ kỳ vọng — màn hình "Danh sách bảo hành" / "Xử lý bảo hành" CHƯA tìm thấy implement trong source.** Mô tả để định hướng nghiệp vụ + nuôi chatbot; **cần product xác nhận** trước khi coi là chính thức. Chi tiết kỹ thuật & phần đã/chưa tìm thấy code: [dev.md](./dev.md).

> Tóm tắt: [summarize.md](./summarize.md) · FAQ: [faq.md](./faq.md) · Module: [../README.md](../README.md)

## Tiền điều kiện
- Sản phẩm đã được **bán cho khách** (có đơn hàng `DonHang` chứa dòng `DonHangSanPham`).
- Dòng sản phẩm có **hạn bảo hành** (`HanBaoHanh`) và/hoặc **ngày kích hoạt bảo hành** (`NgayKichHoatBaoHanh`) — hoặc cửa hàng vẫn nhận bảo hành tính phí dù hết hạn.
- Với hàng IMEI: máy có **mã IMEI/serial** định danh được trong hệ thống.
- Người dùng có quyền tiếp nhận / xử lý bảo hành. *(Phân quyền cụ thể cần product xác nhận — chưa tìm thấy trong code.)*

## Luồng dùng chính (tiếp nhận → xử lý bảo hành)
1. **Tiếp nhận yêu cầu** — khách mang sản phẩm tới. Nhân viên tra cứu theo **IMEI/serial** hoặc theo **đơn hàng đã bán**; hệ thống hiển thị **hạn bảo hành còn lại**.
2. **Xác định điều kiện bảo hành**:
   - Còn hạn + lỗi thuộc diện bảo hành → hướng **bảo hành miễn phí (FOC)**.
   - Còn hạn nhưng lỗi do người dùng / ngoài phạm vi → **bảo hành có tính phí**.
   - Hết hạn → có thể nhận diện **bảo hành tính phí** (sửa chữa / bán linh kiện) hoặc **từ chối**.
3. **Lập/ghi nhận phiếu bảo hành** — ghi tình trạng lỗi, phụ kiện kèm theo, hướng xử lý dự kiến. *(Thực thể phiếu bảo hành chưa xác nhận có trong code.)*
4. **Xử lý** (màn Xử lý bảo hành):
   - **Sửa chữa** — cập nhật tiến độ; nếu lấy linh kiện thì phát sinh xuất kho.
   - **Đổi mới** — xuất máy mới ("Xuất đổi mới trong CS bảo hành"), nhận máy cũ vào ("Trả hàng nhận bảo hành").
   - **Từ chối** — đóng phiếu, nêu lý do.
5. **Hoàn tất** — trả sản phẩm cho khách, đóng phiếu; nếu có phí thì ghi nhận thu; nếu phát sinh kho thì lập phiếu xuất/nhập theo lý do bảo hành.

- **Kết quả mong đợi:** yêu cầu bảo hành có trạng thái rõ (đang xử lý / đã đổi / đã sửa / từ chối / hoàn tất), kho và chi phí được ghi nhận đúng theo loại bảo hành (FOC vs có phí).

## Các trường hợp / nhánh (case biên)
| Tình huống | Kết quả (kỳ vọng) |
|---|---|
| Còn hạn, lỗi thuộc bảo hành | Bảo hành **miễn phí (FOC)** |
| Còn hạn, lỗi do người dùng | Bảo hành **có tính phí** (sửa chữa / linh kiện) |
| Hết hạn bảo hành | Từ chối **hoặc** nhận sửa **có phí** |
| Sản phẩm chưa kích hoạt bảo hành | Cách tính hạn có thể khác — *cần product xác nhận* |
| Đổi máy mới cho khách | Phát sinh **xuất đổi mới** + **nhận máy cũ (trả hàng bảo hành)** trong kho |
| Nhầm IMEI khi tiếp nhận | Tra cứu/đổi sai máy — phải đúng IMEI mới ghi nhận |
| Không tìm thấy đơn hàng / IMEI đã bán | Không xác minh được bảo hành — *xử lý cần product xác nhận* |

## Ví dụ cụ thể có số
Khách mua **điện thoại X** (IMEI `35012345678`), `NgayKichHoatBaoHanh` = 01/01/2026, `HanBaoHanh` = 12 tháng → hết hạn **01/01/2027**.
- Ngày **15/06/2026** máy lỗi màn hình do nhà sản xuất → còn hạn (≈ 6.5 tháng) → tiếp nhận diện **FOC**, đổi mới.
- Hệ thống ghi: xuất 1 máy mới ("Xuất đổi mới trong CS bảo hành"), nhập 1 máy cũ ("Trả hàng nhận bảo hành").
- Trường hợp khác: ngày **10/02/2027** (đã quá hạn 40 ngày) máy lỗi do rơi vỡ → ngoài hạn + lỗi người dùng → nhận **bảo hành tính phí** (báo giá sửa chữa / bán linh kiện), không FOC.

## Hậu điều kiện
- Yêu cầu bảo hành được ghi nhận trạng thái cuối (hoàn tất / từ chối). *(Nơi lưu phiếu bảo hành chưa xác nhận trong code.)*
- Nếu đổi mới/lấy linh kiện: tồn kho cập nhật qua phiếu xuất/nhập theo **lý do bảo hành** tương ứng.
- Chi phí được phân loại **FOC** hay **có phí** để kế toán kiểm soát lại.
- Hạn bảo hành của máy thay/đổi mới: *quy tắc kế thừa hay tính lại cần product xác nhận.*
