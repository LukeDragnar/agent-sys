---
title: Quản lý đơn hàng hoàn
alias:
  - đơn hoàn
  - trả hàng
  - hoàn hàng TMĐT
  - đơn trả hàng hoàn tiền
  - đơn hoàn Shopee/TikTok
module: kho
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: cần product xác nhận
phu_thuoc:
  - ecommerce_connect
  - don-hang-logistic (đơn bán gốc)
  - phieu-ban-tra-lai (HBBTL — kế toán)
  - warehouse-product (nhập kho hoàn)
---

# Quản lý đơn hàng hoàn

## Là gì

Đơn hàng hoàn là đơn **khách trả lại hàng / đòi hoàn tiền** sau khi đã mua. Màn hình
"Đơn hàng hoàn" (module Kho) là nơi tập trung **toàn bộ đơn hoàn của shop**, gom về 1 chỗ
để theo dõi và xử lý — thay vì phải vào từng sàn TMĐT xem riêng.

Phần lớn đơn hoàn đổ về **tự động từ các sàn thương mại điện tử** (Shopee, TikTok, Lazada):
khi khách bấm "Trả hàng / Hoàn tiền" trên sàn, SCRM nhận thông tin và tạo đơn hoàn tương ứng.
Mỗi đơn hoàn luôn gắn với **một đơn bán gốc**, biết khách nào, sản phẩm nào, lý do hoàn,
số tiền hoàn, hàng đang ở đâu (đang gửi về, đã về kho...) và trạng thái xử lý của sàn.

Có 2 dạng xử lý chính:
- **Trả hàng & hoàn tiền**: khách gửi hàng về, shop nhận lại hàng (nhập kho hoàn) rồi mới hoàn tiền.
- **Chỉ hoàn tiền**: khách giữ hàng, shop chỉ hoàn tiền (không nhập kho).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem / lọc / tìm đơn hoàn (theo ngày, sàn, gian hàng, trạng thái, lý do, mã đơn) | ✅ | tập trung đơn hoàn mọi sàn về 1 màn |
| Lọc theo tab nghiệp vụ (Tất cả / Trả hàng-Hoàn tiền / Đơn hủy / Giao không thành công) | ✅ | |
| Lấy đơn hoàn mới (đồng bộ thủ công từ sàn) | ✅ | theo khoảng thời gian + gian hàng |
| Đồng bộ trạng thái đơn từ sàn | ✅ | tick đơn cần cập nhật rồi kéo trạng thái mới nhất |
| Đồng bộ lại thông tin đơn gốc (khách, địa chỉ, NV bán, tiền gốc) | ✅ | dùng cho đơn cũ thiếu thông tin |
| Nhập kho hàng hoàn | ✅ | chỉ với đơn "trả hàng & hoàn tiền"; ❌ với đơn "chỉ hoàn tiền" |
| Map sản phẩm hoàn với sản phẩm đơn gốc | ⚠️ | đa số tự khớp; khớp tay khi đơn cũ / đổi SKU |
| Tạo HBBTL (hóa đơn bán hàng trả lại) | ✅ | khi hàng đã về kho (đơn trả hàng) hoặc đơn chỉ hoàn tiền |
| Sửa số tiền / lý do / trạng thái hoàn | ❌ | dữ liệu lấy từ sàn; SCRM chỉ nhận và hiển thị lại |
| Khiếu nại / cung cấp bằng chứng / thương lượng đẩy ngược lên sàn | ⚠️ | đang phát triển — có chỗ nhưng chưa hoàn chỉnh end-to-end |

## Để làm gì / cho ai

**Cho ai:** nhân viên kho, nhân viên vận hành đơn (operations), kế toán bán hàng của shop
bán trên sàn TMĐT.

**Để làm gì:**
- Nhìn một chỗ tất cả đơn khách trả lại từ mọi sàn, không sót đơn nào.
- Biết đơn nào **cần xử lý gấp** (sắp hết hạn khiếu nại, sàn đang chờ shop phản hồi).
- Theo dõi **hàng hoàn đang ở đâu** (khách chưa gửi / đang vận chuyển về / đã về kho).
- Khi hàng về thì **nhập kho hoàn** và lập **Hóa đơn bán hàng trả lại (HBBTL)** để kế toán
  đảo lại doanh thu đúng số.
