---
controller: TienIchController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Quản lý tiện ích (admin): CRUD danh mục tiện ích; + Đơn vị > Mua tiện ích / Tiện ích đã mua: chợ tiện ích, mua ngay, lịch sử giao dịch, xóa tiện ích đã đăng ký"
muc_dich: "Hai mặt: (1) admin CRUD danh mục Tiện ích (RoleController-area), (2) đơn vị xem/mua/quản lý tiện ích đã mua (chợ tiện ích, mua ngay, lịch sử, xóa TI đã đăng ký)."
alias: ["TienIch", "tiện ích", "quản lý tiện ích", "mua tiện ích", "chợ tiện ích", "tiện ích đã mua", "marketplace tiện ích"]
auth: mixed
state_machine: []
deprecated: false
entities: [TienIch, HopDongTienIch]
stored_procs: []
depends: ["ITienIchService", "IHopDongTienIchService", "BaseController"]
cross_module_in: ["FE Hệ thống/Quản lý tiện ích (admin) gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/Xoa", "FE Đơn vị/Mua tiện ích gọi GetDanhSachTienIchFilter/MuaNgay/GetDanhSachTienIchDaMua/LichSuGiaoDich/XoaTiDaDangKy", "Service nội bộ gọi GetDanhSachTienIchDaMua/{idDonVi}/{IsConHan} qua PrivateSBAuthorize"]
cross_module_out: ["MuaNgay → tạo HopDongTienIch → bật/tắt tính năng (TienIch attribute) cho đơn vị; XoaTiDaDangKy → thu hồi quyền dùng tiện ích của đơn vị; là nguồn cấu hình [TienIch] cho mọi controller toàn hệ thống"]
---

# TienIchController — Quản lý / Chợ tiện ích

⚠️ Có **2 class trùng tên `TienIchController`** ở 2 namespace khác nhau (cùng tên file gốc `TienIchController.cs` ở 2 thư mục):
- **(A) Admin CRUD** — `Core.API.Areas.SanboxArea`, route `api/QuanTriArea/TienIch`, `[TienIch(..., TI_HeThong, true)]`. CRUD danh mục tiện ích.
- **(B) Đơn vị / chợ tiện ích** — `Core.API.Controllers`, route `api/TienIch`, `[TienIch(..., TI_Chung)]`. Mua/quản lý tiện ích đã mua.

