---
alias: ["chiến dịch khách hàng", "tập điều kiện khách hàng", "bộ lọc khách hàng", "lọc khách theo điều kiện", "customer condition filter", "tập khách hàng theo điều kiện", "lọc đối tượng chiến dịch"]
module: marketing
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — CRUD tập điều kiện theo quyền marketing; phạm vi xem theo cây quản lý (đơn vị → chi nhánh → người tạo). Vai/gói cụ thể cần product xác nhận."
phu_thuoc: ["Danh sách khách hàng + Danh mục KH (hạng, nghề nghiệp… làm điều kiện lọc)", "Đơn hàng (điều kiện theo đơn hàng / tổng tiền mua)", "Nhãn sản phẩm (điều kiện đơn hàng theo nhãn SP)", "Chiến dịch gửi (SMS/Zalo/voucher dùng tập điều kiện này để chọn khách)"]
link_flow: ./flow.md
---

# Chiến dịch khách hàng

## Là gì
Đây là nơi **tạo các "tập điều kiện" để lọc ra nhóm khách hàng mục tiêu** cho chiến dịch — ví dụ "khách hạng VIP, sinh nhật tháng 6, đã mua trên 5 triệu". Mỗi tập điều kiện là một bộ tiêu chí ghép lại; chiến dịch (gửi SMS/Zalo, phát voucher…) sẽ tham chiếu tập này để biết **gửi cho ai**.

Có 2 cách dựng tập:
- **Lọc theo điều kiện:** ghép nhiều tiêu chí (đơn hàng, ngày sinh, địa chỉ, tổng tiền mua, điểm tích lũy, hạng khách, tên, nghề nghiệp, giới tính) nối với nhau bằng VÀ / HOẶC.
- **Lọc theo danh sách số điện thoại:** dán/nhập sẵn một danh sách SĐT → tạo tập từ chính danh sách đó.

Tập có thể đặt **quét tự động hằng ngày** để luôn cập nhật danh sách khách thỏa điều kiện.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tạo tập điều kiện (theo điều kiện) | ✅ | tùy quyền marketing |
| Tạo tập từ danh sách SĐT | ✅ | cùng chức năng Thêm mới, chọn kiểu "danh sách SĐT" |
| Sửa tập điều kiện | ✅ | tùy quyền |
| Xóa tập điều kiện | ✅ | tùy quyền (⚠️ chưa chắc chặn xóa khi đang có chiến dịch dùng — xem mục Cần chú ý) |
| Xem danh sách / chi tiết tập | ✅ | tùy quyền xem (theo phạm vi cây quản lý) |
| Bật quét tự động hằng ngày | ✅ | cờ trên tập (job quét tự động) |
| Ghép điều kiện bằng VÀ / HOẶC | ✅ | cấu hình trong tập |

## Để làm gì / cho ai
- **Marketing:** dựng nhóm khách mục tiêu cho từng chiến dịch (tri ân VIP, kích cầu khách lâu không mua, mừng sinh nhật…).
- **CSKH:** lọc nhóm khách theo tiêu chí để chăm sóc.
- **Quản lý:** tái sử dụng tập điều kiện cho nhiều chiến dịch, không phải lọc lại từ đầu mỗi lần.

## Cách dùng
1. Vào **Marketing → Tập điều kiện khách hàng**.
2. **Thêm mới** tập → chọn cách dựng:
   - **Theo điều kiện:** thêm các tiêu chí (ví dụ "Hạng = VIP" VÀ "Tháng sinh = 6" VÀ "Tổng tiền mua ≥ 5.000.000"). Chọn nối các điều kiện bằng VÀ hay HOẶC.
   - **Theo danh sách SĐT:** dán/nhập danh sách số điện thoại.
3. (Tùy chọn) Bật **quét tự động hằng ngày** để tập luôn cập nhật khách mới thỏa điều kiện.
4. Lưu → tập sẵn sàng để chiến dịch (gửi SMS/Zalo, phát voucher…) chọn dùng.

## Cần chú ý
- **Tập điều kiện là "khuôn lọc", không phải danh sách cố định.** Khi quét, hệ thống áp điều kiện lên dữ liệu khách hiện tại → danh sách khách có thể thay đổi theo thời gian (nếu bật quét tự động).
- **Sửa điều kiện ảnh hưởng mọi chiến dịch đang dùng tập đó** — đổi tiêu chí thì tập khách của tất cả chiến dịch tham chiếu cũng đổi theo.
- **Xóa tập đang được chiến dịch dùng có thể làm chiến dịch "mồ côi"** (trỏ tới tập đã mất) — hiện chưa chắc có chặn, cẩn thận khi xóa (xem dev — cần product xác nhận).
- **Hai kiểu tập khác nhau:** "theo điều kiện" và "theo danh sách SĐT" — chọn sai kiểu khi tạo/sửa sẽ báo lỗi "Kiểu loại bộ lọc không phù hợp".
- **Điều kiện đơn hàng** từ 02/06/2026 đã gộp thêm lọc theo **nhãn sản phẩm** — khi lọc theo đơn hàng có thể kèm nhãn SP.
- **Phạm vi xem theo cây quản lý:** bạn thấy các tập trong phạm vi mình (đơn vị → chi nhánh → người tạo theo cấp quản lý).

## Ảnh hưởng tới
- **Chiến dịch gửi (SMS / Zalo / voucher)** — dùng tập điều kiện để chọn khách nhận.
- **Danh sách khách hàng** — tập lọc trên chính dữ liệu khách (hạng, đơn hàng, điểm…).
- **Tệp khách đã lọc (quét ra)** — tập là input để hệ thống quét ra danh sách khách cụ thể.
