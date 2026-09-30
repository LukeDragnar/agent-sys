# Dev — Lịch sử đăng ký tiện ích

> Service: **Core** (BE .NET) · FE module **CaidatHethong** (Angular 15 + ng-zorro). Dùng endpoint `LichSuGiaoDich` của `TienIchController` (đơn vị) → `IHopDongTienIchService`. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Lịch sử mua tiện ích | `/caidat/lich-su-mua-tien-ich` | `LichSuMuaTienIchComponent` (extends `BaseListComponent`) |

- Route khai báo: `caidat-hethong-routing.module.ts:123`. Module lazy gốc `app-routing.module.ts:141` (`path: 'caidat'`).
- FE service: `services/core/tienich.service.ts:33` (`lichSuGiaoDich(filter)` → `POST api/TienIch/LichSuGiaoDich`).

## Endpoint / Controller (trace registry: `registry/he-thong/TienIchController.md`, `.../HopDongTienIchController.md`)
| MaTinhNang | Route | Quyền | Mục đích |
|---|---|---|---|
| (SBAuthorize trống — chỉ login) | `POST api/TienIch/LichSuGiaoDich` | đăng nhập | Lịch sử giao dịch tiện ích của đơn vị (theo CurrentUser) |

- Controller: `Core.API.Controllers.TienIchController.LichSuGiaoDich` (`TienIchController.cs:87`) → `_hopdongService.LichSuGiaoDichAsync(gridInfo, CurrentUser)`.
- DTO filter: `LichSuGiaoDichFilterDto` (keyword, tuNgay, denNgay + grid option).

## Entities
- **`HopDongTienIch`** — nguồn lịch sử (Id, IdDonVi, MaGiaoDich, TrangThai, ThanhTien, NgayTao). State machine: `[ChoDuyet, DaDuyet, HuyDuyet]` (registry); FE map số 0/1/3/5 → màu.
- **`HopDongTienIch_ChiTiet`** — dòng tiện ích trong giao dịch (cột "Danh sách tiện ích" = `listTienIch`).

## Luồng code
1. FE `LichSuMuaTienIchComponent.gridLoadData()` (`lich-su-mua-tien-ich.component.ts:95`): build filter từ `getGridOption()` + `searchModel` → `lichSuGiaoDich(filter)`.
2. BE `TienIchController.LichSuGiaoDich` (`:87`) → `IHopDongTienIchService.LichSuGiaoDichAsync(gridInfo, CurrentUser)` — lọc hợp đồng theo `CurrentUser.DonViId`, keyword (mã giao dịch), khoảng ngày; trả `data` + `totalRecord`.
3. FE render bảng: cột `trangThai` map màu theo `data.trangThai`, text theo `data.trangThaiText`; cột `listTienIch` lặp tên tiện ích; `thanhTien` định dạng tiền VNĐ.

## Bug-prone / lưu ý
- **`onAddNew` / `onEdit` / `onDelete` trỏ nhầm sang KHUYẾN MÃI** (`lich-su-mua-tien-ich.component.ts:118/137/155`): mở `KhuyenmaiEditComponent`, gọi `_khuyenMaiService.Xoa(id)`. Đây là code copy-paste từ màn khuyến mãi còn sót, **không liên quan tiện ích** — nếu UI có lộ nút này sẽ thao tác nhầm vào khuyến mãi. Cần product xác nhận xóa các handler chết này (KHÔNG sửa ở session này).
- **`<script>console.log(i)</script>` trong template bảng** (`lich-su-mua-tien-ich.component.html:78`): thẻ `<script>` lạ nhúng trong `<tbody>` — rác/nguy cơ; Angular thường strip nhưng vẫn là code bẩn. Ghi lại để session cleanup/security xử lý.
- **Endpoint `LichSuGiaoDich` không có MaTinhNang riêng** (`[SBAuthorize()]` trống) — chỉ chặn ở mức login, không phân quyền role. Mọi user đăng nhập của đơn vị đều xem được lịch sử mua tiện ích → cần product xác nhận có muốn giới hạn role (vd chỉ DV-AD-TONG/kế toán) không.
- **Ý nghĩa trạng thái số (0/1/3/5) chưa map rõ** trong registry (state machine chỉ có tên ChoDuyet/DaDuyet/HuyDuyet). Khi cần đối chiếu chính xác → xem enum `HopDongTienIch_TrangThai` trong BE.
- **Export Excel ẩn:** menu cấu hình có item Export nhưng `showExportButton` mặc định (BaseListComponent) — cần xác nhận màn này có bật export không.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\Core.API\Controllers\TienIchController.cs:87` (LichSuGiaoDich).
- BE service hợp đồng: `IHopDongTienIchService.LichSuGiaoDichAsync` (Core.Service — `HopDongTienIchService`).
- Registry node: `projects/scrm/registry/he-thong/HopDongTienIchController.md` (state machine + quan hệ), `.../TienIchController.md`.
- FE list: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\caidat-hethong\lich-su-mua-tien-ich\lich-su-mua-tien-ich.component.ts:50` (ngOnInit/cột), `:95` (gridLoadData), `:118/137/155` (handler khuyến mãi sót).
- FE template: `...\lich-su-mua-tien-ich\lich-su-mua-tien-ich.component.html:78` (`<script>` lạ), `:142` (map trạng thái màu).
- FE service: `...\src\app\services\core\tienich.service.ts:33`.
- FE routing: `...\caidat-hethong\caidat-hethong-routing.module.ts:123`.
