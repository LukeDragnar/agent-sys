# Dev — Cấu hình thông báo

> Service: **Notification** (BE .NET, NotificationDataContext - SQL) · FE module **NotificationModule** (Angular 15 + ng-zorro). Gồm 2 màn: Cấu hình gửi (NotificationConfig) + Mẫu thông báo (NotiTemplate). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Cấu hình gửi thông báo web | `/noti/noti-config` | `NotiConfigComponent` |
| Mẫu thông báo | `/noti/mau-thongbao` | `NotiTemplateComponent` (sửa: `noti-template-edit/`) |
| Mẫu thông báo (admin/SA) | `/noti/ad-mau-thongbao` | `AdminNotiTemplateComponent` |
| Quản lý thông báo (hộp thông báo) | `/noti/thongbao` | `ThongbaoComponent` |

- Route gốc module: `app-routing.module.ts:163` (`path: 'noti'` → lazy `NotificationModule`).
- Route con: `notification-routing.module.ts:30` (noti-config), `:16` (mau-thongbao), `:23` (ad-mau-thongbao).

## Endpoint / Controller (trace registry)

**Cấu hình gửi** — `NotificationConfigController` (registry: `he-thong/NotificationConfigController.md`), lưu **SQL** `NotificationConfig`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tải cấu hình theo đơn vị + kiểu | POST `api/NotificationConfig/TimTheoDonVi` | `NotiConfig_TimTheoDonVi` (HT-AD, DV-AD-TONG, HT-CSKH) |
| Lưu cấu hình Tác nghiệp | POST `.../SaveTacNghiep` | `NotiConfig_SaveTacNghiep` (HT-AD, DV-AD-TONG, HT-CSKH) |
| Lưu cấu hình Trạng thái đơn hàng | POST `.../SaveTrangThaiDonHang` | `NotiConfig_SaveTrangThaiDonHang` (HT-AD, DV-AD-TONG, HT-CSKH) |
| Lưu cấu hình Trạng thái lịch hẹn | POST `.../SaveTrangThaiLichHen` | ⚠️ key bị comment → chỉ cần login |
| Khởi tạo lại (xóa theo loại) | GET `.../DeleteByIdUnit/{loaiCauHinhThongBao}` | `NotiConfig_DeleteByIdUnit` (HT-AD, DV-AD-TONG, HT-CSKH) |
| (private) GetDataUsedByTacNghiep / GetDataUsedByTrangThaiGiaoHang / GetDataUsedByTrangThaiLichHen | service-to-service `[PrivateSBAuthorize]` | service nghiệp vụ đọc lúc runtime |

> Lưu ý FE: nút Lưu/Khởi tạo lại trong `noti-config.component.html:305,307` dùng key `NotificationConfig_Save` / `NotificationConfig_DeleteByIdUnit` — **lệch tên** so với key BE per-tab (`NotiConfig_Save*`, `NotiConfig_DeleteByIdUnit`). Cần đối chiếu mapping quyền FE↔BE.

**Mẫu thông báo** — `NotiTemplateController` (registry: `he-thong/NotiTemplateController.md`), lưu **SQL** `NotiTemplate` (định danh bằng **Code**, không phải Id):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Danh sách mẫu | POST `api/NotiTemplate/TimTheoDieuKien` | `NotiTemplate_TimTheoDieuKien` (DV-AD-TONG) |
| Chi tiết theo mã | GET `api/NotiTemplate/TimTheoMa?code=` | `NotiTemplate_TimTheoMa` (DV-AD-TONG) |
| Cập nhật mẫu | POST `api/NotiTemplate/CapNhatThongTin` | `NotiTemplate_CapNhatThongTin` (DV-AD-TONG) |
| Xóa mẫu | POST `api/NotiTemplate/Xoa/{code}` | `NotiTemplate_Xoa` (DV-AD-TONG) |
| Combo mã / nhóm thông báo | GET `.../DanhSachMaThongBao`, `.../DanhSachNhomThongBao` | `NotiTemplate_DanhSachMaThongBao` (DV-AD-TONG) / nhóm: chỉ login |

> **Không có endpoint ThemMoi** trên NotiTemplateController — chỉ Update/Xóa. Mẫu mới phải seed DB / kênh khác (cần xác nhận team). FE cũng không có nút Thêm mới (`noti-template.component.html` chỉ có Sửa/Xóa/Export).

