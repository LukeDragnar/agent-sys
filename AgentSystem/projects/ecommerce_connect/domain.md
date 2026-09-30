# EcommerceConnect — Domain & Entities

## DB: EcommerceConnectDataContext

### UnitConnect — Kết nối shop sàn TMĐT
| Field | Mô tả |
|-------|-------|
| ServerCode, EcommerceCode | SHOPEE / TIKTOK |
| ConnectName | Tên kết nối |
| CrmUnitId/Name, CrmUnitChildId/Name | Đơn vị CRM |
| PartnerUnitId/Name/Logo | Thông tin shop đối tác |
| PartnerToken, PartnerTokenExpireDate | Access token |
| PartnerRefreshToken, PartnerRefreshTokenExpireDate | Refresh token |
| IsTokenRefreshing, IsErrorToken | Trạng thái token |
| CrmCallBackUrl, CrmProductUrl | URL callback CRM |
| CrmOrderSourceId, CrmOrderWarehouseId/Name | Mapping kho đơn hàng |
| OrderSyncOptionToCrm, OrderSyncOptionProductSync | Tùy chọn đồng bộ |
| ProductSyncQuantity, ProductSyncPrice, ProductSyncStatus | Đồng bộ sản phẩm |
| ProductSyncAutoOn/Off + schedule fields | Tự động bật/tắt đồng bộ |
| CrmProductSyncUnitChildId/Name, CrmProductSyncWarehouseId/Name | Kho đồng bộ sản phẩm |
| ProductSyncOutOfStockWhenQuantityCrmBelow | Ngưỡng hết hàng |
| ProductSyncRate | Tỷ lệ đồng bộ giá |
| DocTypes, DocSizes | Loại và kích thước vận đơn |
| PropName1-5, PropValue1-5 | Custom properties |

### WarehouseConnect — Mapping kho
| Field | Mô tả |
|-------|-------|
| UnitConnectId | FK sang UnitConnect |
| PartnerUnitId, PartnerWarehouseId/Name, PartnerLocationId | Kho đối tác |
| PartnerDataFull | Raw data JSON |
| CrmOrderWarehouseId/Name | Kho xử lý đơn CRM |
| CrmProductSyncWarehouseId/Name | Kho đồng bộ sản phẩm |
| IsDefault | Kho mặc định |
| ProductSyncOutOfStockWhenQuantityCrmBelow, ProductSyncRate | Cấu hình đồng bộ |

### ProductConnect — Mapping sản phẩm
| Field | Mô tả |
|-------|-------|
| UnitConnectId, EcommerceCode | |
| CrmProductId/Code/Name, CrmSkuId/Code/Name | Sản phẩm CRM |
| CrmAttributeId/Name/Value (3 bộ) | Thuộc tính CRM |
| PartnerProductId/Code/Name, PartnerSkuId/Code/Name | Sản phẩm sàn |
| PartnerAttributeId/Name/Value (3 bộ) | Thuộc tính sàn |
| PartnerProductImage | Ảnh (NVARCHAR MAX) |
| WeightGram, HeightCm, LengthCm, WidthCm | Kích thước |
| SyncQuantity, Quantity, SyncPrice, Price | Đồng bộ tồn kho/giá |
| IsCombo, SyncWait, SyncSuccess, InProcess, Status | Trạng thái |

### OrderData — Đơn hàng từ sàn
| Field | Mô tả |
|-------|-------|
| UnitConnectId, EcommerceCode | |
| CrmUnitId/ChildId, CrmWarehouseId | Đơn vị CRM |
| CrmOrderId/Code, CrmOrderDetail | Đơn CRM (detail: JSON) |
| PartnerUnitId/WarehouseId, PartnerOrderId/Code | Đơn sàn |
| PartnerOrderDetail, PartnerOrderPayment | Raw data JSON |
| MyOrderStatusId/Code/Name/Date | Trạng thái nội bộ |
| PartnerOrderStatusCode/Name, PartnerTimeStamp | Trạng thái sàn |
| CrmCallBackUrl | URL callback |
| OrderCode3Pl | Mã vận đơn |
| Amount, Status, Description, Logs | |
| PropName1-5, PropValue1-5 | Custom (NVARCHAR MAX) |

### ProductQuantity — Tồn kho đồng bộ
| Field | Mô tả |
|-------|-------|
| CrmUnitId/Name, CrmUnitChildId/Name | Đơn vị CRM |
| CrmWarehouseId/Name | Kho |
| IsCombo, CrmProductId/Code/Name, CrmSkuId/Code/Name | Sản phẩm |
| Quantity, Status, Description | |
| InProcess, ResendNo | Trạng thái xử lý |

### DataReceive — Data webhook từ sàn
| Field | Mô tả |
|-------|-------|
| EcommerceCode, PartnerUnitId | |
| PartnerDataType, PartnerData | Loại và raw data |
| TimeStamp, Status, Description | |
| ResendNo, InProcess | |
| PropName1-3, PropValue1-3 | |
| CreatedAtUnixMs | Unix timestamp (ms) |

