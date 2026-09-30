---
alias: ["đặt cọc", "dat coc", "phiếu cọc", "phieu coc", "hoàn cọc", "hoan coc", "trả cọc", "đợt trả cọc", "thay sản phẩm đặt cọc", "tiền cọc", "quản lý đặt cọc", "deposit"]
module: mua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — toàn bộ endpoint phiếu cọc và đợt trả cọc yêu cầu role DV-AD-TONG, DV-QL-KHO-TONG."
phu_thuoc: ["Quản lý sản phẩm (sản phẩm đặt cọc)", "Kho / Chi nhánh nhận", "Voucher / Marketing (mã voucher gắn đợt; hủy voucher khi hoàn cọc)", "SMS / Zalo (gửi tin theo từng hành động cọc)", "Giao hàng / Order (cập nhật trạng thái đã giao theo mã voucher)"]
link_flow: ./flow.md
---

# Quản lý đặt cọc

## Là gì
Đây là nơi **quản lý toàn bộ vòng đời tiền cọc của khách cho một sản phẩm**: khách đặt cọc → thanh toán cọc → (có thể) thay sản phẩm đã cọc, hoặc hoàn cọc, hoặc xếp vào "đợt trả cọc" để giao hàng/gửi tin nhắn hàng loạt. Tính năng gồm hai màn chính đi đôi:

- **Quản lý đặt cọc (phiếu cọc)**: danh sách từng phiếu cọc (1 phiếu = 1 khách × 1 sản phẩm). Tại đây cập nhật thanh toán, thay sản phẩm, hoàn cọc (2 bước: yêu cầu → xác nhận), xác nhận đợi đợt sau, xem lịch sử, xuất Excel.
- **Đợt trả cọc**: gom nhiều phiếu cọc đủ điều kiện theo (sản phẩm × số lượng × ngày × chi nhánh) thành một "đợt" để chuẩn bị trả cọc / gửi SMS-Zalo hàng loạt, gắn mẫu tin nhắn, rồi khóa đợt khi chốt.

Mỗi thao tác trạng thái phiếu cọc đều **gửi SMS/Zalo cho khách** theo hành động (thay SP / hoàn cọc / đợi đợt sau / xác nhận hoàn / đã nhận).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách phiếu cọc + lọc | ✅ | role kho tổng; phiếu "bị thay thế" luôn bị ẩn |
| Cập nhật trạng thái thanh toán cọc | ✅ | tùy quyền; lần đầu chuyển "đã thanh toán" sẽ gửi SMS |
| Thay sản phẩm đã đặt cọc | ⚠️ | chỉ khi **đã thanh toán** & chưa vào đợt trả cọc & chưa chọn đợi đợt sau |
| Hoàn cọc (yêu cầu) | ⚠️ | chỉ khi **đã thanh toán** & chưa hoàn & chưa giao |
| Xác nhận hoàn cọc | ⚠️ | chỉ khi đã yêu cầu hoàn & chưa xác nhận |
| Xác nhận sẽ đợi đợt sau | ✅ | chỉ có hiệu lực lần đầu (lặp lại không đổi) |
| Xóa phiếu khỏi đợt trả cọc | ⚠️ | đã vào đợt & chưa giao; tối đa 1.000 phiếu/lần |
| Xem lịch sử thay đổi phiếu | ✅ | tùy quyền |
| Xuất Excel quản lý đặt cọc | ✅ | tùy quyền |
| Tạo đợt trả cọc | ✅ | mã đợt tự sinh (prefix BAT); SP không trùng, SL>0 |
| Sửa / Xóa đợt | ⚠️ | chỉ khi đợt **chưa khóa**; xóa cần chưa gán phiếu & chưa có SMS |
| Gán danh sách trả cọc vào đợt | ⚠️ | chỉ khi đợt chưa khóa |
| Cấu hình mẫu SMS/Zalo cho đợt | ⚠️ | chỉ khi đợt chưa khóa |
| Khóa đợt | ⚠️ | cần đã có ≥1 mẫu SMS/Zalo |
| **Xóa phiếu cọc** | ❌ | không có chức năng xóa phiếu cọc (chỉ thay/hoàn/quản lý trạng thái) |

## Để làm gì / cho ai
- **Bộ phận kho / mua hàng / CSKH:** theo dõi khách đã cọc, ai đã thanh toán, ai cần hoàn cọc, ai chờ đợt giao tiếp theo.
- **Quản lý kho tổng:** tổ chức "đợt trả cọc" để giao hàng và nhắn tin hàng loạt cho nhóm khách cùng sản phẩm.
- **Marketing:** voucher gắn theo đợt; khi hoàn cọc voucher tương ứng bị hủy.

## Cách dùng

**Quản lý phiếu cọc:**
1. Vào **Đặt cọc → Quản lý đặt cọc**.
2. Lọc theo chi nhánh, sản phẩm, trạng thái thanh toán / giao / hoàn cọc, mốc ngày.
3. Trên từng phiếu: **Cập nhật thanh toán**, **Thay sản phẩm**, **Hoàn cọc** rồi **Xác nhận hoàn cọc**, **Xác nhận đợi đợt sau**, **Xem lịch sử**, hoặc **Xóa khỏi đợt trả cọc**.
4. **Xuất Excel** để báo cáo.

**Đợt trả cọc:**
1. Vào **Đặt cọc → Đợt trả cọc**.
2. **Tạo đợt**: khai danh sách sản phẩm × số lượng × khoảng ngày cọc × chi nhánh nhận.
3. **Gán danh sách trả cọc**: hệ thống tìm các phiếu cọc đủ điều kiện và gán vào đợt.
4. **Cấu hình mẫu SMS/Zalo** cho đợt.
5. **Khóa đợt** khi chốt — sau khi khóa không sửa được nữa, hệ thống chuẩn bị gửi tin cho khách trong đợt.

## Cần chú ý
- **Trạng thái phiếu cọc là tập cờ độc lập, không tuyến tính**: đã thanh toán, đã giao, đã yêu cầu hoàn, đã xác nhận hoàn, đợi đợt sau — mỗi cờ có điều kiện riêng cho từng hành động.
- **Phiếu "bị thay thế" luôn bị ẩn** khỏi mọi tìm kiếm/xuất Excel.
- **Hoàn cọc đi 2 bước**: yêu cầu hoàn (ghi thông tin ngân hàng) → xác nhận hoàn (khi đã chi tiền). Xác nhận hoàn áp cho mọi phiếu cùng mã cọc.
- **Khóa đợt là chốt cứng** — sau đó không sửa/gán/xóa/cấu hình SMS được nữa.
- **Hệ thống không tự gửi SMS** ngay trong màn này — nó bắn yêu cầu sang dịch vụ SMS/Zalo để gửi.
- **Tiền cọc trung bình hiển thị có thể bằng 0** do một lỗi tính toán đã biết (xem dev) — không phải khách chưa nộp.

## Ảnh hưởng tới
- **SMS / Zalo:** mỗi hành động cọc bắn tin cho khách.
- **Voucher / Marketing:** đợt trả cọc sinh/hủy voucher; hoàn cọc hủy voucher liên quan.
- **Giao hàng:** khi đơn giao xong (theo mã voucher), phiếu cọc được đánh dấu "đã giao" và khóa hoàn cọc/thay SP.
