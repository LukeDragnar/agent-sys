---
alias: ["đối tác trả góp", "cấu hình trả góp", "tài khoản trả góp", "đối tác tín dụng", "lịch sử thay đổi domain", "yêu cầu đổi domain", "domain đơn vị"]
module: cai-dat
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — Cấu hình đối tác trả góp: chỉ yêu cầu đăng nhập (không phân quyền chi tiết; non-admin bắt buộc chọn chi nhánh, admin tổng thì không). Yêu cầu/hủy thay đổi domain + tra lịch sử (phía đơn vị): DV-AD-TONG. Duyệt/từ chối domain (phía admin hệ thống): HT-AD, HT-KT."
phu_thuoc: ["Đơn vị & chi nhánh (cấu hình trả góp scope theo đơn vị/chi nhánh)", "Nhà cung cấp (tài khoản trả góp gắn NCC)", "Danh mục đối tác trả góp (DMDoiTacTraGop — master, lọc phương thức thanh toán trả góp)", "Bộ duyệt domain phía admin hệ thống (QuanTriArea)"]
link_flow: ./flow.md
---

# Quản lý tài chính (đối tác trả góp & domain đơn vị)

## Là gì
Nhóm thiết lập tài chính/hạ tầng của đơn vị, gồm 2 phần khác nhau:
- **Cấu hình đối tác trả góp:** chọn các **đối tác trả góp** (công ty tín dụng) mà đơn vị chấp nhận khi khách thanh toán trả góp, và khai báo **tài khoản trả góp** chi tiết (gắn nhà cung cấp, mã/tên tài khoản, theo chi nhánh). Đây là input cho luồng thanh toán trả góp tại quầy.
- **Lịch sử / yêu cầu thay đổi domain:** đơn vị gửi **yêu cầu đổi tên miền (domain)** cho hệ thống của mình, hủy yêu cầu, và tra **lịch sử thay đổi domain**. Yêu cầu phải được admin hệ thống duyệt mới áp domain mới.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách đối tác trả góp của đơn vị | ✅ | chỉ login |
| Map nhanh (chọn/bỏ chọn) đối tác trả góp | ✅ | chỉ login |
| Thêm cấu hình tài khoản trả góp (gắn NCC) | ✅ | chỉ login; non-admin **bắt buộc chọn chi nhánh** |
| Sửa cấu hình trả góp | ✅ | chỉ login; chống trùng theo (đơn vị, chi nhánh, đối tác, mã TK) |
| Xóa cấu hình trả góp | ✅ | chỉ login; theo đơn vị mình |
| Gửi yêu cầu thay đổi domain | ✅ | DV-AD-TONG (vào trạng thái chờ duyệt) |
| Hủy yêu cầu thay đổi domain | ✅ | DV-AD-TONG; khi chưa được duyệt |
| Tra lịch sử thay đổi domain (đơn vị) | ✅ | DV-AD-TONG |
| **Duyệt / từ chối yêu cầu domain** | ❌ (với đơn vị) | chỉ admin hệ thống (HT-AD, HT-KT) |

## Để làm gì / cho ai
- **Chủ shop / admin đơn vị (tài chính):** thiết lập sẵn các đối tác trả góp + tài khoản để thu tiền trả góp đúng quỹ, gắn đúng nhà cung cấp.
- **Admin đơn vị (DV-AD-TONG):** quản lý tên miền truy cập của đơn vị (gửi yêu cầu đổi domain khi cần đổi thương hiệu/địa chỉ web).
- **Admin hệ thống:** duyệt/từ chối yêu cầu đổi domain.

## Cách dùng
**Cấu hình đối tác trả góp:**
1. Vào **Cài đặt → Cấu hình đối tác trả góp**.
2. Dùng màn **map nhanh** để tích chọn các đối tác trả góp đơn vị chấp nhận, rồi Lưu.
3. Với từng đối tác, **Thêm cấu hình** đầy đủ: chọn nhà cung cấp, nhập mã/tên tài khoản, chọn chi nhánh (bắt buộc nếu không phải admin tổng) → Lưu.
4. Sửa/Xóa cấu hình khi cần.

**Yêu cầu / lịch sử domain:**
1. Vào màn **Domain đơn vị / Lịch sử thay đổi domain**.
2. Bấm **Tạo yêu cầu** đổi domain → yêu cầu vào trạng thái chờ duyệt.
3. Có thể **Hủy bỏ** yêu cầu khi chưa được duyệt. Tra **lịch sử** để xem các lần thay đổi và trạng thái.

## Cần chú ý
- **Cấu hình trả góp chỉ yêu cầu đăng nhập** — không phân quyền chi tiết theo vai trò; người không phải admin tổng bắt buộc chọn chi nhánh, admin tổng có thể cấu hình cấp đơn vị (không chi nhánh).
- **Chống trùng cấu hình:** không tạo trùng theo (đơn vị, chi nhánh, đối tác trả góp, mã tài khoản); trùng sẽ báo "Đối tượng đã tồn tại trong hệ thống".
- **Lưu ý kỹ thuật (xem dev.md):** loại quỹ khi **Thêm** và khi **Sửa** cấu hình trả góp đang khác nhau (Thêm = Quỹ trả góp, Sửa = Ví điện tử) → cần product xác nhận đúng ý đồ.
- **Đổi domain phải chờ duyệt:** đơn vị chỉ tạo/hủy yêu cầu; admin hệ thống duyệt mới áp domain mới. Hủy/Từ chối thì giữ domain cũ.
- **Thao tác hủy/duyệt domain qua link (GET):** một số nút đổi trạng thái dùng GET → cẩn thận tránh bấm/nhấn nhầm (xem dev.md).

## Ảnh hưởng tới
- **Thanh toán trả góp tại quầy:** danh sách đối tác + tài khoản trả góp là nguồn cho form thanh toán và tạo nhà cung cấp trả góp; báo cáo trả góp đọc theo đối tác.
- **Truy cập hệ thống của đơn vị:** đổi domain (sau khi duyệt) thay đổi địa chỉ web đơn vị dùng.
