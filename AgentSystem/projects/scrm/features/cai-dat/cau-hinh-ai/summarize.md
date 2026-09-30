---
alias: ["cấu hình AI", "token AI", "khóa AI", "api key AI", "prompt AI", "cấu hình prompt", "thiết lập trợ lý AI", "cài đặt AI"]
module: cai-dat
trang_thai: cần product xác nhận
goi: "Add-on AI — cần product xác nhận (tính năng AI thường là gói nâng cao / tiện ích riêng)"
quyen: "cần product xác nhận — Token AI: xem danh sách/chi tiết cần HT-AD, DV-AD-TONG, HT-CSKH; thêm/sửa/xóa cần DV-AD-TONG. Prompt AI (SandBox): code chỉ yêu cầu đăng nhập (permission key còn ở comment), thao tác scope theo đơn vị của người dùng."
phu_thuoc: ["Tính năng AI / trợ lý AI sử dụng token + prompt này để gọi model", "Chat AI theo fanpage — dùng token để bot trả lời", "Phân quyền & hồ sơ đơn vị (dữ liệu tách riêng theo từng shop)"]
link_flow: ./flow.md
---

# Cấu hình AI

## Là gì
Đây là nơi shop **khai báo các thông số để tính năng AI của hệ thống hoạt động**: cung cấp **token (khóa) để gọi model AI**, và soạn **bộ prompt** (câu lệnh/ngữ cảnh chỉ dẫn cho AI). Khi đã cấu hình, các tính năng AI (ví dụ trợ lý AI, bot trả lời chat) mới có "chìa khóa" gọi model và "kịch bản" để trả lời theo ý shop.

Tính năng gồm 2 phần:
- **Token AI**: danh sách token/khóa gọi model theo từng loại AI; có thể đặt một token làm mặc định.
- **Prompt AI (theo đơn vị)**: bộ prompt riêng của từng shop (mã, tên, nội dung prompt, mô tả). Có nút "Khởi tạo mẫu" để clone nhanh bộ prompt mẫu chuẩn cho shop mới.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách token AI | ✅ | HT-AD / DV-AD-TONG / HT-CSKH |
| Thêm token AI | ✅ | DV-AD-TONG |
| Sửa token AI | ✅ | DV-AD-TONG |
| Xóa token AI | ✅ | DV-AD-TONG |
| Đặt token mặc định | ✅ | trong form thêm/sửa token |
| Xem / lọc danh sách prompt AI | ✅ | đăng nhập (scope theo đơn vị) |
| Thêm prompt AI | ✅ | đăng nhập; tự gắn vào đơn vị của bạn |
| Sửa / Xóa prompt AI | ✅ | đăng nhập; chỉ trong phạm vi đơn vị mình |
| Khởi tạo bộ prompt mẫu | ⚠️ | chỉ chạy khi đơn vị **chưa có** prompt nào (nếu đã có sẽ báo "đã khởi tạo mẫu") |
| Xuất Excel danh sách prompt | ✅ | tùy quyền |

## Để làm gì / cho ai
- **Admin / chủ shop:** bật được tính năng AI cho shop bằng cách nạp token và bộ prompt phù hợp với nghiệp vụ của mình.
- **Người vận hành AI / CSKH:** điều chỉnh prompt để AI trả lời đúng giọng, đúng phạm vi sản phẩm/dịch vụ của shop.
- **Kỹ thuật triển khai:** khai báo token model, đặt token mặc định để các tính năng AI dùng.

## Cách dùng
**Token AI:**
1. Vào **Quản lý AI → Danh sách Token AI**.
2. Bấm **Thêm mới**, chọn **Loại AI**, dán **Token**, tích **Mặc định** nếu muốn dùng làm token chính → Lưu.
3. Có thể **Sửa**, **Xóa** token; bấm **Tải lại** để làm mới danh sách.

**Prompt AI:**
1. Vào màn **Cấu hình AI (SandBox) → Quản lý Prompt**.
2. Với shop mới: bấm **Khởi tạo mẫu** để có sẵn bộ prompt chuẩn, rồi chỉnh lại.
3. Bấm **Thêm mới** để tạo prompt (mã, tên, nội dung prompt, mô tả), hoặc **Sửa**/**Xóa** prompt có sẵn. Tìm theo tên/mã; có thể **Xuất Excel**.

## Cần chú ý
- **Token là khóa nhạy cảm** — sai/xóa token đang dùng có thể làm AI/bot ngừng trả lời.
- **Prompt và token gắn theo từng shop (đơn vị)** — bạn chỉ thấy và sửa được dữ liệu của shop mình.
- **"Khởi tạo mẫu" chỉ chạy một lần** khi shop chưa có prompt nào; nếu đã có prompt thì bấm sẽ không tạo thêm (báo "đã khởi tạo mẫu").
- Đây là nhóm tính năng nâng cao (AI) — **gói/quyền cần product xác nhận**; không phải shop nào cũng bật.
- Lưu ý kỹ thuật: một số màn AI hiện chưa siết quyền chặt (permission key còn để ở mức đăng nhập) — xem dev.md.

## Ảnh hưởng tới
- **Trợ lý AI / bot chat trả lời khách**: dùng token + prompt ở đây để gọi model và sinh câu trả lời.
- **Chat AI theo fanpage (PageAI)**: phụ thuộc token còn hiệu lực.
