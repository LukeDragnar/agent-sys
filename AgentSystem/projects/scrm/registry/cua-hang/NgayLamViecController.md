---
controller: NgayLamViecController
module: cua-hang
service: Booking
man_hinh_ui: "Cửa hàng / Đặt lịch — cấu hình 'Ngày làm việc' (lịch làm việc theo 7 ngày trong tuần của chi nhánh: bật/tắt hoạt động + gán các khung giờ cho từng ngày)"
muc_dich: "Quản lý lịch làm việc theo ngày-trong-tuần (NgayLamViec) của chi nhánh: khởi tạo 7 ngày mặc định gắn khung giờ, lấy lịch (kèm danh sách khung giờ), và cập nhật trạng thái hoạt động + bộ khung giờ của 1 ngày."
alias: ["ngày làm việc", "lịch làm việc chi nhánh", "giờ làm việc theo ngày", "ngày trong tuần làm việc", "cấu hình ngày làm việc"]
auth: mixed
state_machine: []
deprecated: false
entities: [NgayLamViec, KhungGio, KhungGioNgayLamViec]
stored_procs: []
depends: [Booking, CauHinhService (RoleDuocPhepThaoTacNgayLamViecVaKhungGio), KhungGio (HTTP nội bộ KhoiTao/KhoiTaoByIdChiNhanh), Core (ChiNhanh/CheckChiNhanh), Redis/Cache (NgayLamViec_ChiNhanh)]
cross_module_in: [Luồng khởi tạo chi nhánh gọi nội bộ `KhoiTaoByIdChiNhanhPrivate`]
cross_module_out: [Cache NgayLamViec_ChiNhanh (clear khi cập nhật); KhungGio/KhoiTao gọi sang khi chưa có khung giờ; lịch đặt (calendar) dùng ngày làm việc + khung giờ để render slot]
---

# NgayLamViecController — Quản lý ngày làm việc chi nhánh

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy lịch làm việc (chi nhánh user) | load grid 7 ngày | GET `api/NgayLamViec/GetNgayLamViec?isDataShort=` | `NgayLamViec_GetNgayLamViec` | public | màn cấu hình ngày làm việc | List 7 `NgayLamViec` theo `DonVi+ChiNhánh`, sort `NgayTrongTuan`; **nếu chưa có → tự `KhoiTao`**; nếu `isDataShort=true` chỉ trả Id/NgayTrongTuan/Ten (cho dropdown); ngược lại kèm `ListIdKhungGio` + `ListKhungGio` (join `KhungGioNgayLamViec`) | EF (nolock) | - |
| 2 | Lấy lịch làm việc theo chi nhánh chỉ định (AD-DV) | (màn AD tổng) | GET `api/NgayLamViec/GetNgayLamViecByIdChiNhanh/{id}` | `NgayLamViec_GetNgayLamViecByIdChiNhanh` | public (DV-AD-TONG, DV-HR) | - | Như (1) cho `id` chi nhánh; nếu chưa có → `KhoiTaoByIdChiNhanh(id)` | EF (nolock) | - |
| 3 | Cập nhật ngày làm việc | "Lưu" (bật/tắt hoạt động + chọn khung giờ) | POST `api/NgayLamViec/CapNhatThongTin` | (không key — `[SBAuthorize]` rỗng) | public | nút Lưu per-row/ngày | Check role `RoleDuocPhepThaoTacNgayLamViecVaKhungGio`; validate (`Id`, `IsHoatDong`, `ListIdKhungGio` không rỗng + khung giờ thuộc `DonVi+ChiNhánh`); transaction: update `IsHoatDong`, insert mapping khung giờ mới + xóa mapping cũ; clear cache `NgayLamViec_ChiNhanh`; ghi log tác động | EF (transaction) + Cache | Permission key gốc đã comment (Controller:69), giờ chỉ cần đăng nhập + đúng role cấu hình |
| 4 | Khởi tạo ngày làm việc (chi nhánh user) | "Khởi tạo" | GET `api/NgayLamViec/KhoiTao` | `NgayLamViec_KhoiTao` | public | khi chi nhánh chưa có | Nếu chi nhánh đã có dữ liệu → lỗi "Chi nhánh đã tồn tại dữ liệu"; nếu chưa có khung giờ → gọi nội bộ `KhungGio/KhoiTao`; tạo 7 ngày trong tuần (`IsHoatDong=true`) × map mọi khung giờ; transaction | EF (transaction) + HTTP KhungGio | - |
| 5 | TimKiemTheoDieuKien | (stub) | POST `api/NgayLamViec/TimKiemTheoDieuKien` | `NgayLamViec_TimKiemTheoDieuKien` | public | - | **Trả `Ok()` rỗng** — chưa implement | (none) | Endpoint rỗng (Controller:26-29), giữ key nhưng vô tác dụng |

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize]`] **`KhoiTaoByIdChiNhanhPrivate`** (POST `api/NgayLamViec/KhoiTaoByIdChiNhanhPrivate`, `NgayLamViecReq`) — khởi tạo 7 ngày làm việc cho `IdDonVi+IdChiNhanh` truyền qua body (service-to-service, không token user). Nếu chi nhánh đã có dữ liệu → "Chi nhánh đã tồn tại dữ liệu". Nếu chưa có khung giờ → gọi nội bộ `KhungGio/KhoiTao` (lưu ý dùng token user nếu có). Dùng trong luồng tạo mới chi nhánh.

## Quy tắc nghiệp vụ (gating)
1. **Quyền cập nhật:** `CapNhat` gọi `CheckRoleThaoTac(userLogin, cauHinh.RoleDuocPhepThaoTacNgayLamViecVaKhungGio)` — không thuộc role → "Bạn không có quyền thực hiện thao tác này".
2. **Validate cập nhật** (dừng ở lỗi đầu): `Id` null → lỗi; bản ghi phải tồn tại theo `DonVi+ChiNhánh+Id`; `IsHoatDong` null → lỗi; `ListIdKhungGio` rỗng → "Khung giờ không được để trống!"; mọi khung giờ phải thuộc `DonVi+ChiNhánh` (số khớp) → lệch thì "Khung giờ không đúng!".
3. **Cập nhật mapping khung giờ = replace toàn bộ:** insert list mới + delete list cũ trong cùng transaction (không diff từng phần).
4. **Khởi tạo:** chỉ khi chi nhánh CHƯA có `NgayLamViec`; tạo đủ 7 ngày trong tuần (`EnumDayOfWeek`), mỗi ngày `IsHoatDong=true`, gán toàn bộ khung giờ hiện có của chi nhánh.
5. **Auto-khởi tạo khi đọc:** `GetNgayLamViec`/`GetNgayLamViecByIdChiNhanh` tự gọi `KhoiTao(...)` nếu chưa có dữ liệu (lazy init).

## Quan hệ
- **Gọi sang:**
  - `CauHinhService.GetAsync(DonViId)` — `RoleDuocPhepThaoTacNgayLamViecVaKhungGio`.
  - HTTP nội bộ Booking `api/KhungGio/KhoiTao` & `api/KhungGio/KhoiTaoByIdChiNhanh/{id}` (qua `_clientHelper`, token user) khi chi nhánh chưa có khung giờ.
  - HTTP `Core` `api/ChiNhanh/CheckChiNhanh/{id}` (trong `KhoiTaoByIdChiNhanh`) — xác minh chi nhánh thuộc đơn vị.
  - Cache (Redis) — `ClearCache_NgayLamViec_ChiNhanh`.
- **Dữ liệu (entities):** `NgayLamViec` (Ten, NgayTrongTuan, IsHoatDong, IdDonVi, IdChiNhanh); `KhungGioNgayLamViec` (mapping ngày ↔ khung giờ); `KhungGio`.
- **Ripple / đồng bộ:** `CapNhat` → clear cache `NgayLamViec_ChiNhanh` + ghi `LichSuTacDong` (UPDATE, `SCRM_BOOKING_NgayLamViec`); lịch đặt (calendar) dùng `NgayLamViec` (IsHoatDong + ngày trong tuần) × khung giờ để render slot.

## Luồng xuyên module (khởi tạo lịch làm việc kéo theo khung giờ)
```
KhoiTao / KhoiTaoByIdChiNhanh / KhoiTaoByIdChiNhanhPrivate:
  chi nhánh chưa có KhungGio → HTTP nội bộ Booking api/KhungGio/KhoiTao(ByIdChiNhanh)
    → tạo Sáng/Chiều/Tối
  → tạo 7 NgayLamViec (IsHoatDong=true) × map toàn bộ KhungGio (KhungGioNgayLamViec)
