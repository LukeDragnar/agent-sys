# Dev — Pre-Order (đặt trước qua QR)

> Service: **PreOrder** (BE .NET). 4 controller: **BangQRCodeController** (quản lý QR), **PublicPreOrderController** (khách quét QR — AllowAnonymous), **DonHangPreController** (admin list/chi tiết/xóa), **PrivateDonHangPreController** (nhập đơn vào POS — internal). Chỉ dành cho dev.

## Màn hình & route
| Màn hình | Loại | Ghi chú |
|---|---|---|
| Cấu hình QR PreOrder | admin (cần login) | Cửa hàng / PreOrder — gán QR cho bàn, list, xóa |
| Danh sách đặt trước | admin (cần login) | lọc ngày/trạng thái/chỗ ngồi/keyword, chi tiết, xóa |
| Trang khách quét QR | public (mobile web, AllowAnonymous) | menu + gửi đơn + tra đơn |

## Endpoint/Controller (trace registry)

### BangQRCodeController (trace BangQRCode_PreOrder)
Node: `registry/cua-hang/BangQRCodeController.md`. `auth: public`.
| Quyền (key) | Route | Hành vi |
|---|---|---|
| `BangQRCode_PreOrder_TaoMaQR` | POST `api/BangQRCode/GetOrCreate` | tìm theo (DonViId+IdChiNhanh+IdChoNgoi); có → trả cũ; chưa → sinh MaQR 8 ký tự + insert (idempotent) |
| `BangQRCode_PreOrder_XemTheoChiNhanh` | GET `api/BangQRCode/GetByChiNhanh?idChiNhanh=` | list theo (DonViId+idChiNhanh), không phân trang |
| `BangQRCode_PreOrder_XoaMaQR` | POST `api/BangQRCode/Delete/{id:Guid}` | xóa **cứng** theo (Id+DonViId) |

`GetByMaQR(maQR)` (service) = entry point cho luồng public resolve đơn vị/chi nhánh/chỗ ngồi.

### DonHangPreController (trace DonHangPre)
Node: `registry/cua-hang/DonHangPreController.md`. `auth: public`, state machine `[0_ChuaXuLy, 1_DaTaoDonHang, 2_DaThanhToan]`.
| Quyền (key) | Route | Hành vi |
|---|---|---|
| `DonHangPre_TimTheoDieuKien` | POST `api/DonHangPre/TimTheoDieuKien` | lọc DonViId + ngày; **role gating**: không DV-AD-TONG → ép IdChiNhanh=chi nhánh user |
| `DonHangPre_GetChiTiet` | GET `api/DonHangPre/GetByMaPreOrder?maPreOrder=` | trả full dto theo mã — **KHÔNG filter đơn vị/chi nhánh** (known-issue) |
| `DonHangPre_Xoa` | POST `api/DonHangPre/Delete` | xóa **mềm** (IsDelete=true); không DV-AD-TONG → chỉ chi nhánh mình |

### PublicPreOrderController (AllowAnonymous)
Node: `registry/cua-hang/PublicPreOrderController.md`. Kế thừa `ControllerBase` (không `BaseController` → không có CurrentUser; ngữ cảnh lấy từ maQR).
| Route | Hành vi |
|---|---|
| GET `public/PublicPreOrder/GetSanPham?maQR=` | menu theo QR (GetSanPhamForPreOrder + cache CoCau) |
| POST `public/PublicPreOrder/Save` | validate + sinh MaPreOrder + (tùy chọn) tạo khách + insert đơn TrangThai=0 |
| GET `public/PublicPreOrder/GetByMaPreOrder?maPreOrder=` | tra đơn theo mã (không filter đơn vị/chi nhánh) |

### PrivateDonHangPreController (`[PrivateSBAuthorize]`, internal)
Node: `registry/cua-hang/PrivateDonHangPreController.md`.
- `GetFullByMaPreOrder` (GET, query maPreOrder/idDonVi/idChiNhanh) — filter chặt đơn vị+chi nhánh; gating TrangThai 1/2 → "Mã PreOrder này đã được sử dụng!".
- `MarkImported` (POST `DonHangPreMarkImportedDto`) — set TrangThai=1 + gắn IdDonHang/MaDonHang.

