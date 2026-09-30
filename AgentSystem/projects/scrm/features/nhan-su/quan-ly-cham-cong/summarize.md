---
alias: ["chấm công", "check-in", "giờ làm việc", "phân ca", "xếp ca", "chấm công wifi", "chấm công IMEI", "báo cáo chấm công", "duyệt công"]
module: nhan-su
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — phần XEM/báo cáo có phân quyền theo key (vd GioLamViecNhanVien_BaoCaoChamCongChiTiet, CaLamViecNhanVienNangCao_TimTheoDieuKien, BSSID_TimTheoDieuKien, UserIMEI_TimTheoDieuKien). Riêng các thao tác GHI (duyệt/từ chối/chấm công hộ/tạo-xóa ca/thêm-sửa-xóa-duyệt wifi+IMEI) trong code đã bỏ key phân quyền, chỉ còn 'đã đăng nhập'; với wifi/IMEI thì kiểm tra quyền bằng cấu hình đơn vị (RoleDuocPhepThaoTacThietBiWifi / RoleDuocPhepThaoTacThietBiChamCong). Vai cụ thể được phép: cần product xác nhận."
phu_thuoc: ["Phân ca làm việc (có ca thì mới chấm công đúng ca)", "Khung giờ làm việc của chi nhánh", "Hồ sơ nhân viên / chi nhánh", "Cấu hình đơn vị (vai được thao tác thiết bị wifi/IMEI)", "Tính lương (lấy công đã duyệt làm đầu vào)"]
link_flow: ./flow.md
---

# Quản lý chấm công

## Là gì
Đây là nơi **theo dõi giờ công đi làm của nhân viên**: nhân viên tự bấm check-in/check-out (qua vị trí, wifi của cửa hàng, hoặc thiết bị đã đăng ký), quản lý xếp ca cho nhân viên, rồi **duyệt hoặc từ chối** từng buổi công. Cuối cùng hệ thống cho ra **báo cáo chấm công** (theo danh sách, theo ca, theo ngày) và **xuất Excel** để đối chiếu, làm lương.

Tính năng gồm các phần chính:
- **Phân ca**: xếp nhân viên vào ca/khung giờ theo ngày (tạo từng ca hoặc tạo hàng loạt cho nhiều nhân viên).
- **Chấm công / Giờ làm việc**: xem các buổi công theo danh sách hoặc theo lịch; nhân viên tự check-in theo vị trí; quản lý chấm công hộ; duyệt/từ chối công.
- **Wifi (BSSID) & IMEI thiết bị**: đăng ký và phê duyệt wifi cửa hàng + thiết bị nhân viên dùng để chấm công, chống chấm công khống.
- **Báo cáo chấm công**: thống kê ngày nghỉ, báo cáo chi tiết/theo ca/theo ngày + xuất Excel.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Nhân viên tự check-in theo vị trí (kèm ảnh) | ✅ | đã đăng nhập; thiếu/sai dữ liệu check-in sẽ báo lỗi |
| Quản lý chấm công hộ nhân viên | ✅ | đã đăng nhập (code chỉ chặn mức đăng nhập — xem "Cần chú ý") |
| Duyệt buổi công | ✅ | chỉ buổi đang **Chờ duyệt** |
| Từ chối buổi công | ✅ | chỉ buổi đang **Chờ duyệt**, phải nhập lý do |
| Xóa buổi công | ✅ | đã đăng nhập |
| Xếp ca (tạo từng ca / hàng loạt cơ bản / hàng loạt nâng cao) | ✅ | đã đăng nhập |
| Xóa ca (theo nhóm / theo từng ca) | ✅ | đã đăng nhập |
| Đăng ký & phê duyệt wifi chấm công (BSSID) | ✅ | vai được cấu hình cho phép thao tác thiết bị wifi |
| Đăng ký & phê duyệt thiết bị IMEI | ✅ | vai được cấu hình cho phép thao tác thiết bị chấm công; nhân viên tự đăng ký IMEI rồi chờ duyệt |
| Xem báo cáo chấm công (chi tiết/theo ca/theo ngày) | ✅ | theo quyền xem báo cáo |
| Xem báo cáo chấm công của chính mình | ✅ | đã đăng nhập (luôn chỉ ra công của bản thân) |
| Thống kê ngày nghỉ của nhân viên | ✅ | theo quyền |
| Xuất Excel báo cáo chấm công | ✅ | theo quyền; 2 kiểu layout |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** biết nhân viên đi làm đủ ca không, duyệt công cuối ngày/cuối tháng, xuất bảng công làm lương.
- **Quản lý chi nhánh / HR:** xếp ca, đăng ký wifi và thiết bị chấm công cho cửa hàng, kiểm soát công khống.
- **Nhân viên:** tự check-in/check-out khi đến/về, xem lịch ca của mình và báo cáo công của mình.

