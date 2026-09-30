---
alias: ["quản lý website", "nội dung web", "tin tức website", "banner", "domain", "tên miền", "theme", "giao diện web", "chuyên gia"]
module: marketing
trang_thai: cần product xác nhận — cần bật tiện ích Website
goi: cần product xác nhận
quyen: "cần product xác nhận — phần lớn thao tác ghi (thêm/sửa/xóa) yêu cầu DV-AD-TONG; một số thao tác đọc/gán mở rộng cho nhiều role DV (KT/QLDV/NV). Tin tức loại trừ DV-NV ở list. Theme: tìm/gán ở khu vực đơn vị (HT-AD + nhiều role DV), CRUD theme đã chuyển sang bản superadmin"
phu_thuoc: ["Tiện ích Website đã mua của đơn vị", "Domain (tên miền để gán link website)", "Theme (giao diện storefront)", "Trang web công khai (storefront) hiển thị nội dung đã cấu hình"]
link_flow: ./flow.md
---

# Quản lý Website

## Là gì
Đây là nơi quản lý **nội dung và cấu hình trang web (storefront)** của đơn vị/shop, gồm các phần:
- **Tin tức**: bài viết hiển thị trên website (theo nhóm tin).
- **Banner**: ảnh quảng cáo theo vị trí, thời gian hiển thị, link.
- **Domain (tên miền)**: danh mục tên miền của đơn vị để gán cho website.
- **Theme (giao diện)**: chọn/gán giao diện cho website.
- **Chuyên gia**: danh sách chuyên gia (bác sĩ/kỹ thuật viên) hiển thị trên web.

Nội dung khai báo ở đây sẽ được trang web công khai (storefront) hiển thị cho khách.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm / Sửa / Xóa tin tức | ✅ | DV-AD-TONG (list loại trừ DV-NV) |
| Lấy tin theo nhóm / đơn vị | ✅ | nhiều role DV |
| Thêm / Sửa / Xóa banner | ✅ | DV-AD-TONG (đọc mở rộng cho KT/QLDV) |
| Thêm / Sửa / Xóa domain | ✅ | DV-AD-TONG |
| Lấy domain để gán link website | ✅ | DV-AD + KT/QLDV/NV |
| Tìm / gán theme cho website | ✅ | HT-AD + nhiều role DV |
| Thêm / Sửa / Xóa theme | ❌ | đã chuyển sang bản superadmin (CRUD theme ở khu quản trị hệ thống) |
| Thêm / Sửa / Xóa chuyên gia | ✅ | role nhóm DV (dịch vụ/spa) |

## Để làm gì / cho ai
- **Chủ shop / Marketing:** cập nhật nội dung website (tin tức, banner, chuyên gia), gán tên miền và giao diện.
- **Khách hàng cuối:** xem nội dung trên trang web công khai (storefront).
- **Quản trị hệ thống:** quản lý theme gốc (bản superadmin).

## Cách dùng

**Tin tức:**
1. Vào **Website → Quản lý tin tức → Thêm mới**, soạn bài, gắn nhóm tin, lưu.
2. Sửa / Xóa / lọc theo nhóm; tin sẽ hiển thị trên storefront.

**Banner:**
1. Vào **Website → Quản lý banner → Thêm mới**: nhập tên, vị trí, ảnh web + ảnh mobile, thời gian hiển thị, link.
2. Sửa / Xóa per-row.

**Domain & Theme:**
1. Khai báo **domain** (tên miền) ở màn Quản lý domain.
2. Vào màn cấu hình link website → chọn **domain** + chọn **theme** để gán cho website.

**Chuyên gia:**
1. Vào **Website → Quản lý chuyên gia → Thêm mới**, nhập thông tin chuyên gia, lưu.

## Cần chú ý
- **CRUD theme đã chuyển sang bản superadmin** — ở khu vực đơn vị chỉ còn **tìm/gán** theme, không thêm/sửa/xóa theme tại đây.
- **Domain — tên miền là duy nhất toàn hệ thống:** khi thêm domain, hệ thống kiểm tra trùng tên trên **toàn bộ** (không chỉ trong đơn vị) → 2 đơn vị không thể tạo cùng một tên domain. (cần product xác nhận đây có đúng chủ ý không.)
- **Domain dùng chung:** domain để trống đơn vị (dùng chung toàn hệ thống) cũng hiện ra khi gán link website.
- **Banner: ngày kết thúc không được nhỏ hơn ngày bắt đầu** (thông báo lỗi diễn đạt ngược, dễ gây nhầm khi đọc).
- **Vài màn còn thông báo lỗi "Không tìm thấy nhà cung cấp!"** do copy nhầm label — thực chất là domain/banner; không phải bug chức năng.
- **Mỗi đơn vị chỉ thấy nội dung của mình** (lọc theo đơn vị); riêng nội dung chuyên gia/tin tức được dùng để hiển thị ra trang web bán hàng cho khách xem.

## Ảnh hưởng tới
- **Trang web công khai (storefront)** — tin tức/banner/chuyên gia/theme đều render ra đây.
- **Cấu hình link website** — domain + theme quyết định địa chỉ và giao diện web.
- **Đổi/xóa domain hoặc theme đang gán** → ảnh hưởng cấu hình website đang chạy.
