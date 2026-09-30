---
alias: ["thu chi", "phiếu thu", "phiếu chi", "quỹ tiền mặt", "tiền gửi", "chuyển tiền nội bộ", "tồn quỹ", "số dư ban đầu", "khởi tạo công nợ"]
module: ke-toan
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — chỉ màn Danh sách phiếu thu-chi cần quyền xem danh sách; các thao tác còn lại (xem chi tiết, xuất Excel, thêm/xóa/danh sách Số dư ban đầu) chỉ cần đăng nhập (không gắn quyền riêng theo code hiện tại — xem dev.md). Người không phải admin tổng/admin nhiều chi nhánh bị ép xem theo đúng chi nhánh của mình."
phu_thuoc: ["Khách hàng (lấy tên KH hiển thị trên phiếu; ghi số dư ban đầu công nợ KH)", "Nhà cung cấp (ghi số dư ban đầu công nợ NCC)", "Lý do thu chi (danh mục lý do để lọc/khai phiếu)", "Đơn hàng (phiếu thu-chi gắn đơn được module Đơn hàng tạo/đồng bộ tự động)", "Bút toán kế toán (phiếu thu/chi sinh hạch toán riêng)"]
link_flow: ./flow.md
---

# Thu chi (Quỹ tiền mặt / tiền gửi)

## Là gì
Đây là nơi **theo dõi dòng tiền vào – ra quỹ** của shop/đơn vị: phiếu thu, phiếu chi (tiền mặt và tiền gửi/chuyển khoản), chuyển tiền nội bộ giữa các quỹ, cùng bảng **tồn quỹ** (số dư đầu kỳ – phát sinh trong kỳ – số dư cuối kỳ). Ngoài ra màn này còn có phần **Số dư ban đầu** — nơi khai báo công nợ khởi tạo của khách hàng, nhà cung cấp hoặc tài khoản khi mới bắt đầu dùng phần mềm.

Phần lớn phiếu thu-chi gắn với đơn hàng được hệ thống **tự sinh** từ luồng bán hàng/thanh toán; màn này chủ yếu để **xem, lọc, tổng hợp và xuất Excel**, cộng thêm việc tự khai báo **số dư ban đầu**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách phiếu thu-chi + tồn quỹ | ✅ | cần quyền xem danh sách; người thường chỉ thấy chi nhánh của mình |
| Xem chi tiết một phiếu | ✅ | chỉ cần đăng nhập |
| Xuất Excel (theo từng loại phiếu) | ✅ | chỉ cần đăng nhập; phải có dữ liệu, nếu rỗng báo "Không có dữ liệu để xuất" |
| Thêm Số dư ban đầu (KH / NCC / tài khoản) | ✅ | chỉ cần đăng nhập; số tiền phải khác 0 |
| Xóa Số dư ban đầu | ✅ | chỉ cần đăng nhập (xóa vĩnh viễn) |
| Tạo / sửa phiếu thu, phiếu chi trực tiếp tại màn này | ❌ | màn này **không phải nơi ghi phiếu thu-chi thực sự** — phiếu phát sinh từ luồng đơn hàng / chứng từ mua bán; sửa số tiền phiếu của đơn do module Đơn hàng điều khiển |

## Để làm gì / cho ai
- **Kế toán / chủ shop:** nắm được tiền mặt và tiền gửi đang còn bao nhiêu (tồn quỹ), tổng thu – chi – nạp – rút trong kỳ; tra một phiếu cụ thể; xuất Excel để đối chiếu sổ sách.
- **Quản lý chi nhánh:** xem dòng tiền của chi nhánh mình phụ trách.
- **Người mới triển khai phần mềm:** khai báo **số dư ban đầu** để công nợ khách/NCC và số dư quỹ khớp với thực tế tại thời điểm bắt đầu dùng.

## Cách dùng

**Xem quỹ và phiếu thu-chi:**
1. Vào **Kế toán → Quỹ / Thu chi**.
2. Chọn chi nhánh, loại thu-chi, lý do, khoảng ngày để lọc; có thể gõ từ khóa (tìm cả theo mã hóa đơn của phiếu gắn đơn hàng).
3. Bảng phiếu hiện ra (mới nhất lên đầu), kèm panel **tồn quỹ** (đầu kỳ / trong kỳ / cuối kỳ) và **tổng Thu / Chi / Nạp / Rút**.
4. Bấm vào một phiếu để xem chi tiết.
5. Bấm **Xuất Excel** ở tab loại phiếu tương ứng (phiếu thu tiền mặt, thu tiền gửi, chi tiền mặt, chi tiền gửi, chuyển tiền nội bộ).

**Khai báo số dư ban đầu:**
1. Mở phần **Số dư ban đầu**.
2. Bấm **Khởi tạo số dư ban đầu**, chọn đối tượng (Khách hàng / Nhà cung cấp / Tài khoản), nhập số tiền và kiểu dư (Nợ / Có).
3. Lưu — với khách hàng/nhà cung cấp, số dư được đẩy sang phần công nợ tương ứng.
4. Có thể **xóa** dòng số dư đã khai (xóa vĩnh viễn).

## Cần chú ý
- **Mỗi người xem được dòng tiền khác nhau:** ai không phải admin tổng hoặc admin nhiều chi nhánh sẽ chỉ thấy phiếu của **chính chi nhánh mình**, không phải lỗi.
- **Đây không phải nơi gõ phiếu thu / chi mới.** Phiếu thu-chi thật phát sinh từ luồng đơn hàng, chứng từ mua bán, bù trừ công nợ… Khi đơn hàng đổi phương thức thanh toán, hệ thống tự thêm/sửa/xóa phiếu thu-chi của đơn đó.
- **Xóa số dư ban đầu là xóa vĩnh viễn** (không khôi phục được), và sẽ đồng thời hoàn lại công nợ đã ghi bên Khách hàng/Nhà cung cấp.
- **Số dư ban đầu của Khách hàng / Nhà cung cấp ghi sang 2 nơi** (ở đây + bên công nợ KH/NCC). Nếu một bên lỗi thì có thể lệch — nên kiểm tra lại sau khi khai (xem dev.md, hai bước này không cùng một giao dịch). `cần product xác nhận` về cách xử lý khi lệch.
- **Xuất Excel** sẽ báo "Không có dữ liệu để xuất" nếu bộ lọc hiện tại không có phiếu nào.

## Ảnh hưởng tới
- **Báo cáo quỹ tài khoản** (sổ quỹ theo tài khoản) — lấy từ chính các phiếu thu-chi này.
- **Công nợ khách hàng / nhà cung cấp** — số dư ban đầu khai ở đây cộng vào công nợ tổng.
- **Đơn hàng** — khi đổi phương thức thanh toán, phiếu thu-chi gắn đơn được hệ thống cập nhật lại tự động.
