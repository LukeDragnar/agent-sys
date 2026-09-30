---
controller: GhnController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI riêng. FE màn cấu hình/đăng đơn gọi để lấy metadata đối tác Giao Hàng Nhanh (mã + tên hiển thị)"
muc_dich: "Trả metadata tĩnh của ĐVVC Giao Hàng Nhanh (mã đối tác = EnumShippingUnitCode.GHN, tên 'Giao hàng nhanh'). Controller mỏng phục vụ FE render dropdown/cấu hình đối tác giao vận."
alias: ["ghn", "giao hàng nhanh", "đối tác ghn", "thông tin đối tác ghn"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in:
  - "FE màn cấu hình/đăng đơn ĐVVC (lấy metadata đối tác GHN)"
cross_module_out: []
---

# GhnController — Metadata đối tác giao vận Giao Hàng Nhanh (GHN)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | nội bộ FE | GET `api/Ghn/GetThongTinDoiTac` | `GhnController_GetThongTinDoiTac` (roles: DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | public | - | Trả object tĩnh `{ GiaoHangDoiTacMa = EnumShippingUnitCode.GHN, GiaoHangDoiTacTen = "Giao hàng nhanh" }` | hardcode | XML doc-comment trên class ghi nhầm "GhtkController" (`GhnController.cs:9`); `[TienIch]` đặt đúng "GhnController" |

## Quan hệ
- **Gọi sang:** không có (action thuần trả object tĩnh).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không. Kết nối/đăng đơn GHN thực hiện ở `DeliveryUnitConnectController` (cấu hình) + `OrderService.CreateOrder` (đăng đơn).

## Lỗi đã biết / lưu ý kỹ thuật
- Controller mỏng (1 endpoint metadata), giá trị hardcode trong action.
- `[TienIch("GhnController", "Giao hàng nhanh", MaTienIchSCRM.TI_Ghn)]` + `[SBAuthorize]` mở; endpoint gắn permission key + roles.
- `public partial class GhnController` nhưng không có file partial khác — không có logic ẩn.
- Comment summary class sai tên (GhtkController) — chỉ là lỗi doc copy-paste.
