---
controller: DeliveryAddressController
module: he-thong
service: Core
man_hinh_ui: "Quản trị (QuanTriArea) > Hệ thống > quản lý địa chỉ (hệ thống / giao vận / đối tác)"
muc_dich: "Quản lý địa chỉ: địa chỉ hệ thống (Unit), địa chỉ giao vận (Delivery) và địa chỉ đối tác vận chuyển (GHTK...) qua DeliveryConnect; lấy/lưu/xóa + đồng bộ toàn bộ cây địa chỉ Tỉnh→Huyện→Xã."
alias: ["Địa chỉ giao vận", "DeliveryAddress", "địa chỉ đối tác", "đồng bộ địa chỉ GHTK", "Tỉnh Huyện Xã"]
auth: mixed
state_machine: []
deprecated: false
entities: [Address, AddressDelivery]
stored_procs: []
depends: ["IDeliveryAddressService", "DeliveryConnect (API đối tác GHTK...)"]
cross_module_in: ["[UI] màn quản lý địa chỉ gọi GetList/Unit_*/Delivery_*", "[internal] service khác gọi GetPartnerAddressByAddressSystem để map địa chỉ hệ thống ↔ đối tác"]
cross_module_out: ["DeliveryConnect / API đối tác vận chuyển (lấy + đồng bộ địa chỉ)"]
---

# DeliveryAddressController — Quản lý địa chỉ hệ thống & giao vận & đối tác

Controller trong namespace `Core.API.Areas.SanboxArea` nhưng **route thật `api/QuanTriArea/[controller]`** (KHÔNG có `[Area]`) → `api/QuanTriArea/DeliveryAddress`. `[SBAuthorize]` controller-level → mặc định cần đăng nhập; auth **mixed** vì có 1 endpoint `[PrivateSBAuthorize]` (gọi nội bộ). 3 nhóm: địa chỉ hệ thống (Unit), địa chỉ giao vận (Delivery), địa chỉ đối tác (Partner, qua DeliveryConnect). Nghiệp vụ ủy quyền `IDeliveryAddressService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy DS địa chỉ hệ thống + giao vận | (lưới địa chỉ) | POST `.../DeliveryAddress/GetListDiaChiHeThongGiaoVan` | `DeliveryAddress_GetListDiaChiHeThongGiaoVan` | public | — | Nhận `DeliveryAddressModel`, trả danh sách địa chỉ hệ thống + giao vận theo điều kiện (kèm `CurrentUser`). | EF qua service | — |
| 2 | Lấy chi tiết địa chỉ hệ thống | (chi tiết) | POST `.../DeliveryAddress/Unit_GetAddress` | `DeliveryAddress_Unit_GetAddress` | public | — | Nhận `ReqGetDetailAddress`, trả chi tiết 1 địa chỉ hệ thống. | EF qua service | — |
| 3 | Lưu địa chỉ hệ thống (private) | Lưu | POST `.../DeliveryAddress/Unit_SaveAddress` | `DeliveryAddress_Unit_SaveAddress` | public | — | Nhận `Address`, lưu địa chỉ hệ thống. (Không truyền CurrentUser.) | EF qua service | — |
| 4 | Xóa địa chỉ hệ thống (private) | Xóa | GET `.../DeliveryAddress/Unit_DeleteAddress?id=` | `DeliveryAddress_Unit_DeleteAddress` | public | — | Xóa địa chỉ hệ thống theo `id?` (Guid nullable). **Xóa bằng HTTP GET.** | EF qua service | Xóa bằng GET — lệch convention, rủi ro side-effect qua GET |
| 5 | Lấy chi tiết địa chỉ giao vận | (chi tiết) | POST `.../DeliveryAddress/Delivery_GetAddress` | `DeliveryAddress_Unit_Delivery_GetAddress` | public | — | Nhận `ReqGetDetailAddressDelivery`, trả chi tiết địa chỉ giao vận (kèm CurrentUser). | EF qua service | — |
| 6 | Lưu địa chỉ giao vận | Lưu | POST `.../DeliveryAddress/Delivery_SaveAddress` | `DeliveryAddress_Delivery_SaveAddress` | public | — | Nhận `AddressDelivery`, lưu địa chỉ giao vận. | EF qua service | — |
| 7 | Lấy địa chỉ từ đối tác (1 cấp) | (chọn Tỉnh/Huyện/Xã) | POST `.../DeliveryAddress/GetPartnerAddressList` | `DeliveryAddress_GetPartnerAddressList` | public | — | Lấy địa chỉ trực tiếp từ API đối tác (GHTK...) qua DeliveryConnect theo `shippingUnitCode` + `parentId`. | API đối tác qua service | — |
| 8 | Đồng bộ toàn bộ cây địa chỉ đối tác | Đồng bộ tự động | POST `.../DeliveryAddress/AutoSyncFullSystem?shippingUnitCode=` | `DeliveryAddress_AutoSyncFullSystem` | public | — | Kích hoạt đồng bộ TOÀN BỘ Tỉnh→Huyện→Xã từ đối tác về hệ thống. Thao tác nặng, ripple cross-service. | API đối tác qua service | Thao tác đồng bộ lớn — cần kiểm soát chạy đồng thời/timeout |
| 9 | Map địa chỉ hệ thống → đối tác (nội bộ) | (không UI) | POST `.../DeliveryAddress/GetPartnerAddressByAddressSystem` | — (`[PrivateSBAuthorize]`) | internal | — | Nhận `DeliveryPartnerAddressModel` (danh sách địa chỉ hệ thống), trả thông tin đối tác (PartnerId, PartnerName...) tương ứng. | service | Endpoint nội bộ service-to-service |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GetPartnerAddressByAddressSystem` (`[PrivateSBAuthorize]`): caller nội bộ truyền danh sách địa chỉ hệ thống → nhận map sang địa chỉ/ID đối tác vận chuyển (phục vụ tạo vận đơn).

