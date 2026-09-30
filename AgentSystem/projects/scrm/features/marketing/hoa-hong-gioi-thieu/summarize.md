---
alias: ["hoa hồng giới thiệu", "cộng tác viên", "CTV", "affiliate", "nhóm hoa hồng", "hoa hồng CTV", "nhân viên giới thiệu"]
module: marketing
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — CRUD CTV và nhóm hoa hồng: DV-AD-TONG, DV-AD-CN, DV-AD-MCN; non-admin ép theo chi nhánh; thống kê hoa hồng theo CTV có key riêng. Một số autocomplete/check chỉ yêu cầu đăng nhập (khe hở — xem dev)"
phu_thuoc: ["Nhóm hoa hồng CTV (quyết định % hoa hồng của mỗi CTV)", "Đơn hàng (đơn gắn CTV → ghi dữ liệu hoa hồng để thống kê)", "Sản phẩm / doanh số / chiến dịch (các kiểu tính hoa hồng)", "User/nhân viên (CTV gắn với tài khoản nhân viên)"]
link_flow: ./flow.md
---

# Hoa hồng giới thiệu

## Là gì
Đây là nơi quản lý **cộng tác viên (CTV)** và **nhóm hoa hồng** để tính hoa hồng giới thiệu/bán hàng:
- **Cộng tác viên (CTV)**: danh sách người giới thiệu/bán hàng (có thể là nhân viên), mỗi CTV gắn 1 nhóm hoa hồng.
- **Nhóm hoa hồng CTV**: cấu hình **% hoa hồng** — theo sản phẩm, theo bậc doanh số, theo chiến dịch. Mỗi CTV thuộc 1 nhóm → nhóm quyết định cách tính hoa hồng.

Khi đơn hàng có gắn CTV được thanh toán, hệ thống tính và ghi nhận hoa hồng; các màn **thống kê** cho phép xem hoa hồng theo từng CTV (sản phẩm / doanh số / chiến dịch).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm / Sửa / Xóa cộng tác viên | ✅ | DV-AD-TONG/CN/MCN |
| Xem danh sách / lọc CTV | ✅ | non-admin ép theo chi nhánh |
| Import CTV từ Excel | ✅ | DV-AD-TONG |
| Thêm / Sửa / Xóa nhóm hoa hồng | ✅ | DV-AD-TONG/CN/MCN |
| Bật / tắt sử dụng nhóm | ✅ | tùy quyền |
| Sao chép cấu hình nhóm | ✅ | tùy quyền quản trị |
| Áp dụng cấu hình theo phân loại | ✅ | tùy quyền quản trị |
| Import nhóm hoa hồng từ Excel | ✅ | tùy quyền |
| Xem thống kê hoa hồng theo CTV (SP/doanh số/chiến dịch) | ✅ | key thống kê tương ứng |

## Để làm gì / cho ai
- **Chủ shop / Quản lý:** thiết lập chính sách hoa hồng, theo dõi hoa hồng phải trả cho từng CTV.
- **Kế toán:** đối chiếu hoa hồng theo sản phẩm/doanh số/chiến dịch để chi trả.
- **CTV / nhân viên giới thiệu:** được tính hoa hồng theo đơn họ giới thiệu/bán.

## Cách dùng

**Nhóm hoa hồng (cấu hình trước):**
1. Vào **Hoa hồng CTV → Nhóm hoa hồng → Thêm mới**.
2. Cấu hình % hoa hồng theo sản phẩm / bậc doanh số / chiến dịch (bật cờ dùng hoa hồng chiến dịch nếu cần).
3. Có thể **Sao chép** cấu hình sang nhóm khác hoặc **Áp dụng** theo phân loại nhóm.

**Cộng tác viên:**
1. Vào **Hoa hồng CTV → Cộng tác viên → Thêm mới**, gắn CTV vào một nhóm hoa hồng.
2. Hoặc **Import** danh sách CTV từ Excel.
3. Khi tạo/xử lý đơn, gắn CTV cho đơn → hệ thống tính hoa hồng khi thanh toán.

**Thống kê:**
1. Vào tab thống kê → xem hoa hồng theo CTV theo sản phẩm / doanh số / chiến dịch.

## Cần chú ý
- **Mỗi CTV thuộc 1 nhóm hoa hồng** — đổi cấu hình nhóm sẽ ảnh hưởng % hoa hồng của tất cả CTV trong nhóm.
- **Thống kê chỉ có dữ liệu nếu đơn đã được đẩy vào pipeline hoa hồng.** Nếu đơn chưa được xử lý (hoặc đẩy lỗi), báo cáo sẽ trống dù đơn đã bán.
- **Cờ "dùng hoa hồng chiến dịch" ở nhóm** phải bật thì đơn mới phát sinh hoa hồng theo chiến dịch — tắt cờ thì dù chiến dịch có cấu hình cũng không phát sinh.
- **Xóa nhóm cần kiểm tra ràng buộc:** nếu còn CTV đang gán nhóm bị xóa, nên xử lý trước (xóa nhóm đi qua thủ tục dọn cấu hình con, không tự cascade qua CTV).
- **Một số chức năng (autocomplete CTV, check CTV, lấy KM theo CTV) chỉ yêu cầu đăng nhập** — quyền lỏng hơn các thao tác CRUD.
- **Đừng nhầm với "hoa hồng giới thiệu theo nguồn".** Tính năng này là hoa hồng của **cộng tác viên (CTV)**; còn hoa hồng tính theo **nguồn giới thiệu** (mã/kênh giới thiệu khách) nằm ở tính năng **Người giới thiệu** — hai cái khác nhau dù tên gọi na ná.

## Ảnh hưởng tới
- **Đơn hàng** — đơn gắn CTV → pipeline hoa hồng ghi dữ liệu để thống kê.
- **Nhóm hoa hồng** — thay đổi cấu hình nhóm → đổi % hoa hồng tính cho đơn.
- **Báo cáo / kế toán hoa hồng** — nguồn để chi trả cho CTV.
