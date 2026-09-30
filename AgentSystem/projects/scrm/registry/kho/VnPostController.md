---
controller: VnPostController
module: kho
service: OrderLogistic
man_hinh_ui: "Giao vận > Cấu hình đối tác giao hàng — màn cấu hình/kết nối đối tác VNPOST; trả mã + tên đối tác để FE bind."
muc_dich: "Trả thông tin định danh đối tác giao vận VNPOST (mã EnumShippingUnitCode.VnPost + tên 'VNPOST') cho FE — controller stub theo mẫu chung các đối tác DeliveryConnect."
alias: ["vnpost", "vn post", "đối tác vnpost", "giao hàng vnpost", "bưu điện"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# VnPostController — Thông tin đối tác giao vận VNPOST

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | (màn cấu hình đối tác VNPOST) | GET `api/VnPost/GetThongTinDoiTac` | `VnPostController_GetThongTinDoiTac` (DV-AD-TONG/CN/MCN) | public | màn cấu hình đối tác giao vận | Trả object `{ GiaoHangDoiTacMa = EnumShippingUnitCode.VnPost, GiaoHangDoiTacTen = "VNPOST" }`; hard-code, không gọi service | hard-code (không EF/SP) | Stub — chỉ trả mã + tên cố định |

## Quan hệ
- **Gọi sang:** không.
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `VnPostController.cs:11` `[TienIch("VnPostController", "VnPost", MaTienIchSCRM.TI_Vnp)]` + class-level `[SBAuthorize()]`. Cùng khuôn với các controller đối tác DeliveryConnect khác.
