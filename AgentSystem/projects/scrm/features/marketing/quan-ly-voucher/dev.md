# Dev — Quản lý voucher

> Service: **Promotion** (BE .NET) · module FE Marketing. Áp/hoàn voucher khi thanh toán đi qua `PromotionUpdateWithBillService` (giống KhuyenMai). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Mục đích |
|---|---|
| Danh sách voucher + tab lịch sử sử dụng | list, tab mã voucher, gia hạn |

## Endpoint / Controller (trace registry)
**VoucherController** (`module marketing` · service `Promotion`, gate `TI_Voucher`):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Danh sách (grid) | POST `TimTheoDieuKien` | `Voucher_TimTheoDieuKien` — non-admin ép `IdChiNhanh` |
| Lịch sử sử dụng | POST `LichSuSuDungVoucher` | `Voucher_LichSuSuDungVoucher` |
| Thêm mới | POST `ThemMoi` | `Voucher_ThemMoi` |
| Chi tiết | GET `TimTheoId/{id}` | `Voucher_TimTheoId` |
| Cập nhật | POST `CapNhat` | `Voucher_CapNhat` |
| Xóa | GET `Xoa/{id}` | `Voucher_Xoa` — **GET cho hành động xóa** |
| Khóa / Mở khóa | GET `Khoa/{id}` / `MoKhoa/{id}` | `Voucher_Khoa` / `_MoKhoa` — **GET đổi state** |
| Gia hạn | GET `GetDataGiaHanVoucher/{id}` + POST `GiaHanVoucher` | `Voucher_GetDataGiaHanVoucher` / `_GiaHanVoucher` |
| Tìm mã voucher | POST `TimKiemMaVoucher` | `Voucher_TimKiemMaVoucher` |
| Tải mã (Excel) | GET `DownloadMaVoucher/{id}` | `Voucher_DownloadMaVoucher` |
| Dropdown / autocomplete | GET `DropdownVoucher`, POST `VoucherAutoComplete` | `[SBAuthorize()]` (chỉ login) — **khe hở** |
| Voucher khả dụng theo điều kiện | POST `GetListWithConditions*` / `GetListVoucherByCodeAndId` / `GetListVoucherInfo` | `Voucher_GetListWithConditions*` |
| Xử lý thủ công mã | POST `XuLyThuCongMaVoucher/{maVoucher}/{actionId}` | `Voucher_XuLyThuCongMaVoucher` |
| Voucher gửi SMS/Zalo | GET `GetAllKhuyenMaiGuiSmsZaloDonVi` | `Voucher_GetAllKhuyenMaiGuiSmsZaloDonVi` (qua `_commonService`) |
| Job sinh/dọn mã | `GenVoucherCodeForJobMarketingCampaign*` / `GenVoucherCodeByBatch` / `DeleteVoucherCodeByIdNguonTao/{idNguonTao}/{idDonVi}` | `[PrivateSBAuthorize]` |

**Áp/hoàn voucher** — `PromotionUpdateWithBillService.ValidateThanhToanDonHang` / `ApplyVoucher` (qua `ThanhToanDonHang` / `RollBackThanhToanDonHang`).

## Entities
- **`Voucher`** + **`VoucherOption`** — cấu hình 1 đợt voucher (state `HoatDong`/`Khoa`).
- **`VoucherMa`** — từng mã (loại `EnumVoucher_Type`: MotMa đếm `SoLanSuDung`; NhieuMa cờ `IsDaDung`; Deposit/Deposit_Custom có `IdThamChieu`/`MaThamChieu`).
- **`VoucherLichSu`** — lịch sử mỗi lần dùng mã.
- **`VoucherDoiTuongApDung`** / **`VoucherDoiTuongKhongApDung`** — whitelist/blacklist đối tượng (KieuLoai = `EnumVoucher_KieuLoai_ApDung`).

## Luồng code
1. CRUD ở VoucherService; áp/hoàn ở `PromotionUpdateWithBillService` (chuỗi gate đối tượng — xem flow.md).
2. Job chiến dịch gọi `GenVoucherCode*` sinh mã batch theo `idNguonTao`; `DeleteVoucherCodeByIdNguonTao` dọn khi hủy chiến dịch.

## Bug-prone / lưu ý (path:line)
- **`_commonService` optional null** — `VoucherController.cs:23` inject `ICommonService commonService = null` → `GetAllKhuyenMaiGuiSmsZaloDonVi` (`:262`) NRE nếu DI không đăng ký.
- **GET cho hành động đổi/xóa state** — `VoucherController.cs:140,148,156`: `Xoa`/`Khoa`/`MoKhoa` dùng HTTP GET (không idempotent-safe, dễ trigger qua prefetch/crawler).
- **`XuLyThuCongMaVoucher` actionId magic number** — `cs:292`: nhận `actionId:int` raw từ route, không validate enum ở controller.
- **4 biến thể tra cứu drift** — `cs:57-104`: `GetListWithConditions`, `GetListWithConditionsCheckCode`, `GetListVoucherByCodeAndId`, `GetListVoucherInfo` khác nhau ở mức check điều kiện → sửa rule phải đồng bộ nhiều nhánh.
- **Khe hở quyền dropdown** — `cs:205,247`: `DropdownVoucher`/`VoucherAutoComplete` chỉ `[SBAuthorize()]` (login) trong khi API khác yêu cầu role AD → rò danh sách voucher.
- Hoàn mã khi rollback phụ thuộc tầng gọi idempotent (giống KhuyenMai) để không hoàn 2 lần.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\VoucherController.cs` (`:23,57-104,140,148,156,205,247,262,292`).
- BE service áp voucher: `...\PromotionUpdateWithBillService.cs`.
- Registry: `projects/scrm/registry/marketing/VoucherController.md` (+ `VoucherLichSuController.md`).
