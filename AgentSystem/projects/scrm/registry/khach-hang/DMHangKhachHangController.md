---
controller: DMHangKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/dm-hang-khachhang (Danh mục hạng khách hàng) · khachhang-hangkhachhang (gán/xem hạng theo KH) · dm-thuhang-khachhang"
muc_dich: "CRUD hạng khách hàng (rank) theo đơn vị + lấy cấu hình hạng theo KH"
alias: ["Hạng khách hàng", "Rank khách hàng", "DM Hạng KH", "DMHangKhachHang"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMHangKhachHang]
stored_procs: []
depends: [IDmHangKhachHangService, BaseController]
cross_module_in: []
cross_module_out: []
---

# DMHangKhachHangController — Quản lý hạng khách hàng

> Controller danh mục hạng KH (rank). Không gắn `[SBAuthorize]` cấp class; có 1 action `[PrivateSBAuthorize]` → auth = **mixed**. Lưu ý có cặp method "OLD/mới" cho lấy cấu hình hạng theo KH.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm hạng theo điều kiện | Lưới "Hạng khách hàng" | `POST api/DMHangKhachHang/TimTheoDieuKien_DV` | DMHangKhachHang_TimTheoDieuKien_DV | public | — | ModelState invalid → lỗi; `TimTheoDieuKien_DV(grindInfo)` | EF | — |
| 2 | Lấy tất cả hạng theo đơn vị | Combo/lọc hạng | `GET api/DMHangKhachHang/GetAllRankByDonVi_DV` | `[SBAuthorize]` (chỉ đăng nhập) | public | — | `GetAllRankByDonVi_DV(CurrentUser)` | EF | Quyền nới lỏng — dòng role gốc (NV/QLDV/SALE...) bị comment, còn `[SBAuthorize]` trống |
| 3 | Thêm mới hạng | Nút "Thêm mới" | `POST api/DMHangKhachHang/ThemMoi_DV` | DMHangKhachHang_ThemMoi_DV | public | — | ModelState invalid → lỗi; map DTO→`DMHangKhachHang` rồi `Insert_DV` | EF | — |
| 4 | Lấy hạng theo Id | Mở chi tiết | `GET api/DMHangKhachHang/TimTheoId_DV/{id}` | DMHangKhachHang_TimTheoId_DV | public | — | `TimTheoId_DV(id)` | EF | — |
| 5 | Cập nhật hạng | Nút "Lưu" | `POST api/DMHangKhachHang/CapNhat_DV` | DMHangKhachHang_CapNhat_DV | public | — | ModelState invalid → lỗi; map DTO→entity rồi `Update_DV` | EF | — |
| 6 | Khởi tạo hạng mặc định | Nút "Khởi tạo" | `GET api/DMHangKhachHang/KhoiTao_DV` | DMHangKhachHang_KhoiTao_DV | public | — | `KhoiTao_DV()` tạo bộ hạng mặc định cho đơn vị | EF | — |
| 7 | Lấy cấu hình hạng theo KH (OLD) | (gọi từ hồ sơ KH) | `GET api/DMHangKhachHang/GetConfigHangByIdKh/{idKh}` | DMHangKhachHang_GetConfigHangByIdKh | public | — | `GetConfigHangByIdKh(idKh)` — mô tả ghi "(API_OLD)" | EF | Bản cũ; ưu tiên #8 (`GetCauHinhHangByIdKh`) |
| 8 | Lấy cấu hình hạng theo KH | Tab hạng trong hồ sơ KH | `GET api/DMHangKhachHang/GetCauHinhHangByIdKh/{idKh}` | DMHangKhachHang_GetCauHinhHangByIdKh | public | — | `GetCauHinhHangByIdKh(idKh, CurrentUser)` | EF | Trùng vai trò với #7 — chọn nhầm dễ ra dữ liệu khác |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GET api/DMHangKhachHang/GetAllRankByDonVi_DV_Private/{idDonVi}` (`[PrivateSBAuthorize]`) — lấy danh sách hạng theo đơn vị cho service khác (không qua token user).

## Quan hệ
- **Gọi sang:** `IDmHangKhachHangService`.
- **Dữ liệu (entities):** `DMHangKhachHang` (DTO map qua `AutoMapperConfiguration.Mapper`).
- **Ripple / đồng bộ:** cấu hình hạng được hồ sơ KH (KhachHang) đọc qua `GetCauHinhHangByIdKh`; không có cross-service write trực tiếp.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMHangKhachHangController.cs:38-39` (`GetAllRankByDonVi_DV`) — role gốc bị comment, action còn `[SBAuthorize]` trống → quyền lỏng hơn các action CRUD (DV-AD-TONG).
- `DMHangKhachHangController.cs:106-120` — tồn tại 2 endpoint gần trùng `GetConfigHangByIdKh` (OLD) và `GetCauHinhHangByIdKh`; bản OLD còn quyền/route nhưng đã bị đánh dấu "API_OLD".
- `DMHangKhachHangController.cs:70-76` (`ChangeCoCheXepHang_DV`) — đã **comment cứng**, không còn endpoint đổi cơ chế xếp hạng.
