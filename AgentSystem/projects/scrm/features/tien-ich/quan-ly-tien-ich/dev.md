# Dev — Quản lý tiện ích

> Service: **Core** (BE .NET) · FE module **CaidatHethong** (Angular 15 + ng-zorro). Mặt "đơn vị / chợ tiện ích" của `TienIchController`. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Đăng ký / chợ tiện ích | `/caidat/dangky-tien-ich` | `DangkyTienIchComponent` |
| Popup thanh toán tiện ích | (modal) | `ChonMuaTienIchComponent` |
| Đăng ký tiện ích (bản v2) | `/caidat/dangky-tien-ich-v2` | `DangkyTienIchV2Component` |

- Route khai báo: `caidat-hethong-routing.module.ts:103` (dangky-tien-ich), `:107` (v2). Module lazy gốc `app-routing.module.ts:141` (`path: 'caidat'` → `CaidatHethongModule`).
- Component lọc nhóm: `app-filter-nhom-tienich` (`components/filter-nhom-tienich`).
- FE service: `services/core/tienich.service.ts` (`getDanhSachTienIchFilter`, `muaNgayTienIch`, `XoaTiDaDangKy`, `lichSuGiaoDich`).

## Endpoint / Controller (trace registry: `registry/he-thong/TienIchController.md`)
Controller (B) đơn vị — `Core.API.Controllers.TienIchController` (`[TienIchAttribute("TienIchController", "Quản lý tiện ích", MaTienIchSCRM.TI_Chung)]`, route `api/TienIch`):
| MaTinhNang (SBAuthorize) | Route | Quyền | Mục đích |
|---|---|---|---|
| `TienIch_GetDanhSachTienIch` | `POST api/TienIch/GetDanhSachTienIchFilter` | DV-AD-TONG, HT-KT | Chợ tiện ích (lọc theo nhóm) |
| `TienIch_GetDanhSachTienIchDaMua` | `GET api/TienIch/GetDanhSachTienIchDaMua/{IsConHan:int}` | DV-AD-TONG/CN/MCN, DV-QLDV, HT-KT | DS tiện ích đã mua (IsConHan=1 → còn hạn) |
| `TienIch_MuaNgay` | `POST api/TienIch/MuaNgay` | DV-AD-TONG, HT-KT | Mua/đăng ký tiện ích |
| `TienIch_XoaTiDaDangKy` | `GET api/TienIch/XoaTiDaDangKy/{id:Guid}` | DV-AD-TONG | Hủy tiện ích đã đăng ký |
| (PrivateSBAuthorize) | `GET api/TienIch/GetDanhSachTienIchDaMua/{idDonVi:Guid}/{IsConHan:int}` | service-to-service | Lấy DS đã mua của đơn vị bất kỳ |

> ⚠️ Có **2 class trùng tên `TienIchController`**: bản này (Controllers, `api/TienIch`) cho đơn vị; bản Admin CRUD ở `Areas/SanboxArea` (`api/QuanTriArea/TienIch`). Phân biệt bằng route.

## Entities
- **`TienIch`** — danh mục tiện ích (Ma, Ten, MoTa, GiaTien nullable, IsMacDinh, IsFree, IsAllowDelete, IdNhomTienIch…).
- **`HopDongTienIch`** — hợp đồng/giao dịch mua (Id, IdDonVi, MaGiaoDich, TrangThai, ThanhTien). Trạng thái: `HopDongTienIch_TrangThai.ChoDuyet`…
- **`HopDongTienIch_ChiTiet`** — dòng chi tiết (MaTienIch, GiaTien, SoLuong, ThanhTien).
- **`DonViTienIch`** — bản ghi tiện ích đã gắn cho đơn vị (DonViId, TienIchId) — đối tượng xóa của `XoaTiDaDangKy`.

