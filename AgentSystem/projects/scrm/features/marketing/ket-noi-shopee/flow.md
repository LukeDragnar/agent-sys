# Luồng — Kết nối Shopee

## Tiền điều kiện
- Đơn vị đã **bật tiện ích Shopee** (`TI_Shopee`).
- Người dùng có quyền marketing/admin (DV-AD-TONG/CN/MCN, DV-QL-MKT, DV-MKT-LEAD, DV-MKT) cho bước khởi tạo kết nối.
- Đã khai báo **kho** (kho nhận đơn + kho đồng bộ tồn) và có **sản phẩm CRM** để ghép.
- Hệ thống **ecommerce-connect gateway** (dịch vụ kết nối sàn ngoài) hoạt động — đây là nơi thực sự giữ token + nói chuyện với Shopee.

## Luồng dùng (kết nối end-to-end)
1. **Ủy quyền (OAuth):** Người dùng mở màn gian hàng TMĐT (Shopee) → bấm **Kết nối / Thêm kết nối** → hệ thống lấy URL ủy quyền từ gateway → redirect sang Shopee.
2. Người dùng **đăng nhập + cấp quyền** trên Shopee → Shopee callback về hệ thống → **lưu token** (qua `AuthorizationController.CRM_SaveAuthorization`, entity `Authorization`).
3. **Map gian hàng:** Người dùng cấu hình gian hàng (`UnitConnectController.CRM_SaveUnitConnect`): chọn kho nhận đơn + kho đồng bộ tồn + đơn vị. Hệ thống tự gắn **callback nhận đơn** (về OrderLogistic) + url tra sản phẩm theo SKU.
4. **Map kho** (`WarehouseConnectController`) nếu cần map kho sàn ↔ kho CRM.
5. **Ghép sản phẩm:** lấy danh sách SP Shopee (`CRM_GetListProductEcomm`) → **Ghép tự động** theo SKU (`CRM_ConnectProductAuto`) hoặc **ghép tay** từng SP (`CRM_ConnectProduct`, resolve SP cha CRM).
6. **Đồng bộ tồn:** bấm Đồng bộ tồn (`CRM_SyncProductEcomm`) → hệ thống lấy kho từ gian hàng → đọc tồn `KhoChiTiet` của từng SP con → đẩy số lượng lên Shopee qua gateway.
7. Từ đó, **đơn mới trên Shopee** → Shopee gọi callback `OrderLogistic/api/DonHang/NhanDuLieuDonHangTMDT` → đơn về hệ thống.

## Luồng cấu hình (admin)
- Mua / bật tiện ích Shopee.
- Đảm bảo cấu hình URL dịch vụ (OrderLogistic, WarehouseProduct) đúng để callback nhận đơn + tra SKU hoạt động.

## Case biên
- **Khởi tạo kết nối qua controller `ShopeeConnect` (stub):** endpoint `ConnectShopee` hiện trả cứng `"true"`, không trả URL ủy quyền thật → nếu FE đi qua đường này thì kết nối **gãy ngay bước 1**. Luồng thật đi qua FE-service gọi gateway (xem dev.md).
- **Token Shopee hết hạn / bị thu hồi** → đơn không về, đồng bộ lỗi → phải **ủy quyền lại**.
- **Map gian hàng nhưng cấu hình URL dịch vụ sai** → lưu vẫn thành công nhưng Shopee **không callback được** → mất đơn / không tra được SKU (khó phát hiện).
- **Đồng bộ tồn khi chưa chọn gian hàng** → chặn "Lỗi không được để trống gian hàng."
- **Đồng bộ tồn khi danh sách sản phẩm rỗng** → chặn "Lỗi không được để trống sản phẩm."
- **Gian hàng không có kho đồng bộ tồn (và không có kho nhận đơn)** → chặn "Lỗi không tìm thấy kho để đồng bộ."
- **SP con không khớp SKU khi đồng bộ tồn** → bỏ qua SP đó (số lượng giữ giá trị khởi tạo -1; cần xác nhận gateway có lọc -1 không — xem dev.md).
- **Xóa kết nối gian hàng** → ngắt nhận đơn + ngừng đồng bộ SP/kho của gian hàng đó.

## Ví dụ cụ thể có số
Shop C có gian hàng Shopee với 50 sản phẩm, kho "Kho HCM" tồn thực 120 cái cho SKU `AO-TRANG-M`.
- Ủy quyền Shopee → map gian hàng, chọn kho nhận đơn = kho đồng bộ tồn = "Kho HCM".
- Lấy 50 SP Shopee → Ghép tự động theo SKU → khớp 45 SP, còn 5 SP SKU lệch → ghép tay.
- Đồng bộ tồn → SKU `AO-TRANG-M` đẩy lên Shopee 120 cái (đúng tồn kho HCM).
- Khách đặt 2 cái trên Shopee → đơn về hệ thống qua callback, vào kho HCM; lần đồng bộ tồn sau đẩy lên 118.

## Hậu điều kiện
- Token gian hàng lưu ở `Authorization`; gian hàng map lưu ở `UnitConnect` (kèm callback url nhận đơn + url tra SKU).
- Sản phẩm ghép lưu ở `ProductConnect` (map SKU sàn ↔ SP CRM).
- Đồng bộ tồn đẩy số lượng tồn CRM lên Shopee (một chiều); không đổi tồn CRM.
- Đơn Shopee về hệ thống qua OrderLogistic để xử lý/giao như đơn thường.
