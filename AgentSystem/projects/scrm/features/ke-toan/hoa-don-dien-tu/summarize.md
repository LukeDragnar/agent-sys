---
alias: ["hóa đơn điện tử", "HĐĐT", "xuất hóa đơn", "phát hành hóa đơn", "MeInvoice", "MISA", "Viettel Invoice", "SInvoice", "tải hóa đơn PDF", "tra cứu hóa đơn"]
module: ke-toan
trang_thai: cần bật (tiện ích) — cần product xác nhận tên gói
goi: cần product xác nhận — tính năng gate qua tiện ích hóa đơn MISA và/hoặc tiện ích hóa đơn Viettel; đơn vị chưa bật tiện ích sẽ bị chặn
quyen: "cấu hình tài khoản/mẫu HĐ: admin đơn vị/chi nhánh (DV-AD-TONG, DV-AD-CN, DV-AD-MCN); xem/đồng bộ mẫu thêm kế toán (DV-KT). Phát hành/tra cứu từ màn đơn: gate bằng tiện ích (không phân quyền key per-action). — chi tiết cần product xác nhận"
phu_thuoc: ["Tài khoản nhà cung cấp HĐĐT (MISA/MeInvoice hoặc Viettel/SInvoice) đã khai báo và kết nối", "Mẫu hóa đơn đã đồng bộ từ nhà cung cấp + đặt 1 mẫu mặc định", "Đơn hàng (POS/cửa hàng) hoặc đơn logistic (online) — nguồn dữ liệu để xuất hóa đơn", "Cấu hình thuế/thông tin xuất hóa đơn của khách"]
link_flow: ./flow.md
---

# Hóa đơn điện tử (MeInvoice/MISA + Viettel/SInvoice)

## Là gì
Đây là tính năng giúp shop **phát hành và tra cứu hóa đơn điện tử (HĐĐT)** cho đơn hàng, kết nối với **2 nhà cung cấp**: **MeInvoice (của MISA)** và **Viettel SInvoice**. Shop khai báo tài khoản nhà cung cấp, đồng bộ các mẫu hóa đơn, rồi từ màn đơn hàng bấm xuất hóa đơn — hệ thống gửi sang nhà cung cấp để ký và phát hành, sau đó khách có thể nhận hóa đơn (kèm thông báo SMS/Zalo nếu cấu hình).

Tính năng gồm 2 phần:
- **Cấu hình HĐĐT**: nơi khai báo **tài khoản** MISA/Viettel và quản lý **mẫu hóa đơn** (đồng bộ từ nhà cung cấp, đặt mẫu mặc định, ánh xạ các trường tùy biến). Có 2 tab: tab MeInvoice và tab Viettel.
- **Phát hành & tra cứu**: từ màn đơn hàng (cửa hàng/online) bấm xuất hóa đơn, tạo hóa đơn nháp, tải file PDF; và màn tra cứu lịch sử hóa đơn toàn đơn vị (tìm + xuất Excel).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm / sửa tài khoản MISA (MeInvoice) | ✅ | admin đơn vị/chi nhánh (DV-AD-*) |
| Thêm / sửa tài khoản Viettel (SInvoice) | ✅ | admin đơn vị/chi nhánh (DV-AD-*) |
| Xóa tài khoản nhà cung cấp | ✅ | admin đơn vị/chi nhánh (DV-AD-*) |
| Đồng bộ mẫu hóa đơn từ nhà cung cấp | ✅ | cần đã có tài khoản; thêm quyền kế toán (DV-KT) |
| Đặt mẫu hóa đơn mặc định | ✅ | tùy quyền; mẫu mặc định dùng khi phát hành |
| Cập nhật ánh xạ trường tùy biến của mẫu | ✅ | mọi user đăng nhập (⚠️ endpoint đã bỏ quyền — xem dev.md) |
| Xem trước (preview) hóa đơn | ✅ | mọi user đăng nhập (⚠️ endpoint đã bỏ quyền) |
| Phát hành hóa đơn thật cho đơn | ✅ | đơn vị phải bật tiện ích MISA hoặc Viettel; đơn đủ điều kiện |
| Tạo hóa đơn **nháp** | ✅ | chỉ MISA + Viettel (nhà cung cấp khác không hỗ trợ nháp) |
| Đồng bộ nháp → phát hành | ✅ | có hóa đơn nháp; gate tiện ích |
| Tải file PDF hóa đơn | ✅ | sau khi đã phát hành (đơn có số hóa đơn); gate tiện ích |
| Tra cứu HĐĐT toàn đơn vị + lọc | ✅ | gate tiện ích |
| Xuất Excel danh sách HĐĐT | ✅ | gate tiện ích |
| Phát hành HĐ qua **Phiếu xuất kho** bằng Viettel | ❌ | phiếu xuất kho **chỉ** hỗ trợ phát hành qua MeInvoice/MISA |
| Tạo nháp với nhà cung cấp ngoài MISA/Viettel | ❌ | báo "Nhà cung cấp hóa đơn không hỗ trợ tạo nháp" |