Cả hai `partial`, kế thừa `BaseController`. auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (A) Danh sách tiện ích | (load lưới admin) | POST `api/QuanTriArea/TienIch/TimTheoDieuKien` | `TienIch_TimTheoDieuKien` (HT-AD,HT-KT) | mixed | — | Lọc danh mục tiện ích theo `TienIchFilterDto`. | EF | — |
| 2 | (A) Lấy tất cả | (dropdown) | GET `api/QuanTriArea/TienIch/LayTatCa` | `TienIch_LayTatCa` (HT-AD,HT-KT,HT-MB) | mixed | — | Trả toàn bộ tiện ích. | EF | — |
| 3 | (A) Xem chi tiết | (mở form) | GET `api/QuanTriArea/TienIch/TimTheoId/{id:Guid}` | `TienIch_TimTheoId` (HT-AD,HT-KT) | mixed | — | Map TienIch→TienIchDto. | EF | — |
| 4 | (A) Thêm mới | Thêm | POST `api/QuanTriArea/TienIch/ThemMoi` | `TienIch_ThemMoi` (HT-AD) | mixed | — | ModelState valid → map → Insert. | EF | — |
| 5 | (A) Cập nhật | Sửa | POST `api/QuanTriArea/TienIch/CapNhatThongTin` | `TienIch_CapNhatThongTin` (HT-AD) | mixed | — | ModelState valid → kiểm tra tồn tại → Update. | EF | — |
| 6 | (A) Xóa | Xóa | POST `api/QuanTriArea/TienIch/Xoa/{id:Guid}` | `TienIch_Xoa` (HT-AD) | mixed | — | Kiểm tra tồn tại + `CheckTienIchDaSuDung` (chặn nếu có đơn vị đang dùng) → Delete. | EF | — |
| 7 | (B) DS tiện ích (chợ) | (chợ tiện ích) | POST `api/TienIch/GetDanhSachTienIchFilter` | `TienIch_GetDanhSachTienIch` (DV-AD-TONG, HT-KT) | mixed | — | Lọc tiện ích bán cho đơn vị theo `TienIchFilterNhomDto`. | EF | — |
| 8 | (B) TI đã mua | (tab đã mua) | GET `api/TienIch/GetDanhSachTienIchDaMua/{IsConHan:int}` | `TienIch_GetDanhSachTienIchDaMua` (DV-AD-TONG/CN/MCN, DV-QLDV, HT-KT) | mixed | — | `IsConHan==1` → chỉ còn hạn; else → tất cả. | EF | — |
| 9 | (B) Mua ngay | Mua | POST `api/TienIch/MuaNgay` | `TienIch_MuaNgay` (DV-AD-TONG, HT-KT) | mixed | — | Tạo hợp đồng tiện ích cho list `MuaNgayTienIchDto` → kích hoạt tính năng cho đơn vị. | EF | — |
| 10 | (B) Lịch sử giao dịch | Lịch sử | POST `api/TienIch/LichSuGiaoDich` | — (login) | mixed | — | Trả lịch sử giao dịch tiện ích (qua IHopDongTienIchService) theo CurrentUser. | EF | — |
| 11 | (B) Xóa TI đã đăng ký | Xóa | GET `api/TienIch/XoaTiDaDangKy/{id:Guid}` | `TienIch_XoaTiDaDangKy` (DV-AD-TONG) | mixed | — | Thu hồi/hủy tiện ích đã đăng ký của đơn vị. | EF | Dùng HttpGet cho thao tác xóa (không idempotent về nghiệp vụ) |

## Endpoint nội bộ (private / service-to-service)
- [internal] **GET `api/TienIch/GetDanhSachTienIchDaMua/{idDonVi:Guid}/{IsConHan:int}`** `[PrivateSBAuthorize]` — service khác truyền `idDonVi` để lấy DS tiện ích đã mua của đơn vị bất kỳ (không cần token user).

## Quy tắc nghiệp vụ (gating)
1. (A) Xóa: `CheckTienIchDaSuDung(id) == true` → lỗi "Tiện ích đang có đơn vị sử dụng nên không thể xóa." → chặn xóa danh mục đang được mua. (Area/TienIchController.cs:123)
2. (B) GetDanhSachTienIchDaMua: `IsConHan == 1` → `service.GetDanhSachTienIchDaMua(true)` (còn hạn); else `false` (tất cả). (Controllers/TienIchController.cs:43)

## Quan hệ
- **Gọi sang:** ITienIchService (EF + CheckTienIchDaSuDung), IHopDongTienIchService (LichSuGiaoDich).
- **Dữ liệu (entities):** `TienIch` (danh mục), `HopDongTienIch` (hợp đồng đơn vị mua).
- **Ripple / đồng bộ:** `[TienIch(...)]` là cơ chế bật/tắt tính năng cho mọi controller toàn hệ thống; MuaNgay/XoaTiDaDangKy thay đổi quyền dùng tính năng của đơn vị.

## Luồng xuyên module
- Đơn vị **Mua ngay** → `HopDongTienIch` được tạo → tiện ích kích hoạt → các controller có `[TienIch("<code>")]` cho phép truy cập với đơn vị đó.
- **XoaTiDaDangKy** → thu hồi → đơn vị mất quyền dùng tính năng tương ứng.

## Lỗi đã biết / lưu ý kỹ thuật
- 2 class cùng tên `TienIchController` ở 2 namespace (Area `api/QuanTriArea/TienIch` vs Controllers `api/TienIch`) — dễ nhầm khi grep theo tên class; phân biệt bằng route.
- `XoaTiDaDangKy` dùng `[HttpGet]` cho thao tác xóa nghiệp vụ.
