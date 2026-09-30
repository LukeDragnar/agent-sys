# EcommerceConnect — Architecture

## Vai trò trong hệ thống

```
[Shopee / TikTok]
       │  OAuth callback
       ▼
EcommerceConnect.API (CrmAuthorizationController)
  → Lưu token vào UnitConnect

[Shopee webhook]──▶ EcommerceReceive.API/Shopee/PushMechanism/Receive
[TikTok webhook]──▶ EcommerceReceive.API/Tiktok/PushNotification/Receive
                         │ publish: EcommerceConnect_DataReceivePartner
                         ▼
                   DataEcomReceiveConsumer
                         │ route theo EcommerceCode
                         ├──▶ IPushMechanismService  (Shopee)
                         └──▶ IPushNotificationService (TikTok)
                                   │ publish: EcommerceConnect_CreateOrderCrm
                                   ▼
                   CreateOrderCrmReceiveConsumer
                         │ ICrmOrderService.SaveOrderToCrm()
                         ▼
                   [CRM (SCRM)]

Product sync:
  ProductEcommReceiveConsumer ──▶ ICrmProductConnectService.SaveProductConnect()
  CreateDraftProductReceiveConsumer ──▶ ICrmCustomProductService.SaveCreateDraftProduct()
  UploadDraftProductEcommReceiveConsumer ──▶ ICrmCustomProductService.SaveSendProductDraftToEcomm()

Order to delivery:
  PostOrderConsumer ──▶ ICrmOrderService.ReadyToShip()
  EcommerceConnect_CreateOrder ──▶ delivery_connect (DeliveryConnect)
```

## Cấu trúc Solution

```
ecommerce_connect/
├── EcommerceConnect.API/
│   ├── Controllers/
│   │   ├── Base/BaseController.cs
│   │   ├── Crm/
│   │   │   ├── CrmAuthorizationController.cs
│   │   │   ├── CrmOrderController.cs
│   │   │   ├── CrmProductConnectController.cs
│   │   │   ├── CrmUnitConnectController.cs
│   │   │   ├── CrmWarehouseConnectController.cs
│   │   │   ├── CrmCustomProductController.cs
│   │   │   ├── CrmLogProcessController.cs
│   │   │   ├── CrmResendDataEcomController.cs
│   │   │   └── CrmCampaignAdsController.cs
│   │   ├── Shopee/
│   │   │   ├── ShopeeAuthorizationController.cs
│   │   │   ├── ShopeeOrderController.cs
│   │   │   ├── ShopeeProductController.cs
│   │   │   ├── ShopeeLogisticsController.cs
│   │   │   ├── ShopeePushMechanismController.cs
│   │   │   ├── ShopeePaymentController.cs
│   │   │   └── ShopeeShopCategoryController.cs
│   │   ├── TikTok/
│   │   │   ├── TiktokAuthorizationController.cs
│   │   │   ├── TiktokOrderController.cs
│   │   │   ├── TiktokProductController.cs
│   │   │   ├── TiktokPushNotificationController.cs
│   │   │   └── TiktokShopController.cs
│   │   ├── MySystem/
│   │   │   ├── ConfigSystemController.cs
│   │   │   ├── ConfigEcommerceController.cs
│   │   │   └── ConfigCategoryController.cs
│   │   └── LiveController.cs
│   ├── Consumer/
│   │   ├── CreateOrderConsumer.cs           (NotImplemented)
│   │   ├── CancelOrderConsumer.cs           (NotImplemented)
│   │   ├── CreateOrderCrmReceiveConsumer.cs
│   │   ├── DataEcomReceiveConsumer.cs
│   │   ├── ProductEcommReceiveConsumer.cs
│   │   ├── CreateDraftProductReceiveConsumer.cs
│   │   ├── UploadDraftProductEcommReceiveConsumer.cs
│   │   └── PostOrderConsumer.cs
│   ├── Extensions/
│   │   ├── MassTransitExtensions.cs
│   │   ├── JobExtention.cs
│   │   └── ServicesExtention.cs
│   └── Program.cs
│
├── EcommerceReceive.API/             (webhook receiver riêng)
│   ├── Controllers/
│   │   ├── Shopee/PushMechanismController.cs
│   │   ├── TikTok/PushNotificationController.cs
│   │   └── LiveController.cs
│   └── Program.cs
│
├── EcommerceConnect.DataContext/
│   ├── Generated/DbContexts/DeliveryConnectDataContext.cs
│   ├── Entity/
│   └── Mapper/
└── EcommerceConnect.Service/
    └── Generated/
```

