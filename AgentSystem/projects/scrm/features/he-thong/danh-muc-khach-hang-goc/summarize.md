---
alias: ["danh mục khách hàng gốc", "nhóm khách hàng hệ thống", "hạng khách hàng hệ thống", "phân loại khách hàng cấp hệ thống", "DM nhóm/hạng KH admin"]
module: he-thong
trang_thai: "cần product xác nhận — đây là phần super-admin/nội bộ SaaS, không cho khách hàng cuối"
goi: "không áp dụng (chỉ admin vận hành hệ thống) — cần product xác nhận"
quyen: "Nhóm KH (bản _SA): chỉ yêu cầu đăng nhập hệ thống (không siết role trong attribute). Hạng KH (bản _SA): role HT-AD (admin hệ thống). — theo registry node"
phu_thuoc: ["Quản lý thông tin khách hàng (KH360) — dùng nhóm/hạng để xếp loại khách", "Luồng thanh toán/bán hàng — cờ tự động xét nâng nhóm khi thanh toán"]
link_flow: ./flow.md
---

# Danh mục khách hàng (gốc — cấp hệ thống)

## Là gì
Đây là nơi **quản trị viên hệ thống (super-admin của nhà cung cấp SaaS)** khai báo bộ **danh mục phân loại khách hàng dùng chung ở cấp hệ thống**, gồm hai loại chính:
- **Nhóm khách hàng** (ví dụ: Khách lẻ, Khách sỉ, Khách thân thiết…): dùng để gom khách theo nhóm; mỗi nhóm có thể bật cờ "tự động xét nâng nhóm khi thanh toán".
- **Hạng khách hàng** (ví dụ: Đồng, Bạc, Vàng, Kim cương…): dùng để xếp hạng khách phục vụ tích điểm / ưu đãi.

Đây là **danh mục gốc cấp hệ thống** — khác với danh mục mà từng đơn vị (shop) tự khai báo trong màn của họ. Phần này nằm trong khu quản trị nội bộ, **không hiển thị cho khách hàng cuối**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm / xem danh sách nhóm KH | ✅ | admin hệ thống đăng nhập |
| Thêm nhóm KH | ✅ | admin hệ thống đăng nhập |
| Sửa nhóm KH | ✅ | admin hệ thống đăng nhập |
| **Xóa nhóm KH** | ❌ | chức năng xóa đã bị tắt — không xóa được qua màn này (hãy tắt "Sử dụng" thay thế) |
| Bật/tắt "Sử dụng" cho nhóm KH | ✅ | admin hệ thống; nhóm tắt sẽ không áp cho khách mới |
| Bật/tắt "Tự động xét nâng nhóm khi thanh toán" | ✅ | admin hệ thống; ảnh hưởng logic bán hàng |
| Tìm / xem danh sách hạng KH | ✅ | role HT-AD |
| Thêm / Sửa hạng KH | ✅ | role HT-AD |
| Lấy toàn bộ hạng KH theo đơn vị | ✅ | role HT-AD |
| **Thay đổi cơ chế xếp hạng (hạng KH)** | ❌ | chức năng đã bị tắt — tính năng chưa bật |

## Để làm gì / cho ai
- **Quản trị viên hệ thống (nhà cung cấp SaaS):** chuẩn hóa bộ nhóm/hạng khách hàng ở cấp hệ thống, để các đơn vị có khung phân loại nhất quán.
- **Gián tiếp phục vụ shop/đơn vị:** nhóm và hạng khai báo ở đây là nền cho việc xếp loại khách trong màn Quản lý khách hàng 360 và cho logic tích điểm, nâng hạng.

## Cách dùng
**Quản lý Nhóm khách hàng (cấp hệ thống):**
1. Vào khu quản trị hệ thống → màn **Danh mục nhóm khách hàng (admin)**.
2. Bấm tìm để xem danh sách nhóm hiện có.
3. Bấm **Thêm mới**, nhập tên nhóm rồi lưu.
4. Có thể bật/tắt **"Sử dụng"** (ẩn nhóm khỏi việc gán cho khách mới) hoặc bật/tắt **"Tự động xét nâng nhóm khi thanh toán"**.
5. Mở một nhóm để xem chi tiết và **sửa**.

**Quản lý Hạng khách hàng (cấp hệ thống):**
1. Vào khu quản trị hệ thống → màn **Danh mục hạng khách hàng (admin)**.
2. Bấm tìm để xem danh sách hạng.
3. Bấm **Thêm mới** / mở một hạng để **sửa**, rồi lưu.

## Cần chú ý
- **Không xóa được nhóm khách hàng** qua màn admin này — chức năng xóa đã bị tắt trong code. Thay vào đó hãy **tắt "Sử dụng"** để ngừng dùng một nhóm.
- **Cờ "Tự động xét nâng nhóm khi thanh toán"**: khi bật, hệ thống sẽ tự xét và nâng nhóm khách ở thời điểm thanh toán; khi tắt thì không tự xét. Đây là thay đổi có ảnh hưởng tới luồng bán hàng — cân nhắc trước khi bật.
- **Phân quyền hai màn khác nhau**: màn Hạng KH siết chặt role HT-AD; màn Nhóm KH (bản admin) trong code chỉ yêu cầu đăng nhập, không siết role trong attribute — *cần product xác nhận* đây có phải chủ ý không.
- Đây là **danh mục gốc cấp hệ thống** (dùng chung), khác với danh mục từng đơn vị tự khai báo — đừng nhầm hai nơi.

## Ảnh hưởng tới
- **Xếp loại khách trong Quản lý khách hàng 360**: nhóm/hạng hiển thị và lọc khách lấy nền từ đây.
- **Logic tích điểm / nâng hạng / nâng nhóm khi thanh toán**: phụ thuộc cấu hình nhóm (cờ tự động xét) và hạng khai báo ở cấp hệ thống.
