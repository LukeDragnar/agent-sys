---
alias: ["bảo hành", "quản lý bảo hành", "bảo hành sản phẩm", "tiếp nhận bảo hành", "xử lý bảo hành"]
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — chưa tìm thấy phân quyền cụ thể trong code"
phu_thuoc: [WarehouseProduct (SanPham, SanPhamImei, Kho), Order (DonHang, DonHangSanPham — chứa HanBaoHanh / NgayKichHoatBaoHanh), Core (chi nhánh / khách hàng / nhân viên)]
link_flow: ./flow.md
module: kho
---

# Quản lý bảo hành

> ⚠️ **Tài liệu mức nghiệp vụ chung — CHƯA tìm thấy màn hình "Danh sách bảo hành" / "Xử lý bảo hành" được implement trong source.** Phần dữ liệu bảo hành hiện chỉ tồn tại ở mức **thuộc tính trên đơn hàng/sản phẩm** (hạn bảo hành, ngày kích hoạt) và một số **lý do xuất kho liên quan bảo hành**. Toàn bộ luồng tiếp nhận → xử lý mô tả bên dưới là **nghiệp vụ kỳ vọng, cần product xác nhận** trước khi dùng nuôi chatbot. Chi tiết kỹ thuật: [dev.md](./dev.md).

## Là gì
Quản lý bảo hành là việc **theo dõi và xử lý các yêu cầu bảo hành** cho sản phẩm khách đã mua: khi sản phẩm còn trong hạn (hoặc hết hạn nhưng vẫn nhận sửa có phí) bị lỗi, cửa hàng **tiếp nhận** yêu cầu, ghi nhận tình trạng máy/sản phẩm, rồi **xử lý** theo một trong các hướng: sửa chữa, đổi mới, trả linh kiện, hoặc từ chối nếu hết điều kiện bảo hành.

Mỗi yêu cầu gắn với **một sản phẩm cụ thể** (thường nhận diện qua mã **IMEI/serial** hoặc dòng sản phẩm trên đơn hàng) và một **hạn bảo hành** (`HanBaoHanh`) tính từ **ngày kích hoạt bảo hành** (`NgayKichHoatBaoHanh`).

## Hành động làm được
> ⚠️ Bảng dựa trên nghiệp vụ kỳ vọng — **chưa tìm thấy màn hình bảo hành độc lập trong source**; cần product xác nhận trước khi khẳng định với khách.
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tra hạn bảo hành theo IMEI / đơn hàng đã bán | ✅ | dữ liệu `HanBaoHanh` / `NgayKichHoatBaoHanh` có sẵn trên đơn/sản phẩm |
| Mở màn "Danh sách bảo hành" / "Tiếp nhận bảo hành" / "Xử lý bảo hành" | ⚠️ | chưa xác nhận có màn hình riêng được implement — cần product xác nhận |
| Ghi nhận tình trạng lỗi, hướng xử lý (sửa / đổi mới / FOC / có phí / từ chối) | ⚠️ | nghiệp vụ kỳ vọng; cần product xác nhận |
| Đổi mới / trả linh kiện làm phát sinh xuất–nhập kho | ✅ | qua phiếu kho theo lý do bảo hành tương ứng; không tự cộng/trừ tồn nếu không lập phiếu |
| Phân biệt bảo hành miễn phí (FOC) và có phí | ✅ | hệ thống có tách 2 loại để kế toán kiểm soát |

## Để làm gì / cho ai
- **Cho chủ shop / quản lý:** kiểm soát hàng nhận bảo hành ra vào kho, biết món nào đang sửa, đã đổi mới hay đã trả khách; tách được chi phí bảo hành có phí / miễn phí (FOC).
- **Cho nhân viên CSKH / kỹ thuật:** có chỗ tiếp nhận yêu cầu, tra cứu hạn bảo hành theo IMEI/đơn hàng, cập nhật tiến độ xử lý.
- **Cho khách hàng:** được phục vụ nhanh, tra cứu được tình trạng máy đang bảo hành.
- **Actor:** nhân viên tiếp nhận (CSKH/quầy), kỹ thuật/kho xử lý, quản lý duyệt. *(Vai trò chính xác cần product xác nhận.)*

## Cách dùng
> Mô tả kỳ vọng — màn hình thực tế chưa tìm thấy trong source, cần product xác nhận.
1. Vào **Kho → Quản lý bảo hành → Danh sách bảo hành** để xem các yêu cầu đang theo dõi.
2. Bấm **Tiếp nhận bảo hành**: tra sản phẩm theo **IMEI/serial** hoặc theo **đơn hàng đã bán**; hệ thống hiển thị **hạn bảo hành** còn lại.
3. Ghi nhận: tình trạng lỗi, khách báo gì, phụ kiện kèm theo, có còn trong hạn không.
4. Mở phiếu sang màn **Xử lý bảo hành**: chọn hướng xử lý (sửa chữa / đổi mới / FOC / có phí / từ chối), cập nhật tiến độ.
5. Khi xong: trả máy cho khách, đóng phiếu; nếu phát sinh xuất/nhập kho (đổi máy, lấy linh kiện) thì ghi nhận theo **lý do bảo hành** tương ứng.

## Cần chú ý (cạm bẫy)
- **Hạn bảo hành tính từ ngày kích hoạt, không phải ngày mua** — `HanBaoHanh` đi cùng `NgayKichHoatBaoHanh`; nếu chưa kích hoạt thì cách tính hạn có thể khác. *(Quy tắc tính cần product xác nhận.)*
- **Còn hạn ≠ luôn miễn phí** — hệ thống có phân biệt **bảo hành không tính phí (FOC)** và **bảo hành có tính phí** (sửa chữa / bán linh kiện). Lỗi do người dùng có thể bị tính phí.
- **Sản phẩm IMEI:** mỗi máy là một mã IMEI riêng — tiếp nhận/đổi máy phải đúng IMEI, tránh nhầm máy khác.
- **Đổi mới làm phát sinh xuất/nhập kho** — đổi máy mới cho khách là một nghiệp vụ kho (lý do "Xuất đổi mới trong CS bảo hành" / "Trả hàng nhận bảo hành"); không tự cộng trừ tồn nếu không lập phiếu kho tương ứng.
- ⚠️ **CHƯA xác nhận có màn hình quản lý bảo hành độc lập** — ở thời điểm tài liệu này, bảo hành mới là **field trên đơn/sản phẩm**, chưa thấy module phiếu bảo hành riêng. Đừng khẳng định với khách là có màn hình cho tới khi product xác nhận.

## Ảnh hưởng tới
- **Đơn hàng (Order)** — hạn/ngày kích hoạt bảo hành gắn trên dòng sản phẩm của đơn (`DonHangSanPham`); là nguồn để tra bảo hành theo lịch sử mua.
- **Tồn kho (WarehouseProduct)** — đổi mới / trả hàng nhận bảo hành / lấy linh kiện làm phát sinh xuất–nhập kho theo các lý do bảo hành.
- **Sản phẩm IMEI** — mỗi máy bảo hành theo IMEI riêng.
- **Báo cáo / kế toán** — bảo hành có phí và FOC được phân loại để kế toán kiểm soát lại.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · [dev.md](./dev.md) · Module: [../README.md](../README.md)