## Luồng code
**Mua** (`TienIchService.MuaNgayTienIchAsync`, `Core.Service/Generated/TienIchService.cs:291`):
1. List rỗng → lỗi. Lấy `dsTienIch` theo Id (`TableWithNolock`).
2. Tạo 2 hợp đồng tách: `hopdongTienIch` (trả phí) + `hopdongTienIchTuDongDuyet` (giá 0). Cả hai khởi tạo TrangThai = ChoDuyet.
3. Loop từng item: validate SoLuong (<0 lỗi; >100 && GiaTien>0 lỗi); item không có trong DB → lỗi. `GiaTien==0` → vào nhóm auto-duyệt (`isAutoDuyet=true`); else vào nhóm trả phí, cộng `tongTien`.
4. Transaction (ExecutionStrategy + BeginTransaction): insert hợp đồng + chi tiết cho nhóm nào có dòng.
5. Nếu `isAutoDuyet` → `_hopDongTienIchService.AutoDuyetHopDongAsync(...)`. Nếu **không có** dòng trả phí → return `Code="02"` (dừng).
6. Còn dòng trả phí → đọc `ConfigCode.HT_TK_NGANHANG` (JSON ngân hàng); rỗng → fallback tài khoản Techcombank cứng trong code → return `Code="01"` + thông tin QR + `NoiDungThanhToan = "TT SandboxVN Ma {MaGiaoDich}"`.

**FE thanh toán** (`ChonMuaTienIchComponent.onThanhToan`, `chon-mua-tienich.component.ts:130`): gọi `muaNgayTienIch(gridData)`; `code=='02'` → đóng + báo thành công; else → step=1, gen QR (`QrCodeHelper.gennerateQRCode`).

**Xóa** (`TienIchService.XoaTiDaDangKy`, `:467`): tìm `DonViTienIch` đúng đơn vị; tiện ích tồn tại; `IsMacDinh==true && IsAllowDelete!=true` → chặn; else xóa trong transaction.

## Bug-prone / lưu ý
- **`XoaTiDaDangKy` dùng `[HttpGet]` cho thao tác xóa nghiệp vụ** (`TienIchController.cs:94`) — không idempotent, dễ bị trigger nhầm (prefetch/crawler). (đã ghi ở registry)
- **Hàm `BuyTienIch(idTienIch)` ở FE (`dangky-tien-ich.component.ts:79`) là rỗng** — modal confirm có `nzOnOk` rỗng, không gọi API. Code chết, đừng nhầm là luồng mua thật (luồng thật là `MuaNgayTienIch`).
- **Tài khoản ngân hàng fallback hardcode** (`TienIchService.cs:442`) — số TK Techcombank + tên công ty cứng trong code khi `HT_TK_NGANHANG` rỗng. Lộ thông tin TK trong source; cần product xác nhận có nên giữ.
- **2 class trùng tên `TienIchController`** — grep theo tên class ra cả Admin lẫn đơn vị; phải lọc theo route/namespace.
- **`onSelect` không chặn tiện ích đã mua hết hạn** rõ ràng — chỉ chặn `isChoDuyet`; cần kiểm `isDaMua`/hết hạn ở UI (cần xác nhận hành vi mong muốn khi mua lại tiện ích hết hạn).
- **MuaNgay tạo 2 hợp đồng tách biệt** (1 cho free auto-duyệt, 1 cho trả phí) — khi giỏ trộn, sinh 2 MaGiaoDich khác nhau cho cùng 1 lần bấm mua → màn Lịch sử có thể hiện 2 dòng cho 1 thao tác.

## File source (path:line)
- BE controller (đơn vị): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\Core.API\Controllers\TienIchController.cs:30` (filter), `:41` (đã mua), `:79` (MuaNgay), `:95` (XoaTiDaDangKy).
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\Core.Service\Generated\TienIchService.cs:291` (MuaNgay), `:467` (XoaTiDaDangKy), `:552` (GetDanhSachTienIchFilter).
- Registry node: `projects/scrm/registry/he-thong/TienIchController.md`, `.../HopDongTienIchController.md`, `.../DonViTienIchController.md`.
- FE chợ: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\caidat-hethong\dangky-tien-ich\dangky-tien-ich.component.ts:54` (GetAllTienIch), `:95` (MuaNgayTienIch), `:130` (xóa).
- FE thanh toán: `...\dangky-tien-ich\chon-mua-tienich\chon-mua-tienich.component.ts:130`.
- FE service: `...\src\app\services\core\tienich.service.ts:23`.
- FE routing: `...\caidat-hethong\caidat-hethong-routing.module.ts:103`.
