# Flow — Kết nối đơn vị vận chuyển

Tài liệu luồng cho QA / đào tạo. Mô tả 2 luồng: **cấu hình (kết nối hãng)** và **dùng (đăng đơn)**, kèm case biên và ví dụ có số.

---

## Tiền điều kiện

- Tài khoản SCRM có quyền thao tác giao vận: cấp **Tổng / Chi nhánh / Quản lý chi nhánh** (`DV-AD-TONG`, `DV-AD-CN`, `DV-AD-MCN`).
- Đã có **kho lấy hàng / chi nhánh** để gắn tài khoản giao vận.
- Có **token / tài khoản hãng** hợp lệ (lấy từ trang quản trị của hãng GHN, Viettel Post, J&T...).
- Gói dịch vụ của tenant bật tính năng tích hợp giao vận (*cần product xác nhận*).

---

## Luồng cấu hình — Kết nối hãng / nhập token

1. Vào **Kho → Kết nối đơn vị vận chuyển**, chọn hãng (vd GHN).
2. Hệ thống gọi `GetThongTinDoiTac` của hãng + `GetThongTinKetNoiDonVi` để lấy **cấu hình form động**: hãng cần những trường nào (Token, Tài khoản, Kho lấy hàng, dịch vụ, phương thức lấy...). Mỗi trường có cờ `_Enable` / `_Mandatory` / danh sách giá trị `_ListValueOption`.
3. Người dùng nhập:
   - **PartnerToken** (token/API key) và/hoặc **PartnerUserName + PartnerPassword** tùy hãng.
   - **PartnerWarehouseId** = kho/cửa hàng lấy hàng (có thể bấm lấy danh sách cửa hàng từ hãng qua `GetDanhSachCuaHangShipping`).
   - Phương thức lấy hàng / dịch vụ / bảo hiểm... (PropValue1..5).
4. Bấm:
   - **Kết nối** → `CRM_ConnectShippingUnit`: SCRM gọi sang DeliveryConnect service để **kiểm tra tài khoản với hãng**. Hãng OK → lưu kết nối. Lỗi → trả message.
   - **Lưu** → `SaveThongTinKetNoiDonVi`: lưu cấu hình mà không bắt buộc test ngay.
   - Với hãng OAuth (vd Ninja Van) → `CRM_GetUrlLoginPartner` mở trang đăng nhập của hãng, hãng callback về SCRM.
5. Kết nối thành công → tài khoản giao vận sẵn sàng để đăng đơn.
6. **Hủy kết nối**: `CRM_DisconnectShippingUnit` theo `Id` tài khoản giao vận.

> SCRM **không tự gọi thẳng API hãng**. SCRM là proxy: gọi sang một **DeliveryConnect service** riêng (đặt ở `_deliveryConnectServiceSetting.ServiceUrl`), service này mới chứa tích hợp từng hãng và lưu token. Mỗi tenant nhận diện qua `ServerId/ServerCode/CrmUnitId`.

---

## Luồng dùng — Đăng đơn lên hãng

1. Đơn hàng đã chốt, chọn **hãng (GiaoHangDoiTacMa)** + **tài khoản giao vận (IdGiaoHangTaiKhoan)**.
2. (Tùy chọn) **Tính phí**: `Order/CalculateFee` → gom sản phẩm (số lượng, cân nặng, kích thước), người gửi/nhận, COD → trả phí ship dự kiến.
3. **Đăng đơn**: SCRM map dữ liệu đơn → đẩy message **`MessageCreateOrder` qua RabbitMQ** (không gọi đồng bộ). Consumer phía DeliveryConnect gọi hãng tạo vận đơn.
4. Hãng trả kết quả → webhook `Order/StatusCreateOrder` → hàm `StatusCreateOrder`:
   - **Thành công**: gắn `MaDonGiaoVan` (mã vận đơn), `ParnerDonHangId`, `GiaoHangMaInDon`; set `DonHangIsDangDon = true`, trạng thái **Đã đăng**; cập nhật phí ship nếu hãng trả khác; sinh **phiếu bán hàng** cho đơn online; bắn thông báo + đồng bộ ES / Marketing / Bravo.
   - **Thất bại**: **cộng lại tồn kho** (chờ xuất), rollback phiếu bán hàng, ghi log "Đăng đơn thất bại".
