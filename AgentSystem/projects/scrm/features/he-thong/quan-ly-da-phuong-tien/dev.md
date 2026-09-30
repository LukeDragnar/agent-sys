# Dev — Quản lý đa phương tiện

> Service: **Core** (BE .NET) · FE module **quantri** (Angular 15 + ng-zorro). Trace registry: `registry/he-thong/DaPhuongTienController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Danh sách đa phương tiện | `/quantri/daphuongtien` | `QuanlyDaphuongtienComponent` |
| Form thêm/sửa (popup) | (modal) | `QuanlyDaphuongtienChitietComponent` |

- List component extends `BaseListComponent`; search model `{ nhom, keyword }`.
- FE service: `services/sanboxsarea/daphuongtien.service` (`timTheoDieuKien`, `timTheoId`, `luu`, `xoa`, `layNhom`).
- **Lưu ý lệch cột:** `gridColumns` khai báo trong list component dùng các field của ĐƠN VỊ (`tenDonVi`, `tenMien`, `nguoiDaiDien`, `tenNganhNghe`…) — copy nhầm từ màn đơn vị; nhưng `<thead>`/`<tbody>` trong HTML render thủ công đúng field đa phương tiện (`ten`, `nhom`, `thuTuHienThi`, `duongDan`, `suDung`, `ngayCapNhat`). Cột khai báo không khớp cột render → dọn lại khi refactor.

## Endpoint / Controller (trace registry)
`DaPhuongTienController` (Core service, route `api/DaPhuongTien`, kế thừa `BaseController`):
| # | Hành động | Route | Quyền | Auth |
|---|---|---|---|---|
| 1 | Lưu (thêm/sửa) | `POST api/DaPhuongTien/Luu` | `HT_Luu_DaPhuongTien` | có key |
| 2 | Xóa | `GET api/DaPhuongTien/Xoa/{id:Guid}` | `HT_Xoa_DaPhuongTien` | có key |
| 3 | Tìm theo điều kiện | `POST api/DaPhuongTien/TimTheoDieuKien` | — (không key) | chỉ login |
| 4 | Tìm theo Id | `GET api/DaPhuongTien/TimTheoId/{id:Guid}` | — (không key) | chỉ login |
| 5 | Lấy nhóm | `GET api/DaPhuongTien/LayNhom` | — (không key) | chỉ login |

> `[TienIch("DaPhuongTienController", "Quản lý đơn vị", MaTienIchSCRM.TI_Chung)]` — nhãn "Quản lý đơn vị" copy nhầm context.

## Entities
- `DaPhuongTien` — bản ghi media: `Ten`, `Nhom`, `ThuTuHienThi`, `DuongDan`, `MoTa`, `SuDung` (bool), audit (`NgayCapNhat`, `NguoiCapNhat`). DTO vào: `DaPhuongTienView`; filter: `ReqTimKiemTheoDieuKien`.

## Luồng code
1. List: `gridLoadData` → `daPhuongTienService.timTheoDieuKien({...searchModel, ...gridOption})`.
2. Thêm/Sửa: `onShowDetail(id)` mở `QuanlyDaphuongtienChitietComponent` → `initForm` (nếu có id thì `timTheoId` nạp dữ liệu) → `addOrEdit` validate + `checkSpace` (Ten, MoTa) → convert `suDung` `'1'/'0'` → bool → `daPhuongTienService.luu(body)` → `service.Luu`.
3. Xóa: `onDelete(id)` confirm → `daPhuongTienService.xoa(id)` → `service.Xoa` (BE `[HttpGet]`).
4. Toàn bộ nghiệp vụ ủy quyền `IDaPhuongTienService`; không thấy ripple cross-service ở tầng controller.

## Bug-prone / lưu ý (path:line)
- **Xóa qua HTTP GET** — `Core/Core.API/Controllers/DaPhuongTienController.cs:46`: `Xoa` gắn `[HttpGet]` để xóa dữ liệu, lệch convention (xóa nên POST), rủi ro side-effect qua GET (prefetch/crawler). FE có confirm nên rủi ro thấp, nhưng API thì open.
- **Thiếu phân quyền** — `DaPhuongTienController.cs:58-83`: `TimTheoDieuKien`, `TimTheoId`, `LayNhom` thiếu `[SBAuthorize]` key → chỉ chặn ở mức token, bất kỳ user login nào cũng đọc được (để session security xử lý).
- **Di tích comment** — `DaPhuongTienController.cs:33`: còn dòng `//[AllowAnonymous]` trên `Luu` (đã comment, không ảnh hưởng).
- **Nhãn TienIch sai context** — `DaPhuongTienController.cs:12`: mô tả "Quản lý đơn vị" trong khi là đa phương tiện → gây nhầm khi tra phân quyền.
- **FE gridColumns lệch field** (xem mục Màn hình): cột khai báo dùng field đơn vị, không khớp HTML render → dọn lại.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\Core.API\Controllers\DaPhuongTienController.cs` (`Luu`, `Xoa` GET `:46`, các Tim/LayNhom `:58-83`).
- FE list: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\quantri\quanly-daphuongtien\quanly-daphuongtien.component.ts`.
- FE form chi tiết: `...\quanly-daphuongtien\quanly-daphuongtien-chitiet\quanly-daphuongtien-chitiet.component.ts`.
- FE routing: `...\scrm_frontend\src\app\pages\quantri\quantri-routing.module.ts:247` (`daphuongtien`).
