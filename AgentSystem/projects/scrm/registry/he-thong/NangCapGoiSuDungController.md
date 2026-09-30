---
controller: NangCapGoiSuDungController
module: he-thong
service: Core
man_hinh_ui: "Đơn vị > Nâng cấp gói sử dụng: xem gói hiện tại, chọn gói nâng cấp, áp mã giới thiệu rồi xác nhận nâng cấp"
muc_dich: "Nâng cấp / gia hạn gói sử dụng (subscription) của đơn vị: lấy gói hiện tại, danh sách gói khả dụng, thời hạn, kiểm tra mã giới thiệu và thực hiện nâng cấp."
alias: ["nâng cấp gói", "gia hạn gói", "gói sử dụng", "subscription", "NangCapGoiSuDung"]
auth: mixed
state_machine: []
deprecated: false
entities: [NangCapGoiSuDung]
stored_procs: []
depends: ["INangCapGoiSuDungService", "SBBaseController<NangCapGoiSuDung, NangCapGoiSuDungDto>", "AutoMapper", "MaGioiThieu (kiểm tra mã KM)"]
cross_module_in: ["FE màn nâng cấp gói gọi GetGoiSuDungHienTai/GetListGoiSuDung/NangCapSuDung", "Form áp mã giới thiệu gọi CheckSoLuotApDungMaGioiThieu"]
cross_module_out: ["NangCapSuDung đổi gói/quota đơn vị → ảnh hưởng quyền/hạn mức tính năng toàn hệ thống của đơn vị"]
---

# NangCapGoiSuDungController — Nâng cấp gói sử dụng đơn vị

Kế thừa `SBBaseController` nhưng KHÔNG dùng CRUD chuẩn — chỉ override để host các action nghiệp vụ nâng cấp gói. Controller-level `[TienIch("NangCapGoiSuDungController", TI_Chung)]`, từng method gắn key riêng (DV-AD-TONG, HT-CSKH...) → auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nâng cấp gói | Nâng cấp / Xác nhận | POST `api/NangCapGoiSuDung/NangCapSuDung` | `NangCapGoiSuDung_NangCapSuDung` (DV-AD-TONG, HT-CSKH) | public | — | ModelState valid → map DTO→entity → `NangCapSuDung(model, CurrentUser)` đổi gói/thời hạn cho đơn vị. | EF | — |
| 2 | Lấy gói hiện tại | (load màn) | GET `api/NangCapGoiSuDung/GetGoiSuDungHienTai` | `NangCapGoiSuDung_GetGoiSuDungHienTai` (DV-AD-TONG/CN/MCN, HT-CSKH) | public | — | Trả gói đang dùng của đơn vị theo CurrentUser. | EF | — |
| 3 | Danh sách gói | (dropdown gói) | GET `api/NangCapGoiSuDung/GetListGoiSuDung` | `NangCapGoiSuDung_GetListGoiSuDung` (DV-AD-*, HT-CSKH) | public | — | Trả danh sách gói có thể nâng cấp lên. | EF | — |
| 4 | Lấy thời hạn gói | (chọn thời hạn) | GET `api/NangCapGoiSuDung/GetThoiHanGoi` | — (login) | public | — | Trả các mốc thời hạn gói (KHÔNG gắn key → chỉ cần login). | EF | — |
| 5 | Kiểm tra mã giới thiệu | (áp mã KM) | POST `api/NangCapGoiSuDung/CheckSoLuotApDungMaGioiThieu` | `NangCapGoiSuDung_CheckSoLuotApDungMaGioiThieu` (DV-AD-TONG) | public | — | Kiểm tra số lượt còn áp dụng của mã giới thiệu trước khi nâng cấp. | EF | — |

## Quan hệ
- **Gọi sang:** INangCapGoiSuDungService, AutoMapper NangCapGoiSuDungDto↔NangCapGoiSuDung.
- **Dữ liệu (entities):** `NangCapGoiSuDung` (+ tham chiếu MaGioiThieu khi check mã).
- **Ripple / đồng bộ:** Nâng cấp đổi gói/quota đơn vị → ảnh hưởng giới hạn tính năng & quyền sử dụng module của đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `GetThoiHanGoi` (NangCapGoiSuDungController.cs:61) không gắn `SBAuthorize` key → mọi user đăng nhập đều gọi được, không gate theo role admin như các action còn lại.
