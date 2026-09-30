---
controller: NjvController
module: kho
service: OrderLogistic
man_hinh_ui: "Giao vận > Cấu hình đối tác giao hàng — màn cấu hình/kết nối đối tác Ninja Van; nút trên màn chọn đối tác vận chuyển trả về mã + tên đối tác để FE bind."
muc_dich: "Trả thông tin định danh đối tác giao vận Ninja Van (mã EnumShippingUnitCode.Njv + tên 'Ninja Van') cho FE — controller stub theo mẫu chung các đối tác DeliveryConnect."
alias: ["ninja van", "njv", "đối tác ninja van", "giao hàng ninja van"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# NjvController — Thông tin đối tác giao vận Ninja Van

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | (màn cấu hình đối tác Ninja Van) | GET `api/Njv/GetThongTinDoiTac` | `NjvController_GetThongTinDoiTac` (DV-AD-TONG/CN/MCN) | public | màn cấu hình đối tác giao vận | Trả object `{ GiaoHangDoiTacMa = EnumShippingUnitCode.Njv, GiaoHangDoiTacTen = "Ninja Van" }`; KHÔNG gọi service, dữ liệu hard-code | hard-code (không EF/SP) | Stub — không gọi service nào; chỉ trả mã + tên cố định |

## Quan hệ
- **Gọi sang:** không (trả dữ liệu cố định trong controller).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `NjvController.cs:11` gắn `[TienIch("NjvController", "Njv", MaTienIchSCRM.TI_Njv)]` + class-level `[SBAuthorize()]`; mọi đối tác giao vận (Njv/Spx/SuperShip/ViettelPost/VnPost/ShippingUnitCustom) dùng chung 1 khuôn `GetThongTinDoiTac`.
