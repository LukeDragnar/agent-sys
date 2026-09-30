---
alias: ["quản lý khách", "hồ sơ khách hàng", "tra cứu khách", "danh sách khách hàng", "khách hàng 360", "thông tin khách hàng"]
module: khach-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — code yêu cầu 1 trong các role để xem danh sách KH 360: DV-AD-TONG, DV-QLDV, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, DV-SALE, DV-QL-SALE, DV-SALE-LEAD, DV-MKT, DV-QL-MKT, DV-MKT-LEAD. Riêng màn Danh mục khách hàng chỉ DV-AD-TONG."
phu_thuoc: ["Danh mục khách hàng (nghề nghiệp, tôn giáo, tình trạng… để chọn khi lọc/khai báo)", "Nhóm / Hạng / Nguồn / Nhãn khách hàng", "Cấu hình đơn vị (che số điện thoại, cho phép xem khách toàn đơn vị, phân quyền Sale/MKT)", "Đơn hàng (số đơn, công nợ, ngày mua gần nhất hiển thị trên hồ sơ)"]
link_flow: ./flow.md
---

# Quản lý thông tin khách hàng

## Là gì
Đây là nơi **lưu và tra cứu toàn bộ hồ sơ khách hàng** của shop/spa: tên, số điện thoại, giới tính, ngày sinh, địa chỉ, nghề nghiệp, nhóm – hạng – nguồn khách, nhân viên Sale/Marketing đang phụ trách, số đơn đã mua, ghi chú… Mỗi khách là một "hồ sơ 360°" — nhìn vào là thấy gần như mọi thông tin liên quan đến khách đó.

Tính năng này gồm 2 màn hình:
- **Danh sách khách hàng**: bảng liệt kê tất cả khách, có ô tìm kiếm và bộ lọc rất chi tiết. Đây là màn hình dùng hằng ngày.
- **Danh mục khách hàng**: nơi khai báo sẵn các "lựa chọn" để gắn cho khách (ví dụ danh sách nghề nghiệp, tôn giáo, tình trạng khách hàng, lý do từ chối…). Khai báo ở đây thì khi nhập/lọc khách mới có sẵn để chọn.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm khách | ✅ | tùy quyền |
| Sửa khách | ✅ | tùy quyền |
| **Xóa khách** | ❌ | tính năng đã ngừng sử dụng (luôn báo lỗi) |
| Nhập từ Excel | ✅ | quyền import |
| Xuất Excel | ✅ | tối đa 5.000 khách/lần, cần quyền xuất |
| Gửi SMS / Zalo | ✅ | tùy quyền |
| Phân bổ khách cho Sale/MKT | ✅ | tùy quyền |
| Mở hồ sơ 360° chi tiết | ✅ | tùy quyền xem |
| (Danh mục KH) Thêm/Sửa/Xóa lựa chọn phân loại | ✅ | chỉ admin tổng đơn vị (DV-AD-TONG) |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** nắm được mình có bao nhiêu khách, khách mới hôm nay, khách sinh nhật trong tháng; xem khách nào mua nhiều, khách nào thuộc nhân viên nào.
- **Nhân viên Sale / Marketing:** tra cứu nhanh khách mình phụ trách, xem thông tin liên hệ, lịch sử, ghi chú để chăm sóc.
- **Kế toán / CSKH:** tìm khách theo số điện thoại, kiểm tra công nợ, thông tin xuất hóa đơn.
- **Người quản trị (admin đơn vị):** khai báo danh mục để chuẩn hóa cách phân loại khách trên toàn shop.

## Cách dùng

**Xem và tìm khách (Danh sách khách hàng):**
1. Vào module **Khách hàng → Danh sách khách hàng**.
2. Gõ tên hoặc số điện thoại vào ô tìm kiếm để tìm nhanh.
3. Bấm vào **bộ lọc** để lọc theo nhiều điều kiện: nhóm/hạng/nguồn khách, giới tính, khu vực (tỉnh/huyện/xã), nghề nghiệp, tình trạng khách, khách mới/cũ, tháng/ngày sinh nhật, khoảng tuổi, số lần mua, nhân viên Sale/MKT phụ trách, khoảng thời gian…
4. Bấm xem → bảng khách hiện ra, có phân trang và sắp xếp theo cột; phía trên có ô thống kê (tổng số khách, khách tạo hôm nay, sinh nhật hôm nay/tháng này).
5. Bấm vào một dòng để **mở hồ sơ chi tiết** của khách đó.
6. Có thể **thêm khách mới**, **sửa**, **nhập danh sách khách từ file Excel**, **xuất Excel**, **gửi SMS/Zalo**, hoặc **phân bổ khách cho Sale/MKT** (tùy quyền).

**Khai báo lựa chọn phân loại (Danh mục khách hàng):**
1. Vào **Khách hàng → Danh mục khách hàng** (cần quyền quản trị đơn vị).
2. Chọn **Kiểu loại** muốn khai báo (Nghề nghiệp, Tôn giáo, Tình trạng khách hàng, Lý do từ chối…).
3. Bấm **Thêm mới**, nhập **Mã**, **Tên**, **Số thứ tự** rồi lưu.
4. Có thể **sửa** hoặc **xóa** các mục đã khai báo.

## Cần chú ý
- **Mỗi người nhìn thấy lượng khách khác nhau tùy quyền.** Nhân viên Sale/MKT thường chỉ thấy khách mình phụ trách; admin/quản lý thấy nhiều hơn hoặc toàn đơn vị. Đây là do cấu hình và phân quyền, không phải lỗi.
- **Số điện thoại có thể bị che** (hiện dạng 09xxxx) nếu đơn vị bật cấu hình che số. Một số trường hợp còn không cho gọi trực tiếp.
- **Không xóa được khách hàng** — tính năng xóa khách đã ngừng sử dụng. Khách chỉ được thêm/sửa, không xóa.
- **Xuất Excel** giới hạn tối đa 5.000 khách mỗi lần, và có thể bị chặn nếu chưa được cấp quyền xuất.
- Ở màn **Danh mục khách hàng**: **Mã** trong cùng một kiểu loại không được trùng, **Tên** cũng không được trùng.

## Ảnh hưởng tới
- **Hồ sơ chi tiết khách hàng**, lịch sử mua, đơn hàng, hóa đơn — đều lấy thông tin gốc từ đây.
- **Chăm sóc khách hàng, gửi SMS/Zalo, chiến dịch Marketing** — chọn khách từ danh sách này.
- **Phân loại khi nhập/sửa khách**: phụ thuộc danh mục đã khai báo. Nếu danh mục trống thì khi nhập khách sẽ không có lựa chọn để chọn.