Calendar đặt lịch: GetConfigKhungGioByDayOfWeek → join NgayLamViec(hoạt động) × KhungGio(IsSuDung) theo ngày
```

## Lỗi đã biết / lưu ý kỹ thuật
- **`CapNhat` insert mapping mới TRƯỚC khi xóa mapping cũ** (Service:131-132) — trong transaction insert `khungGioNgayLamViec` rồi mới `DeleteAsync(khungGioLamViecOld)`; nếu khung giờ mới trùng khung giờ cũ, thứ tự insert-trước-delete có thể tạo bản ghi mapping trùng tạm thời / hoặc xóa nhầm bản vừa insert tùy cách `DeleteAsync` so khớp (cần xác nhận hành vi repository). Nên delete cũ trước, insert sau.
- **`TimKiemTheoDieuKien` là stub** — `return Ok();` (Controller:26-29), không gọi service.
- **`CapNhatThongTin` mất permission key** — `[SBAuthorize]` rỗng (key gốc comment, Controller:69) → chỉ cần đăng nhập + đúng `RoleDuocPhepThaoTac...`; không có key phân quyền riêng.
- **Lazy-init khi đọc** — `GetNgayLamViec` gọi `KhoiTao()` nếu rỗng (Service:501-508); thao tác đọc có side-effect tạo dữ liệu (kể cả gọi HTTP nội bộ khởi tạo khung giờ) → request "đọc" đầu tiên có thể chậm / ghi DB.
- **`userLogin` cache ở constructor** — service lấy `userLogin = GetUserLogin()` trong ctor (Service:66); với endpoint `[PrivateSBAuthorize]` (không token user) thì `userLogin` có thể rỗng/khác — `KhoiTaoByIdChiNhanhPrivate` dùng `req.IdDonVi/IdChiNhanh` từ body thay vì userLogin (đúng), nhưng HTTP nội bộ gọi `KhungGio/KhoiTao` vẫn truyền `userLogin.Token` (Service:433) → có thể null token trong context private.
- Code thật: BE controller `Booking/Booking.API/Controllers/NgayLamViecController.cs`; service `Booking/Booking.Service/Generated/NgayLamViecService.cs`; entity `Booking.DataContext/Entity/NgayLamViec.cs`, `KhungGioNgayLamViec.cs`.
