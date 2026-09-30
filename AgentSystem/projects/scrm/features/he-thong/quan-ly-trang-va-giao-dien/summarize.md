---
alias: ["quản lý trang", "cấu hình trang", "cấu hình menu", "page web", "page mobile", "cây trang", "phân quyền trang", "key mobile"]
module: he-thong
trang_thai: cần product xác nhận — dành cho admin hệ thống / nội bộ SaaS
goi: cần product xác nhận
quyen: "cần product xác nhận — CRUD trang web & page mobile chủ yếu admin hệ thống (HT-AD) và quản trị mobile (HT-MB). App mobile đọc key chỉ cần đăng nhập."
phu_thuoc: ["Phân quyền & quản lý quyền (Page gán role → role mới thấy trang)", "Quản lý tài khoản & người dùng (user mang role để hiện menu)"]
link_flow: ./flow.md
---

# Quản lý trang & giao diện

## Là gì
Đây là phần **nội bộ của nhà cung cấp SCRM (admin hệ thống / quản trị mobile)** dùng để định nghĩa **danh sách màn hình (trang) của web và app, cùng menu và quyền truy cập từng trang**. Gồm hai mảng:
- **Quản lý trang web (Page):** các trang/màn web được tổ chức **dạng cây** (trang cha – trang con). Thêm/sửa/xóa trang, gán **role nào được vào trang chủ (home page)**. Đây là nguồn dữ liệu để dựng menu web và quyết định ai thấy màn nào.
- **Quản lý Page Mobile:** danh sách các màn (key) của app di động, cũng gán role theo từng màn. App mobile gọi để lấy **danh sách màn được phép** theo quyền user (có bản mã hóa cho client). Còn có **import từ Excel**, **backup**, **tải file mẫu**.

> Đây KHÔNG phải màn cho khách hàng cuối. Khách chỉ thấy kết quả: menu hiện ra những trang họ được phép theo role.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem cây trang web | ✅ | admin hệ thống (HT-AD), quản trị mobile (HT-MB) |
| Thêm / Sửa / Xóa trang web | ✅ | HT-AD, HT-MB |
| Gán role vào trang chủ (home page) | ✅ | kèm khi xem/sửa trang |
| Thêm / Sửa / Xóa page mobile | ✅ | HT-AD, HT-MB |
| Gán role cho từng page mobile | ✅ | kèm khi thêm/sửa page mobile |
| Thêm key mobile | ✅ | đăng nhập (có bản mã hóa) |
| Import page mobile từ Excel | ✅ | HT-AD, HT-MB; file phải có dòng hợp lệ |
| Backup / Tải file mẫu page mobile | ✅ | HT-AD, HT-MB |
| App mobile đọc danh sách màn được phép | ✅ | đăng nhập (theo quyền user) |
| Thêm/Sửa/Xóa page mobile ở màn app dùng (bản chỉ đọc) | ❌ | màn app chỉ để đọc; thêm/sửa/xóa nằm ở màn quản trị |

## Để làm gì / cho ai
- **Admin hệ thống:** dựng và bảo trì danh sách màn/menu web, quyết định cấu trúc cây trang và ai thấy trang nào.
- **Quản trị mobile (HT-MB):** quản lý danh sách màn app, import hàng loạt page mobile từ Excel, backup.
- **App mobile (gián tiếp cho user):** khi mở app, hệ thống lấy danh sách màn (key) user được phép để chỉ hiển thị đúng phần đó.

## Cách dùng
**Trang web:**
1. Vào **Quản trị → Quản lý trang**.
2. Xem **cây trang** (chọn loại trang nếu có), bấm vào một nút để **Sửa** hoặc **Thêm** trang con; có thể gán role được vào trang chủ.
3. **Xóa** trang không dùng (thao tác này sẽ làm mới lại menu cho mọi người).

**Page mobile:**
1. Vào **Quản trị → Quản lý Page Mobile**.
2. Lưới liệt kê các màn mobile; **Thêm/Sửa/Xóa** và gán role cho từng màn.
3. Cần nhập hàng loạt: tải **file mẫu** → điền → **Import**. Có thể **Backup** toàn bộ ra Excel.

## Cần chú ý
- **Xóa trang web sẽ xóa cache menu** → menu web được dựng lại cho mọi user. Đây là hành vi đúng, nhưng nghĩa là thao tác xóa có ảnh hưởng diện rộng.
- **Sửa trang web KHÔNG tự làm mới cache menu ở tầng controller** — nếu menu cũ còn hiển thị sau khi sửa, có thể do cache chưa được dọn (xem dev.md). Cần product/dev xác nhận hành vi mong muốn.
- **Có hai bản Page Mobile**: bản **chỉ đọc** (cho app gọi) và bản **quản trị** (CRUD + import + backup). Đừng nhầm — sửa dữ liệu phải dùng bản quản trị.
- **File backup và file mẫu import page mobile trùng tên** (`PageMobile_Backup.xlsx`) — dễ lẫn khi tải về (known-issue).
- **Import page mobile** yêu cầu file có dòng hợp lệ; file rỗng/không có dòng sẽ báo lỗi.

## Ảnh hưởng tới
- **Menu & quyền truy cập màn:** trang gán cho role nào thì role đó mới thấy; đổi trang → đổi menu/quyền vào màn.
- **App mobile:** thay đổi danh sách page mobile → app đọc danh sách màn mới.
- **Phân quyền:** kết hợp với role — trang là một phần của hệ phân quyền truy cập.
