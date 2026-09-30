---
controller: DonHangGiaoVanDoiTacController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > thao tác đăng đơn/chọn đối tác giao vận: khi user chọn đơn vị vận chuyển (GHN/GHTK/Viettel Post/J&T/Best/Ahamove...), gọi lấy thông tin kết nối/cấu hình giao vận đối tác để chuẩn bị tạo vận đơn"
muc_dich: "Lấy thông tin cấu hình kết nối của một đơn vị giao vận đối tác (theo shippingUnitCode) bằng cách proxy sang service delivery_connect (DeliveryConnect/api/unit/GetUnitConnect), phục vụ luồng đăng đơn / chọn hãng vận chuyển."
alias: ["giao vận đối tác", "đối tác vận chuyển", "thông tin kết nối ĐVVC", "cấu hình hãng ship", "GetUnitConnect", "đơn vị vận chuyển kết nối"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "IDonHangGiaoVanDoiTacService (OrderLogistic.Service/Generated/DonHangGiaoVanDoiTacService.cs)"
  - "IDeliveryConnectService (MapUserLoginDeliveryConnect — map user sang delivery_connect)"
  - "IClientHelper + DeliveryConnectServiceSetting (HTTP POST sang delivery_connect)"
cross_module_in: []
cross_module_out:
  - "delivery_connect (GetThongTinGiaoVanDoiTac → POST {DeliveryConnect}/api/unit/GetUnitConnect lấy ResGetUnitConnect)"
---

# DonHangGiaoVanDoiTacController — Đơn hàng giao vận đối tác

> Controller mỏng (1 endpoint), chỉ là **proxy đọc** sang service delivery_connect. Không persist gì, không có entity riêng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin kết nối ĐVVC | (auto khi chọn hãng ship) | GET `api/DonHangGiaoVanDoiTac/GetThongTinGiaoVanDoiTac?shippingUnitCode=` | `[SBAuthorize]` (mở — chỉ cần login) | public | có shippingUnitCode | Map UserLogin sang delivery_connect → POST `{DeliveryConnect}/api/unit/GetUnitConnect` (body ReqGetUnitConnect{ShippingUnitCode}) → trả `ResGetUnitConnect.Data`. Thất bại/null → "Get thông tin kết nối thất bại." | proxy delivery_connect | Không gắn permission key riêng; chỉ gate qua tiện ích `TI_OrderLogistic`. Có comment TODO "check tiện ích" chưa làm (DonHangGiaoVanDoiTacService.cs:45) |

## Endpoint nội bộ (private / service-to-service)
- [internal-out] **OrderLogistic → delivery_connect `GetUnitConnect`**: gọi qua `IClientHelper.PostAsync` với URL `{ServiceUrl}/api/unit/GetUnitConnect`, truyền `userLogin.Token`. Trả về cấu hình kết nối của một hãng vận chuyển (ResGetUnitConnect) để FE biết hãng có kết nối hay không trước khi đăng đơn.

## Quan hệ
- **Gọi sang:** `IDeliveryConnectService.MapUserLoginDeliveryConnect` (map user) + `IClientHelper` POST sang delivery_connect; URL/host lấy từ `DeliveryConnectServiceSetting`.
- **Dữ liệu (entities):** không có — chỉ DTO `ReqGetUnitConnect`/`ResGetUnitConnect`.
- **Ripple / đồng bộ:** không ripple; thao tác READ-ONLY (lấy cấu hình). Thao tác tạo vận đơn thật nằm ở các controller DeliveryConnect/* (Ghn/Ghtk/...) và OrderController.

## Lỗi đã biết / lưu ý kỹ thuật
- `DonHangGiaoVanDoiTacService.cs:29-55`: bọc try/catch trả `ex.Message` ra UI; nếu delivery_connect down/timeout sẽ trả message lỗi thô.
- Comment `//check tiện ích.` (DonHangGiaoVanDoiTacService.cs:45) — chưa kiểm tra quyền tiện ích trên kết quả; hiện chỉ kiểm tra Success/Data null.
- Controller chỉ READ — đừng tìm logic tạo vận đơn ở đây (nằm ở các controller hãng vận chuyển trong folder DeliveryConnect/).
