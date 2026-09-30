---
alias: ["trạng thái khách hàng", "trạng thái khách", "phân loại khách hoạt động", "tình trạng chăm sóc khách", "danh mục trạng thái KH"]
module: khach-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — code yêu cầu role để xem (Tìm theo điều kiện): DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLDV. Thêm/sửa/xóa trạng thái: chỉ DV-AD-TONG, DV-QLDV. Lấy danh sách trạng thái để gắn cho khách (GetAllTrangThaiKhachHangByDonVi) mở cho hầu hết role nghiệp vụ (Sale, MKT, Kế toán, Kho…)."
phu_thuoc: ["Ngành nghề (mỗi đơn vị thuộc 1 ngành nghề; dữ liệu mẫu trạng thái được khởi tạo theo ngành nghề)", "Khởi tạo đơn vị (khi mở đơn vị mới, hệ thống tự sinh bộ trạng thái mẫu)", "Hồ sơ khách hàng (trạng thái được gắn vào từng khách qua màn hồ sơ KH)", "Báo cáo trạng thái khách hàng (thống kê số khách theo từng trạng thái)"]
link_flow: ./flow.md
---

# Quản lý trạng thái khách hàng

## Là gì
Đây là nơi **khai báo danh sách các "trạng thái" để mô tả tình trạng chăm sóc / vòng đời của khách hàng** trong shop/spa. Ví dụ: *Khách mới*, *Đang tư vấn*, *Đã chốt*, *Đang chăm sóc*, *Ngừng quan tâm*… Mỗi trạng thái có **tên**, **số thứ tự** (để sắp xếp) và **màu nền** (để dễ nhìn khi gắn lên khách).

Đây là một **danh mục cấu hình**: bạn tạo sẵn các trạng thái ở màn hình này, sau đó vào hồ sơ từng khách để gắn trạng thái phù hợp. Khi mở đơn vị mới, hệ thống đã tự tạo sẵn một bộ trạng thái mẫu theo ngành nghề, bạn có thể dùng luôn hoặc chỉnh lại theo ý mình.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm / xem danh sách trạng thái (theo điều kiện) | ✅ | DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLDV |
| Thêm trạng thái (kể cả thêm nhiều dòng) | ✅ | DV-AD-TONG, DV-QLDV; tên không được trùng trong đơn vị |
| Sửa trạng thái | ✅ | DV-AD-TONG, DV-QLDV (lưu ý: sửa không clear cache → tên có thể lệch tạm thời) |
| Cập nhật nhiều bản ghi (sắp xếp/bật-tắt) | ✅ | DV-AD-TONG |
| Xóa trạng thái | ✅ | DV-AD-TONG, DV-QLDV |
| Lấy danh sách để gắn cho khách (dropdown) | ✅ | hầu hết role nghiệp vụ (Sale, MKT, Kế toán, Kho…) |
| Gắn trạng thái cho khách | ✅ | làm ở hồ sơ KH360, không phải ở màn này |
| Cấu hình trạng thái dùng chung (cấp admin hệ thống) | ✅ | HT-AD (nhóm endpoint _SA); riêng Xóa_SA là DV-AD-TONG/DV-QLDV |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** thiết lập "bảng trạng thái" chung cho cả shop để mọi nhân viên phân loại khách thống nhất; nhìn màu là biết khách đang ở giai đoạn nào.
- **Nhân viên Sale / CSKH:** gắn trạng thái cho khách mình phụ trách để theo dõi ai cần chăm, ai đã chốt, ai nên dừng.
- **Marketing:** lọc/báo cáo khách theo trạng thái để lên chiến dịch đúng nhóm.
- **Admin đơn vị:** chuẩn hóa, thêm/bớt trạng thái khi quy trình bán hàng thay đổi.

## Cách dùng
1. Vào module **Khách hàng → Trạng thái khách hàng**.
2. Chọn **Ngành nghề** (nếu shop có nhiều) — danh sách trạng thái hiển thị theo ngành nghề đang chọn.
3. Màn hình hiện bảng các trạng thái hiện có, gồm: **Tên**, **Số thứ tự**, **Màu nền**, **Ngày cập nhật**.
4. Gõ từ khóa vào ô tìm để lọc nhanh theo tên.
5. Bấm **Thêm mới** → nhập **Tên**, chọn **Màu nền**, đặt **Số thứ tự** rồi **Lưu**. Có thể thêm nhiều dòng cùng lúc và **Lưu tất cả**.
6. Sửa trực tiếp tên / màu / số thứ tự trên bảng rồi lưu; hoặc **Xóa** trạng thái không dùng nữa.
7. Sau khi khai báo xong, vào **hồ sơ từng khách** để chọn (gắn) trạng thái cho khách đó.

## Cần chú ý
- **Tên trạng thái không được trùng** trong cùng một shop/đơn vị (kể cả lúc thêm nhiều dòng một lần). Hệ thống sẽ báo lỗi và không lưu nếu trùng.
- **Tên, Số thứ tự, Màu nền đều bắt buộc** khi lưu — bỏ trống sẽ báo lỗi.
- **Số thứ tự** quyết định thứ tự hiển thị (sắp xếp tăng dần). Đặt số nhỏ để đẩy lên đầu.
- **Mỗi đơn vị có bộ trạng thái riêng.** Trạng thái khai báo ở shop này không ảnh hưởng shop khác.
- **Bộ trạng thái mẫu** được tạo tự động khi mở đơn vị (theo ngành nghề). Nếu đơn vị đã có sẵn trạng thái thì hệ thống sẽ không khởi tạo lại để tránh trùng.
- Một khách có thể gắn **nhiều trạng thái** (việc gắn làm ở màn hồ sơ khách, không phải ở màn này).

## Ảnh hưởng tới
- **Hồ sơ khách hàng**: trạng thái khai báo ở đây chính là các lựa chọn để gắn cho từng khách. Nếu chưa khai báo trạng thái nào thì ở hồ sơ khách sẽ không có gì để chọn.
- **Báo cáo trạng thái khách hàng**: thống kê số khách theo từng trạng thái dựa trên danh mục này.
- **Lọc / chăm sóc khách theo nhóm**: việc phân nhóm khách theo giai đoạn chăm sóc dựa vào trạng thái đã gắn.
