# Dev — Quản lý tài chính (đối tác trả góp & domain)

> 2 mảng độc lập: (1) cấu hình trả góp — `DonViDoiTacTraGopController` + `DMDoiTacTraGopController` (service Accounting); (2) lịch sử/duyệt domain — `LichSuThayDoiDomainController` (service Core, 2 bản: đơn vị + QuanTriArea). FE module `caidat-hethong`.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Cấu hình đối tác trả góp | `caidat/cauhinh-doitac-tragop` | `CauHinhDoiTacTraGopComponent` (con `cau-hinh-doi-tac-tra-gop-edit`) |
| Domain đơn vị / lịch sử thay đổi domain | (màn quản trị đơn vị — cần product xác nhận route FE chính xác) | — |

- Route gốc: `app-routing.module.ts:141` (`caidat` → `CaidatHethongModule`).
- Route con trả góp: `caidat-hethong-routing.module.ts:272` (`cauhinh-doitac-tragop`).
- Màn lịch sử domain phía đơn vị: chưa xác định component trong `caidat-hethong-routing` — **cần product xác nhận** route/màn FE.

## Endpoint / Controller (trace registry)

### Cấu hình đối tác trả góp theo đơn vị — `DonViDoiTacTraGopController` (Accounting)
> Registry: `registry/ke-toan/DonViDoiTacTraGopController.md`. Mọi `[SBAuthorize]` KHÔNG kèm key (chỉ login). `[TienIch(TI_Chung)]`. Phần CRUD cơ bản (TimTheoId/ThemMoi/CapNhat/Xoa/TimTheoDieuKien) **comment toàn bộ** → chỉ còn 9 endpoint cấu hình.

| Hành động | Route | Ghi chú |
|---|---|---|
| DS đối tác của đơn vị (form thanh toán) | `GET .../GetDSDoiTacTraGop` | ưu tiên chi nhánh user, fallback null |
| DS đối tác kèm checked (màn map) | `GET .../GetDSDoiTacTraGopCheckedByDonVi` | LEFT JOIN DMDoiTacTraGop (PTTT=8) |
| Lưới cấu hình | `POST .../GetDanhSachCauHinhDoiTacTraGop` | lọc CN/keyword, paging |
| Xem 1 cấu hình | `POST .../GetDonViTraGopById` (body=Guid) | null → "Không tìm thấy thông tin đối tương" (typo) |
| Map nhanh | `POST .../MapCauHinhDoiTac` | insert/delete; chỉ IdDonVi+IdDoiTacTraGop |
| Thêm cấu hình (gắn NCC) | `POST .../ThemMoiCauHinhTraGopTaoNcc` | validate; IdLoaiQuy=QuyTraGop |
| Sửa cấu hình | `POST .../UpdateTraGopById` | validate isUpdate; **IdLoaiQuy=ViDienTu** |
| Xóa cấu hình | `POST .../DeleteDonViTraGopById` (body=Guid) | theo Id && IdDonVi |
| Toàn bộ danh mục đối tác | `GET .../GetDanhMucDoiTacTraGop` | DMDoiTacTraGop PTTT=8 (master) |

### Danh mục đối tác trả góp (master) — `DMDoiTacTraGopController` (Accounting)
> Registry: `registry/ke-toan/DMDoiTacTraGopController.md`. Master data toàn hệ thống, KHÔNG scope đơn vị. `[SBAuthorize]` đều **bị comment** → public, không key. Ghi → xóa cache `DMDoiTacTraGop_All` + prefix `DoiTacTraGop_ByDonVi_`.

| Hành động | Route |
|---|---|
| Lưới | `POST api/DMDoiTacTraGop/TimTheoDieuKien` |
| Chi tiết | `GET .../TimTheoId/{id:Guid?}` |
| Thêm/Sửa/Xóa | `POST .../ThemMoi`, `/CapNhatThongTin`, `/Xoa/{id}` |

### Lịch sử thay đổi domain — `LichSuThayDoiDomainController`
> Registry: `registry/he-thong/LichSuThayDoiDomainController.md` (phía đơn vị) + `..._Core.md` (phía admin QuanTriArea). State `[ChoDuyet, Duyet, TuChoi, HuyBo]`.

| Hành động | Route | Quyền |
|---|---|---|
| Tạo yêu cầu (đơn vị) | `POST api/LichSuThayDoiDomain/ThemMoi` | `LichSuThayDoiDomain_ThemMoi` (DV-AD-TONG) |
| Hủy bỏ (đơn vị) | `GET .../CapNhatTrangThaiHuyBo/{id:Guid}` | `_CapNhatTrangThaiHuyBo` (DV-AD-TONG) — ⚠ GET đổi state |
| Tra lịch sử (đơn vị) | `POST .../TimKiemTheoDieuKienDV` | `_TimKiemTheoDieuKienDV` (DV-AD-TONG) |
| Duyệt (admin HT) | `GET api/QuanTriArea/LichSuThayDoiDomain/CapNhatTrangThaiDuyet/{id}` | `_CapNhatTrangThaiDuyet` (HT-AD/HT-KT) — ⚠ GET |
| Từ chối (admin HT) | `POST .../CapNhatTrangThaiTuChoi` | `_CapNhatTrangThaiTuChoi` (HT-AD/HT-KT) |
| Tra lịch sử (toàn HT) | `POST .../TimKiemTheoDieuKienAdmin` | `_TimKiemTheoDieuKienAdmin` (HT-AD/HT-KT) |

