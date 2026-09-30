---
controller: JntController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI riêng. FE màn cấu hình/đăng đơn gọi để lấy metadata đối tác J&T Express (mã + tên hiển thị)"
muc_dich: "Trả metadata tĩnh của ĐVVC J&T Express (mã đối tác = EnumShippingUnitCode.JNT, tên 'J&T'). Controller mỏng phục vụ FE render dropdown/cấu hình đối tác giao vận."
alias: ["jnt", "j&t", "j&t express", "đối tác jnt", "thông tin đối tác jnt"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in:
  - "FE màn cấu hình/đăng đơn ĐVVC (lấy metadata đối tác J&T)"
cross_module_out: []
---

# JntController — Metadata đối tác giao vận J&T Express

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | nội bộ FE | GET `api/Jnt/GetThongTinDoiTac` | `JntController_GetThongTinDoiTac` (roles: DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | public | - | Trả object tĩnh `{ GiaoHangDoiTacMa = EnumShippingUnitCode.JNT, GiaoHangDoiTacTen = "J&T" }` | hardcode | - |

## Quan hệ
- **Gọi sang:** không có (action thuần trả object tĩnh).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không. Kết nối/đăng đơn J&T thực hiện ở `DeliveryUnitConnectController` (cấu hình) + `OrderService.CreateOrder` (đăng đơn).

## Lỗi đã biết / lưu ý kỹ thuật
- Controller mỏng (1 endpoint metadata), giá trị hardcode trong action.
- `[TienIch("JntController", "JNT", MaTienIchSCRM.TI_Jnt)]` + `[SBAuthorize]` mở; endpoint gắn permission key + roles.
