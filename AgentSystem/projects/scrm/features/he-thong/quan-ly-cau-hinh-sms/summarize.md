---
alias: ["cấu hình SMS hệ thống", "duyệt tài khoản SMS", "phê duyệt đại lý SMS", "đơn vị dùng tài khoản SMS", "cấp phép tài khoản gửi tin", "AccountSmsUnitUsed", "cấu hình gateway SMS", "nhà cung cấp SMS", "ConfigSMS"]
module: he-thong
trang_thai: Core (nội bộ super-admin / kế toán hệ thống)
quyen: "Tìm/Thêm/Xem/Duyệt/Xóa yêu cầu `AccountSmsUnitUsed_*` — role DV-AD-TONG (admin tổng đơn vị) hoặc HT-KT (kế toán hệ thống). Mỗi đơn vị chỉ xem được bản ghi của chính mình (lọc theo IdDonVi)."
goi: cần product xác nhận
phu_thuoc: ["Tài khoản SMS (AccountSms) — tài khoản gửi tin được đăng ký dùng", "SMS gửi tin (SmsSend/Consumer) — kiểm tra đơn vị đã được duyệt trước khi gửi", "Cấu hình gateway SMS (ConfigSMS) — khai báo kết nối tới nhà cung cấp SMS để gửi tin (bổ sung Pha 2)"]
link_flow: ./flow.md
---

# Quản lý cấu hình SMS (duyệt đơn vị dùng tài khoản SMS)

## Là gì
Đây là nơi **quản lý và phê duyệt việc một đơn vị (chi nhánh/khách hàng SaaS) được dùng tài khoản SMS** của hệ thống/đại lý để gửi tin. Khi một đơn vị muốn dùng tài khoản SMS mặc định/đại lý, hệ thống tạo một bản ghi "đơn vị-dùng-tài-khoản" ở trạng thái **Đang đợi duyệt**; admin hệ thống/kế toán **Phê duyệt** (hoặc **Từ chối/Hủy phê duyệt**) để đơn vị đó được phép gửi tin qua tài khoản này.

Phục vụ **admin hệ thống (super-admin)** và **kế toán hệ thống**. Trên UI là màn "Quản lý phê duyệt tài khoản cấu hình hệ thống" — bảng danh sách đơn vị đăng ký + nút duyệt/hủy duyệt từng dòng.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm/lọc đơn vị đăng ký dùng tài khoản SMS (theo đơn vị/chi nhánh) | ✅ | DV-AD-TONG hoặc HT-KT |
| Thêm mới bản ghi đơn vị-dùng-tài-khoản | ✅ | DV-AD-TONG hoặc HT-KT |
| Xem chi tiết một bản ghi | ✅ | DV-AD-TONG/HT-KT, và bản ghi phải thuộc đơn vị đang đăng nhập |
| **Phê duyệt** đơn vị được dùng tài khoản | ✅ | DV-AD-TONG hoặc HT-KT; chuyển trạng thái Đang đợi → Đã duyệt |
| **Hủy phê duyệt / Từ chối** | ✅ | DV-AD-TONG hoặc HT-KT; chuyển sang Bị từ chối |
| Xóa bản ghi | ✅ | DV-AD-TONG hoặc HT-KT (kiểm tra tồn tại trước) |
| Kiểm tra "đơn vị mình đã được duyệt chưa" | ✅ | nội bộ — chỉ dựa trên đơn vị đăng nhập |

### Cấu hình gateway/nhà cung cấp SMS (ConfigSMS) *(bổ sung Pha 2 — ConfigSMS gateway)*
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm/lọc danh sách cấu hình gateway SMS (lưới) | ✅ | mọi user đã đăng nhập (chỉ chặn bằng đăng nhập, không có quyền riêng từng nút — ⚠️ xem Cần chú ý) |
| Xem chi tiết / lấy toàn bộ cấu hình gateway | ✅ | như trên |
| Thêm mới cấu hình gateway (mã, nhà cung cấp, brandname, khóa kết nối...) | ✅ | như trên |
| Cập nhật cấu hình gateway | ✅ | như trên; kiểm tra bản ghi tồn tại trước khi lưu |
| Xóa cấu hình gateway | ⚠️ | như trên; **lưu ý kỹ thuật**: kiểm tra "tồn tại trước khi xóa" hiện không chặn đúng (xem dev.md) |
| Lấy cấu hình gateway theo mã (cho hệ thống gửi tin đọc) | ⚠️ | **không cần đăng nhập** (`GetByMa` mở public) — ⚠️ rủi ro lộ khóa kết nối, xem Cần chú ý + dev.md |

