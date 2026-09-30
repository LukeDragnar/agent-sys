---
alias: ["nghiệp vụ tự động", "tự cộng trừ khi giao hàng", "cập nhật chi tiêu khách khi đơn giao", "hàng đợi nghiệp vụ logistics", "queue nghiệp vụ", "đơn pending nghiệp vụ", "thử lại nghiệp vụ lỗi"]
module: kho
trang_thai: Core (chạy nền tự động — không có menu cho người dùng thường; có màn quản trị nội bộ HT-CSKH)
goi: cần product xác nhận
quyen: "Tự chạy nền — không cần quyền. Màn quản trị/thử lại: chỉ nội bộ HT-CSKH (key NghiepVuManagement_*)."
phu_thuoc: ["Đơn hàng logistics (DonHangLogistic — trạng thái giao vận)", "Khách hàng (Customer — chi tiêu & công nợ Nợ/Trả)", "Cấu hình nghiệp vụ (DonHangLogisticNghiepVuCauHinh — trạng thái Tăng/Giảm, delay)", "RabbitMQ + Quartz job (hàng đợi xử lý)"]
link_flow: ./flow.md
---

# Nghiệp vụ tự động theo trạng thái giao hàng

## Là gì
Đây là **bộ máy chạy nền** (không có menu bấm tay): mỗi khi một đơn giao hàng (đơn logistics) **đổi trạng thái giao vận** — ví dụ "Đã giao", "Đã thanh toán", "Giao hàng một phần", "Hủy", "Hoàn" — hệ thống **tự động cập nhật lại con số chi tiêu của khách hàng và công nợ liên quan** mà không cần ai bấm nút.

Để làm việc này một cách **an toàn khi nhiều đơn đổi trạng thái cùng lúc** (và khi một đơn đổi qua lại nhiều trạng thái), hệ thống không xử lý ngay tại chỗ mà **đẩy yêu cầu vào một hàng đợi (queue)** rồi xử lý lần lượt. Nhờ vậy con số luôn đúng kể cả khi đơn bị đổi trạng thái ngược (giao rồi lại hoàn), bị trùng webhook, hay hệ thống bận.

> Lưu ý phạm vi (theo code hiện tại): nghiệp vụ tự động **đang chạy** là **CHI_TIEU** — cập nhật **chi tiêu khách + công nợ Nợ/Trả** theo trạng thái giao. Các nghiệp vụ kiểu "tự trừ tồn kho" / "tự tạo hóa đơn" mới chỉ là **khung để mở rộng** (đã chừa chỗ trong code) **nhưng chưa được bật**. `cần product xác nhận` nếu khách hỏi về tồn kho/hóa đơn tự động qua kênh này.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tự cập nhật chi tiêu khách khi đơn đổi trạng thái | ✅ | chạy nền, không cần thao tác; theo cấu hình trạng thái Tăng/Giảm của đơn vị |
| Tự cập nhật công nợ (Nợ/Trả) theo trạng thái giao | ✅ | chạy nền; gắn liền với chi tiêu (cùng nghiệp vụ CHI_TIEU) |
| Tự trừ/cộng **tồn kho** qua hàng đợi này | ❌ | khung có sẵn nhưng **chưa bật** (chỉ là placeholder TRU_KHO) — `cần product xác nhận` |
| Tự **tạo hóa đơn** qua hàng đợi này | ❌ | khung có sẵn nhưng **chưa bật** (placeholder TAO_HOA_DON) — `cần product xác nhận` |
| Hoãn xử lý sau N phút (delay) | ✅ | đặt trong cấu hình (vd chờ 6 giờ rồi mới cộng, tránh giao xong lại hoàn) |
| Cấu hình trạng thái nào = Tăng, còn lại = Giảm | ✅ | qua bảng cấu hình nghiệp vụ của đơn vị (mặc định: 31, 32 = Tăng) |
| Theo dõi đơn Pending / Thành công / Thất bại | ✅ | màn quản trị nội bộ HT-CSKH |
| Thử lại thủ công nghiệp vụ bị lỗi | ✅ | màn quản trị nội bộ HT-CSKH (nút "Thử lại") |
| Tự thử lại khi lỗi tạm thời | ✅ | tự retry tối đa 3 lần (delay 5 phút), sau đó vào lịch sử "Lỗi" + có job quét retry |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** không phải canh chừng — khi đơn được giao xong, **chi tiêu của khách và công nợ tự khớp**; nếu đơn bị hủy/hoàn thì con số tự **bị trừ ngược lại**. Dữ liệu báo cáo doanh số/chăm sóc khách luôn phản ánh đúng tình trạng giao hàng thực tế.
- **Khách hàng cuối:** điểm/chi tiêu tích lũy của họ chỉ tính khi đơn **thực sự giao thành công**, không bị tính oan khi đơn còn đang giao hoặc bị hoàn.
- **Nhân viên CSKH / vận hành (HT-CSKH):** có màn riêng để soi đơn nào đang treo (Pending), đơn nào xử lý lỗi, và **bấm thử lại** khi cần.

