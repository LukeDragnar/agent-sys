---
controller: DeliveryUnitConnectController
module: kho
service: OrderLogistic
man_hinh_ui: "Cấu hình > Kết nối đơn vị vận chuyển (ĐVVC) — màn quản lý kết nối tài khoản giao vận của đơn vị/chi nhánh: chọn ĐVVC (GHN/GHTK/J&T/Ahamove/Best/EMS/ViettelPost...), nhập/lưu thông tin kết nối, lấy URL đăng nhập OAuth đối tác, chọn phương thức giao hàng, ngắt kết nối"
muc_dich: "Quản lý KẾT NỐI/CẤU HÌNH tài khoản giao vận giữa CRM và các ĐVVC: lưu/đọc thông tin kết nối, kết nối/ngắt kết nối tài khoản đối tác, lấy URL login OAuth đối tác, list cửa hàng & phương thức giao hàng. Là controller cấu hình — KHÔNG đăng đơn (đăng đơn nằm ở OrderService.CreateOrder qua RabbitMQ)."
alias: ["kết nối đơn vị vận chuyển", "kết nối ĐVVC", "cấu hình giao vận", "kết nối tài khoản giao hàng", "shipping unit connect", "phương thức giao hàng", "url login đối tác giao vận"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "IDeliveryUnitConnectService (OrderLogistic.Service/Generated/DeliveryUnitConnect/DeliveryUnitConnectService.cs)"
  - "delivery_connect private service (proxy HTTP tới {DeliveryConnectServiceSetting.ServiceUrl}/api/unit/* : SaveUnitConnect, GetUnitConnect, GetStoreShippingUnit, GetUrlLogin)"
  - "OtherService.IDeliveryConnectService (MapUserLoginDeliveryConnect, CRM_ConnectShippingUnit, CRM_DisConnectShippingUnit, GetDeliveryMethodByShippingUnitCode...)"
cross_module_in:
  - "FE màn cấu hình ĐVVC + các màn đăng đơn (lấy phương thức giao hàng theo tài khoản đã kết nối)"
cross_module_out:
  - "delivery_connect (toàn bộ thao tác kết nối/đọc cấu hình proxy sang service riêng delivery_connect qua IClientHelper / IDeliveryConnectService)"
---

# DeliveryUnitConnectController — Kết nối & cấu hình đơn vị vận chuyển (ĐVVC)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu thông tin kết nối ĐVVC | "Lưu" form kết nối | POST `api/DeliveryUnitConnect/SaveThongTinKetNoiDonVi` | `[SBAuthorize]` (mở — chỉ cần login) | public | form kết nối | Map UserLogin + CrmUnitChildId (chi nhánh chọn) + CallBackUrl (OrderLogisticServiceUrl+CallBackUrl), proxy POST `delivery_connect /api/unit/SaveUnitConnect`. Lỗi → "Kết nối giao hàng thất bại: {msg}" | proxy delivery_connect | Side-effect lưu kết nối nằm ở delivery_connect, không ở service này |
| 2 | Đọc thông tin kết nối ĐVVC | load màn kết nối | POST `api/DeliveryUnitConnect/GetThongTinKetNoiDonVi` | `[SBAuthorize]` | public | - | Proxy POST `delivery_connect /api/unit/GetUnitConnect` theo ShippingUnitCode + CrmUnitChildId → trả `ResGetUnitConnect.Data` | proxy delivery_connect | - |
| 3 | List cửa hàng theo ĐVVC | dropdown cửa hàng | POST `api/DeliveryUnitConnect/GetDanhSachCuaHangShipping` | `[SBAuthorize]` | public | đã kết nối | Proxy POST `delivery_connect /api/unit/GetStoreShippingUnit` (lấy store/shop của tài khoản giao vận) | proxy delivery_connect | - |
| 4 | Phương thức giao hàng theo ĐVVC | dropdown phương thức | GET `api/DeliveryUnitConnect/GetPhuongThucGiaoHangByShipping?shippingUnitCode=&propertyCode=` | `[SBAuthorize]` | public | có shippingUnitCode+propertyCode | Validate 2 tham số bắt buộc → `GetDeliveryMethodByShippingUnitCode` → list `ResDeliveryPropertyPrivate` | proxy delivery_connect | Thiếu 1 trong 2 tham số → trả lỗi "Thiếu thông tin shippingUnitCode hoặc propertyCode" |
| 5 | Phương thức giao hàng theo tài khoản | dropdown phương thức | GET `api/DeliveryUnitConnect/GetPhuongThucGiaoHangByIdGiaoHangTaiKhoan?shippingUnitCode=&idGiaoHangTaiKhoan=` | `[SBAuthorize]` | public | có shippingUnitCode | Map sang `ReqGetDeliveryMethodByCrmUnitChildId` (CrmUnitChildId = idGiaoHangTaiKhoan) → `GetDeliveryMethodByCrmUnitChildId` | proxy delivery_connect | Validate chỉ bắt buộc `shippingUnitCode` ("Mã đối tác"); idGiaoHangTaiKhoan optional |
| 6 | Thông tin bổ sung theo ĐVVC | form cấu hình | POST `api/DeliveryUnitConnect/GetThongTinBoSungByShipping` | `[SBAuthorize]` | public | - | `GetInfoGiaoHangByShippingUnitCode` (info giao hàng bổ sung theo ĐVVC) | proxy delivery_connect | - |
| 7 | Kết nối tài khoản ĐVVC (CRM) | "Kết nối" | POST `api/DeliveryUnitConnect/CRM_ConnectShippingUnit` | `[SBAuthorize]` | public | form kết nối | Map UserLogin + ShippingUnitCode (ToUpper) + CallBackUrl → `CRM_ConnectShippingUnit`. Thành công → "Kết nối tài khoản thành công." | proxy delivery_connect | - |
| 8 | Lấy URL login OAuth đối tác | "Đăng nhập đối tác" | POST `api/DeliveryUnitConnect/CRM_GetUrlLoginPartner` | `[SBAuthorize]` | public | ĐVVC dùng OAuth | Proxy POST `delivery_connect /api/unit/GetUrlLogin` → trả URL string để FE redirect login đối tác | proxy delivery_connect | - |
| 9 | Ngắt kết nối tài khoản ĐVVC | "Ngắt kết nối" | POST `api/DeliveryUnitConnect/CRM_DisconnectShippingUnit` | `[SBAuthorize]` | public | đã kết nối | `CRM_DisConnectShippingUnit` (hủy kết nối tài khoản). Thành công → "Hủy kết nối tài khoản thành công." | proxy delivery_connect | - |

## Quan hệ
- **Gọi sang:** `IDeliveryUnitConnectService` → 2 đường: (a) HTTP trực tiếp qua `IClientHelper` tới `{DeliveryConnectServiceSetting.ServiceUrl}/api/unit/*` (SaveUnitConnect / GetUnitConnect / GetStoreShippingUnit / GetUrlLogin); (b) qua `OtherService.IDeliveryConnectService` (CRM_ConnectShippingUnit, CRM_DisConnectShippingUnit, GetDeliveryMethod*, GetInfoGiaoHangByShippingUnitCode). Mọi request đều map UserLogin sang dạng delivery_connect (`MapUserLoginDeliveryConnect`).
- **Dữ liệu (entities):** không thao tác EF entity trực tiếp ở OrderLogistic — toàn bộ persist kết nối nằm ở DB của service `delivery_connect`.
- **Ripple / đồng bộ:** kết quả kết nối (tài khoản giao vận, phương thức giao hàng) được các màn ĐĂNG ĐƠN dùng lại — nhưng việc đăng đơn thực tế KHÔNG ở controller này.

## Luồng xuyên module (cấu hình kết nối ĐVVC)
```
FE chọn ĐVVC + chi nhánh → CRM_ConnectShippingUnit / SaveThongTinKetNoiDonVi
  → OrderLogistic proxy sang delivery_connect (/api/unit/*) kèm CrmCallBackUrl (OrderLogisticServiceUrl + CallBackUrl)
  → delivery_connect lưu tài khoản kết nối + (nếu OAuth) trả URL login (CRM_GetUrlLoginPartner) để FE redirect
  → sau kết nối: GetPhuongThucGiaoHangBy* / GetDanhSachCuaHangShipping nạp option cho màn đăng đơn
[Đăng đơn thực tế] tách riêng: OrderService.CreateOrder → publish RabbitMQ MessageCreateOrder → delivery_connect tạo đơn (xem OrderService.cs)
```

## Lỗi đã biết / lưu ý kỹ thuật
- Controller là **cấu hình kết nối**, KHÔNG đăng đơn. Đừng tìm logic "đăng đơn giao hàng" ở đây — nó ở `OrderLogistic.Service/Generated/DeliveryUnitConnect/OrderService.cs:333` (`CreateOrder` publish RabbitMQ `MessageCreateOrder` sang delivery_connect).
- Tất cả endpoint `[SBAuthorize]` mở (chỉ cần login) — không gắn permission key per-action như nhóm controller GetThongTinDoiTac. Không có `[TienIch]` trên controller này (khác các controller đối tác).
- Toàn bộ side-effect (lưu/ngắt kết nối) nằm ở service riêng `delivery_connect` (URL từ `DeliveryConnectServiceSetting.ServiceUrl`); OrderLogistic chỉ là tầng proxy + map UserLogin + gắn CallBackUrl.
- `CRM_ConnectShippingUnit` set `ShippingUnitCode.ToUpper()` 2 lần (dòng lặp vô hại) — `DeliveryUnitConnectService.cs:237-238`.
