# Luồng nghiệp vụ — Đơn hàng hoàn

## Tiền điều kiện

- Shop đã **kết nối gian hàng sàn TMĐT** (Shopee/TikTok/Lazada) qua module kết nối sàn
  (UnitConnect). Luồng dữ liệu đơn hoàn đi qua hệ thống `ecommerce_connect`.
- Tồn tại **đơn bán gốc** (DonHangLogistic) khớp với đơn hoàn (ECOMM resolve sẵn `CrmOrderId`).
  Nếu không tìm thấy đơn gốc → SCRM từ chối tạo đơn hoàn (báo "Không tìm thấy đơn hàng gốc").
- Có quyền truy cập màn hình Đơn hàng hoàn (module Kho / OrderLogistic).

## Luồng dùng (nhận đơn hoàn → xử lý → nhập kho hoàn)

### A. Nhận đơn hoàn (tự động)

1. Khách bấm "Trả hàng / Hoàn tiền" trên sàn → sàn bắn webhook về `ecommerce_connect`.
2. `ecommerce_connect` chuẩn hóa dữ liệu (map raw status sàn → code CRM, gắn `CrmOrderId`,
   `CrmSkuId`, `CrmProductId`) rồi gọi backend-to-backend sang SCRM:
   `DonHangHoanService.NhanDuLieuDonHangHoanTMDT`.
3. SCRM route theo `DataType`:
   - **CREATE** → tạo `DonHangHoan` mới + các dòng `DonHangHoanSanPham`, snapshot thông tin
     đơn bán gốc (khách, địa chỉ, NV bán, mã PBH, tiền từng dòng).
   - **UPDATE** → chỉ cập nhật trạng thái (status, logistics, deadline...).
   - **UPDATE_DETAIL** → cập nhật trạng thái + thay danh sách sản phẩm hoàn.
4. CREATE lặp lại (webhook trùng) → **idempotent**: trả về ref đơn đã có, không tạo trùng.

### B. Nhận đơn hoàn (thủ công — khi tự động sót)

1. User bấm **"Lấy đơn hoàn mới"** → chọn sàn + gian hàng + khoảng thời gian → SCRM proxy sang
   ECOMM kéo các đơn hoàn chưa tồn tại về.
2. Hoặc tick đơn đã có → **"Đồng bộ trạng thái"** → kéo trạng thái mới nhất từ sàn.

### C. Theo dõi & phân loại

1. User mở màn hình, lọc theo ngày/sàn/gian hàng/trạng thái/lý do.
2. Chọn **tab** (Tất cả / Trả hàng-Hoàn tiền / Hủy / Giao không thành công) và **sub-tab**
   (Sàn đang xem xét / Đang trả hàng / Đã hoàn tiền / Đã khiếu nại / Yêu cầu bị hủy).
3. Lọc theo **Hành động quan trọng** (cần cung cấp bằng chứng, kiểm tra hàng hoàn...) — kèm
   số đếm để biết còn bao nhiêu đơn chờ.
4. Ưu tiên đơn theo **deadline khiếu nại** (còn ≤1 ngày / ≤2 ngày).

### D. Nhập kho hoàn + tạo HBBTL

1. Với đơn **trả hàng**: theo dõi vận chuyển hoàn (badge trạng thái: Chờ gửi / Đang về /
   Đã nhận / Giao fail / Mất hàng).
2. Khi **hàng đã về kho** (logistics = DELIVERY_DONE theo nơi kiểm hàng) → đủ điều kiện lập HBBTL.
3. User bấm **"Tạo HBBTL"** → SCRM pre-fill form Hóa đơn bán hàng trả lại từ đơn hoàn + đơn gốc
   (khách, sản phẩm, giá vốn, IMEI/lô, số tiền). HBBTL có cờ `IsKiemNhapKho` = true khi trả hàng
   (cần nhập kho), false khi chỉ hoàn tiền.
4. User duyệt/lưu HBBTL → kế toán đảo doanh thu + hàng (nếu trả hàng) nhập lại tồn kho.

