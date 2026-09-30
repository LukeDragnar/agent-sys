---
alias: ["kho số", "nhặt số", "lấy số", "chọn số", "pool lead", "xem số", "kho số floating", "kho số thả nổi", "database số"]
module: sales
trang_thai: cần cấu hình
goi: cần product xác nhận
quyen: "cần product xác nhận — để CHỌN SỐ (nhặt lead) gate service yêu cầu 1 trong các role sale: DV-QL-SALE, DV-SALE, DV-SALE-LEAD. (Lưu ý: gate API của endpoint ChonSo lại cấp cho DV-AD-TONG/CN/MCN, DV-SALE, DV-SALE-LEAD — KHÔNG có DV-QL-SALE → 2 danh sách lệch nhau, xem dev.md.) Khởi tạo lại kho số chỉ admin (DV-AD-TONG/CN/MCN). Xem/lọc kho số và xem số tùy cấu hình kho số của đơn vị."
phu_thuoc: ["Cấu hình kho số (bật/tắt dùng kho số, cooldown xem/lấy số, giới hạn số lần/ngày, lọc theo team)", "Contact / khách hàng (đối tượng được gán chủ khi chọn số)", "Tác nghiệp kho số (đánh dấu tác nghiệp thuộc kho số)", "Đơn hàng (cập nhật nhân viên phụ trách lên đơn của contact khi chọn số)"]
link_flow: ./flow.md
---

# Quản lý kho số (pool lead cho sale)

## Là gì
**Kho số** là một "bể" chứa các số điện thoại / lead **chưa có nhân viên phụ trách**. Sale vào kho để **xem số** (lộ số điện thoại của lead) và **chọn số** (nhặt lead đó về mình). Khi đã chọn, lead rời khỏi kho và trở thành khách do sale đó phụ trách. Mục tiêu là chia lead công bằng theo kiểu "ai nhanh tay người đó được", nhưng có nhiều ràng buộc để tránh tranh giành và lạm dụng.

"Floating / thả nổi" nghĩa là các số nằm trong kho chung, chưa thuộc về ai cho đến khi có người nhặt.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem / lọc danh sách kho số | ✅ | tùy cấu hình kho số + phạm vi team/đơn vị của bạn |
| Xem số (lộ số điện thoại lead) | ⚠️ | chỉ khi đơn vị bật "cho dùng icon xem số"; bị giới hạn tần suất (cooldown) mỗi lần xem |
| Chọn số (nhặt lead về mình) | ⚠️ | cần role sale; phải qua nhiều điều kiện (cooldown, giới hạn/ngày, lead chưa chốt đơn/chưa đối soát…) |
| Xem lịch sử xem số | ⚠️ | chỉ khi cấu hình bật hiển thị lịch sử |
| Khởi tạo lại kho số (đơn vị) | ⚠️ | chỉ admin; **bắt buộc TẮT cấu hình "dùng kho số" trước** mới reset được |
| Khởi tạo lại kho số (chi nhánh) | ⚠️ | chỉ admin; tương tự, phải tắt cấu hình kho số của chi nhánh trước |
| Lọc theo nhóm nhân viên | ✅ | combo lọc trên màn kho số |

## Để làm gì / cho ai
- **Nhân viên sale:** có nguồn lead sẵn để chủ động nhặt và chăm sóc, thay vì chờ được chia.
- **Quản lý sale / chủ shop:** phân phối lead theo cơ chế tự nhặt có kiểm soát — đặt cooldown và giới hạn để tránh một người ôm hết, và đảm bảo lead đã chốt/đối soát thì không bị nhặt lại.
- **Admin:** thiết lập cấu hình kho số và khởi tạo lại kho khi cần làm mới.

## Cách dùng
1. Vào **Sale → Kho số**.
2. Dùng ô lọc (theo nhóm…) để xem các số đang có trong kho.
3. Bấm **icon xem số** để lộ số điện thoại của lead (nếu được phép) — lưu ý có giới hạn số lần xem trong một khoảng thời gian.
4. Bấm **Chọn số** để nhặt lead về mình. Nếu đủ điều kiện, lead rời khỏi kho và trở thành khách của bạn; hệ thống tự gán bạn là người phụ trách lên cả đơn hàng liên quan.
5. Có thể mở **Lịch sử xem số** (nếu bật) để xem ai đã xem số nào.
6. (Admin) Khi muốn làm mới toàn bộ kho, **tắt cấu hình dùng kho số** rồi bấm **Khởi tạo lại** theo đơn vị hoặc chi nhánh.

## Cần chú ý
- **Chọn số bị chặn vì nhiều lý do hợp lệ**, không phải lỗi: chưa tới giờ (cooldown), đã vượt số lần chọn trong ngày, lead không thuộc nhóm bạn được phép, lead đã chốt đơn hoặc đã được đối soát… Hệ thống báo rõ lý do từng trường hợp.
- **Xem số có giới hạn tần suất**: mỗi lần xem cách nhau một khoảng phút theo cấu hình; cùng một lead cũng giới hạn số lần mở xem trong ngày.
- **Khởi tạo lại kho là thao tác nguy hiểm**: nó reset hàng loạt cả kho số, chỉ admin làm và **bắt buộc tắt cấu hình dùng kho số trước**. Cân nhắc kỹ vì ảnh hưởng toàn bộ lead trong kho.
- **Chọn số là thao tác có "ripple"**: sau khi nhặt, lead được gán cho bạn trên cả khách hàng, đơn hàng, và đồng bộ sang công cụ tìm kiếm cùng các phần khác của hệ thống. Đây là hành vi đúng.
- Phạm vi lead nhìn thấy/nhặt được phụ thuộc cấu hình "chỉ hiển thị kho số của team" và mốc "sau X phút lead mới mở cho người ngoài team".

## Ảnh hưởng tới
- **Hồ sơ khách hàng / Contact:** lead được gán người phụ trách mới, đánh dấu nguồn phân bổ = "Kho số".
- **Đơn hàng của lead:** nhân viên phụ trách (SaleUserId) được cập nhật lên đơn hàng/đơn logistic liên quan.
- **Tìm kiếm & đồng bộ:** sau khi chọn số, dữ liệu được cập nhật lại trên công cụ tìm kiếm và đồng bộ sang các phần khác của hệ thống (nhóm nhân viên…).
- **Chia Lead / số và Cấu hình Sale:** kho số chịu chi phối bởi các cấu hình chia số và cấu hình kho số.
