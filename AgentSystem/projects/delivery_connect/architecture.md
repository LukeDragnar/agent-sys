# DeliveryConnect — Architecture

## Vai trò trong hệ thống

```
[CRM (SCRM)]
       │ gọi API
       ▼
DeliveryConnect.API
  - Tạo/hủy/cập nhật đơn vận chuyển
  - Tính phí, in vận đơn
  - Quản lý địa chỉ, UnitConnect
  - Đăng ký callback URL (RegistReceiveStatusFromPS)
       │
       ▼
[Đối tác vận chuyển]
  GHN, GHTK, VTP, JNT, EMS, SuperShip, Best,
  BoxMe, AhaMove, NJV (Ninja Van), SPX, VnPost, SSC
       │ webhook callback trạng thái
       ▼
delivery_receive → delivery_status → CRM

Consume từ ecommerce_connect:
  ◄── MessageCreateOrder (BusDonHang)
  ◄── MessageCancelOrder (BusDonHang)

Đồng bộ địa chỉ:
  JobSyncAddressPs (1h) → ISyncAddressService.DongBoDiaChiPsConnect()
  → URL_SERVICE_PUSHSALE_DELIVERY_CONNECT
```

## Cấu trúc Solution

```
delivery_connect/
├── DeliveryConnect.API/
│   ├── Controllers/
│   │   ├── Base/BaseController.cs
│   │   ├── CRM/
│   │   │   ├── CrmOrderController.cs
│   │   │   ├── CrmBaseController.cs
│   │   │   ├── CrmAddressController.cs
│   │   │   ├── CrmPartnerAddressController.cs  (CrmAddressDeliveryController)
│   │   │   └── CrmUnitConnectController.cs
│   │   ├── MySystem/
│   │   │   ├── AddressMngController.cs
│   │   │   ├── ConfigDeliveryController.cs
│   │   │   ├── ConfigDeliveryCustomController.cs
│   │   │   ├── ConfigSystemController.cs       (disabled)
│   │   │   └── DeliveryPropertyController.cs
│   │   └── LiveController.cs
│   ├── Consumer/
│   │   ├── CreateOrderConsumer.cs
│   │   └── CancelOrderConsumer.cs
│   ├── Service/Generated/Schedule/
│   │   ├── DataReceiveJob.cs               (disabled)
│   │   ├── JobClearPrintOrderFile.cs
│   │   ├── JobDeleteLogData.cs
│   │   ├── JobRefreshTokenNJV.cs
│   │   └── JobSyncAddressPs.cs
│   ├── Extensions/
│   │   ├── MassTransitExtensions.cs
│   │   ├── JobExtention.cs
│   │   └── ServicesExtention.cs
│   └── Program.cs
├── DeliveryConnect.DataContext/
│   ├── Generated/DbContexts/DeliveryConnectDataContext.cs
│   ├── Entity/
│   │   ├── Address.cs, AddressDelivery.cs
│   │   ├── AddressNational.cs, AddressNationalDelivery.cs
│   │   ├── DM_TinhTP.cs, DM_QuanHuyen.cs, DM_PhuongXa.cs
│   │   ├── SyncAddress.cs, SyncAddressDelivery.cs
│   │   ├── UnitConnect.cs
│   │   ├── OrderStatus.cs, OrderStatusExt.cs
│   │   ├── ConfigDelivery.cs, ConfigDeliveryCustom.cs, ConfigSystem.cs
│   │   ├── DeliveryProperty.cs
│   │   └── LogData.cs
│   └── Model/
│       ├── Crm/ (CrmOrderModel, CrmAddress, CrmBaseModel, CrmUnitConnectModel, CrmRabbitModel)
│       └── Common/
└── DeliveryConnect.Service/
    └── Generated/
        ├── Schedule/ (Jobs)
        └── Services/ (Implementations)
```

## RabbitMQ

| Queue | Direction | Message | Mô tả |
|-------|-----------|---------|-------|
| DeliveryConnect_CreateOrder (BusDonHang) | Consume ← ecommerce_connect | MessageCreateOrder { Data: string } | Tạo đơn vận chuyển |
| DeliveryConnect_CancelOrder (BusDonHang) | Consume ← ecommerce_connect | MessageCancelOrder { Data: string } | Hủy đơn |

## Đối tác vận chuyển được hỗ trợ

| Mã | Tên | Service |
|----|-----|---------|
| GHN | Giao Hàng Nhanh | IGhnService |
| GHTK | Giao Hàng Tiết Kiệm | IGhtkService |
| VTP | Viettel Post | IVtpService |
| JNT | J&T Express | IJntService |
| EMS | EMS | IEmsService |
| SUPERSHIP | SuperShip | ISuperShipService |
| BEST | Best Express | IBestService |
| BOXME | BoxMe | IBoxMeService |
| AHAMOVE | AhaMove | IAhaMoveService |
| NJV | Ninja Van | INjvService |
| SPX | SPX Express | ISpxService |
| VNP | Vietnam Post | IVnPostService |
| SSC | Custom | ISscService |
| CUSTOM | Tùy chỉnh | IShippingUnitCustomService |

## External Service URLs (appsettings)

```
URL_SERVICE_DELIVERY_STATUS   → delivery_status service
URL_SERVICE_DELIVERY_RECEIVE  → delivery_receive service
URL_SERVICE_PUSHSALE_DELIVERY_CONNECT → PushSale CRM
```
