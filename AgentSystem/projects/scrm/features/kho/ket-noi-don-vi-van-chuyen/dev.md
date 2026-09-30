# Dev — Kết nối đơn vị vận chuyển

> CHỈ dev. Không cho khách. Service: **OrderLogistic** (BE) + module **delivery** (FE).
> Kiến trúc cốt lõi: SCRM **không gọi thẳng API hãng**. SCRM là **proxy** → gọi sang một **DeliveryConnect service** riêng (`DeliveryConnectServiceSetting.ServiceUrl`), service đó mới giữ tích hợp từng hãng + lưu token. Đăng/hủy đơn đi **bất đồng bộ qua RabbitMQ**.

## Màn hình & route (FE — Angular 15)

Module: `src/app/pages/delivery/` (`delivery.module.ts`, `delivery-routing.module.ts`).

- Component dùng chung (form kết nối động): `src/app/pages/delivery/delivery-unit-connect/delivery-unit-connect.component.ts|html|scss`
- Component theo hãng (route riêng, bọc form chung):
  - `/delivery/ghn` → `delivery/ghn/ghn.component.ts`
  - `/delivery/ghtk` → `delivery/ghtk/ghtk.component.ts`
  - `/delivery/jnt` → `delivery/jnt/jnt.component.ts`
  - `/delivery/vtp` → `delivery/viettel-post/viettel-post.component.ts`
  - `/delivery/vnp` → `delivery/vnpost/vnpost.component.ts`
  - `/delivery/best`, `/ems`, `/ahamove`, `/supership`, `/njv`, `/spx-express`
  - `/delivery/don-vi-tuy-chinh` → `delivery/shipping-unit-custom/shipping-unit-custom.component.ts`
  - `/delivery/giaohangthucong` → `delivery/delivery-account-page/delivery-account-page.component.ts`
- FE service: `src/app/services/orderLogistic/deliveryUnit.service.ts`
  - `Xxx_GetThongTinDoiTac()` → `GET /api/{Hang}/GetThongTinDoiTac`
  - `GetThongTinKetNoiDonVi`, `SaveThongTinKetNoiDonVi`, `CRM_ConnectShippingUnit`, `CRM_DisconnectShippingUnit`, `GetDanhSachCuaHangShipping`, `GetPhuongThucGiaoHangByShipping`, `CRM_GetUrlLoginPartner` → `POST/GET /api/DeliveryUnitConnect/...`
  - `CalculateFee` → `POST /api/Order/CalculateFee`
- Đăng đơn FE đi qua các service theo vai trò: `don-hang-sale.service.ts`, `thuKhoTacNghiep.service.ts`, `keToanTacNghiep.service.ts` (method `InsertDonHangDangDon` / `ProcessDonHangDangDon` / `InsertDonHangHuyDangDon`); Ahamove riêng: `donHangLogistic.service.ts` → `Crm_DangDonAhamove`.
- Form nhập token/tài khoản: control `partnerToken`, `partnerUserName`, `partnerPassword`, `partnerWarehouseId`, `propValue1..5`. Nút: `onSaveConnect(isConnect)` (true=CRM_ConnectShippingUnit test, false=SaveThongTinKetNoiDonVi), `onDisConnect()`.

## Controller mỗi hãng (BE)

Thư mục: `OrderLogistic/OrderLogistic.API/Controllers/DeliveryConnect/`

Mỗi hãng có 1 controller **stub mỏng**, chỉ trả metadata `{ GiaoHangDoiTacMa, GiaoHangDoiTacTen }` qua `GetThongTinDoiTac` (KHÔNG chứa logic gọi hãng):

| Controller | Endpoint perm | Code hãng (EnumShippingUnitCode) |
|------------|---------------|-----------------------------------|
| `ViettelPostController` | `ViettelPostController_GetThongTinDoiTac` | `VTP` |
| `GhnController` (partial) | `GhnController_GetThongTinDoiTac` | `GHN` |
| `JntController` | `JntController_GetThongTinDoiTac` | `JNT` |
| `VnPostController` | ... | `VNP` |
| `SuperShipController` | ... | `SUPERSHIP` |
| `EmsController` | ... | `EMS` |
| `AhamoveController` (partial) | `AhamoveController_GetThongTinDoiTac` | `AHAMOVE` |
| `BestController` | ... | `BEST` |
| `NjvController` (Ninja Van) | ... | `NJV` |
| `SpxController` | ... | `SPX` |
| `GhtkController` | ... | `GHTK` |
| `ShippingUnitCustomController` | `ShippingUnitCustomController_GetThongTinDoiTac` | `SHIPPINGUNITCUSTOM` |
| `BoxmeController` | ... | `BOXME` (có trong enum, không nằm trong 12 hãng UI chính) |

