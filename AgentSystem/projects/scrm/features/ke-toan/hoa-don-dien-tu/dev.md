# Hóa đơn điện tử — Kỹ thuật

> Service **Invoice** (BE .NET) — 3 controller chính: `MeInvoiceController` (MISA), `ViettelInvoiceController` (Viettel/SInvoice), `InvoiceController` (phát hành & tra cứu chung). `Account` là entity dùng chung 2 nhà cung cấp, phân biệt bằng `TypeInvoice`. Phát hành **bất đồng bộ qua RabbitMQ**; tra cứu lai SQL (`InvoiceStatus`/`InvoiceInfo`) + MongoDB (`HoaDonDienTuHistory`).

## Màn hình & route (FE)
| Màn hình | Vị trí | Ghi chú |
|---|---|---|
| Cấu hình HĐĐT — tab MeInvoice (MISA) | Kế toán > Cấu hình hóa đơn điện tử | CRUD tài khoản + lưới mẫu (đồng bộ/mặc định/mapping) + preview |
| Cấu hình HĐĐT — tab Viettel (SInvoice) | Kế toán > Cấu hình hóa đơn điện tử | tương tự + ký hiệu/seri/template code Viettel |
| Popup "Danh sách hóa đơn điện tử" của đơn | Trên màn đơn (Order/OrderLogistic) | lưới phát hành + nháp + tải PDF |
| Tra cứu HĐĐT toàn đơn vị | màn tra cứu hóa đơn | tìm theo điều kiện + Xuất Excel |

## Endpoint/Controller (trace registry)

### MeInvoiceController (MISA) — `registry/ke-toan/MeInvoiceController.md`
- Controller-level `[TienIchAttribute("MeInvoice_Controller", ..., TI_Invocie_Misa)]`.
- `POST Insert` / `Update` — quyền `MeInvoice_Insert/_Update` (DV-AD-TONG/CN/MCN). Ép `TypeInvoice = MeInovice` trước khi gọi service (`MeInvoiceController.cs:39,51`).
- `POST Delete/{id:Guid}` — `MeInvoice_Delete`.
- `POST TimKiemAccountTheoDieuKien` / `GetAccountMeInvoice/{id}` — thêm quyền DV-KT.
- `POST SyncTemplate/{idAccount:Guid}` — gọi MISA API, upsert `InvoiceTemplate`.
- `POST TimKiemTemplateTheoDieuKien` / `ChangeDefaultTemplate/{id}`.
- `POST PreviewMeInvoice` / `CapNhatThongTinMeInvoice` / `LayMauHoaDonTheoId_MeInvoice/{id}` — **đã bỏ key permission** (chỉ `[SBAuthorize]` trống), key gốc comment tại `MeInvoiceController.cs:113,121,135`.

### ViettelInvoiceController (Viettel/SInvoice) — `registry/ke-toan/ViettelInvoiceController.md`
- `partial class`, `[TienIch("AccountController", ..., TI_Invocie_Viettel)]` (`ViettelInvoiceController.cs:16`).
- `GET TimTheoId/{id:Guid?}` — `ViettelInvoice_TimTheoId`.
- `POST InsertOrUpdate` — `ViettelInvoice_InsertOrUpdate`; **nếu `TypeInvoice==null` → set `MeInovice`** (`:51-54`) — BUG: tab Viettel có thể tạo tài khoản loại MISA.
- `POST Delete/{id}` — ép NCC = ViettelInvoice khi xóa.
- `POST TimTheoDieuKien` / `SyncTemplate/{idAccount}` / `TimKiemTemplateTheoDieuKien` / `ChangeDefaultTemplate/{id}`.
- `POST PreviewSInvoice` / `CapNhatThongTinSInvoice` / `LayMauHoaDonTheoId_SInvoice/{id}` — **bỏ key** (`:104,113,125`).

### InvoiceController (phát hành & tra cứu) — `registry/ke-toan/InvoiceController.md`
- **Gate tiện ích lặp từng method:** `CheckTienIch(TI_Invocie_Misa) || CheckTienIch(TI_Invocie_Viettel)` — `InvoiceController.cs:73,123,152,165,177`.
- UI (không key, gate tiện ích):
  - `GET GetInvoiceFile?idDonHang&no&type` — tải PDF (stream byte từ NCC; bản đọc file vật lý wwwroot đã comment `:81-117`).
  - `POST GetHoaDonDienTuByIdDonHang/{id}` — trạng thái HĐ của đơn (SQL `InvoiceStatus` NoLock).
  - `POST LayDanhSachPhatHanhTheoIdDonHang` — danh sách phát hành theo đơn, đọc **Mongo** `HoaDonDienTuHistory` (filter: Keyword, ActionType, IsNhap, TuNgay/DenNgay, TypeExcute 1=thủ công/2=tự động+gửi lại).
  - `POST TimHoaDonDienTuTheoDieuKien` — tra cứu toàn đơn vị, đọc **SQL** `InvoiceInfo`.
  - `POST XuatExcelHoaDonDienTuTheoDieuKien` — xuất Excel (gate fail → `BadRequest` 400, khác convention).
- Internal (`PrivateSBAuthorize`, Order/OrderLogistic gọi sang):
  - `POST CreateInvoice` — phát hành thật; PhieuXuatKho chỉ MISA; dispatch theo `TypeInvoice` → publish queue (retry 3 lần, delay 1s); luôn trả "Đã nhận yêu cầu…".
  - `POST TaoHoaDonNhap` — tạo nháp (chỉ MISA + Viettel), `IsNhap=true`.
  - `POST XoaHoaDonNhap` — qua MISA.
  - `POST DongBoTrangThaiHoaDonNhap` — orchestrator tách NCC theo `InvoiceStatus.TypeInvoice`, gộp kết quả.

