---
alias: ["rà soát tồn kho", "reset tồn kho", "reset toàn kho", "tính lại giá vốn", "tái tạo phiếu", "đối soát tồn kho", "tồn kho bị lệch", "giá vốn bị sai"]
module: kho
trang_thai: đang dùng
goi: cần product xác nhận
quyen: Bắt đầu/Xem — Admin tổng/CN/MCN, Quản lý dịch vụ, Quản kho (các cấp); Reset & Tái tạo toàn kho — CHỈ Admin tổng; Xóa phiếu sai — Admin tổng/MCN/Quản lý dịch vụ — chi tiết cần product xác nhận
phu_thuoc: [Phiếu nhập/xuất/chuyển kho/kiểm kho (nguồn ledger để dựng lại), Quản lý tồn kho, Giá vốn theo kỳ, Kế toán (đồng bộ chứng từ), Lô date & Imei]
link_flow: ./flow.md
---

# Rà soát / Reset tồn kho & giá vốn

## Là gì
Công cụ **kiểm tra và tái tạo lại số tồn kho + giá vốn của cả đơn vị** khi số liệu bị lệch (do lỗi thao tác, phiếu sai, hoặc dữ liệu cũ). Khác với nút "Reset tồn kho & Imei" (chỉ đưa **1 sản phẩm trong 1 kho** về 0), công cụ này làm việc ở **quy mô cả đơn vị**: dò lệch theo đơn, quét toàn kho, hoặc **dựng lại toàn bộ phiếu và tính lại tồn + giá vốn** từ đầu.

Chạy như 1 **tiến trình nền**: bạn bấm bắt đầu, hệ thống xử lý ngầm và **báo tiến trình realtime** trên màn hình. Việc nặng nên có thể mất thời gian; nếu gián đoạn, hệ thống **tự thử lại** và cho **bấm "Tiếp tục"** từ chỗ dừng.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Bắt đầu rà soát (dò lệch theo đơn) | ✅ | Admin tổng/CN/MCN, QLDV, Quản kho các cấp |
| Quét toàn kho | ✅ | như trên (chọn loại phiên Scan) |
| **Reset toàn kho** (ghi đè tồn = tính lại từ phiếu) | ⚠️ | **CHỈ Admin tổng** — nguy hiểm, nên chạy lúc kho ít hoạt động |
| **Tái tạo phiếu + reset** (dựng lại ledger) | ⚠️ | **CHỈ Admin tổng** — nặng nhất, dựng lại toàn bộ phiếu tự động |
| Tiếp tục phiên đang dở | ✅ | như quyền bắt đầu; resume từ chỗ dừng |
| Xem phiên gần nhất / kết quả / tiến trình | ✅ | Admin/QLDV/Quản kho |
| Xóa phiếu sai (đã dò ra) + hoàn kho | ✅ | Admin tổng / MCN / QLDV |
| Chọn hướng tính giá vốn (Anchor/Stream) | ⚠️ | Super user — **hiện đang khóa ở "Anchor"**, chọn Stream chưa có tác dụng |

## Để làm gì / cho ai
- **Cho quản kho / kế toán:** khi tồn kho hoặc giá vốn "không khớp thực tế" → chạy rà soát để tìm nguồn lệch, hoặc reset/tái tạo để đưa số về đúng.
- **Cho admin tổng:** công cụ "chữa cháy" khi dữ liệu kho của đơn vị sai hàng loạt (không nên dùng thường xuyên).

## Cách dùng (các bước thường gặp)
1. Vào công cụ **Rà soát tồn kho** (Quản lý kho).
2. Chọn loại: **Rà soát theo đơn** (dò lệch) hoặc **Quét toàn kho**; hoặc (admin tổng) **Reset toàn kho** / **Tái tạo phiếu**.
3. Bấm **Bắt đầu** → theo dõi thanh tiến trình realtime.
4. Xem **kết quả** (dòng nào lệch, phiếu nào sai). Có thể **chọn phiếu sai → Xóa + hoàn kho**.
5. Nếu tiến trình bị dừng/lỗi → bấm **Tiếp tục** để chạy tiếp từ chỗ dừng.

## Cần chú ý (cạm bẫy)
- **Reset / Tái tạo toàn kho là thao tác nặng và phá dữ liệu ledger** — chỉ Admin tổng, **chạy lúc kho ít hoạt động**. Reset ghi đè tuyệt đối → nếu kho đang giao dịch, thay đổi đồng thời có thể bị **nuốt**.
- **Sau Reset/Tái tạo, giá vốn theo kỳ cả đơn vị bị đánh dấu "cần tính lại"** — không phải lỗi, là cảnh báo để kế toán tính lại giá vốn.
- **Tồn có thể âm** (nếu kho cho chốt âm): giá vốn khi tính bình quân **coi tồn âm như 0**, nhưng số tồn lưu vẫn giữ âm để không sai lần sau.
- **Hướng giá vốn hiện khóa ở "Anchor"** — dù màn có radio chọn Stream, hệ thống vẫn chạy Anchor.
- **Không auto-chạy lại vô hạn:** hệ thống tự thử lại tối đa 3 lần; hết → báo lỗi, giữ điểm dừng để bạn bấm **Tiếp tục** thủ công (kèm kiểm tra dữ liệu tạm).
- **Mỗi đơn vị chỉ 1 job chạy 1 lúc** (khóa Redis) — bấm khi đang chạy sẽ không tạo job trùng.

## Ảnh hưởng tới
- **Quản lý tồn kho** — số tồn sau reset/tái tạo là con số mới.
- **Giá vốn theo kỳ / Kế toán** — bị đánh dấu tính lại; phiếu dựng/reset được đẩy lại sang kế toán.
- **Báo cáo kho** — dữ liệu lịch sử kho (KhoChiTietLichSu) được resync.
- **Lô date & Imei** — serial/lô được re-point theo phiếu mới.

## Link
→ [flow.md](./flow.md) · Module: [../README.md](../README.md)