Mỗi controller gắn `[TienIch("XxxController", "Tên", MaTienIchSCRM.TI_Xxx)]` + `[SBAuthorize()]`.

**Controller thật chứa logic** (đều ở cùng thư mục):
- `DeliveryUnitConnectController` — kết nối/lưu/hủy/lấy thông tin tài khoản giao vận.
- `OrderController` — tính phí, đăng/hủy/in đơn, nhận webhook trạng thái.

## Entities (DataContext: `OrderLogistic.DataContext.Entity`)

- `DonHangLogistic` — đơn hàng. Field giao vận quan trọng:
  - `GiaoHangDoiTacMa` (code hãng), `IdGiaoHangTaiKhoan` (tài khoản giao vận đã kết nối)
  - `MaDon` (mã đơn CRM), `MaDonGiaoVan` (mã vận đơn hãng), `ParnerDonHangId` (id đơn phía hãng), `GiaoHangMaInDon` (mã in)
  - `DonHangIsDangDon`, `NgayDangDon`, `GiaoHangTrangThaiMa`, `GiaoHangTrangThaiNgayCapNhat`
  - `DonHangGiaoHangShipFee`, `DonHangHoTroShipFee`, `DonHangTienThuKhach`, `DonHangTienCuoi`, `DonHangDatCoc`
- `DonHangDangDon`, `DonHangHuyDangDon` — hàng đợi/ log đăng & hủy đăng đơn.
- `DonHangLogisticInfo` (MongoDB collection `DonHangLogisticInfo`) — chi tiết đơn + tên trạng thái giao hàng.
- Enum code hãng: `Models.Core/DTO/OrderLogistic.Client/DeliveryConnect/EnumShippingUnitCode.cs`.

## Luồng code (pattern chung các hãng)

Điểm mấu chốt: **tất cả hãng dùng chung 1 đường**, phân biệt nhau chỉ bằng `ShippingUnitCode`. Không có code if-else theo từng hãng ở SCRM (logic riêng hãng nằm bên DeliveryConnect service).

1. **Kết nối** — `DeliveryUnitConnectService` (`Generated/DeliveryUnitConnect/DeliveryUnitConnectService.cs`):
   - `MapUserLoginDeliveryConnect(userLogin)` gắn `ServerId/ServerCode/ServerName/CrmUnitId/CrmCallBackUrl` (từ `DeliveryConnectServiceSetting`) → định danh tenant với DeliveryConnect.
   - `CRM_ConnectShippingUnit` → `DeliveryConnectService.CRM_ConnectShippingUnit` → `POST {ServiceUrl}/api/unit/ConnectShippingUnit`.
   - `SaveThongTinKetNoiDonVi` → `POST {ServiceUrl}/api/unit/SaveUnitConnect`.
   - `GetThongTinKetNoiDonVi` → `POST {ServiceUrl}/api/unit/GetUnitConnect` (trả cấu hình form động).
   - `GetDanhSachCuaHangShipping` → `/api/unit/GetStoreShippingUnit`.
   - `CRM_GetUrlLoginPartner` → `/api/unit/GetUrlLogin` (OAuth, vd Ninja Van).
   - `CRM_DisconnectShippingUnit` → `DeliveryConnectService.CRM_DisConnectShippingUnit` → `/api/unit/DisConnectShippingUnit`.
   - DTO: `DeliveryConnectModel.cs` (`ModelDataUnitConnect`, `ReqSaveUnitConnect`, `ReqGetUnitConnect`...). Token = `PartnerToken`, tài khoản = `PartnerUserName/PartnerPassword`, kho lấy = `PartnerWarehouseId`, cấu hình thêm = `PropValue1..5`. Mỗi field kèm cờ `_Enable/_Mandatory/_ListValueOption` → FE render form động.

2. **Tính phí** — `OrderService.CalculateFee` (`Generated/DeliveryUnitConnect/OrderService.cs`): map `CalculateFeeDto` → `ReqCalculateFee` (sum số lượng/cân nặng/kích thước sản phẩm) → `POST {ServiceUrl}/api/order/CalculateFee`.

