---
alias: ["danh mục hành chính", "địa giới hành chính", "tỉnh huyện xã", "quốc gia tỉnh quận phường", "danh mục địa chỉ", "DM địa giới"]
module: he-thong
trang_thai: "cần product xác nhận — phần super-admin/nội bộ SaaS, không cho khách hàng cuối"
goi: "không áp dụng (chỉ admin vận hành hệ thống) — cần product xác nhận"
quyen: "role HT-AD và HT-CSKH (quản trị hệ thống / CSKH hệ thống) — theo registry node; các endpoint đọc kế thừa (LayTatCa/TimTheoId) chỉ cần đăng nhập"
phu_thuoc: []
---

# Danh mục hành chính (địa giới — cấp hệ thống)

## Là gì
Đây là nơi **quản trị viên hệ thống** khai báo và bảo trì **cây địa giới hành chính dùng chung** cho toàn hệ thống, theo 4 cấp:

**Quốc gia → Tỉnh/Thành phố → Quận/Huyện → Phường/Xã**

Bộ dữ liệu này là **nguồn gốc cho mọi ô nhập địa chỉ** trong hệ thống (địa chỉ khách hàng, đơn vị/chi nhánh, đơn hàng, giao vận…). Đây là **danh mục cấp hệ thống**, nằm trong khu quản trị nội bộ, **không hiển thị cho khách hàng cuối**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm / xem danh sách (4 cấp) | ✅ | HT-AD, HT-CSKH |
| Thêm mới (Quốc gia / Tỉnh / Quận-Huyện / Phường-Xã) | ✅ | HT-AD, HT-CSKH; cần chọn cấp cha tương ứng |
| Sửa | ✅ | HT-AD, HT-CSKH |
| **Xóa** | ⚠️ | HT-AD, HT-CSKH; **không chặn xóa khi còn cấp con tham chiếu** (chỉ ràng buộc ở DB nếu có FK) → dễ để lại dữ liệu mồ côi |
| Lấy dropdown cascade (Tỉnh theo Quốc gia, Quận theo Tỉnh) | ✅ | HT-AD, HT-CSKH |
| Lấy cây địa chỉ tổng hợp (cho màn nhập địa chỉ) | ✅ | đăng nhập (có endpoint private cho service nội bộ) |

## Để làm gì / cho ai
- **Quản trị viên hệ thống:** duy trì danh mục địa giới chuẩn, cập nhật khi có thay đổi đơn vị hành chính.
- **Gián tiếp phục vụ mọi người dùng:** mỗi khi nhập địa chỉ (khách, đơn vị, đơn hàng), dropdown Tỉnh/Quận/Xã lấy dữ liệu từ đây.

## Cách dùng
1. Vào khu **quản trị hệ thống** → màn danh mục địa giới (Quốc gia / Tỉnh-TP / Quận-Huyện / Phường-Xã).
2. Bấm tìm để xem danh sách theo cấp.
3. **Thêm mới**: chọn cấp cha (ví dụ thêm Tỉnh phải chọn Quốc gia; thêm Quận phải chọn Tỉnh; thêm Phường-Xã phải chọn Tỉnh + Quận), nhập **Mã** và **Tên** rồi lưu.
4. Mở một mục để **sửa** hoặc **xóa**.

## Cần chú ý
- **Thêm cấp con bắt buộc chọn cấp cha**: Tỉnh cần Quốc gia, Quận/Huyện cần Tỉnh, Phường/Xã cần Tỉnh + Quận. Thiếu cấp cha → báo lỗi.
- **Mã và Tên không được trùng** — nhưng chỉ kiểm tra khi **thêm mới**. Khi **sửa** thì hệ thống **không kiểm tra trùng lại**, nên có thể vô tình sửa thành trùng với mục khác cùng cấp cha (lưu ý vận hành).
- **Xóa không chặn khi còn cấp con / địa chỉ đang tham chiếu** — xóa một Tỉnh đang có Quận/Huyện con có thể để lại dữ liệu con mồ côi nếu DB không ràng buộc. Hãy kiểm tra trước khi xóa.
- (Quốc gia) Có một lỗi kiểm tra đầu vào: điều kiện validate "trống" bị viết nhầm nên **không thực sự bắt buộc nhập Tên** khi thêm Quốc gia — *xem dev.md*.

## Ảnh hưởng tới
- **Mọi ô nhập địa chỉ toàn hệ thống**: khách hàng, đơn vị/chi nhánh, đơn hàng, giao vận — dropdown địa chỉ cascade lấy từ đây.
- Sửa/xóa một cấp ảnh hưởng dropdown ở tất cả màn nhập địa chỉ liên quan.
