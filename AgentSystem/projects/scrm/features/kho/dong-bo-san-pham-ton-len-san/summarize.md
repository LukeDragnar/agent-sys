---
alias: ["đăng sản phẩm lên Shopee", "đẩy sản phẩm lên TikTok", "đồng bộ tồn lên sàn", "đồng bộ kho sàn TMĐT", "kết nối sản phẩm sàn", "map sản phẩm Shopee/TikTok", "sync tồn kho lên sàn", "đẩy hàng lên sàn"]
module: kho
trang_thai: cần product xác nhận (cần bật + cấu hình kết nối sàn TMĐT)
goi: cần product xác nhận
quyen: cần product xác nhận (thao tác trong module Kết nối sàn TMĐT — đồng bộ chạy tự động dưới nền theo cấu hình gian hàng, không theo thao tác từng user)
phu_thuoc: ["Kết nối gian hàng sàn TMĐT (UnitConnect — token Shopee/TikTok)", "Mapping kho sàn ↔ kho SCRM (WarehouseConnect)", "Quản lý sản phẩm (sản phẩm/SKU gốc để map)", "Quản lý tồn kho (nguồn số lượng đẩy lên sàn)"]
link_flow: ./flow.md
---

# Đồng bộ sản phẩm & tồn kho lên sàn TMĐT

## Là gì
Đây là cơ chế **đẩy thông tin sản phẩm và số lượng tồn kho từ SCRM lên các sàn TMĐT (Shopee, TikTok Shop)** — chiều ngược lại với việc nhận đơn từ sàn về.

Gồm 3 việc chính:
- **Map (kết nối) sản phẩm**: ghép 1 sản phẩm/biến thể (SKU) trên sàn với 1 sản phẩm/SKU trong SCRM, để hai bên "hiểu nhau là cùng một món hàng". Có thể kết nối **tự động** (ghép theo mã SKU trùng) hoặc **thủ công** (chọn tay từng cặp).
- **Đồng bộ tồn kho**: khi tồn kho trong SCRM thay đổi (bán, nhập, xuất, kiểm kho…), hệ thống tự đẩy số lượng mới lên sàn cho những sản phẩm đã map → tồn trên sàn luôn bám theo tồn thật trong kho.
- **Đăng sản phẩm mới lên sàn**: tạo sản phẩm nháp từ SCRM rồi đẩy lên Shopee/TikTok thành sản phẩm bán thật trên sàn.

Mục tiêu: bán nhiều kênh nhưng chỉ quản 1 kho duy nhất trong SCRM, tránh tình trạng sàn còn hàng nhưng kho đã hết (oversell).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Kết nối (map) sản phẩm tự động theo SKU trùng | ✅ | gian hàng đã kết nối token, sàn đã có sản phẩm |
| Kết nối (map) sản phẩm thủ công từng cặp | ✅ | chọn sản phẩm SCRM ↔ sản phẩm sàn rồi lưu |
| Hủy kết nối / gỡ map sản phẩm | ✅ | sản phẩm đang được map |
| Bật/tắt đồng bộ tồn cho từng sản phẩm | ✅ | sản phẩm đã map |
| Đồng bộ tồn kho lên sàn (tự động) | ✅ | bật "đồng bộ số lượng" ở gian hàng; chạy nền, không bấm tay |
| Đồng bộ tồn kho thủ công (bấm nút Đồng bộ) | ✅ | đẩy ngay số lượng hiện tại lên sàn |
| Đăng sản phẩm mới lên sàn (từ sản phẩm nháp) | ✅ | tạo nháp → gửi lên sàn |
| **Đồng bộ giá bán lên sàn (tự động)** | ❌ | có ô cấu hình "đồng bộ giá" nhưng phần đẩy giá lên sàn **chưa chạy** — xem mục Cần chú ý |
| Đặt ngưỡng "báo hết hàng khi tồn dưới X" | ✅ | cấu hình ở gian hàng / kho kết nối |
| Đặt tỷ lệ đồng bộ tồn (đẩy %số lượng) | ✅ | cấu hình ở gian hàng |
| Xem log / lịch sử tiến trình đồng bộ | ✅ | màn lịch sử tiến trình |

