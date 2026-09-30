# Kiểm kho — Luồng

> Tóm tắt: [summarize.md](./summarize.md) · FAQ: [faq.md](./faq.md) · Module: [../README.md](../README.md)

## Tiền điều kiện
- Đã có **chi nhánh** và **kho** được cấu hình.
- Kho đã có hàng (sản phẩm đã nhập kho — có dòng trong tồn kho `KhoChiTiet` hoặc IMEI ở trạng thái "Đã nhập kho").
- Người dùng có 1 trong các role: `DV-AD-TONG`, `DV-AD-CN`, `DV-QL-KHO`, `DV-QL-KHO-TONG`, `DV-ADMIN-KIEMKHO`, `DV-NHANVIEN-KIEMKHO`.

## Luồng dùng chính (tạo phiếu → kiểm → đối chiếu)
1. **Tạo phiếu kiểm** — chọn chi nhánh, kho; đặt Tên; bấm sinh **Mã** tự động (`TÊNKHO-ddMMyyyy-STT`); chọn **kiểu sản phẩm**:
   - *Tất cả sản phẩm* — kiểm mọi món trong kho.
   - *Sản phẩm cụ thể* — chỉ kiểm danh sách sản phẩm đã chọn.
   - Chọn **người kiểm**, **số lần kiểm lại tối đa**, **ngày bắt đầu – ngày kết thúc**.
   - Hệ thống chặn nếu Mã hoặc Tên trùng trong cùng đơn vị.
2. **Mở màn kiểm** — tải danh sách sản phẩm cần kiểm. Nếu phiếu "sản phẩm cụ thể" → hiện sẵn danh sách; nếu "tất cả" → hiện danh sách đã quét.
3. **Đếm thực tế** — với mỗi món: **quét mã vạch/IMEI** (mỗi lần quét +1 vào "Số lượng đã kiểm") hoặc **nhập tay** số lượng đã kiểm.
   - Hệ thống kiểm tra thời điểm hiện tại phải nằm trong [Ngày bắt đầu, Ngày kết thúc].
4. **Kiểm tra kết quả** — đối chiếu **Số lượng đã kiểm** với **Số lượng tồn hệ thống**:
   - Bằng nhau → **Kiểm đủ**.
   - Khác nhau → **Kiểm sai**.
   - Mỗi lần bấm tính 1 lượt; tăng "Số lần đã kiểm".
5. **Kiểm lại (nếu cần)** — còn lệch + còn lượt → đếm lại và bấm Kiểm tra kết quả lần nữa, tới khi hết lượt hoặc hết hạn.

- **Kết quả mong đợi:** mỗi sản phẩm trong phiếu có trạng thái Kiểm đủ / Kiểm sai kèm số đã kiểm và số tồn hệ thống.

## Các trường hợp / nhánh (case biên)
| Tình huống | Kết quả |
|---|---|
| Đếm khớp tồn hệ thống | Trạng thái **Kiểm đủ** |
| **Đếm lệch tồn** (thiếu hoặc thừa) | Trạng thái **Kiểm sai** — ghi nhận chênh, KHÔNG tự sửa tồn |
| Quét/nhập ngoài khoảng ngày phiếu | Báo "Không trong thời gian cho phép kiểm kho" |
| Hết số lần kiểm lại | Không cho kiểm lại (cờ "Hết lượt kiểm") |
| Quét IMEI đã quét trong phiếu | Báo "Imei này đã được kiểm trước đó" |
| Quét mã không có trong kho | Báo "Sản phẩm này chưa được nhập kho" |
| Phiếu "sản phẩm cụ thể" nhưng quét món ngoài danh sách | Báo "không nằm trong danh sách sản phẩm trên phiếu kiểm" |
| Phiếu "sản phẩm cụ thể" mà chưa chọn sản phẩm | Báo "Phiếu kiểm chưa chọn sản phẩm" |

## Ví dụ cụ thể có số
Kho A có sản phẩm **Kem chống nắng**, tồn hệ thống = **20 hộp**.
- Nhân viên quét/đếm thực tế được **18 hộp** → bấm Kiểm tra kết quả.
- Đối chiếu 18 (đã kiểm) ≠ 20 (tồn) → **Kiểm sai**, lệch **−2 hộp**.
- "Số lần đã kiểm" = 1; nếu số lần kiểm lại tối đa = 3 → còn 2 lượt.
- Đếm lại lần 2 đúng = 20 → **Kiểm đủ**.
- Nếu vẫn là 18: phiếu ghi nhận thiếu 2 hộp → cần tạo **phiếu xuất điều chỉnh** riêng để nắn tồn về 18 (kiểm kho không tự làm việc này).

## Hậu điều kiện
- Phiếu kiểm có danh sách sản phẩm kèm Số lượng đã kiểm + Trạng thái (Kiểm đủ/Kiểm sai) lưu ở `PhieuKiemKhoChiTietSanPham`; IMEI đã quét lưu ở `PhieuKiemKhoChiTietImei`.
- "Số lần đã kiểm" tăng theo mỗi lần Kiểm tra kết quả.
- Tồn kho thực tế **chưa thay đổi** — cần nghiệp vụ nhập/xuất điều chỉnh riêng nếu muốn đồng bộ.
