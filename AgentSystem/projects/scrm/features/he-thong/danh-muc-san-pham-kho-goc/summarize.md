---
alias: ["danh mục sản phẩm gốc", "sản phẩm hệ thống", "danh mục kho gốc", "DM kho hệ thống", "nhóm sản phẩm hệ thống", "dịch vụ hệ thống", "master data sản phẩm/kho"]
module: he-thong
trang_thai: "cần product xác nhận — phần super-admin/nội bộ SaaS, không cho khách hàng cuối"
goi: "không áp dụng (chỉ admin vận hành hệ thống) — cần product xác nhận"
quyen: "role HT-AD (admin hệ thống), thuộc nhóm tiện ích hệ thống — theo source khu quản trị"
phu_thuoc: ["Ngành nghề (nhiều màn lọc/khởi tạo theo idNganhNghe)", "Quản lý sản phẩm cấp đơn vị (danh mục gốc là nền để đơn vị dùng/khởi tạo)", "Quản lý tồn kho (danh mục kho)"]
link_flow: ./flow.md
---

# Danh mục sản phẩm & kho (gốc — cấp hệ thống)

## Là gì
Đây là khu **quản trị hệ thống (super-admin)** để khai báo **bộ danh mục gốc về sản phẩm, dịch vụ, nhóm và kho ở cấp hệ thống** — làm dữ liệu mẫu/dùng chung cho toàn bộ các đơn vị (shop). Bao gồm:
- **Sản phẩm hệ thống** (`SanPham` cấp hệ thống) và **Nhóm sản phẩm hệ thống**.
- **Danh mục sản phẩm hệ thống** (DMSanPham) — phân loại sản phẩm.
- **Dịch vụ hệ thống** và **Nhóm dịch vụ hệ thống**.
- **Danh mục kho hệ thống** (DMKho).

Phần lớn màn lọc/khởi tạo theo **ngành nghề** (idNganhNghe). Đây là **danh mục cấp hệ thống**, nằm trong khu quản trị nội bộ, **không hiển thị cho khách hàng cuối**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm / xem sản phẩm hệ thống | ✅ | role HT-AD |
| Thêm / Sửa / Xóa sản phẩm hệ thống | ✅ | role HT-AD |
| Dropdown sản phẩm theo ngành nghề | ✅ | role HT-AD |
| Tìm / Thêm / Sửa / Xóa nhóm sản phẩm hệ thống | ✅ | role HT-AD |
| Tìm / Thêm / Sửa / Xóa danh mục sản phẩm (DMSanPham) | ✅ | role HT-AD |
| Tìm / Thêm / Sửa / Xóa dịch vụ hệ thống | ✅ | role HT-AD |
| Tìm / Thêm / Sửa / Xóa nhóm dịch vụ hệ thống | ✅ | role HT-AD |
| Tìm / Thêm / Sửa / Xóa danh mục kho (DMKho) | ✅ | role HT-AD |
| Quản lý danh mục Combo / Thẻ cấp hệ thống | ❌ | chưa có màn quản lý Combo/Thẻ ở cấp hệ thống (chỉ có ở cấp đơn vị) |

## Để làm gì / cho ai
- **Quản trị viên hệ thống (nhà cung cấp SaaS):** dựng và bảo trì bộ danh mục sản phẩm/dịch vụ/nhóm/kho gốc theo từng ngành nghề, làm khung chuẩn cho các đơn vị.
- **Gián tiếp phục vụ shop/đơn vị:** danh mục gốc là nền để đơn vị tham chiếu, khởi tạo và phân loại sản phẩm/kho của riêng họ.

## Cách dùng
1. Vào khu **quản trị hệ thống** → chọn màn cần quản lý (Sản phẩm / Nhóm sản phẩm / Danh mục SP / Dịch vụ / Nhóm dịch vụ / Danh mục kho).
2. Hầu hết màn lọc theo **ngành nghề** — chọn ngành nghề trước khi xem danh sách.
3. Bấm tìm để xem danh sách, **Thêm mới** để tạo mục mới, mở chi tiết để **sửa**, hoặc **xóa** mục không dùng.
4. Các màn nhóm/danh mục có **dropdown** dùng để chọn khi gắn vào sản phẩm/dịch vụ.

## Cần chú ý
- Tất cả màn ở đây là **cấp hệ thống (HT-AD)** — khác hoàn toàn với danh mục sản phẩm/kho mà từng đơn vị tự quản lý. Đừng nhầm hai nơi.
- **Danh mục kho hệ thống**: màn quản trị danh mục kho ở khu hệ thống mới là nơi có đủ chức năng thêm/sửa/xóa; đừng nhầm với màn quản lý tồn kho thực tế ở đơn vị. (Chi tiết kỹ thuật xem dev.md.)
- **Combo / Thẻ dịch vụ** chưa có màn quản lý cấp hệ thống — chỉ có ở cấp đơn vị. *Cần product xác nhận* nếu cần bổ sung.

## Ảnh hưởng tới
- **Quản lý sản phẩm / dịch vụ / kho cấp đơn vị**: danh mục gốc là nền tham chiếu và khởi tạo.
- **Phân loại sản phẩm theo nhóm / danh mục / ngành nghề** trong các màn đơn vị: phụ thuộc danh mục đã khai báo ở cấp hệ thống.