## Quan hệ
- **Gọi sang:** `IDeliveryAddressService` → DeliveryConnect / API đối tác (GHTK...).
- **Dữ liệu (entities):** `Address` (hệ thống), `AddressDelivery` (giao vận); models `DeliveryAddressModel`, `DeliveryPartnerAddressModel`.
- **Ripple / đồng bộ:** `AutoSyncFullSystem` kéo toàn bộ Tỉnh/Huyện/Xã từ đối tác về hệ thống; `GetPartnerAddressByAddressSystem` được service khác dùng khi đẩy đơn.

## Luồng xuyên module
- Đồng bộ địa chỉ: UI/job → `AutoSyncFullSystem(shippingUnitCode)` → service gọi DeliveryConnect/API đối tác lấy cây Tỉnh→Huyện→Xã → ghi/đối chiếu vào địa chỉ hệ thống (side-effect: cập nhật danh mục địa chỉ dùng chung cho tạo vận đơn).

## Lỗi đã biết / lưu ý kỹ thuật
- `DeliveryAddressController.cs:11,15` — namespace `SanboxArea` nhưng route `api/QuanTriArea/[controller]`, không có `[Area]` → đường dẫn thật `api/QuanTriArea/DeliveryAddress`.
- `DeliveryAddressController.cs:75-76` — `Unit_DeleteAddress` xóa dữ liệu bằng `[HttpGet]` → lệch convention, rủi ro side-effect qua GET/prefetch.
- `DeliveryAddressController.cs:137-143` — `AutoSyncFullSystem` là thao tác đồng bộ nặng (cả cây địa chỉ) gọi qua HTTP request đồng bộ → nguy cơ timeout / chạy chồng; nên có khóa/idempotency.
- `DeliveryAddressController.cs:89` — permission key `DeliveryAddress_Unit_Delivery_GetAddress` đặt tên lẫn lộn Unit/Delivery (khó tra cứu phân quyền).
