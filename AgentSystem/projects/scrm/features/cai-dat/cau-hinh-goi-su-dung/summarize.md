---
alias: ["nâng cấp gói", "gia hạn gói", "gói sử dụng", "đăng ký gói", "nạp tiền SMS", "lịch sử nạp tiền sms", "mã giới thiệu nâng cấp", "subscription"]
module: cai-dat
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — Nâng cấp gói: DV-AD-TONG (+ HT-CSKH hỗ trợ). Xem gói hiện tại/danh sách gói: DV-AD-TONG/CN/MCN, HT-CSKH. Yêu cầu nạp tiền SMS + xem lịch sử: DV-AD-TONG, HT-CSKH. Duyệt nâng cấp gói và duyệt nạp tiền SMS nằm phía admin hệ thống (SuperUser/HT-KT/HT-AD)."
phu_thuoc: ["Đơn vị (gói áp theo đơn vị)", "Mã giới thiệu (áp khi nâng cấp)", "Tài khoản SMS (AccountSms — số dư SMS sau khi nạp được duyệt)", "Bộ duyệt phía admin hệ thống (QuanTriArea)"]
link_flow: ./flow.md
---

# Cấu hình gói sử dụng

## Là gì
Đây là nơi đơn vị **xem gói đang dùng, nâng cấp/gia hạn gói**, và **nạp tiền cho tài khoản SMS**. Gồm:
- **Nâng cấp gói sử dụng:** xem gói hiện tại, chọn gói muốn lên, chọn thời hạn, áp mã giới thiệu (nếu có) rồi xác nhận. Gói quyết định hạn mức và tính năng đơn vị được dùng.
- **Nạp tiền SMS:** gửi yêu cầu nạp tiền vào tài khoản SMS để có số dư gửi tin SMS cho khách. Có màn tra **lịch sử nạp tiền**.

> Quan trọng: cả **nâng cấp gói** lẫn **nạp tiền SMS** đều phải qua bước **duyệt của admin hệ thống** (CSKH/kế toán hệ thống) thì mới có hiệu lực. Đơn vị chỉ tạo yêu cầu; không tự duyệt cho mình.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem gói hiện tại của đơn vị | ✅ | DV-AD-TONG/CN/MCN, HT-CSKH |
| Xem danh sách gói có thể nâng cấp | ✅ | DV-AD-*, HT-CSKH |
| Xem các mốc thời hạn gói | ✅ | chỉ login |
| Gửi yêu cầu nâng cấp / gia hạn gói | ✅ | DV-AD-TONG (+ HT-CSKH hỗ trợ) |
| Áp / kiểm tra mã giới thiệu khi nâng cấp | ✅ | DV-AD-TONG |
| **Duyệt yêu cầu nâng cấp gói** | ❌ (với đơn vị) | chỉ admin hệ thống (SuperUser/HT-KT/HT-AD) |
| Gửi yêu cầu nạp tiền SMS | ✅ | DV-AD-TONG, HT-CSKH |
| Xem lịch sử nạp tiền SMS | ✅ | DV-AD-TONG, HT-CSKH |
| **Duyệt yêu cầu nạp tiền SMS** | ❌ (với đơn vị) | chỉ admin hệ thống (cộng số dư sau khi duyệt) |

## Để làm gì / cho ai
- **Chủ shop / admin đơn vị:** mở rộng tính năng (lên gói cao hơn), gia hạn khi sắp hết hạn, nạp tiền SMS để chạy chiến dịch nhắn tin.
- **CSKH / kế toán hệ thống:** hỗ trợ đơn vị tạo yêu cầu và **duyệt** yêu cầu nâng cấp/nạp tiền.

## Cách dùng
**Nâng cấp / gia hạn gói:**
1. Vào **Cài đặt → Nâng cấp gói sử dụng**.
2. Xem gói hiện tại → chọn gói muốn lên và thời hạn.
3. (tùy chọn) Nhập **mã giới thiệu** để được ưu đãi; hệ thống kiểm tra số lượt còn áp dụng.
4. Bấm **Xác nhận** → tạo yêu cầu nâng cấp (chờ admin hệ thống duyệt).

**Nạp tiền SMS:**
1. Vào màn **Nạp tiền SMS / Lịch sử nạp tiền**.
2. Bấm **Nạp tiền**, nhập thông tin số tiền → gửi yêu cầu (trạng thái chờ duyệt).
3. Theo dõi trạng thái trong **lịch sử nạp tiền**; sau khi được duyệt, số dư SMS được cộng.

## Cần chú ý
- **Phải chờ duyệt mới có hiệu lực:** yêu cầu nâng cấp gói và nạp tiền SMS đều ở trạng thái chờ duyệt; admin hệ thống duyệt xong mới đổi gói / cộng số dư.
- **Mã giới thiệu có giới hạn lượt:** hệ thống kiểm tra số lượt còn lại của mã trước khi cho áp; hết lượt thì không áp được.
- **Nâng cấp đổi hạn mức tính năng:** sau khi gói áp dụng, quyền và giới hạn module của đơn vị thay đổi theo gói mới.
- **Phân vai rõ ràng:** đơn vị **tạo** yêu cầu; **duyệt** là việc của admin hệ thống — đơn vị không tự duyệt được.

## Ảnh hưởng tới
- **Quyền & hạn mức toàn đơn vị:** gói quyết định module/tính năng và giới hạn (ví dụ số thiết bị đăng nhập, dung lượng…).
- **Gửi SMS Marketing/CSKH:** số dư tài khoản SMS sau khi nạp được duyệt mới dùng để gửi tin.
- **Mã giới thiệu / CTV:** lượt áp mã ảnh hưởng tracking đại lý/giới thiệu.