3. **Đăng đơn** — `OrderService.CreateOrder(donHangDb, donHangLogisticInfo, idTransaction, userLogin)`:
   - **Validate COD**: `(DonHangTienCuoi - DonHangDatCoc) == DonHangTienThuKhach` else "Tiền thu hộ không chính xác." (chống thu sai 0đ — đã có sự cố Pushsale→Sandbox).
   - `MapDataReqCreateOrder(...)` → publish **`MessageCreateOrder`** (JSON) qua `IPublishEndpointDonHang` (RabbitMQ). **Không** gọi đồng bộ.
   - Set cache tra cứu vận đơn theo mã đơn (5 phút).

4. **Callback tạo đơn** — `OrderController.StatusCreateOrder` (`[AllowAnonymous]`) → `OrderService.StatusCreateOrder(reqData)`:
   - Parse `OrderDataShort`, lấy `donHangDb` qua SP `PRC_SCRM_OrderLogistic_DonHangLogistic_DeliveryStatusCreateAndCancelOrder` (param `@IdDonVi/@GiaoHangDoiTacMa/@MaDon`).
   - **Success**: gán `ParnerDonHangId/MaDonGiaoVan/GiaoHangMaInDon`, `DonHangIsDangDon=true`, trạng thái `GiaoVan_TT_PS.DaDang`; bù chênh phí ship (chỉ khi `ShipFee>0 && khác cũ`); update Mongo; tạo phiếu bán hàng online (`DonHangKeToan_PhieuChoDonHangOnline_Save`); publish thông báo; sync ES/Marketing/Bravo; log `DangDon`.
   - **Fail**: `DangHuyDon_V2(..., mode 2)` cộng lại kho, `PhieuBanHangRollBack`, log `DangDonThatBai`.
   - Update trong transaction + `TaoNghiepVuPendingInTransactionAsync`.

5. **Hủy đăng đơn** — `OrderService.CancelOrder` → publish `MessageCancelOrder` (RabbitMQ) → callback `StatusCancelOrder` → đưa trạng thái về `TrangThaiGiaoHangKhiHuyDangDon` (mặc định `ChoVanDon`), xóa `MaDonGiaoVan`, `DonHangIsDangDon=false`, cộng kho, rollback phiếu bán hàng. Có chặn double-cancel (`DonHangIsDangDon != true`).

6. **Khác**: `ViewDetail` → `/api/order/ViewDetail`; `PrintOrders` → in vận đơn hàng loạt; `ReceiveStatusOrder` → webhook cập nhật trạng thái giao hàng; `CRM_GetTrackingUrl` → `/api/Order/GetTrackingUrl`.

7. **Consumer** (`OrderLogistic/OrderLogisticConsumer.API/Consumer/Delivery/`): `StatusCreateOrderConsumer`, `StatusCancelOrderConsumer` nhận kết quả từ DeliveryConnect và gọi lại `StatusCreateOrder`/`StatusCancelOrder`.

## Bug-prone

