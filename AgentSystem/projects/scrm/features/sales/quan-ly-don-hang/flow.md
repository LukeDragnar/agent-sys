# Luồng — Quản lý đơn hàng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị + chi nhánh; có role cho thao tác tương ứng (xem summarize).
- Có sản phẩm / khách hàng / contact để lên đơn.
- Đăng đơn vận chuyển: đã cấu hình kết nối đơn vị vận chuyển (Ahamove tại màn này; GHN/GHTK/VTP/J&T ở màn DonHangGiaoVanDoiTac).
- Phát hành HĐĐT: đơn vị có tiện ích `TI_Invocie_Misa` hoặc `TI_Invocie_Viettel`.
- Cập nhật trạng thái hàng loạt: user được bật quyền trong cấu hình quản trị (`KiemTraQuyenUser`).

## Luồng dùng (vòng đời đơn)
1. **Tạo đơn** (`TaoDonLogisticDto`): tạo đơn nháp. Nếu có ContactId → khóa Redis `LockTaoDon:{ContactId}` (TTL 60s) chống tạo trùng cùng SĐT; release ở `finally`.
2. **Chốt đơn** (`ChotDon`): set `DonHangNgayChot`, chuyển trạng thái **DaChot**; cùng cơ chế lock theo ContactId; đồng bộ Contact.
3. **Tính phí VC** (`TinhPhiVc`): gọi OrderService, áp tier cấu hình hỗ trợ ship.
4. **Xuất kho:** quét **IMEI** (`MappingImeiSPIntoOrder`) / **lô date** (`MappingLoDateSPIntoOrder`) → **Hoàn thành SP** (`HoanThanhSanPham`) → ripple WarehouseProduct sinh phiếu xuất, trừ tồn.
5. **Đăng đơn ĐVVC:** Ahamove (`Crm_DangDonAhamove`) → set `GiaoHangHinhThucGiaoHangMa`, lấy `IdTransaction` từ `DonHangDangDon`, POST sang delivery_connect `/api/order/CreateOrderAhamove`.
6. **Cập nhật trạng thái giao hàng** (`CapNhatTTGH`, `CapNhatTTDoiSoat`, `CapNhatTTCareDon`, `CapNhatTTMaDonHang`…): gate runtime `KiemTraQuyenUser`; chuyển DaDang → DaLayHang → DaGiaoHang.
7. **Kế toán:** đạt mốc cấu hình → sinh phiếu bán; **Phát hành HĐĐT** (`PhatHanhHoaDon`) → Accounting → callback `UpdateTrangThaiPhatHanhHoaDon`.

## Luồng cấu hình (admin)
- **Cấu hình quyền cập nhật hàng loạt:** admin bật quyền cho user qua cấu hình quản trị (`cauHinhQuanTriService.KiemTraQuyenUser`).
- **Tiện ích HĐĐT:** bật `TI_Invocie_Misa` / `TI_Invocie_Viettel` để cho phép phát hành.
- **Cấu hình hỗ trợ ship (tier):** ảnh hưởng `TinhPhiVc` → `DonHangHoTroShipFee`.
- **🆕 Cho phép hủy chốt không giới hạn thời gian:** bật `IsKhongGioiHanThoiGianHuyChotDon` (CauHinhSetting đơn vị) → sau 30 ngày vẫn hủy chốt được nhờ fallback snapshot. Mặc định off = giữ giới hạn 30 ngày.