### ProductDraft — Sản phẩm nháp
| Field | Mô tả |
|-------|-------|
| UnitConnectId, EcommerceCode | |
| CrmProductId, ProductName/Code | |
| CategoryId/Name | Danh mục sàn |
| ListAttributes, ListChannels | JSON |
| Description | NVARCHAR(3000) |
| AttributeName/Value (1-3) | Thuộc tính |
| Price, Stock | |
| WeightGram, HeightCm, LengthCm, WidthCm | |
| IsPreOrder, IsNew, IsOrderLogistic | |
| PartnerProductId, PartnerImageId/Url | Sau khi đăng lên sàn |
| Status, MessageError | |

### Các entity khác
| Entity | Mô tả |
|--------|-------|
| ProductDraftChildren | SKU con của ProductDraft |
| ProductCategory | Danh mục sản phẩm sàn |
| ProductCrmProperty | Thuộc tính sản phẩm CRM |
| ProductCrmChildren | Variant sản phẩm CRM |
| OrderTempShopee / OrderTempTiktok | Đơn tạm thời từ sàn |
| PaymentTransactionShopee/Tiktok | Giao dịch thanh toán |
| PrintOrderShopee/Tiktok | Lịch sử in vận đơn |
| InvoiceShopee | Hóa đơn Shopee — populate qua SP `PRC_EcommerceConnect_InvoiceShopee_InsertByOrderDataId(@OrderDataId)` copy field từ `OrderData`. **Caller phải `InsertAsync(OrderData)` trước khi gọi SP**, nếu không child rows sẽ có `CrmOrderId/CrmOrderCode` NULL. |
| AdsUnitConnect | Kết nối tài khoản quảng cáo |
| AdsCampaign | Chiến dịch quảng cáo |
| AdsCampaignPerformance | Hiệu suất chiến dịch |
| LogData | Log hệ thống (Type, ProcessName, DataInfo, DataError) |
| LogProcess | Tracking tiến trình (Total, TotalFail, TotalSuccess, Status) |
| LogProcessDetail | Chi tiết từng tiến trình |
| ConfigSystem | Cấu hình hệ thống |
| ConfigEcommerce | Cấu hình từng sàn |

## Endpoints — EcommerceConnect.API

### CrmAuthorizationController (`/api/crm/auth`)
| Endpoint | Mô tả |
|----------|-------|
| POST GetAuthorizationUrl | Tạo URL kết nối sàn mới |
| POST SaveAuthorization | Lưu token sau khi xác thực |
| POST GetAdsAuthorizationUrl | URL kết nối tài khoản quảng cáo |
| POST SaveAdsAuthorization | Lưu token quảng cáo |

### CrmOrderController
| Endpoint | Mô tả |
|----------|-------|
| POST PostOrder | Thông báo đơn sẵn sàng giao |
| POST GetShippingDocument | Lấy vận đơn |
| POST PrintOrder | In vận đơn |
| POST GetOrderCreateError | Lỗi tạo đơn |
| POST ReCreateOrderToCrm | Tạo lại đơn lỗi (V2) |
| POST ReCreateOrderToCrm_Old | Legacy |
| POST DeleteOrderSyncError | Xóa lỗi sync |
| POST GetOrderFromEcomm | Lấy đơn từ sàn |
| POST GetDocTypes | Dropdown loại chứng từ |
| POST GetDocSizes | Dropdown kích thước |

### CrmProductConnectController (`/api/crm/product`)
| Endpoint | Mô tả |
|----------|-------|
| POST GetListProductConnectV2 | Danh sách sản phẩm đã kết nối |
| POST GetListProductStatus | Trạng thái kết nối sản phẩm |
| POST GetListProductEcomm | Sản phẩm trên sàn |
| POST DeleteProductConnect | Hủy kết nối sản phẩm |
| POST ConnectProductAuto | Tự động kết nối sản phẩm |
| POST SyncProductEcomm | Đồng bộ sản phẩm lên sàn |
| POST ConnectProduct | Kết nối thủ công |
| POST DisconnectProduct | Hủy kết nối |
| POST SaveProductQuantity | Cập nhật tồn kho |

### CrmUnitConnectController (`/api/crm/unit`)
| Endpoint | Mô tả |
|----------|-------|
| POST GetListUnitConnect | Danh sách kết nối (phân trang) |
| POST GetAllListUnitConnect | Tất cả kết nối |
| POST GetUnitConnect | Chi tiết một kết nối |
| POST SaveUnitConnect | Tạo/cập nhật kết nối |
| POST DeleteUnitConnect | Xóa kết nối |