- **COD validate** là chốt chặn quan trọng nhất — đã có case sync data bị null → thu khách 0đ. Mọi thay đổi mapping COD/tiền phải giữ check `(TienCuoi - DatCoc) == TienThuKhach`.
- **Phí ship bù chênh lệch**: chỉ cộng **chênh lệch** vào `ShipFee` + `HoTroShipFee`, **không** tính lại theo công thức, **không** đổi tiền thu khách đã chốt. Phí null/0 phải bỏ qua (hãng ẩn phí như GHTK), không set về 0.
- **Bất đồng bộ**: đăng/hủy đơn qua RabbitMQ → kết quả về sau qua webhook/consumer. Đừng giả định đăng đơn xong là có mã vận đơn ngay.
- **Double-process**: `StatusCancelOrder` chặn `DonHangIsDangDon != true`; cờ `IsProcessed/ProcessedDate` để chống xử lý trùng.
- **`IdGiaoHangTaiKhoan` = `CrmUnitChildId`**: chọn sai tài khoản → sai kho lấy hàng / sai cấu hình hãng. Khi đăng/hủy phải truyền đúng.
- **Webhook `[AllowAnonymous]`** (`StatusCreateOrder/StatusCancelOrder/ReceiveStatusOrder`): không có auth — cần verify nguồn ở DeliveryConnect / network.
- **`ConvertResDeliveryConnect`** dùng `reqData.ToString()` + `JsonConvert` 2 lớp (Data lồng JSON string) — dễ vỡ nếu format payload đổi.
- Controller hãng chỉ là **stub metadata**; đừng tìm logic gọi hãng ở đó — logic ở `OrderService`/`DeliveryUnitConnectService` (proxy) và bên **DeliveryConnect service** ngoài repo này.
- Enum `EnumShippingUnitCode` còn `BOXME` (có controller) nhưng UI chính liệt kê 12 hãng khác — kiểm tra trước khi coi Boxme là hãng đang bán.
- **SP quy đổi đơn vị**: `DonGia` là giá theo **đơn vị BÁN**, phải ghép với `SoLuongBanTheoDonViQuyDoi`; lấy nhầm `SoLuong` (đơn vị nhỏ, để trừ kho) → SL **và** giá trị đơn gấp `TiLeQuyDoi` lần trên vận đơn (hãng tự cộng tổng giá trị từ `LIST_ITEM`). Sự cố 21/07/2026 BigSun→VTP: 19.160.000 thay vì 1,9tr. Nhớ rà **cả 2 nhánh** dựng `Products` (`isTinhPhi=true` lấy Mongo `DonHangSanPhamInfos` / `false` lấy DB qua `GenerateProduct`). Chi tiết: `lessons/sp-quy-doi-gui-dvvc-sai-don-vi.md`.
- **Giá trị khai = `tongTienSp` (Σ `DonGia`×SL) → TRƯỚC chiết khấu**, không phải tiền khách trả. `Price` và `Insurance` cùng nhận giá trị này nhưng **mỗi hãng đọc một field khác nhau** (VTP/JNT/SuperShip/Best/EMS/BoxMe đọc `Price`; NJV/GHN-create đọc `Insurance`; GHTK ưu tiên `Insurance` fallback `Price`) → sửa 1 field không phủ hết hãng, sửa `Price` thì đụng cả tính phí. Liên quan task 1136 (giá trị bảo hiểm sau CK).

## File source

BE (`D:\PROJECT\Git\_SourceForCode\Group2\scrm`):
- `OrderLogistic/OrderLogistic.API/Controllers/DeliveryConnect/` — 15 controller (12 hãng stub + Boxme + `DeliveryUnitConnectController` + `OrderController`).
- `OrderLogistic/OrderLogistic.Service/Generated/DeliveryUnitConnect/DeliveryUnitConnectService.cs` — proxy kết nối.
- `OrderLogistic/OrderLogistic.Service/Generated/DeliveryUnitConnect/OrderService.cs` — tính phí / đăng / hủy / callback (~2143 dòng).
- `OrderLogistic/OrderLogistic.Service/Generated/OtherService/DeliveryConnectService.cs` — client gọi DeliveryConnect service + `MapUserLoginDeliveryConnect`.
- `OrderLogistic/OrderLogisticConsumer.API/Consumer/Delivery/StatusCreateOrderConsumer.cs`, `StatusCancelOrderConsumer.cs`.
- `Libs/Models.Core/DTO/OrderLogistic.Client/DeliveryConnect/DeliveryConnectModel.cs`, `EnumShippingUnitCode.cs`, `OrderModel.cs`, `OrderDataShortModel.cs`, `UserLoginDeliveryConnectModel.cs`.
- `Libs/SB.Core/Setting/DeliveryConnectServiceSetting.cs` — `ServiceUrl`, `ServerId/Code/Name`, `CallBackUrl`.
- `OrderLogistic/OrderLogistic.DataContext/Entity/DonHangLogistic.cs`, `DonHangDangDon.cs`, `DonHangHuyDangDon.cs`.
- SP: `PRC_SCRM_OrderLogistic_DonHangLogistic_DeliveryStatusCreateAndCancelOrder`.

FE (`D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`):
- `src/app/pages/delivery/` (module + route + component theo hãng + `delivery-unit-connect/`).
- `src/app/services/orderLogistic/deliveryUnit.service.ts` (kết nối + GetThongTinDoiTac + CalculateFee).
- `src/app/services/orderLogistic/{don-hang-sale,thuKhoTacNghiep,keToanTacNghiep,donHangLogistic}.service.ts` (đăng/hủy đơn).
