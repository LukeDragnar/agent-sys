# Dev — Thiết lập tác nghiệp

> Service: **Contact** (BE .NET) · FE module Sale (Angular 15 + ng-zorro). Controller: `DMTacNghiepCauHinhController`. Chỉ dành cho dev. Trace registry: `projects/scrm/registry/sales/DMTacNghiepCauHinhController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Thiết lập tác nghiệp | `sale/thiet-lap-tac-nghiep` | `DanhmucTacnghiepCauhinhComponent` |

- Mỗi bản ghi = 1 luật "NẾU `IdTacNghiepCan` cho ra `IdKetQua` THÌ làm `IdTacNghiepTiepTheo` sau `IdSauBaoLau`".

## Endpoint / Controller (trace registry)
**`DMTacNghiepCauHinhController`** (TienIch `TI_OrderLogistic`):
| MaTinhNang (SBAuthorize) | Route (HTTP) | Mục đích |
|---|---|---|
| `DMTacNghiepCauHinh_InsertAsync` | POST `InsertAsync` | Thêm luật (AD-TONG/CN/MCN) |
| `DMTacNghiepCauHinh_UpdateOneAsync` | POST `UpdateOneAsync` | Sửa luật (Id bắt buộc) |
| `DMTacNghiepCauHinh_TimTheoDieuKienAsync` | POST `TimTheoDieuKienAsync` | Lọc/list grid (join SauBaoLau, gắn "(Deleted)") |
| `DMTacNghiepCauHinh_DeleteOneItemAsync` | POST `DeleteOneItemAsync/{id:guid}` | Xóa luật |
| `DMTacNghiepCauHinh_CheckExistAsync` | POST `CheckExistAsync` | Validate trùng (cặp NẾU+KẾT QUẢ) |
| `DMTacNghiepCauHinh_DonViTacNghiepCopy` | POST `DonViTacNghiepCopy` | Copy mẫu — **ép `IdsDonVi = [CurrentUser.DonViId]`** |
| `DMTacNghiepCauHinh_CheckDaCauHinhTacNghiep` | GET `CheckDaCauHinhTacNghiep` | Đơn vị/CN đã thiết lập chưa |
| `DMTacNghiepCauHinh_TimTheoDieuKienByNganhNghe` | POST `TimTheoDieuKienByNganhNghe` | Tìm mẫu theo ngành nghề (hệ thống) |
| `[SBAuthorize()]` (chỉ login) | GET `LayThongTinThietLapTacNghiep` | Trả gói thông tin thiết lập — **authorize rỗng** |

Internal (`[PrivateSBAuthorize]`, service-to-service):
- `GetAllThietLapTacNghiepForNotificationConfig` / `...ByChiNhanh?idChiNhanh=` — Notification config đọc luật để dựng nhắc việc.
- `GetAllByDonViPrivate` — service khác lấy toàn bộ luật của đơn vị.

## Entities
- **`DMTacNghiepCauHinh`** — bản ghi luật: `IdTacNghiepCan`, `IdKetQua`, `IdTacNghiepTiepTheo`, `IdSauBaoLau` (+ các trường `Ten*` cache tên hiển thị), `IdDonVi`, `IdChiNhanh`.
- Tham chiếu: `DMTacNghiep` (vế NẾU + vế tiếp theo), `DMKetQuaTacNghiep` (vế KẾT QUẢ), `DMTacNghiepSauBaoLau` (mốc thời gian).

## Luồng code
- **Gắn chi nhánh theo cấu hình** (`DMTacNghiepCauHinhService.cs:183-187`): nếu `GetCauHinhInit().IsEnableTacNghiepTheoChiNhanh == true` → `entity.IdChiNhanh = userLogin.IdChiNhanh`; ngược lại null (dùng chung đơn vị).
- **Chống trùng** (`DMTacNghiepCauHinhService.cs:189-193, 327-331`): gọi `CheckExistAsync` trước insert/update; trùng → "Cấu hình tác nghiệp đã tồn tại, không thể lưu cấu hình."
- **List** (`TimTheoDieuKienAsync`): join `DMTacNghiepSauBaoLau`; nếu vế tham chiếu đã xóa → gắn "(Deleted)".
- **Copy** (endpoint `DonViTacNghiepCopy`): controller ép `model.IdsDonVi = [CurrentUser.DonViId]` rồi gọi `DonViTacNghiepCopy(model, CurrentUser)`; clear cache.
- Mỗi Insert/Update/Delete/Copy → **clear cache** `DMTacNghiepCauHinh` theo đơn vị + chi nhánh.

## Bug-prone / lưu ý (KHÔNG sửa ở đây — chỉ ghi)
- **Validate vế NẾU/KẾT QUẢ và cặp tiếp/sau-bao-lâu KHÔNG có hiệu lực chặn** (`DMTacNghiepCauHinhService.cs:141-148`): các nhánh gọi `MethodResult<bool>.ResultWithError(...)` **nhưng KHÔNG `return`** → kết quả validate bị bỏ, luồng vẫn chạy tiếp xuống insert. Tức luật có thể thiếu KẾT QUẢ, hoặc có bước tiếp theo nhưng thiếu "sau bao lâu", mà vẫn lưu được. Chỉ còn CheckExist + constraint DB (nếu có) chặn.
- **Endpoint `LayThongTinThietLapTacNghiep` quyền lỏng** (`DMTacNghiepCauHinhController.cs:113`): dùng `[SBAuthorize()]` rỗng (chỉ cần login) trong khi các endpoint khác yêu cầu role AD → user không phải admin vẫn đọc được thông tin thiết lập.
- **Copy bị override IdsDonVi**: bản đơn vị ép `IdsDonVi = [CurrentUser.DonViId]` (không copy chéo được — đây là chủ ý, nhưng payload gửi gì cũng bị bỏ, dễ gây hiểu nhầm "copy không chạy").
- **Hai bản controller sinh đôi**: ngoài bản đơn vị còn `Areas/SanboxArea/DMTacNghiepCauHinhController.cs` (route `api/QuanTriArea/DMTacNghiepCauHinh`, TienIch `TI_HeThong`, role `HT-AD, HT-CSKH`, gọi `HeThong*`). Bản Base `DonViTacNghiepCopy` chặn `if (!CurrentUser.IsSuperUser) return BadRequest` (chỉ super-user copy được). Sửa logic copy nhớ check cả 2 bản.
- **Lệch tiện ích**: `DMTacNghiep` gắn TienIch `TI_Marketing`, còn `DMTacNghiepCauHinh` gắn `TI_OrderLogistic` — cùng họ tác nghiệp nhưng 2 tiện ích khác nhau; đơn vị chỉ mua 1 trong 2 có thể bị chặn phân quyền tiện ích.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Contact\...\Controllers\DMTacNghiepCauHinhController.cs` (`:113` LayThongTinThietLapTacNghiep).
- BE controller (bản hệ thống): `...\Areas\SanboxArea\DMTacNghiepCauHinhController.cs`.
- BE service: `...\DMTacNghiepCauHinhService.cs:141-148` (validate bỏ return), `:183-187` (gắn chi nhánh), `:189-193, 327-331` (chống trùng).
- FE: component `DanhmucTacnghiepCauhinhComponent` (route `sale/thiet-lap-tac-nghiep`).
- Entities: `DMTacNghiepCauHinh`, `DMTacNghiep`, `DMKetQuaTacNghiep`, `DMTacNghiepSauBaoLau` (service Contact).