5. **In vận đơn**: `Order/PrintOrders` (hàng loạt).
6. **Theo dõi trạng thái**: webhook `Order/ReceiveStatusOrder` cập nhật trạng thái giao hàng theo thời gian.
7. **Hủy đăng đơn**: đẩy `MessageCancelOrder` qua RabbitMQ → webhook `Order/StatusCancelOrder` → `StatusCancelOrder`: xóa `MaDonGiaoVan`, đưa trạng thái về cấu hình (mặc định **Chờ vận đơn**), `DonHangIsDangDon = false`, **cộng lại kho**, rollback phiếu bán hàng.

---

## Case biên

| Case | Hành vi mong đợi |
|------|------------------|
| Token sai / chưa nhập | `CRM_ConnectShippingUnit` trả `Success=false` + message "Kết nối tài khoản thất bại". Không lưu kết nối dùng được. |
| Token hết hạn (trước chạy được) | Đăng đơn thất bại với lỗi xác thực từ hãng; cần nhập lại token. |
| Chưa đăng nhập / `userLogin == null` | Trả "Không tìm thấy thông tin đăng nhập." |
| COD ≠ (TienCuoi − DatCoc) | `CreateOrder` chặn: "Tiền thu hộ không chính xác." Không đẩy message. |
| Đơn không tồn tại | "Đơn hàng không tồn tại." |
| Đăng đơn thất bại phía hãng | Cộng lại kho (DangHuyDon_V2 mode 2), rollback phiếu bán hàng, log DangDonThatBai. |
| Hủy đơn nhưng đơn đã hủy ở tiến trình khác | "Đơn hàng đã hủy đăng đơn ở một tiến trình khác." (chặn double) |
| Hãng trả phí ship mới > 0 và khác phí cũ | Cộng chênh lệch vào `DonHangGiaoHangShipFee` + `DonHangHoTroShipFee`, **không đổi** tiền thu khách. |
| Hãng trả phí null / 0 | Giữ nguyên phí cũ (coi như hãng ẩn phí, vd GHTK). |
| Giao hàng tùy chỉnh | Không gọi API hãng lấy mã tự động; quản lý thủ công. |

---

## Ví dụ có số

**Đơn ĐH001**, kết nối GHN, kho "CN Hà Nội":
- Tiền hàng cuối `DonHangTienCuoi` = 500.000đ, đặt cọc `DonHangDatCoc` = 100.000đ.
- COD phải thu `DonHangTienThuKhach` = 500.000 − 100.000 = **400.000đ**. → khớp, qua kiểm tra.
- Tính phí: 1 sản phẩm, 1.200g, GHN trả **30.000đ** ship dự kiến.
- Đăng đơn → message vào RabbitMQ. Hãng tạo vận đơn → callback:
  - `MaDonGiaoVan` = "GHN1234567", trạng thái → **Đã đăng**.
  - Hãng trả phí thật **35.000đ** (khác 30.000đ): chênh **+5.000đ** → `DonHangGiaoHangShipFee` 30.000 → 35.000; `DonHangHoTroShipFee` +5.000. **COD vẫn 400.000đ** (không đổi).
- Nếu COD nhập nhầm 350.000đ (≠ 400.000) → đăng đơn bị chặn "Tiền thu hộ không chính xác."

---

## Hậu điều kiện

- **Kết nối thành công**: có tài khoản giao vận sẵn sàng (token lưu ở DeliveryConnect service), dùng lại cho mọi đơn.
- **Đăng đơn thành công**: đơn có `MaDonGiaoVan`, trạng thái Đã đăng, kho trừ chờ xuất, có phiếu bán hàng, log lịch sử, đồng bộ ES/Marketing/Bravo.
- **Hủy / thất bại**: kho cộng lại, không còn mã vận đơn, phiếu bán hàng rollback, log tương ứng.
