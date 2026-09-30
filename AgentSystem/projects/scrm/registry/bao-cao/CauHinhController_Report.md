---
controller: CauHinhController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Cài đặt cấu hình Report: cấu hình hiển thị/tìm kiếm của Report-service cho đơn vị (init setting), cấu hình SuperAdmin (HT-AD: cho phép search báo cáo bằng SQL / trả kết quả ES từ SQL), và toggle bật/tắt log query report của user. Phần lớn là cấu hình nội bộ, không phải lưới dữ liệu."
muc_dich: "Quản lý cấu hình của Report-service: đọc/lưu CauHinh setting theo đơn vị (JsonSetting + JsonSetting_SuperAdmin), bật/tắt show query log report. Tất cả CRUD đi EF entity CauHinh + cache Redis; KHÔNG stored proc."
alias: ["cấu hình report", "cau hinh report", "cấu hình báo cáo", "config report", "cài đặt report", "show query report"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [CauHinhService (EF reposCauHinh + CommonCacheService Redis), CommonService (UpdateStatusShowQueryReport/GetStatusShowQueryReport qua Redis), CoreService /api/DonVi/GetAllIdDonVi (khi áp cấu hình SuperAdmin cho tất cả đơn vị)]
cross_module_in: [Core/khác gọi GetCauHinhReport (PrivateSBAuthorize) để lấy cấu hình report theo idDonVi]
cross_module_out: []
---

# CauHinhController — Cấu hình Report-service (bản chính, api/CauHinh)

> Controller cấu hình **chính** của Report-service (route `api/CauHinh`, `[TienIchAttribute("CauHinh_Report_Controller", ..., TI_Report_Chung)]`). Không gắn `[SBAuthorize]` ở controller-level (dòng đã comment) → auth set **theo từng method**: `[SBAuthorize]` mặc định (public, chỉ cần đăng nhập), `[SBAuthorize("Key","Mô tả","Role")]` cho thao tác SuperAdmin (HT-AD), `[PrivateSBAuthorize]` cho 2 endpoint nội bộ. **Toàn bộ đi EF entity `CauHinh`** (đọc/ghi `JsonSetting` + `JsonSetting_SuperAdmin`), qua cache Redis `CauHinhSetting_DonVi`; KHÔNG có stored proc. Lưu ý: cấu hình đơn vị thường (init) lưu theo `userLogin.DonViId`; cấu hình SuperAdmin có thể áp cho 1 danh sách đơn vị hoặc tất cả đơn vị (gọi sang CoreService lấy `GetAllIdDonVi`).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình init (cho màn cài đặt) | (load form cấu hình) | `GET api/CauHinh/GetCauHinhInit` | (mặc định) | public | mở màn cấu hình | Lấy CauHinh theo DonViId, map sang `CauHinhSettingInitDto`. Nếu chưa khởi tạo → fallback bản ghi IdDonVi=Empty. | **EF** `CauHinh` (qua cache) | — |
| 2 | Lấy cấu hình report đầy đủ | (load setting) | `GET api/CauHinh/Get` | (mặc định) | public | nội bộ màn report | Trả `CauHinhSettingDto` của đơn vị (qua cache). | **EF** `CauHinh` (qua cache) | — |
| 3 | Lưu cấu hình init | Lưu cấu hình | `POST api/CauHinh/SaveCauHinhInit` | `CauHinh_Report_LuuCauHinhReport_2` | public | màn cài đặt (DV-AD/HT-AD/HT-CSKH) | Upsert `CauHinh.JsonSetting` theo DonViId; clear cache đơn vị. Trả Id. | **EF** `CauHinh` | — |
| 4 | Lấy cấu hình SuperAdmin | (màn SuperAdmin) | `POST api/CauHinh/GetCauHinhSuperAdmin` | `CauHinh_Report_GetCauHinhSuperAdmin` | public (HT-AD) | màn quản trị SuperAdmin | Trả `JsonSetting_SuperAdmin` theo IdDonVi (payload); rỗng → default (cờ search-by-SQL/ES = false). | **EF** `CauHinh` | — |
| 5 | Lưu cấu hình SuperAdmin | Lưu (SuperAdmin) | `POST api/CauHinh/Save_SuperAdmin` | `CauHinh_Report_LuuCauHinhReportSuperAdmin` | public (HT-AD) | màn quản trị SuperAdmin | Upsert `JsonSetting_SuperAdmin` cho list đơn vị hoặc tất cả (IsApplyForAll → gọi Core GetAllIdDonVi). Merge object cũ-mới (CombineObjects), transaction + clear cache từng đơn vị. | **EF** `CauHinh` (+ gọi Core `GetAllIdDonVi`) | — |
| 6 | Bật/tắt log query report | Toggle show query | `GET api/CauHinh/UpdateStatusShowQueryReport/{status}` | (mặc định) | public | chỉ Super/CSKH thực thi được | status=1 → set Redis `StatusShowQueryReport:{IdDonVi}` (TTL 60'); khác → xóa. Service chặn nếu không phải Super/CSKH. | **EF** (Redis, không entity) | — |
| 7 | Lấy trạng thái log query report | (đọc toggle) | `GET api/CauHinh/GetStatusShowQueryReport` | (mặc định) | public | màn cấu hình | Đọc cờ Redis `StatusShowQueryReport:{IdDonVi}`. | **EF** (Redis, không entity) | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GetCauHinhReport/{idDonVi}` (`[PrivateSBAuthorize]`) → trả `CauHinhSettingDto` của đơn vị theo idDonVi (cho service khác lấy cấu hình report, không cần token user).
- [internal] `GetAllCauHinhSuperAdmin` (`[PrivateSBAuthorize]`) → trả toàn bộ `JsonSetting_SuperAdmin` của các đơn vị (entity `CauHinh`); phục vụ đồng bộ/đọc cấu hình SuperAdmin hàng loạt.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Phạm vi đơn vị:** mọi đọc/ghi init đi theo `userLogin.DonViId`. Bản ghi `IdDonVi=Guid.Empty` là cấu hình mặc định fallback khi đơn vị chưa khởi tạo (`GetAsync` đệ quy lấy Empty).
- **SuperAdmin apply-all:** `IsApplyForAll==true` → lấy tất cả IdDonVi từ Core (`/api/DonVi/GetAllIdDonVi`), không lấy được → lỗi; ngược lại chỉ áp cho `ListIdDonVi` (rỗng → "IdDonVi không được để trống").
- **Gate quyền toggle query:** `UpdateStatusShowQueryReport` chỉ cho `IsSuperUser || IsCSKH`; user khác → "Bạn không có quyền cập nhật trạng thái này!".

## Quan hệ
- **Gọi sang:** CoreService `/api/DonVi/GetAllIdDonVi` (khi apply-all SuperAdmin); CommonService cho toggle query log (Redis).
- **Dữ liệu (entities):** EF entity `CauHinh` (cột `JsonSetting`, `JsonSetting_SuperAdmin`) trên `ReportDataContext`; cache Redis `CauHinhSetting_DonVi`.
- **Ripple / đồng bộ:** clear cache `CauHinhSetting_DonVi` mỗi lần lưu; không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/CauHinhService.cs`
  - **L140-144** `Save_SuperAdminAsync`: `try/catch` build list nuốt exception, chỉ gán biến cục bộ `var a = index; var b = 5;` (debug leftover) — lỗi build config bị nuốt âm thầm, không log/return.
  - L221-224, L439-443: nhiều block "superuser/HT-AD → IdDonVi=Empty" đã bị comment → hiện superuser cũng lưu/đọc theo DonViId thật của mình (đổi behavior so với doc-comment đầu file).
- BE `Report/Report.API/Controllers/CauHinhController.cs`
  - L15 `[SBAuthorize("CauHinhController")]` controller-level đã comment → controller KHÔNG có auth chung, dựa hoàn toàn vào attribute từng method.
  - L30-40, L70-96: 2 block method (`SaveCauHinhInit` bản cũ DV-AD-TONG, `LuuCauHinhDonHangSale`) đã comment toàn bộ.
- Trùng tên: có bản SandboxArea cũ `CauHinhController_SandboxArea_Report.md` (`deprecated: true`) — node này là bản CHÍNH đang dùng.
