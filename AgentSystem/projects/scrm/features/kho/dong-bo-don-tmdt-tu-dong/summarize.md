---
alias: ["đơn sàn tự về", "đồng bộ đơn Shopee", "đồng bộ đơn TikTok", "đơn TMĐT tự động", "nhận đơn từ sàn", "đơn sàn về SCRM", "tự động lấy đơn shopee", "đồng bộ trạng thái giao", "đơn hoàn từ sàn"]
module: kho
trang_thai: cần bật (phải kết nối gian hàng sàn ở Marketing → Kết nối Shopee/TikTok trước)
goi: cần product xác nhận
quyen: "Webhook nhận đơn chạy nền (không cần quyền user). Các nút thao tác tay (lấy đơn, in đơn, đăng đơn) trên màn Đơn hàng TMĐT chỉ cần đăng nhập có tiện ích Kho vận (TI_OrderLogistic) — không phân quyền riêng từng nút. cần product xác nhận gói/role chi tiết."
phu_thuoc: ["Kết nối gian hàng sàn (Marketing → Kết nối Shopee / Kết nối TikTok — lưu token + mapping kho/đơn vị)", "Mapping kho & sản phẩm sàn ↔ SCRM (để trừ tồn đúng kho)", "Đơn hàng TMĐT / Đơn hàng logistic (nơi đơn hiện ra sau khi về)", "Đơn hàng hoàn (đơn trả lại tự về cùng cơ chế)", "Hệ thống ecommerce_connect (cầu nối SCRM ↔ sàn — chạy riêng)"]
link_flow: ./flow.md
---

# Tự động nhận & đồng bộ đơn từ sàn TMĐT

## Là gì
Đây là cơ chế **đơn hàng từ Shopee / TikTok (và Lazada) tự động chảy về SCRM** mà nhân viên không phải gõ tay. Khi có khách đặt đơn trên sàn, sàn báo cho hệ thống kết nối (ecommerce_connect), hệ thống này dựng đơn rồi **đẩy thẳng vào SCRM** để đơn xuất hiện trong màn Đơn hàng TMĐT/Đơn hàng logistic. Sau đó mỗi khi đơn đổi trạng thái trên sàn (đã thanh toán, đang giao, giao xong, hủy…) hoặc khách trả hàng/hoàn tiền, thông tin cũng **tự cập nhật** về theo.

Nói ngắn: đơn sàn về SCRM gồm 3 nhịp tự động — **tạo đơn mới**, **cập nhật trạng thái/phí giao dịch/hóa đơn**, và **nhận đơn hoàn (trả hàng)** — cộng thêm vài nút bấm tay để "kéo bù" những đơn bị sót.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Đơn mới từ sàn tự tạo trong SCRM | ✅ | tự động (webhook), cần đã kết nối gian hàng |
| Trạng thái đơn tự cập nhật (thanh toán/giao/hủy) | ✅ | tự động khi sàn báo đổi trạng thái |
| Phí giao dịch / hóa đơn sàn tự cập nhật | ✅ | tự động (DataType cập phí / cập hóa đơn) |
| Đơn hoàn / trả hàng tự về | ✅ | tự động — vào màn Đơn hàng hoàn |
| Kéo tay đơn bị sót ("Lấy đơn từ sàn") | ✅ | trên màn Đơn hàng TMĐT, cần login |
| Kéo tay đơn hoàn sót ("Lấy đơn hoàn mới") | ✅ | trên màn Đơn hàng hoàn |
| Đồng bộ lại trạng thái đơn hoàn (nút tay) | ✅ | tick chọn đơn rồi bấm |
| In vận đơn / Đăng đơn lên sàn | ✅ | chỉ Shopee/TikTok; Lazada & sàn khác chưa hỗ trợ in/đăng |
| Xem & xử lý "Đơn lỗi đồng bộ" (tạo lại / xóa) | ✅ | tab Đơn lỗi trên màn Đơn hàng TMĐT |
| Đơn về có **trùng** (cùng mã đơn sàn) | ⚠️ | hệ thống tự bỏ qua, không tạo 2 đơn |
| Đơn hoàn tự cộng/trừ tồn & công nợ | ❌ | đơn hoàn chỉ là dữ liệu; tồn & công nợ chỉ thay đổi khi nhân viên tạo phiếu Hàng bán bị trả lại (HBBTL) |
| In/đăng đơn Lazada | ❌ | code chỉ map Shopee/TikTok cho in & đăng đơn |

