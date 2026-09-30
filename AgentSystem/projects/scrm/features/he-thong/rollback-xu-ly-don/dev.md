# Dev — RollBack & xử lý đơn

> Service chính: **OrderProcess** (BE .NET) + **ClearData** (bản tra cứu trùng tên). FE: module **quantri** (Angular 15 + ng-zorro). Trace registry: `registry/he-thong/OrderProcessController_OrderProcess.md`, `OrderRollBackController_OrderProcess.md`, `ConfigSystemController_OrderProcess.md` (+ `OrderProcessController_ClearData.md`, `OrderProcessController_SocketServer.md` cho 2 controller trùng tên khác ngữ cảnh).

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Danh sách đơn hàng lỗi (theo dõi rollback) | `/quantri/ds-donhang-loi` | `RollbackDonhangDsComponent` |

- Đây là màn **read-only**: chỉ có search + grid + phân trang, không nút thêm/sửa/xóa/retry.
- Cột grid: `maDonHang`, `tenKieuLoaiLoi`, `tenDonVi`, `tenChiNhanh`, `isBookingMajorSuccess`, `isCustomerSuccess`, `isPromotionSuccess`, `isWarehouseProductSuccess`, `ngayTao`.
- `kieuLoaiLoi`: 1 = Hủy đơn, 2 = Thanh toán đơn (hardcode trong component).
- Gọi service `OrderProcessService.TimTheoDieuKien` (FE service `services/sanboxsarea/orderProcess.service`).

## Endpoint / Controller (trace registry)
**Tra cứu rollback (màn UI)** — `OrderProcessController` (OrderProcess service, route `api/SanboxArea/OrderProcess`):
| MaTinhNang | Route | Mục đích |
|---|---|---|
| `OrderProcess_TimTheoDieuKien` | `POST api/SanboxArea/OrderProcess/TimTheoDieuKien` | Lọc bản ghi rollback (read-only) → `IOrderRollBackService.TimTheoDieuKien` |

> Controller-level KHÔNG có `[SBAuthorize]`; chỉ action có key. `[TienIch("DMDonHangController", ...)]` là nhãn copy nhầm từ danh mục đơn hàng.

**Hạ tầng rollback (service-to-service, không UI)** — `OrderRollBackController` (route `api/OrderRollBack`), phần lớn `[PrivateSBAuthorize]`:
| Hành động | Route | Auth |
|---|---|---|
| Tạo rollback ALL | `POST .../CreateRollBackAll` | internal |
| Cập nhật rollback | `POST .../UpdateRollBack` | internal |
| Lấy theo idDonHang | `GET .../GetListByIdDonHang?idDonHang=` | internal |
| Tạo/Update rollback logistic | `POST .../CreateRollBackLogistic`, `.../UpdateRollBackLogistic` | internal |
| **Rollback logistic TMĐT** | `POST .../TMDT_CreateRollBackLogistic` | **anonymous** |
| TMDT update logistic | `POST .../TMDT_UpdateRollBackLogistic` | internal (bất đối xứng) |
| Rollback phiếu nhập trả khách | `POST .../CreateRollBackPhieuNhapTraKhach`, `.../UpdateRollBackPhieuNhap` | internal |
| Rollback đối soát | `.../CreateRollBackLogisticDoiSoat(All)`, `.../UpdateRollBackLogisticDoiSoat` | internal |
| Lấy rollback đối soát/logistic theo đơn | `GET .../GetListByIdDonHangDoiSoat`, `.../GetListByIdDonHangLogistic` | internal |

**Cấu hình tham số rollback** — `ConfigSystemController` (OrderProcess, route `api/QuanTriArea/ConfigSystem`): CRUD bảng `ConfigSystem` mà `OrderRollBackService` đọc khi chạy job. Key `ConfigSystem_Customer_*` (nhãn copy từ service Customer).

## Entities
- `OrderRollBack` — bản ghi rollback chính: `IdDonHang`, `MaDonHang`, `Data`, cờ `IsWarehouseProductSuccess`/`IsCustomerSuccess`/`IsBookingMajorSuccess`/`IsPromotionSuccess`/`IsThuChiSuccess`/`IsPhieuBanHangSuccess`, message lỗi từng phân hệ, `IsRollBackLuon`, `IsProcessing`, `SoLanDaGui`.
- `OrderRollBackLogistic`, `OrderRollBackLogisticDoiSoat`, `OrderRollBackOnlineSuccess`, `OrderRollBackThuNganSuccess`, `NhapKhoRollBackLogistic` — rollback theo từng nhánh.
- `OrderRollBackLog` — log chi tiết quá trình.
- `ConfigSystem` — tham số job (bảng riêng của service OrderProcess).
- `OrderProcess` — entity tra cứu của controller ClearData.

