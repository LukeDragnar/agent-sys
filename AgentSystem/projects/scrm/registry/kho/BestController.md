---
controller: BestController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI riêng. FE màn cấu hình/đăng đơn gọi để lấy metadata đối tác Best (mã + tên hiển thị)"
muc_dich: "Trả metadata tĩnh của ĐVVC Best Express (mã đối tác = EnumShippingUnitCode.Best, tên 'Best'). Controller mỏng phục vụ FE render dropdown/cấu hình đối tác giao vận."
alias: ["best", "best express", "đối tác best", "thông tin đối tác best"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in:
  - "FE màn cấu hình/đăng đơn ĐVVC (lấy metadata đối tác Best)"
cross_module_out: []
---

# BestController — Metadata đối tác giao vận Best Express

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | nội bộ FE | GET `api/Best/GetThongTinDoiTac` | `BestController_GetThongTinDoiTac` (roles: DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | public | - | Trả object tĩnh `{ GiaoHangDoiTacMa = EnumShippingUnitCode.Best, GiaoHangDoiTacTen = "Best" }` | hardcode | Mô tả permission ghi nhầm "Get thông tin EMS" (copy-paste từ EmsController) — `BestController.cs:21` |

## Quan hệ
- **Gọi sang:** không có (action thuần trả object tĩnh).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không. Kết nối/đăng đơn Best thực hiện ở `DeliveryUnitConnectController` (cấu hình) + `OrderService.CreateOrder` (đăng đơn).

## Lỗi đã biết / lưu ý kỹ thuật
- Controller mỏng (1 endpoint metadata), giá trị hardcode trong action.
- `[TienIch("BestController", "Best", MaTienIchSCRM.TI_Best)]` + `[SBAuthorize]` mở; endpoint gắn permission key + roles.
- `BestController.cs:21`: mô tả `[SBAuthorize]` ghi "Get thông tin EMS" thay vì "Get thông tin Best" — copy-paste lỗi, chỉ ảnh hưởng label permission, không ảnh hưởng hành vi.