## Để làm gì / cho ai
- **Admin hệ thống / kế toán hệ thống:** kiểm soát đơn vị nào được dùng tài khoản SMS chung/đại lý — chống lạm dụng, gắn với việc tính phí/nạp tiền SMS.
- **Hệ thống gửi tin:** trước khi gửi SMS cho một đơn vị, kiểm tra đơn vị đó đã được duyệt dùng tài khoản hay chưa.

## Cách dùng
1. Vào **Quản trị → Quản lý phê duyệt tài khoản cấu hình hệ thống** (cấu hình cấp phép tài khoản SMS).
2. Lọc theo **Đơn vị** / **Chi nhánh** → bấm **Tìm kiếm**.
3. Mỗi dòng hiển thị: đơn vị, chi nhánh, loại tin (SMS/Zalo), tên tài khoản, **trạng thái phê duyệt** (Đang đợi duyệt / Đã được duyệt / Bị từ chối), ngày cập nhật.
4. Bấm nút **Phê duyệt** (✓) để cho phép, hoặc **Hủy phê duyệt** (✗) để từ chối — có hộp xác nhận.

## Cần chú ý
- **Mỗi đơn vị chỉ thấy bản ghi của chính mình** (lọc theo đơn vị đăng nhập) — admin tổng đơn vị xem trong phạm vi đơn vị mình.
- Trạng thái phê duyệt có **3 mức**: Đang đợi duyệt (chưa quyết), Đã được duyệt, Bị từ chối — đọc đúng để biết đơn vị có gửi được tin không.
- Việc duyệt ở đây **không gửi tin** — chỉ cấp/thu quyền dùng tài khoản; gửi tin nằm ở module SMS.
- Có một thông báo lỗi backend ghi nhầm domain ("Không tìm thấy Q&A!") khi không tìm thấy bản ghi — chỉ là text sai, không ảnh hưởng nghiệp vụ (xem dev.md).

## Cấu hình gateway/nhà cung cấp SMS (ConfigSMS) *(bổ sung Pha 2 — ConfigSMS gateway)*
Ngoài phần duyệt đơn vị-dùng-tài-khoản ở trên, hệ thống còn có một phần **khai báo cấu hình kết nối tới nhà cung cấp SMS (SMS gateway)** — gọi là **ConfigSMS**. Đây là nơi lưu các thông tin để hệ thống biết **gửi tin qua nhà cung cấp nào, bằng tài khoản/khóa kết nối nào**: mã cấu hình, nhà cung cấp/server, brandname (tên thương hiệu hiển thị khi gửi tin), loại tin, có dùng tài khoản gốc hay không, bật/tắt sử dụng, chế độ thử nghiệm (sandbox)...

- Đây là **cấu hình kỹ thuật cấp quản trị hệ thống**, không phải màn cho khách hàng cuối. Người dùng cuối/chủ shop thường **không** thao tác trực tiếp ở đây.
- Khi soạn và gửi SMS, hệ thống đọc đúng cấu hình gateway theo mã để kết nối tới nhà cung cấp và gửi tin.

### Cần chú ý (ConfigSMS gateway)
- Phần cấu hình này chứa **khóa kết nối nhạy cảm** (khóa API/secret của nhà cung cấp, brandname). Lộ ra ngoài đồng nghĩa người khác có thể giả mạo gửi tin bằng tài khoản của bạn.
- **⚠️ Cảnh báo bảo mật:** đường lấy cấu hình theo mã hiện **không yêu cầu đăng nhập** — nếu đoán được mã cấu hình thì có thể lộ thông tin khóa kết nối. Đây là điểm cần đội kỹ thuật xử lý (đã ghi chi tiết ở dev.md, mục Bug-prone). *(cần product xác nhận có cho khách hàng cuối tự cấu hình gateway hay không)*

## Ảnh hưởng tới
- **Khả năng gửi SMS/Zalo của đơn vị:** đơn vị chưa được duyệt dùng tài khoản → có thể bị chặn gửi qua tài khoản đó.
- **Tài khoản SMS (AccountSms):** quan hệ đơn vị ↔ tài khoản được quản lý tại đây.
- **Kết nối tới nhà cung cấp SMS (ConfigSMS gateway):** đổi/tắt cấu hình gateway ảnh hưởng trực tiếp tới việc gửi tin thực tế qua nhà cung cấp đó.
