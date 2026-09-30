---
alias: ["cấu hình chung hệ thống", "tham số hệ thống", "system config", "quản lý cấu hình", "config hệ thống", "domain cors", "whitelist domain"]
module: he-thong
trang_thai: cần product xác nhận — chức năng nội bộ super-admin SaaS
goi: cần product xác nhận
quyen: "Nội bộ SaaS (super admin) — code yêu cầu role hệ thống (gợi ý HT-AD) cho CRUD cấu hình và quản lý domain CORS; riêng XÓA cấu hình chỉ tài khoản SuperUser. Một endpoint đọc config app là công khai (anonymous) để giao diện lấy key cấu hình khi khởi động."
phu_thuoc: ["Bảng cấu hình hệ thống (ConfigSystem) — mọi module đọc tham số từ đây", "Danh sách domain CORS (DomainCors) — whitelist origin được gọi API", "Từ điển cấu hình mobile (MobileDictionary) cho nhãn/cấu hình app"]
link_flow: ./flow.md
---

# Cấu hình chung hệ thống

## Là gì
Đây là nơi **khai báo các tham số dùng chung cho toàn hệ thống** — gọi là cấu hình hệ thống (system config). Mỗi tham số là một cặp **mã (code) → giá trị**, gom theo nhóm. Ví dụ: key tích hợp bản đồ, tài khoản ngân hàng mặc định, cờ bật/tắt một tính năng (ẩn/hiện điện thoại trên mobile)… Đổi giá trị ở đây sẽ ảnh hưởng hành vi của nhiều màn hình khác.

Tính năng này gồm:
- **Quản lý cấu hình (ConfigSystem):** xem/thêm/sửa/xóa các tham số hệ thống, gom theo nhóm và sắp xếp dạng cây.
- **Quản lý domain CORS:** danh sách "domain được phép gọi API" (whitelist). Thêm/sửa/xóa domain ở đây để cho phép hoặc chặn một website gọi vào hệ thống.
- (Liên quan) **Từ điển cấu hình mobile** cho nhãn/cấu hình app — xem feature "Quản lý ngôn ngữ".

> Đây là phần super-admin/nội bộ — khách hàng cuối **không thấy** màn này.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách / cây cấu hình | ✅ | admin hệ thống |
| Thêm tham số cấu hình | ✅ | admin hệ thống |
| Sửa tham số cấu hình | ✅ | admin hệ thống |
| **Xóa tham số cấu hình** | ⚠️ | **chỉ tài khoản SuperUser** |
| Đọc 1 config theo mã (cho service/màn khác) | ✅ | nội bộ |
| Đọc config app (key bản đồ…) | ✅ | công khai — giao diện tự lấy khi khởi động |
| Thêm domain CORS (whitelist) | ✅ | admin hệ thống; tự cập nhật lại CORS toàn cục ngay |
| Sửa domain CORS | ✅ | admin hệ thống; tự cập nhật lại CORS toàn cục |
| Xóa domain CORS | ✅ | admin hệ thống; tự cập nhật lại CORS toàn cục |
| Khách hàng cuối tự cấu hình hệ thống | ❌ | đây là chức năng nội bộ super-admin |

## Để làm gì / cho ai
- **Đội vận hành / kỹ thuật SaaS:** chỉnh tham số toàn hệ thống mà không cần đổi code (key tích hợp, cờ bật/tắt tính năng, thông tin mặc định…).
- **Quản trị bảo mật truy cập:** quản lý danh sách domain được phép gọi API (CORS) — mở cho website đối tác/triển khai mới, hoặc gỡ domain không còn dùng.

## Cách dùng
**Quản lý cấu hình hệ thống:**
1. Vào **Quản trị → Quản lý cấu hình**.
2. Xem danh sách (lưới hoặc cây theo nhóm). Mở 1 dòng để sửa giá trị.
3. Thêm tham số mới: nhập mã, giá trị, nhóm… rồi lưu.
4. Xóa: chỉ tài khoản SuperUser mới xóa được.

**Quản lý domain CORS:**
1. Vào **Quản trị → Quản lý domain CORS**.
2. Xem danh sách domain đang được phép.
3. Thêm/sửa/xóa domain. Mỗi lần thay đổi, hệ thống **tự cập nhật lại cấu hình CORS toàn cục ngay** — không cần khởi động lại.

## Cần chú ý
- **Đổi một tham số cấu hình có thể ảnh hưởng nhiều màn hình.** Ví dụ cờ ẩn/hiện điện thoại, key tích hợp… Hãy hiểu rõ tham số trước khi sửa.
- **Xóa tham số chỉ SuperUser.** Admin thường không xóa được — đây là chủ ý để tránh xóa nhầm tham số quan trọng.
- **Thêm/xóa domain CORS có hiệu lực ngay** (cập nhật CORS toàn cục liền). Xóa nhầm domain đang dùng → website đó **bị chặn gọi API** ngay lập tức.
- Nếu cập nhật CORS toàn cục gặp lỗi sau khi đã ghi danh sách domain → có thể **lệch giữa danh sách đã lưu và cấu hình thực thi** (xem dev.md). *Cần kỹ thuật lưu ý.*
- Một endpoint **đọc config app là công khai** (giao diện cần key bản đồ trước khi đăng nhập) — chỉ nên để key dùng phía client, không để lộ thông tin bí mật phía máy chủ.

## Ảnh hưởng tới
- **Nhiều module đọc tham số từ cấu hình hệ thống** → đổi giá trị ảnh hưởng hành vi nhiều nơi (ẩn/hiện field, tích hợp, thông tin mặc định).
- **Mọi service kiểm tra origin** áp danh sách domain CORS → thêm/xóa domain quyết định website nào gọi API được.
