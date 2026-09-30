# Danh mục khách hàng (gốc — cấp hệ thống) — Kỹ thuật

> Service: **Customer** (BE .NET). Hai controller biến thể **_SA / SandBoxArea**. Chỉ dành cho dev.
> Registry: `projects/scrm/registry/khach-hang/DMNhomKhachHangSAController.md`, `projects/scrm/registry/khach-hang/DMHangKhachHangSAController.md`.

## Màn hình & route (FE)
| Màn hình | Route phía FE (admin) | Ghi chú |
|---|---|---|
| Danh mục nhóm khách hàng (admin) | `Customer/dm-nhomkhachhang-admin` | gọi API area `api/SanboxArea/DMNhomKhachHangController` |
| Danh mục hạng khách hàng (admin) | `Customer/dm-hangkh-admin` | gọi API area `api/SanboxArea/DMHangKhachHangController` |

## Endpoint / Controller (trace registry)

**Nhóm KH (admin)** — `DMNhomKhachHangController` (SandBoxArea), namespace `Customer.API.Areas.Controllers` (file trong thư mục `Areas/SandBoxArea`). Controller-level `[SBAuthorize()]` **không** kèm role; action không truyền role string → mức public (chỉ đăng nhập):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tìm theo điều kiện | `POST api/SanboxArea/DMNhomKhachHangController/TimTheoDieuKien_SA` | `DMNhomKhachHang_TimTheoDieuKienSA` |
| Thêm mới | `POST .../ThemMoi_SA` | `DMNhomKhachHang_ThemMoi_SA` |
| Đổi cờ Sử dụng | `GET .../ChangeSuDung_SA/{id:Guid}` | `DMNhomKhachHang_ChangeSuDung_SA` |
| Đổi cờ xét nâng nhóm | `GET .../ChangeChoPhepXetNangNhom_SA/{id:Guid}` | `DMNhomKhachHang_ChangeChoPhepXetNangNhom_SA` |
| Lấy theo Id | `GET .../TimTheoId_SA/{id:Guid}` | `DMNhomKhachHang_TimTheoId_SA` |
| Cập nhật | `POST .../CapNhat_SA` | `DMNhomKhachHang_CapNhat_SA` |

**Hạng KH (admin)** — `DMHangKhachHangController` (SandBoxArea), namespace `Customer.API.Areas.SandBoxArea`, tất cả endpoint siết role **HT-AD**:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tìm theo điều kiện | `POST api/SanboxArea/DMHangKhachHangController/TimTheoDieuKien_SA` | `DMHangKhachHang_TimTheoDieuKien_SA` (HT-AD) |
| Lấy tất cả hạng theo đơn vị | `GET .../GetAllRankByDonVi_SA` | `DMHangKhachHang_GetAllRankByDonVi_SA` (HT-AD) |
| Thêm mới | `POST .../ThemMoi_SA` | `DMHangKhachHang_ThemMoi_SA` (HT-AD) |
| Cập nhật | `POST .../CapNhat_SA` | `DMHangKhachHang_CapNhat_SA` (HT-AD) |
| Lấy theo Id | `GET .../TimTheoId_SA/{id:Guid}` | `DMHangKhachHang_TimTheoId_SA` (HT-AD) |

## Entities
- **`DMNhomKhachHang`** — nhóm khách hàng (qua DTO `DMNhomKhachHangFilterDto` / `DMNhomKhachHangCUDto`). Có cờ Sử dụng và cờ "cho phép xét nâng nhóm khi thanh toán".
- **`DMHangKhachHang`** — hạng khách hàng (map từ `DMHangKhachHangDto` qua AutoMapper).

## Luồng code
- Cả 2 controller _SA **dùng chung service** với bản non-SA (đơn vị): `IDMNhomKhachHangService` và `IDmHangKhachHangService`, chỉ khác bộ method có suffix `*_SA`.
- Nhóm KH: `ChangeChoPhepXetNangNhom_SA` đảo cờ tự-xét; khi TRUE → logic tự động xét nâng nhóm KH chạy tại thời điểm thanh toán (ripple sang luồng bán hàng/thanh toán).

## Bug-prone / lưu ý
- **`DMNhomKhachHangController.cs:8`** — namespace `Customer.API.Areas.Controllers` (KHÔNG phải `...SandBoxArea`) dù file đặt trong thư mục `Areas/SandBoxArea` → dễ nhầm khi grep theo namespace.
- **`DMNhomKhachHangController.cs:72-78`** — endpoint `Xoa_SA` **đã bị comment** → không hỗ trợ xóa nhóm qua API admin này. Đừng tin route xóa còn sống.
- **Phân quyền lệch giữa 2 màn**: action Nhóm KH `*_SA` **không** truyền role string (mức public, chỉ kiểm soát qua permission key), trong khi Hạng KH siết HT-AD. *Cần xác nhận* đây có phải chủ ý (khe hở quyền tiềm tàng cho màn nhóm).
- **`DMHangKhachHangController.cs:11`** — trùng class name `DMHangKhachHangController` với controller non-SA (đơn vị); khác area/route. Tra cứu theo tên class dễ nhầm bản đơn vị.
- **`DMHangKhachHangController.cs:60-66`** — endpoint `ChangeCoCheXepHang_SA` đã bị **comment** → tính năng đổi cơ chế xếp hạng chưa bật.

## File source (path:line)
- BE Nhóm KH (admin): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Customer\Customer.API\Areas\SandBoxArea\DMNhomKhachHangController.cs:8` (namespace), `:72-78` (Xoa_SA comment).
- BE Hạng KH (admin): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Customer\Customer.API\Areas\SandBoxArea\DMHangKhachHangController.cs:11` (namespace/route), `:60-66` (ChangeCoCheXepHang_SA comment).
- Registry nodes: `projects/scrm/registry/khach-hang/DMNhomKhachHangSAController.md`, `.../DMHangKhachHangSAController.md`.
