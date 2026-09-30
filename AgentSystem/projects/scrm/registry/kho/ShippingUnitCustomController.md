---
controller: ShippingUnitCustomController
module: kho
service: OrderLogistic
man_hinh_ui: "Giao vận > Cấu hình đối tác giao hàng — màn cấu hình 'Giao hàng tùy chỉnh' (đơn vị tự giao, không qua API đối tác); trả mã + tên để FE bind."
muc_dich: "Trả thông tin định danh hình thức 'Giao hàng tùy chỉnh' (mã EnumShippingUnitCode.ShippingUnitCustom + tên 'Giao hàng tùy chỉnh') cho FE — controller stub theo mẫu chung các đối tác DeliveryConnect."
alias: ["giao hàng tùy chỉnh", "shipping unit custom", "tự giao hàng", "đối tác tùy chỉnh", "custom shipping"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# ShippingUnitCustomController — Thông tin hình thức giao hàng tùy chỉnh

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | (màn cấu hình giao hàng tùy chỉnh) | GET `api/ShippingUnitCustom/GetThongTinDoiTac` | `ShippingUnitCustomController_GetThongTinDoiTac` (DV-AD-TONG/CN/MCN) | public | màn cấu hình đối tác giao vận | Trả object `{ GiaoHangDoiTacMa = EnumShippingUnitCode.ShippingUnitCustom, GiaoHangDoiTacTen = "Giao hàng tùy chỉnh" }`; hard-code, không gọi service | hard-code (không EF/SP) | Stub — chỉ trả mã + tên cố định |

## Quan hệ
- **Gọi sang:** không.
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `ShippingUnitCustomController.cs:11` `[TienIch(..., MaTienIchSCRM.TI_ShippingUnitCustom)]` + class-level `[SBAuthorize()]`. Có code comment cũ (`GetThongTinKetNoiDonVi`) đã comment-out. Đây là "đối tác" đặc biệt = đơn vị tự giao, không kết nối API đối tác thật.
