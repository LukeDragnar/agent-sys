---
alias: ["quản lý nguồn dữ liệu", "nguồn dữ liệu", "sản phẩm landing", "landing page", "kênh tiếp nhận", "nguồn contact", "nguồn khách"]
module: marketing
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — CRUD nguồn dữ liệu (landing) cho admin + quản lý/lead/nhân viên MKT (gán user MKT chỉ admin); một số nút (xem theo id, đếm chia chat) chỉ cần đăng nhập; vài endpoint mở công khai cho web booking / ecommerce (anonymous). Danh mục kênh tiếp nhận chỉ admin hệ thống + admin tổng (HT-AD, DV-AD-TONG)."
phu_thuoc: ["Cấu hình chia số / chia chat (nguồn gắn cấu hình phân bổ)", "Facebook / Ads (đẩy số liệu landing theo quảng cáo)", "Contact / khách hàng (gán nguồn + kênh tiếp nhận khi tạo)", "Báo cáo Marketing (lead/ngân sách theo landing)"]
link_flow: ./flow.md
---

# Quản lý nguồn dữ liệu

## Là gì
Đây là nơi **khai báo và quản lý các "nguồn dữ liệu" marketing** — nơi khách đổ về từ đâu — để hệ thống biết lead/contact đến từ kênh nào, gắn cấu hình phân bổ phù hợp, và thống kê hiệu quả theo nguồn.

Gồm 2 phần:
- **Nguồn dữ liệu (sản phẩm landing):** mỗi nguồn = một landing page / chiến dịch thu lead. Tại đây cấu hình **chia số / chia chat / kênh bán hàng**, gán **nhân viên Marketing phụ trách**, cho/không cho sale tự tạo contact, và **nhập danh sách nguồn từ Excel**. Còn quản lý **số liệu landing** (theo FB Ads/ngày) để biết chi phí, số lead theo ngày.
- **Danh mục kênh tiếp nhận:** danh sách các kênh nguồn vào của khách (Facebook, Zalo, Hotline, Website…) để phân loại contact/khách khi tiếp nhận.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm / Sửa nguồn dữ liệu | ✅ | admin + quản lý/lead/nhân viên MKT |
| Xóa nguồn dữ liệu | ✅ | admin + nhân viên MKT |
| Cấu hình chia số cho nguồn | ✅ | key riêng (vai MKT) |
| Cấu hình chia chat cho nguồn | ✅ | ⚠️ chỉ chặn bằng danh sách vai, **không có key riêng** (xem dev) |
| Cấu hình kênh bán hàng cho nguồn | ✅ | key riêng (vai MKT) |
| Bật/tắt cho sale tự tạo contact thủ công | ✅ | vai MKT |
| Gán nhân viên Marketing cho nguồn | ✅ | chỉ admin (AD-TONG/CN/MCN) |
| Xem danh sách / dropdown nguồn | ✅ | danh sách vai rộng (AD/SALE/MKT/KHO/KT…) |
| Nhập nguồn dữ liệu từ Excel | ✅ | vai MKT |
| Quản lý số liệu landing (theo FB Ads/ngày) | ✅ | chỉ cần đăng nhập |
| Thêm / Sửa / Xóa / Xem kênh tiếp nhận | ✅ | chỉ admin hệ thống + admin tổng (HT-AD, DV-AD-TONG) |

## Để làm gì / cho ai
- **Quản lý Marketing:** dựng nguồn cho mỗi landing/chiến dịch, gán người phụ trách, cấu hình cách lead được chia về cho ai.
- **Nhân viên Marketing:** nhập nguồn mới, theo dõi số liệu landing.
- **Quản trị đơn vị:** chuẩn hóa danh mục kênh tiếp nhận để phân loại nguồn khách nhất quán.
- **Hệ thống ngoài (web booking, ecommerce, Facebook):** đọc thông tin landing / đẩy số liệu quảng cáo về nguồn.

## Cách dùng
**Quản lý nguồn dữ liệu (landing):**
1. Vào **Marketing → Quản lý nguồn dữ liệu**.
2. **Thêm mới** nguồn → đặt tên, gắn cấu hình chia số / chia chat / kênh bán hàng, gán nhân viên MKT phụ trách.
3. Bật/tắt **cho sale tự tạo contact thủ công** tùy chính sách.
4. Vào tab **số liệu landing** để xem/nhập số liệu theo FB Ads/ngày (chi phí, lead).
5. Có thể **nhập danh sách nguồn từ Excel**: tải template → import file → kiểm tra danh sách tạm → đưa vào nguồn dữ liệu.

**Danh mục kênh tiếp nhận:**
1. Vào **Quản lý kênh tiếp nhận** (cần quyền admin tổng/hệ thống).
2. **Thêm mới** kênh (Facebook, Zalo, Hotline, Website…) → lưu. Có thể **sửa / xóa**.

## Cần chú ý
- **Cấu hình chia số / chia chat gắn vào nguồn** quyết định lead của nguồn đó được chia cho ai → sai cấu hình thì lead "đi nhầm người" hoặc không ai nhận.
- **Gán nhân viên Marketing chỉ admin làm được** — nhân viên MKT thường không tự gán mình vào nguồn.
- **Danh mục kênh tiếp nhận chỉ admin tổng/hệ thống quản lý** — non-admin đơn vị không thêm/sửa/xóa kênh được. Đây là phân quyền.
- **Sau khi thêm/sửa kênh tiếp nhận, danh sách có thể chưa cập nhật ngay** (cache chỉ làm mới khi xóa) — xem dev; nếu thấy kênh mới chưa xuất hiện, thử lại sau hoặc xóa-lại để làm mới.
- **Một số thông tin landing mở công khai** cho web booking / ecommerce (không cần đăng nhập) — chỉ trả theo id, không lộ dữ liệu nhạy cảm.
- **Nút "cập nhật toàn bộ data landing"** là thao tác bảo trì hàng loạt — cẩn thận khi dùng.

## Ảnh hưởng tới
- **Chia số / chia chat** — nguồn quyết định lead/chat của nguồn đó được phân bổ thế nào.
- **Quản lý Lead / Contact** — lead/contact gắn nguồn + kênh tiếp nhận khai báo ở đây.
- **Báo cáo Marketing** — báo cáo lead/ngân sách "theo nguồn / theo landing" lấy từ chính nguồn này.
- **Facebook / Ads / Ecommerce** — đẩy số liệu landing và đọc thông tin nguồn qua các kênh kết nối.
