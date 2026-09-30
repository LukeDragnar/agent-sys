# Dev — Quản lý khuyến mãi

> Service: **Promotion** (BE .NET) · module FE Marketing. Việc **áp KM/trừ lượt** nằm ở `PromotionUpdateWithBillService` (do Order/OrderProcess gọi), KHÔNG ở controller này. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Component |
|---|---|
| Danh sách khuyến mãi | `khuyenmai.component` |
| Thêm/Sửa khuyến mãi | `khuyenmai-edit-v1` |

## Endpoint / Controller (trace registry)
**KhuyenMaiController** (`module marketing` · service `Promotion`):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tìm theo điều kiện (grid) | POST `TimTheoDieuKien` | `KhuyenMai_TimTheoDieuKien` (AD-TONG/CN/MCN, QLDV) — non-admin ép `IdChiNhanh` |
| Lấy tất cả KM còn hạn | POST `LayTatCa` | `KhuyenMai_LayTatCa` (AD-TONG/CN/MCN) |
| Thêm mới | POST `ThemMoi` | `KhuyenMai_ThemMoi` (AD-TONG) |
| Thêm mới V2 | POST `ThemMoiV2` | `[SBAuthorize()]` rỗng (chỉ login) — **khe hở quyền** |
| Chi tiết | GET `TimTheoId/{id}` | `KhuyenMai_TimTheoId` (AD-TONG/CN/MCN, QLDV) |
| Cập nhật | POST `CapNhat` | `KhuyenMai_CapNhat` (AD-TONG) |
| Cập nhật V2 | POST `CapNhatV2` | `[SBAuthorize()]` rỗng — **khe hở quyền** |
| Xóa | GET `Xoa/{id}` | `KhuyenMai_Xoa` (AD-TONG) — **GET cho hành động xóa** |
| KM khả dụng theo KH | POST `GetKhuyenMaiByIdKhachHang` (+`_V2`,`_DonHang`) | nhiều role |
| KM cho đơn (thu ngân/logistic) | POST `GetKhuyenMai_DonHang_V2` / `GetKhuyenMai_DonHang` | `[SBAuthorize()]` |
| Kiểm tra giới hạn áp dụng | POST `KiemTraGioiHanApDungTheoDon` | `[SBAuthorize()]` |
| Batch check KM sản phẩm | POST `KiemTraKMSanPhamBatch` | `[SBAuthorize()]` (Redis-first) |
| KM web booking | POST `GetListKhuyenMaiShowOnWebBooking` | `[AllowAnonymous]` |
| API private | POST `GetByIds`/`GetKhuyenMaiByIds`/`LayTatCa_Private`/`GetById_Private`/`GetPromotionPrivate` | `[PrivateSBAuthorize]` |
| Import combo KM (Excel) | `DownloadFileTemplateImportKhuyenMai`/`ImportKhuyenMai`/`DownloadDataWrongKhuyenMai/{id}` | `KhuyenMai_*ImportKhuyenMai*` (AD-TONG) |

**Áp/hoàn lượt** — `PromotionUpdateWithBillController` → `PromotionUpdateWithBillService`:
- `ThanhToanDonHang` → `PromotionUpdate(payload)` (áp KM + voucher + hoa hồng chung 1 transaction).
- `RollBackThanhToanDonHang` → `PromotionRollback(OrderRollBackBody)` (hoàn lượt theo IdDonHang).
- Biến thể logistic: `PromotionUpdate_Logistic` / `PromotionRollback_Logistic`.

## Entities
- **`KhuyenMai`** — master + điều kiện; `LoaiKhuyenMai` default `1` (KM sản phẩm) vs đơn hàng — phân nhánh rule chính theo field này.
- **`KhuyenMaiKhachHang`** — KM đã gán/đã dùng theo KH+đơn (`IsSuDung`, `IdDonHang`, `SoLuongDaDung`).
- **`KhuyenMaiKhachHangLichSu`** — lịch sử mỗi lần áp (nguồn đếm số-lần/KH).
- Liên quan: `Voucher*`, `HoaHongGioiThieu` (xử lý chung trong PromotionUpdateWithBill).

## Luồng code
1. CRUD ở `KhuyenMaiService` → mỗi Create/Update/Delete gọi **`XoaRedisKhuyenMai(donViId)`**.
2. Áp KM khi thanh toán (`PromotionUpdateWithBillService.cs:217-296`): trong `ExecutionStrategy` + transaction (retry) → insert `KhuyenMaiKhachHangLichSu`, update `KhuyenMaiKhachHang`, tăng `SoLuongDaDung` + `SoLuotDaApDung`. Catch → RollbackAsync.
3. Rollback (`:355-358`): `SoLuotDaApDung -= 1` (guard `> 0`), reset `IsSuDung=false`.

## Bug-prone / lưu ý (path:line)
- **Khe hở phân quyền V2** — `KhuyenMaiController.cs:65,126,178,238`: `ThemMoiV2`/`CapNhatV2`/`GetKhuyenMai_DonHang`… dùng `[SBAuthorize()]` **rỗng** (chỉ login) trong khi V1 yêu cầu AD-TONG. Nếu V2 lộ ra UI → mọi user login sửa được KM.
- **Global limit = 0 ≥ 0** — `KhuyenMaiService.cs:1415`: `(SoLuotDaApDung ?? 0) >= (SoLuotApDung ?? 0)` → nếu `SoLuotApDung` null thì hết lượt ngay. `IsApDungSoLuot` chỉ nên true khi có `SoLuotApDung`.
- **Per-customer limit null = chặn** — `KhuyenMaiService.cs:1425`: `SoLanSuDungDichVu == null` → coi như quá số lần. Đếm bằng `CountAsync` trên lịch sử (**nolock** → có thể đếm thiếu dưới race khi thanh toán đồng thời).
- **`GetListKhuyenMaiShowOnWebBooking` `[AllowAnonymous]`** (`cs:196`) — không lọc đơn vị qua token, dựa hoàn toàn vào body.
- **`GetKhuyenMaiByIds` trả `Ok()` thô** (`cs:220`) — không wrap `ResponseResult` → response shape khác.
- **`Xoa` dùng HTTP GET** (`cs:139`) — không idempotent/CSRF-safe.
- **Rollback idempotency** (`PromotionUpdateWithBillService.cs:355-358`): guard `> 0` cho lượt nhưng cần idempotency ở tầng gọi (OrderRollBackService) để không trừ lượt 2 lần.
- **God class** — `KhuyenMaiService.cs` ~6696 dòng; ~5 biến thể `GetKhuyenMaiByIdKhachHang` (_V1/_V2/_DonHang/OLD) + GetKhuyenMai_DonHang(_V2) song song → drift logic, sửa rule phải đồng bộ nhiều nhánh.
- **Toàn bộ KM/voucher/hoa hồng chung 1 transaction** (`:217-296`) — lỗi 1 phần rollback tất cả (đúng ý đồ nhưng coupling cao).

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\KhuyenMaiController.cs` (`:65,126,139,178,196,220,238`).
- BE service: `...\KhuyenMaiService.cs` (`:1415,1425`) · `...\PromotionUpdateWithBillService.cs` (`:217-296,355-358`).
- Registry: `projects/scrm/registry/marketing/KhuyenMaiController.md`.
