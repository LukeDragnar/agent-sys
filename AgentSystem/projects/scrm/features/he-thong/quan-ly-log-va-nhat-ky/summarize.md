---
alias: ["log hệ thống", "nhật ký data", "log delivery", "log ecommerce", "nhật ký giao vận", "nhật ký sàn", "gửi lại data lỗi", "resend data"]
module: he-thong
trang_thai: cần product xác nhận — dành cho admin hệ thống nội bộ SaaS (HT-AD)
goi: cần product xác nhận
quyen: "cần product xác nhận — chỉ admin hệ thống (HT-AD). Cả 2 màn Log Delivery và Log Ecommerce đều dùng [SBAuthorize] key nội bộ HT-AD."
phu_thuoc: ["Luồng xử lý giao vận (Delivery) — nơi data lỗi được gửi lại vào", "Luồng nhận đơn sàn TMĐT (Ecommerce) — nơi data lỗi được gửi lại vào", "MongoDB (kho lưu log)"]
link_flow: ./flow.md
---

# Quản lý log & nhật ký (Log Delivery / Log Ecommerce)

## Là gì
Đây là phần **nội bộ của nhà cung cấp SCRM** dùng để **tra cứu nhật ký (log) data trao đổi giữa hệ thống và các luồng tích hợp**, và **gửi lại (resend) những bản ghi bị lỗi** để hệ thống xử lý lại. Gồm 2 màn:
- **Log Delivery (nhật ký giao vận):** lưu lại từng lần hệ thống nhận/gửi data với luồng **giao vận** (đơn vị vận chuyển, cập nhật trạng thái giao hàng…). Khi một bản ghi bị lỗi, admin có thể bấm **Gửi lại** để đẩy nó vào luồng giao vận xử lý lần nữa.
- **Log Ecommerce (nhật ký sàn TMĐT):** lưu lại từng lần hệ thống nhận data từ **sàn thương mại điện tử** (Shopee, TikTok…). Tương tự, admin có thể **Gửi lại** bản ghi lỗi để hệ thống nhận/xử lý lại.

> Đây KHÔNG phải màn cho khách hàng cuối. Nó là công cụ vận hành/khắc phục sự cố của đội kỹ thuật khi data tích hợp bị lỗi (ví dụ đơn từ sàn không vào, trạng thái giao hàng không cập nhật).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tra cứu log Delivery theo điều kiện | ✅ | admin hệ thống (HT-AD) |
| Tra cứu log Ecommerce theo điều kiện | ✅ | admin hệ thống (HT-AD) |
| Gửi lại (resend) data Delivery bị lỗi | ✅ | HT-AD; bản ghi đang ở trạng thái lỗi |
| Gửi lại (resend) data Ecommerce bị lỗi | ✅ | HT-AD; bản ghi đang ở trạng thái lỗi |
| Thêm / Sửa / Xóa log thủ công | ❌ | log do hệ thống tự ghi, không nhập/sửa/xóa tay |
| Dọn (xóa) log cũ trong kho | ⚠️ | có job dọn log tự động; có một endpoint test gọi dọn log **không phân quyền** (known-issue — xem dev.md) |

## Để làm gì / cho ai
- **Đội kỹ thuật / admin hệ thống:** khi khách báo "đơn từ sàn không lên hệ thống" hoặc "trạng thái giao hàng không cập nhật" → vào đây tra log để biết data đã tới chưa, lỗi ở bước nào; nếu lỗi tạm thời thì **gửi lại** để xử lý lại mà không cần thao tác thủ công phức tạp.
- **CSKH nội bộ:** đối chiếu thời điểm/nội dung data đã trao đổi để giải thích cho khách.

## Cách dùng
**Tra cứu & gửi lại log Delivery:**
1. Vào **Hệ thống → Log Delivery**.
2. Đặt điều kiện lọc (theo thời gian, đơn vị, trạng thái lỗi…) rồi bấm **Tìm**.
3. Xem danh sách bản ghi log; với bản ghi **bị lỗi**, bấm **Gửi lại** để đẩy lại vào luồng giao vận.

**Tra cứu & gửi lại log Ecommerce:**
1. Vào **Hệ thống → Log Ecommerce**.
2. Đặt điều kiện lọc rồi bấm **Tìm**.
3. Với bản ghi **bị lỗi**, bấm **Gửi lại** để hệ thống nhận/xử lý lại data từ sàn.

## Cần chú ý
- **Gửi lại không đảm bảo thành công 100%:** nó chỉ đẩy bản ghi vào luồng xử lý lại; nếu nguyên nhân gốc (data sai định dạng, đơn vị cấu hình thiếu…) chưa được khắc phục thì có thể vẫn lỗi.
- **Gửi lại nhiều lần một bản ghi** có thể tạo xử lý trùng ở luồng đích — chỉ gửi lại khi chắc bản ghi thực sự cần xử lý lại.
- Log được lưu ở kho riêng (không nằm chung CSDL nghiệp vụ) và có thể **bị dọn theo job định kỳ**, nên log quá cũ có thể không còn.
- Đây là công cụ **chỉ dành cho nội bộ** — không mở cho khách hàng cuối.

## Ảnh hưởng tới
- **Luồng giao vận (Delivery):** bấm gửi lại data Delivery → luồng giao vận xử lý lại (có thể cập nhật lại trạng thái đơn, vận đơn).
- **Luồng nhận đơn sàn (Ecommerce):** bấm gửi lại data Ecommerce → đơn/data từ sàn được nhận và xử lý lại.
- Vì là **xử lý lại data thật**, thao tác gửi lại có thể tạo/cập nhật dữ liệu nghiệp vụ phía sau → cần cẩn trọng.
