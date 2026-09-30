---
controller: PageMobileController (SanboxArea / QuanTriArea)
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Quản lý Page Mobile (route api/QuanTriArea/PageMobile): lưới + form Thêm/Sửa/Xóa, gán role; backup/template/import Excel; thêm key mobile (có bản _Encrypt)"
muc_dich: "Biến thể QUẢN TRỊ của PageMobile: CRUD page mobile + gán role theo page, thêm key mobile (kèm _Encrypt mã hóa), và backup/template/import từ Excel. Khác bản Controllers (chỉ đọc)."
alias: ["quản lý page mobile", "import page mobile", "backup page mobile", "key mobile quản trị", "PageMobile QuanTriArea"]
auth: public
state_machine: []
deprecated: false
entities: [PageMobile]
stored_procs: []
depends: ["IPageMobileService", "BaseController", "AES_ECB_128 (mã hóa)", "ExcelService + GemBox.Spreadsheet", "AutoMapper"]
cross_module_in: ["FE Quản trị/PageMobile gọi CRUD + ThemMoiKeyMobile + backup/template/import"]
cross_module_out: ["Import/CRUD page mobile thay đổi danh sách màn mobile → ảnh hưởng key mobile app đọc (GetAllKeyMobile)"]
---

# PageMobileController (QuanTriArea) — Quản lý page mobile

Bản QuanTriArea, kế thừa `BaseController`, controller-level `[SBAuthorize]` + `[TienIch(TI_HeThong)]`. CRUD đầy đủ + gán role + thêm key mobile (kèm `_Encrypt`) + backup/template/import Excel. Các endpoint đọc key (GetAllKeyMobile...) trùng bản Controllers nhưng đặt cùng controller này theo route QuanTriArea.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết | (mở form) | GET `api/QuanTriArea/PageMobile/TimTheoId/{id:Guid}` | `PageMobile_TimTheoId` (HT-AD, HT-MB) | public | — | `GetByIdAsync` + `GetRolesByPageAsync` → map kèm Roles. | EF | — |
| 2 | Tìm theo điều kiện | (load lưới) | POST `api/QuanTriArea/PageMobile/TimTheoDieuKien` | `PageMobile_TimTheoDieuKien` (HT-AD, HT-MB) | public | — | Lọc theo `PageMFilterDto`. | EF | — |
| 3 | Thêm mới | Thêm | POST `api/QuanTriArea/PageMobile/ThemMoi` | `PageMobile_ThemMoi` (HT-AD, HT-MB) | public | — | Valid → map → `InsertAsync` → nếu OK `UpdateRolesByPageAsync`. | EF | — |
| 4 | Cập nhật | Sửa | POST `api/QuanTriArea/PageMobile/CapNhatThongTin` | `PageMobile_CapNhatThongTin` (HT-AD, HT-MB) | public | — | Valid → kiểm tra tồn tại → `UpdateAsync` → cập nhật roles. | EF | — |
| 5 | Xóa | Xóa | POST `api/QuanTriArea/PageMobile/Xoa/{id:Guid}` | `PageMobile_Xoa` (HT-AD, HT-MB) | public | — | Kiểm tra tồn tại → `DeleteAsync`. | EF | — |
| 6 | Thêm key mobile | (thêm key) | POST `api/QuanTriArea/PageMobile/ThemMoiKeyMobile` (+ `_Encrypt`) | — (login) | public | — | `ThemMoiKeyMobile(model)`; bản `_Encrypt` giải mã Key/ParentKey/DisplayName (AES) trước khi lưu, trả kết quả mã hóa lại. | EF | — |
| 7 | Backup Excel | Download backup | GET `api/QuanTriArea/PageMobile/DownloadPageMobileBackup` | `PageMobile_DownloadPageMobileBackup` (HT-AD, HT-MB) | public | — | Xuất toàn bộ page mobile ra file `PageMobile_Backup.xlsx`. | EF + Excel | — |
| 8 | File mẫu import | Download template | GET `api/QuanTriArea/PageMobile/DownloadFileTemplate` | `PageMobile_DownloadFileTemplate` (HT-AD, HT-MB) | public | — | Tải file template Excel import. | Excel | Tên file template cũng là `PageMobile_Backup.xlsx` (trùng tên file backup) |
| 9 | Import Excel | Import | POST `api/QuanTriArea/PageMobile/ImportPageMobile` (IFormFile) | `PageMobile_ImportPageMobile` (HT-AD, HT-MB) | public | — | Đọc Excel (Url, DisplayName, RolesCode, IsNotImport) → `ImportPageMobile(datas)`; file rỗng/không có dòng → báo lỗi. | EF + Excel | — |
| 10 | Đọc key mobile | (mobile/admin) | GET `GetAllKeyMobile`/`GetKeyByUserMobile` (+ `_Encrypt`), `LayTatCa`, `GetAllForMobileAsync` | — (login) | public | — | Đọc key/list (trùng chức năng bản Controllers, route QuanTriArea). | EF | — |

## Quan hệ
- **Gọi sang:** IPageMobileService (EF), AES_ECB_128, ExcelService (GemBox), AutoMapper.
- **Dữ liệu (entities):** `PageMobile` (+ role-by-page mapping).
- **Ripple / đồng bộ:** Import/CRUD đổi danh sách page mobile → app mobile đọc key mới qua GetAllKeyMobile.

## Lỗi đã biết / lưu ý kỹ thuật
- `DownloadFileTemplate` (dòng 282) đặt tên file `PageMobile_Backup.xlsx` giống endpoint backup → dễ nhầm file template với file backup.
- Bản này KHÁC `Controllers/PageMobileController` (chỉ đọc): toàn bộ ghi (CRUD/import/key) ở đây.
