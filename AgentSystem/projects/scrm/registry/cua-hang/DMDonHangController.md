---
controller: DMDonHangController
module: cua-hang
service: Order
man_hinh_ui: "nội bộ — không có màn quản trị riêng; endpoint dropdown public dùng để fill các combobox 'kiểu/loại danh mục đơn hàng' trên các màn tạo/lọc đơn (consumer chưa xác nhận chính xác màn nào)"
muc_dich: "Cung cấp dropdown danh mục đơn hàng (DMDonHang) theo kieuLoai cho FE; chỉ 1 endpoint công khai (AllowAnonymous)."
alias: ["dropdown danh mục đơn hàng", "DM đơn hàng theo kiểu/loại", "dropdownlist DMDonHang"]
auth: anonymous
state_machine: []
deprecated: false
entities: [DMDonHang]
stored_procs: []
depends: [Order]
cross_module_in: []
cross_module_out: []
---

# DMDonHangController — Dropdown danh mục đơn hàng (public)

> Controller `Order.API/Controllers/DMDonHangController.cs`, route `api/DMDonHang`. Permission ở controller-level bị **comment** (`//[SBAuthorize("Quản lý danh mục đơn hàng")]`); chỉ còn `[TienIch(... TI_Order)]`. Endpoint duy nhất gắn `[AllowAnonymous]` → **public không cần đăng nhập**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy dropdown DM đơn hàng theo kiểu/loại | (fill combobox) | GET `api/DMDonHang/DropDownList?kieuLoai={int}` | — (key cũ `DMDonHangController_DropDownList` đã comment) | anonymous | luôn | Gọi `_dmDonHangService.DropDownList(kieuLoai)`; map sang `DropdownCommon` (Id / Ten / Image=IdImage); trả `List<DropdownCommon>` | EF (DMDonHang) | **`[AllowAnonymous]`** — danh mục đơn hàng lộ public, không cần token; cân nhắc rủi ro lộ dữ liệu master |

## Quan hệ
- **Gọi sang:** `IDMDonHangService.DropDownList`.
- **Dữ liệu (entities):** `DMDonHang`.
- **Ripple / đồng bộ:** không. CRUD/quản trị DMDonHang nằm ở controller superadmin riêng (`DMDonHangController-QuanTri` — area SandBox).

## Lỗi đã biết / lưu ý kỹ thuật
- **Endpoint public** — `Controllers/DMDonHangController.cs:21` `[AllowAnonymous]` trên `DropDownList`; controller-level `[SBAuthorize]` đã bị comment (dòng 9) → không có lớp auth nào chặn.
- FE: `services/order/dmdonhang.service.ts` — `DMDonHangService` base url `api/DMDonHang`, nhưng các thao tác CRUD (TimTheoDieuKien/ThemMoi/CapNhat/Xoa) lại trỏ sang `api/SanboxArea/DMDonHang/...` (controller superadmin), không gọi `DropDownList` ở service này.