## Entities
- **`NotificationConfig`** (SQL, NotificationDataContext) — 1 dòng = 1 quy tắc: `LoaiCauHinhThongBao` (enum `EnumLoaiCauHinhThongBao`: HeThong / TacNghiep / TrangThaiDonHang(GiaoHang) / TrangThaiLichHen) + khóa sự kiện (`IdTacNghiepCan/IdKetQua/IdTacNghiepTiepTheo` hoặc `GiaoHangTrangThaiMa` hoặc `TrangThaiLichHen`) + mẫu (`WebPatternCode`/`WebPatternCodeNext`) + thời điểm (`WebSendLaterMinute`/`WebSendBeforMinute`) + tập cờ đối tượng nhận (`IsSendToSale/IsSendToTruongPhongSale/IsSendToTruongNhomSale/IsSendToMkt/IsSendToTruongPhongMkt/IsSendToTruongNhomMkt/IsSendToThuNgan/IsSendToAdminChiNhanh/IsSendToAdminTong`) + `SendToListIdUser`.
- **`NotiTemplate`** (SQL) — mẫu nội dung định danh bằng `Code`. **`NotiGroupCode`** — nhóm + mã thông báo (cấp combo cho màn cấu hình).

## Luồng code
- `NotificationConfigController.TimTheoDonVi`: đọc `NotificationConfig` (nolock) theo đơn vị + loại; với Tác nghiệp gọi `IClientHelper` sang service tác nghiệp để merge danh sách sự kiện đầy đủ.
- `Save*`: upsert list cấu hình; `NotificationConfigService.UpdateAsync` MarkDirty ~30 field thủ công.
- Lúc runtime, service CSKH/Order/Booking gọi `GetDataUsedBy*` (private) → nhận mẫu + đối tượng nhận → dựng `NotificationInfo` → gửi qua `NotificationsController.Create`.
- `NotiTemplate`: CRUD theo Code; `WebPatternCode` của NotificationConfig trỏ tới Code của template.

## Bug-prone / lưu ý (path:line) — chỉ GHI, không sửa
- `NotificationConfigController.cs:59-66` `SaveTrangThaiLichHen` — key quyền bị comment, `[SBAuthorize]` rỗng → mọi user login lưu được tab Lịch hẹn (lỏng hơn 3 tab Save kia cần HT-AD/DV-AD-TONG/HT-CSKH).
- `NotificationConfigController.cs:96-103` `TimTheoDonViCustom` `[AllowAnonymous]` — không lọc theo đơn vị/token (endpoint test còn sót).
- `NotificationConfigController.cs:67-73` `DeleteByIdUnit` dùng **HTTP GET** cho xóa hàng loạt theo loại → không idempotent-safe, dễ trigger ngoài ý muốn.
- `NotificationConfigService.cs:60-105` `UpdateAsync` MarkDirty thủ công từng field → thêm field mới mà quên MarkDirty thì update bỏ sót field đó (lỗi âm thầm).
- `NotiTemplateController.cs:32-49` — XML doc copy từ controller chi nhánh ("Tìm kiếm chi nhánh"…) → mô tả swagger sai.
- `NotiTemplateController` **không có ThemMoi** — mẫu mới phải seed/kênh khác (cần xác nhận team).
- `NotiTemplate.Xoa/{code}` không kiểm tra ràng buộc với `NotificationConfig.WebPatternCode` → xóa mẫu để lại cấu hình "mồ côi" (noti rỗng nội dung).
- FE key quyền nút Lưu/Xóa (`NotificationConfig_Save`/`NotificationConfig_DeleteByIdUnit`) lệch với key BE per-tab → kiểm tra mapping kẻo nút hiển thị/ẩn sai.

## File source (path:line)
- BE controller cấu hình: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Notification\Notification.API\Controllers\NotificationConfigController.cs` (lưu ý: còn 1 bản trùng tên ở `SMS\SMS.API\Controllers\NotificationConfigController.cs` — service Notification là bản dùng cho màn này).
- BE controller mẫu: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Notification\Notification.API\Controllers\NotiTemplateController.cs` (bản SA: `...\Areas\SanboxArea\NotiTemplateController.cs`).
- FE cấu hình gửi: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\notification\noti-config\noti-config.component.html` / `.ts`.
- FE mẫu: `...\pages\notification\noti-template\noti-template.component.html` / `.ts` (sửa: `noti-template-edit/`).
- FE routing: `...\pages\notification\notification-routing.module.ts`.
- Registry node: `projects/scrm/registry/he-thong/NotificationConfigController.md`, `projects/scrm/registry/he-thong/NotiTemplateController.md`.
