---
controller: UpdateListDomainCorsController
module: cua-hang
service: Booking
man_hinh_ui: "nội bộ — không có UI; tiện ích quản trị (QuanTriArea/SandBoxArea) reset danh sách domain CORS đang cache trong process. (LƯU Ý: trùng tên với UpdateListDomainCorsController của PreOrder — file PreOrder ở UpdateListDomainCorsController.md; file này là bản Booking.)"
muc_dich: "Reset cache danh sách domain CORS của service Booking: xóa `GlobalConfig.ListDomainCors` để lần kiểm tra origin tiếp theo nạp lại từ DB (sau khi admin thêm/sửa domain được phép)."
alias: ["update cors", "reset domain cors", "làm mới list cors booking"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [SB.Core (GlobalConfig.ListDomainCors, CorsHelper), CoreConnectStr (DB đọc origin allowed)]
cross_module_in: [Admin hệ thống gọi sau khi cập nhật danh sách domain CORS để service Booking nạp lại]
cross_module_out: [Reset cache CORS trong process Booking — middleware CORS lần sau đọc lại từ DB]
---

# UpdateListDomainCorsController (Booking) — Reset danh sách domain CORS (nội bộ)

> Trùng tên controller với `UpdateListDomainCorsController` của PreOrder (xem `UpdateListDomainCorsController.md`). Node này là bản service **Booking** (`Booking.API/Areas/SandBoxArea/UpdateListDomainCorsController.cs`).

## Hành động (UI / công khai)
Không có UI. Controller 1 endpoint quản trị, route area `api/QuanTriArea/UpdateListDomainCors`, TienIch `MaTienIchSCRM.TI_HeThong` (ẩn). Controller-level `[SBAuthorize]` (yêu cầu đăng nhập).

## Endpoint nội bộ (private / service-to-service)
- [internal — `[SBAuthorize("DomainCors_UpdateCors", role "HT-AD")]`] **`UpdateCors`** (POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors`) — gán `GlobalConfig.ListDomainCors = null` (clear cache), đọc connection string `CoreConnectStr`, gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", ctr)` (kích hoạt nạp lại list từ DB); luôn trả `true`. Chỉ role `HT-AD` (admin hệ thống) dùng.

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed` (đọc DB qua `CoreConnectStr`); `GlobalConfig.ListDomainCors` (static cache trong process).
- **Dữ liệu (entities):** không truy cập entity nghiệp vụ.
- **Ripple / đồng bộ:** sau khi clear, middleware CORS của Booking nạp lại danh sách domain được phép từ DB.

## Lỗi đã biết / lưu ý kỹ thuật
- **Kết quả check bị bỏ** — `UpdateListDomainCorsController.cs:28`: gọi `CorsHelper.IsOriginAllowed(...)` với origin hardcode `http://localhost:4200` chỉ để trigger nạp lại; giá trị `rs` không được dùng (chỉ warm cache). Endpoint luôn trả `true`.
- **`async` không `await`** — method khai báo `async Task` nhưng không `await` gì (`CorsHelper.IsOriginAllowed` đồng bộ) → cảnh báo compiler, không sai logic.
- **Reset chỉ tác động process hiện tại** — `GlobalConfig.ListDomainCors` là static in-process; nếu Booking chạy nhiều instance/pod thì phải gọi trên từng instance mới clear hết.
- Code thật: BE `Booking/Booking.API/Areas/SandBoxArea/UpdateListDomainCorsController.cs`.
