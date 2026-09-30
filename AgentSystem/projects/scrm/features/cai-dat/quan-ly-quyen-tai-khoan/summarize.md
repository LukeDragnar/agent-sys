---
alias: ["quản lý thiết bị đăng nhập", "duyệt thiết bị", "phê duyệt thiết bị login", "log đăng nhập", "lịch sử đăng nhập", "giới hạn đăng nhập", "mở rộng thiết bị đăng nhập", "quản lý user login"]
module: cai-dat
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — quản lý/duyệt thiết bị: DV-AD-TONG, DV-AD-CN, DV-AD-MCN (key UserDevice_*). Tra log đăng nhập: chỉ login (user thường chỉ thấy log đơn vị mình; SuperUser/CSKH lọc theo đơn vị bất kỳ). Mở rộng slot đăng nhập đa thiết bị: HT-AD, HT-KT (key UserDeviceLoginExtend_*)."
phu_thuoc: ["Đơn vị (mọi thao tác scope theo IdDonVi đang đăng nhập)", "Hồ sơ nhân viên (chọn user để cấp slot / buộc đổi)", "Hạn mức đơn vị DonVi.LoginOneDeviceExtend (số thiết bị đăng nhập đồng thời tối đa)"]
link_flow: ./flow.md
---

# Quản lý quyền & tài khoản (thiết bị & đăng nhập)

## Là gì
Đây là nhóm thiết lập **kiểm soát ai/đăng nhập trên thiết bị nào** trong đơn vị. Gồm 3 phần:
- **Quản lý thiết bị đăng nhập:** danh sách các thiết bị nhân viên đã/đang đăng nhập; admin **phê duyệt**, **hủy duyệt** hoặc **xóa** thiết bị để kiểm soát đăng nhập đa thiết bị.
- **Mở rộng số thiết bị đăng nhập (slot):** mặc định mỗi nhân viên đăng nhập trên số thiết bị giới hạn; admin có thể **cấp thêm slot** cho từng nhân viên, trong hạn mức tổng của đơn vị.
- **Lịch sử (log) đăng nhập:** tra cứu nhật ký các lần đăng nhập (tài khoản, IP, thiết bị, thời gian) theo đơn vị.

> Phần **phân quyền vai trò** (gán quyền theo role) và **đồng bộ danh mục quyền toàn hệ thống** thuộc khu quản trị hệ thống (super-admin), không nằm trong cẩm nang khách này — cần product xác nhận phạm vi.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách thiết bị đăng nhập | ✅ | DV-AD-TONG/CN/MCN |
| Phê duyệt thiết bị | ✅ | DV-AD-TONG/CN/MCN — cho phép thiết bị đăng nhập |
| Hủy duyệt thiết bị | ✅ | DV-AD-TONG/CN/MCN — thu hồi quyền đăng nhập thiết bị |
| Xóa thiết bị | ✅ | DV-AD-TONG/CN/MCN; chỉ thiết bị **thuộc đơn vị mình** |
| Cấp / sửa số thiết bị đăng nhập (slot) cho nhân viên | ✅ | HT-AD, HT-KT; tổng slot ≤ hạn mức đơn vị |
| Xóa slot mở rộng của nhân viên | ✅ | HT-AD, HT-KT — đăng xuất hết thiết bị mở rộng |
| Tra lịch sử (log) đăng nhập | ✅ | chỉ login; user thường chỉ thấy đơn vị mình |
| Lọc log đăng nhập theo đơn vị bất kỳ | ⚠️ | chỉ SuperUser / CSKH hệ thống |

## Để làm gì / cho ai
- **Admin đơn vị:** kiểm soát nhân viên đăng nhập ở đâu, chặn thiết bị lạ, giới hạn số thiết bị đồng thời để tránh chia sẻ tài khoản.
- **Admin/Kế toán hệ thống:** cấp thêm slot đăng nhập đa thiết bị cho nhân viên trong hạn mức gói của đơn vị.
- **Người phụ trách an ninh / CSKH:** tra log đăng nhập khi điều tra sự cố (IP lạ, thời điểm bất thường).

## Cách dùng
**Duyệt / chặn thiết bị:**
1. Vào màn **Quản lý thiết bị đăng nhập**.
2. Thiết bị mới chờ duyệt → bấm **Duyệt** để cho phép, hoặc **Hủy duyệt** để chặn.
3. Bấm **Xóa** để loại bỏ thiết bị (chỉ thiết bị thuộc đơn vị mình).

**Cấp thêm slot đăng nhập:**
1. Vào màn **Giới hạn đăng nhập / Quản lý user login**.
2. Bấm **Thêm**, chọn nhân viên và nhập số thiết bị được phép → Lưu. Hệ thống chặn nếu vượt hạn mức tổng đơn vị.
3. Sửa/Xóa slot sẽ **tự đăng xuất** các thiết bị vượt quá số mới.

**Tra log đăng nhập:**
1. Vào màn **Lịch sử đăng nhập**.
2. Lọc theo từ khóa (tài khoản/IP), từ ngày–đến ngày → xem danh sách.

## Cần chú ý
- **Xóa thiết bị chỉ trong đơn vị mình:** thiết bị của đơn vị khác sẽ báo "Không tìm thấy thiết bị" (chặn xóa chéo đơn vị).
- **Cấp slot bị giới hạn theo gói:** tổng slot của tất cả nhân viên không vượt hạn mức thiết bị đăng nhập của đơn vị; chưa cấu hình hạn mức = coi như 0 (không cấp được).
- **Sửa/Xóa slot làm logout ngay:** khi giảm số thiết bị, các thiết bị thừa của nhân viên bị đăng xuất lập tức.
- **Một nhân viên chỉ có 1 bản ghi slot trong đơn vị** — thêm trùng nhân viên sẽ báo lỗi trùng lặp.
- **Log đăng nhập có độ trễ nhỏ:** ghi async sau khi login, nên vừa đăng nhập xong có thể chưa thấy ngay.
- **Sắp xếp log luôn theo thời gian giảm dần** (mới nhất trước) — không đổi được thứ tự sắp xếp (xem dev.md).

## Ảnh hưởng tới
- **Phiên đăng nhập của nhân viên:** duyệt/hủy/xóa thiết bị và thay đổi slot đều ảnh hưởng trực tiếp việc nhân viên có đăng nhập được hay không.
- **Bảo mật đơn vị:** kiểm soát thiết bị + log là công cụ điều tra sự cố đăng nhập.
- **Hạn mức gói:** số slot mở rộng phụ thuộc gói/hợp đồng của đơn vị.
