# Dev — Tự động nhận & đồng bộ đơn từ sàn TMĐT

> Tính năng **nền (Pass B — không có menu riêng)**. Trải 2 repo:
> - **SCRM (OrderLogistic service):** `D:\PROJECT\Git\_SourceForCode\Group2\scrm` — cửa NHẬN webhook.
> - **ecommerce_connect:** `D:\PROJECT\Git\_SourceForCode\ecommerce_connect` — cầu nối sàn ↔ SCRM, bên GỬI webhook.
>
> Hướng dữ liệu: `[sàn] → EcommerceReceive.API → (RabbitMQ) → ecommerce_connect → POST webhook → SCRM OrderLogistic`.

## Màn hình & route (FE — phần thao tác tay)
| Màn hình | Vai trò với feature này |
|---|---|
| Đơn hàng TMĐT / Đơn hàng logistic | Nơi đơn về hiển thị; nút "Lấy đơn từ sàn", "In đơn", "Đăng đơn", tab "Đơn lỗi đồng bộ" |
| Đơn hàng hoàn | Đơn hoàn về; nút "Lấy đơn hoàn mới", "Đồng bộ trạng thái" |

> Bản thân luồng tự động **không có UI** — chạy webhook BE-to-BE. UI chỉ là các nút "kéo bù".

## Endpoint / Controller (trace registry)
**SCRM — cửa nhận (registry: `kho/DonHangController_OrderLogistic.md`, `kho/DonHangHoanEcommController.md`):**

| Endpoint | DataType nhận | Controller | Service route |
|---|---|---|---|
| POST `api/DonHang/NhanDuLieuDonHangTMDT` | CREATE / UPDATE / UPDATEPAYMENT / UPDATEINVOICE | `DonHangController` (Ecommerce) | `DonHangService.NhanDuLieuDonHangTMDT` |
| POST `api/DonHangHoanEcomm/NhanDuLieuDonHangHoanTMDT` | RETURN_CREATE / RETURN_UPDATE / RETURN_UPDATE_DETAIL | `DonHangHoanEcommController` | `DonHangHoanService.NhanDuLieuDonHangHoanTMDT` |

- Cả 2 webhook: `[SBAuthorize]` **bị comment** → anonymous, tin tưởng mạng nội bộ (theo convention `ecommerce_connect/convention/internal-webhook-no-auth.md`).
- Nút thao tác tay (`CRM_GetOrderFromEcomm`, `CRM_GetOrderPrintFromEcomm`, `PostOrder`, `CRM_GetOrderInvoice`, `CRM_GetOrderCreateError`, `CRM_ReCreateOrderToCrm`, `CRM_DeleteOrderSyncError`) đều `[SBAuthorize]` mở (chỉ cần login), gate qua tiện ích `TI_OrderLogistic`. Đơn hoàn: `LayDonHoanMoiTuEcomm`, `DongBoTrangThaiDonHoan` (registry `kho/DonHangHoanController.md`).

**ecommerce_connect — bên gửi:**
- Webhook vào sàn: `EcommerceReceive.API` `POST /Shopee/PushMechanism/Receive`, `POST /Tiktok/PushNotification/Receive` → publish `EcommerceConnect_DataReceivePartner` + `LogEcommerceMessage`.
- Đẩy đơn về CRM: `CrmOrderController` (PostOrder, GetOrderFromEcomm, ReCreateOrderToCrm, GetOrderCreateError, DeleteOrderSyncError, GetOrderInvoice) — proxy mà SCRM gọi sang.

## Entities
- **SCRM:** `DonHangLogistic` (header: `MaDon`, `MaDonEcomm`, `MaDonGiaoVan`, `EcommerceId`, `IdDonVi`, `IdChiNhanh`, `UnitCode=Ecommerce`, `DonHangIsInDon`), `DonHangLogisticSanPham` / `DonHangLogisticCombo` / `DonHangLogisticChiTietTMDT` / `DonHangThongTinThue`; `DonHangHoan` / `DonHangHoanSanPham`.
- **ecommerce_connect:** `UnitConnect` (token + `CrmUnitId/ChildId` + `CrmCallBackUrl`), `WarehouseConnect`, `ProductConnect`, `OrderData` (`Status`: CreateFail/Created…, `CrmOrderId/Code`, `PartnerOrderCode`), `DataReceive`, `ReturnTempShopee` + tbl OrderReturnData (qua SP), `LogProcess`/`LogProcessDetail`/`LogData`.

