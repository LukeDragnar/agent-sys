---
alias: ["chia số care đơn", "phân đơn cho nhân viên", "chia đơn chăm sóc", "chia care đơn cho kho", "phân bổ đơn care"]
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — code yêu cầu 1 trong các role: HT-CSKH, DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-TP-QL-KHO, DV-TN-QL-KHO, DV-CHIASO-CAREDON (riêng phần lưu/đổi cấu hình thì không nhận 2 role TP/TN quản lý kho)"
phu_thuoc: [OrderLogistic (đơn hàng logistic — đối tượng được chia), Core (User/nhóm nhân viên kho care đơn, cơ cấu tổ chức, quyền), Cấu hình đơn vị (bật/tắt tính năng care đơn, kho tổng, chi nhánh tổng), Cấu hình trạng thái giao hàng (trạng thái nào thì chia + chia sau bao nhiêu phút), Sản phẩm (chia theo sản phẩm), RabbitMQ + job nền (chia tự động)]
link_flow: ./flow.md
module: kho
---

# Chia số care đơn

## Là gì
Chia số care đơn là cách hệ thống **giao đơn hàng cho nhân viên kho / chăm sóc đơn (care đơn) phụ trách** — giống như chia bài: mỗi đơn về tay một người (hoặc một nhóm) để người đó theo dõi, xử lý, chăm sóc đơn đó tới cùng.

Việc chia có thể diễn ra **tự động** (đơn vừa đạt trạng thái cấu hình là hệ thống tự gắn người phụ trách, không ai phải bấm) hoặc **thủ công** (quản lý tự chọn đơn rồi phân về nhóm / về người). Mỗi đơn chỉ được chia **một lần** — đã có người care thì thôi.

Tính năng gồm 3 màn hình chính:
- **Cấu hình care đơn** — đặt luật chia: chia về nhóm hay về thẳng từng tài khoản, chia theo sản phẩm không, định mức mỗi nhóm/người nhận bao nhiêu đơn.
- **Chia số care đơn thủ công** — màn để quản lý tự tay phân những đơn chưa có người care.
- **Bật/tắt nhận dữ liệu** — bật/tắt từng tài khoản kho có được nhận đơn care hay không (ví dụ nhân viên nghỉ phép thì tắt).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Cấu hình luật chia (về nhóm / về tài khoản, theo sản phẩm, định mức) | ✅ | cần role chia care đơn; lưu/đổi cấu hình KHÔNG nhận role TP/TN quản lý kho |
| Chia tự động khi đơn đạt trạng thái cấu hình | ✅ | phải bật tính năng care đơn ở cấp đơn vị; chạy bằng job nền |
| Chia thủ công đơn chưa có người care | ✅ | role chia care đơn; chỉ chia đơn chưa được phân bổ |
| Chia lại đơn đã có người / nhóm care | ❌ | mỗi đơn chỉ chia một lần, kể cả đổi trạng thái nhiều lần |
| Bật/tắt nhận dữ liệu cho từng tài khoản kho | ✅ | chỉ ảnh hưởng đơn chia về sau, không thu hồi đơn đã giao |
| Đặt chia trễ X phút sau khi đạt trạng thái | ✅ | cấu hình ở phần trạng thái giao hàng |
| Dùng khi đơn vị chưa bật tính năng care đơn | ❌ | không màn nào hoạt động; chia tự động im lặng bỏ qua |

## Để làm gì / cho ai
- **Cho chủ shop / quản lý kho:** đảm bảo mọi đơn đều có người phụ trách, chia đều việc cho đội care, không để đơn "rơi" không ai xử lý.
- **Cho trưởng/phó phòng kho:** cân tải — ai đang ít đơn thì hệ thống ưu tiên giao thêm; điều chỉnh thủ công khi cần.
- **Cho nhân viên care đơn:** nhận đúng phần đơn của mình để chăm sóc, không giẫm chân nhau.
- **Actor:** admin đơn vị/chi nhánh, CSKH hệ thống, trưởng/phó phòng quản lý kho, tài khoản có quyền chia số care đơn.