## Case biên

- **Không tìm thấy đơn bán gốc** → từ chối tạo đơn hoàn (báo lỗi, ECOMM giữ để retry/log).
- **Webhook trùng (CREATE lại)** → idempotent, trả ref đơn cũ.
- **Chỉ hoàn tiền (REFUND / REFUND_ONLY)** → bỏ qua bước nhập kho; HBBTL chỉ giảm doanh thu.
- **Loại đơn hoàn = Trước hoàn thành đơn (RRBOC)** → đơn gốc chưa ghi doanh thu → **không tạo HBBTL**.
- **Nơi kiểm hàng = Kho trung gian (warehouse validation, FBS)** → hàng "về kho sàn" (REVERSE done)
  CHƯA tính là về seller; phải đợi chặng 2 POST_RETURN DELIVERY_DONE mới đủ điều kiện HBBTL.
- **Hàng không thể trả vật lý** (digital goods, `CanHoanHangVatLy=false`) → ẩn nút nhận/kiểm hàng hoàn.
- **Sản phẩm hoàn không auto-map được dòng đơn gốc** (đơn cũ / đổi SKU) → cần map tay; HBBTL fallback
  fuzzy match theo IdSanPham.
- **Combo** → 1 dòng hoàn combo được "nổ" thành N dòng SP con (mirror cách Phiếu bán hàng xử lý combo).
- **Lệch tiền hoàn vs đơn gốc** → chỉ hiển thị badge cảnh báo, chưa chặn nghiệp vụ.
- **Giao về shop thất bại / mất hàng** (GIAO_FAIL / MAT_HANG) → KHÔNG đủ điều kiện HBBTL, cần khiếu nại sàn.
- **Đơn giao không thành công (return-on-the-spot, LoaiYeuCauHoan=2)** → luồng auto-refund, không qua
  bước khách gửi hàng về → tab riêng, sub-tab status không áp dụng.

## Ví dụ có số

**Bối cảnh:** Đơn bán gốc trên Shopee: 2 sản phẩm.
- SP A: số lượng 3, đơn giá gốc 100.000, net (sau giảm sàn) 90.000/cái.
- Khách trả 1 cái SP A. Sàn báo tiền hoàn cho dòng = 90.000.

**Tính toán khi SCRM nhận đơn hoàn (BuildSanPham):**
- `ratio` = SL hoàn / SL gốc = 1 / 3 ≈ 0,333.
- `ThanhTienPhanBoGoc` (net dòng gốc) = 270.000 (3 × 90.000).
- Phần net phân bổ cho 1 cái hoàn = 270.000 × 0,333 = 90.000.
- `ChenhLechTienDonGoc` = tiền hoàn − net×ratio = 90.000 − 90.000 = **0** → không cảnh báo lệch.
- `TienChietKhauSan` (sàn trợ giá phần hoàn) = (gross − net) × ratio
  = (100.000 − 90.000) × 1 cái = **10.000**.

**Khi hàng về kho** (REVERSE LOGISTICS_DELIVERY_DONE, NoiKiemHoan = NguoiBan):
- `IsHangVeKho` = true → nút "Tạo HBBTL" sáng.
- HBBTL pre-fill: 1 × SP A, tiền hoàn 90.000, giá vốn lấy từ dòng đơn bán gốc, `IsKiemNhapKho=true`.

## Hậu điều kiện

- `DonHangHoan` tồn tại với trạng thái + logistics + tiền + lý do đồng bộ từ sàn.
- Khi lập HBBTL: `DonHangHoan.IdPhieuBanTraLai` + `MaPhieuHBBTL` được set (link 1:1, chống tạo trùng).
- Hàng trả (đơn trả hàng) nhập lại tồn kho qua HBBTL; doanh thu đơn gốc bị đảo (giảm) đúng số.
- Đơn hoàn vẫn tra cứu được theo đơn bán gốc (liên kết `IdDonHangLogistic` + snapshot mã đơn gốc).
