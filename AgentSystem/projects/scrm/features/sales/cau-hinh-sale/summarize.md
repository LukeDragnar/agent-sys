---
alias: ["cấu hình sale", "cài đặt sale", "cấu hình contact", "cấu hình đơn hàng sale", "cấu hình kho số", "cấu hình giảm giá đơn hàng", "cấu hình chia contact", "thiết lập bán hàng"]
module: sales
trang_thai: cần cấu hình
goi: cần product xác nhận
quyen: "cần product xác nhận — lưu cấu hình chính thường yêu cầu DV-AD-TONG (admin tổng đơn vị); cấu hình SuperAdmin chỉ HT-AD/HT-CSKH (cấp hệ thống). Một số endpoint bị hạ quyền chỉ cần đăng nhập — xem dev.md."
phu_thuoc: ["Kho số (cấu hình kho số đơn vị/chi nhánh)", "Chia số / chia contact xoay vòng", "Nguồn dữ liệu (cấu hình quản lý nguồn)", "Đơn hàng / OrderLogistic (cấu hình đơn hàng sale, kho tổng, chia số care đơn)", "Khuyến mãi / thanh toán (cấu hình giảm giá theo giá trị đơn)"]
link_flow: ./flow.md
---

# Cấu hình Sale (contact / đơn / giảm giá / ship / kho số)

## Là gì
Đây là **bộ cấu hình tổng cho hoạt động bán hàng của sale** — gom nhiều nhóm cài đặt mà admin đơn vị thiết lập một lần, ảnh hưởng tới cách sale làm việc:
- **Cấu hình Contact**: cấu hình chung của module liên hệ (init, kho số, chia contact xoay vòng, quản lý nguồn dữ liệu, sinh bản ghi CSKH, cho phép tạo contact khi có cuộc gọi đến, chuyển CSKH khi giao hàng…).
- **Cấu hình đơn hàng sale**: cờ điều khiển hành vi đơn hàng cho sale (kho tổng, chia số care đơn, cấu hình nghiệp vụ…).
- **Cấu hình giảm giá theo đơn hàng**: bảng "ngưỡng giá trị đơn → mức giảm" (giảm theo % hoặc theo tiền), tự áp khi thanh toán.
- **Cấu hình quyền sửa field contact** *(bổ sung Pha 2 — CauHinhSuaSo)*: ma trận "field/cột contact → danh sách role được phép sửa". Quyết định nhân viên ở vai trò nào được chỉnh field nào của contact (vd: số điện thoại, nguồn, người phụ trách…) theo từng đơn vị.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Lưu cấu hình init / chung của contact | ✅ | cần DV-AD-TONG |
| Lưu cấu hình kho số (đơn vị / chi nhánh) | ✅ | đơn vị cần DV-AD-TONG; chi nhánh hiện chỉ cần đăng nhập (xem lưu ý) |
| Lưu cấu hình chia contact xoay vòng | ✅ | cần DV-AD-TONG |
| Lưu cấu hình quản lý nguồn dữ liệu | ✅ | cần DV-AD-TONG |
| Lưu cấu hình MKT chia số thủ công | ⚠️ | hiện chỉ cần đăng nhập (key role đã bị bỏ — xem dev.md) |
| Lưu cấu hình đơn hàng sale | ✅ | cần DV-AD-TONG |
| Lưu cấu hình nghiệp vụ order | ⚠️ | hiện tạm mở (chỉ cần đăng nhập) do key đang debug — xem dev.md |
| Lưu cấu hình SuperAdmin (contact/order) | ⚠️ | chỉ HT-AD / HT-CSKH (cấp hệ thống) |
| Thêm / Sửa / Xóa cấu hình giảm giá theo đơn | ✅ | cần role AD-TONG/CN/MCN hoặc QLDV |
| Xem lịch sử thay đổi cấu hình | ✅ | tùy quyền |
| Xem ma trận quyền sửa field contact *(bổ sung Pha 2 — CauHinhSuaSo)* | ✅ | cần DV-AD-TONG / HT-AD / HT-CSKH |
| Lưu ma trận quyền sửa field contact *(bổ sung Pha 2 — CauHinhSuaSo)* | ✅ | cần DV-AD-TONG / HT-AD / HT-CSKH |