## Cách dùng
**Thiết lập một lần (Cấu hình care đơn):**
1. Vào màn **Cấu hình care đơn**, chọn **kiểu chia**:
   - **Kiểu chia về nhóm** — đơn được chia cho các nhóm kho, trong nhóm chia tiếp cho nhân viên.
   - **Kiểu chia trực tiếp về tài khoản** — tài khoản care đơn liên kết thẳng với nhóm sale, đơn của nhóm sale nào về đúng tài khoản đó.
2. Bật **chia tự động** theo nhóm / theo nhân viên, đặt **định mức** (mỗi đối tượng nhận tối đa bao nhiêu đơn/ngày), bật **chia theo sản phẩm** nếu muốn đơn chứa sản phẩm A chỉ về nhóm/người chuyên A.
3. (Tùy chọn) đặt **chia sau X phút** ở phần cấu hình trạng thái giao hàng — đơn không chia ngay mà chờ X phút.

**Vận hành hằng ngày:**
- Để hệ thống **tự chia** khi đơn đạt trạng thái đã cấu hình.
- Với đơn còn sót / chưa có người care → vào **Chia số care đơn thủ công**, lọc đơn chưa phân bổ, chọn đơn, phân **về nhóm** hoặc **về người**.
- Khi nhân viên nghỉ / không nhận đơn → vào **Bật/tắt nhận dữ liệu**, gạt tắt tài khoản đó để hệ thống ngừng giao đơn cho họ.

## Cần chú ý
- **Phải bật tính năng ở cấp đơn vị trước.** Nếu đơn vị chưa bật "sử dụng chia số care đơn" thì không màn nào hoạt động, và chia tự động sẽ im lặng bỏ qua.
- **Mỗi đơn chỉ chia một lần.** Đơn đã có nhóm hoặc đã có người care thì hệ thống không chia lại, kể cả khi đơn đổi trạng thái nhiều lần.
- **Chia đều dựa trên "ai ít đơn nhất / lâu chưa nhận nhất".** Nhóm/người đã đủ định mức trong ngày sẽ bị bỏ qua cho tới khi sang ngày mới.
- **Kho tổng:** nếu đơn vị dùng kho tổng, đơn của nhiều chi nhánh được gom về một cấu hình care đơn của chi nhánh tổng.
- **Trạng thái giao hàng quyết định thời điểm chia.** Chỉ những trạng thái được tick "chia số care đơn" mới kích hoạt; có thể đặt chia ngay hoặc chia trễ X phút.
- **Tắt nhận dữ liệu chỉ ảnh hưởng đơn chia về sau** — đơn đã giao cho người đó trước đó không bị thu hồi.
- **Số điện thoại khách có thể bị che** tùy cấu hình che số của tài khoản đang xem.

## Ảnh hưởng tới
- **Đơn hàng logistic:** mỗi lần chia sẽ gắn **nhóm care đơn** và/hoặc **người care đơn** lên đơn — đây là dữ liệu các màn xử lý kho, báo cáo care đơn, tác nghiệp care đơn dựa vào.
- **Báo cáo care đơn / báo cáo tác nghiệp care đơn:** số liệu "ai care bao nhiêu đơn" đến từ kết quả chia này.
- **Định mức & bộ đếm theo ngày:** mỗi lần chia làm tăng bộ đếm của nhóm/người trong ngày, ảnh hưởng lượt chia kế tiếp.
- **Lịch sử chia số:** mỗi đơn lưu lại nhật ký đã được chia thế nào, để truy vết khi cần.

Nếu đơn không có ai care hoặc chia lệch, gốc rễ thường nằm ở **cấu hình** (chưa bật, sai định mức, sai trạng thái giao hàng, tài khoản bị tắt nhận dữ liệu) chứ không phải lỗi ngẫu nhiên.