## Entities
- `BangQRCode` (Id, IdDonVi, IdChiNhanh, IdChoNgoi, MaQR, TenDonVi, TenChiNhanh, TenChoNgoi, TenKhuVuc).
- `DonHangPre` (MaPreOrder, TrangThai, IdDonVi, IdChiNhanh, IdChoNgoi, TenKhachHang, SoDienThoai, IdKhachHang, IdDonHang, MaDonHang, IsDelete) + `DonHangPreSanPham` + `DonHangPreSanPhamDinhKem`.

## Luồng code
- Service chung: `BangQRCodeService` (GetByMaQR, sinh MaQR retry), `DonHangPreService` (Save / list / GetFull / MarkImported / MarkThanhToan).
- `DonHangPreService.Save`: chuỗi validate (list SP rỗng → XOR tên/SĐT → claim MaPreOrder Redis `preorder:ma:{code}` TTL 5' retry 10 lần → tạo/tìm khách → insert đơn + SP + đính kèm → nhả claim).
- Gọi sang: WarehouseProduct `GetSanPhamForPreOrder`, CommonCache `GetCoCauDonViTuCache`, Customer `FindOrCreatedCustomResult`, Redis (claim mã).
- Cron: `JobClearDonHangPreExpired` (~2h sáng) xóa cứng đơn TrangThai=0 quá `SoNgayLuuDonHangPre` (mặc định 30).

## Bug-prone / lưu ý (path:line) — KHÔNG sửa ở session này
- **BangQRCode xóa cứng** — QR đã in/dán mất hiệu lực ngay (GetByMaQR null → public "đã bị xóa"). Khác DonHangPre dùng xóa mềm.
- **GetOrCreate có khe đua** — `BangQRCodeService` dùng `TableWithNolock()`, không lock/unique constraint tầng app → 2 request đồng thời cùng (DV+CN+ChoNgoi) có thể tạo 2 QR.
- **DonHangPre.GetByMaPreOrder (UI) không filter đơn vị/chi nhánh** (`DonHangPreService:199`) — ai có quyền `DonHangPre_GetChiTiet` + biết mã đều xem được (khác bản Private có check).
- **PublicPreOrder lock chống spam theo IP đã bị comment** (`PublicPreOrderController:43-49`) — endpoint public không còn rate-limit tầng app → dễ spam scan/tạo đơn rác.
- **public/GetByMaPreOrder không kiểm quyền sở hữu** — lộ tên/SĐT khách nếu biết/đoán mã PO-xxxxxx (6 ký tự).
- **Gọi Customer lỗi không rollback đơn** — đơn vẫn lưu, chỉ thiếu IdKhachHang.
- **Sản phẩm đính kèm nhân đôi vai trò** — vừa bản ghi DinhKem vừa 1 dòng DonHangPreSanPham (IsSanPhamDinhKem=true) → tránh đếm trùng khi tính tiền.
- **MarkImported không idempotent guard** (`PrivateDonHangPreController`) — gọi 2 lần với idDonHang khác sẽ ghi đè; tuy GetFull đã chặn nhập lại ở bước trước.
- **Mọi query `TableWithNolock()`** ở cả 3 service → đọc dirty.

## File source (path:line)
- BE QR: `…\scrm\PreOrder\PreOrder.API\Controllers\BangQRCodeController.cs`, service `…\PreOrder.Service\Generated\BangQRCodeService.cs` (sinh MaQR ~L138), entity `…\PreOrder.DataContext\Entity\BangQRCode.cs`.
- BE admin list: `…\PreOrder\PreOrder.API\Controllers\DonHangPreController.cs`, service `…\Generated\DonHangPreService.cs` (GetByMaPreOrder ~L199).
- BE public: `…\PreOrder\PreOrder.API\Controllers\Public\PublicPreOrderController.cs` (lock IP comment L43-49), service `DonHangPreService.cs` (Save).
- BE private: `…\PreOrder\PreOrder.API\Controllers\Private\PrivateDonHangPreController.cs`, service `DonHangPreService.cs` (GetFullByMaPreOrder/MarkImported).
- Entity đơn: `…\PreOrder.DataContext\Entity\DonHangPre.cs`.