## Các trường hợp / case biên
- **Tạo / chốt đơn trùng cùng SĐT:** lock Redis `LockTaoDon:{ContactId}` chặn lần thứ hai trong 60s. ContactId rỗng → bỏ qua lock.
- **🆕 Hủy chốt đúng lúc đang đăng đơn:** `HuyChotDon` lấy lock chung `OL_ProcessDonHangDangDon_{orderId}` với luồng đăng đơn; nếu đơn đang đăng → chặn hủy chốt ("Đơn đang trong quy trình đăng đơn, không thể hủy chốt. Vui lòng thử lại sau!"). Vào lock rồi đọc lại đơn để guard thấy state đã commit; nhả lock sau khi commit xong.
- **🆕 Hủy chốt sau 30 ngày:** rollback data `OrderRollBackLogisticDoiSoat` đã bị job cleanup xóa → nếu đơn vị bật `IsKhongGioiHanThoiGianHuyChotDon` thì fallback đọc `DonHangLogisticChotDonSnapshot` (1-1, không bị xóa) để rollback; config off → không hủy chốt được (như cũ). Đơn chốt trước khi bật config / trước khi deploy snapshot → không có snapshot → vẫn chặn.
- **🆕 KM bội số quá entitlement:** SL quà đã chọn > entitlement ở SL mua hiện tại (bội số/điều kiện đổi) → `IsCanChonLai=true`, bắt sale chọn lại quà. Dịch vụ Promotion lỗi lúc kiểm → không khóa (fail-open).
- **Controller bị kill giữa chừng:** lock không release ở `finally` được → tự hết hạn sau 60s (TTL), không stuck vĩnh viễn.
- **Cập nhật trạng thái hàng loạt khi chưa được cấp quyền cấu hình:** `KiemTraQuyenUser` fail → "Bạn không có quyền" (kể cả admin).
- **Phát hành HĐĐT khi không có tiện ích:** `CheckTienIch` chặn → không phát hành.
- **Đăng đơn Ahamove lỗi sàn:** trả lỗi từ delivery_connect; kiểm tra `DonHangDangDon.IdTransaction` + response.
- **Xóa nhiều đơn không phải admin tổng:** `XoaDonHangByIds` chỉ DV-AD-TONG → từ chối.
- **Export khi service bận:** trả `Message="Service_Unavailable"` → HTTP 503; lỗi khác → 500 `EXPORT_ERROR`.
- **Hủy chốt đơn:** revert đơn về nháp (chỉ khi đang DaChot).
- **Endpoint dead:** `FixBugOrderLogistic`, `ExportExcelDonHangSaleFixBug` return ngay (không có tác dụng); `FixSaleDisplayName?key=...` là GET sửa data, gate bằng key hardcode — không dùng ở production.

## Ví dụ cụ thể có số
Sale Linh tạo đơn cho khách (SĐT 0912xxxxxx, ContactId X), 2 sản phẩm, 1 cái quản lý IMEI.
- Linh bấm **Tạo đơn** → đơn nháp; hệ thống đặt lock `LockTaoDon:X` 60s. Linh lỡ double-click → lần 2 bị chặn (lock còn hiệu lực).
- Linh thêm SP, **Tính phí VC** = 30.000đ (áp tier hỗ trợ ship còn 22.000đ), **Chốt đơn** → trạng thái DaChot, ngày chốt = hôm nay.
- Kho quét IMEI cho cái điện thoại, **Hoàn thành SP** → phiếu xuất trừ tồn 1 cái; tồn từ 5 → 4.
- Kho **Đăng đơn Ahamove** → lấy IdTransaction, đẩy sang delivery_connect → đơn sang DaDang.
- Hôm sau shipper lấy hàng → cập nhật **DaLayHang**; giao xong → **DaGiaoHang** (qua `CapNhatTTGH` — user này đã được bật quyền trong cấu hình quản trị).
- Đơn đạt mốc → sinh phiếu bán kế toán; kế toán **Phát hành HĐĐT** (đơn vị có tiện ích Misa) → callback set trạng thái phát hành.
- Cuối tháng admin tổng lọc 12 đơn test → **Xóa hàng loạt** (chỉ admin tổng làm được, ghi log).

## Hậu điều kiện
- Tạo / chốt đơn → ghi đơn + đồng bộ Contact; lock release sau xử lý hoặc hết TTL.
- Hoàn thành SP → phiếu xuất WarehouseProduct, tồn kho giảm.
- Đăng đơn Ahamove → bản ghi `DonHangDangDon` + đơn ở hệ thống vận chuyển.
- Phát hành HĐĐT → hóa đơn ở Accounting + trạng thái phát hành trên đơn (qua callback).
- Mọi thay đổi đơn → đồng bộ Contact + cập nhật ES store (search đơn).
