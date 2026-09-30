---
controller: CommonController
module: cua-hang
service: Order
man_hinh_ui: "nội bộ — không có màn riêng; cung cấp dropdown/enum cho nhiều màn Order (combobox kiểu/loại DM đơn hàng, kiểu máy in, danh sách trạng thái đơn/dịch vụ/khách). FE: services/order/common.service.ts"
muc_dich: "Các helper chung của service Order: dropdown DM đơn hàng & kiểu máy in, và liệt kê enum trạng thái (đơn hàng / dịch vụ / khách) để FE bind."
alias: ["common order", "dropdown kiểu loại đơn hàng", "list trạng thái đơn hàng", "list trạng thái dịch vụ", "list trạng thái khách", "enum trạng thái order"]
auth: public
state_machine: [DonHangTrangThai.ChuaThanhToan, DonHangTrangThai.DaThanhToan, DonHang_TrangThaiKhach.KhachCho, DonHang_TrangThaiKhach.DangPhucVu, DonHang_TrangThaiKhach.HoanThanh, DonHang_TrangThaiKhach.Huy]
deprecated: false
entities: [DMDonHang]
stored_procs: []
depends: [Order, ElasticSearch/Booking (gián tiếp qua CommonService cho các method khác)]
cross_module_in: [internal — GetNgayLenDonGanNhat gọi service-to-service (PrivateSBAuthorize)]
cross_module_out: []
---

# CommonController (Order) — Dropdown & enum trạng thái chung

> Controller `Order.API/Controllers/Common/CommonController.cs`, route **`api/Common`**. Controller-level `[SBAuthorize("Quản lý chung")]` đã comment; mỗi endpoint dropdown/enum gắn `[SBAuthorize()]` rỗng → **public chỉ-cần-đăng-nhập**. Một endpoint `GetNgayLenDonGanNhat` gắn `[PrivateSBAuthorize()]` → **internal service-to-service**. (Có node trùng tên cho service PreOrder ở `CommonController.md` (rỗng/scaffold) — đây là bản service Order, đặt tên `-Order` để tránh đè.)

## Hành động (UI / công khai)
| # | Hành động | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn |
|---|---|---|---|---|---|---|
| 1 | Dropdown kiểu/loại DM đơn hàng | GET `api/Common/DropdownDMDonHang_KieuLoai` | — (`[SBAuthorize()]` rỗng) | public | `DropdownDMDonHang_KieuLoai()` — list kiểu/loại để bind combobox | EF/enum |
| 2 | Dropdown kiểu loại máy in | GET `api/Common/Dropdown_KieuLoai_MayIn` | — (`[SBAuthorize()]` rỗng) | public | `Dropdown_KieuLoai_MayIn()` | enum |
| 3 | DM đơn hàng theo kiểu/loại | GET `api/Common/DMDonHangByKieuLoai/{kieuLoai}` | — (`[SBAuthorize()]` rỗng) | public | `DMDonHangByKieuLoai(kieuLoai)` — list DMDonHang lọc theo kiểu/loại | EF (DMDonHang) |
| 4 | List trạng thái dịch vụ | GET `api/Common/ListTrangThaiDichVu` | — (`[SBAuthorize()]` rỗng) | public | Trả `List<EnumItem>` — **thực tế dùng enum `DonHang_TrangThaiKhach`**: KhachCho / DangPhucVu / HoanThanh / Huy (xem known-issue) | enum |
| 5 | List trạng thái đơn hàng | GET `api/Common/ListTrangThaiDonHang` | — (`[SBAuthorize()]` rỗng) | public | Trả `List<EnumItem>` từ enum `DonHangTrangThai`: ChuaThanhToan / DaThanhToan | enum |
| 6 | List trạng thái khách | GET `api/Common/ListTrangThaiKhach` | — (`[SBAuthorize()]` rỗng) | public | Trả `List<EnumItem>` từ enum `DonHang_TrangThaiKhach`: KhachCho / DangPhucVu / HoanThanh / Huy | enum |

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize()]`] **`GetNgayLenDonGanNhat`** (POST, payload `NgayLenDonGanNhatPayload`) — validate ModelState; gọi `_commonService.GetNgayLenDonGanNhat(payload)`. Service-to-service (cross-service), không phải UI.

## State machine (enum tham chiếu)
- **`DonHangTrangThai`** (trạng thái thanh toán đơn): `ChuaThanhToan`, `DaThanhToan`. → bind ở `ListTrangThaiDonHang`.
- **`DonHang_TrangThaiKhach`** (trạng thái phục vụ khách): `KhachCho`, `DangPhucVu`, `HoanThanh`, `Huy`. → bind ở `ListTrangThaiKhach` **và** `ListTrangThaiDichVu`.
> Tên hiển thị lấy từ `.ToDescription()` của enum (Description attribute) — không hardcode tại đây.

## Quan hệ
- **Gọi sang:** `ICommonService` (DropdownDMDonHang_KieuLoai / Dropdown_KieuLoai_MayIn / DMDonHangByKieuLoai / ListTrangThaiDichVu / ListTrangThaiDonHang / ListTrangThaiKhach / GetNgayLenDonGanNhat). CommonService còn gọi nhiều service ngoài (Booking/ES) cho các method khác không expose ở controller này.
- **Dữ liệu (entities):** `DMDonHang` (qua dropdown); còn lại là enum tĩnh.
- **Ripple / đồng bộ:** không (read-only helpers).

## Lỗi đã biết / lưu ý kỹ thuật
- **`ListTrangThaiDichVu` trả nhầm enum** — `Order.Service/Generated/Common/CommonService.cs:473` build list từ `DonHang_TrangThaiKhach` (KhachCho/DangPhucVu/HoanThanh/Huy), **trùng hệt** `ListTrangThaiKhach` (CommonService.cs:527); không có enum "trạng thái dịch vụ" riêng. Nếu FE kỳ vọng trạng thái dịch vụ khác → đây là bug copy enum.
- Tất cả endpoint dropdown/enum chỉ `[SBAuthorize()]` rỗng (login-only, không permission key cụ thể) → bất kỳ user đăng nhập đều gọi được.
- FE: `services/order/common.service.ts` wrap đủ 6 endpoint public (DMDonHangByKieuLoai, DropdownDMDonHang_KieuLoai, Dropdown_KieuLoai_MayIn, ListTrangThaiDichVu/DonHang/Khach).
