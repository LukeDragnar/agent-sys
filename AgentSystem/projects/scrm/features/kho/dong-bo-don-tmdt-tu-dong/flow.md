# Luồng — Tự động nhận & đồng bộ đơn từ sàn TMĐT

## Tiền điều kiện
- Gian hàng sàn (Shopee/TikTok/Lazada) đã được **kết nối**: có token hợp lệ, lưu trong `UnitConnect` (ecommerce_connect) cùng `CrmUnitId` / `CrmUnitChildId` (đơn vị + chi nhánh CRM) và `CrmCallBackUrl` (URL webhook về SCRM).
- Mapping **kho** (`WarehouseConnect`) và **sản phẩm** (`ProductConnect`) sàn ↔ SCRM đã thiết lập để trừ tồn đúng kho.
- Hai hệ thống ecommerce_connect và SCRM (OrderLogistic) đang chạy, nằm cùng mạng nội bộ (webhook BE-to-BE không cần token — tin tưởng ở tầng mạng).
- Webhook đầu vào từ sàn được EcommerceReceive.API tiếp nhận (`/Shopee/PushMechanism/Receive`, `/Tiktok/PushNotification/Receive`).

## Luồng dùng (tự động — không thao tác tay)
**A. Đơn mới về (DataType CREATE):**
1. Khách đặt đơn trên sàn → sàn gọi webhook → **EcommerceReceive.API** nhận, publish `EcommerceConnect_DataReceivePartner`.
2. `DataEcomReceiveConsumer` route theo `EcommerceCode` → `IPushMechanismService` (Shopee) / `IPushNotificationService` (TikTok) dựng đơn từ raw data.
3. ecommerce_connect build payload `TaoDonHangEcommerceDto` với `DataType = CREATE` và POST sang **SCRM** `api/DonHang/NhanDuLieuDonHangTMDT`.
4. SCRM (`DonHangService.NhanDuLieuDonHangTMDT`) lấy `CrmUnitId`/`CrmUnitChildId` → kiểm tra **đơn vị tồn tại** → gọi `TaoDonHangEcommerce`.
5. `TaoDonHangEcommerce` kiểm tra **trùng theo mã đơn sàn** (`PRC_..._GetByMaDonEcomm`). Chưa có → validate → insert `DonHangLogistic` + line sản phẩm + combo + thuế → **ripple** kho / kế toán / ElasticSearch / notification. Đã có → trả lại đơn cũ, **không insert lại**.
6. Đơn hiện trong màn Đơn hàng TMĐT.

**B. Cập nhật trạng thái / phí / hóa đơn:**
- Sàn báo đổi trạng thái → ecommerce_connect POST cùng endpoint với `DataType = UPDATE` → `CapNhatDonHangEcommerce` (cập trạng thái đơn).
- Cập phí giao dịch → `DataType = UPDATEPAYMENT` → `CapNhatGiaoDichDonHangEcommerce`.
- Cập hóa đơn → `DataType = UPDATEINVOICE` → `CapNhatHoaDonDonHangEcommerce`.

**C. Đơn hoàn (return/refund):**
1. Sàn báo phát sinh/đổi đơn hoàn → ecommerce_connect (`OrderReturnShopeeService` / `OrderReturnTiktokService`) build payload, quyết `DataType`: chưa có trên CRM → **CREATE**; đổi trạng thái return → **UPDATE_DETAIL** (kèm items); chỉ đổi field phụ → **UPDATE** (minimal).
2. POST sang **SCRM** `api/DonHangHoanEcomm/NhanDuLieuDonHangHoanTMDT`.
3. SCRM (`DonHangHoanService.NhanDuLieuDonHangHoanTMDT`) route theo `DataType`: RETURN_CREATE → tạo `DonHangHoan` + `DonHangHoanSanPham`; RETURN_UPDATE → chỉ cập status (Items=null OK); RETURN_UPDATE_DETAIL → cập status + thay items.
4. Đơn hiện trong màn Đơn hàng hoàn (chưa đụng tồn/công nợ).

## Luồng cấu hình (admin / thao tác tay khi sót)
1. **Kết nối gian hàng:** Marketing → Kết nối Shopee/TikTok → OAuth → lưu token + chọn đơn vị/chi nhánh/kho.
2. **Kéo bù đơn bán:** màn Đơn hàng TMĐT → "Lấy đơn từ sàn" (`CRM_GetOrderFromEcomm`) chọn gian hàng + khoảng thời gian.
3. **Kéo bù đơn hoàn:** màn Đơn hàng hoàn → "Lấy đơn hoàn mới" (`LayDonHoanMoiTuEcomm`); "Đồng bộ trạng thái" (`DongBoTrangThaiDonHoan`).
4. **Đơn lỗi đồng bộ:** tab Đơn lỗi → xem (`CRM_GetOrderCreateError`) → tạo lại (`CRM_ReCreateOrderToCrm`) / xóa (`CRM_DeleteOrderSyncError`).

