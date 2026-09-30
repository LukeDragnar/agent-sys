---
controller: KhungGioController
module: cua-hang
service: Booking
man_hinh_ui: "Nội bộ — Area SandBox (TI_HeThong), không có UI người dùng. Chỉ phục vụ luồng khởi tạo khi tạo mới chi nhánh (route `api/SanboxArea/KhungGio/...`)."
muc_dich: "Bản SandBoxArea của KhungGioController — chỉ 1 endpoint khởi tạo bộ khung giờ mặc định (Sáng/Chiều/Tối) cho 1 chi nhánh khi chi nhánh mới được tạo (quyền KHOI_TAO)."
alias: ["khởi tạo khung giờ chi nhánh mới", "sandbox khung giờ", "khung giờ khi tạo chi nhánh"]
auth: internal
state_machine: []
deprecated: false
entities: [KhungGio]
stored_procs: []
depends: [Booking]
cross_module_in: [Luồng tạo mới chi nhánh (Core/Branch) gọi sang để khởi tạo khung giờ mặc định cho chi nhánh vừa tạo]
cross_module_out: [Tạo dữ liệu KhungGio Sáng/Chiều/Tối cho chi nhánh → ngày làm việc / đặt lịch của chi nhánh dùng tiếp]
---

# KhungGioController (SandBoxArea) — Khởi tạo khung giờ làm việc khi tạo mới chi nhánh

## Hành động (UI / công khai)
Không có UI. Controller nằm trong Area `SandBoxArea`, gắn `TienIchAttribute(..., MaTienIchSCRM.TI_HeThong, true)` (cờ `isArea=true`) — phục vụ luồng hệ thống/khởi tạo, không hiển thị cho người dùng cuối.

## Endpoint nội bộ (private / service-to-service)
- [internal — `[SBAuthorize("KhungGio_KhoiTaoByIdChiNhanh", ..., "KHOI_TAO")]`] **`KhoiTaoByIdChiNhanh`** (GET `api/SanboxArea/KhungGio/KhoiTaoByIdChiNhanh/{id}`) — quyền `KHOI_TAO` (role đặc thù luồng khởi tạo, không phải role nghiệp vụ thường). Gọi `_service.KhoiTaoByIdChiNhanh(id)`:
  - Nếu chi nhánh `id` ĐÃ có khung giờ → trả `false` (không tạo lại).
  - Nếu chưa → tạo Sáng (07:00-12:00), Chiều (12:00-18:00), Tối (18:00-23:00), `IsSuDung=true`, `IdChiNhanh=id`, `IdDonVi = userLogin.DonViId`; trả `true`.

> Lưu ý: dùng CHUNG service method `KhungGioService.KhoiTaoByIdChiNhanh` với endpoint cùng tên ở controller thường (`api/KhungGio/KhoiTaoByIdChiNhanh/{id}`, quyền `DV-AD-TONG`). Khác biệt nằm ở route/area + permission key role (`KHOI_TAO` vs `DV-AD-TONG`), KHÔNG khác logic.

## Quan hệ
- **Gọi sang:** không gọi service ngoài (chỉ insert `KhungGio` qua repository).
- **Dữ liệu (entities):** `KhungGio` (Ten, GioBatDau, GioKetThuc, IsSuDung, IdChiNhanh, IdDonVi).
- **Ripple / đồng bộ:** dữ liệu khung giờ mới tạo là tiền đề cho khởi tạo `NgayLamViec` của chi nhánh và slot đặt lịch. (Method service này KHÔNG clear cache `KhungGio_ChiNhanh` — khác `ChangeIsSuDung`/`ThemMoi`.)

## Lỗi đã biết / lưu ý kỹ thuật
- **`IdDonVi` lấy từ `userLogin.DonViId`** (KhungGioService.cs:320) — không từ chi nhánh đầu vào; nếu gọi trong context khởi tạo mà `userLogin` không khớp đơn vị của `id` chi nhánh → khung giờ gán sai đơn vị. Cần đảm bảo caller chạy đúng context đơn vị.
- **Không clear cache sau khởi tạo** — `KhoiTaoByIdChiNhanh` không gọi `ClearCache_KhungGio_ChiNhanh`; nếu chi nhánh đã có cache rỗng trước đó thì dữ liệu mới chưa phản ánh tới khi cache hết hạn / có thao tác clear khác.
- Bản SandBox và bản thường dùng chung 1 method service → mọi thay đổi logic ở `KhungGioService.KhoiTaoByIdChiNhanh` ảnh hưởng cả 2 route.
- Code thật: BE controller `Booking/Booking.API/Areas/SandBoxArea/KhungGioController.cs`; service `Booking/Booking.Service/Generated/KhungGioService.cs` (`KhoiTaoByIdChiNhanh`, dòng 307-344).
