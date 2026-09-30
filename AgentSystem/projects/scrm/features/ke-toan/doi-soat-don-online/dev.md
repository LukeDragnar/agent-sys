# Đối soát đơn online (Kế toán tác nghiệp) — Kỹ thuật

> Service **OrderLogistic**. Controller `KeToanTacNghiepController` (`partial`, 1 file). TienIch `KeToanTacNghiepController` / `MaTienIchSCRM.TI_KeToanLogistic`. Dùng 2 service: `IThuKhoTacNghiepService` (chung với ThuKhoTacNghiep/DonHangSale — đa số endpoint tác nghiệp) + `IKeToanTacNghiepService` (riêng — TimTheoDieuKien, ViewDetail, Export _V2). Registry: `registry/kho/KeToanTacNghiepController.md`.

## Màn hình & route (FE)
| Màn hình | Vị trí |
|---|---|
| Kế toán tác nghiệp (đối soát đơn online) | Kế toán > Kế toán tác nghiệp |

> Route FE cụ thể: cần dev xác nhận (không trace trong registry node).

## Endpoint/Controller (trace registry + source `KeToanTacNghiepController.cs`)
Quyền: `KeToanTacNghiep_*` + role kế toán (DV-KT, DV-KT-TONG, HT-CSKH); một số admin thêm DV-AD-*.

- `POST TimTheoDieuKien` (`:35`) — `IKeToanTacNghiepService`, list đơn theo bộ lọc kế toán.
- `POST TimTheo_TrangThaiGiaoHang` (`:43`).
- `POST OrderLogistic_KeToanTacNghiepViewDetail` (`:59`) — `IKeToanTacNghiepService`.
- `POST GetOrderLogisticByPhone` / `GetOrderLogisticHistoryByPhone` (`:66`,`:73`).
- `POST InsertDonHangDangDon`/`ProcessDonHangDangDon` (`:183`,`:197`) — queue đăng đơn (dùng `IThuKhoTacNghiepService`).
- `POST InsertDonHangHuyDangDon`/`ProcessDonHangHuyDangDon` (`:234`,`:241`) — queue hủy đăng.
- `POST AdminHuyDangDon`/`AdminDangDon` (`:95`,`:102`) — admin bỏ gate.
- `POST CapNhatTrangThaiGiaoHangTheoDon` (`:109`), `ThayDoiMaDon` (`:88`), `UpdateGhiChuKeToan` (`:174`), `UpdateTrangThaiCareDon` (`:264`), `UpdateNgayMuonNhanHang` (`:452`).
- `POST SaveMessage`/`GetListMessageByIdDonHangLogistic`/`GetListMessageByPhone` (`:134`,`:141`,`:148`).
- `POST InsertOrUpdateBlackList`/`DeleteBlackList` (`:158`,`:166`).
- `POST GetDonHangDangDonTotal`/`GetDonHangDangDonByUser`/`DeleteDonHangDangDonByStatus`/`DangDonSearch` + nhánh HuyDangDon (`:80`,`:190`,`:204`,`:211`,`:220`,`:227`,`:248`,`:271`).
- `POST ExportExcelKeToanTacNghiepKieu1..4` (`:286`,`:329`,`:369`,`:409`) — **`[SBAuthorize()]` rỗng** (chỉ login).

## Entities
- `DonHangLogistic` (header đơn), `DonHangDangDon`/`DonHangHuyDangDon` (hàng đợi đăng/hủy đăng), `DonHangLogisticMessage` (tin nhắn nội bộ), `ContactPhoneBlacklist`.
- Stored proc: `PRC_SCRM_OrderLogistic_DonHangDangDon_GetForDangDon`.

## Luồng code
- **List/Detail/Export** đi nhánh `IKeToanTacNghiepService` (`KeToanTacNghiepService.cs` — TimTheoDieuKien + ViewDetail + Export _V2).
- **Đăng/hủy đăng + tác nghiệp** đi nhánh `IThuKhoTacNghiepService` (`KhoTacNghiepService.cs`, dùng CHUNG với ThuKhoTacNghiep + DonHangSale) → mọi gate/known-issue luồng đăng đơn xem node `registry/kho/KhoTacNghiepController.md`.
- Đăng đơn: enqueue `DonHangDangDon` → `ProcessDonHangDangDon` bốc SP có lock → gửi DeliveryConnect (ĐTGV) + trừ kho (WarehouseProduct). Hủy đăng: ngược lại.

## Bug-prone / lưu ý (path:line)
- **"Đối soát nội bộ" bị tắt:** `UpdateDoiSoatNoiBo` (key `KeToanTacNghiep_UpdateDoiSoatNoiBo`, "Đối soát nội bộ") **comment chết** tại `KeToanTacNghiepController.cs:254-256`. Chức năng đánh dấu đơn "đã đối soát" hiện không có endpoint sống → đây là điểm cần dev/product xác nhận nếu muốn bật.
- **4 endpoint export bỏ quyền:** `[SBAuthorize()]` rỗng (`:286,329,369,409` ~ registry ghi `:284,327,367,407`) — chỉ cần đăng nhập.
- **Export kiểu 1 & 2 trùng tên file** `KeToanTacNghiep.xlsx` (`:316`,`:359`); kiểu 3/4 có hậu tố.
- **Endpoint comment chết khác:** `OrderLogistic_KeToanTacNghiepCount` (`:51`), `AdminDangDon` bản 2 (`:117`), `TimTheoId` (`:124`).
- **Đăng đơn dùng chung service** với ThuKhoTacNghiep — đơn TMĐT chặn hủy đăng ở service chung.
- **Lưu ý đừng nhầm** với `DonHangKeToanController` (cùng service OrderLogistic) — controller đó lo **đồng bộ đơn online → tạo phiếu bán hàng kế toán** (xem feature "Tạo hóa đơn tự động").

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\OrderLogistic\OrderLogistic.API\Controllers\KeToanTacNghiepController.cs` (`:254-256` UpdateDoiSoatNoiBo comment; `:286/329/369/409` export bỏ quyền).
- BE service riêng: `...\OrderLogistic\OrderLogistic.Service\Generated\...\KeToanTacNghiepService.cs`.
- BE service chung (đăng đơn): `...\OrderLogistic\OrderLogistic.Service\Generated\KhoTacNghiepService.cs`.
- Registry ripple: `registry/kho/KhoTacNghiepController.md` (luồng đăng đơn), `registry/kho/DonHangKeToanController.md` (đồng bộ kế toán).
