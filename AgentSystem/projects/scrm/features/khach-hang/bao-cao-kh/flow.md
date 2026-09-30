# Flow — Báo cáo chân dung khách hàng

> Luồng dùng + test + đào tạo cho màn **Báo cáo chân dung khách hàng** (route `report/visitor`). Dữ liệu ví dụ là giả lập để minh họa.

## Tiền điều kiện
- Người dùng đã đăng nhập, có 1 trong các quyền: `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, HT-CSKH, DV-QLDV` (quyền màn: `OrderReport_OrderCustomerReportVisitors`).
- Đơn vị đã có dữ liệu khách hàng (giới tính, tỉnh/thành, nhóm, hạng) và đơn hàng trong khoảng thời gian muốn xem.
- Cấu hình "khoảng thời gian cho phép truy vấn" và cấu hình "chi nhánh hiển thị tất cả khách của đơn vị" của đơn vị đã được thiết lập (lấy từ Customer service).

## Luồng dùng (chọn báo cáo → lọc → xem)
1. **Mở màn:** vào module Báo cáo → Báo cáo chân dung khách hàng. Màn khởi tạo, lấy danh sách chi nhánh và thông tin người đăng nhập trước; **chưa tải số liệu** cho tới khi bộ lọc ngày sẵn sàng (sự kiện `changeInit` của control chọn ngày).
2. **Chọn chi nhánh:** nếu là super-user/admin/CSKH (cờ `isSuper`), hiện dropdown chọn chi nhánh; mặc định lấy chi nhánh của người đăng nhập. Người quyền thấp không thấy dropdown chọn chi nhánh khác.
3. **Chọn khoảng thời gian:** qua control chọn ngày + kiểu ngày. Khi đổi → tự gọi tải lại.
4. **Xem kết quả:** hệ thống gọi **2 API song song**:
   - Lượng khách theo ngày + 2 ô tổng (tổng khách, khách mới trong tuần) → vẽ biểu đồ đường.
   - Chân dung (địa chỉ, giới tính, nhóm, hạng) → vẽ 1 biểu đồ tròn địa chỉ, 1 tròn giới tính, 2 cột (nhóm, hạng).
5. **Đổi lọc → lặp lại bước 4.** Mỗi lần đổi chi nhánh hoặc khoảng thời gian, toàn bộ biểu đồ vẽ lại.

## Case biên
- **Khoảng thời gian vượt giới hạn:** hệ thống trả lỗi "Chỉ được xem dữ liệu trong khoảng 'N ngày'" → không vẽ biểu đồ, hiện thông báo lỗi.
- **Không lấy được cấu hình** (Customer service lỗi) → lỗi "Không lấy được cấu hình!" → không có dữ liệu.
- **Người quyền thấp + đơn vị không bật "hiển thị tất cả khách":** dù không chọn chi nhánh, hệ thống ép lọc theo chi nhánh của người đăng nhập → chỉ thấy khách chi nhánh mình.
- **Không có bản ghi tổng** (summary rỗng) ở API lượng khách → trả "Không có bản ghi nào!".
- **Khách thiếu tỉnh/thành:** dồn hết vào nhóm "Không chọn thành phố"; nhiều bản ghi rỗng được FE gộp thành 1 lát trên biểu đồ tròn địa chỉ.
- **Kỳ không có khách:** các biểu đồ rỗng, 2 ô tổng = 0.

## Ví dụ có số
Đơn vị A, chi nhánh "CN Quận 1", khoảng thời gian 01/06 → 07/06.

| Ngày | Tổng khách | Khách lẻ | Khách cũ |
|---|---|---|---|
| 01/06 | 20 | 12 | 8 |
| 02/06 | 18 | 10 | 8 |
| ... | ... | ... | ... |

- Ô tổng: **Tổng số khách hàng = 250**, **Khách mới trong tuần = 35**.
- Biểu đồ giới tính: Nữ 180, Nam 65, Khác 5.
- Biểu đồ hạng: Thường 200, VIP 40, Bạc 10.
- Biểu đồ địa chỉ: (VN) TP.HCM 210, (VN) Bình Dương 25, "Không chọn thành phố" 15.
- Biểu đồ nhóm: "Khách spa" 120, "Khách bán lẻ" 80, "Chưa phân nhóm" 50.

> Tổng các lát ở biểu đồ địa chỉ/giới tính/nhóm/hạng không bắt buộc bằng "Tổng số khách hàng" vì cách đếm và phạm vi (theo lượt mua trong kỳ vs. tổng hồ sơ) khác nhau — đây là điểm cần product xác nhận khi đào tạo.

## Hậu điều kiện
- Không có thay đổi dữ liệu nào (báo cáo chỉ đọc).
- Nếu đơn vị bật "hiển thị query" hoặc người dùng là super-user/CSKH, câu lệnh stored procedure được ghi log (phục vụ debug), không ảnh hưởng nghiệp vụ.
- Người dùng có cái nhìn tổng quan về tệp khách để ra quyết định chăm sóc / marketing.
