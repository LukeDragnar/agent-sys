---
alias: ["job kế toán chạy nền", "tự phát hành hóa đơn chạy ngầm", "đối soát đơn online tự động", "đối soát bank tự động", "đối soát đơn hoàn nền", "hóa đơn tự động chạy job", "job đối soát kế toán"]
module: ke-toan
trang_thai: nền (không menu) — cần bật tiện ích HĐĐT / logistic — cần product xác nhận
goi: cần product xác nhận — phụ thuộc tiện ích hóa đơn điện tử (MISA/Viettel) + tích hợp ngân hàng MB
quyen: "không thao tác tay — toàn bộ chạy bằng consumer/job nền, không gắn vai. Vai cấu hình (mốc trạng thái tự phát hành, tài khoản HĐĐT) — cần product xác nhận"
phu_thuoc: ["Tạo hóa đơn tự động (cấu hình mốc trạng thái + loại HĐ tự động — feature ke-toan/tao-hoa-don-tu-dong)", "Hóa đơn điện tử (tài khoản + mẫu MISA/Viettel)", "Đối soát đơn online / Kế toán tác nghiệp (đơn online + lịch sử đối soát đơn hoàn — feature ke-toan/doi-soat-don-online)", "Phiếu bán trả lại (Accounting — đích publish của job đối soát đơn hoàn)", "Tích hợp ngân hàng MB (file đối soát .txt) — chỉ cho đối soát bank"]
link_flow: ./flow.md
---

# Tự động hóa đơn & đối soát chạy nền (job kế toán)

## Là gì
Đây là **phần "chạy ngầm" của kế toán** — những việc hệ thống **tự làm sau lưng** mà người dùng không bấm nút: tự phát hành/ tạo nháp hóa đơn điện tử khi đơn đạt mốc, tự đối soát đơn hoàn để sinh phiếu bán trả lại, và tự đối soát giao dịch thanh toán với ngân hàng MB. Tất cả do **consumer hàng đợi** (nhận sự kiện rồi xử lý) và **job định kỳ** (chạy lặp theo lịch) đảm nhiệm, không có màn hình thao tác trực tiếp.

Gồm 3 mạch nền:
- **Tự phát hành / tạo nháp hóa đơn điện tử** từ đơn (consumer nhận sự kiện đổi trạng thái → xếp hàng đợi → phát hành). *(Góc cấu hình + cơ chế hàng đợi xem feature "Tạo hóa đơn tự động"; ở đây nhấn vào phần consumer/retry chạy ngầm.)*
- **Đối soát đơn hoàn → tạo phiếu bán trả lại** (job định kỳ ~10 giây/lần quét đơn hoàn đã đối soát chưa tạo phiếu → đẩy sang kế toán).
- **Đối soát giao dịch ngân hàng MB** (job nền chạy mỗi ngày từ 8h, đọc file đối soát ngân hàng gửi → so khớp với giao dịch trong hệ thống → xuất file kết quả khớp/lệch).

> Lưu ý phân biệt: feature **"Tạo hóa đơn tự động"** (`ke-toan/tao-hoa-don-tu-dong`) nói về **cấu hình + hàng đợi**; feature **"Đối soát đơn online"** (`ke-toan/doi-soat-don-online`) nói về **màn tác nghiệp kế toán**. Feature này gom **góc job/consumer chạy nền + retry + đối soát tự động** của cả hai.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tự phát hành / tạo nháp HĐĐT khi đơn đạt mốc | ✅ | consumer nền, theo cấu hình "Tạo hóa đơn tự động" |
| Tự đẩy lại (re-send) đơn phát hành lỗi | ✅ | có consumer re-send riêng |
| Tự đối soát đơn hoàn → tạo phiếu bán trả lại | ✅ | job định kỳ ~10 giây/lần, quét hàng đợi đơn hoàn đã đối soát |
| Tự đối soát giao dịch ngân hàng MB hằng ngày | ✅ | job nền; cần có **file đối soát MB** ngân hàng gửi về; xuất file khớp/lệch |
| Bật/tắt từng job | ❌ | không có công tắc UI — bật/tắt nằm trong code (đăng ký job/consumer) |
| Xem nhật ký job ngay trên màn hình | ❌ | không có màn nhật ký riêng; phải tra log hệ thống / bảng hàng đợi (xem dev.md) |
| Đối soát ngân hàng khác MB qua job | ❌ | job đối soát bank hiện chỉ làm cho MB (kênh "0G") |

