---
controller: AhamoveController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI riêng. FE màn cấu hình/đăng đơn gọi để lấy metadata đối tác Ahamove (mã + tên hiển thị)"
muc_dich: "Trả metadata tĩnh của ĐVVC Ahamove (mã đối tác = EnumShippingUnitCode.Ahamove, tên 'Ahamove'). Controller mỏng phục vụ FE lấy thông tin đối tác giao vận để render dropdown/cấu hình."
alias: ["ahamove", "giao hàng ahamove", "đối tác ahamove", "thông tin đối tác ahamove"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in:
  - "FE màn cấu hình/đăng đơn ĐVVC (lấy metadata đối tác Ahamove)"
cross_module_out: []
---

# AhamoveController — Metadata đối tác giao vận Ahamove

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | nội bộ FE | GET `api/Ahamove/GetThongTinDoiTac` | `AhamoveController_GetThongTinDoiTac` (roles: DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | public | - | Trả object tĩnh `{ GiaoHangDoiTacMa = EnumShippingUnitCode.Ahamove, GiaoHangDoiTacTen = "Ahamove" }` | hardcode | Không gọi service/DB — giá trị cứng trong action |

## Quan hệ
- **Gọi sang:** không có (action thuần trả object tĩnh).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không. Kết nối/đăng đơn Ahamove thực hiện ở `DeliveryUnitConnectController` (cấu hình) và `OrderService.CreateOrder` / `Crm_DangDonAhamove` (đăng đơn riêng cho Ahamove, xem OrderService.cs:139).

## Lỗi đã biết / lưu ý kỹ thuật
- Controller mỏng (chỉ 1 endpoint metadata). `public partial class` nhưng không có file partial khác (`AhamoveController.cs:15`) — không có logic ẩn ở part khác.
- `[TienIch("AhamoveController", "Ahamove", MaTienIchSCRM.TI_Ahamove)]` controller-level + `[SBAuthorize]` mở; endpoint gắn thêm permission key + roles.
- Logic đăng đơn Ahamove (Crm_DangDonAhamove) nằm ở `OrderLogistic.Service/Generated/DeliveryUnitConnect/OrderService.cs`, KHÔNG ở controller này.