## Để làm gì / cho ai
- **Chủ shop bán đa kênh:** không phải sửa tồn tay trên từng sàn; nhập/bán ở đâu thì sàn tự cập nhật theo.
- **Nhân viên kho / vận hành:** tránh oversell (bán quá số tồn thật trên sàn rồi không có hàng giao).
- **Marketing / quản lý gian hàng:** đăng nhanh sản phẩm SCRM lên sàn mà không nhập lại từ đầu.

## Cách dùng
**Map sản phẩm (làm 1 lần cho mỗi món):**
1. Vào module **Kết nối sàn TMĐT** → chọn gian hàng (Shopee/TikTok) đã kết nối.
2. Mở **Danh sách sản phẩm** → hệ thống hiển thị sản phẩm trên sàn và trạng thái đã/chưa kết nối với SCRM.
3. Bấm **Kết nối tự động** (ghép theo SKU trùng) hoặc bấm từng dòng để **kết nối thủ công**, chọn đúng sản phẩm/SKU SCRM tương ứng → Lưu.
4. Bật **đồng bộ số lượng** cho sản phẩm muốn cho tồn chạy theo SCRM.

**Đồng bộ tồn (tự động sau khi đã map):**
1. Bật **"Đồng bộ số lượng"** ở cấu hình gian hàng.
2. Từ đó, mỗi khi tồn trong SCRM đổi, hệ thống tự đẩy lên sàn (chạy nền, có độ trễ vài chục giây đến vài phút).
3. Cần đẩy ngay → bấm nút **Đồng bộ** trên màn sản phẩm.

**Đăng sản phẩm mới lên sàn:**
1. Tạo **sản phẩm nháp** từ sản phẩm SCRM (chọn danh mục sàn, thuộc tính, giá, ảnh, cân nặng/kích thước).
2. Bấm **Gửi lên sàn** → hệ thống đăng lên Shopee/TikTok và trả về kết quả (thành công / lỗi từng dòng).

## Cần chú ý
- **Phải map sản phẩm trước thì tồn mới đồng bộ được.** Sản phẩm chưa kết nối (chưa map SKU) sẽ **không** được đẩy tồn — đây là lý do phổ biến nhất khiến "tồn trên sàn không nhảy".
- **Đồng bộ giá hiện chưa hoạt động.** Có ô bật "đồng bộ giá" và lưu được tỷ lệ giá, nhưng phần đẩy giá lên sàn chưa chạy → **giá trên sàn không tự đổi theo SCRM**. Vẫn phải chỉnh giá trực tiếp trên Shopee/TikTok. (cần product xác nhận lộ trình)
- **Tỷ lệ đồng bộ tồn (ProductSyncRate)** cho phép đẩy chỉ một phần tồn lên sàn (ví dụ 80%) để giữ hàng đệm; mặc định 100%.
- **Ngưỡng báo hết hàng:** nếu tồn (sau khi nhân tỷ lệ) **≤ ngưỡng cấu hình**, hệ thống đẩy lên sàn là **0 (hết hàng)** thay vì số thật — tránh khách đặt khi gần hết.
- **Phải khai báo đúng "kho đồng bộ"**: hệ thống chỉ đẩy tồn của đúng kho/chi nhánh đã cấu hình để đồng bộ; sửa/nhập ở kho khác sẽ không tác động lên sàn.
- **Có độ trễ.** Đồng bộ chạy theo tiến trình nền (khoảng mỗi 15 giây gom xử lý) nên tồn trên sàn không đổi tức thì.
- **Tồn âm trong SCRM** sẽ được đẩy thành 0 trên sàn (số lượng không bao giờ đẩy số âm lên sàn).

## Ảnh hưởng tới
- **Quản lý tồn kho** — là nguồn số liệu; mọi thay đổi tồn (bán/nhập/xuất/kiểm) đều có thể kích hoạt đẩy lên sàn.
- **Quản lý sản phẩm** — sản phẩm/SKU gốc để map; xóa hoặc đổi SKU có thể làm gãy kết nối với sàn.
- **Nhận đơn từ sàn** — dùng chung kết nối gian hàng và mapping kho; cấu hình sai kho ảnh hưởng cả hai chiều.
- **Kết nối sàn TMĐT (gian hàng)** — token hết hạn / mất kết nối thì đồng bộ dừng.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · Module: [../README.md](../README.md) · Kỹ thuật (chỉ dev): [dev.md](./dev.md)
