---
alias: ["thêm sản phẩm", "tạo combo", "quản lý hàng hóa", "khai báo dịch vụ", "tạo thẻ dịch vụ", "danh mục sản phẩm"]
module: kho
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: cần product xác nhận (BE chặn theo nhóm quyền DV-AD-TONG để thêm/sửa/xóa; xem được theo nhiều nhóm quyền kho/kế toán/dịch vụ)
phu_thuoc: [Kho & tồn kho (xuất/nhập), Đơn hàng (bán SP/DV/combo/thẻ), Khuyến mãi, Chi nhánh (giá theo chi nhánh), Website/Ecommerce (đồng bộ SP)]
link_flow: ./flow.md
---

# Quản lý sản phẩm

## Là gì
Nơi **khai báo & quản lý toàn bộ danh mục bán** của cửa hàng/spa: **Sản phẩm** (hàng hóa), **Dịch vụ** (làm tại spa/salon), **Combo** (gói nhiều DV/SP bán kèm giá ưu đãi), **Thẻ dịch vụ** (thẻ nạp tiền / thẻ số lần / thẻ lịch), cùng **Danh mục sản phẩm** (nhóm để phân loại) và **Nhãn sản phẩm** (gắn tag màu cho dễ nhận biết).

Đây là dữ liệu gốc — mọi thứ bán ra (đơn quầy, đơn online, đặt lịch) đều **lấy từ đây**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm SP / Dịch vụ / Combo / Thẻ | ✅ | BE chặn theo nhóm quyền DV-AD-TONG; bắt buộc chọn nhóm + chi nhánh; Tên/Mã/Barcode không trùng |
| Sửa SP / DV / Combo / Thẻ | ✅ | DV-AD-TONG; SP có tồn/đã bán không đổi được đơn vị tính |
| Xóa SP / DV | ⚠️ | xóa thật, không khôi phục; chặn nếu SP đang nằm trong combo / DV / SP cấu thành |
| Xóa Combo | ⚠️ | không xóa được nếu combo đã bán cho khách |
| Bật / tắt "Sử dụng" (ngừng bán tạm) | ✅ | thay cho xóa khi chỉ muốn ẩn |
| Thêm / Sửa / Xóa Danh mục (nhóm SP) | ✅ | tạo nhóm trước vì thêm SP bắt buộc chọn nhóm |
| Thêm / Sửa / Xóa Nhãn sản phẩm | ✅ | theo phân quyền nhãn SP |
| Đổi đơn vị tính của SP | ❌ | khi SP đã có tồn kho hoặc đã bán trong đơn |

## Để làm gì / cho ai
- **Cho chủ shop / quản lý kho:** dựng danh mục hàng hóa & dịch vụ, đặt giá bán, giá vốn, VAT, ảnh, mô tả.
- **Cho spa/salon:** khai báo dịch vụ kèm **thời gian phục vụ (phút)**, tính tiền theo giờ / theo block, giá theo khung giờ.
- **Cho marketing/bán hàng:** đóng gói **combo** ưu đãi, phát hành **thẻ** trả trước để giữ chân khách.
- **Cho kế toán:** giá vốn, VAT đúng để hạch toán.

## Cách dùng
1. Vào menu **Kho** → chọn màn tương ứng: Quản lý sản phẩm / Quản lý dịch vụ / Quản lý Combo / Quản lý thẻ / Danh mục sản phẩm / Nhãn sản phẩm.
2. Trước khi thêm sản phẩm/dịch vụ: tạo **Danh mục (nhóm)** trước, vì khi thêm SP/DV **bắt buộc chọn nhóm**.
3. Bấm **Thêm mới** → nhập tên, ảnh, giá, **chọn khu vực kho hàng (chi nhánh)** áp dụng → Lưu.
4. Với combo: chọn các SP/DV thành phần (phải thuộc chi nhánh đó) → đặt giá gói → Lưu.
5. Với thẻ: chọn **loại thẻ** (số tiền / số lần / lịch), nhập giá bán, thời hạn → Lưu.

## Cần chú ý (cạm bẫy)
- **Bắt buộc chọn nhóm + chọn chi nhánh (khu vực kho hàng)**, nếu không sẽ không lưu được.
- **Tên / Mã / Barcode không được trùng** trong cùng đơn vị — hệ thống chặn và báo lỗi.
- Sản phẩm là **IMEI hoặc Lô-date**, **không được chọn cả hai**.
- **Đã có tồn kho hoặc đã bán trong đơn → KHÔNG đổi được đơn vị tính.** Cần khai báo đơn vị/quy đổi đúng ngay từ đầu.
- **Combo đã bán cho khách → không xóa được.** Sản phẩm đang nằm trong combo / dịch vụ / sản phẩm cấu thành cũng **không xóa được** (phải gỡ khỏi nơi dùng trước).
- **Xóa sản phẩm/dịch vụ là xóa thật (không khôi phục).**
- **Tắt "Sử dụng" thay vì xóa** nếu chỉ muốn ngừng bán tạm thời.
- Sửa giá/giá vốn của SP **đang có tồn kho** vẫn cho phép — cân nhắc vì ảnh hưởng tính giá vốn về sau.

## Ảnh hưởng tới
- **Kho & tồn kho** — SP khai báo ở đây mới có để nhập/xuất/kiểm kho.
- **Đơn hàng (quầy & online)** — danh sách SP/DV/combo/thẻ khách mua lấy từ đây; ẩn/tắt thì không bán được.
- **Đặt lịch (Booking)** — dịch vụ + thời gian phục vụ hiển thị khi đặt lịch.
- **Khuyến mãi** — áp giảm giá lên SP/combo.
- **Website / Sàn TMĐT (Shopee/TikTok)** — đồng bộ thông tin & giá SP.
- **Kế toán** — giá vốn, VAT.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · Module: [../README.md](../README.md) · Kỹ thuật (chỉ dev): [dev.md](./dev.md)
