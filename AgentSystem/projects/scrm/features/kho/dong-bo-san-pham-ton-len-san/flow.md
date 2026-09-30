# Luồng — Đồng bộ sản phẩm & tồn kho lên sàn TMĐT

> Chiều SCRM → sàn (Shopee/TikTok). Khác với chiều nhận đơn (sàn → SCRM). Xử lý chính nằm ở service **EcommerceConnect**; SCRM chỉ bắn số lượng sang.

## Tiền điều kiện
- Gian hàng sàn đã kết nối (UnitConnect có token Shopee/TikTok còn hạn).
- Đã bật đồng bộ ở gian hàng: `ProductSyncStatus = true` **và** `ProductSyncQuantity = true`.
- Đã khai báo **kho đồng bộ sản phẩm** cho gian hàng (`CrmProductSyncUnitChildId` + `CrmProductSyncWarehouseId`), hoặc fallback về kho tạo đơn (`CrmUnitChildId` + `CrmOrderWarehouseId`). Nếu dùng đa kho thì có WarehouseConnect mapping kho sàn ↔ kho SCRM.
- Sản phẩm đã được **map** (ProductConnect có cặp CrmSkuId ↔ PartnerSkuId) và sản phẩm đó bật đồng bộ (`SyncQuantity` null hoặc true).

## Luồng dùng (đồng bộ tồn tự động — luồng chính)
1. Tồn trong SCRM thay đổi (bán/nhập/xuất/kiểm kho/chuyển kho) → bảng tồn (KhoChiTiet) cập nhật.
2. **Job phía SCRM** (`JobSendQuantityToEcommerce`) định kỳ gom các sản phẩm có tồn thay đổi → build danh sách `ReqSaveProductQuantity` → gọi API **SaveProductQuantity** của EcommerceConnect.
3. EcommerceConnect ghi vào bảng **ProductQuantity** (hàng chờ xử lý, `InProcess`/`Status`).
4. **Job phía EcommerceConnect** (`JobProduct`) chạy 2 pha mỗi ~15 giây:
   - Pha 1 (`ProcessProductQuantityHandleData`): với mỗi sản phẩm, lấy record tồn mới nhất; tìm các UnitConnect đang bật đồng bộ; tìm ProductConnect đã map; khớp kho đồng bộ; tính số lượng cần đẩy → sinh bản ghi tạm `ProductQuantityTemp`.
   - Pha 2 (`ProcessProductQuantitySyncToEcomm`): đẩy số lượng tạm lên sàn qua `ShopeeProductService.UpdateStock` / `TiktokProductService.UpdateStock`.
5. Kết quả push thành công/thất bại được ghi log tiến trình (LogProcess/LogData) để truy vết.

## Luồng dùng (đồng bộ tồn thủ công)
1. User mở màn sản phẩm gian hàng → bấm nút **Đồng bộ** cho 1 hoặc nhiều sản phẩm.
2. FE gọi `SyncProductEcomm`; service duyệt từng ProductConnect, gọi thẳng `UpdateStock` (Shopee/TikTok) với số lượng hiện tại → đẩy ngay, không qua hàng chờ ProductQuantity.

## Luồng cấu hình / map sản phẩm (admin)
1. **Đồng bộ kho từ sàn về** (`GetListWarehouseEcomm`) để có danh sách kho sàn → tạo WarehouseConnect map kho sàn ↔ kho SCRM.
2. **Lấy sản phẩm trên sàn** (`GetListProductEcomm`) → hiển thị danh sách sản phẩm/SKU của sàn.
3. **Kết nối tự động** (`ConnectProductAuto`): ghép SCRM ↔ sàn theo SKU trùng; hoặc **Kết nối thủ công** (`ConnectProduct`) chọn từng cặp.
4. Bật/tắt `SyncQuantity` (và `SyncPrice` — hiện chưa tác dụng) cho từng sản phẩm.
5. Đặt cấu hình gian hàng: `ProductSyncRate` (tỷ lệ %), `ProductSyncOutOfStockWhenQuantityCrmBelow` (ngưỡng báo hết).

