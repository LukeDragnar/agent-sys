---
alias: ["đơn đặt hàng", "don dat hang", "đặt hàng nhập", "đơn mua hàng", "DDH", "PO", "purchase order", "đặt mua hàng từ NCC", "tạo đơn đặt hàng"]
module: mua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — có 2 nhóm quyền tách bạch: quyền DUYỆT (cho pha duyệt) và quyền TẠO/xử lý mua (cho pha xử lý). Quyền tạo đơn theo chi nhánh cấu hình từ module Kho/WarehouseProduct. Vai cụ thể: cần product xác nhận."
phu_thuoc: ["Quản lý nhà cung cấp (chọn NCC trên đơn)", "Quản lý sản phẩm / Kho / Chi nhánh", "Cấu hình duyệt (CanKiemDuyet — đơn vị có bật bước duyệt hay không)", "Hóa đơn/Phiếu mua hàng (tạo phiếu mua hàng từ đơn)", "Quản lý tồn (dự báo) — nguồn tạo đơn"]
link_flow: ./flow.md
---

# Quản lý đơn đặt hàng

## Là gì
Đây là nơi **lập và theo dõi đơn đặt hàng (PO) để nhập hàng từ nhà cung cấp**. Mỗi đơn gồm thông tin nhà cung cấp, chi nhánh nhận, và danh sách sản phẩm cần mua kèm số lượng. Đơn đi qua **hai pha**:
- **Pha duyệt**: Nháp → Chờ duyệt → Đã duyệt (hoặc Không duyệt / Trình lại). Nếu đơn vị không bật duyệt thì đơn đi thẳng sang pha xử lý.
- **Pha xử lý mua**: Chờ xử lý → Đang xử lý → Hoàn thành. Trong pha này có thể **tạo phiếu mua hàng** từ đơn; khi đã nhận đủ hàng, đơn tự chuyển sang Hoàn thành.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm đơn đặt hàng | ✅ | cần ≥1 sản phẩm (SL>0), có NCC + chi nhánh; cần quyền tạo theo chi nhánh |
| Sửa đơn | ⚠️ | chỉ khi đơn ở trạng thái **Nháp** hoặc **Trình lại** |
| Xóa đơn | ⚠️ | chỉ khi **Nháp**; là xóa hẳn |
| Xem chi tiết đơn | ✅ | tùy quyền |
| Duyệt / Không duyệt / Trình lại | ⚠️ | cần quyền **duyệt** (pha 1) |
| Xử lý / Tạm dừng / Hoàn thành / Hủy | ⚠️ | cần quyền **tạo/xử lý mua** (pha 2) |
| Cập nhật thanh toán (đánh dấu đã thanh toán) | ✅ | tùy quyền |
| Tạo phiếu mua hàng từ đơn | ✅ | đơn đang ở Chờ xử lý / Đang xử lý / Tạm dừng |
| Xem danh sách phiếu mua hàng theo đơn | ✅ | tùy quyền |
| Nhập đơn đặt hàng từ Excel | ✅ | qua bảng tạm, có kiểm tra hợp lệ |

## Để làm gì / cho ai
- **Bộ phận mua hàng / quản lý kho:** lập đơn đặt hàng cho hàng sắp hết, theo dõi tiến độ nhập về.
- **Quản lý / người duyệt:** kiểm soát chi tiêu mua hàng qua bước duyệt trước khi thực mua.
- **Kế toán / kho:** từ đơn đặt hàng tạo phiếu mua hàng, ghi nhận hàng về và công nợ nhà cung cấp.

## Cách dùng
1. Vào **Kho → Quản lý đơn đặt hàng**.
2. Bấm **Thêm mới**: chọn nhà cung cấp, chi nhánh nhận, thêm các dòng sản phẩm cần mua và số lượng → lưu (đơn vào trạng thái Nháp hoặc gửi duyệt tùy cấu hình).
3. Nếu đơn vị bật duyệt: người có quyền duyệt bấm **Duyệt** (đơn tự chuyển sang Chờ xử lý), hoặc **Không duyệt** / **Trình lại**.
4. Người mua hàng **Xử lý** đơn, bấm **Tạo phiếu mua hàng** để nhập hàng về; có thể **Tạm dừng** hoặc **Hủy** đơn.
5. Khi nhận đủ hàng, đơn **tự chuyển Hoàn thành**.
6. Có thể **Nhập từ Excel** nhiều đơn cùng lúc.

## Cần chú ý
- **Chỉ sửa được khi Nháp/Trình lại; chỉ xóa được khi Nháp** — đơn đã đi vào quy trình thì khóa chỉnh sửa.
- **Trạng thái "Đã duyệt" không bao giờ được lưu lại** — duyệt xong là tự chuyển sang Chờ xử lý.
- **Duyệt và xử lý mua là hai quyền khác nhau** — người duyệt chưa chắc xử lý mua được và ngược lại.
- **Hủy đơn có thể kéo theo hủy các phiếu mua hàng liên quan** (nếu chọn tùy chọn hủy kèm). Lưu ý: thao tác hủy kèm này không phải lúc nào cũng "trọn gói" nếu có nhiều phiếu (xem dev).
- **Xóa đơn là xóa hẳn**, không khôi phục được.
- Nếu đơn vị **không bật duyệt** (`CanKiemDuyet=false`), đơn bỏ qua bước Chờ duyệt và vào thẳng Chờ xử lý.

## Ảnh hưởng tới
- **Phiếu mua hàng / Hóa đơn mua bán:** đơn đặt hàng là nguồn sinh phiếu mua hàng; hủy đơn có thể cascade hủy phiếu mua hàng.
- **Tồn kho:** khi phiếu mua hàng nhận hàng về sẽ tăng tồn (qua module phiếu mua hàng), và đơn tự hoàn thành khi nhận đủ.
- **Quản lý tồn (dự báo):** đơn đặt hàng thường bắt đầu từ cảnh báo dự báo hết hàng.
