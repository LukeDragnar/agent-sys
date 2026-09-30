# DeliveryConnect — Domain & Entities

## DB: DeliveryConnectDataContext

### UnitConnect — Cấu hình kết nối đơn vị vận chuyển
| Field | Mô tả |
|-------|-------|
| ServerCode, ShippingUnitCode | Mã đơn vị vận chuyển (GHN, GHTK, VTP...) |
| CrmUnitId/Name, CrmUnitChildId/Name | Đơn vị CRM |
| CrmCallBackUrl | URL callback về CRM |
| PartnerWarehouseId/Code/Name | Kho đối tác |
| PartnerUserCode/Name, PartnerPassword | Tài khoản đối tác |
| PartnerPhone, PartnerAddress | Địa chỉ lấy hàng |
| PartnerToken | Token xác thực (VARCHAR 1500) |
| PartnerPickUpMethod/Time | Phương thức / thời gian lấy hàng |
| PartnerServiceCode | Mã dịch vụ vận chuyển |
| PartnerViewOption | Tùy chọn xem hàng |
| PartnerUseInsurance | Bảo hiểm hàng hóa |
| PartnerIsSignPart | Giao một phần |
| ShippingUnitCodeCustom | Mã đơn vị tùy chỉnh |
| PropName1-5, PropValue1-5 | Custom properties |

### OrderStatus — Trạng thái đơn vận chuyển
| Field | Mô tả |
|-------|-------|
| ServerCode, ShippingUnitCode | |
| CrmUnitId/Name, CrmUnitChildId/Name | |
| CrmCallBackUrl | URL callback CRM |
| OrderCode, OrderCodeExternal | Mã đơn nội bộ / đối tác |
| OrderStatusCode/Name, OrderStatusDate | Trạng thái |
| Amount | COD |
| ReturnOrderCode, ReturnFee, ReturnCod, ReturnOrderStatusCode | Đơn hoàn |
| ProcessName, ProcessStatus | Tên và trạng thái process (success/error/doing/fail/prepare) |
| Description, Logs | Ghi chú và log xử lý |
| ResendNo | Số lần gửi lại |
| DataCreateOrder | NVARCHAR(MAX) — request tạo đơn gốc |
| PropName1-5, PropValue1-5 | Custom properties |

### OrderStatusExt
| Field | Mô tả |
|-------|-------|
| ShippingUnitCode, OrderCode, OrderCodeExternal | |
| DataCreateOrder | NVARCHAR(MAX) |
| CreatedAtUnixMs | Unix timestamp UTC0 (ms) |

### Address (hệ thống địa chỉ nội bộ)
| Entity | Mô tả |
|--------|-------|
| AddressNational | Quốc gia (Code, Name) |
| Address | Tỉnh/Huyện/Xã — LevelAddress (0=quốc gia, 1=tỉnh, 2=huyện, 3=xã) |
| AddressNationalDelivery | Mapping quốc gia → đối tác |
| AddressDelivery | Mapping địa chỉ hệ thống → đối tác (ShippingUnitCode, MyCode, PartnerCode/Id/Name) |
| SyncAddress | Tracking đồng bộ địa chỉ nội bộ |
| SyncAddressDelivery | Tracking đồng bộ địa chỉ đối tác |

### DM (Danh mục hành chính từ đối tác)
| Entity | Mô tả |
|--------|-------|
| DM_TinhTP | Tỉnh/Thành phố — có code cho từng đối tác (VTP, GHN, VNPOST...) |
| DM_QuanHuyen | Quận/Huyện — hỗ trợ BOXME, SHIP60, HOLASHIP, SHOPEE, NJV |
| DM_PhuongXa | Phường/Xã — hỗ trợ VTP, GHN, VNPOST, EMS, SUPERSHIP, SHIP60, HOLASHIP, SHOPEE, NJV, có Prop1-6 |

### Configuration
| Entity | Mô tả |
|--------|-------|
| ConfigDelivery | Cấu hình đơn vị vận chuyển (Type, ConfigKey, ValueInitial, ValueCurrent) |
| ConfigDeliveryCustom | Cấu hình tùy chỉnh (cùng cấu trúc) |
| ConfigSystem | Cấu hình hệ thống |
| DeliveryProperty | Thuộc tính vận chuyển (ShippingUnitCode, PropertyCode, ValueCode/Text) |
| LogData | Log hệ thống (Type, ProcessName, DataInfo, DataError) |

## Endpoints

