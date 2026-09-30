# Dev — Mẫu & quản lý thông báo

> Service: **Notification** (BE .NET, NotificationDataContext = SQL) · FE module **notification** + **quantri** (Angular 15 + ng-zorro). Trace registry: `registry/he-thong/NotiTemplateController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Quản lý mẫu thông báo (cấp đơn vị) | `/notification/mau-thongbao` | `NotiTemplateComponent` |
| Cấu hình mẫu thông báo (admin) | `/notification/ad-mau-thongbao` | `AdminNotiTemplateComponent` |
| Cấu hình gửi thông báo | `/notification/noti-config` | `NotiConfigComponent` |
| Quản lý thông báo (đẩy) | `/notification/thongbao` · `/quantri/thongbao` | `ThongbaoComponent` / `AdminThongbaoComponent` |
| Quản lý thông báo hệ thống | `/quantri/thongbao-hethong` | `AdminThongbaoHethongComponent` |

- Admin mẫu: cột grid `ma`, `ten`, `mauThongBao`, `nhomThongBao`, `listRolesSend`, `guiSau`, `suDung`, `ngayCapNhat`; service `services/notification/admin/admin.notitemplate.service`.
- Mẫu cấp đơn vị: service `services/notification/notitemplate.service`.
- Thông báo đẩy: service `services/notification/thongbao.service`; bản hệ thống `services/sanboxsarea/thongbao-hethong.service`.

## Endpoint / Controller (trace registry)
`NotiTemplateController` (Notification service, route `api/NotiTemplate`, controller-level `[SBAuthorize]`):
| # | Hành động | Route | Quyền (key) |
|---|---|---|---|
| 1 | Danh sách mẫu | `POST .../TimTheoDieuKien` | `NotiTemplate_TimTheoDieuKien` (DV-AD-TONG) |
| 2 | Chi tiết theo Mã | `GET .../TimTheoMa?code=` | `NotiTemplate_TimTheoMa` (DV-AD-TONG) |
| 3 | Cập nhật mẫu | `POST .../CapNhatThongTin` | `NotiTemplate_CapNhatThongTin` (DV-AD-TONG) |
| 4 | Xóa mẫu (theo Code) | `POST .../Xoa/{code}` | `NotiTemplate_Xoa` (DV-AD-TONG) |
| 5 | DS mã thông báo | `GET .../DanhSachMaThongBao` | `NotiTemplate_DanhSachMaThongBao` (DV-AD-TONG) |
| 6 | DS nhóm thông báo | `GET .../DanhSachNhomThongBao` | — (chỉ login) |

> **Không có endpoint `ThemMoi`** trên controller cấp hệ thống — chỉ Update/Xóa. FE admin có nút "Thêm mới" (Action THEMMOI) → đường tạo thực tế đi qua bản khác (có 2 file controller: `Areas/SanboxArea/NotiTemplateController.cs` + `Controllers/NotiTemplateController.cs`) hoặc seed DB → **cần dev xác nhận**.

## Entities
- `NotiTemplate` — mẫu thông báo: định danh `Code` (không phải Id), tiêu đề/nội dung mẫu, nhóm, danh sách vai trò nhận, `GuiSau` (giây), `SuDung`. SQL (NotificationDataContext).
- `NotiGroupCode` — nhóm + mã thông báo.
- `NotificationConfig` — cấu hình gửi: `WebPatternCode`/`WebPatternCodeNext` trỏ tới Code mẫu.
- (Thông báo đẩy) entity thông báo/tin tức: tiêu đề, hình ảnh, url, `DaGui`, ngày gửi.

## Luồng code
1. Danh sách/chi tiết mẫu: `TimTheoDieuKien`, `GetByCodeAsync(code)` (null → `MESS_NotFound`).
2. Cập nhật: `CapNhatThongTin` → validate → `UpdateAsync`.
3. Xóa: `DeleteAsync(code)` theo Code.
4. Combo: `NotiGroupCodeService.DanhSachMaThongBaoAsync` / `DanhSachNhomThongBaoAsync`.
5. Ripple: mẫu là **nguồn nội dung tĩnh** — service nghiệp vụ dựng `NotificationInfo` từ mẫu (theo Code trong `NotificationConfig`) → gửi qua `NotificationsController.Create`.

## Bug-prone / lưu ý (path:line)
- **Xóa mẫu không kiểm tra ràng buộc** — `NotiTemplateController.cs` `Xoa/{code}`: xóa theo Code mà không check `NotificationConfig.WebPatternCode` còn trỏ tới → template "mồ côi" → thông báo rỗng nội dung. Cần guard trước khi xóa.
- **XML doc copy-paste sai** — `NotiTemplateController.cs:32-49`: mô tả method copy từ controller chi nhánh ("Tìm kiếm chi nhánh", "Xem chi tiết chi nhánh") → swagger hiển thị sai mô tả, dễ nhầm khi đọc.
- **Không có ThemMoi cấp hệ thống** — controller chỉ Update/Xóa; FE admin lại có nút Thêm mới → mismatch, đường tạo mẫu cần xác nhận (sanbox vs controller thường).
- **DanhSachNhomThongBao thiếu key quyền** — chỉ `[SBAuthorize]` controller-level (login), trong khi các action khác yêu cầu DV-AD-TONG → chênh mức kiểm soát.
- **Định danh bằng Code** — mọi thao tác mẫu theo Code, không Id; đừng nhầm key tra cứu.
- **2 controller trùng tên NotiTemplate** (Areas/SanboxArea vs Controllers) — đọc đúng file khi debug.

## File source (path:line)
- BE controller mẫu (cấp hệ thống): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Notification\Notification.API\Controllers\NotiTemplateController.cs` (XML doc sai `:32-49`).
- BE controller mẫu (sanbox/admin): `...\scrm\Notification\Notification.API\Areas\SanboxArea\NotiTemplateController.cs`.
- FE admin mẫu: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\notification\admin-noti-template\admin-noti-template.component.ts` (`onAddNew` THEMMOI `:148`, `onDelete` XoaTheoMa `:189`).
- FE mẫu cấp đơn vị: `...\notification\noti-template\noti-template.component.ts`.
- FE cấu hình gửi: `...\notification\noti-config\noti-config.component.ts`.
- FE thông báo đẩy: `...\quantri\admin-thongbao\thongbao.component.ts`, `...\quantri\admin-thongbao-hethong\admin-thongbao-hethong.component.ts`.
- FE routing: `...\scrm_frontend\src\app\pages\notification\notification-routing.module.ts` (`mau-thongbao` `:17`, `ad-mau-thongbao` `:24`, `noti-config` `:31`); `...\quantri\quantri-routing.module.ts:211` (`thongbao`), `:370` (`thongbao-hethong`).
- Cẩm nang nền notification: `projects/scrm/features/notification.md`.
