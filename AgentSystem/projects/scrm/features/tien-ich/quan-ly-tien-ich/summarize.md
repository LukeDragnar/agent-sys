---
alias: ["tiện ích", "quản lý tiện ích", "mua tiện ích", "chợ tiện ích", "đăng ký tiện ích", "tiện ích đã mua", "tiện ích dịch vụ"]
module: tien-ich
trang_thai: Core
goi: cần product xác nhận
quyen: "Mua / xóa tiện ích: chỉ Admin tổng đơn vị (DV-AD-TONG) — và HT-KT cho thao tác mua. Xem danh sách tiện ích đã mua: DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLDV, HT-KT. (theo registry TienIchController)"
phu_thuoc: ["Hợp đồng tiện ích (mỗi lần mua tạo 1 hợp đồng chờ duyệt)", "Phê duyệt tiện ích của quản trị hệ thống (tiện ích trả phí phải được duyệt mới kích hoạt)", "Cấu hình tài khoản ngân hàng hệ thống (sinh mã QR thanh toán)", "Các tính năng gắn cờ [TienIch] trên toàn hệ thống (tiện ích bật/tắt quyền dùng tính năng)"]
link_flow: ./flow.md
---

# Quản lý tiện ích

## Là gì
Đây là **chợ tiện ích** của đơn vị: nơi chủ shop/đơn vị **xem danh sách các tiện ích dịch vụ** mà hệ thống cung cấp (có loại miễn phí, có loại trả phí, có loại mặc định đã có sẵn), **chọn mua/đăng ký** để **mở thêm tính năng** cho đơn vị mình, và **xem/xóa các tiện ích đang dùng**.

Mỗi tiện ích là một "gói chức năng": khi đăng ký thành công, đơn vị được mở quyền dùng tính năng tương ứng trong toàn hệ thống. Tiện ích trả phí khi mua sẽ tạo một **yêu cầu mua (hợp đồng)** ở trạng thái **chờ duyệt** kèm mã QR/thông tin chuyển khoản; tiện ích miễn phí được **kích hoạt ngay**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách tiện ích (theo nhóm) | ✅ | tùy quyền |
| Lọc tiện ích theo nhóm | ✅ | bấm bộ lọc nhóm tiện ích |
| Chọn & **Mua tiện ích** | ✅ | chỉ Admin tổng đơn vị (DV-AD-TONG); tiện ích trả phí phải chờ duyệt mới dùng được |
| Mua tiện ích **miễn phí** | ✅ | kích hoạt ngay, không cần chờ duyệt |
| **Xóa / hủy** tiện ích đã đăng ký | ⚠️ | chỉ DV-AD-TONG; **không xóa được tiện ích mặc định không cho phép xóa** |
| Xem tiện ích **đang sử dụng** + ngày hết hạn | ✅ | tùy quyền |
| Sửa nội dung 1 tiện ích (mô tả, giá…) | ❌ | đây là việc của quản trị hệ thống (Admin SaaS), không phải đơn vị |

## Để làm gì / cho ai
- **Chủ shop / Admin đơn vị:** chủ động mở thêm tính năng cho hệ thống của mình bằng cách đăng ký tiện ích — không cần liên hệ thủ công.
- **Quản lý đơn vị (DV-QLDV) / Admin chi nhánh:** xem được những tiện ích đơn vị đang dùng và hạn dùng để biết khả năng của hệ thống.
- **Kế toán hệ thống (HT-KT):** hỗ trợ đơn vị mua tiện ích.

## Cách dùng
1. Vào **Cài đặt hệ thống → Đăng ký tiện ích** (chợ tiện ích).
2. Danh sách tiện ích hiện ra, chia theo **nhóm**. Có thể bấm **bộ lọc nhóm** để xem từng nhóm hoặc xem tất cả.
3. Mỗi tiện ích hiển thị: ảnh, tên, mô tả, **giá tiền**. Tiện ích **đang dùng** sẽ có nhãn **"Đang sử dụng"** + ngày hết hạn; tiện ích đang **chờ duyệt** có nhãn **"Chờ duyệt"**.
4. Bấm chọn một hoặc nhiều tiện ích muốn mua (ô được tô sáng), rồi bấm **"Mua tiện ích"**.
5. Màn **Thanh toán tiện ích** hiện ra: nhập **số lượng**, xem tổng tiền.
   - Nếu là tiện ích **miễn phí** → bấm thanh toán là **kích hoạt ngay**, báo "Mua tiện ích thành công".
   - Nếu là tiện ích **trả phí** → hệ thống tạo **yêu cầu mua** (mã giao dịch) + hiện **mã QR/thông tin chuyển khoản**; chuyển khoản xong **chờ quản trị hệ thống duyệt** mới được dùng.
6. Với tiện ích đang dùng, có thể bấm biểu tượng **xóa** để hủy đăng ký (nếu được phép).

## Cần chú ý
- **Chỉ Admin tổng đơn vị (DV-AD-TONG) mới mua/xóa tiện ích** — nhân viên thường không thấy nút này hoặc bị chặn quyền.
- **Tiện ích trả phí KHÔNG dùng được ngay sau khi bấm mua.** Nó ở trạng thái **chờ duyệt**, phải chuyển khoản và đợi bên quản trị hệ thống duyệt hợp đồng thì tính năng mới bật.
- **Tiện ích miễn phí được kích hoạt tức thì** (hệ thống tự duyệt).
- **Không xóa được tiện ích mặc định** nếu nó được đánh dấu không cho phép xóa (báo "Không được xóa tiện ích mặc định!").
- **Số lượng mua:** tiện ích trả phí giới hạn **< 100**; tiện ích miễn phí hệ thống tự đặt số lượng lớn (999).
- Tiện ích đang **chờ duyệt** thì **không bấm chọn lại để mua tiếp** (đã khóa chọn).

## Ảnh hưởng tới
- **Quyền dùng tính năng toàn hệ thống:** mỗi tính năng có gắn cờ tiện ích; mua/duyệt tiện ích → mở quyền, xóa tiện ích → thu hồi quyền dùng tính năng đó.
- **Lịch sử đăng ký tiện ích:** mỗi lần mua tạo một bản ghi giao dịch xem được ở màn Lịch sử mua tiện ích.
- **Phê duyệt tiện ích (quản trị hệ thống):** yêu cầu mua trả phí đẩy sang hàng chờ duyệt của Admin SaaS.