## RabbitMQ — 3 Bus riêng biệt

| Queue | Bus | Direction | Mô tả |
|-------|-----|-----------|-------|
| EcommerceConnect_DataReceivePartner | BusEcommerce | Consume ← EcommerceReceive | Webhook data từ sàn |
| EcommerceConnect_ProductReceiveEcomm | BusEcommerce | Consume | Data sản phẩm từ sàn |
| EcommerceConnect_CreateDraftProduct | BusEcommerce | Consume | Tạo sản phẩm nháp |
| EcommerceConnect_UploadDraftProductEcomm | BusEcommerce | Consume | Đăng sản phẩm nháp |
| EcommerceConnect_CreateOrderCrm | BusCrm | Consume | Tạo đơn về CRM |
| PostOrderRabbitModel | BusDonHang | Consume | Đơn sẵn sàng giao |
| MessageCreateOrder | BusDonHang | Consume (NotImpl) | Tạo đơn vận chuyển |
| MessageCancelOrder | BusDonHang | Consume (NotImpl) | Hủy đơn vận chuyển |
| LogEcommerceMessage | BusEcommerce | Publish ← EcommerceReceive | Log webhook |

## appsettings.json Keys

```json
{
  "ServiceName": "EcommerceConnect.API",
  "MyDomain": { "MyDomainUrl": "https://..." },
  "ConnectionStrings": {
    "EcommerceConnectConnectStr": "server=...;database=EcommerceConnect;..."
  },
  "Redis": {
    "UrlConnect": "...:6379",
    "Pass": "...",
    "InstanceName": "EcommerceConnect_Development",
    "Database": 10
  },
  "BusDonHang": { "RabbitMq": { "Host": [...], "User": "...", "Password": "..." } },
  "BusEcommerce": { "RabbitMq": { "Host": [...], "User": "...", "Password": "..." } },
  "BusCrm": { "RabbitMq": { "Host": "...", "User": "...", "Password": "..." } },
  "FilteServiceSetting": { "FilteServiceUrl": "http://...:50003" },
  "WarehouseProductServiceUrl": "http://...:50005"
}
```

> Production: BusDonHang có 3 RabbitMQ nodes, BusEcommerce có 2 nodes.

## Redis Cache

- `CacheCommon.KeyCache_CancelLogProcessById(Guid)` — check duplicate processing trong consumers
- `CacheCommon.GetCacheByKey<T>(string)` — lấy cached values
- Khởi tạo tại startup: `CacheCommon.SetRedisCacheService()`

## Luồng chính

### 1. Nhận webhook từ sàn
```
Sàn POST → EcommerceReceive.API
→ publish EcommerceConnect_DataReceivePartner
→ DataEcomReceiveConsumer → route theo EcommerceCode
→ PushMechanismService/PushNotificationService
→ SavePushMechanism() / SavePushNotification()
→ publish EcommerceConnect_CreateOrderCrm
→ CreateOrderCrmReceiveConsumer
→ ICrmOrderService.SaveOrderToCrm()
→ HTTP POST đến CRM callback URL
```

### 2. Đồng bộ sản phẩm
```
CRM → SaveProductQuantity endpoint
→ ProductQuantity DB
→ Quartz Job → sync lên sàn qua IShopeeProductService / ITiktokProductService
```

### 3. Tạo sản phẩm nháp
```
CRM → CreateProductDraft endpoint
→ publish EcommerceConnect_CreateDraftProduct
→ CreateDraftProductReceiveConsumer
→ ICrmCustomProductService.SaveCreateDraftProduct()
→ publish EcommerceConnect_UploadDraftProductEcomm
→ UploadDraftProductEcommReceiveConsumer
→ ICrmCustomProductService.SaveSendProductDraftToEcomm()
→ POST lên Shopee/TikTok API
```
