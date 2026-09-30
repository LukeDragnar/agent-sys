---
controller: SuperShipController
module: kho
service: OrderLogistic
man_hinh_ui: "Giao vận > Cấu hình đối tác giao hàng — màn cấu hình/kết nối đối tác SuperShip; trả mã + tên đối tác để FE bind."
muc_dich: "Trả thông tin định danh đối tác giao vận SuperShip (mã EnumShippingUnitCode.Supership + tên 'SuperShip') cho FE — controller stub theo mẫu chung các đối tác DeliveryConnect."
alias: ["supership", "super ship", "đối tác supership", "giao hàng supership"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# SuperShipController — Thông tin đối tác giao vận SuperShip

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | (màn cấu hình đối tác SuperShip) | GET `api/SuperShip/GetThongTinDoiTac` | `SuperShipController_GetThongTinDoiTac` (DV-AD-TONG/CN/MCN) | public | màn cấu hình đối tác giao vận | Trả object `{ GiaoHangDoiTacMa = EnumShippingUnitCode.Supership, GiaoHangDoiTacTen = "SuperShip" }`; hard-code, không gọi service | hard-code (không EF/SP) | Stub — chỉ trả mã + tên cố định |

## Quan hệ
- **Gọi sang:** không.
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `SuperShipController.cs:11` `[TienIch("SuperShipController", "SuperShip", MaTienIchSCRM.TI_Supership)]` + class-level `[SBAuthorize()]`. Có code comment cũ (gọi `GetThongTinKetNoiDonVi`) đã bị comment-out — hiện chỉ trả dữ liệu cố định.
