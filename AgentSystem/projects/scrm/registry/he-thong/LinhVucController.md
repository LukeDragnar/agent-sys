---
controller: LinhVucController
module: he-thong
service: Core
man_hinh_ui: "Quản trị hệ thống (QuanTriArea) > Quản lý lĩnh vực / ngành nghề: list + form thêm/sửa/xóa lĩnh vực; nút Lấy tất cả; màn cấu hình tiện ích theo lĩnh vực (chọn tiện ích cho ngành nghề)"
muc_dich: "CRUD danh mục lĩnh vực (ngành nghề) ở khu quản trị hệ thống, và cấu hình các tiện ích (TienIch) gắn cho từng lĩnh vực — quyết định đơn vị thuộc lĩnh vực nào được bật những tiện ích nào."
alias: ["LinhVuc", "lĩnh vực", "ngành nghề", "cấu hình tiện ích theo lĩnh vực"]
auth: public
state_machine: []
deprecated: false
entities: [LinhVuc, LinhVucTienIch]
stored_procs: []
depends: ["ILinhVucService", "AutoMapper (LinhVucDto ↔ LinhVuc)"]
cross_module_in: ["FE Quản trị hệ thống/Lĩnh vực gọi CRUD + cấu hình tiện ích (role HT-AD)", "Màn cấu hình đơn vị tham chiếu lĩnh vực để quyết định bộ tiện ích"]
cross_module_out: ["Cấu hình tiện ích theo lĩnh vực ảnh hưởng tới bộ tính năng (TienIch) khả dụng của đơn vị thuộc lĩnh vực đó"]
---

# LinhVucController — Quản lý lĩnh vực & cấu hình tiện ích theo lĩnh vực

