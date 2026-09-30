# Luồng — Quản lý trang & giao diện

## Tiền điều kiện
- Người thao tác là **admin hệ thống (HT-AD)** hoặc **quản trị mobile (HT-MB)**.
- App mobile đọc key chỉ cần **đăng nhập**.
- Role đã được khai báo (Page/PageMobile gán role để quyết hiển thị).

## Luồng dùng (admin — trang web)
1. Mở **Quản trị → Quản lý trang**.
2. `Page/LayTatCaTheoTree?type=` → cây trang theo loại.
3. **Xem chi tiết** `Page/TimTheoId/{id}` → kèm `ListRolesHomePage` (role được vào home page).
4. **Thêm** `Page/ThemMoi` → `InsertOrUpdateAsync(model)` (insert).
5. **Sửa** `Page/CapNhatThongTin` → `InsertOrUpdateAsync(model, true)` (update).
6. **Xóa** `Page/Xoa/{id}` → kiểm tra tồn tại → `DeleteAsync` → **xóa cache menu** (`RemoveWithPattern Cache_MenuPrefix`) → menu web dựng lại.

## Luồng dùng (admin — page mobile)
1. Mở **Quản trị → Quản lý Page Mobile** (route `api/QuanTriArea/PageMobile`).
2. `PageMobile/TimTheoDieuKien` (theo `PageMFilterDto`) → lưới; `TimTheoId/{id}` → kèm Roles.
3. **Thêm** `PageMobile/ThemMoi` → `InsertAsync` → nếu OK `UpdateRolesByPageAsync`.
4. **Sửa** `PageMobile/CapNhatThongTin` → kiểm tra tồn tại → `UpdateAsync` → cập nhật roles.
5. **Xóa** `PageMobile/Xoa/{id}` → kiểm tra tồn tại → `DeleteAsync`.
6. **Thêm key mobile** `PageMobile/ThemMoiKeyMobile` (bản `_Encrypt` giải mã AES Key/ParentKey/DisplayName trước khi lưu).
7. **Backup** `DownloadPageMobileBackup` → `PageMobile_Backup.xlsx`; **File mẫu** `DownloadFileTemplate`; **Import** `ImportPageMobile` (IFormFile: Url, DisplayName, RolesCode, IsNotImport).

## Luồng app mobile (đọc — chỉ cần login)
1. App gọi `api/PageMobile/GetAllKeyMobile` (+ `_Encrypt`) → toàn bộ key.
2. Hoặc `api/PageMobile/GetKeyByUserMobile` (+ `_Encrypt`) → key user được phép (theo role), trả về mã hóa AES-ECB-128.

## Case biên
- **Xóa trang web** → invalidate cache menu → mọi user nạp menu mới (ảnh hưởng diện rộng).
- **Sửa trang web** → KHÔNG xóa cache menu ở controller → nếu service không tự dọn, menu có thể **stale** (cần product/dev xác nhận).
- **Import page mobile file rỗng / không có dòng** → báo lỗi, không import.
- **Thêm key mobile bản `_Encrypt`** mà chuỗi không giải mã được → lưu sai/lỗi (chú ý đúng khóa AES client–server).
- **Tải file mẫu** ra file tên `PageMobile_Backup.xlsx` (trùng tên file backup) → dễ nhầm.
- **Bản chỉ-đọc `api/PageMobile`** không có CRUD → gọi Thêm/Sửa/Xóa ở route này sẽ không tồn tại endpoint; phải dùng route QuanTriArea.

## Ví dụ cụ thể có số
Hệ thống có cây trang web ~150 màn, app mobile ~80 màn (key).
- Admin thêm trang con "Báo cáo công nợ mới" dưới nhóm "Kế toán", gán cho role DV-KT, DV-AD-TONG → lưu → role DV-KT thấy màn này trong menu (đăng nhập lại).
- Admin xóa trang cũ "Báo cáo tạm" → cache menu bị dọn → toàn bộ user dựng lại menu, không còn thấy "Báo cáo tạm".
- HT-MB import 25 page mobile từ Excel (file 25 dòng hợp lệ) → 25 màn được thêm; app mobile đọc `GetKeyByUserMobile` → user có role tương ứng thấy 25 màn mới.
- HT-MB tải Backup → file `PageMobile_Backup.xlsx`; sau đó cần file mẫu lại bấm "Tải file mẫu" → cũng ra `PageMobile_Backup.xlsx` (lưu ý dễ nhầm).

## Hậu điều kiện
- Thêm/Sửa/Xóa trang web → cập nhật bảng `Page`; **Xóa** còn dọn cache menu.
- Thêm/Sửa/Xóa page mobile → cập nhật `PageMobile` + role-by-page → app đọc key mới.
- Import → thêm hàng loạt page mobile.
- Đọc key mobile → chỉ trả dữ liệu (read-only), quyết định màn app hiển thị theo quyền.