## Để làm gì / cho ai
- **Kế toán:** phát hành hóa đơn cho đơn đã bán, tạo nháp để soát trước khi phát hành thật, tra cứu lịch sử hóa đơn, tải PDF gửi khách.
- **Admin đơn vị:** khai báo tài khoản nhà cung cấp HĐĐT, đồng bộ và cấu hình mẫu hóa đơn cho đơn vị.
- **Thu ngân/cửa hàng:** bấm xuất hóa đơn ngay tại màn thanh toán đơn (nếu đơn vị bật tiện ích).
- **Chủ shop:** đảm bảo mọi đơn xuất hóa đơn hợp lệ, tra soát số hóa đơn đã phát hành.

## Cách dùng

**Khai báo tài khoản & mẫu (Cấu hình HĐĐT):**
1. Vào **Kế toán → Cấu hình hóa đơn điện tử**.
2. Chọn tab nhà cung cấp: **MeInvoice (MISA)** hoặc **Viettel (SInvoice)**.
3. Bấm **Thêm mới**, nhập thông tin kết nối tài khoản (theo nhà cung cấp), lưu.
4. Bấm **Đồng bộ mẫu** để kéo danh sách mẫu hóa đơn từ nhà cung cấp về.
5. Chọn 1 mẫu, bật **Mặc định** — mẫu này sẽ được dùng khi phát hành.
6. (Tùy chọn) Cấu hình **ánh xạ trường tùy biến** cho mẫu, và bấm **Xem trước** để kiểm tra.

**Phát hành hóa đơn cho đơn:**
1. Mở đơn hàng (cửa hàng/POS hoặc đơn online), bấm **Xuất hóa đơn** / **Phát hành HĐĐT**.
2. Hệ thống nhận yêu cầu và xử lý **ngầm (bất đồng bộ)** — sẽ báo "Đã nhận yêu cầu xuất hóa đơn điện tử".
3. Đợi vài giây rồi mở lại popup **Danh sách hóa đơn điện tử** của đơn để xem trạng thái (đang phát hành / thành công / thất bại).
4. Khi đã có số hóa đơn, bấm **Tải** để lấy file PDF.

**Tra cứu lịch sử:**
1. Vào màn **Tra cứu hóa đơn điện tử** (toàn đơn vị).
2. Lọc theo điều kiện (mã đơn, số HĐ, khoảng thời gian, nháp/thật, thủ công/tự động…), bấm tìm.
3. Bấm **Xuất Excel** để tải danh sách.

## Cần chú ý
- **Phải bật tiện ích** MISA hoặc Viettel cho đơn vị thì các nút xuất/tra cứu hóa đơn mới dùng được. Chưa bật → báo "Bạn không có quyền sử dụng tiện ích này".
- **Phát hành là bất đồng bộ:** bấm xong KHÔNG có kết quả ngay. Hệ thống chỉ báo "đã nhận yêu cầu"; phải mở lại để xem hóa đơn đã phát hành thành công chưa.
- **Phiếu xuất kho chỉ phát hành qua MISA (MeInvoice)** — không phát hành được qua Viettel.
- **Tài khoản dùng chung 2 nhà cung cấp:** phân biệt bằng loại nhà cung cấp. Khi tạo tài khoản qua tab Viettel, nếu thiếu thông tin loại nhà cung cấp, hệ thống có thể gán nhầm thành MISA (xem dev.md) — nên kiểm tra lại tài khoản sau khi tạo.
- **Hai nguồn tra cứu khác nhau:** popup hóa đơn theo từng đơn và màn tra cứu tổng lấy từ 2 kho dữ liệu khác nhau, có thể lệch nếu dữ liệu chưa đồng bộ kịp.
- **Nháp chỉ hỗ trợ MISA + Viettel.** Nhà cung cấp khác không tạo được nháp.

## Ảnh hưởng tới
- **Đơn hàng (cửa hàng + online):** nút phát hành/nháp/tải hóa đơn nằm trên màn đơn; trạng thái phát hành ghi lại theo đơn.
- **Tạo hóa đơn tự động:** dùng chung tài khoản + mẫu mặc định đã cấu hình ở đây (xem feature "Tạo hóa đơn tự động").
- **Thông báo SMS/Zalo (ZNS):** sau phát hành thành công, hệ thống có thể đẩy tin báo hóa đơn cho khách.