## Để làm gì / cho ai
- **Chủ shop / quản lý bán hàng đa kênh:** không phải copy đơn từ Shopee/TikTok sang phần mềm — đơn tự về, gộp chung một chỗ để xử lý, trừ kho, đối soát.
- **Nhân viên kho / vận đơn:** mở màn Đơn hàng TMĐT là thấy đơn mới, in vận đơn, đẩy đơn cho hãng giao.
- **Nhân viên CSKH / kế toán:** theo dõi trạng thái giao, đơn hoàn, phí sàn, hóa đơn — tất cả tự cập nhật.

## Cách dùng
**Chuẩn bị (làm 1 lần):**
1. Vào **Marketing → Kết nối Shopee / Kết nối TikTok**, đăng nhập gian hàng để lấy quyền (token).
2. Chọn **đơn vị / chi nhánh** và **kho** sẽ nhận đơn, mapping sản phẩm sàn ↔ sản phẩm SCRM.

**Hằng ngày (gần như không phải làm gì):**
1. Khách đặt trên sàn → vài giây đến vài phút sau đơn **tự hiện** trong **Đơn hàng TMĐT** (hoặc Đơn hàng logistic).
2. Trạng thái đơn (đã thanh toán, đang giao, giao xong, hủy) **tự đổi** theo sàn.
3. Khách trả hàng → đơn hoàn **tự về** màn **Đơn hàng hoàn**.

**Khi đơn về thiếu / bị sót:**
1. Mở **Đơn hàng TMĐT**, bấm **"Lấy đơn từ sàn"**, chọn gian hàng + khoảng thời gian để kéo bù.
2. Với đơn hoàn: mở **Đơn hàng hoàn**, bấm **"Lấy đơn hoàn mới"** hoặc **"Đồng bộ trạng thái"**.
3. Đơn nào tạo lỗi → vào tab **"Đơn lỗi đồng bộ"** để **tạo lại** hoặc **xóa**.

## Cần chú ý
- **Đơn không tự về thường là do chưa kết nối / token gian hàng hết hạn**, chứ không phải mất đơn. Kiểm tra lại kết nối Shopee/TikTok trước.
- **Có độ trễ.** Đơn về theo thông báo của sàn; lúc cao điểm hoặc sàn báo chậm thì đơn về trễ vài phút. Sốt ruột thì bấm "Lấy đơn từ sàn" để kéo tay.
- **Không bao giờ bị nhân đôi đơn:** nếu cùng một mã đơn sàn về 2 lần, hệ thống nhận ra và **giữ nguyên đơn cũ**, không tạo đơn mới.
- **Đơn hoàn về chỉ là thông tin** — kho và công nợ **chưa** đổi. Phải tạo phiếu **Hàng bán bị trả lại (HBBTL)** khi hàng thực về kho thì tồn/công nợ mới cập nhật.
- **In vận đơn & đăng đơn chỉ chạy với Shopee/TikTok.** Lazada hiện chỉ nhận đơn về, chưa in/đăng được qua hệ thống.
- **Mỗi gian hàng phải gắn đúng kho/đơn vị.** Mapping sai thì đơn về nhầm chi nhánh hoặc trừ nhầm kho.

## Ảnh hưởng tới
- **Đơn hàng TMĐT / Đơn hàng logistic** — nơi đơn hiện ra, in, đăng, xử lý giao vận.
- **Tồn kho** — đơn mới tạo sẽ trừ/giữ tồn ở kho được mapping.
- **Kế toán / hóa đơn** — phí giao dịch sàn, hóa đơn sàn cập nhật theo đơn.
- **Đơn hàng hoàn** — đơn trả hàng dùng chung cơ chế đồng bộ này.
- **Báo cáo theo nhãn / phí sàn** — dữ liệu doanh số & phí sàn lấy từ các đơn đã đồng bộ về.
