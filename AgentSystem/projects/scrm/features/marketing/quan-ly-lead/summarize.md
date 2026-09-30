---
alias: ["quản lý lead", "khách tiềm năng", "hồ sơ khách hàng lead", "lead MKT", "hồ sơ KH marketing", "danh sách lead"]
module: marketing
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — màn Hồ sơ khách hàng (lead) chỉ cho vai admin/marketing (AD/MKT/QL-MKT/CHIASO; đếm trùng số chỉ DV_MKT); Dashboard MKT cho admin tổng/chi nhánh + quản lý/lead/nhân viên MKT; danh sách số Seeding cho admin + quản lý/lead Sale & MKT. Quyền gán cứng một phần trong code — cần product xác nhận chi tiết."
phu_thuoc: ["Contact / khách hàng (lead lấy từ contact tiếp nhận)", "Quản lý nguồn dữ liệu (landing) — lead gắn nguồn/landing", "Danh sách số Seeding (lọc số nội bộ/test khỏi lead thật)", "Cấu hình chia số / chia chat (lead vào được phân bổ cho nhân viên)"]
link_flow: ./flow.md
---

# Quản lý Lead

## Là gì
Đây là nơi **theo dõi và xử lý khách tiềm năng (lead)** mà bộ phận Marketing thu được từ các kênh quảng cáo, landing page, mạng xã hội… Mỗi lead là một liên hệ (contact) đổ về kèm thông tin nguồn — landing nào, chiến dịch nào, ngày nào.

Tính năng này gồm vài phần liên quan nhau:
- **Hồ sơ khách hàng (lead MKT):** lưới liệt kê các lead đổ về, có tìm kiếm/lọc, đếm các lead **trùng số điện thoại**, và xuất Excel danh sách.
- **Dashboard Marketing:** màn thống kê nhanh phục vụ Marketing — theo dõi **lịch sử thao tác dữ liệu của nhân viên** (ai đụng gì vào dữ liệu lead, theo khoảng ngày và loại hoạt động).
- **Danh sách số Seeding:** danh sách số điện thoại nội bộ / test (số "seeding") để hệ thống **loại các số này khỏi lead thật**, tránh đếm nhầm số ảo thành khách.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách hồ sơ KH (lead) | ✅ | vai admin/marketing (AD/MKT/QL-MKT/CHIASO) |
| Đếm lead trùng số điện thoại | ✅ | vai marketing (DV-MKT) |
| Xuất Excel danh sách lead (kiểu 1 / kiểu 2) | ✅ | chỉ cần đăng nhập (chưa gắn quyền riêng — xem dev) |
| Xem lịch sử thao tác dữ liệu (Dashboard MKT) | ✅ | admin tổng/chi nhánh + quản lý/lead/nhân viên MKT |
| Thêm số Seeding | ✅ | admin + quản lý/lead Sale & MKT |
| Tìm / lọc số Seeding | ✅ | như trên |
| Xóa số Seeding (1 / nhiều) | ✅ | như trên |
| Kiểm tra một số có phải số Seeding | ✅ | chỉ cần đăng nhập |
| Sửa / Xóa lead trực tiếp ở màn này | ❌ | màn lead là **báo cáo chỉ đọc** — không sửa/xóa lead ở đây; chỉnh sửa làm ở Quản lý Contact / khách hàng |
| Tự động nhận lead từ landing page / website (Ladi, WooCommerce) | ✅ | _(bổ sung Pha 2 — lead intake tự động)_ khách điền form trên landing → hệ thống tự nhận, không cần thao tác tay; **không cần đăng nhập** (cổng nhận công khai) |

## Để làm gì / cho ai
- **Quản lý Marketing:** nhìn tổng số lead đổ về theo nguồn/landing, phát hiện lead trùng số, theo dõi nhân viên thao tác dữ liệu.
- **Nhân viên Marketing:** tra danh sách lead mình phụ trách, xuất ra Excel để chăm sóc.
- **Quản trị/Sale:** khai báo số Seeding (số test/nội bộ) để không bị tính vào lead thật, tránh thổi phồng số liệu.

## Cách dùng
**Xem danh sách lead (Hồ sơ khách hàng):**
1. Vào màn **Hồ sơ khách hàng** (lead MKT) trong khu vực Marketing/Landing.
2. Lọc/tìm theo điều kiện → lưới lead hiện ra; phần trên có badge **số lead trùng số điện thoại**.
3. Bấm **Xuất Excel** để tải danh sách (có 2 kiểu xuất khác nhau về cách trình bày).

**Theo dõi thao tác dữ liệu (Dashboard MKT):**
1. Vào **Marketing → Dashboard**.
2. Chọn khoảng ngày + loại hoạt động → xem lịch sử ai đã thao tác gì trên dữ liệu lead.

**Khai báo số Seeding:**
1. Vào **Danh sách số Seeding**.
2. **Thêm mới** số điện thoại nội bộ/test → lưu. Có thể **xóa một hoặc nhiều** số.
3. Khi có số mới đổ về, hệ thống tự **kiểm tra** số đó có nằm trong danh sách seeding không để loại khỏi lead thật.

**Lead vào tự động từ landing page / website** _(bổ sung Pha 2 — lead intake tự động)_:
1. Bạn dựng landing page (LadiPage) hoặc website (WooCommerce) có form thu thông tin khách (tên / số điện thoại / nhu cầu...).
2. Cấu hình form gửi dữ liệu về **cổng nhận lead** của hệ thống (kỹ thuật/đối tác làm phần kết nối; mỗi landing có mã định danh riêng).
3. Khách điền và gửi form → hệ thống **tự nhận, ghi nhận và tạo lead/contact** mà bạn không phải nhập tay; lead này hiện trong màn Hồ sơ khách hàng kèm nguồn/landing tương ứng.
4. Số nằm trong Danh sách Seeding sẽ tự bị loại; lead trùng số được badge trùng số ghi nhận.

## Cần chú ý
- **Màn Hồ sơ khách hàng là báo cáo chỉ đọc** — chỉ xem/đếm/xuất, không sửa hay xóa lead tại đây. Muốn sửa thông tin thì vào Quản lý Contact / khách hàng.
- **Mỗi người thấy lượng lead khác nhau tùy vai trò** (admin thấy nhiều, nhân viên MKT thấy lead trong phạm vi mình). Đây là phân quyền, không phải lỗi.
- **Số Seeding không phải khách thật** — khai báo đúng để báo cáo lead chính xác; số seeding bị loại khỏi luồng tạo contact / chia số.
- **Lead trùng số** thường do cùng một khách điền nhiều landing/nhiều lần — badge đếm trùng giúp phát hiện, nhưng việc gộp/xử lý làm ở màn Contact.
- **Xuất Excel hai kiểu** (kiểu 1 / kiểu 2) khác nhau cách trình bày — chọn đúng kiểu theo nhu cầu báo cáo.

## Ảnh hưởng tới
- **Quản lý Contact / khách hàng** — lead chính là contact; sửa/gộp/chăm sóc làm ở đó.
- **Quản lý nguồn dữ liệu (landing)** — lead gắn với landing/nguồn; nguồn sai thì thống kê lead sai.
- **Báo cáo Marketing** — số lead theo nguồn/sản phẩm lấy từ chính dòng dữ liệu lead này.
- **Chia số / chia chat** — lead sạch (đã loại seeding) mới được phân bổ cho nhân viên chăm sóc.
