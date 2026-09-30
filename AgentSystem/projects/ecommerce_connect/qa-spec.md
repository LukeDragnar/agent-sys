# EcommerceConnect — QA Spec / Behavior Test Cases

Checklist test luồng nghiệp vụ, edge cases. Mỗi case ghi rõ INPUT → EXPECTED. Bổ sung khi phát hiện bug mới.

## Luồng tạo đơn Shopee → CRM

| Case | Input | Expected |
|---|---|---|
| Đơn mới UNPAID đầu tiên | webhook Shopee `order_status_update`, order chưa có trong OrderData | OrderData được tạo với `CrmOrderId` đầy đủ; InvoiceShopee + PrintOrderShopee record được sinh kèm |
| Đơn đã tồn tại, status update | Webhook lại với status mới | OrderData được `UpdateAsync`, không insert duplicate; `Logs` được append |
| TimeStamp cũ hơn lần xử lý trước | `orderTempShopee.PartnerTimeStamp <= orderDataDb.PartnerTimeStamp` (trừ CANCELLED) | OrderTempShopee được delete, không update OrderData (idempotent) |
| Token Shopee hết hạn | `unitConnect.IsErrorToken = true` | OrderData được tạo với Status `CreateFail`, không gọi Shopee API |
| Tạo đơn CRM lỗi response Nothing | `resSendOrderDataToCrm.Message == "Response Crm is Nothing"` | OrderTempShopee giữ lại, retry sau (không insert OrderData) |
| Tạo đơn CRM thành công nhưng không có `CrmOrderCode` | resp success nhưng `CrmOrderCode` rỗng | OrderData status `CreateFail`, không gọi SP InvoiceShopee |

## Luồng cập nhật hóa đơn Shopee → CRM (`ProcessSendInvoiceToCrm`)

| Case | Input | Expected |
|---|---|---|
| Shopee trả `error: "receipt settings not found"` | invoice_detail null | InvoiceShopee row **DELETED** với message rõ ràng, **không retry** |
| Shopee trả `error: "order not found"` | invoice_detail null | InvoiceShopee row **DELETED**, không retry |
| Shopee có invoice_detail đầy đủ, invoice_type `personal` | name/tax_id/address có giá trị | Build `InvoiceModel` (personal fields), gửi CRM, mark CREATED khi CRM success |
| Shopee có invoice_detail đầy đủ, invoice_type `company` | company_name/company_tax_id có giá trị | Build `InvoiceModel` (company fields), gửi CRM |
| InvoiceShopee.CrmOrderId NULL, OrderData đã có CrmOrderId | record bị stale | Job re-lookup từ OrderData, backfill `CrmOrderId` + `CrmOrderCode`, gửi CRM thành công |
| InvoiceShopee.CrmOrderId NULL, OrderData cũng NULL | đơn chưa sync xong | Job set `Error` + `ResendNo++`, message `"Chưa có CrmOrderId, đợi đơn sync sang CRM xong"`, không gửi CRM rỗng |
| CRM reject update (response Success=false) | có CrmOrderId nhưng CRM fail | Set Error + ResendNo++, retry lần sau |
| Row Error quá 15 ngày từ CreatedDate | `CreatedDate < Now.AddDays(-15)` | Không pickup nữa, zombie record (cần cleanup thủ công) |
| Job lock pickup batch | `InProcess = false` rồi được set `true` | Cùng `PartnerOrderCode` không bị pickup bởi job instance khác |

## Luồng tạo sản phẩm nháp → đăng Shopee

| Case | Input | Expected |
|---|---|---|
| CRM tạo draft đầu tiên | webhook CRM `CreateProductDraft` | ProductDraft tạo với status pending, publish `EcommerceConnect_UploadDraftProductEcomm` |
| Shopee reject category | category_id sai | ProductDraft status `Error`, message lỗi từ Shopee, không retry tự động |
| Upload thành công | Shopee trả `partner_product_id` | ProductDraft populate `PartnerProductId`, status `Success` |

## Luồng đồng bộ tồn kho Shopee

| Case | Input | Expected |
|---|---|---|
| CRM update tồn kho | `SaveProductQuantity` từ CRM | ProductQuantity record tạo, status pending |
| Job sync chạy | ProductQuantity pending | Gọi Shopee `UpdateStock`, mark success/fail |
| Auto on/off đồng bộ | `ProductSyncAutoOn/Off` schedule trigger | UnitConnect `ProductSyncQuantity` toggle theo schedule |

## Luồng xử lý webhook (EcommerceReceive)

| Case | Input | Expected |
|---|---|---|
| Webhook Shopee hợp lệ | POST `/Shopee/PushMechanism/Receive` với Authorization | Publish `EcommerceConnect_DataReceivePartner`, ack 200 |
| Webhook duplicate (cùng `CreatedAtUnixMs` + `PartnerOrderCode`) | Shopee retry lần 2 | Redis cache check → skip processing, ack 200 |
| Webhook TikTok signature sai | Authorization sai | Reject 401 |

## Idempotency / Retry behaviors

| Case | Expected |
|---|---|
| Job chạy 2 lần liên tiếp trên cùng record | Lần 2 không pickup (InProcess = true), không double-send CRM |
| Service restart giữa flow tạo đơn | OrderData hoặc InvoiceShopee row đã insert sẽ retry sau, không lost data |
| RabbitMQ redelivery message | Consumer check Redis cache `CacheCommon.KeyCache_CancelLogProcessById` để skip duplicate |
