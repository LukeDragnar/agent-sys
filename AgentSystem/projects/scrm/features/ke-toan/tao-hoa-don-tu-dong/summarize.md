---
alias: ["tạo hóa đơn tự động", "phát hành hóa đơn tự động", "xuất HĐĐT tự động", "hóa đơn tự động theo trạng thái giao hàng", "tự động xuất hóa đơn"]
module: ke-toan
trang_thai: cần cấu hình + cần bật tiện ích HĐĐT — cần product xác nhận
goi: cần product xác nhận — phụ thuộc tiện ích hóa đơn điện tử (MISA/Viettel)
quyen: "cấu hình do admin/kế toán thiết lập (trạng thái giao hàng được tự phát hành + loại HĐ tự động). Quá trình chạy là tự động (job/queue), không thao tác tay — cần product xác nhận vai cấu hình"
phu_thuoc: ["Hóa đơn điện tử (tài khoản + mẫu mặc định MISA/Viettel đã cấu hình)", "Cấu hình trạng thái giao hàng có cờ tự phát hành hóa đơn", "Cấu hình đơn vị: loại HĐ tự động + số phút trễ", "Đơn hàng (đơn cửa hàng/POS hoặc đơn online logistic) đạt trạng thái giao hàng cấu hình"]
link_flow: ./flow.md
---

# Tạo hóa đơn tự động

## Là gì
Đây là tính năng giúp hệ thống **tự phát sinh hóa đơn điện tử** cho đơn hàng mà **không cần kế toán bấm tay**, dựa trên **trạng thái giao hàng** của đơn. Khi đơn đạt một trạng thái giao hàng đã được cấu hình "cho phép tự phát hành" (ví dụ "Đã giao hàng"), hệ thống tự xếp đơn vào hàng đợi và sau một khoảng thời gian trễ sẽ **tạo hóa đơn nháp** hoặc **phát hành hóa đơn thật** (tùy cấu hình loại hóa đơn tự động).

Tính năng này gắn với 2 nguồn đơn:
- **Đơn cửa hàng/POS** (đơn tạo ở module Cửa hàng): khi đơn chuyển trạng thái phù hợp.
- **Đơn online logistic**: khi trạng thái giao hàng đạt mốc (thường qua thao tác trên Kế toán tác nghiệp / cập trạng thái giao hàng).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Bật tự tạo/phát hành hóa đơn theo trạng thái giao hàng | ✅ | qua cấu hình (đánh dấu trạng thái giao hàng được tự phát hành hóa đơn) |
| Chọn chế độ: tự **phát hành thật** hay tự **tạo nháp** | ✅ | cấu hình loại hóa đơn tự động (nháp hoặc phát hành) |
| Đặt độ trễ trước khi tự phát hành | ✅ | cấu hình số phút trễ trước khi phát hành (mặc định 5 phút) |
| Đặt độ trễ trước khi tự tạo nháp | ✅ | cấu hình số phút trễ trước khi tạo nháp (mặc định 2 phút) |
| Tự bỏ qua đơn đã có hóa đơn | ✅ | hệ thống tự skip đơn đã có số/ngày HĐ |
| Tự chống tạo trùng (dedupe hàng đợi) | ✅ | hệ thống kiểm tra bảng hàng đợi + bảng gửi lại |
| Hủy phát hành tự động (đơn đang chờ tự phát hành) | ⚠️ | có luồng hủy phát hành tự động — cần product xác nhận thao tác UI |
| Tự phát hành cho nhà cung cấp ngoài MISA/Viettel | ❌ | chỉ hỗ trợ MISA + Viettel |

## Để làm gì / cho ai
- **Kế toán/chủ shop:** giảm thao tác tay — đơn giao xong tự ra hóa đơn, hạn chế bỏ sót.
- **Admin đơn vị:** cấu hình mốc trạng thái + chế độ (nháp/phát hành) + độ trễ phù hợp quy trình.
- **Khách:** nhận hóa đơn nhanh sau khi đơn hoàn tất (kèm SMS/Zalo nếu cấu hình).

## Cách dùng (cấu hình)
1. Đảm bảo đã bật **tiện ích hóa đơn điện tử** và cấu hình **tài khoản + mẫu mặc định** (xem feature "Hóa đơn điện tử").
2. Trong cấu hình **trạng thái giao hàng**, đánh dấu trạng thái nào được **tự phát hành hóa đơn**.
3. Trong cấu hình đơn vị, chọn **loại hóa đơn tự động**: tự phát hành thật hay tự tạo nháp; đặt **số phút trễ**.
4. Từ đó, mọi đơn khi chuyển sang trạng thái giao hàng đã đánh dấu sẽ tự được xử lý (sau độ trễ).

## Cần chú ý
- **Phụ thuộc trạng thái giao hàng:** chỉ đơn đạt đúng trạng thái cấu hình mới được tự xử lý. Cấu hình sai mốc → đơn không tự ra hóa đơn (hoặc ra sớm/muộn không mong muốn).
- **Đơn đã có hóa đơn bị bỏ qua:** nếu đơn đã có số/ngày hóa đơn, hệ thống không tạo lại (tránh trùng).
- **Chống trùng theo hàng đợi:** đơn đã nằm trong hàng đợi tự phát hành/tạo nháp sẽ không bị thêm lần nữa.
- **Chế độ nháp:** chỉ tự **tạo nháp**, KHÔNG tự phát hành thật — kế toán vẫn phải đồng bộ nháp → phát hành.
- **Lỗi tự phát hành ghi log riêng:** ví dụ "Không lấy được cấu hình trạng thái giao hàng", "Đơn hàng đã phát hành trước đó" được ghi vào nhật ký hóa đơn tự động để tra cứu.
- **Chỉ MISA + Viettel.** Nhà cung cấp khác không tự phát hành được.

## Ảnh hưởng tới
- **Hóa đơn điện tử:** dùng chung tài khoản + mẫu mặc định; kết quả phát hành ghi vào trạng thái hóa đơn của đơn.
- **Kế toán tác nghiệp / cập trạng thái giao hàng:** là nơi đơn đổi trạng thái → kích hoạt tự phát hành.
- **Đồng bộ kế toán (phiếu bán hàng tự động):** đơn đạt mốc trạng thái cũng có thể tự sinh phiếu bán hàng kế toán (luồng song song).
- **Thông báo SMS/Zalo (ZNS):** sau phát hành thành công có thể đẩy tin báo khách.
