# Dev — Chứng từ khác (Chứng từ nghiệp vụ khác)

> Service: **Accounting** (BE .NET) + cross-service MISA / ButToan / PhieuKeToanHistory. Controller `ChungTuKhacController`. Trace registry: `registry/ke-toan/ChungTuKhacController.md`. Chỉ dành cho dev.

## Màn hình & route
- **Kế toán → Chứng từ nghiệp vụ khác**: popup lập (đa dòng Nợ/Có) + lưới danh sách + chi tiết + xóa + xuất Excel.

## Endpoint / Controller (trace registry)
**`ChungTuKhacController`** (`auth: public`; `[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]`):
| Hành động | Endpoint | Quyền |
|---|---|---|
| Lập chứng từ | `POST api/ChungTuKhac/InsertChungTu` | `[SBAuthorize()]` trống |
| Danh sách | `POST api/ChungTuKhac/TimTheoDieuKien` | `ChungTuKhac_TimTheoDieuKien` |
| Chi tiết | `POST api/ChungTuKhac/ViewChiTiet?idChungTu=` | `[SBAuthorize()]` trống |
| Xóa | `POST api/ChungTuKhac/XoaPhieu?idChungTu=` | `[SBAuthorize()]` trống |
| Xuất Excel | `POST api/ChungTuKhac/ExportExcelChungTuKhac` | `[SBAuthorize()]` trống |

> Không thấy endpoint cập nhật chứng từ riêng trong registry — `cần product xác nhận` luồng sửa.

## Entities
- `ChungTuKhac` + `ChungTuKhacChiTiet` (mỗi chi tiết = cặp TkNo/TkCo + đối tượng Nợ (NhomDoiTuongNo/IdDoiTuongNo...) + đối tượng Có (...Co) + SoTien).
- `CauHinhKetNoiKeToan` (đọc để quyết định đẩy MISA); `DataReceiveTaoPhieu` (queue MISA); `PhieuKeToanHistory` (qua service xóa).
- DTO: `Models.Core.DTO.Accounting.Client.ChungTuKhac` (ChungTuKhacRequest, ChungTuKhacGridInfo, chi tiết Nợ/Có).

## Luồng code
- **InsertChungTu** (`ChungTuKhacService.cs`): `ValidateRequest` → sinh/chống trùng `SoChungTu` → insert phiếu + N chi tiết trong EF transaction. Sau commit: với mỗi chi tiết đồng bộ đối tượng Nợ & Có MISA (`CheckDongBoDoiTuong`); tìm `CauHinhKetNoiKeToan` mặc định (CN → fallback `IsApDungDonVi=true` đơn vị, L214-226) → có thì insert `DataReceiveTaoPhieu(Status=Received)`; cuối cùng `ButToan_PhieuChungTuKhac_Save(chungTuKhac.Id, null)`.
- **TimTheoDieuKien**: non-admin ép `IdChiNhanh`; lọc DonVi + CN + keyword(SoChungTu/DienGiai) + khoảng ngày `NgayChungTu` + `TrangThaiDongBo` (ChuaDongBo→StatusSendPartner null); paging DESC `NgayCapNhat`.
- **XoaPhieu** (`...Service.cs:346`): `IdHoaDon != null` → chặn (chứng từ tạo tự động); hợp lệ → `PhieuKeToanHistoryService.DeletePhieuKeToan`.

## Bug-prone / lưu ý
- **Lập & xóa chứng từ không kiểm permission key** (`ChungTuKhacController.cs:49` là endpoint duy nhất có key): `InsertChungTu`/`XoaPhieu`/`ViewChiTiet`/`ExportExcelChungTuKhac` đều `[SBAuthorize()]` trống → ghi/xóa chứng từ hạch toán chỉ cần login. Rủi ro phân quyền.
- **Message xóa sai ngữ cảnh** (`ChungTuKhacService.cs:346`): trả "Id phiếu bù trừ không hợp lệ" — copy nhầm từ BuTru. Chỉ ảnh hưởng text.
- **Sinh bút toán với `userLogin=null`** (`...Service.cs:256`): `ButToan_PhieuChungTuKhac_Save(id, null)` — đúng pattern nội bộ nhưng lưu ý truy vết (không gắn user vào bút toán).
- **Đẩy MISA fallback CN→đơn vị** (`...Service.cs:214-226`): không có cấu hình → phiếu vẫn lưu nhưng không vào queue đẩy (user tích thủ công đẩy sau).

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Accounting\Accounting.API\Controllers\ChungTuKhacController.cs:49` (TimTheoDieuKien key; còn lại trống).
- BE service: `...\Accounting.Service\Generated\ChungTuKhacService.cs:214-226` (cấu hình MISA), `:256` (ButToan null), `:346` (message sai).
- SP: không có (toàn EF).
- Cross-service: `MisaDanhMucSync.CheckDongBoDoiTuong`, `ButToanService.ButToan_PhieuChungTuKhac_Save`, `PhieuKeToanHistoryService.DeletePhieuKeToan`.