## Các trường hợp / case biên
- **Trùng đơn (cùng mã đơn sàn về 2 lần):** `TaoDonHangEcommerce` tra `PRC_..._GetByMaDonEcomm` thấy đã có → trả đơn cũ, **không tạo trùng**. Webhook trả Success nhưng không insert mới (idempotent).
- **Thiếu đơn vị/chi nhánh trong payload header:** với CREATE/UPDATE/UPDATEPAYMENT, code fallback lấy `CrmUnitId`/`CrmUnitChildId` từ chính khối dữ liệu (OrderDataDetail/OrderStatusData/OrderPaymentData). Nếu DataType lạ → trả **"Loại hành động không hợp lệ."**.
- **Đơn vị không tồn tại:** `DonVi_GetDonViById` null → trả **"Đơn vị không tồn tại."**, không tạo đơn.
- **Tạo đơn lỗi (mapping SP/kho thiếu):** đơn rơi vào danh sách "Đơn lỗi đồng bộ" để tạo lại — không mất dữ liệu.
- **Retry:** webhook là HTTP đồng bộ; ecommerce_connect lưu `OrderData.Status = CreateFail` khi lỗi, có nút tạo lại (`ReCreateOrderToCrm` / `SaveOrderToCrm`). Consumer có Redis dedup (`KeyCache_CancelLogProcessById`) tránh xử lý lặp.
- **Đơn hoàn RETURN_UPDATE với Items = null:** hợp lệ — chỉ cập trạng thái, **đừng nhầm là thiếu data**.
- **Đơn hoàn trùng (CREATE retry):** SCRM tra theo IdDonVi + IdChiNhanh + `MaDonHoanEcomm`; đã tồn tại → trả ref đơn hoàn cũ (Success) để ecommerce_connect dừng retry, **không tạo trùng**. UPDATE/UPDATE_DETAIL mà chưa có đơn → trả "Đơn hoàn không tồn tại để UPDATE…". CREATE thiếu `ProductReturnInfo` → "CREATE cần ProductReturnInfo.".
- **Đổi trạng thái ngược / trạng thái đã mới nhất:** `CapNhatDonHangEcommerce` có nhánh trả **"Đơn hàng đã ở trạng thái mới nhất."** (bỏ qua cập lùi).
- **Sàn không hỗ trợ in/đăng (Lazada):** in đơn (`CRM_GetOrderPrintFromEcomm`) chỉ map SHOPEE/TIKTOK → đơn sàn khác trả fail *"Đơn hàng {mã} không phải đơn của {EcommerceCode}"*; đăng đơn (`PostOrder`) cũng chỉ map 2 sàn này.
- **Đồng bộ trạng thái đơn hoàn bỏ thầm lặng:** `MapEcommerceCode` chỉ map 8→Shopee / 9→TikTok / 10→Lazada; sàn khác hoặc đơn thiếu `MaDonHoanEcomm` bị bỏ qua không báo riêng. Cả batch không có đơn hợp lệ → "Không có đơn hợp lệ để đồng bộ".
- **Đơn hoàn về nhưng tồn/công nợ = 0 thay đổi:** đúng thiết kế. Tồn & công nợ chỉ đổi khi tạo HBBTL (PhieuBanTraLai) bên Accounting, và chỉ khi `IsHangVeKho = true`.

## Ví dụ cụ thể có số
Gian hàng Shopee "Shop ABC" đã kết nối, mapping kho "Kho HCM".
- 10:00 khách đặt đơn sàn **`#SP12345`** (2 sản phẩm). 10:01 webhook CREATE về → SCRM tạo `DonHangLogistic` MaDonEcomm=`SP12345`, trừ giữ tồn Kho HCM. Đơn hiện trong Đơn hàng TMĐT.
- 10:01 sàn gửi lại webhook CREATE `#SP12345` lần 2 (retry) → SCRM thấy đã có → trả đơn cũ, **vẫn 1 đơn** (không thành 2).
- 12:00 sàn báo "đã thanh toán" → webhook UPDATE → trạng thái đơn đổi. 18:00 "đang giao" → UPDATE tiếp.
- Hôm sau khách trả 1/2 sản phẩm → webhook RETURN_CREATE về `api/DonHangHoanEcomm/...` → tạo `DonHangHoan` 1 dòng SP. Tồn vẫn chưa cộng lại.
- 3 ngày sau hàng về Kho HCM, logistics hoàn đạt mốc "đã về" → nhân viên bấm "Tạo HBBTL" → lập phiếu trả lại 1 SP → **lúc này** tồn Kho HCM +1, công nợ khách giảm.
- Đơn Lazada `#LZ999` về bình thường, nhưng bấm "In đơn" → báo *"Đơn hàng LZ999 không phải đơn của LAZADA"* (in/đăng chưa hỗ trợ Lazada).

## Hậu điều kiện
- Đơn bán TMĐT: insert/cập `DonHangLogistic` (+ line/combo/thuế) — CREATE kéo ripple kho/kế toán/ES/notification; UPDATE chỉ đổi trạng thái/phí/hóa đơn.
- Đơn hoàn: insert/cập `DonHangHoan` (+ `DonHangHoanSanPham`) — **không** ripple tồn/kế toán cho tới khi tạo HBBTL.
- Webhook trả `ResSendOrderDataToCrm` / `ResSendReturnDataToCrm` (Success + CrmOrderId/CrmReturnId) để ecommerce_connect lưu lại liên kết và không gửi trùng lần sau.
- Đơn tạo lỗi giữ ở `OrderData.Status = CreateFail` (ecommerce_connect) cho phép tạo lại.