## Luồng đăng sản phẩm mới lên sàn (admin)
1. `CreateProductDraft` → tạo ProductDraft (chọn danh mục sàn, thuộc tính, giá, ảnh, cân nặng/kích thước) → publish `EcommerceConnect_CreateDraftProduct`.
2. Consumer lưu nháp → publish `EcommerceConnect_UploadDraftProductEcomm`.
3. Consumer `SaveSendProductDraftToEcomm` → POST lên Shopee/TikTok → cập nhật `PartnerProductId`, `Status`, `MessageError` của nháp.

## Các trường hợp / case biên
- **Sản phẩm chưa map (chưa có ProductConnect)** → Pha 1 không tìm thấy kết nối → bỏ qua, tồn KHÔNG được đẩy. (Nguyên nhân #1 "tồn sàn không nhảy".)
- **Gian hàng tắt đồng bộ** (`ProductSyncStatus`/`ProductSyncQuantity` ≠ true) → không có UnitConnect nào khớp → ProductQuantity bị bỏ qua / xóa, không đẩy.
- **Kho thay đổi ≠ kho đồng bộ** → điều kiện khớp kho (`tmpCrmProductSyncWarehouseId == CrmWarehouseId`) sai → không đẩy.
- **Tồn = 0 hoặc tồn ≤ ngưỡng báo hết** → đẩy lên sàn là **0** (hết hàng). Công thức: `qtySync = round(QtyCRM * Rate/100)`; nếu `qtySync ≤ Nguỡng` thì `qtySync = 0`.
- **Tồn âm trong SCRM** → qua công thức ra số âm/nhỏ → kẹp về 0; không đẩy số âm.
- **Tỷ lệ đồng bộ < 100%** → đẩy ít hơn tồn thật (giữ hàng đệm).
- **Token sàn hết hạn / lỗi** (`IsErrorToken`) → `UpdateStock` thất bại → ghi lỗi log, record có thể retry; tồn sàn không cập nhật cho đến khi kết nối lại.
- **Đăng sản phẩm nháp lỗi** → ProductDraft `Status` = lỗi + `MessageError`, không tạo được sản phẩm trên sàn; sửa nháp rồi gửi lại.
- **Đồng bộ giá** → có cấu hình `ProductSyncPrice`/`SyncPrice`/`ProductSyncRate` nhưng KHÔNG có callsite `UpdatePrice` → giá KHÔNG được đẩy (xem dev.md, Bug-prone).

## Ví dụ cụ thể có số
Gian hàng Shopee "Shop A", bật đồng bộ số lượng, `ProductSyncRate = 80%`, `ProductSyncOutOfStockWhenQuantityCrmBelow = 3`.
- Sản phẩm "Áo thun M" (đã map SKU `AT-M` ↔ sàn) có tồn SCRM = **50** ở kho đồng bộ.
  - `qtySync = round(50 * 80 / 100) = 40`. 40 > 3 → đẩy lên Shopee **40**.
- Bán bớt còn tồn SCRM = **4**:
  - `qtySync = round(4 * 80/100) = 3`. 3 ≤ 3 (ngưỡng) → đẩy lên Shopee **0 (hết hàng)** dù kho còn 4.
- Sản phẩm "Quần Jean 32" tồn SCRM = **20** nhưng **chưa map** → Pha 1 bỏ qua → Shopee giữ nguyên số cũ, không đồng bộ.
- Bấm **Đồng bộ thủ công** cho "Áo thun M" lúc tồn = 50 (rate cho luồng thủ công lấy số truyền vào) → `UpdateStock` đẩy ngay.
- User sửa giá "Áo thun M" trong SCRM từ 150.000 → 180.000 → giá Shopee **vẫn 150.000** (đồng bộ giá chưa chạy).

## Hậu điều kiện
- Tồn sản phẩm đã map trên sàn = số lượng đã tính (`qtySync`), không vượt quá tồn thật theo tỷ lệ; sản phẩm dưới ngưỡng hiển thị hết hàng.
- Bản ghi ProductQuantity/ProductQuantityTemp được đánh dấu đã xử lý (`InProcess`/`Status`), log tiến trình lưu lại ai/khi nào/kết quả.
- SCRM **không đổi** tồn của chính mình do đồng bộ (chiều đẩy 1 chiều SCRM → sàn).
- Map sản phẩm (ProductConnect) lưu DB; gỡ map chỉ ngắt đồng bộ về sau, không xóa sản phẩm/tồn đã có trên sàn.