- Phân loại đơn theo nghiệp vụ: trả hàng-hoàn tiền, đơn hủy, đơn giao không thành công (khách
  từ chối nhận tại cửa).

## Cách dùng

1. **Mở màn hình:** menu Kho → **Đơn hàng hoàn**.
2. **Lọc / tìm:** chọn khoảng ngày, sàn TMĐT, gian hàng, trạng thái, lý do hoàn, hoặc gõ
   mã đơn hoàn để tìm nhanh.
3. **Chọn tab** để gom đơn theo nghiệp vụ:
   - *Tất cả* — toàn bộ đơn hoàn.
   - *Trả hàng / Hoàn tiền* — đơn khách yêu cầu trả hàng hoặc đòi hoàn.
   - *Đơn hủy* — đơn bị hủy.
   - *Giao hàng không thành công* — khách từ chối nhận hàng ngay tại cửa.
4. **Lọc theo việc cần làm** ("Hành động quan trọng"): Cung cấp bằng chứng, Thương lượng với
   khách, Kiểm tra hàng hoàn, Giữ lại kiện hàng, Phản hồi quyết định hoàn tiền — kèm số đếm
   để biết còn bao nhiêu đơn đang chờ.
5. **Lấy đơn hoàn mới** (nút 1): đồng bộ thủ công các đơn hoàn còn thiếu từ sàn theo khoảng
   thời gian + gian hàng (phòng khi đơn chưa tự về).
6. **Đồng bộ trạng thái** (nút 2): tick các đơn cần cập nhật → kéo trạng thái mới nhất từ sàn.
7. **Xem chi tiết:** bấm vào đơn để xem sản phẩm hoàn, số tiền, lý do, lịch sử vận chuyển hoàn.
8. **Tạo HBBTL:** khi đủ điều kiện (hàng đã về kho với đơn trả hàng, hoặc đơn chỉ hoàn tiền),
   bấm tạo Hóa đơn bán hàng trả lại — hệ thống tự điền sẵn thông tin từ đơn bán gốc.

## Cần chú ý

- **Đơn hoàn không tự sinh tiền hoàn của bạn** — số tiền, lý do, trạng thái đều **lấy từ sàn**.
  SCRM chỉ nhận và hiển thị lại, giúp shop xử lý nhập kho + kế toán.
- **"Chỉ hoàn tiền" thì không nhập kho**: khách giữ hàng nên không có bước nhận hàng về kho.
- **Đơn cũ (trước khi bật tính năng) có thể thiếu thông tin** — dùng "Đồng bộ thông tin đơn gốc"
  để lấy lại khách hàng, địa chỉ, NV bán, số hóa đơn, tiền gốc từng dòng.
- **Sản phẩm hoàn cần khớp đúng với sản phẩm đơn bán gốc.** Đa số tự khớp; nếu không khớp được
  (đơn cũ, đổi mã SKU) thì khớp tay qua chức năng "Map sản phẩm" trước khi lập HBBTL.
- **Hạn khiếu nại**: với đơn sàn đang chờ shop xử lý, có đếm ngược "Còn N ngày" — quá hạn shop
  mất quyền khiếu nại, nên ưu tiên các đơn sắp hết hạn.
- Một số thao tác nâng cao (khiếu nại, cung cấp bằng chứng, thương lượng đẩy ngược lên sàn)
  hiện **đang phát triển** — màn hình đã có chỗ nhưng chưa hoàn chỉnh end-to-end.

## Ảnh hưởng tới

- **Tồn kho**: khi nhập kho hàng hoàn (đơn trả hàng), hàng quay lại tồn kho.
- **Doanh thu / kế toán**: HBBTL từ đơn hoàn làm **đảo (giảm) doanh thu** đã ghi nhận của đơn gốc.
- **Đơn bán gốc**: đơn hoàn liên kết và snapshot thông tin từ đơn bán gốc (DonHangLogistic).
- **Kết nối sàn TMĐT** (ecommerce_connect): luồng dữ liệu đơn hoàn phụ thuộc hoàn toàn vào
  webhook + API đồng bộ từ Shopee/TikTok/Lazada qua hệ thống kết nối sàn.
