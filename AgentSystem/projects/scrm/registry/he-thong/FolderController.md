---
controller: FolderController
module: he-thong
service: File
man_hinh_ui: "Quản lý tài liệu / Thư mục đơn vị (cây folder + file đính kèm, thùng rác khôi phục)"
muc_dich: "Quản lý cây thư mục theo đơn vị: tạo/sửa/xóa folder, upload & xóa file trong folder, thùng rác (recover/remove vĩnh viễn)."
alias: ["thư mục", "folder", "quản lý tài liệu", "thùng rác file", "drive nội bộ"]
auth: internal
state_machine: []
deprecated: false
entities: [Files, Folders]
stored_procs: []
depends: ["IFoldersService", "IFilesService", "ImageSettings"]
cross_module_in: ["Màn hình Quản lý tài liệu/Thư mục của FE gọi toàn bộ endpoint"]
cross_module_out: ["Ghi bảng Folders & Files; lưu file vật lý wwwroot/Files"]
---

# FolderController — Quản lý thư mục & tài liệu theo đơn vị

Controller quản lý cây thư mục theo `CurrentUser` (đơn vị/chi nhánh): liệt kê folder, CRUD folder, upload nhiều file vào folder, và bộ thao tác thùng rác (xem/khôi phục/xóa vĩnh viễn folder & file). Toàn bộ endpoint dùng `[SBAuthorize()]` (chỉ check đăng nhập, không key chi tiết) và truyền `CurrentUser` xuống service để scope theo đơn vị.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách folder theo đơn vị | (cây thư mục) | GET `/api/Folder/GetListFolderByUnitId` | — (`[SBAuthorize()]`) | internal | — | Trả cây folder scope theo `CurrentUser`. | EF | — |
| 2 | Tạo folder | (nút Tạo) | POST `/CreateFolderByUnitId` | — | internal | — | Tạo folder mới gắn đơn vị. | EF | — |
| 3 | Danh sách file trong folder | (mở folder) | POST `/GetListFileByFolderId` | — | internal | — | Liệt kê file thuộc folder. | EF | — |
| 4 | Xóa folder (vào thùng rác) | (nút Xóa) | POST `/RemoveFolderById` | — | internal | — | Chuyển folder vào thùng rác. | EF | — |
| 5 | Sửa folder | (nút Sửa) | POST `/UpdateFolderById` | — | internal | — | Đổi tên/thuộc tính folder. | EF | — |
| 6 | Upload nhiều file vào folder | (kéo-thả/chọn) | POST `/UploadFileInFolder` | — | internal | `[DisableRequestSizeLimit]` | Validate extension từng file, ghi wwwroot/Files, insert `Files` (Type=2, IdFolderUnit), trả list. | EF Insert Files | Lưu nguyên file (không resize ảnh như FilesController) |
| 7 | Xóa 1 file (vào thùng rác) | (nút Xóa file) | POST `/RemoveFileById` | — | internal | — | Chuyển file vào thùng rác. | EF | — |
| 8 | Chi tiết folder | (mở chi tiết) | POST `/GetFolderDetailById` | — | internal | — | Trả thông tin folder. | EF | — |
| 9 | Khôi phục folder từ thùng rác | (nút Khôi phục) | POST `/RecoverFolderInTrash` | — | internal | folder đang ở thùng rác | Đưa folder ra khỏi thùng rác. | EF | — |
| 10 | Khôi phục file từ thùng rác | (nút Khôi phục) | POST `/RecoverFileInTrash` | — | internal | file đang ở thùng rác | Đưa file ra khỏi thùng rác. | EF | — |
| 11 | Xóa vĩnh viễn folder | (nút Xóa vĩnh viễn) | POST `/RemoveFolderInTrash` | — | internal | folder trong thùng rác | Xóa hẳn folder. | EF | — |
| 12 | Xóa vĩnh viễn file | (nút Xóa vĩnh viễn) | POST `/RemoveFileInTrash` | — | internal | file trong thùng rác | Xóa hẳn file. | EF | — |
| 13 | Danh sách thùng rác | (tab Thùng rác) | POST `/GetFolderFileInTrash` | — | internal | — | Liệt kê folder+file đã xóa. | EF | — |

## Quan hệ
- **Gọi sang:** `IFoldersService` (toàn bộ nghiệp vụ folder/trash), `IFilesService.InsertAsync` (lúc upload).
- **Dữ liệu (entities):** `Folders`, `Files` (IdFolderUnit liên kết file ↔ folder).
- **Ripple / đồng bộ:** ghi file vật lý wwwroot/Files; state thùng rác quản ở service (soft-delete).

## Lỗi đã biết / lưu ý kỹ thuật
- `FolderController.cs:48,...` — mọi action chỉ `[SBAuthorize()]` không key → không phân quyền chi tiết (đọc/ghi/xóa cùng mức), scope dựa hoàn toàn vào `CurrentUser` ở service.
- `FolderController.cs:118-197` `UploadFileInFolder` lưu file gốc không resize và **không tạo biến thể ảnh** (khác FilesController) → ảnh trong folder hiển thị bằng file gốc.
- `ImageExtensions` ở controller này hẹp hơn FilesController (thiếu .XLSM/.TXT/.CSV) → lệch whitelist giữa 2 controller cùng service.