## Luồng code
**1. Sàn → ecommerce_connect (nhận webhook + dựng đơn):**
`EcommerceReceive.../PushMechanismController.Receive` (publish) → `DataEcomReceiveConsumer.Consume` route theo `EcommerceCode` → `IPushMechanismService.SavePushMechanism` (Shopee) / `IPushNotificationService.SavePushNotification` (TikTok) → build `OrderData` → POST `NhanDuLieuDonHangTMDT` sang SCRM (DataType CREATE/UPDATE/UPDATEPAYMENT).

**2. SCRM nhận đơn bán** (`DonHangService.NhanDuLieuDonHangTMDT`):
- Validate `reqData` null → "Dữ liệu không hợp lệ."
- Lấy `CrmUnitId/CrmUnitChildId` (fallback từ khối con theo DataType); DataType lạ → "Loại hành động không hợp lệ."
- `coreService.DonVi_GetDonViById` null → "Đơn vị không tồn tại."
- Route: CREATE → `TaoDonHangEcommerce`; UPDATE → `CapNhatDonHangEcommerce`; UPDATEPAYMENT → `CapNhatGiaoDichDonHangEcommerce`; UPDATEINVOICE → `CapNhatHoaDonDonHangEcommerce`.
- `TaoDonHangEcommerce`: dedup qua SP `PRC_SCRM_OrderLogistic_DonHangLogistic_GetByMaDonEcomm` (IdDonVi + IdChiNhanh + MaDonEcomm). Có → trả đơn cũ. Chưa → `ValidateDon` → build `DonHangLogistic` (MaDon = MaDonEcomm = PartnerOrderCode) + line/combo/thuế → ripple kho/KT/ES/notification.

**3. Đơn hoàn** (`DonHangHoanService.NhanDuLieuDonHangHoanTMDT`): route theo `DataType` (RETURN_CREATE insert + items / RETURN_UPDATE chỉ status / RETURN_UPDATE_DETAIL status + replace items). Bên gửi (`OrderReturnShopeeService`/`OrderReturnTiktokService`) quyết DataType: `!isCreatedInCrm`→CREATE, `hasStatusUpdate`→UPDATE_DETAIL, còn lại→UPDATE; gửi qua `SendOrderReturnDataToCrmAsync` (RestSharp `ApiPost` tới `{baseCrmUrl}/api/DonHangHoanEcomm/NhanDuLieuDonHangHoanTMDT`).

## Bug-prone / lưu ý
- **Webhook anonymous (cả 2):** `DonHangController.cs:34` và `DonHangHoanEcommController.cs:29` comment `[SBAuthorize]`. Mở hoàn toàn ở app-level, chỉ dựa mạng nội bộ. Hardening tương lai: header `SystemAuthorization` (service token). (Có session security riêng — KHÔNG sửa ở đây.)
- **Dedup CREATE chỉ theo `MaDonEcomm` + IdDonVi + IdChiNhanh** (qua SP). Nếu PartnerOrderCode rỗng/khác nhau giữa 2 lần gửi → có thể lọt 2 đơn. Idempotency phụ thuộc SP, không có unique constraint thấy ở tầng code.
- **DataType lạ → "Loại hành động không hợp lệ."** chỉ khi thiếu đơn vị/chi nhánh ở header; nếu có sẵn đơn vị thì rơi xuống cuối trả **"Not Responding."** (DataType ngoài 4 nhánh) — dễ nuốt lỗi thầm lặng.
- **Map sàn cứng cho in/đăng đơn:** `DonHangService.cs:2854` (in) & `:2947` (đăng) chỉ map SHOPEE/TIKTOK → Lazada/sàn khác fail.
- **Đồng bộ trạng thái đơn hoàn bỏ thầm lặng:** `DonHangHoanService.cs:1352` `MapEcommerceCode` chỉ 8/9/10; đơn sàn khác hoặc thiếu `MaDonHoanEcomm` bị skip không báo.
- **Đơn hoàn không ripple tồn/KT:** đừng tìm logic cộng tồn ở webhook đơn hoàn — nó ở Accounting `PhieuBanTraLai` (HBBTL), gate bởi `IsHangVeKho` (`DonHangHoanService.cs:1374`, theo `NoiKiemHoan` + `LogisticsLoai` + `LogisticsTrangThaiMa`).
- **RETURN_UPDATE cho phép Items=null** (chỉ cập status) — không phải thiếu data.
- **ecommerce_connect persist trước khi gọi external:** xem convention `defer-persist-until-external-success.md` / `reference_ecommerce_connect_deleteasync_persists_dirty.md` (lỗi `DeleteAsync` persist cả dirty entity) — liên quan khi debug đơn về lỗi.

