# Dev — Người giới thiệu

> Gồm 2 controller khác service/module:
> - **NguonGioiThieuController** — service `Promotion`, module marketing (nguồn giới thiệu cấp đơn vị, gate `TI_HoaHongGioiThieu`).
> - **MaGioiThieuController** — service `Core`, module he-thong (mã referral cấp hệ thống, gate `TI_HeThong`).
> Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Mục đích |
|---|---|
| Marketing > Nguồn giới thiệu | CRUD nguồn giới thiệu khách hàng |
| Hệ thống > Mã giới thiệu | 2 góc nhìn: Admin (HT-AD) + NGT (người giới thiệu, HT-NGT) |

## Endpoint / Controller (trace registry)

**NguonGioiThieuController** — ⚠️ class nằm trong file `NguonKhachHangController.cs` (tên file ≠ tên class), route `NguonGioiThieu`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Danh sách (grid) | POST `TimTheoDieuKien` | `NguonGioiThieu_TimTheoDieuKien` (AD-TONG/CN/MCN) |
| Tìm theo keyword | POST `SearchByKeyword` | `[SBAuthorize()]` rỗng (key bị comment) — **khe hở** |
| Thêm / Cập nhật | POST `ThemMoi` / `CapNhat` | `NguonGioiThieu_ThemMoi` / `_CapNhat` |
| Xóa | GET `Xoa/{id}` | `NguonGioiThieu_Xoa` — **GET cho xóa** |
| Chi tiết | GET `TimTheoId/{id}` | `NguonGioiThieu_TimTheoId` |
| Check tồn tại | GET `CheckById/{id}` | `NguonGioiThieu_CheckById` (+QLDV) |
| Private (Order lấy nguồn) | GET `TimTheoId_Private/{id}`, POST `GetByIds` | `[PrivateSBAuthorize]` |

**MaGioiThieuController** (Core / he-thong) — tách 2 luồng Admin vs NGT:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Mã của tôi (NGT) | POST `GetListMaGioiThieuByUserLogin` | `MaGioiThieu_GetListMaGioiThieuByUserLogin` (HT-NGT, HT-KT, HT-CSKH) |
| Lọc Admin | POST `TimTheoDieuKienAdmin` | `MaGioiThieu_TimTheoDieuKienAdmin` (HT-AD, HT-KT, HT-CSKH) |
| Lọc NGT | POST `TimTheoDieuKienUser` | `MaGioiThieu_TimTheoDieuKienUser` (HT-NGT, HT-KT, HT-CSKH) |
| Tìm theo đơn vị/hợp đồng | POST `UserTimKiemTheoDonVi` / `UserTimKiemTheoHopDong` / `AdminTimKiemTheoDonVi` / `AdminTimKiemTheoHopDong` | 4 key tương ứng (User: HT-NGT…; Admin: HT-AD…) |
| Thêm mới | POST `ThemMoi` | `MaGioiThieu_ThemMoi` (HT-AD, HT-KT, HT-CSKH) |
| Xóa | POST `Xoa/{id}` | `MaGioiThieu_Xoa` |
| Chi tiết | POST `TimTheoId/{id}` | `MaGioiThieu_TimTheoId` — **POST cho thao tác đọc** |
| Kiểm tra mã (public) | POST `TimTheoMaGioiThieu` | `[AllowAnonymous]` |
| Đổi trạng thái sử dụng | POST `ChangeStatusOfUse/{id}` | `MaGioiThieu_ChangeStatusOfUse` |
| Duyệt hoa hồng (1) | POST `DuyetThanhToanHoaHong/{id}` | `MaGioiThieu_DuyetThanhToanHoaHong` (**chỉ HT-AD, HT-KT**) |
| Duyệt hoa hồng (nhiều) | POST `DuyetThanhToanHoaHongMulti` | `MaGioiThieu_DuyetThanhToanHoaHongMulti` (**chỉ HT-AD, HT-KT**) |

## Entities
- **`NguonGioiThieu`** — master nguồn; liên kết **`CauHinhHoaHongGioiThieu`** (cấu hình hoa hồng theo nguồn).
- **`MaGioiThieu`** — mã GT, NGT chủ mã, trạng thái sử dụng, liên kết đơn vị/hợp đồng, hoa hồng.

## Luồng code
1. Nguồn: CRUD ở NguonGioiThieuService; đơn gắn `IdNguonGioiThieu` + có cấu hình → `PromotionUpdateWithBillService` sinh `HoaHongGioiThieu` khi thanh toán.
2. Mã: `IMaGioiThieuService` CRUD + duyệt hoa hồng → cập nhật trạng thái thanh toán theo hợp đồng; kênh anonymous check mã khi đăng ký đơn vị/hợp đồng.

## Bug-prone / lưu ý (path:line)
- **Tên file lệch tên class** — `NguonKhachHangController.cs` chứa `NguonGioiThieuController` → dễ nhầm khi tìm file.
- **`SearchByKeyword` `[SBAuthorize]` rỗng** — `NguonGioiThieuController.cs:35` (key bị comment) → mọi user login tìm được, rò danh sách nguồn.
- **`Xoa` HTTP GET** — `NguonGioiThieuController.cs:72`.
- Nhiều method check `!ModelState.IsValid` cho action chỉ nhận `Guid id` từ route → check thừa.
- **`TimTheoMaGioiThieu` `[AllowAnonymous]`** — `MaGioiThieuController.cs:103-108`: public không login; truyền `CurrentUser` (có thể null) xuống service → service phải null-safe.
- **Tên method lệch route** — `MaGioiThieuController.cs:33` method `TimTheoDieuKienAsync` map route `TimTheoDieuKienAdmin` → khó grep.
- **`TimTheoId` dùng POST** cho thao tác đọc 1 bản ghi — `MaGioiThieuController.cs:96-101` (lệch REST).
- **Phân quyền không đối xứng (đúng ý đồ):** phần lớn action mở HT-AD/HT-KT/HT-CSKH; riêng duyệt hoa hồng chỉ HT-AD/HT-KT.

## File source (path:line)
- `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\NguonKhachHangController.cs` (class `NguonGioiThieuController`, `:35,72`).
- `...\MaGioiThieuController.cs` (`:33,96-101,103-108`).
- Registry: `projects/scrm/registry/marketing/NguonGioiThieuController.md`, `projects/scrm/registry/he-thong/MaGioiThieuController.md`.