CRUD danh mục **LinhVuc** (ngành nghề) + cấu hình tiện ích (TienIch) cho từng lĩnh vực. Bản **Areas/SanboxArea**, route `api/QuanTriArea/LinhVuc/...`, `[TienIch(..., TI_HeThong, true)]` (khu quản trị admin). `partial class`. Map DTO↔entity bằng AutoMapper; logic ở `ILinhVucService`. Đa số action yêu cầu role **HT-AD**; riêng `TimTheoId` và `TimTheoDieuKien` chỉ `[SBAuthorize]` (login).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết | Mở | GET `api/QuanTriArea/LinhVuc/TimTheoId/{id:Guid?}` | — (chỉ `[SBAuthorize]` login) | public | — | `GetByIdAsync(id)`. | EF `LinhVuc` | Permission gốc bị comment (dòng //ChiNhanh_TimTheoId) → còn `[SBAuthorize]` trống; id optional nhưng method nhận Guid non-nullable |
| 2 | Thêm lĩnh vực | Thêm | POST `api/QuanTriArea/LinhVuc/ThemMoi` | `LinhVuc_ThemMoi` (HT-AD) | public | ModelState valid | Map Dto→LinhVuc → `InsertAsync`. | EF | XML doc ghi "Tạo chi nhánh mới" (copy-paste sai mô tả) |
| 3 | Sửa lĩnh vực | Lưu | POST `api/QuanTriArea/LinhVuc/CapNhatThongTin` | `LinhVuc_CapNhatThongTin` (HT-AD) | public | ModelState valid; bản ghi tồn tại (GetById ≠ null) | Kiểm tra tồn tại → `UpdateAsync`; không thấy → `MESS_NotFound`. | EF | — |
| 4 | Xóa lĩnh vực | Xóa | POST `api/QuanTriArea/LinhVuc/Xoa/{id:Guid}` | `LinhVuc_Xoa` (HT-AD) | public | bản ghi tồn tại | GetById ≠ null → `DeleteAsync` → `MESS_Delete_Success`. | EF | Không kiểm tra ràng buộc đơn vị đang thuộc lĩnh vực này (xóa có thể để đơn vị mồ côi lĩnh vực) |
| 5 | Lấy tất cả | (combo/list) | GET `api/QuanTriArea/LinhVuc/LayTatCa` | `LinhVuc_LayTatCa` (HT-AD) | public | — | `GetAllAsync()` (method `virtual`). | EF | — |
| 6 | Tìm theo điều kiện | Grid / Lọc | POST `api/QuanTriArea/LinhVuc/TimTheoDieuKien` | — (login) | public | — | `TimTheoDieuKienAsync(LinhVucFilterDto)`. | EF | Không gắn permission key (khác các action HT-AD) |
| 7 | Lấy tiện ích theo lĩnh vực | (màn cấu hình) | GET `api/QuanTriArea/LinhVuc/GetTienIchTheoLinhVuc/{id:Guid}` | `LinhVuc_GetTienIchTheoNganhNghe` (HT-AD) | public | — | `GetTienIchTheoLinhVuc(id)` — list tiện ích đang gắn cho lĩnh vực. | EF `LinhVucTienIch` | Route `GetTienIchTheoLinhVuc` nhưng key/method tên "NganhNghe" — lệch danh xưng lĩnh vực/ngành nghề |
| 8 | Cấu hình tiện ích cho lĩnh vực | Lưu cấu hình | POST `api/QuanTriArea/LinhVuc/CauHinhTienIchTheoLinhVuc` | `LinhVuc_CauHinhNganhNgheTienIch` (HT-AD) | public | — | `CauHinhTienIchTheoNganhNghe(RequestLinhVucTienIchDto)` — gán bộ tiện ích cho lĩnh vực. | EF | Tên route/key/method lẫn lộn "LinhVuc"/"NganhNghe" → khó tra |

## Quy tắc nghiệp vụ (gating / tier)
- **Lĩnh vực → bộ tiện ích:** `CauHinhTienIchTheoLinhVuc` quyết định lĩnh vực nào bật những TienIch nào → đơn vị thuộc lĩnh vực đó kế thừa bộ tiện ích. Đây là điểm gating tính năng theo ngành nghề.
- Sửa/Xóa luôn kiểm tra tồn tại trước (GetById) → không thấy trả `MESS_NotFound` (không ném exception).

## Quan hệ
- **Gọi sang:** `ILinhVucService` (CRUD + GetTienIchTheoLinhVuc + CauHinhTienIchTheoNganhNghe); AutoMapper map `LinhVucDto`↔`LinhVuc`.
- **Dữ liệu (entities):** `LinhVuc` (danh mục ngành nghề), `LinhVucTienIch` (map lĩnh vực ↔ tiện ích). DTO: `LinhVucDto`, `LinhVucFilterDto`, `RequestLinhVucTienIchDto`.
- **Ripple / đồng bộ:** cấu hình tiện ích theo lĩnh vực → bộ tính năng khả dụng cho đơn vị thuộc lĩnh vực; xóa lĩnh vực có thể ảnh hưởng đơn vị đang tham chiếu.

## Lỗi đã biết / lưu ý kỹ thuật
- `LinhVucController.cs:13` — `[TienIch(..., TI_HeThong, true)]` khu QuanTriArea admin; route prefix `api/QuanTriArea/LinhVuc`.
- `LinhVucController.cs:31` — permission gốc `ChiNhanh_TimTheoId` bị comment, để lại `[SBAuthorize]` trống → `TimTheoId` chỉ cần login; `TimTheoDieuKien` (dòng 117) cũng không có key → 2 endpoint đọc dữ liệu lỏng quyền hơn phần CRUD.
- `LinhVucController.cs:26-43` — XML doc/method lẫn lộn copy từ ChiNhanh ("Tạo chi nhánh mới", "ChiNhanh_TimTheoId") và lẫn lộn danh xưng **LinhVuc** vs **NganhNghe** (route `GetTienIchTheoLinhVuc` ↔ method `GetTienIchTheoNganhNghe` ↔ key `LinhVuc_GetTienIchTheoNganhNghe`) → dễ nhầm khi tra cứu.
- `TimTheoId/{id:Guid?}` khai báo id optional nhưng tham số method là `Guid` non-nullable → gọi không kèm id sẽ không khớp/lỗi bind (cần xác nhận hành vi route).