## File source (path:line)
**SCRM (`D:\PROJECT\Git\_SourceForCode\Group2\scrm`):**
- Controller đơn bán: `OrderLogistic\OrderLogistic.API\Controllers\Ecommerce\DonHangController.cs:35` (`NhanDuLieuDonHangTMDT`), `:34` (`[SBAuthorize]` commented).
- Controller đơn hoàn: `OrderLogistic\OrderLogistic.API\Controllers\Ecommerce\DonHangHoanEcommController.cs:28` (`NhanDuLieuDonHangHoanTMDT`), `:29` (`[SBAuthorize]` commented).
- Service đơn bán: `OrderLogistic\OrderLogistic.Service\Generated\Ecommerce\DonHangService.cs:179` (route NhanDuLieuDonHangTMDT), `:233/:259/:264/:270` (CREATE/UPDATE/UPDATEPAYMENT/UPDATEINVOICE), `:511` (TaoDonHangEcommerce), `:526` (SP dedup GetByMaDonEcomm), `:1594` ("đã ở trạng thái mới nhất"), `:2165` (ValidateDon), `:2854` (map in đơn), `:2947` (map đăng đơn).
- Service đơn hoàn: `OrderLogistic\OrderLogistic.Service\Generated\Ecommerce\DonHangHoanService.cs:105` (NhanDuLieuDonHangHoanTMDT), `:163/:184/:193` (route 3 DataType).

**ecommerce_connect (`D:\PROJECT\Git\_SourceForCode\ecommerce_connect`):**
- Webhook receiver: `EcommerceReceive.API\Controllers\Shopee\PushMechanismController.cs:32` (Receive), `:57` (publish `EcommerceConnect_DataReceivePartner`); `EcommerceReceive.API\Controllers\TikTok\PushNotificationController.cs`.
- Consumer route: `EcommerceConnect.API\Consumer\DataEcomReceiveConsumer.cs:43` (Consume), `:55/:61` (route Shopee/TikTok).
- Đơn lỗi / tạo lại: `EcommerceConnect.Service\Generated\Crm\CrmOrderService.cs:1115` (`SaveOrderToCrm` — ReCreate).
- Đơn hoàn gửi CRM: `EcommerceConnect.Service\Generated\MySystem\OrderReturnShopeeService.cs:605-608` (quyết DataType), `:617` (gọi send), `:1190` (`SendOrderReturnDataToCrmAsync`), `:1213` (`ApiPost`), `:1214` (URL `/api/DonHangHoanEcomm/NhanDuLieuDonHangHoanTMDT`); `OrderReturnTiktokService.cs` (tương tự cho TikTok).
- Controller proxy CRM: `EcommerceConnect.API\Controllers\Crm\CrmOrderController.cs`.

**Tài liệu nền:** registry `projects/scrm/registry/kho/{DonHangController_OrderLogistic,DonHangHoanEcommController,DonHangHoanController}.md`; project `projects/ecommerce_connect/{architecture,domain,patterns}.md` + `convention/internal-webhook-no-auth.md`.
