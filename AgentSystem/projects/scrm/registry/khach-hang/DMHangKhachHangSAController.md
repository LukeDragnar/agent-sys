---
controller: DMHangKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/dm-hangkh-admin (Danh mục hạng khách hàng — biến thể admin/SandBox) — route api/SanboxArea/DMHangKhachHangController; phân biệt với DMHangKhachHangController non-SA (đơn vị thường)"
muc_dich: "CRUD danh mục Hạng khách hàng ở cấp ADMIN/SandBox (các endpoint suffix _SA, role HT-AD): tìm theo điều kiện, lấy tất cả hạng theo đơn vị, thêm/sửa, lấy theo Id"
alias: ["DMHangKhachHang_SA", "Hạng khách hàng (admin)", "DM Hạng KH SandBox", "DMHangKhachHangController (SandBoxArea)"]
auth: public
state_machine: []
deprecated: false
entities: [DMHangKhachHang]
stored_procs: []
depends: [IDmHangKhachHangService, BaseController]
cross_module_in: []
cross_module_out: []
---

# DMHangKhachHangController (SandBoxArea) — Danh mục hạng khách hàng (biến thể admin _SA)

> **Biến thể SA/admin** trong `Customer.API.Areas.SandBoxArea`, route `api/SanboxArea/DMHangKhachHangController`.
> Cùng class name `DMHangKhachHangController` với controller non-SA (vùng đơn vị thường), NHƯNG khác area/route và tất cả endpoint có suffix `_SA` siết role **HT-AD** (admin hệ thống). Node này tài liệu hóa bản SandBox; xem node non-SA riêng cho bản đơn vị.
> Dùng chung service `IDmHangKhachHangService` với bản non-SA — chỉ khác bộ method `*_SA`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm hạng KH theo điều kiện | Lưới (dm-hangkh-admin) | `POST api/SanboxArea/DMHangKhachHangController/TimTheoDieuKien_SA` | DMHangKhachHang_TimTheoDieuKien_SA (role **HT-AD**) | public | — | ModelState invalid → `MESS_Model_UnValidate`; `TimTheoDieuKien_SA(grindInfo)` trả danh sách phân trang | EF (IDmHangKhachHangService) | — |
| 2 | Lấy tất cả hạng theo đơn vị | Dropdown/cấu hình | `GET .../GetAllRankByDonVi_SA` | DMHangKhachHang_GetAllRankByDonVi_SA (HT-AD) | public | — | `GetAllRankByDonVi_SA()` trả toàn bộ hạng KH theo đơn vị | EF | — |
| 3 | Thêm mới hạng KH | Nút "Thêm mới" | `POST .../ThemMoi_SA` | DMHangKhachHang_ThemMoi_SA (HT-AD) | public | — | ModelState invalid → lỗi; map DTO→`DMHangKhachHang`, `Insert_SA(entity)` | EF | — |
| 4 | Cập nhật hạng KH | Nút "Lưu" | `POST .../CapNhat_SA` | DMHangKhachHang_CapNhat_SA (HT-AD) | public | — | ModelState invalid → lỗi; map DTO→entity, `Update_SA(entity)` | EF | — |
| 5 | Lấy hạng KH theo Id | (mở chi tiết) | `GET .../TimTheoId_SA/{id:Guid}` | DMHangKhachHang_TimTheoId_SA (HT-AD) | public | — | `TimTheoId_SA(id)` trả chi tiết | EF | — |

## Quan hệ
- **Gọi sang:** `IDmHangKhachHangService` (bộ method `*_SA`: TimTheoDieuKien_SA, GetAllRankByDonVi_SA, Insert_SA, Update_SA, TimTheoId_SA).
- **Dữ liệu (entities):** `DMHangKhachHang` (map từ `DMHangKhachHangDto` qua AutoMapper).
- **Ripple / đồng bộ:** Hạng KH là danh mục cấu hình ảnh hưởng xếp hạng khách hàng (KH360, tích điểm) — thay đổi ở cấp admin lan tới logic xếp hạng đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMHangKhachHangController.cs:11` — namespace `Customer.API.Areas.SandBoxArea`, route `api/SanboxArea/[controller]`; **trùng class name** với controller non-SA → khi tra cứu bằng tên class dễ nhầm bản đơn vị thường.
- `DMHangKhachHangController.cs:60-66` — endpoint `ChangeCoCheXepHang_SA` (thay đổi cơ chế xếp hạng) đã bị **comment** → tính năng chưa bật.
- Tất cả endpoint siết role **HT-AD** (admin), khác bản non-SA dành cho user đơn vị.