### CrmWarehouseConnectController (`/api/crm/warehouse`)
| Endpoint | Mô tả |
|----------|-------|
| POST GetListWarehouseConnect | Danh sách mapping kho |
| POST SaveWarehouseConnect | Tạo/cập nhật mapping |
| POST DeleteWarehouseConnect | Xóa mapping |
| POST GetListWarehouseEcomm | Sync kho từ sàn về |

### CrmCustomProductController (`/api/crm/customProduct`)
| Endpoint | Mô tả |
|----------|-------|
| POST GetListCategory | Danh mục sản phẩm |
| POST GetAttributes | Thuộc tính sản phẩm |
| POST SaveCrmProduct | Lưu sản phẩm CRM |
| POST CreateProductDraft | Tạo sản phẩm nháp (V2) |
| POST GetListProductDraft | Danh sách sản phẩm nháp |
| POST SendProductDraftToEcomm | Đăng sản phẩm nháp lên sàn (V2) |
| POST UpdateListProductDraft | Cập nhật sản phẩm nháp |

### CrmLogProcessController (`/api/crm/logProcess`)
| Endpoint | Mô tả |
|----------|-------|
| POST GetListLogProcess | Danh sách log tiến trình |
| POST GetLogProcessDetail | Chi tiết log |
| POST CancelLogProcess | Hủy tiến trình |

### Shopee Controllers
| Controller | Endpoints |
|-----------|-----------|
| ShopeeAuthorizationController | GET ResultAuth (callback OAuth), GET ResultAdsAuth |
| ShopeeOrderController | GET GetOrderList, GetShipmentList, GetOrderDetail |
| ShopeeProductController | GET GetCategory, GetAttributes, POST AddItem, GET GetItemBaseInfo |

### TikTok Controllers
| Controller | Endpoints |
|-----------|-----------|
| TiktokAuthorizationController | Authorization flow |
| TiktokOrderController | POST GetOrderList, GET GetOrderDetail, GET ProcessSendOrderToPs |

## Endpoints — EcommerceReceive.API

| Controller | Endpoint | Mô tả |
|-----------|----------|-------|
| Shopee/PushMechanismController | POST /Shopee/PushMechanism/Receive | Webhook từ Shopee |
| TikTok/PushNotificationController | POST /Tiktok/PushNotification/Receive | Webhook từ TikTok |

Cả hai: đọc Authorization header, publish `LogEcommerceMessage` + `EcommerceConnect_DataReceivePartner`.

## Consumers

| Consumer | Message | Gọi service |
|----------|---------|-------------|
| CreateOrderCrmReceiveConsumer | EcommerceConnect_CreateOrderCrm | ICrmOrderService.SaveOrderToCrm() |
| DataEcomReceiveConsumer | EcommerceConnect_DataReceivePartner | IPushMechanismService (Shopee) / IPushNotificationService (TikTok) |
| ProductEcommReceiveConsumer | EcommerceConnect_ProductReceiveEcomm | ICrmProductConnectService.SaveProductConnect() |
| CreateDraftProductReceiveConsumer | EcommerceConnect_CreateDraftProduct | ICrmCustomProductService.SaveCreateDraftProduct() |
| UploadDraftProductEcommReceiveConsumer | EcommerceConnect_UploadDraftProductEcomm | ICrmCustomProductService.SaveSendProductDraftToEcomm() |
| PostOrderConsumer | PostOrderRabbitModel | ICrmOrderService.ReadyToShip() |
| CreateOrderConsumer | MessageCreateOrder | (NotImplementedException) |
| CancelOrderConsumer | MessageCancelOrder | (NotImplementedException) |

> Các consumer có Redis cache check để tránh xử lý trùng (`CacheCommon.KeyCache_CancelLogProcessById`).

## RabbitMQ — 3 Bus riêng biệt

| Bus | Dùng cho |
|-----|---------|
| BusDonHang | Đơn hàng (CreateOrder, CancelOrder, PostOrder) |
| BusEcommerce | Dữ liệu từ sàn (DataReceivePartner, ProductReceive, Draft) |
| BusCrm | Giao tiếp với CRM (CreateOrderCrm) |

## Services chính

| Interface | Mô tả |
|-----------|-------|
| ICrmOrderService | Xử lý đơn hàng (SaveOrderToCrm, ReadyToShip) |
| ICrmProductConnectService | Mapping sản phẩm |
| ICrmCustomProductService | Sản phẩm nháp |
| ICrmUnitConnectService | Kết nối shop |
| ICrmWarehouseConnectService | Mapping kho |
| ICrmLogProcessService | Log tiến trình |
| ICrmCampaignAdsService | Quảng cáo |
| IPushMechanismService | Xử lý webhook Shopee |
| IPushNotificationService | Xử lý webhook TikTok |
| IShopeeOrderService | API Shopee — đơn hàng |
| IShopeeProductService | API Shopee — sản phẩm |
| ITiktokOrderService | API TikTok — đơn hàng |
| LogDataService | Ghi log |
