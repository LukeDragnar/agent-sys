---
alias: ["báo cáo khách hàng", "chân dung khách hàng", "phân tích khách hàng", "báo cáo lượng khách", "thống kê khách hàng"]
module: khach-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — code yêu cầu 1 trong các role: DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, HT-CSKH, DV-QLDV. Người không phải admin/super-user/CSKH/kế toán thường chỉ thấy dữ liệu chi nhánh của mình."
phu_thuoc: [Customer (hồ sơ khách hàng — giới tính, địa chỉ/tỉnh thành, nhóm khách, hạng khách), Order (đơn hàng — để xác định khách mới/cũ/lẻ theo lượt mua), Report service (kiểm soát giới hạn khoảng thời gian + phân quyền chi nhánh), cấu hình "chi nhánh hiển thị tất cả khách của đơn vị"]
link_flow: ./flow.md
---

# Báo cáo & phân tích khách hàng

## Là gì
Là **nhóm các báo cáo giúp bạn "nhìn ra" tệp khách hàng của mình**: có bao nhiêu khách, khách mới và khách cũ thế nào, khách là nam hay nữ, ở tỉnh thành nào, thuộc nhóm/hạng nào. Màn hình chính là **Báo cáo chân dung khách hàng** — vẽ bức tranh tổng quan về tập khách hàng bằng các biểu đồ trực quan.

Tất cả số liệu được tính tại thời điểm bạn bấm xem, dựa trên dữ liệu khách hàng và đơn hàng thực tế trong khoảng thời gian đã chọn.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo chân dung / số lượng khách cũ-mới | ✅ | DV-AD-TONG/CN/MCN, DV-KT, DV-KT-TONG, HT-AD, HT-CSKH, DV-QLDV; non-admin bị ép theo chi nhánh của mình |
| Chọn chi nhánh / khoảng thời gian để lọc | ✅ | tùy quyền xem nhiều chi nhánh; bị chặn nếu khoảng ngày vượt giới hạn cấu hình đơn vị |
| **Thêm / Sửa / Xóa dữ liệu** | ❌ | báo cáo chỉ đọc, không tạo/sửa/xóa khách hàng hay đơn |
| Xuất Excel báo cáo | ⚠️ | chưa xác nhận có nút xuất ở màn chân dung (registry không thấy endpoint export) |
| (Báo cáo KH theo đơn / lượt khách cũ) | ❌ | endpoint đã bị vô hiệu (toàn bộ action comment trong KhachHangReportController) |

## Để làm gì / cho ai
- **Cho chủ shop / quản lý:** hiểu khách của mình là ai (giới tính, vùng miền, nhóm, hạng) để chọn đúng cách chăm sóc, đúng chương trình khuyến mãi, đúng kênh quảng cáo.
- **Cho marketing:** biết tệp khách tập trung ở đâu, nhóm nào đông để nhắm chiến dịch.
- **Cho CSKH:** theo dõi lượng khách mới mỗi ngày, khách quay lại (khách cũ) để đánh giá hiệu quả giữ chân khách.
- **Actor:** chủ đơn vị, quản lý chi nhánh, kế toán, CSKH. Mỗi báo cáo yêu cầu quyền tương ứng.

## Cách dùng
1. Vào module **Báo cáo** → mở **Báo cáo chân dung khách hàng**.
2. Chọn **chi nhánh** (nếu bạn có quyền xem nhiều chi nhánh) và **khoảng thời gian** muốn xem.
3. Hệ thống tự tải số liệu và vẽ các biểu đồ: tổng số khách, khách mới trong tuần, biểu đồ lượng khách theo ngày, phân nhóm, giới tính, hạng khách, địa chỉ.
4. Đổi chi nhánh hoặc đổi khoảng thời gian → báo cáo tự cập nhật lại.

> Lưu ý: hệ thống giới hạn khoảng thời gian được phép truy vấn theo cấu hình của đơn vị (xem mục Cần chú ý).

## Các loại phân tích
1. **Lượng khách hàng theo ngày** — biểu đồ đường: mỗi ngày có bao nhiêu lượt khách, tách ra **khách lẻ** và **khách cũ** (khách quay lại). Kèm 2 ô tổng: **tổng số khách hàng** và **khách mới trong tuần**.
2. **Phân nhóm khách hàng** — biểu đồ cột: mỗi nhóm khách (nhóm do đơn vị tự đặt) có bao nhiêu khách.
3. **Giới tính** — biểu đồ tròn: tỷ lệ khách nam / nữ / khác.
4. **Cấp độ (hạng) khách hàng** — biểu đồ cột: số khách theo từng hạng (ví dụ thường, VIP…).
5. **Địa chỉ** — biểu đồ tròn: phân bố khách theo tỉnh/thành; khách chưa chọn tỉnh được gộp vào "Không chọn thành phố".

> Ngoài chân dung khách hàng, module còn các báo cáo phân tích khác liên quan đến khách: **thống kê khách hàng dùng sản phẩm** và **lịch sử dùng thẻ dịch vụ** (cần product xác nhận phạm vi gói).

## Cần chú ý
- **Giới hạn khoảng thời gian:** đơn vị có cấu hình "chỉ được xem dữ liệu trong khoảng N ngày". Chọn khoảng dài hơn sẽ báo lỗi và không cho xem.
- **Phân quyền theo chi nhánh:** nếu bạn không phải admin/super-user/CSKH/kế toán và đơn vị không bật "hiển thị tất cả khách", báo cáo chỉ tính khách của chi nhánh bạn — kể cả khi không chọn chi nhánh nào.
- **Khách mới / khách cũ / khách lẻ** được phân loại dựa trên lịch sử mua hàng, không phải tự khai báo. Một khách lần đầu mua là khách mới; quay lại lần sau là khách cũ.
- **Địa chỉ phụ thuộc khách có khai tỉnh/thành hay không** — khách thiếu thông tin sẽ dồn vào "Không chọn thành phố", nên biểu đồ địa chỉ chỉ chính xác khi hồ sơ khách được nhập đầy đủ.
- **Số liệu là ảnh chụp tại thời điểm xem** — chạy lại lúc khác có thể khác nếu có khách mới hoặc đơn hàng mới phát sinh.

## Ảnh hưởng tới
Báo cáo này **chỉ đọc, không thay đổi dữ liệu** — không tạo, sửa hay xóa khách hàng. Nó **phản ánh** kết quả của các nghiệp vụ khác:
- **Tạo / cập nhật hồ sơ khách hàng** (giới tính, tỉnh thành, nhóm, hạng) → ảnh hưởng các biểu đồ phân nhóm, giới tính, hạng, địa chỉ.
- **Bán hàng / tạo đơn** → ảnh hưởng số khách mới, khách cũ, lượng khách theo ngày.
- **Cấu hình nhóm / hạng khách hàng** → ảnh hưởng cách gộp và hiển thị các biểu đồ tương ứng.

Nếu số liệu báo cáo trông sai (ví dụ biểu đồ địa chỉ trống nhiều), gốc rễ thường nằm ở việc hồ sơ khách chưa được nhập đầy đủ, không phải ở màn báo cáo.
