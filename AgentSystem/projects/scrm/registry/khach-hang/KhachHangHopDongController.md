---
controller: KhachHangHopDongController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/quanly-khachhang-detail > tab 'Hợp đồng' (hopdong-kh): danh sách + thêm/sửa/xóa hợp đồng khách hàng"
muc_dich: "CRUD hợp đồng khách hàng (thêm/sửa/xóa/tìm theo điều kiện/lấy theo id) + cung cấp danh sách hợp đồng private cho service khác"
alias: [KhachHangHopDong, "Hợp đồng khách hàng", "Hợp đồng KH", HopDongKh]
auth: mixed
state_machine: []
deprecated: false
entities: [KhachHangHopDong]
stored_procs: []
depends: [IKhachHangHopDongService, BaseDanhMucController]
cross_module_in: ["(private) service khác gọi GetDanhSachHopDongByKhachHangPrivate để lấy DS hợp đồng theo KH"]
cross_module_out: []
---

# KhachHangHopDong — Hợp đồng khách hàng

> Controller CRUD vừa phải, gắn 1 KH ↔ nhiều hợp đồng. Logic nghiệp vụ nằm trong service; controller chủ yếu forward + validate ModelState (cho endpoint private).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm hợp đồng theo điều kiện | Lưới tab "Hợp đồng" | `POST TimTheoDieuKienAsync` | KhachHangHopDong_TimTheoDieuKienAsync (nhiều role) | public | — | `TimTheoDieuKienAsync(reqDto)` → list hợp đồng theo filter | EF | — |
| 2 | Lấy hợp đồng theo id | (mở chi tiết HĐ) | `GET GetById/{id}` | KhachHangHopDong_GetById | public | — | `GetById(id)` | EF | — |
| 3 | Thêm hợp đồng | Nút "Thêm hợp đồng" | `POST ThemMoiHopDongKh` | KhachHangHopDong_ThemMoiHopDongKh | public | — | `ThemMoiHopDongKh(model)` | EF | — |
| 4 | Cập nhật hợp đồng | Nút "Lưu" (sửa HĐ) | `POST UpdateHopDongKh` | KhachHangHopDong_UpdateHopDongKh | public | — | `UpdateHopDongKh(model)` | EF | — |
| 5 | Xóa hợp đồng | Nút "Xóa" | `POST XoaKhHopDong/{id}` | KhachHangHopDong_XoaKhHopDong | public | — | `XoaKhHopDong(id)` | EF | — |

## Endpoint nội bộ (private / service-to-service)

- [internal] `POST GetDanhSachHopDongByKhachHangPrivate` (`[PrivateSBAuthorize]`, validate ModelState → MESS_Model_UnValidate nếu invalid) — service khác lấy danh sách hợp đồng của 1 KH qua `GetDataCrossServiceDto`.

## Quan hệ
- **Gọi sang:** `IKhachHangHopDongService` (toàn bộ CRUD + validate nghiệp vụ).
- **Dữ liệu (entities):** `KhachHangHopDong` (gắn theo idKhachHang).
- **Ripple / đồng bộ:** không có side-effect cross-module rõ ràng ngoài endpoint private cho phép service khác đọc DS hợp đồng.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `KhachHangHopDongController.cs:34,75` — `ThemMoiHopDongKh`/`UpdateHopDongKh` không kiểm tra ModelState ở controller (validate dồn vào service); chỉ endpoint private mới check ModelState (`:102`).
- Kế thừa `BaseDanhMucController<KhachHangHopDong, KhachHangHopDongDto>` → có thể lộ thêm endpoint CRUD generic từ base (cần xác nhận base có expose route công khai không).