### CrmOrderController
| Endpoint | Tham số | Mô tả |
|----------|---------|-------|
| POST /api/Order/CalculateFee | ReqCalculateFee | Tính phí vận chuyển |
| POST /api/Order/CreateOrder | ReqCreateOrder | Tạo đơn vận chuyển |
| POST /api/Order/ViewDetail | OrderDataShort | Xem chi tiết đơn |
| POST /api/Order/CancelOrder | OrderDataShort | Hủy đơn |
| POST /api/Order/UpdateOrder | ReqUpdateOrder | Cập nhật trạng thái |
| POST /api/Order/EditOrder | ReqEditOrder | Chỉnh sửa đơn |
| POST /api/Order/CreateOrderAhamove | ReqModelCreateOrderAhamove | Tạo đơn Ahamove |
| POST /api/Order/CancelOrderAhamove | ReqModelCancelOrderAhamove | Hủy đơn Ahamove |
| POST /api/Order/GetTrackingUrl | ReqGetTrackingUrlDonHang | Lấy URL tracking |
| POST /api/Order/PrintOrders | ReqPrintOrder | In vận đơn |
| POST /api/Order/RegistReceiveStatusFromPS | ReqRegistReceiveStatusFromPS | Đăng ký nhận trạng thái từ PS |

### CrmBaseController
| Endpoint | Mô tả |
|----------|-------|
| GET /GetDeliveryActive | Danh sách vận chuyển đang hoạt động (cache) |
| GET /GetDeliveryMethod | Phương thức vận chuyển (cache) |
| GET /GetNational | Dữ liệu quốc gia (cache) |
| GET /GetAddressByLevel | Địa chỉ theo level (cache) |
| GET /GetDeliveryMethodByShippingUnitCode | Phương thức theo đơn vị vận chuyển |
| POST /GetDeliveryMethodByCrmUnitChildId | Phương thức theo đơn vị CRM |
| POST /GetInfoGiaoHangByShippingUnitCode | Thông tin vận chuyển |

### CrmUnitConnectController (`/api/unit`)
| Endpoint | Mô tả |
|----------|-------|
| POST /GetUnitConnect | Lấy thông tin kết nối |
| POST /SaveUnitConnect | Tạo/cập nhật kết nối |
| POST /GetStoreShippingUnit | Danh sách store của đơn vị vận chuyển |
| POST /ConnectShippingUnit | Kết nối đơn vị vận chuyển |
| POST /DisconnectShippingUnit | Ngắt kết nối |
| POST /DeleteUnitConnect | Xóa kết nối |
| POST /GetUrlLogin | Lấy URL đăng nhập đối tác |

### CrmPartnerAddressController
| Endpoint | Mô tả |
|----------|-------|
| POST /GetListPartnerAddress | Danh sách địa chỉ đối tác |
| POST /AutoSyncFullSystem | Đồng bộ toàn bộ địa chỉ |
| POST /GetPartnerAddressByAddressSystem | Địa chỉ đối tác theo địa chỉ hệ thống |

### AddressMngController
| Endpoint | Mô tả |
|----------|-------|
| GET /GetAllAddressNational | Tất cả quốc gia |
| GET /GetAllAddressByLevel | Địa chỉ theo level |
| POST /SaveAddress | Lưu địa chỉ |
| GET /DeleteAddress | Xóa địa chỉ |
| POST /SaveAddressDelivery | Lưu địa chỉ đối tác |
| GET /GetListShippingUnitCode | Danh sách mã đơn vị vận chuyển |

### ConfigDeliveryController / ConfigDeliveryCustomController
| Endpoint | Mô tả |
|----------|-------|
| GET /GetConfig?pass=&key= | Lấy config |
| POST /SetConfig?pass= | Set config |
| POST /RefreshConfig?pass=&key= | Refresh cache config |

### DeliveryPropertyController
| Endpoint | Mô tả |
|----------|-------|
| GET /GetByShippingUnitCode | Thuộc tính theo đơn vị vận chuyển |
| GET /GetByPropertyCode | Thuộc tính theo mã |
| POST /Save | Lưu thuộc tính |
| GET /Delete, /GetById | Xóa / lấy theo ID |
| GET /GetAllPropertyCode | Tất cả mã thuộc tính |

## Consumers

