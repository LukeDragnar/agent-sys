---
alias: ["danh mục khác", "nguồn đặt lịch hệ thống", "danh mục ngành nghề", "lĩnh vực kinh doanh", "nhóm ngành nghề", "danh mục gốc khác"]
module: he-thong
trang_thai: "cần product xác nhận — phần super-admin/nội bộ SaaS, không cho khách hàng cuối"
goi: "không áp dụng (chỉ admin vận hành hệ thống) — cần product xác nhận"
quyen: "Nguồn đặt lịch (bản _SA): role HT-AD. Ngành nghề (QuanTriArea): role HT-AD. Lĩnh vực (QuanTriArea): đa số HT-AD, vài endpoint đọc chỉ cần login. Nhóm ngành nghề: KHÔNG gắn key (mở) — theo registry node"
phu_thuoc: ["Đặt lịch / Booking (nguồn đặt lịch là template cho đơn vị copy về)", "Cấu hình đơn vị theo ngành nghề / lĩnh vực", "Tiện ích theo lĩnh vực (bộ tính năng khả dụng cho đơn vị)"]
link_flow: ./flow.md
---

# Danh mục khác (gốc — cấp hệ thống)

## Là gì
Nhóm các **danh mục gốc cấp hệ thống còn lại** mà quản trị viên hệ thống khai báo, gồm:
- **Nguồn đặt lịch** (bản hệ thống): danh sách kênh/nguồn khách đặt lịch (Trực tiếp, Website…) — là **template** để mỗi đơn vị "Khởi tạo mặc định" copy về.
- **Ngành nghề** (NganhNghe): danh mục ngành nghề kinh doanh; nền để khởi tạo dữ liệu mẫu cho đơn vị mới theo ngành.
- **Lĩnh vực** (LinhVuc) và cấu hình **tiện ích theo lĩnh vực**: quyết định đơn vị thuộc lĩnh vực nào được bật những tính năng (tiện ích) nào.
- **Nhóm ngành nghề** (NhomNganhNghe): phân cấp trên ngành nghề.

Đây là **danh mục cấp hệ thống**, nằm trong khu quản trị nội bộ, **không hiển thị cho khách hàng cuối**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm / Thêm / Sửa nguồn đặt lịch (hệ thống) | ✅ | HT-AD; thao tác trên dữ liệu gốc (IdDonVi rỗng) |
| Đặt một nguồn đặt lịch làm "mặc định" | ✅ | HT-AD; mặc định duy nhất trong phạm vi hệ thống |
| **Xóa nguồn đặt lịch lõi** (Trực tiếp / Website) | ❌ | bị chặn (Ma = TRUCTIEP/WEBSITE không cho xóa) |
| Xóa nguồn đặt lịch hệ thống khác | ⚠️ | HT-AD; chỉ xóa được nguồn cấp hệ thống (không xóa nguồn của đơn vị) |
| Tìm / Thêm / Sửa / Xóa ngành nghề | ✅ | HT-AD |
| Tìm / Thêm / Sửa / Xóa lĩnh vực | ✅ | HT-AD (vài thao tác xem dữ liệu chỉ cần đăng nhập) |
| Gán bộ tiện ích cho lĩnh vực | ✅ | HT-AD; quyết định tính năng khả dụng cho đơn vị thuộc lĩnh vực |
| Tra nhóm ngành nghề | ✅ | mở (không gate role — lưu ý bảo mật) |

## Để làm gì / cho ai
- **Quản trị viên hệ thống:** dựng bộ danh mục nền (nguồn đặt lịch, ngành nghề, lĩnh vực) và quyết định bộ tiện ích theo lĩnh vực.
- **Gián tiếp phục vụ shop/đơn vị:** đơn vị copy nguồn đặt lịch mẫu về, được khởi tạo dữ liệu theo ngành nghề, và được bật/tắt tính năng theo lĩnh vực.

## Cách dùng
**Nguồn đặt lịch (hệ thống):**
1. Vào khu quản trị → màn Nguồn đặt lịch (admin) → tìm danh sách (dữ liệu gốc IdDonVi rỗng).
2. **Thêm mới** / **sửa** / đặt **mặc định** / xóa (trừ nguồn lõi).

**Ngành nghề / Lĩnh vực:**
1. Vào khu quản trị → màn Ngành nghề hoặc Lĩnh vực.
2. Thêm/sửa/xóa danh mục; với Lĩnh vực có thêm bước **gán bộ tiện ích** cho lĩnh vực.

## Cần chú ý
- **Nguồn đặt lịch là template, không tự đồng bộ ngược**: sửa/xóa nguồn ở cấp hệ thống **KHÔNG** tự cập nhật về các đơn vị đã copy trước đó.
- **Không xóa được nguồn lõi**: nguồn có Mã `TRUCTIEP` hoặc `WEBSITE` bị chặn xóa.
- **Mặc định duy nhất**: chỉ một nguồn đặt lịch hệ thống được đặt mặc định tại một thời điểm.
- **Lĩnh vực ↔ tiện ích**: gán tiện ích cho lĩnh vực ảnh hưởng trực tiếp tới **bộ tính năng đơn vị thuộc lĩnh vực đó được dùng** — thay đổi cẩn thận.
- **Nhóm ngành nghề hiện không siết quyền theo vai trò** (ai đăng nhập cũng truy cập được) — *cần product xác nhận* và lưu ý bảo mật.

## Ảnh hưởng tới
- **Đặt lịch ở đơn vị**: dropdown nguồn đặt lịch của đơn vị bắt nguồn từ template hệ thống (qua "Khởi tạo mặc định").
- **Khởi tạo dữ liệu mẫu cho đơn vị mới** theo ngành nghề.
- **Bộ tính năng (tiện ích) khả dụng** của đơn vị theo lĩnh vực.
