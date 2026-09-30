# Luồng — Kết nối TikTok

## Tiền điều kiện
- Đơn vị đã **bật tiện ích TikTok** (`TI_Tiktok`).
- Người dùng có quyền marketing/admin (DV-AD-TONG/CN/MCN, DV-QL-MKT, DV-MKT-LEAD, DV-MKT).
- Đã khai báo **kho** (kho nhận đơn + kho đồng bộ tồn) và có **sản phẩm CRM** để ghép.
- **Ecommerce-connect gateway** hoạt động (giữ token + nói chuyện với TikTok).

## Luồng dùng (kết nối end-to-end)
> **Thực tế FE đi đường này — KHÔNG qua controller `TiktokConnect`.** Màn `ecomm-gian-hang-danh-sach` tham số hóa theo `EcommerceCode`; nút Thêm kết nối / Xác thực lại gọi thẳng `EcommerceService.CRM_GetAuthorizationUrl({ ecommerceCode: Tiktok, ... })` rồi redirect.
1. **Ủy quyền (OAuth):** mở màn gian hàng (TikTok) → bấm **Thêm kết nối** → FE lấy URL ủy quyền từ gateway → redirect sang TikTok.
2. **Đăng nhập + cấp quyền** trên TikTok → callback về hệ thống → **lưu token** (`AuthorizationController.CRM_SaveAuthorization`, entity `Authorization`).
3. **Map gian hàng** (`UnitConnectController.CRM_SaveUnitConnect`): chọn kho nhận đơn + kho đồng bộ tồn + đơn vị; hệ thống gắn **callback nhận đơn** (OrderLogistic) + url tra SKU.
4. **Map kho** (`WarehouseConnectController`) nếu cần.
5. **Ghép sản phẩm:** lấy SP TikTok (`CRM_GetListProductEcomm`) → **Ghép tự động** theo SKU (`CRM_ConnectProductAuto`) hoặc **ghép tay** (`CRM_ConnectProduct`).
6. **Đồng bộ tồn** (`CRM_SyncProductEcomm`): lấy kho từ gian hàng → đọc tồn `KhoChiTiet` → đẩy số lượng lên TikTok qua gateway.
7. **Đơn mới trên TikTok** → callback `OrderLogistic/api/DonHang/NhanDuLieuDonHangTMDT` → đơn về hệ thống.

## Luồng cấu hình (admin)
- Mua / bật tiện ích TikTok.
- Cấu hình URL dịch vụ (OrderLogistic, WarehouseProduct) đúng để callback nhận đơn + tra SKU hoạt động.

## Case biên
- **Controller `TiktokConnect.ConnectTiktok` là dead stub:** trả cứng `"true"`, **FE không gọi**. Nên không ảnh hưởng khách; nhưng nếu ai gọi nhầm endpoint này thì không nhận URL ủy quyền thật.
- **Token TikTok hết hạn / thu hồi** → đơn không về, đồng bộ lỗi → **Xác thực lại**.
- **Map gian hàng nhưng URL dịch vụ sai** → lưu thành công nhưng TikTok **không callback được** → mất đơn (khó phát hiện).
- **Đồng bộ tồn thiếu gian hàng** → "Lỗi không được để trống gian hàng."
- **Đồng bộ tồn danh sách SP rỗng** → "Lỗi không được để trống sản phẩm."
- **Gian hàng không có kho đồng bộ tồn / kho nhận đơn** → "Lỗi không tìm thấy kho để đồng bộ."
- **SP con không khớp SKU** → bỏ qua (số lượng giữ -1; cần xác nhận gateway lọc -1).
- **Xóa kết nối gian hàng** → ngắt nhận đơn + ngừng đồng bộ.

## Ví dụ cụ thể có số
Shop D có gian hàng TikTok, kho "Kho Tổng" tồn 80 cái SKU `QUAN-JEAN-32`.
- Bấm Thêm kết nối → ủy quyền TikTok → map gian hàng, kho nhận đơn = kho đồng bộ tồn = "Kho Tổng".
- Lấy SP TikTok → Ghép tự động → khớp theo SKU; SP lệch SKU ghép tay.
- Đồng bộ tồn → `QUAN-JEAN-32` đẩy lên TikTok 80 cái.
- Khách đặt 3 cái → đơn về hệ thống qua callback; lần đồng bộ sau đẩy 77.

## Hậu điều kiện
- Token gian hàng lưu `Authorization`; gian hàng map lưu `UnitConnect` (kèm callback nhận đơn + url tra SKU).
- Sản phẩm ghép lưu `ProductConnect`.
- Đồng bộ tồn đẩy số lượng tồn CRM lên TikTok (một chiều).
- Đơn TikTok về hệ thống qua OrderLogistic để xử lý/giao như đơn thường.
