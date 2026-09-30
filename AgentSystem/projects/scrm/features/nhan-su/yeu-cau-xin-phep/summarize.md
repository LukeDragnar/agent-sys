---
alias: ["phiếu yêu cầu", "đăng ký ca làm", "đăng ký làm thay", "làm hộ", "đăng ký làm hộ", "xin phép", "duyệt phiếu"]
module: nhan-su
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "Xem danh sách/chi tiết phiếu (màn quản lý): có key (PhieuYeuCau_Branch_TimTheoDieuKien, _TimTheoId) — vai HR/AD/KT-TONG (cần product xác nhận đủ vai). Các thao tác tự phục vụ (đăng ký ca/làm thay/chấm công trên phiếu/hủy): chỉ cần 'đã đăng nhập'. Duyệt/Từ chối: trong code chặn mức 'đã đăng nhập' rồi check vai bằng cấu hình đơn vị (RoleDuocPhepDuyetPhieuYeuCau), không qua phân quyền màn hình."
phu_thuoc: ["Phân ca / khung giờ làm việc của chi nhánh (Booking)", "Hồ sơ nhân viên (người đăng ký / người được làm thay)", "Cấu hình đơn vị (vai được duyệt phiếu, có bắt buộc chụp ảnh khi làm hộ không)", "Lưu file ảnh selfie (FileService)", "Chấm công (duyệt phiếu sinh ca nâng cao / buổi công)"]
link_flow: ./flow.md
---

# Yêu cầu & xin phép (Phiếu yêu cầu)

## Là gì
Đây là nơi **nhân viên gửi phiếu yêu cầu liên quan đến ca làm việc**, và **quản lý duyệt/từ chối** các phiếu đó. Có 2 loại phiếu:
- **Đăng ký làm ca của mình (Loại 1)**: nhân viên xin được xếp vào một ca/khung giờ.
- **Đăng ký làm thay người khác (Loại 2)**: nhân viên xin làm thay ca của một nhân viên khác (có thể khác chi nhánh), kèm chấm công (check-in/check-out) ngay trên phiếu và ảnh selfie.

Khi quản lý duyệt, hệ thống tự sinh ca làm việc và/hoặc buổi công tương ứng vào phần chấm công.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Nhân viên xem phiếu của mình theo ngày | ✅ | đã đăng nhập (loại trừ phiếu đã duyệt) |
| Đăng ký làm ca của mình (Loại 1) | ✅ | đã đăng nhập; chặn ca quá khứ & khung giờ không thuộc chi nhánh |
| Đăng ký làm thay người khác (Loại 2) | ✅ | đã đăng nhập; người được thay phải cùng đơn vị, có ca, chưa đi làm; không tự làm thay chính mình |
| Chấm công (check-in/out) trên phiếu làm thay | ✅ | chủ phiếu, phiếu Loại 2 đang chờ duyệt; ảnh selfie bắt buộc trừ khi cấu hình tắt |
| Hủy phiếu | ✅ | chủ phiếu, chỉ phiếu đang **Chờ duyệt** |
| Duyệt phiếu (Loại 1 & 2) | ✅ | vai được cấu hình duyệt (RoleDuocPhepDuyetPhieuYeuCau); chỉ phiếu Chờ duyệt |
| Từ chối phiếu (Loại 1 & 2) | ✅ | vai được duyệt; bắt buộc nhập lý do |
| Xem danh sách/chi tiết phiếu (màn quản lý) | ✅ | HR/AD/KT-TONG (cần product xác nhận) |

## Để làm gì / cho ai
- **Nhân viên:** chủ động đăng ký ca làm, hoặc xin làm thay đồng nghiệp khi cần (đổi ca, hỗ trợ chi nhánh khác).
- **Quản lý / HR:** kiểm soát và duyệt các yêu cầu ca làm, đảm bảo đủ người trực, có lý do rõ ràng khi từ chối.

## Cách dùng
**Nhân viên đăng ký làm ca của mình (Loại 1):**
1. Mở app → Phiếu yêu cầu → Đăng ký ca.
2. Chọn khung giờ + ngày (phải thuộc chi nhánh mình, không phải ngày quá khứ) → gửi → phiếu ở trạng thái **Chờ duyệt**.

**Nhân viên đăng ký làm thay (Loại 2):**
1. Chọn nhân viên muốn làm thay → hệ thống hiện các ca của người đó theo ngày.
2. Chọn ca → gửi phiếu làm thay (Chờ duyệt).
3. Đến ca, vào phiếu **Check-in** (chụp ảnh selfie nếu cửa hàng yêu cầu), hết ca **Check-out**.

**Quản lý duyệt/từ chối:**
1. Vào màn quản lý Phiếu yêu cầu, lọc theo loại/trạng thái/ngày/nhân viên.
2. Với phiếu **Chờ duyệt** → **Duyệt** (hệ thống tạo ca + buổi công) hoặc **Từ chối** (nhập lý do).

## Cần chú ý
- **Chỉ phiếu đang "Chờ duyệt" mới được duyệt/từ chối/hủy/chấm công.** Phiếu đã duyệt/từ chối/hủy thì không thao tác tiếp được.
- **Không tự làm thay chính mình**, và người được làm thay phải **cùng đơn vị, có ca trong ngày, và chưa đi làm ca đó**.
- **Đăng ký làm ca không được chọn ca quá khứ** và khung giờ phải thuộc chi nhánh của mình.
- **Chấm công trên phiếu làm thay thường bắt buộc chụp ảnh selfie** — trừ khi đơn vị bật cấu hình "không bắt buộc chụp ảnh khi làm hộ".
- **Làm thay khác chi nhánh**: khi duyệt, quản lý phải chọn khung giờ tương ứng ở chi nhánh người làm thay (hệ thống yêu cầu khung giờ ánh xạ); cùng chi nhánh thì hệ thống tự lấy.
- **Quyền duyệt lấy từ cấu hình đơn vị** (vai được phép duyệt phiếu), không phải từ phân quyền màn hình thông thường — nếu không duyệt được, kiểm tra cấu hình này.
- **Hệ thống chống gửi trùng phiếu** trong vài giây (khóa chống bấm 2 lần) và chống đăng ký trùng ca.

## Ảnh hưởng tới
- **Chấm công / phân ca:** duyệt phiếu sinh ca nâng cao và/hoặc buổi công → ảnh hưởng bảng công của cả người đăng ký và người được làm thay (người được thay được ghi nhận nghỉ ca đó).
- **Tính lương:** vì sinh buổi công, kết quả duyệt phiếu gián tiếp ảnh hưởng lương.
- **Đồng bộ tài khoản giữa chi nhánh:** duyệt làm thay khác chi nhánh đồng bộ lại dữ liệu user 2 chi nhánh.
