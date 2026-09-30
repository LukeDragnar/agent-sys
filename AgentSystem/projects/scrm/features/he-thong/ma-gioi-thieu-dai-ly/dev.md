# Dev — Mã giới thiệu đại lý

> Service: **Core** (BE .NET) · FE module **quantri** (Angular 15 + ng-zorro). Trace registry: `registry/he-thong/MaGioiThieuController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Mã giới thiệu (góc Admin) | `/quantri/ma-gioi-thieu-admin` | `MaGioiThieuComponent` |
| Mã giới thiệu (góc NGT) | `/quantri/ma-gioi-thieu-ngt` | `MaGioiThieuUserComponent` |
| Tra đơn vị đã dùng mã (User) | `/quantri/ma-gioi-thieu-view-donvi-user` | `ViewDonviUserComponent` |
| Tra hợp đồng đã dùng mã (User) | `/quantri/ma-gioi-thieu-view-hopdong-user` | `ViewHopdongUserComponent` |
| Tra đơn vị đã dùng mã (Admin) | `/quantri/ma-gioi-thieu-view-donvi-admin` | `ViewDonviAdminComponent` |
| Tra hợp đồng đã dùng mã (Admin) | `/quantri/ma-gioi-thieu-view-hopdong-admin` | `ViewHopdongAdminComponent` |
| Form thêm/sửa mã (popup) | (modal) | `MaGioiThieuEditComponent` |

- Admin component extends `BaseListComponent`; search `{ keyword, isSuDung, idUser }`; cột grid: NGT, mã, %CK, %HH, số tháng cộng thêm, số lần áp dụng (đơn vị/NGT), trạng thái, số đơn vị, số hợp đồng, số tiền hoa hồng, số tiền hợp đồng (ban đầu/sau CK), ngày cập nhật.
- FE service: `services/core/magioithieu.service` (`TimTheoDieuKienAdmin`, `ChangeStatusOfUse`, `Xoa`, …).

## Endpoint / Controller (trace registry)
`MaGioiThieuController` (Core service, route `api/MaGioiThieu`, TienIch `TI_HeThong`):
| # | Hành động | Route | Quyền (key, role) |
|---|---|---|---|
| 1 | DS mã của tôi (NGT) | `POST .../GetListMaGioiThieuByUserLogin` | HT-NGT, HT-KT, HT-CSKH |
| 2 | Lọc (Admin) | `POST .../TimTheoDieuKienAdmin` | HT-AD, HT-KT, HT-CSKH |
| 3 | Lọc (NGT) | `POST .../TimTheoDieuKienUser` | HT-NGT, HT-KT, HT-CSKH |
| 4 | Tìm theo đơn vị/hợp đồng (User+Admin) | `POST .../{User\|Admin}TimKiemTheo{DonVi\|HopDong}` | User: HT-NGT…, Admin: HT-AD… |
| 5 | Thêm mới | `POST .../ThemMoi` | HT-AD, HT-KT, HT-CSKH |
| 6 | Xóa | `POST .../Xoa/{id}` | HT-AD, HT-KT, HT-CSKH |
| 7 | Chi tiết theo Id | `POST .../TimTheoId/{id}` | HT-AD, HT-KT, HT-CSKH |
| 8 | Kiểm tra mã (public) | `POST .../TimTheoMaGioiThieu` | `[AllowAnonymous]` |
| 9 | Đổi trạng thái sử dụng | `POST .../ChangeStatusOfUse/{id}` | HT-AD, HT-KT, HT-CSKH |
| 10 | Duyệt hoa hồng (1 hợp đồng) | `POST .../DuyetThanhToanHoaHong/{id}` | **HT-AD, HT-KT** |
| 11 | Duyệt hoa hồng hàng loạt | `POST .../DuyetThanhToanHoaHongMulti` | **HT-AD, HT-KT** |

## Entities
- `MaGioiThieu` — mã GT: mã, NGT chủ mã, `PhanTramChietKhau`, `PhanTramHoaHong`, `SoThangCongThem`, `SoLanApDungChoDonVi`, `SoLanApDungChoNguoiGioiThieu`, `IsSuDung`, liên kết đơn vị/hợp đồng, số tiền hoa hồng, số tiền hợp đồng ban đầu/sau CK.

## Luồng code
1. Admin list: `gridLoadData` → `TimTheoDieuKienAdmin(filter)`.
2. Toggle: `changeStatusOfUse(id)` → `ChangeStatusOfUse/{id}`; lỗi → revert cờ dòng.
3. Thêm/Sửa: modal `MaGioiThieuEditComponent` → `ThemMoi`/Update.
4. Duyệt hoa hồng: `DuyetThanhToanHoaHong(id, CurrentUser)` (id = id hợp đồng) → ripple cập nhật trạng thái thanh toán hoa hồng theo hợp đồng (kế toán hoa hồng).
5. Public: `TimTheoMaGioiThieu(payload, CurrentUser)` — kiểm tra mã khi đăng ký.

## Bug-prone / lưu ý (path:line)
- **Endpoint public không login** — `MaGioiThieuController.cs:103-108`: `TimTheoMaGioiThieu` `[AllowAnonymous]` truyền `CurrentUser` (có thể null) xuống service → service **phải null-safe**, nếu không sẽ NullReferenceException khi khách chưa đăng nhập nhập mã.
- **Method tên không khớp route** — `MaGioiThieuController.cs:33`: method `TimTheoDieuKienAsync` map route `TimTheoDieuKienAdmin` → khó grep, dễ nhầm.
- **POST cho thao tác đọc** — `MaGioiThieuController.cs:96-101`: `TimTheoId` dùng POST để đọc 1 bản ghi (lệch REST so với GET ở controller khác).
- **Phân quyền không đối xứng:** phần lớn action mở HT-AD/HT-KT/HT-CSKH; riêng duyệt hoa hồng (#10, #11) chỉ HT-AD/HT-KT — đúng ý đồ siết duyệt tiền, đừng "nới" nhầm.
- **4 endpoint tìm đơn vị/hợp đồng gần trùng** (User vs Admin) chỉ khác phạm vi quyền — sửa logic nhớ đồng bộ cả 4.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\Core.API\Controllers\MaGioiThieuController.cs` (anonymous `:103-108`, TimTheoDieuKienAsync `:33`, TimTheoId POST `:96-101`).
- FE list Admin: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\quantri\ma-gioi-thieu\ma-gioi-thieu.component.ts`.
- FE form: `...\ma-gioi-thieu\ma-gioi-thieu-edit\ma-gioi-thieu-edit.component.ts`.
- FE góc NGT: `...\ma-gioi-thieu\ma-gioi-thieu-user\ma-gioi-thieu-user.component.ts`.
- FE view đơn vị/hợp đồng: `...\ma-gioi-thieu\view-admin\*`, `...\ma-gioi-thieu\view-user\*`.
- FE routing: `...\scrm_frontend\src\app\pages\quantri\quantri-routing.module.ts:266` (`ma-gioi-thieu-admin`), `:270` (`ma-gioi-thieu-ngt`), `:274-287` (các view).
