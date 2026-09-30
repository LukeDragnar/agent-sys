---
controller: NotiTemplateController
module: he-thong
service: Notification
man_hinh_ui: "Hệ thống > Mẫu thông báo (quản lý template nội dung noti theo mã): list + form sửa; nút Sửa / Xóa; combo Danh sách mã / Danh sách nhóm thông báo"
muc_dich: "Quản lý MẪU thông báo (NotiTemplate) định danh theo Code: nội dung/tiêu đề mẫu dùng để sinh thông báo cho từng sự kiện; cung cấp danh sách mã & nhóm mã thông báo cho màn cấu hình."
alias: ["NotiTemplate", "mẫu thông báo", "template thông báo", "mã thông báo"]
auth: public
state_machine: []
deprecated: false
entities: [NotiTemplate, NotiGroupCode]   # SQL (NotificationDataContext)
stored_procs: []
depends: ["INotiTemplateService / INotiGroupCodeService", "NotificationDataContext (SQL)"]
cross_module_in: ["FE Hệ thống/Mẫu thông báo gọi CRUD + load combo mã/nhóm", "NotificationConfig (WebPatternCode) tham chiếu Code của template"]
cross_module_out: ["Template là nguồn nội dung khi service nghiệp vụ build NotificationInfo gửi qua NotificationsController.Create"]
---

# NotiTemplateController — Quản lý mẫu / mã thông báo

CRUD mẫu thông báo (`NotiTemplate`, định danh bằng **Code** không phải Id), và trả danh sách mã/nhóm mã thông báo cho các màn cấu hình (NotificationConfig dùng `WebPatternCode` trỏ tới mẫu). Lưu **SQL**. Controller-level `[SBAuthorize]` (cần login); hầu hết action yêu cầu role **DV-AD-TONG**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách mẫu | Grid / Lọc | POST `api/NotiTemplate/TimTheoDieuKien` | `NotiTemplate_TimTheoDieuKien` (DV-AD-TONG) | public | — | Lọc mẫu theo điều kiện (filter dto). | EF `NotiTemplate` | XML doc copy-paste ghi "Tìm kiếm chi nhánh" (sai mô tả) |
| 2 | Chi tiết theo mã | Mở mẫu | GET `api/NotiTemplate/TimTheoMa?code=` | `NotiTemplate_TimTheoMa` (DV-AD-TONG) | public | — | `GetByCodeAsync(code)`; null → `MESS_NotFound`; có → map sang NotiTemplateDto. | EF | Tra theo **Code** (không phải Id) |
| 3 | Cập nhật mẫu | Lưu | POST `api/NotiTemplate/CapNhatThongTin` | `NotiTemplate_CapNhatThongTin` (DV-AD-TONG) | public | ModelState valid | Validate model → `UpdateAsync`. | EF | Chỉ có Update, **không có ThemMoi** trên controller (tạo mẫu ở nơi khác / seed) |
| 4 | Xóa mẫu | Xóa | POST `api/NotiTemplate/Xoa/{code}` | `NotiTemplate_Xoa` (DV-AD-TONG) | public | — | `DeleteAsync(code)` theo Code. | EF | Xóa theo Code → nếu NotificationConfig còn trỏ WebPatternCode tới mã này sẽ "mồ côi" |
| 5 | Danh sách mã thông báo | Combo (màn cấu hình) | GET `api/NotiTemplate/DanhSachMaThongBao` | `NotiTemplate_DanhSachMaThongBao` (DV-AD-TONG) | public | — | `NotiGroupCodeService.DanhSachMaThongBaoAsync()` — list mã để cấu hình chọn. | EF `NotiGroupCode` | — |
| 6 | Danh sách nhóm thông báo | Combo nhóm | GET `api/NotiTemplate/DanhSachNhomThongBao` | — | public (login) | — | `DanhSachNhomThongBaoAsync()` — list nhóm mẫu. | EF | Không yêu cầu role cụ thể (chỉ login) khác các action khác |

## Quan hệ
- **Gọi sang:** INotiTemplateService (CRUD mẫu), INotiGroupCodeService (mã/nhóm), NotificationDataContext (SQL).
- **Dữ liệu (entities):** `NotiTemplate` (Code, tiêu đề/nội dung mẫu), `NotiGroupCode` (nhóm + mã thông báo).
- **Ripple / đồng bộ:** mẫu là **nguồn nội dung tĩnh** — `NotificationConfig.WebPatternCode/WebPatternCodeNext` tham chiếu Code; service nghiệp vụ dựng `NotificationInfo` từ mẫu rồi gửi qua `NotificationsController.Create`.

## Lỗi đã biết / lưu ý kỹ thuật
- `NotiTemplateController.cs:32-49` — XML doc các method copy từ controller chi nhánh ("Tìm kiếm chi nhánh", "Xem chi tiết chi nhánh") → mô tả sai, dễ gây nhầm khi đọc swagger.
- Controller **không có endpoint ThemMoi** — chỉ Update/Xóa; mẫu mới phải seed DB hoặc tạo qua kênh khác (cần xác nhận với team).
- `Xoa/{code}` xóa theo Code không kiểm tra ràng buộc với `NotificationConfig.WebPatternCode` → cấu hình có thể trỏ tới mẫu đã bị xóa (template mồ côi → noti rỗng nội dung).
- `DanhSachNhomThongBao` thiếu key quyền (chỉ `[SBAuthorize]` controller-level) trong khi các action cùng controller yêu cầu DV-AD-TONG → chênh mức kiểm soát.
