---
controller: SpxController
module: kho
service: OrderLogistic
man_hinh_ui: "Giao vận > Cấu hình đối tác giao hàng — màn cấu hình/kết nối đối tác Shopee Express; trả mã + tên đối tác để FE bind."
muc_dich: "Trả thông tin định danh đối tác giao vận Shopee Express (mã EnumShippingUnitCode.Spx + tên 'Shopee Express') cho FE — controller stub theo mẫu chung các đối tác DeliveryConnect."
alias: ["shopee express", "spx", "đối tác spx", "giao hàng shopee express"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# SpxController — Thông tin đối tác giao vận Shopee Express

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | (màn cấu hình đối tác Shopee Express) | GET `api/Spx/GetThongTinDoiTac` | `SpxController_GetThongTinDoiTac` (DV-AD-TONG/CN/MCN) | public | màn cấu hình đối tác giao vận | Trả object `{ GiaoHangDoiTacMa = EnumShippingUnitCode.Spx, GiaoHangDoiTacTen = "Shopee Express" }`; hard-code, không gọi service | hard-code (không EF/SP) | Stub — chỉ trả mã + tên cố định |

## Quan hệ
- **Gọi sang:** không.
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `SpxController.cs:14` `[TienIch("SpxController", "Spx", MaTienIchSCRM.TI_Spx)]` + class-level `[SBAuthorize()]` (Author hunglq, 02/06/2025). Cùng khuôn với các controller đối tác DeliveryConnect khác.