## Cách dùng
**Với người dùng thường: không cần làm gì.** Cứ cập nhật trạng thái giao hàng cho đơn (thủ kho thao tác, hoặc đối tác vận chuyển bắn webhook về) → hệ thống tự lo phần cộng/trừ phía sau.

**Với quản trị viên cấu hình (nếu cần đổi quy tắc):**
1. Đặt **trạng thái nào được tính là "Tăng"** (mặc định: *Đã giao hàng* và *Đã thanh toán*). Mọi trạng thái khác mặc định là "Giảm" (trừ ngược về 0).
2. Đặt **độ trễ (delay)** trước khi xử lý — ví dụ chờ 6 giờ sau khi giao mới cộng, để nếu khách trả hàng ngay thì khỏi cộng nhầm.
3. Nếu muốn **tắt** nghiệp vụ cho đơn vị: đặt delay = -1.

**Với CSKH khi có sự cố:**
1. Vào màn quản trị nghiệp vụ Order Logistic.
2. Tìm đơn theo Mã đơn, xem số Pending / Thành công / Thất bại.
3. Nếu có bản ghi lỗi → bấm **Thử lại**.

## Cần chú ý
- **Con số tự đổi là đúng thiết kế, không phải lỗi.** Đơn giao → chi tiêu khách tăng; đơn hủy/hoàn → chi tiêu giảm về 0. Nếu thấy chi tiêu khách "tự nhiên đổi", hãy kiểm tra trạng thái giao của đơn liên quan.
- **Có độ trễ.** Vì có delay cấu hình (mặc định thường để vài chục phút đến vài giờ) nên con số **không cập nhật tức thì** — bình thường.
- **Giao hàng một phần (trạng thái 35):** chỉ tính phần đã giao = tiền COD một phần + cọc, không tính toàn bộ tiền đơn.
- **Đổi trạng thái qua lại nhiều lần:** hệ thống xử lý **tuần tự theo đúng thứ tự thời gian** đổi trạng thái, nên kết quả cuối cùng luôn khớp trạng thái mới nhất của đơn.
- **Thử lại đơn đã Thành công:** không cho (chặn) — chỉ thử lại được bản ghi Lỗi hoặc Bỏ qua.
- Phạm vi hiện tại **chỉ là chi tiêu + công nợ khách**. Tồn kho và hóa đơn **không** đi qua kênh này (xem ghi chú phần "Là gì").

## Ảnh hưởng tới
- **Hồ sơ khách hàng 360 / chi tiêu / điểm tích lũy** — con số chi tiêu khách lấy từ kết quả nghiệp vụ này.
- **Công nợ khách (Nợ/Trả)** — bảng ghi nợ bên Customer được cập nhật theo cùng luồng.
- **Báo cáo doanh số / chăm sóc khách** — phản ánh đúng đơn đã giao thật.
- **Màn "Lịch sử nghiệp vụ logistics" (HT-CSKH)** — nơi tra cứu kết quả và thử lại.
