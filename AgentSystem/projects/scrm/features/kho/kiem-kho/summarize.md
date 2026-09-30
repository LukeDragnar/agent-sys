---
alias: ["kiểm kho", "phiếu kiểm kho", "kiểm tra tồn thực tế", "đếm kho", "kiểm kê hàng hóa"]
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — code yêu cầu 1 trong các role: DV-AD-TONG, DV-AD-CN, DV-QL-KHO, DV-QL-KHO-TONG, DV-ADMIN-KIEMKHO, DV-NHANVIEN-KIEMKHO"
phu_thuoc: [WarehouseProduct (Kho, KhoChiTiet, SanPham, SanPhamImei), Core (chi nhánh/nhân viên), Elasticsearch (tên sản phẩm)]
link_flow: ./flow.md
module: kho
---

# Kiểm kho

## Là gì
Kiểm kho là việc **lập phiếu để đếm lại số lượng hàng hóa thực tế đang có trong kho**, rồi so với số liệu hệ thống đang ghi nhận. Mỗi lần kiểm là một **phiếu kiểm kho** — chỉ định kho nào, kiểm sản phẩm gì, ai kiểm, trong khoảng thời gian nào.

Khi nhân viên đi kiểm, họ **quét mã vạch / mã IMEI** (hoặc nhập tay số lượng) cho từng món. Hệ thống đối chiếu số đã đếm với số tồn trên hệ thống và báo từng món là **"Kiểm đủ"** (khớp) hay **"Kiểm sai"** (lệch).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tạo phiếu kiểm kho (chọn kho, sản phẩm, người kiểm, thời gian) | ✅ | cần 1 trong các role kiểm kho; Mã và Tên phiếu không trùng trong đơn vị |
| Quét mã vạch / IMEI hoặc nhập tay số lượng đếm | ✅ | chỉ trong khoảng "Ngày bắt đầu → Ngày kết thúc" của phiếu |
| Đối chiếu kết quả (Kiểm đủ / Kiểm sai) | ✅ | bấm "Kiểm tra kết quả" |
| Kiểm lại khi còn lệch | ⚠️ | giới hạn theo "Số lần kiểm lại tối đa"; hết lượt hoặc hết hạn → không kiểm lại |
| Quét IMEI đã kiểm / IMEI chưa nhập kho | ❌ | báo trùng / báo "chưa được nhập kho" |
| Tự sửa tồn kho từ phiếu kiểm | ❌ | kiểm kho chỉ ghi chênh lệch; nắn tồn phải lập phiếu nhập/xuất điều chỉnh riêng |

## Để làm gì / cho ai
- **Cho chủ shop / quản lý kho:** biết hàng thực tế còn bao nhiêu, phát hiện thất thoát, hàng thừa/thiếu so với sổ sách.
- **Cho nhân viên kho:** có danh sách rõ ràng để đi đếm, quét là xong, không cần đối chiếu tay.
- **Actor:** quản lý kho, nhân viên được phân công kiểm (trong danh sách "người kiểm kho" của phiếu).

## Cách dùng
1. Vào **Kho → Quản lý kiểm kho**.
2. Bấm **Tạo phiếu kiểm**: chọn chi nhánh, kho, đặt tên (mã có thể bấm sinh tự động), chọn kiểu sản phẩm (tất cả / sản phẩm cụ thể), chọn người kiểm, số lần kiểm lại tối đa, ngày bắt đầu–kết thúc.
3. Đến giờ kiểm, mở phiếu → màn **kiểm tra**: quét mã vạch/IMEI hoặc nhập tay số lượng đã đếm cho từng sản phẩm.
4. Bấm **Kiểm tra kết quả**: hệ thống đối chiếu và đánh dấu Kiểm đủ / Kiểm sai cho từng món.
5. Nếu còn lệch và còn lượt kiểm → kiểm lại lần nữa cho tới khi hết lượt hoặc hết hạn.

## Cần chú ý (cạm bẫy)
- **Chỉ kiểm được trong khoảng thời gian phiếu cho phép** — ngoài khoảng "Ngày bắt đầu → Ngày kết thúc", hệ thống chặn không cho quét/nhập.
- **Số lần kiểm lại có giới hạn** — đặt "Số lần kiểm lại tối đa"; hết lượt thì không kiểm lại được nữa.
- **Mã và Tên phiếu không được trùng** trong cùng đơn vị.
- **Kiểm kho KHÔNG tự sửa tồn kho** — phiếu chỉ ghi nhận chênh lệch để báo cáo; muốn nắn tồn về đúng phải tạo phiếu nhập/xuất điều chỉnh riêng (cần product xác nhận quy trình điều chỉnh).
- Sản phẩm IMEI: quét đúng mã IMEI đang ở trạng thái "Đã nhập kho"; quét lại IMEI đã kiểm sẽ bị báo trùng.
- Sản phẩm chưa từng nhập kho thì quét sẽ báo "chưa được nhập kho".

## Ảnh hưởng tới
- **Tồn kho (Quản lý tồn kho)** — là số liệu gốc để đối chiếu; kiểm kho phát hiện lệch nhưng không tự đổi tồn.
- **Báo cáo kho** — kết quả kiểm (đủ/sai) phục vụ rà soát thất thoát.
- **Lô date & IMEI** — sản phẩm IMEI được đối chiếu theo từng mã IMEI trong kho.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · Module: [../README.md](../README.md)