## Entities
- **`Account`** — tài khoản nhà cung cấp, dùng chung MISA/Viettel, phân biệt bằng `TypeInvoice` (`EnumTypeInvoice.MeInovice` / `.ViettelInvoice`).
- **`InvoiceTemplate`** — mẫu hóa đơn thuộc 1 `IdAccount`; `IsDefault` quyết mẫu phát hành; MISA có `MeInvoiceJsonMappingCustomField`; Viettel có `ViettelTemplateCode`, `ViettelInvoiceSeri`, `SInvoiceJsonMappingCustomField`.
- **`InvoiceStatus`** (SQL) — trạng thái HĐ/nháp theo đơn (`Status`, `Status_Nhap`, `TypeInvoice`). State machine: Draft / Created / CreateFail / ValidateFail.
- **`InvoiceInfo`** (SQL) — chi tiết phát hành theo đơn, gồm trạng thái gửi SMS/ZNS.
- **`HoaDonDienTuHistory`** (MongoDB) — lịch sử từng lần gửi NCC + file URL.

## Luồng code
1. Order/OrderLogistic validate precondition (cấu hình `IsAllowPublishInvoice` + trạng thái) → gọi `InvoiceController.CreateInvoice` (`PrivateSBAuthorize`).
2. `HoaDonDienTuService.TaoHoaDonDienTu` (`HoaDonDienTuService.cs:91-144`) publish `InputDataCreateInvoiceMISARabit` / `...ViettelRabit` (retry 3 lần).
3. Consumer (service hóa đơn) gọi partner API ký+phát hành → ghi `InvoiceStatus.Status` + append `HoaDonDienTuHistory` → đẩy SMS/ZNS.
4. FE poll `GetHoaDonDienTuByIdDonHang` / `LayDanhSachPhatHanhTheoIdDonHang`.
5. Realtime batch: service hóa đơn → `InvoiceController` (SocketServer) `notify-statistics-batch-update` / `notify-result-from-batch` → SignalR `InvoiceHub` push group `CHINHANH_{IdChiNhanh}_*` (xem `registry/he-thong/InvoiceController_SocketServer.md`).

## Bug-prone / lưu ý (path:line)
- **Endpoint bỏ quyền (chỉ login):** Preview/CapNhatThongTin/LayMauHoaDon ở cả MISA và Viettel — `MeInvoiceController.cs:113,121,135` · `ViettelInvoiceController.cs:104,113,125`. Mọi user đăng nhập gọi được → cần session security review.
- **Tài khoản Viettel lạc NCC:** `ViettelInvoiceController.cs:51-54` — `InsertOrUpdate` fallback `MeInovice` khi `TypeInvoice==null`. Tab UI Viettel nhưng entity có thể thành MISA.
- **Publish fail vẫn báo OK:** `HoaDonDienTuService.cs:91-144` — retry 3 lần publish; fail cả 3 vẫn trả "Đã nhận yêu cầu…" → đơn "tưởng đã gửi" mà chưa publish.
- **2 nguồn tra cứu khác store:** `LayDanhSachPhatHanhTheoIdDonHang` (Mongo) vs `TimHoaDonDienTuTheoDieuKien` (SQL `InvoiceInfo`) — cùng khái niệm "hóa đơn theo đơn", dễ lệch dữ liệu.
- **Xuất Excel trả BadRequest:** `InvoiceController.cs` `XuatExcelHoaDonDienTuTheoDieuKien` gate fail → 400 thay vì ResponseResult (status code lệch).
- **Gate tiện ích copy-paste 5 lần** trong InvoiceController — sửa quy tắc gate phải sửa cả 5.
- **Đồng bộ nháp tách NCC theo `InvoiceStatus.TypeInvoice` (không theo payload)** — cố ý chống lệch NCC; cố ý KHÔNG return sớm khi MISA lỗi (`HoaDonDienTuService.cs:199-237`).
- **Typo "Invocie":** tên service `IMeInvocieService`, tiện ích `TI_Invocie_Misa/_Viettel` — đúng theo code, không sửa.

## File source (path:line)
- BE MISA: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Invoice\Invoice.API\Controllers\MeInvoiceController.cs` (`:39,51` ép NCC; `:113,121,135` bỏ key).
- BE Viettel: `...\Invoice\Invoice.API\Controllers\ViettelInvoiceController.cs` (`:16` tienich; `:51-54` fallback NCC; `:104,113,125` bỏ key).
- BE phát hành/tra cứu: `...\Invoice\Invoice.API\Controllers\InvoiceController.cs` (`:73,123,152,165,177` gate; `:81-117` bản cũ GetInvoiceFile comment).
- BE orchestrator: `...\Invoice\Invoice.Service\Generated\HoaDonDienTuService.cs` (`:91-144` TaoHoaDonDienTu; `:199-237` DongBoTrangThaiHoaDonNhap).
- BE cấu hình HĐ tự động: `...\Invoice\Invoice.Service\Generated\CauHinhService.cs` · `...\Invoice\Invoice.API\Controllers\CauHinhController.cs`.
- BE socket push: `...\SocketServer...\Controllers\InvoiceController.cs` (`:41-44` try/catch nuốt lỗi).
- DTO publish: `...\Libs\Models.Core\DTO\Invocie.Client\HoaDonDienTuDto\InputDataCreateInvoice.cs`.
- Stored procedures: không có (registry node ghi `stored_procs: []`).