## Để làm gì / cho ai
- **Admin đơn vị:** bật/tắt và tinh chỉnh hành vi bán hàng — kho số, chia contact, nguồn dữ liệu, đơn hàng sale, giảm giá tự động — để khớp quy trình của shop.
- **Quản lý sale:** dùng các cờ này để kiểm soát cách lead/đơn được xử lý.
- **Nhân viên sale / thu ngân:** hưởng kết quả gián tiếp — ví dụ giảm giá tự áp khi thanh toán, kho số bật/tắt, contact tự sinh khi có cuộc gọi.
- **Quản trị hệ thống (SuperAdmin):** cấu hình cấp hệ thống vượt trên đơn vị.

## Cách dùng
1. Vào **Cài đặt → Cấu hình Contact** (hoặc Cấu hình Order tùy nhóm), chọn tab tương ứng (init, kho số, chia contact xoay vòng, nguồn dữ liệu, sinh bản ghi CSKH, giao hàng → CSKH…).
2. Chỉnh các cờ/giá trị → **Lưu**. Hệ thống áp dụng cho toàn đơn vị (hoặc chi nhánh).
3. **Cấu hình giảm giá theo đơn**: vào màn cấu hình giảm giá, thêm các dòng "từ giá trị X đến Y → giảm Z% (hoặc Z đồng)". Khi khách thanh toán, hệ thống tự tra mức giảm theo tổng đơn.
4. Cấu hình SuperAdmin nằm ở màn quản trị riêng (chỉ HT-AD/HT-CSKH).
5. **Cấu hình quyền sửa field contact** *(bổ sung Pha 2 — CauHinhSuaSo)*: vào màn cấu hình ma trận sửa số, mỗi field/cột của contact tích chọn các role được phép sửa → **Lưu**. Hệ thống áp cho toàn đơn vị; sau này khi nhân viên mở contact, chỉ field mà vai trò của họ được gán mới cho chỉnh.

## Cần chú ý
- **Một lần lưu cấu hình kho số chi nhánh chạm 2 phạm vi**: lưu cấu hình kho số theo chi nhánh sẽ tự đồng bộ lại cấu hình kho số cấp đơn vị (gom các chi nhánh active). Đổi 1 chỗ ảnh hưởng cả đơn vị.
- **Cấu hình giảm giá không được chồng khoảng**: hai dòng có khoảng giá trị giao nhau sẽ bị chặn ("Cấu hình giá trị đã được thiết lập"). Lưu ý ràng buộc kiểm tra chồng lấn hiện chưa phủ hết trường hợp lồng nhau — xem dev.md.
- **Giảm giá kiểu %**: không quá 100%. **Giảm giá kiểu tiền**: mức giảm phải nhỏ hơn "giá trị từ".
- **Nhiều cấu hình đồng bộ chéo module**: cấu hình đơn hàng đồng bộ 2 chiều giữa Contact ↔ OrderLogistic ↔ Invoice; cấu hình kho tổng/chia số care đơn được phần phân bổ đơn đọc lại.
- **Một số màn cấu hình hiện quyền lỏng** (chỉ cần đăng nhập do key bị tạm bỏ/đang debug) — đây là lưu ý kỹ thuật cho dev, không nên lộ ra UI ngoài quản trị.

## Ảnh hưởng tới
- **Kho số:** cấu hình kho số đơn vị/chi nhánh quyết định kho số hoạt động thế nào (cooldown, giới hạn, team).
- **Chia Lead / số:** cấu hình chia contact xoay vòng + nhận data ảnh hưởng việc chia lead.
- **Đơn hàng:** cờ đơn hàng sale, kho tổng, chia số care đơn áp cho luồng tạo/phân bổ đơn.
- **Thanh toán:** cấu hình giảm giá tự áp mức giảm theo tổng giá trị đơn khi thu ngân.
- **CSKH:** cấu hình sinh bản ghi CSKH, chuyển CSKH khi giao hàng.
