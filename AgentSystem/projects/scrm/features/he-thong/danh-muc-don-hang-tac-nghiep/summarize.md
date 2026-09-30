---
alias: ["danh mục đơn hàng gốc", "loại đơn hàng hệ thống", "danh mục tác nghiệp hệ thống", "master data đơn hàng", "DM đơn hàng/tác nghiệp admin"]
module: he-thong
trang_thai: "cần product xác nhận — phần super-admin/nội bộ SaaS, không cho khách hàng cuối"
goi: "không áp dụng (chỉ admin vận hành hệ thống) — cần product xác nhận"
quyen: "Danh mục đơn hàng (QuanTri/SandBox): internal, tiện ích superadmin (TI_Chung cờ isAdmin). Danh mục tác nghiệp (bản hệ thống QuanTriArea): role HT-AD, HT-CSKH — theo registry node"
phu_thuoc: ["Cấu hình tác nghiệp (DMTacNghiepCauHinh) — dùng tác nghiệp làm bước follow-up", "Dropdown loại đơn hàng dùng ở các màn đơn hàng đơn vị"]
link_flow: ./flow.md
---

# Danh mục đơn hàng & tác nghiệp (gốc — cấp hệ thống)

## Là gì
Khu **quản trị hệ thống (super-admin)** để khai báo **danh mục dùng chung về đơn hàng và tác nghiệp**:
- **Danh mục đơn hàng** (DMDonHang): các loại đơn hàng master data, dùng chung toàn hệ thống — là nguồn cho dropdown loại đơn ở các màn đơn vị.
- **Danh mục tác nghiệp** (DMTacNghiep): các bước thao tác/follow-up (gọi điện, hẹn lịch, chốt…) dùng để chăm sóc kho số/đơn. Có bản hệ thống (QuanTriArea, HT-AD) và bản đơn vị riêng.

Đây là **danh mục cấp hệ thống**, nằm trong khu quản trị nội bộ, **không hiển thị cho khách hàng cuối**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm / xem danh mục đơn hàng | ✅ | quản trị hệ thống (superadmin) |
| Thêm / Sửa danh mục đơn hàng | ✅ | quản trị hệ thống |
| Xem chi tiết danh mục đơn hàng | ✅ | đăng nhập (key rỗng) |
| **Xóa danh mục đơn hàng** | ⚠️ | quản trị hệ thống; sau khi xóa hệ thống mới làm mới (clear) cache dùng chung |
| Tìm / xem danh mục tác nghiệp (hệ thống) | ✅ | HT-AD, HT-CSKH |
| Thêm / Sửa / Xóa danh mục tác nghiệp (hệ thống) | ✅ | HT-AD, HT-CSKH |
| Lọc tác nghiệp theo ngành nghề | ✅ | quản trị hệ thống |

## Để làm gì / cho ai
- **Quản trị viên hệ thống:** chuẩn hóa loại đơn hàng và bộ bước tác nghiệp dùng chung, làm khung cho các đơn vị.
- **Gián tiếp phục vụ shop/đơn vị:** dropdown loại đơn và danh mục tác nghiệp là nền cho luồng tạo đơn và cấu hình follow-up chăm sóc.

## Cách dùng
**Danh mục đơn hàng (quản trị):**
1. Vào khu quản trị hệ thống → màn Danh mục đơn hàng → tìm để xem danh sách.
2. **Thêm mới** / mở chi tiết để **sửa** / **xóa** loại đơn không dùng.

**Danh mục tác nghiệp (quản trị hệ thống):**
1. Vào khu quản trị hệ thống → màn Danh mục tác nghiệp (bản hệ thống).
2. Thêm/sửa/xóa các bước tác nghiệp; có thể lọc theo ngành nghề.

## Cần chú ý
- **Hai biến thể, đừng nhầm**: Danh mục đơn hàng có bản **quản trị hệ thống** (thêm/sửa/xóa đầy đủ) và bản dùng chung chỉ để **đổ dropdown loại đơn**. Danh mục tác nghiệp có bản **hệ thống** (HT-AD/HT-CSKH) và bản **đơn vị** (do shop quản lý). Phần kỹ thuật chi tiết xem dev.md.
- **Cập nhật danh mục đơn hàng có thể chưa hiện ngay ở dropdown**: chỉ thao tác **xóa** mới tự làm mới cache dùng chung; thêm/sửa có thể phải chờ lần làm mới cache kế tiếp.
- Danh mục tác nghiệp có cờ đặc biệt: **bước khởi đầu, khóa số, sửa giờ** — xem flow để hiểu ảnh hưởng.

## Ảnh hưởng tới
- **Luồng tạo đơn hàng** ở đơn vị: dropdown loại đơn lấy từ danh mục đơn hàng (qua cache hệ thống).
- **Cấu hình follow-up / chăm sóc kho số**: `DMTacNghiepCauHinh` dùng danh mục tác nghiệp làm bước cần / bước tiếp theo.
