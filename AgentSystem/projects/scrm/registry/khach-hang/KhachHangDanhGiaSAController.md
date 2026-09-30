---
controller: KhachHangDanhGiaController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/danhgia-chitiet-sa (Đánh giá khách hàng — biến thể admin/SandBox) — route api/SanboxArea/KhachHangDanhGiaController; phân biệt với KhachHangDanhGiaController non-SA (danhgia-chitiet-unit)"
muc_dich: "CRUD đánh giá chi tiết khách hàng ở cấp ADMIN/SandBox (endpoint ForSA, role HT-AD): tìm danh sách đánh giá chi tiết, thêm/sửa/xóa, lấy chi tiết theo Id"
alias: ["KhachHangDanhGia_SA", "Đánh giá khách hàng (admin)", "KH Đánh giá SandBox", "KhachHangDanhGiaController (SandBoxArea)"]
auth: public
state_machine: []
deprecated: false
entities: [KhachHangDanhGia, KhDanhGiaChiTiet]
stored_procs: []
depends: [IKhachHangDanhGiaService, BaseController]
cross_module_in: []
cross_module_out: []
---

# KhachHangDanhGiaController (SandBoxArea) — Đánh giá khách hàng (biến thể admin ForSA)

> **Biến thể SA/admin** trong `Customer.API.Areas.SandBoxArea`, route `api/SanboxArea/KhachHangDanhGiaController`.
> Cùng class name `KhachHangDanhGiaController` với controller non-SA (FE `danhgia-chitiet-unit`); node này tài liệu hóa bản SandBox (endpoint suffix `ForSA`, FE `danhgia-chitiet-sa`). Dùng chung service `IKhachHangDanhGiaService` với bản non-SA, tất cả action siết role **HT-AD**.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm danh sách đánh giá chi tiết | Lưới (danhgia-chitiet-sa) | `POST api/SanboxArea/KhachHangDanhGiaController/TimTheoDieuKienDsDetailForSA` | KhachHangDanhGia_TimTheoDieuKienDsDetailForSA (role **HT-AD**) | public | — | `TimTheoDieuKienDsDetailForSA(model, CurrentUser)` trả danh sách đánh giá theo filter + scope CurrentUser | EF (IKhachHangDanhGiaService) | — |
| 2 | Thêm mới đánh giá chi tiết | Nút "Thêm mới" | `POST .../InsertKhDanhGiaDetailForSA` | KhachHangDanhGia_InsertKhDanhGiaDetailForSA (HT-AD) | public | — | `InsertKhDanhGiaDetailForSA(model, CurrentUser)` | EF | — |
| 3 | Cập nhật đánh giá chi tiết | Nút "Lưu" | `POST .../UpdateKhDanhGiaDetailForSA` | KhachHangDanhGia_UpdateKhDanhGiaDetailForSA (HT-AD) | public | — | `UpdateKhDanhGiaDetailForSA(model, CurrentUser)` | EF | — |
| 4 | Lấy chi tiết đánh giá theo Id | (mở chi tiết) | `GET .../GetKhDanhGiaChiTietForSA/{id:Guid}` | KhachHangDanhGia_GetKhDanhGiaChiTietForSA (HT-AD) | public | — | `GetKhDanhGiaChiTiet(id)` — gọi method **không** suffix ForSA (dùng chung với bản non-SA) | EF | Method service dùng lại bản chung (`GetKhDanhGiaChiTiet`), không có biến thể ForSA riêng |
| 5 | Xóa đánh giá chi tiết | Nút "Xóa" | `POST .../DeleteKhDanhGiaChiTietForSA/{id:Guid}` | KhachHangDanhGia_DeleteKhDanhGiaChiTietForSA (HT-AD) | public | — | `DeleteKhDanhGiaChiTiet(id)` — gọi method **không** suffix ForSA (dùng chung) | EF | như #4, dùng lại method chung |

## Quan hệ
- **Gọi sang:** `IKhachHangDanhGiaService` (TimTheoDieuKienDsDetailForSA, InsertKhDanhGiaDetailForSA, UpdateKhDanhGiaDetailForSA, GetKhDanhGiaChiTiet, DeleteKhDanhGiaChiTiet).
- **Dữ liệu (entities):** `KhachHangDanhGia` / chi tiết `KhDanhGiaChiTiet` (qua DTO `ReqGetLstKhDanhGiaChiTiet` / `KhDanhGiaChiTietDto`).
- **Ripple / đồng bộ:** đánh giá KH gắn hồ sơ KH360 — dữ liệu đánh giá hiển thị ở tab đánh giá của khách hàng.

## Lỗi đã biết / lưu ý kỹ thuật
- `KhachHangDanhGiaController.cs:11` — attribute `TienIch` đăng ký dưới `MaTienIchSCRM.TI_HeThong`; namespace `...Areas.SandBoxArea`, route `api/SanboxArea/[controller]`; **trùng class name** với bản non-SA.
- `KhachHangDanhGiaController.cs:68` & `:80` — endpoint Get/Delete `...ForSA` thực chất gọi method service **không** có suffix ForSA (`GetKhDanhGiaChiTiet`, `DeleteKhDanhGiaChiTiet`) → cùng logic với bản non-SA, chỉ khác route/quyền.
- Tất cả action siết role **HT-AD** (admin hệ thống).
