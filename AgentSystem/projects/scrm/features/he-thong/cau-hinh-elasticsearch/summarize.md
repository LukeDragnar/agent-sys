---
alias: ["quản lý elastic search", "cấu hình elasticsearch", "reindex ES", "đánh index", "đồng bộ ES", "reindex đơn vị", "đánh lại index"]
module: he-thong
trang_thai: cần product xác nhận — chỉ super-admin SaaS (nội bộ), không cho khách hàng cuối
goi: cần product xác nhận
quyen: "Nội bộ SaaS — code yêu cầu role hệ thống HT-AD (admin hệ thống) / HT-CSKH cho phần lớn endpoint; riêng nút Reindex toàn bộ đơn vị chỉ tài khoản SuperUser mới chạy được. Không phải quyền của admin đơn vị/khách hàng."
phu_thuoc: ["Elasticsearch cluster (dịch vụ tìm kiếm ngoài)", "RabbitMQ (hàng đợi để chạy đánh index nền)", "Dữ liệu nguồn cần đánh index: contact, kho số, đơn hàng logistic, đơn hàng, sản phẩm"]
link_flow: ./flow.md
---

# Cấu hình Elasticsearch

## Là gì
Đây là màn hình **nội bộ của đội vận hành SaaS** để khai báo đơn vị (shop) nào được dùng công cụ tìm kiếm nhanh **Elasticsearch (ES)**, và để **đánh lại dữ liệu tìm kiếm (reindex)** hoặc **xóa dữ liệu tìm kiếm (xóa index)** cho từng đơn vị, theo từng loại dữ liệu (danh bạ/contact, kho số điện thoại, đơn hàng logistic, đơn hàng, sản phẩm).

Hiểu nôm na: hệ thống có một "thư mục tra cứu siêu nhanh" riêng cho việc tìm kiếm. Màn này quyết định đơn vị nào dùng thư mục đó, và khi dữ liệu lệch thì bấm cho hệ thống **dựng lại thư mục tra cứu** từ dữ liệu gốc.

> Đây là phần super-admin/nội bộ — khách hàng cuối **không thấy** màn này. Khách chỉ cảm nhận kết quả: tìm kiếm nhanh và đúng hơn.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm/sửa đơn vị được dùng ES | ✅ | admin hệ thống (HT-AD/HT-CSKH) |
| Xem danh sách đơn vị dùng ES | ✅ | admin hệ thống |
| Đổi phương thức tìm kiếm của đơn vị | ✅ | admin hệ thống |
| Reindex (đánh lại) 1 loại dữ liệu | ✅ | admin hệ thống; chọn đơn vị cụ thể hoặc để trống = tất cả đơn vị |
| Reindex nhiều loại dữ liệu theo đơn vị | ✅ | admin hệ thống; tick từng loại muốn đánh lại |
| Xóa index nhiều loại dữ liệu | ✅ | admin hệ thống (HT-AD) |
| **Reindex toàn bộ đơn vị (full)** | ⚠️ | **chỉ tài khoản SuperUser**; tài khoản khác bấm sẽ bị chặn |
| Khách hàng cuối tự cấu hình ES | ❌ | đây là chức năng nội bộ vận hành, không mở cho khách |

## Để làm gì / cho ai
- **Đội vận hành / kỹ thuật SaaS:** bật ES cho đơn vị mới, hoặc khi phát hiện kết quả tìm kiếm của một shop bị thiếu/sai (dữ liệu tìm kiếm lệch so với dữ liệu thật) thì bấm reindex để dựng lại.
- **CSKH nội bộ:** xử lý khi khách báo "tìm khách/đơn không ra" mà dữ liệu thực tế có — thường do index chưa cập nhật, reindex lại là xong.

## Cách dùng
1. Vào **Hệ thống → Quản lý Elastic search** (cần quyền admin hệ thống).
2. **Khai báo đơn vị dùng ES:** thêm/sửa đơn vị, chọn các loại dữ liệu đơn vị đó sẽ dùng ES.
3. **Khi cần đánh lại dữ liệu tìm kiếm:** chọn đơn vị (hoặc để trống = tất cả), tick loại dữ liệu (contact / kho số / đơn logistic / đơn hàng / sản phẩm), bấm **Reindex**.
4. Hệ thống **không chạy ngay tại chỗ** mà đẩy việc vào hàng đợi để xử lý nền — nên sau khi bấm, dữ liệu tìm kiếm sẽ cập nhật dần (vài giây đến vài phút tùy lượng dữ liệu).
5. **Xóa index:** khi muốn dọn sạch dữ liệu tìm kiếm của đơn vị (vd ngừng dùng ES), chọn đơn vị + loại dữ liệu, bấm **Xóa index**.
6. **Reindex full** (dựng lại toàn bộ mọi đơn vị) chỉ dùng khi thật cần và chỉ tài khoản SuperUser chạy được.

## Cần chú ý
- **Reindex chạy nền, không tức thì.** Bấm xong là "đã nhận lệnh", còn dữ liệu tìm kiếm cập nhật sau đó ít phút. Đừng bấm nhiều lần liên tục vì tưởng chưa chạy.
- **Để trống danh sách đơn vị = áp cho TẤT CẢ đơn vị.** Cẩn thận khi reindex loại dữ liệu lớn (đơn hàng/sản phẩm) cho toàn hệ thống — tốn tài nguyên.
- **Reindex full chỉ SuperUser.** Tài khoản admin thường bấm sẽ bị từ chối.
- **Đánh lại sản phẩm** thực tế dựng lại 2 phần (sản phẩm + sản phẩm cha) trong cùng một lệnh.
- Loại **"đơn hàng" (order)** đang bị bỏ ở một số lệnh đánh-hàng-loạt (đã tắt trong code) — nếu reindex gói "tất cả loại" mà đơn hàng không cập nhật thì là do điểm này, *cần product/kỹ thuật xác nhận có cần bật lại không*.

## Ảnh hưởng tới
- **Mọi màn hình tìm kiếm nhanh** (tìm contact, đơn, sản phẩm, kho số): kết quả lấy từ dữ liệu index này. Index lệch → kết quả tìm thiếu/sai.
- **Không đụng dữ liệu gốc.** Reindex chỉ dựng lại bản sao phục vụ tìm kiếm; xóa index chỉ xóa bản sao tìm kiếm, không xóa dữ liệu thật trong hệ thống.