## Để làm gì / cho ai
- **Kế toán / chủ shop:** đỡ thao tác tay — đơn giao xong tự ra hóa đơn; đơn hoàn được đối soát tự sinh phiếu trả lại; giao dịch ngân hàng tự khớp số cuối ngày.
- **Vận hành / kỹ thuật:** đảm bảo 3 mạch nền chạy đều; khi đơn "không ra hóa đơn" / "phiếu trả lại không lên" thì biết là do mạch nền nào.
- **Khách:** nhận hóa đơn nhanh sau khi đơn hoàn tất mà không phải chờ kế toán thao tác.

## Cách dùng
Người dùng **không thao tác trực tiếp** — đây là phần chạy ngầm. Việc cần làm chỉ là **cấu hình đúng đầu vào**:
1. Bật tiện ích **hóa đơn điện tử**, cấu hình tài khoản + mẫu mặc định (MISA/Viettel).
2. Cấu hình **mốc trạng thái giao hàng được tự phát hành** + loại HĐ tự động + độ trễ (xem feature "Tạo hóa đơn tự động").
3. Với đối soát đơn hoàn: thực hiện **đối soát đơn hoàn** ở màn tác nghiệp → hệ thống xếp vào hàng đợi → job tự đẩy sang kế toán tạo phiếu.
4. Với đối soát ngân hàng MB: đảm bảo **file đối soát MB** được đặt đúng thư mục mỗi ngày → job tự đọc và xuất file kết quả.

## Cần chú ý
- **Lỗi bị "nuốt" im lặng:** mạch tự phát hành / gửi lại có thể bỏ qua lỗi mà không báo → khi xử lý gặp lỗi, đơn **không vào hàng đợi mà không có cảnh báo**. Đơn "không tự ra hóa đơn" có thể do mạch nền nuốt lỗi, không phải do cấu hình (chi tiết kỹ thuật xem dev.md).
- **Job đối soát đơn hoàn lệ thuộc hàng đợi:** chỉ đơn hoàn **đã đối soát** và **được xếp vào hàng đợi tạo phiếu** mới được job xử lý. Đối soát xong nhưng chưa enqueue thì job không thấy.
- **Job đối soát MB cần file ngân hàng:** nếu chưa có file `0G_yyyyMMdd_TC.txt` ngân hàng gửi, job **chờ và thử lại mỗi 30 phút**, không tự bịa số. Đối soát chỉ chạy trong khung giờ từ 8h sáng.
- **Đối soát MB chỉ MB (kênh 0G):** ngân hàng khác chưa có job đối soát tự động tương ứng.
- **Một số "đối soát" khác đang tắt:** kênh đối soát ngân hàng nhận tự động (qua thông báo từ ngân hàng) hiện **đang bị tắt** — đừng nhầm với job đối soát MB đang chạy (chi tiết kỹ thuật xem dev.md).

## Ảnh hưởng tới
- **Hóa đơn điện tử:** kết quả tự phát hành ghi vào trạng thái hóa đơn của đơn (dùng chung tài khoản/mẫu).
- **Phiếu bán trả lại (kế toán):** job đối soát đơn hoàn đẩy message sang Accounting để **tạo phiếu bán trả lại** tương ứng đơn hoàn.
- **Báo cáo / số liệu thanh toán:** đối soát MB phát hiện giao dịch **thừa MB / thừa hệ thống / lệch số tiền** → xuất file chênh lệch để kế toán xử lý.
- **Lương / hoa hồng:** đối soát đơn (đánh dấu ngày đối soát) có thể gắn cờ thay đổi vào lương chi tiết (xem feature "Đối soát đơn online").
