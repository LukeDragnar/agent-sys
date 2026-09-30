---
controller: CauHinhController
module: bao-cao
service: Report
man_hinh_ui: "nội bộ — SandboxArea, bản cũ (không có UI thật)"
muc_dich: "Bản DEPRECATED trùng lặp của CauHinh_Report trong Areas/SandBoxArea (route api/SandboxArea/CauHinh, TienIch TI_Chung,true). Cùng dùng ICauHinhService, là phiên bản cũ của CauHinhController chính (CauHinhController_Report) — không nên dùng."
alias: ["cấu hình report sandbox", "cau hinh report cu", "sandbox cauhinh"]
auth: mixed
state_machine: []
deprecated: true
entities: [CauHinh]
stored_procs: []
depends: [CauHinhService (EF reposCauHinh)]
cross_module_in: []
cross_module_out: []
---

# CauHinhController (SandboxArea) — Cấu hình Report bản cũ (DEPRECATED)

> ⚠️ **DEPRECATED.** Bản trùng lặp cũ của `CauHinhController_Report` đặt trong `Areas/SandBoxArea` (route `api/SandboxArea/CauHinh`, `[TienIchAttribute("CauHinh_Report_Controller", ..., MaTienIchSCRM.TI_Chung, true)]` — cờ `true` = ẩn/khóa tiện ích). Có thêm `[SBAuthorize()]` controller-level. Các endpoint gần như trùng bản chính (GetCauHinhInit, Get, SaveCauHinhInit, Save_SuperAdmin, GetCauHinhSuperAdmin, GetCauHinhReport, GetAllCauHinhSuperAdmin) nhưng **thiếu** toggle query log (UpdateStatus/GetStatusShowQueryReport) và dùng key/role khác (vd `SaveCauHinhInit` key `CauHinh_Report_LuuCauHinhReport` role HT-CSKH). Dùng chung `ICauHinhService` → cùng EF entity `CauHinh`, không stored proc. **Ưu tiên dùng bản chính `CauHinhController_Report`.**

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (cũ) Lưu cấu hình init | — | `POST api/SandboxArea/CauHinh/SaveCauHinhInit` | `CauHinh_Report_LuuCauHinhReport` (HT-CSKH) | public | bản cũ, khóa tiện ích | Trùng SaveCauHinhInit bản chính; upsert `CauHinh.JsonSetting`. | **EF** `CauHinh` | DEPRECATED — dùng bản chính |
| 2 | (cũ) Get / GetCauHinhInit / SuperAdmin | — | `GET .../Get`, `GET .../GetCauHinhInit`, `POST .../Save_SuperAdmin`, `POST .../GetCauHinhSuperAdmin` | tương ứng bản chính (role có thêm HT-CSKH) | public/HT-AD | bản cũ | Hành vi như bản chính; cùng `ICauHinhService`. | **EF** `CauHinh` | DEPRECATED |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GetCauHinhReport/{idDonVi}` + `GetAllCauHinhSuperAdmin` (`[PrivateSBAuthorize]`) — trùng bản chính.

## Quan hệ
- **Dữ liệu (entities):** EF `CauHinh` (qua `ICauHinhService`), giống bản chính.
- **Ripple / đồng bộ:** không có riêng — là bản trùng lặp.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.API/Areas/SandBoxArea/CauHinhController.cs`
  - L12 `MaTienIchSCRM.TI_Chung, true` → tiện ích đánh dấu ẩn/khóa (deprecated). Trùng node name với bản chính → tách file này, đặt `deprecated: true`.
  - Thiếu toggle `UpdateStatusShowQueryReport`/`GetStatusShowQueryReport` (chỉ có ở bản chính). Trùng `CauHinh_Report_Controller` cùng tiện ích với bản chính.
