---
alias: ["quản lý user", "quản lý tài khoản", "quản lý nhân viên", "tài khoản người dùng", "khóa tài khoản", "đổi mật khẩu user", "đồng bộ user"]
module: he-thong
trang_thai: cần product xác nhận — dành cho admin hệ thống / CSKH nội bộ SaaS
goi: cần product xác nhận
quyen: "cần product xác nhận — đa số thao tác là admin hệ thống (HT-AD); một số dùng chung HT-CSKH (tìm & chuyển đơn vị). Một số endpoint nội bộ chỉ super-admin/HT-CSKH (check trong service)."
phu_thuoc: ["Phân quyền & quản lý quyền (user gắn role/permission)", "Quản lý đơn vị & hợp đồng (user thuộc đơn vị/chi nhánh)", "Danh mục ngành nghề & domain (gán theo đơn vị khi tạo user)"]
link_flow: ./flow.md
---

# Quản lý tài khoản & người dùng

## Là gì
Đây là phần **nội bộ của nhà cung cấp SCRM (admin hệ thống / CSKH)** dùng để **quản trị tài khoản nhân viên trên toàn hệ thống**: tạo tài khoản mới, sửa thông tin, xóa, đổi mật khẩu, khóa/mở khóa tài khoản, và **đồng bộ lại user theo đơn vị** khi dữ liệu bị lệch giữa các service. Ngoài ra có công cụ cho CSKH **tìm đơn vị/chi nhánh** theo user hoặc theo mã đơn để hỗ trợ chuyển đổi đơn vị.

Mỗi tài khoản gắn với một **đơn vị (tenant)** và **chi nhánh**; khi tạo/sửa, hệ thống tự lấy ngành nghề và domain theo đơn vị đó.

> Đây KHÔNG phải màn tự đăng ký của khách. Khách thường chỉ đổi mật khẩu của chính mình ở phần cá nhân; còn tạo/khóa/xóa tài khoản nhân viên là việc của admin.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm tài khoản (user) | ✅ | admin hệ thống (HT-AD); nếu gắn đơn vị → đơn vị phải tồn tại |
| Sửa thông tin tài khoản | ✅ | admin hệ thống (HT-AD) |
| Xóa tài khoản | ✅ | admin hệ thống (HT-AD) |
| Đổi mật khẩu | ✅ | admin hệ thống (HT-AD) |
| Khóa / Mở khóa tài khoản | ✅ | admin hệ thống (HT-AD); trạng thái Hoạt động ↔ Khóa |
| Tìm tài khoản theo điều kiện / theo đơn vị | ✅ | HT-AD (một số HT-CSKH) |
| Tìm đơn vị/chi nhánh theo user hoặc mã đơn | ✅ | nội bộ — service tự chặn chỉ super-admin & HT-CSKH |
| Đồng bộ lại user theo đơn vị | ✅ | super-admin (re-sync khi user lệch giữa các service) |
| Gán role cho user | ✅ | xử lý kèm khi Thêm/Sửa user (qua phân quyền), không phải màn riêng |

## Để làm gì / cho ai
- **Admin hệ thống:** tạo và quản trị toàn bộ tài khoản nhân viên, khóa tài khoản nghỉ việc, reset mật khẩu khi quên.
- **CSKH nội bộ (HT-CSKH):** tra cứu user thuộc đơn vị/chi nhánh nào, tìm theo mã đơn để hỗ trợ khách chuyển đổi đơn vị.
- **Super-admin:** đồng bộ lại user theo đơn vị khi dữ liệu user bị lệch giữa các service (Core và các service khác).

## Cách dùng
1. Vào **Hệ thống → Quản lý nhân viên / tài khoản**.
2. Bảng liệt kê tài khoản; dùng ô lọc để tìm theo điều kiện hoặc theo đơn vị.
3. **Thêm**: nhập thông tin tài khoản; nếu chọn đơn vị thì hệ thống tự lấy ngành nghề + domain của đơn vị đó. Không chọn đơn vị thì tài khoản được gán domain theo môi trường hệ thống.
4. **Sửa**: mở chi tiết, chỉnh thông tin và lưu.
5. **Đổi mật khẩu / Khóa / Mở khóa / Xóa**: chọn tài khoản rồi bấm nút tương ứng.
6. **Đồng bộ user theo đơn vị** (super-admin): chạy khi nghi ngờ dữ liệu user của một đơn vị bị lệch; có thể xem trạng thái tiến trình đồng bộ.

## Cần chú ý
- **Tạo tài khoản có gắn đơn vị mà đơn vị không tồn tại → hệ thống báo không tìm thấy**, không tạo được. Kiểm tra đơn vị trước.
- **Tài khoản không gắn đơn vị** sẽ được set đơn vị/chi nhánh/ngành nghề rỗng và **domain theo môi trường** (production/staging/sprint/dev). Đây là tài khoản kiểu hệ thống, không thuộc shop nào.
- **Khóa tài khoản** chặn user đăng nhập; mở khóa để dùng lại.
- **Đồng bộ user theo đơn vị** là thao tác diện rộng (đẩy user sang nhiều service) — chỉ chạy khi thực sự cần, theo dõi trạng thái để biết đã xong chưa.
- Một số chức năng tra cứu/đồng bộ **không chặn quyền ở tầng ngoài mà kiểm tra trong service** — nhờ dev nếu cần hiểu rõ ai gọi được.

## Ảnh hưởng tới
- **Đăng nhập & phân quyền:** tạo/sửa/khóa tài khoản đổi ngay khả năng đăng nhập và quyền của user.
- **Đơn vị & chi nhánh:** user gắn đơn vị nào thì thấy dữ liệu đơn vị đó.
- **Các service khác:** tạo/sửa/đồng bộ user sẽ đẩy thông tin user sang các service liên quan (đồng bộ chéo).
