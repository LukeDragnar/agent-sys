---
controller: ChuyenGiaController
module: he-thong
service: AuthenCustomer
man_hinh_ui: "App mobile khách hàng: màn chọn/đặt lịch với chuyên gia (kỹ thuật viên) — danh sách + chi tiết chuyên gia khi khách đặt lịch dịch vụ."
muc_dich: "Cho khách hàng (đã đăng nhập app) tìm danh sách chuyên gia theo điều kiện và xem chi tiết 1 chuyên gia."
alias: ["chuyên gia", "kỹ thuật viên app", "chọn chuyên gia đặt lịch", "danh sách chuyên gia"]
auth: public
state_machine: []
deprecated: false
entities: [User]
stored_procs: []
depends: ["IUserService (AuthenCustomer.Service) → thường gọi cross-service sang module nhân sự/booking để lấy chuyên gia"]
cross_module_in: ["App mobile khách → TimTheoDieuKienPrivate, TimTheoIdPrivate khi đặt lịch"]
cross_module_out: ["UserService có thể gọi sang Core/Booking để lấy dữ liệu chuyên gia (xem features nếu cần)"]
---

# ChuyenGiaController — Chọn chuyên gia khi đặt lịch (app khách)

Controller mỏng (2 endpoint), kế thừa `BaseController`, phục vụ app mobile khách hàng tra cứu **chuyên gia/kỹ thuật viên** để đặt lịch dịch vụ. Cả 2 endpoint đều `[SBAuthorizeCustomer]` (cần JWT khách) và truyền `CurrentCustomer` xuống `IUserService`. Logic thật nằm ở `UserService` (chia sẻ với `UserController`); controller chỉ forward.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm chuyên gia theo điều kiện | Danh sách chuyên gia (lọc) | POST `api/ChuyenGia/TimTheoDieuKienPrivate` | — | public (login) `[SBAuthorizeCustomer]` | Màn chọn chuyên gia khi đặt lịch | `UserService.TimTheoDieuKienPrivateAsync(filter, CurrentCustomer)` — trả danh sách chuyên gia theo `ChuyenGiaFilterReqDto`, scope theo đơn vị/chi nhánh của khách. | qua UserService (EF/cross-service) | — |
| 2 | Chi tiết 1 chuyên gia | Mở thẻ chuyên gia | GET `api/ChuyenGia/TimTheoIdPrivate/{id:Guid}` | — | public (login) `[SBAuthorizeCustomer]` | Click 1 chuyên gia | `UserService.TimTheoIdPrivateAsync(id, CurrentCustomer)` — chi tiết theo Id. | qua UserService | — |

## Quan hệ
- **Gọi sang:** `IUserService` (cùng service AuthenCustomer) — share logic với `UserController`.
- **Dữ liệu (entities):** `User` / dữ liệu chuyên gia (chi tiết lấy qua UserService).
- **Ripple / đồng bộ:** không trực tiếp; phục vụ luồng đặt lịch (`UserController.TaoLich`).

## Lỗi đã biết / lưu ý kỹ thuật
- Controller chỉ là lớp forward sang `UserService`; hành vi/edge-case nằm ở UserService (cần đọc UserService nếu trace sâu).