## Luồng code
1. **Tạo:** Order/OrderLogistic gọi `CreateRollBackAll` HOẶC `OrderRollBackSyncConsumer` nhận `OrderRollBackMassage` → `LuuOrderRollBack` (lưu bản ghi, mọi cờ = false).
2. **Job xử lý:** `JobRollBack` (static lock `isRunning`) → `LayDanhSachOrderRollBack_XuLyRollBack` → tuần tự `CallRollBackWareHouse/Customer/Booking/Promotion/ThuChi` → set cờ `IsXxxSuccess` / message lỗi, tăng `SoLanDaGui`.
3. **Dọn:** `JobXoaBanGhiOrderRollBack` (thành công), `JobXoaBanGhiOrderRollBackLoi` (lỗi).
4. **Realtime (riêng):** `OrderProcessController` (SocketServer) `POST /api/OrderProcess/CreateOrderCrm` → push SignalR `ReceiveProcessUpdate` cho FE theo dõi tiến trình đồng bộ đơn TMĐT (không liên quan rollback).

## Bug-prone / lưu ý (path:line)
- **`<script>console.log(i)</script>` trong template grid** — `rollback-donhang-ds.component.html:67`: thẻ `<script>` lọt trong vòng lặp `*ngFor`, dấu vết debug; Angular không thực thi inline script qua interpolation nhưng đây là rác cần xóa, không nên để trong HTML render.
- **`TMDT_CreateRollBackLogistic` `[AllowAnonymous]`** — `OrderProcess/OrderProcess.API/Controllers/OrderRollBackController.cs:78-84`: endpoint rollback giao vận mở public, không xác thực caller → cần whitelist/HMAC ở gateway (để session security xử lý).
- **Bất đối xứng auth:** `TMDT_UpdateRollBackLogistic` lại có `[PrivateSBAuthorize]` (`OrderRollBackController.cs:94-100`) trong khi Create TMDT là anonymous — dễ nhầm khi tích hợp.
- **Rollback từng phần:** `OrderRollBackSyncConsumer.cs:32-41` — chuỗi `CallRollBack*` chạy tuần tự trong 1 try/catch chung: 1 bước lỗi sau khi đã lưu chỉ log, bước sau vẫn chạy → rollback từng phần, phải đối soát bằng job dọn.
- **Controller OrderProcess gọi service RollBack:** `OrderProcessController.cs:13` inject `IOrderRollBackService` (không phải IOrderProcessService); `[TienIch("DMDonHangController", ...)]` nhãn copy nhầm; route segment viết `SanboxArea` (thiếu "d") khác convention.
- **ConfigSystem key lệch context:** `ConfigSystemController.cs` dùng key `ConfigSystem_Customer_*` (copy từ service Customer) dù đây là OrderProcess; action `TimTheoDieuKien` dùng `[SBAuthorize()]` rỗng (chỉ check login, `ConfigSystemController.cs:125-131`).
- **Trùng tên controller:** có 3 `OrderProcessController` khác service (OrderProcess / ClearData / SocketServer) — đọc đúng node registry theo suffix.

## File source (path:line)
- BE tra cứu rollback (UI): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\OrderProcess\OrderProcess.API\Areas\SandBoxArea\OrderProcessController.cs`.
- BE hạ tầng rollback: `...\scrm\OrderProcess\OrderProcess.API\Controllers\OrderRollBackController.cs` (TMDT anonymous `:78-84`).
- BE bản ClearData (trùng tên): `...\scrm\ClearData\ClearData.API\Areas\SandBoxArea\OrderProcessController.cs` + `...\ClearData\ClearData.API\Controllers\OrderRollBackController.cs`.
- BE SocketServer (push tiến trình, trùng tên): `...\scrm\SocketServer\SocketServer.API\Controllers\OrderProcessController.cs`.
- FE màn theo dõi: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\quantri\rollback-donhang-ds\rollback-donhang-ds.component.ts` (`:125` gọi TimTheoDieuKien) + `.component.html` (`:67` script rác).
- FE routing: `...\scrm_frontend\src\app\pages\quantri\quantri-routing.module.ts:250` (`ds-donhang-loi`).
- Cẩm nang nền (job/consumer chi tiết): `projects/scrm/features/order-process.md`.
