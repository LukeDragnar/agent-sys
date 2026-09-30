---
alias: ["khuyến mãi", "chương trình khuyến mãi", "KM", "discount", "giảm giá", "ưu đãi"]
module: marketing
trang_thai: cần product xác nhận — cần bật tiện ích Khuyến mãi
goi: cần product xác nhận
quyen: "cần product xác nhận — Thêm/Sửa/Xóa KM: DV-AD-TONG; xem danh sách/lọc: DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLDV; non-admin chỉ thấy KM chi nhánh mình. Một số thao tác tra cứu (form v2 / tra cứu theo đơn) chỉ yêu cầu đăng nhập (xem dev — khe hở quyền)"
phu_thuoc: ["Tiện ích Khuyến mãi đã mua của đơn vị", "Danh sách khách hàng (hạng/nhóm KH để lọc đối tượng áp dụng)", "Sản phẩm / kho (quy đổi đơn vị tính khi so số lượng)", "Đơn hàng (áp khuyến mãi khi thanh toán + hoàn lượt khi hủy đơn)"]
link_flow: ./flow.md
---

# Quản lý khuyến mãi

## Là gì
Đây là nơi **tạo và quản lý các chương trình khuyến mãi** của shop: giảm giá theo sản phẩm hoặc theo đơn hàng, tặng quà, ưu đãi theo ngưỡng số lượng/giá trị, theo khung giờ vàng, theo hạng/nhóm khách… Khi khách thanh toán đơn, hệ thống tự kiểm tra và **áp khuyến mãi phù hợp**, đồng thời **đếm số lượt đã dùng** để không vượt giới hạn.

Mỗi chương trình khuyến mãi có thể cấu hình: thời gian áp dụng, khung giờ vàng, tổng số lượt cho cả chương trình, số lần mỗi khách được dùng, ngưỡng số lượng/giá trị tối thiểu, đối tượng khách được hưởng.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm chương trình KM | ✅ | DV-AD-TONG |
| Sửa chương trình KM | ✅ | DV-AD-TONG |
| Xóa chương trình KM | ✅ | DV-AD-TONG |
| Xem danh sách / lọc KM | ✅ | DV-AD-TONG/CN/MCN, DV-QLDV (non-admin chỉ thấy KM chi nhánh mình) |
| Xem chi tiết KM | ✅ | tùy quyền |
| Áp KM khi thanh toán đơn | ✅ | tự động khi đủ điều kiện; đơn vị có tiện ích KM |
| Import chương trình KM (Excel) | ✅ | DV-AD-TONG |
| Tra cứu KM khả dụng theo khách / đơn | ✅ | tùy quyền (một số thao tác tra cứu chỉ cần đăng nhập) |

## Để làm gì / cho ai
- **Chủ shop / Marketing:** thiết kế chương trình kích cầu (giảm giá, tặng quà, mua nhiều giảm nhiều) theo dịp.
- **Quản lý chi nhánh:** xem các KM đang áp tại chi nhánh mình.
- **Thu ngân / bán hàng:** khi tính tiền, hệ thống tự gợi ý/áp KM phù hợp cho giỏ hàng của khách.
- **Khách hàng:** được hưởng ưu đãi tự động khi đủ điều kiện.

## Cách dùng
1. Vào **Marketing → Khuyến mãi**.
2. Bấm **Thêm mới** → chọn loại KM (KM sản phẩm hay KM đơn hàng), nhập tên, thời gian áp dụng.
3. Cấu hình điều kiện: khung giờ vàng, ngưỡng số lượng/giá trị tối thiểu, **tổng số lượt** cho chương trình, **số lần mỗi khách**, đối tượng khách (hạng/nhóm).
4. Lưu → chương trình bắt đầu có hiệu lực trong thời gian đã đặt.
5. Khi khách thanh toán đơn đủ điều kiện → hệ thống tự áp ưu đãi và trừ lượt.
6. Có thể **sửa**, **xóa**, **xem danh sách/lọc** theo chi nhánh, hoặc **import** danh sách KM từ Excel.

## Cần chú ý
- **Mỗi nhân viên không phải admin tổng chỉ thấy KM của chi nhánh mình** — đây là do phân quyền, không phải lỗi.
- **Số lượt là "có vào có ra":** mỗi lần áp KM khi thanh toán sẽ trừ lượt; nếu **hủy đơn** thì hệ thống hoàn lại lượt. Nếu hủy đơn mà không hoàn được (lỗi), lượt có thể bị "khóa" oan.
- **Nếu cấu hình "giới hạn số lượt" mà để trống số lượt** → hệ thống coi như đã hết lượt ngay (0 ≥ 0) → KM không áp được. Phải nhập số lượt cụ thể khi bật giới hạn.
- **Khách vãng lai (không có hồ sơ KH)** thường không được hưởng các KM lọc theo hạng/nhóm khách.
- **Khung giờ vàng / hết tồn / tạm ngừng** đều khiến KM tạm thời không áp được cho sản phẩm đó.
- **Xóa/sửa KM tác động cache** — sau khi đổi, hệ thống cần làm mới cache để áp đúng (hệ thống tự xử lý).

## Ảnh hưởng tới
- **Đơn hàng / thanh toán** — KM quyết định giá sau giảm; áp/hoàn lượt gắn với vòng đời đơn.
- **Voucher & Hoa hồng giới thiệu** — xử lý chung trong bước áp ưu đãi khi thanh toán.
- **Lịch sử tương tác khách hàng** — mỗi lần áp KM được ghi nhận.
- **Báo cáo Marketing** — đo hiệu quả chương trình.
