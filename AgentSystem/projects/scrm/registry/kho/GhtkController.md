---
controller: GhtkController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI riêng. FE màn cấu hình/đăng đơn gọi để lấy metadata đối tác Giao Hàng Tiết Kiệm (mã + tên hiển thị)"
muc_dich: "Trả metadata tĩnh của ĐVVC Giao Hàng Tiết Kiệm (mã đối tác = EnumShippingUnitCode.GHTK, tên 'Giao hàng tiết kiệm'). Controller mỏng phục vụ FE render dropdown/cấu hình đối tác giao vận."
alias: ["ghtk", "giao hàng tiết kiệm", "đối tác ghtk", "thông tin đối tác ghtk"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in:
  - "FE màn cấu hình/đăng đơn ĐVVC (lấy metadata đối tác GHTK)"
cross_module_out: []
---

# GhtkController — Metadata đối tác giao vận Giao Hàng Tiết Kiệm (GHTK)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | nội bộ FE | GET `api/Ghtk/GetThongTinDoiTac` | `GhtkController_GetThongTinDoiTac` (roles: DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | public | - | Trả object tĩnh `{ GiaoHangDoiTacMa = EnumShippingUnitCode.GHTK, GiaoHangDoiTacTen = "Giao hàng tiết kiệm" }` | hardcode | - |

## Quan hệ
- **Gọi sang:** không có (action thuần trả object tĩnh).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không. Kết nối/đăng đơn GHTK thực hiện ở `DeliveryUnitConnectController` (cấu hình) + `OrderService.CreateOrder` (đăng đơn).

## Lỗi đã biết / lưu ý kỹ thuật
- Controller mỏng (1 endpoint metadata), giá trị hardcode trong action.
- `[TienIch("GhtkController", "Giao hàng tiết kiệm", MaTienIchSCRM.TI_Ghtk)]` + `[SBAuthorize]` mở; endpoint gắn permission key + roles.
- `public partial class GhtkController` nhưng không có file partial khác — không có logic ẩn.
