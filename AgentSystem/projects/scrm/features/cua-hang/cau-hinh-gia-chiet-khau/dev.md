# Dev — Cấu hình giá & chiết khấu

> 2 controller: **CauHinhDonHangMatHangMacDinhController** (service WarehouseProduct — ACTIVE) + **DonHangCauHinhGiamGiaController** (service Order — DEPRECATED, vô hiệu toàn bộ). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Cấu hình mặt hàng mặc định | `cauhinh-mathang-macdinh` | `CauhinhMathangMacdinhComponent` (list) + modal `CauhinhMathangMacdinhEditComponent` |

- FE service: `services/warehouseproduct/cauhinh-donhang-mathang-macdinh.service.ts`.

## Endpoint/Controller (trace registry)

### CauHinhDonHangMatHangMacDinhController (ACTIVE)
Node: `registry/cua-hang/CauHinhDonHangMatHangMacDinhController.md`. Service WarehouseProduct, `auth: mixed`.

| Quyền (key) | Route | Mục đích |
|---|---|---|
| `CauHinhDonHangMatHangMacDinhController_TimTheoDieuKien` | POST `api/CauHinhDonHangMatHangMacDinh/TimTheoDieuKien` | List (lọc đơn vị+chi nhánh, enrich tên) |
| `..._ThemMoi` | POST `…/ThemMoi` | Thêm bộ cấu hình (transaction) |
| `..._CapNhat` | POST `…/CapNhat` | Sửa bộ cấu hình |
| `..._TimTheoId` | POST `…/TimTheoId/{id}` | Lấy 1 bộ (deserialize CauHinhJson) |
| `..._Xoa` | POST `…/Xoa/{id}` | Xóa cứng |
| `..._ThayDoiTrangThaiSuDung` | POST `…/ThayDoiTrangThaiSuDung/{id}` | Toggle SuDung |
| `..._ThayDoiTrangThaiMacDinh` | POST `…/ThayDoiTrangThaiMacDinh/{id}` | Toggle IsMacDinh (mặc định duy nhất) |

Endpoint phục vụ POS / public:
- `LayCauHinhHienTai` (POST, `[SBAuthorize()]` login) — POS lấy cấu hình hiện tại theo đơn vị+chi nhánh từ cache (Redis 7 ngày), miss → nạp lại, lỗi → list rỗng.
- `LayCauHinhSuDung` (POST, login) — list cấu hình `SuDung=true` cho dropdown bàn POS (`ds-ban-nv` gọi `getCauHinhSanPhamMacDinh`).
- `LayTatCaDichVuTinhGioTheoKhuVuc` (GET, `[AllowAnonymous]`, query `idDonVi`/`idChiNhanh`) — giá theo khung giờ/khu vực.
- `CheckBanTonTaiTrongCauHinh` (POST, KHÔNG có `[SBAuthorize]`/`[AllowAnonymous]` — chỉ controller-level) — check bàn trùng.

### DonHangCauHinhGiamGiaController (DEPRECATED)
Node: `registry/cua-hang/DonHangCauHinhGiamGiaController.md`. **`deprecated: true`** — toàn bộ class + 6 endpoint (`TimTheoDieuKien`, `ThemMoi`, `CapNhatThongTin`, `TimTheoId`, `Xoa`, `GetConfigOrderByPrice`) bị **comment** trong file → không route nào hoạt động. Logic giảm giá đơn nay nằm ở voucher/KM trong `DonHangController` + chiết khấu theo bill ở `CauHinhChietKhauTheoBillController`.

## Entities
- `CauHinhDonHangMatHangMacDinh` (Ten, SuDung, IsMacDinh, **CauHinhJson** = SP/DV/Combo, IdKhuVuc, IdChoNgoi, IdDichVuTinhGio, IdDichVuChuyenBan), `DichVu`, `DichVuChiNhanh`, `DichVuGiaTheoKhungGio`, `SanPham`.
- `DonHangCauHinhGiamGia` (Order.DataContext) — entity có thể còn nhưng không có API phục vụ.

## Luồng code
- Service: `CauHinhDonHangMatHangMacDinhService` (WarehouseProduct.Service/Generated).
- `ValidateModel`: chuỗi 8 bước (xem `flow.md`). Cấu hình SP/DV/Combo lưu trong cột JSON `CauHinhJson` (`CauHinhJsonMatHangMacDinh`); query theo SP/DV phải deserialize.
- Cache (Redis, TTL 7 ngày): `Get/Set/ClearCache_CauHinhDonHangMatHangMacDinh_DonVi`, `Get/SetCache_..._ChiNhanh`. Mỗi thao tác ghi → clear cache theo đơn vị.
- Gọi sang: Booking `GET /api/Common/LayAllKhuVucChoNgoi` (enrich khu vực/chỗ ngồi), `GetSanPhamElasticByIds` (Elastic), cache DV/Combo.

## Bug-prone / lưu ý (path:line) — KHÔNG sửa ở session này
- **`Xoa` clear cache sai key** (`CauHinhDonHangMatHangMacDinhService:515`) — dùng `ClearCache_..._DonVi(userLogin.Id)` (id user) thay vì `userLogin.DonViId` → cache theo đơn vị KHÔNG bị xóa sau khi xóa → POS còn đọc bộ đã xóa tới khi TTL hết (7 ngày) hoặc thao tác ghi khác clear đúng.
- **`ThayDoiTrangThaiMacDinh` lookup thiếu IdChiNhanh** (`Service:212`) — chỉ filter `Id + IdDonVi` (các method khác có thêm `IdChiNhanh`); rủi ro phạm vi chi nhánh.
- **`CheckBanTonTaiTrongCauHinh` không gắn permission** — chỉ `[HttpPost]`, áp auth mặc định controller; cần xác nhận có chủ ý.
- **`LayTatCaDichVuTinhGioTheoKhuVuc` là `[AllowAnonymous]`** — nhận `idDonVi`/`idChiNhanh` từ query, trả bảng giá tính giờ cho ai biết id → cân nhắc lộ dữ liệu giá.
- **`IsExisted` load toàn bảng `AsEnumerable()`** (`Service:799`) — kéo full về memory rồi lọc bàn trùng, **không filter theo DonVi/ChiNhánh** (quét cross-tenant) → hiệu năng + phạm vi.
- **`CapNhat` trả message "Thêm mới thành công!"** (`Service:176`) — copy nhầm message, không ảnh hưởng logic.
- `SuDung == null` được check nhưng `SuDung` là `bool` non-nullable trên DTO → nhánh đó không bao giờ true.

## File source (path:line)
- BE active: `…\scrm\WarehouseProduct\WarehouseProduct.API\Controllers\CauHinhDonHangMatHangMacDinhController.cs`, service `…\WarehouseProduct.Service\Generated\CauHinhDonHangMatHangMacDinhService.cs`, entity `…\WarehouseProduct.DataContext\Entity\CauHinhDonHangMatHangMacDinh.cs`.
- BE deprecated: `…\scrm\Order\Order.API\Controllers\DonHangCauHinhGiamGiaController.cs` (toàn bộ comment).
- FE: `…\scrm_frontend\src\app\pages\WarehouseProduct\cauhinh-mathang-macdinh\` + `services/warehouseproduct/cauhinh-donhang-mathang-macdinh.service.ts`.