## Entities
- **`DonViDoiTacTraGop`** — IdDonVi, IdDoiTacTraGop, IdChiNhanh, MaTaiKhoan, TenTaiKhoan, SuDung, IsTkDonVi, IdNhaCungCap/Ma/Ten, IdLoaiQuy, IdPhuongThucThanhToan.
- **`DMDoiTacTraGop`** — master: Ma, Ten, Images, IdPhuongThucThanhToan (=8 cho trả góp), IsHideFormThanhToan.
- **`LichSuThayDoiDomain`** — yêu cầu + trạng thái + domain cũ/mới.

## Luồng code
- **Trả góp:** controller → EF (`DonViDoiTacTraGop`/`DMDoiTacTraGop`) + RedisCache. `ValidateCauHinhTraGop` gate chi nhánh/NCC/trùng. `MapCauHinhDoiTac` so `ListIdChecked` vs hiện có → insert/delete + xóa cache.
- **Domain:** controller (đơn vị) → `ILichSuThayDoiDomainService` (Create/HuyBo/TimKiemDV); controller (admin) → cùng service (Duyet/TuChoi/TimKiemAdmin). Duyệt áp domain mới (side-effect trong service).

## Bug-prone / lưu ý
- **IdLoaiQuy lệch Thêm/Sửa:** `DonViDoiTacTraGopService.cs:208` (`UpdateTraGopById`) ép `IdLoaiQuy = ViDienTu` trong khi Insert (`ThemMoiCauHinhTraGopTaoNcc`) để mặc định `QuyTraGop` → loại quỹ khác nhau giữa thêm và sửa (rủi ro nghiệp vụ — **cần product xác nhận**).
- **Typo message:** `DonViDoiTacTraGopService.cs:179/189` "Không tìm thấy thông tin đối tương" (đối tượng).
- **Map nhanh thiếu field:** `MapCauHinhDoiTac` (`:329`) insert chỉ IdDonVi+IdDoiTacTraGop (không chi nhánh/tài khoản/NCC) → khác cấu hình đầy đủ; xóa cache theo donVi (không kèm chi nhánh) trong khi đọc list theo donVi+chiNhanh.
- **Dead path:** `DonViDoiTacTraGopService.TimTheoDieuKienAsync` còn tồn tại nhưng endpoint controller đã comment (L26-111) → không expose.
- **Sort vô tác dụng:** `DMDoiTacTraGopService.cs:76-83` if/else OrderBy NgayTao giống nhau → sort của FE bị bỏ qua (cũng đúng với `DonViDoiTacTraGopService.TimTheoDieuKienAsync` L73).
- **Permission key comment:** cả 2 controller trả góp `[SBAuthorize]` bị comment → chỉ auth mặc định BaseController (public/login, không key). Key dự kiến: `DMDoiTacTraGop_ThemMoi/_CapNhatThongTin/_Xoa`.
- **Side-effect qua GET (domain):** `LichSuThayDoiDomainController.cs:33-34` (HuyBo) và `Areas/SanboxArea/LichSuThayDoiDomainController.cs:22-23` (Duyet) dùng HTTP GET đổi state → lệch REST, dễ trigger ngoài ý muốn (prefetch/crawler); cần chống double-submit.
- **Trùng tên class** `LichSuThayDoiDomainController` (đơn vị vs SanboxArea) — khác namespace/route/role, dễ nhầm khi search.

## File source (path:line)
- BE trả góp controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Accounting\Accounting.API\Controllers\DonViDoiTacTraGopController.cs:26-111` (CRUD comment), `:114-205` (no key), DMDoiTac `...\DMDoiTacTraGopController.cs:29/41/61/88`.
- BE trả góp service: `...\Accounting\Accounting.Service\Generated\DonViDoiTacTraGopService.cs:208` (IdLoaiQuy), `:179/189` (typo), `:329` (MapCauHinhDoiTac), `:73` (sort); `DMDoiTacTraGopService.cs:76-83`.
- BE domain: `...\scrm\<Core>\...\Controllers\LichSuThayDoiDomainController.cs:33-34`; `...\Areas\SanboxArea\LichSuThayDoiDomainController.cs:22-23`.
- Entity: `Accounting/Accounting.DataContext/Entity/{DonViDoiTacTraGop, DMDoiTacTraGop}.cs`.
- FE trả góp: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\caidat-hethong\cau-hinh-doi-tac-tra-gop\cau-hinh-doi-tac-tra-gop.component.ts` (+ `-edit`).
- Registry: `projects/scrm/registry/ke-toan/{DonViDoiTacTraGopController, DMDoiTacTraGopController}.md`; `registry/he-thong/{LichSuThayDoiDomainController, LichSuThayDoiDomainController_Core}.md`.