## Cách dùng
**Xếp ca cho nhân viên:**
1. Vào **Chấm công → Phân ca**.
2. Chọn nhân viên + khung giờ + ngày → tạo từng ca, hoặc tạo hàng loạt (cơ bản/nâng cao) cho nhiều người cùng lúc.
3. Có thể xem ca dạng danh sách hoặc dạng lịch; xóa ca theo từng buổi hoặc theo nhóm.

**Chấm công hằng ngày:**
1. Nhân viên đến cửa hàng, mở app, bấm **Check-in** (hệ thống ghi vị trí + ảnh; nếu cửa hàng bật kiểm tra wifi/IMEI thì máy phải đúng wifi/thiết bị đã đăng ký).
2. Khi về thì check-out tương tự.
3. Quản lý vào **Chấm công → Giờ làm việc**, xem danh sách/lịch các buổi công, **chấm công hộ** nếu nhân viên quên, rồi **Duyệt** hoặc **Từ chối** (kèm lý do).

**Đăng ký wifi & thiết bị (chống chấm công khống):**
1. Vào **Chấm công → Wifi BSSID** (hoặc **IMEI thiết bị**).
2. Thêm wifi/thiết bị theo chi nhánh → **Phê duyệt**. Chỉ wifi/thiết bị đã duyệt mới cho chấm công.

**Báo cáo:**
1. Vào báo cáo chấm công, chọn khoảng thời gian + chi nhánh → xem chi tiết / theo ca / theo ngày.
2. Bấm **Xuất Excel** để tải bảng công về đối chiếu.

## Cần chú ý
- **Nhiều thao tác ghi (duyệt, từ chối, chấm công hộ, tạo/xóa ca) trong code chỉ chặn ở mức "đã đăng nhập", không kiểm tra vai cụ thể.** Việc giới hạn ai được làm hiện chủ yếu dựa vào ẩn/hiện nút trên giao diện. Đây là điểm cần product/đội bảo mật xác nhận (xem dev.md).
- **Wifi (BSSID) và IMEI: nút "Phê duyệt" là công tắc bật/tắt** — bấm lần nữa sẽ **bỏ duyệt**. Đừng bấm 2 lần nếu chỉ muốn duyệt.
- **Quyền thao tác wifi/IMEI lấy từ cấu hình đơn vị**, không phải từ phân quyền màn hình thông thường. Nếu không thao tác được, kiểm tra cấu hình "vai được phép thao tác thiết bị".
- **Mọi wifi/IMEI đều giới hạn theo chi nhánh của người thao tác** — kể cả admin cũng chỉ thấy/sửa trong chi nhánh mình (nhánh "xem toàn đơn vị" hiện đang tắt trong code).
- **Báo cáo "của tôi"** luôn ép về đúng chi nhánh và đúng nhân viên đang đăng nhập — không xem được công người khác qua màn này.
- **Xuất Excel** có 2 kiểu layout; nếu dữ liệu rỗng/lỗi sẽ báo lỗi thay vì ra file.

## Ảnh hưởng tới
- **Tính lương:** công đã duyệt là đầu vào để tính lương — duyệt sai/thiếu sẽ lệch lương.
- **Phân ca ↔ chấm công:** ca đã xếp là cơ sở đối chiếu khi nhân viên check-in; đổi khung giờ đang có người dùng sẽ bị chặn.
- **Phiếu yêu cầu (đăng ký ca / làm thay):** khi duyệt phiếu, hệ thống tự sinh ca nâng cao và/hoặc buổi công vào đây.