| Consumer | Message | Gọi service |
|----------|---------|-------------|
| CreateOrderConsumer | MessageCreateOrder { Data: string (JSON ReqCreateOrder) } | ICrmOrderService.CreateOrder() |
| CancelOrderConsumer | MessageCancelOrder { Data: string (JSON OrderDataShort) } | ICrmOrderService.CancelOrder() |

> Config: PrefetchCount=10, Retry=2x3000ms, discard faulted/skipped.

## Services chính

| Interface | Mô tả |
|-----------|-------|
| ICrmOrderService | CRUD đơn vận chuyển — CreateOrder, CancelOrder, UpdateOrder, EditOrder, CalculateFee, PrintOrders, RegistReceiveStatusFromPS |
| ICrmUnitConnectService | Quản lý kết nối đơn vị vận chuyển |
| ICrmBaseService | Base operations (GetDeliveryMethod, GetInfoGiaoHang) |
| IAddressService | Lấy địa chỉ hệ thống (cache) |
| IAddressDeliveryService | Lấy địa chỉ đối tác (cache) |
| IAddressMngService | CRUD địa chỉ |
| IPartnerAddressService | Địa chỉ từ đối tác vận chuyển |
| ISyncAddressService | Đồng bộ địa chỉ từ PushSale |
| IDeliveryPropertyService | CRUD DeliveryProperty |
| IConfigDeliveryService | Config vận chuyển |
| IConfigDeliveryCustomService | Config tùy chỉnh |
| IGhnService, IGhtkService, IVtpService, IJntService, IEmsService | Service từng đối tác |
| ISuperShipService, IBestService, IBoxMeService, IAhaMoveService | Service đối tác |
| INjvService, ISpxService, IVnPostService, ISscService | Service đối tác |
| IShippingUnitCustomService | Đơn vị vận chuyển tùy chỉnh |
| IJobService | ClearPrintOrderFileOneTimeAHour() |
| LogDataService | Ghi log |

## Models quan trọng

### OrderData (đơn hàng đầy đủ)
```csharp
ShippingUnitCode, OrderCode
Sender: { Name, Phone, national, addressCodeLevel1-5, addressNameLevel1-5, AddressDetail }
Receiver: { (tương tự Sender) }
Products: List<ProductData>
TotalAmount, Cod, Fee, Quantity, Price
WeightGram, HeightCm, WidthCm, LengthCm
Insurance, Coupon, Note
IsFreeship, Service, ServicesAdd
PickUpMethod, PickUpTime, DeliverMethod, DeliverNote
ViewOption, IsSignPart, UseInsurance
PropValue1-5
```

### RabbitMQ Messages
```csharp
MessageCreateOrder  { Data: string }  // JSON ReqCreateOrder
MessageCancelOrder  { Data: string }  // JSON OrderDataShort
MessageStatusCreateOrder { Data: object }
MessageStatusCancelOrder { Data: object }
```

## Quartz Jobs

| Job | Lịch | Mô tả |
|-----|------|-------|
| JobClearPrintOrderFile | 1 giờ | Xóa file PDF vận đơn cũ (giữ theo giờ hiện tại yyyyMMddHH) |
| JobDeleteLogData | 12:00 AM hàng ngày | LogDataService.DeleteLogDataAsync() |
| JobRefreshTokenNJV | 30s | INjvService.Job_Njv_RefreshToken() — refresh token Ninja Van |
| JobSyncAddressPs | 1 giờ | ISyncAddressService.DongBoDiaChiPsConnect() — đồng bộ địa chỉ từ PushSale |
| DataReceiveJob | 30s | (Hiện disabled/commented out) |

> Jobs chỉ chạy ở Staging và Production.

## appsettings.json Keys

```json
{
  "ServiceName": "DeliveryConnect.API",
  "ConnectionStrings": {
    "DeliveryConnectConnectStr": "server=...;database=DeliveryConnect;..."
  },
  "Redis": {
    "UrlConnect": "...:6379",
    "Pass": "...",
    "InstanceName": "DeliveryConnect_Development",
    "Database": 10
  },
  "BusDonHang": {
    "RabbitMq": { "Host": [...], "User": "...", "Password": "..." }
  },
  "ServiceDeliveryStatusSettings": {
    "URL_SERVICE_DELIVERY_STATUS": "http://...:50029",
    "SERVICE_RECEIVE_DATA_API_TIMEOUT": 5000,
    "URL_SERVICE_DELIVERY_RECEIVE": "http://...:50028",
    "URL_SERVICE_PUSHSALE_DELIVERY_CONNECT": "http://...:20000"
  }
}
```
