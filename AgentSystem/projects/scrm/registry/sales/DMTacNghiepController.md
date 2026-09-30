---
controller: DMTacNghiepController
module: sales
service: Contact
man_hinh_ui: "Sale > Danh mục tác nghiệp (route sale/danh-muc-tac-nghiep — DanhmucTacnghiepComponent)"
muc_dich: "CRUD danh mục tác nghiệp (các bước thao tác/follow-up sale dùng để chăm sóc kho số/đơn): tên, màu, cờ khởi đầu, khóa số, sửa giờ. Cung cấp danh sách tác nghiệp cho cấu hình thiết lập follow-up và các service khác."
alias: [DMTacNghiep, "danh mục tác nghiệp", "tác nghiệp", "thao tác sale", "follow-up step"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMTacNghiep]
stored_procs: []
depends:
  - "DMTacNghiepService (IDMTacNghiepService — Insert/Update/TimTheoDieuKien/Delete; bản hệ thống HeThong*)"
  - "GetCauHinhInit (cờ IsEnableTacNghiepTheoChiNhanh — quyết định gắn IdChiNhanh hay không)"
cross_module_in:
  - "DMTacNghiepCauHinh dùng tác nghiệp này làm IdTacNghiepCan / IdTacNghiepTiepTheo khi cấu hình follow-up"
  - "DMTacNghiepImport (import danh mục) ghi vào cùng entity DMTacNghiep"
  - "Service khác gọi GetAllPrivate (PrivateSBAuthorize) lấy snapshot danh mục"
cross_module_out:
  - "Cache DMTacNghiep theo đơn vị/chi nhánh (clear khi đổi danh mục)"
---

# DMTacNghiep — Danh mục tác nghiệp (sale follow-up step)

> CRUD danh mục (86 dòng) cho entity `DMTacNghiep` = các bước tác nghiệp sale (gọi điện, hẹn lịch, chốt...). Mỗi bản ghi có cờ `IsKhoiDau` (bước khởi đầu), `IsKhoSo` (khóa số), `IsSuaGio`. Hành vi phân nhánh theo cờ `IsEnableTacNghiepTheoChiNhanh`: bật → gắn `IdChiNhanh`, tắt → dùng chung đơn vị. Controller này CRUD thuần; rule follow-up nằm ở `DMTacNghiepCauHinh`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới | Lưu (thêm) | POST `InsertAsync` | `DMTacNghiep_InsertAsync` (AD-TONG/CN/MCN) | public | Nút Thêm | `InsertAsync(model)` — insert DMTacNghiep theo đơn vị. | EF | — |
| 2 | Cập nhật | Lưu (sửa) | POST `UpdateOneAsync` | `DMTacNghiep_UpdateOneAsync` (AD-TONG/CN/MCN, MKT/SALE-LEAD, QL-SALE/MKT) | public | Form edit | `UpdateOneAsync(model, CurrentUser)`. Quyền cập nhật rộng hơn thêm/xóa (gồm cả lead/QL). | EF | — |
| 3 | Tìm theo điều kiện (grid) | Danh sách / Lọc | GET `TimTheoDieuKienAsync` | `DMTacNghiep_TimTheoDieuKienAsync` (nhiều role: AD, QL/LEAD/SALE/MKT, CHIASO) | public | Mở màn danh mục | `TimTheoDieuKienAsync(CurrentUser)` — list theo đơn vị/chi nhánh. | EF | — |
| 4 | Tìm theo chi nhánh | (load combo) | GET `TimTheoDieuKienChiNhanhAsync?idChiNhanh=` | `[SBAuthorize]` (chỉ login) | public | — | `TimTheoDieuKienChiNhanhAsync(idChiNhanh, CurrentUser)`. **Authorize rỗng** → mọi user login gọi được. | EF | Quyền lỏng hơn endpoint #3 |
| 5 | Lấy theo chi nhánh (UI) | — | GET `GetAllDMTacNghiepByChiNhanh` | `[SBAuthorize]` (chỉ login) | public | — | `GetAllDMTacNghiepByChiNhanh(CurrentUser)`. | EF | Authorize rỗng |
| 6 | Xóa | Xóa | POST `DeleteOneItemAsync/{id:guid}` | `DMTacNghiep_DeleteOneItemAsync` (AD-TONG/CN/MCN) | public | Nút Xóa | `DeleteOneItemAsync(id, CurrentUser)`. | EF | — |
| 7 | Tìm theo ngành nghề (hệ thống) | — | POST `TimTheoDieuKienByNganhNghe` | `DMTacNghiep_TimTheoDieuKienByNganhNghe` (AD-TONG/CN/MCN) | public | — | `HeThongTimTheoDieuKienAsync(model, CurrentUser)` — lọc theo ngành nghề (dùng cho catalog hệ thống). | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GetAllPrivate` (`[PrivateSBAuthorize]`) — service khác lấy toàn bộ danh mục tác nghiệp theo đơn vị (snapshot không qua quyền user).

## Quan hệ
- **Gọi sang:** `IDMTacNghiepService`, `GetCauHinhInit` (đọc cờ tác nghiệp theo chi nhánh).
- **Dữ liệu (entities):** `DMTacNghiep` (Ten, Color, IsKhoiDau, IsKhoSo, KhoSoThoiGian, IsSuaGio).
- **Ripple / đồng bộ:** cache danh mục theo đơn vị/chi nhánh; `DMTacNghiepCauHinh` và `DMTacNghiepImport` phụ thuộc dữ liệu entity này.

## Lỗi đã biết / lưu ý kỹ thuật
- Có **bản sinh đôi Base** `Areas/SanboxArea/DMTacNghiepController.cs` route `api/QuanTriArea/DMTacNghiep` (TienIch `TI_HeThong`, isAdmin=true), quyền role `HT-AD, HT-CSKH` (quản trị hệ thống) thay vì role đơn vị; gọi các method `HeThong*` của cùng service. Cùng permission key nhưng khác route + role → đừng nhầm 2 màn.
- `DMTacNghiepController.cs:48,63` — `TimTheoDieuKienChiNhanhAsync` và `GetAllDMTacNghiepByChiNhanh` dùng `[SBAuthorize]` **rỗng** (chỉ cần login) trong khi list chính (#3) yêu cầu role → khe hở phân quyền nếu lộ ra UI.
- `DMTacNghiepController.cs:32` — `UpdateOneAsync` cho phép thêm cả role lead/QL (MKT-LEAD, SALE-LEAD, QL-SALE, QL-MKT) sửa danh mục, rộng hơn Insert/Delete (chỉ AD) → drift quyền giữa CRUD.
