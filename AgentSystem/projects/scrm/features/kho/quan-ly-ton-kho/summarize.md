---
alias: ["quản lý tồn kho", "tồn kho", "xem tồn kho", "kiểm tra tồn kho còn bao nhiêu", "số lượng hàng trong kho"]
module: kho
trang_thai: đang dùng
goi: cần product xác nhận
quyen: Admin tổng / Admin chi nhánh / Kế toán / Quản lý dịch vụ / Quản kho (Sale chỉ xem nếu kho không bị ẩn) — chi tiết cần product xác nhận
phu_thuoc: [Quản lý sản phẩm (SP/nhóm SP), Chi nhánh, Phiếu nhập/xuất/chuyển kho/kiểm kho (nguồn làm thay đổi tồn)]
link_flow: ./flow.md
---

# Quản lý tồn kho

## Là gì
Màn hình cho bạn **xem số lượng từng sản phẩm đang còn trong từng kho / chi nhánh**: tồn hiện tại, số đang "giữ chỗ" cho đơn chưa hoàn thành, và số thực bán được. Đây là chỗ tra cứu, **không phải chỗ nhập/xuất hàng** — số liệu được tính tự động từ các phiếu nhập, xuất, chuyển kho, kiểm kho.

Bộ tính năng gồm các màn:
- **Quản lý tồn kho** — bảng tồn theo từng sản phẩm trong 1 kho.
- **Kiểm tra tồn kho** — tra 1 sản phẩm tồn bao nhiêu ở **tất cả chi nhánh** (kèm dự báo hết hàng nếu bật cấu hình).
- **Danh sách kho** — danh mục các kho của chi nhánh.
- **Cấu hình kho** / **Cấu hình kho chi nhánh** — thiết lập cách kho hoạt động (kho mặc định, cho chốt âm, ẩn kho với sale, giới hạn tồn...).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem tồn theo kho / sản phẩm | ✅ | phải chọn nhóm SP mới ra dữ liệu; Sale chỉ xem nếu kho không bị ẩn |
| Kiểm tra 1 SP ở mọi chi nhánh | ✅ | màn Kiểm tra tồn kho |
| Sửa số tồn trực tiếp tại đây | ❌ | màn chỉ tra cứu; tồn thay đổi qua phiếu nhập/xuất/chuyển/kiểm kho |
| Xuất Excel tồn kho | ✅ | bắt buộc chọn nhóm SP; chặn export > 10.000 bản ghi |
| Xuất Excel báo cáo tồn kho đơn vị | ✅ | chỉ Admin tổng / CN / MCN; non-admin bị ép chi nhánh của mình |
| Thêm / Sửa kho (Danh sách kho) | ✅ | Admin tổng / CN / MCN |
| Xóa kho | ⚠️ | Admin tổng/CN/MCN; tắt kho tổng → chặn nếu là kho mặc định hoặc đã có tồn |
| Sửa cấu hình kho / kho chi nhánh | ✅ | UpdateOrCreate chỉ AD-TONG / CN / MCN |
| Xem giá nhập trung bình (giá vốn) | ⚠️ | có thể bị ẩn theo chi nhánh / vai trò |

## Để làm gì / cho ai
- **Cho chủ shop / quản lý:** biết hàng nào sắp hết để nhập thêm, hàng nào tồn đọng.
- **Cho thu ngân / sale:** kiểm tra còn hàng để bán trước khi lên đơn.
- **Cho kế toán / quản kho:** đối chiếu tồn kho với thực tế, xem giá vốn bình quân.

## Cách dùng (các bước thường gặp)
1. Vào **Kho → Quản lý tồn kho**.
2. Chọn **kho** và **nhóm sản phẩm** muốn xem (bắt buộc chọn nhóm thì mới ra dữ liệu).
3. Gõ tên / mã / mã vạch sản phẩm vào ô tìm kiếm nếu muốn lọc nhanh.
4. Xem cột **Số lượng tồn**, **Đang chờ xuất**, **Có thể bán** (= tồn − chờ xuất).
5. Cần tra 1 sản phẩm ở mọi chi nhánh → dùng màn **Kiểm tra tồn kho**, chọn sản phẩm.
6. Cần xuất ra Excel → bấm **Xuất Excel** trên bảng.

## Cần chú ý (cạm bẫy)
- **Tồn ≠ bán được.** Số bán được = Tồn − Số đang chờ xuất (hàng đã được giữ cho đơn chưa hoàn thành). Đừng nhìn mỗi cột Tồn.
- **Phải chọn nhóm sản phẩm** thì bảng mới hiển thị — không chọn sẽ ra bảng trống, không phải lỗi.
- **Số liệu chỉ đúng khi phiếu đã xác nhận.** Phiếu nhập/xuất/chuyển còn ở trạng thái nháp thì chưa cộng/trừ tồn → thấy tồn "sai" thường là do còn phiếu chưa xác nhận.
- **Giá nhập trung bình (giá vốn) có thể bị ẩn** với một số chi nhánh / vai trò theo cấu hình — không thấy giá không phải lỗi.
- **Kho ẩn với sale:** nếu cấu hình bật "ẩn kho với sale", nhân viên sale sẽ không thấy kho đó.

## Ảnh hưởng tới
- **Bán hàng / lên đơn** — quyết định còn hàng để bán hay không, có cho bán âm hay không.
- **Phiếu nhập / xuất / chuyển kho / kiểm kho** — là nguồn làm thay đổi con số tồn ở đây.
- **Báo cáo kho** và **đồng bộ sàn TMĐT (Shopee/TikTok)** — dùng chung dữ liệu tồn.

## Link
→ [flow.md](./flow.md) · Hỏi đáp: [faq.md](./faq.md) · Module: [../README.md](../README.md)
