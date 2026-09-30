---
controller: ViettelPostController
module: kho
service: OrderLogistic
man_hinh_ui: "Giao vận > Cấu hình đối tác giao hàng — màn cấu hình/kết nối đối tác Viettel Post; trả mã + tên đối tác để FE bind."
muc_dich: "Trả thông tin định danh đối tác giao vận Viettel Post (mã EnumShippingUnitCode.VTP + tên 'Viettel Post') cho FE — controller stub theo mẫu chung các đối tác DeliveryConnect."
alias: ["viettel post", "vtp", "đối tác viettel post", "giao hàng viettel post"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# ViettelPostController — Thông tin đối tác giao vận Viettel Post

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | (màn cấu hình đối tác Viettel Post) | GET `api/ViettelPost/GetThongTinDoiTac` | `ViettelPostController_GetThongTinDoiTac` (DV-AD-TONG/CN/MCN) | public | màn cấu hình đối tác giao vận | Trả object `{ GiaoHangDoiTacMa = EnumShippingUnitCode.VTP, GiaoHangDoiTacTen = "Viettel Post" }`; hard-code, không gọi service | hard-code (không EF/SP) | Stub — chỉ trả mã + tên cố định |

## Quan hệ
- **Gọi sang:** không.
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `ViettelPostController.cs:11` `[TienIch("ViettelPostController", "ViettelPost", MaTienIchSCRM.TI_Vtp)]` + class-level `[SBAuthorize()]`. Cùng khuôn với các controller đối tác DeliveryConnect khác.
