---
controller: EmsController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI riêng. FE màn cấu hình/đăng đơn gọi để lấy metadata đối tác EMS (mã + tên hiển thị)"
muc_dich: "Trả metadata tĩnh của ĐVVC EMS (mã đối tác = EnumShippingUnitCode.Ems, tên 'EMS'). Controller mỏng phục vụ FE render dropdown/cấu hình đối tác giao vận."
alias: ["ems", "đối tác ems", "thông tin đối tác ems", "giao hàng ems"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in:
  - "FE màn cấu hình/đăng đơn ĐVVC (lấy metadata đối tác EMS)"
cross_module_out: []
---

# EmsController — Metadata đối tác giao vận EMS

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đối tác | nội bộ FE | GET `api/Ems/GetThongTinDoiTac` | `EmsController_GetThongTinDoiTac` (roles: DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | public | - | Trả object tĩnh `{ GiaoHangDoiTacMa = EnumShippingUnitCode.Ems, GiaoHangDoiTacTen = "EMS" }` | hardcode | Có code cũ commented gọi `GetThongTinKetNoiDonVi` (ViettelPost) — chết, không dùng (`EmsController.cs:28-29`) |

## Quan hệ
- **Gọi sang:** không có (action thuần trả object tĩnh).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không. Kết nối/đăng đơn EMS thực hiện ở `DeliveryUnitConnectController` (cấu hình) + `OrderService.CreateOrder` (đăng đơn).

## Lỗi đã biết / lưu ý kỹ thuật
- Controller mỏng (1 endpoint metadata), giá trị hardcode trong action.
- `[TienIch("EmsController", "Ems", MaTienIchSCRM.TI_Ems)]` + `[SBAuthorize]` mở; endpoint gắn permission key + roles.
- Code commented (gọi GetThongTinKetNoiDonVi cho ViettelPost) là dấu vết template gốc, không còn dùng.
