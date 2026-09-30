---
alias: ["thiết lập tác nghiệp", "cấu hình tác nghiệp", "luật follow-up", "quy trình chăm sóc sale", "nếu - thì tác nghiệp", "cấu hình quy trình bán hàng"]
module: sales
trang_thai: cần cấu hình
goi: cần product xác nhận
quyen: "cần product xác nhận — code yêu cầu role admin để thêm/sửa/xóa/lọc luật: AD-TONG (admin tổng đơn vị), AD-CN (admin chi nhánh), AD-MCN. Riêng màn xem 'thông tin thiết lập' chỉ cần đăng nhập."
phu_thuoc: ["Danh mục tác nghiệp (các loại tác vụ sale như Gọi điện, Nhắn tin, Hẹn gặp…)", "Danh mục kết quả tác nghiệp (kết quả của mỗi lần tác nghiệp)", "Danh mục 'sau bao lâu' (mốc thời gian thực hiện bước tiếp theo)", "Cấu hình đơn vị (bật/tắt tác nghiệp theo chi nhánh)", "Nhắc việc / Notification (đọc lại bộ luật này để nhắc nhân viên)"]
link_flow: ./flow.md
---

# Thiết lập tác nghiệp (luật chăm sóc sale)

## Là gì
Đây là nơi shop **cài đặt sẵn quy trình chăm sóc khách dạng "NẾU – THÌ"** cho đội sale: *nếu nhân viên làm tác nghiệp A và ra kết quả B, thì hệ thống gợi ý làm tiếp tác nghiệp C sau khoảng thời gian D*. Ví dụ: "Nếu **Gọi điện** mà kết quả là **Khách bận**, thì **Gọi lại** sau **2 giờ**". Mỗi dòng là một quy tắc; cả bộ quy tắc giúp sale chăm khách theo đúng kịch bản, không bỏ sót bước nào.

Bộ luật này còn được phần **nhắc việc** của hệ thống đọc lại để tự nhắc nhân viên đến lúc phải làm bước tiếp theo.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm luật mới | ✅ | cần quyền admin (AD-TONG/AD-CN/AD-MCN) |
| Sửa luật | ✅ | cần quyền admin |
| Xóa luật | ✅ | cần quyền admin |
| Xem / lọc danh sách luật | ✅ | cần quyền admin |
| Copy bộ luật mẫu về đơn vị | ✅ | chỉ copy về **đơn vị của chính mình** (không copy chéo sang đơn vị khác qua màn này) |
| Kiểm tra "đã cấu hình tác nghiệp chưa" | ✅ | dùng để cảnh báo đơn vị chưa thiết lập |
| Đặt luật riêng theo từng chi nhánh | ⚠️ | chỉ khi đơn vị bật cấu hình "tác nghiệp theo chi nhánh"; nếu tắt thì luật dùng chung cho cả đơn vị |

## Để làm gì / cho ai
- **Chủ shop / quản lý sale:** chuẩn hóa quy trình chăm khách — mọi nhân viên đều chăm theo cùng một kịch bản, kết quả nào dẫn tới hành động nào đều rõ ràng.
- **Nhân viên sale:** không phải tự nhớ "lần này gọi xong thì khi nào nhắn lại" — hệ thống nhắc theo luật đã cài.
- **Admin đơn vị/chi nhánh:** người thiết lập và bảo trì bộ luật này.

## Cách dùng
1. Vào **Sale → Thiết lập tác nghiệp**.
2. Bấm **Thêm mới**, rồi chọn theo mẫu câu "Nếu – Thì":
   - **Nếu tác nghiệp** = chọn loại tác vụ (ví dụ Gọi điện).
   - **Có kết quả** = chọn kết quả của tác vụ đó (ví dụ Khách bận).
   - **Thì làm tác nghiệp tiếp** = chọn bước tiếp theo (ví dụ Gọi lại) — *tùy chọn*.
   - **Sau bao lâu** = chọn mốc thời gian (ví dụ sau 2 giờ) — *đi kèm với bước tiếp theo*.
3. **Lưu**. Có thể **sửa** hoặc **xóa** từng luật trong danh sách.
4. Nếu là đơn vị mới chưa biết cài gì, có thể bấm **Copy** để lấy bộ luật mẫu theo ngành nghề về đơn vị mình rồi chỉnh lại.

## Cần chú ý
- **Luật theo chi nhánh hay theo đơn vị là do cấu hình.** Nếu đơn vị bật "tác nghiệp theo chi nhánh", mỗi chi nhánh có bộ luật riêng; nếu tắt, cả đơn vị dùng chung. Cài nhầm chỗ sẽ thấy "luật biến mất" ở chi nhánh khác — đó là do phạm vi, không phải lỗi.
- **Không tạo trùng luật.** Hệ thống chặn việc thêm hai luật trùng cùng cặp (Nếu + Kết quả). Khi báo "Cấu hình tác nghiệp đã tồn tại" nghĩa là luật đó đã có rồi.
- **Bước tiếp theo và "sau bao lâu" nên đi cùng nhau.** Về mặt thiết kế, nếu đã chọn bước tiếp theo thì phải chọn cả mốc thời gian (và ngược lại). *(Cần chú ý: ràng buộc này hiện không chặt — xem dev.md.)*
- **Copy chỉ về đơn vị của bạn.** Màn này luôn copy bộ luật về chính đơn vị đang đăng nhập, không thể copy sang đơn vị khác.
- Nếu một tác nghiệp/kết quả được dùng trong luật **bị xóa** khỏi danh mục, luật vẫn hiển thị nhưng vế đó được đánh dấu **"(Deleted)"** để biết cần sửa.

## Ảnh hưởng tới
- **Nhắc việc / thông báo cho sale:** hệ thống nhắc việc đọc bộ luật này để biết khi nào nhắc nhân viên làm bước tiếp theo. Sửa luật → ảnh hưởng nội dung nhắc.
- **Quy trình chăm sóc khách của cả đội sale:** đổi luật là đổi kịch bản chăm khách cho mọi nhân viên trong phạm vi (đơn vị/chi nhánh).
- **Danh mục tác nghiệp / kết quả / sau bao lâu:** luật chỉ chọn được những giá trị đã khai báo sẵn ở các danh mục này.
