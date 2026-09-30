---
controller: CauHinhController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Cấu hình chung (cấu hình service WorkFlow theo đơn vị: dùng Elasticsearch hay không, độ sâu công việc con, cấu hình super-admin...)"
muc_dich: "Đọc/lưu cấu hình service WorkFlow theo đơn vị (CauHinh): IsUseElasticSearch (chọn ES/SP), DoSauCongViecCon (giới hạn cấp con), JsonSetting super-admin; cache Redis."
alias: [CauHinh, "cấu hình WorkFlow", "cấu hình công việc", config, settings, "cấu hình service"]
auth: internal
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [ICauHinhService, IRedisCacheService]
cross_module_in: [CongViecController/CongViecService (ResolveUseEs + DoSauCongViecCon)]
cross_module_out: []
---

# CauHinh — Cấu hình service WorkFlow (per đơn vị)

> Lưu/đọc cấu hình đơn vị, cache Redis (key `CK_CauHinhWorkFlowSetting_`). Cấu hình lái 2 hành vi lõi: `IsUseElasticSearch` (CongViec tìm bằng ES hay SP) và `DoSauCongViecCon` (giới hạn cấp công việc con, mặc định 3). `SaveCauHinhInit` validate ModelState trước khi lưu.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình init | Nút "Lưu cấu hình" | `POST api/CauHinh/SaveCauHinhInit` | `[SBAuthorize]` | internal | màn cấu hình | ModelState invalid → lỗi MESS_Model_UnValidate. Lưu cấu hình đơn vị + reset cache Redis. | EF + Redis | Quyền gốc `CauHinh_WorkFlow_LuuCauHinhInit` (DV-AD-TONG) **đã comment** (L21) → hiện chỉ cần đăng nhập (`[SBAuthorize]`) → bất kỳ user lưu được cấu hình toàn đơn vị. |
| 2 | Lấy cấu hình init | Mở màn cấu hình | `GET api/CauHinh/GetCauHinhInit` | `[SBAuthorize]` | internal | màn cấu hình | Trả `CauHinhSettingInitDto` của đơn vị. | EF (cache) | — |
| 3 | Lấy cấu hình (runtime) | (nội bộ/khởi tạo) | `GET api/CauHinh/Get` | `[SBAuthorize]` | internal | — | Trả `CauHinhSettingDto` (gồm DoSauCongViecCon, super-admin settings). | EF (cache) | — |

## Quan hệ
- **Gọi sang:** `ICauHinhService` + `IRedisCacheService` (cache).
- **Dữ liệu (entities):** `CauHinh` (1 bản/đơn vị).
- **Ripple/đồng bộ:** CongViecService đọc cấu hình mỗi lần tìm (ES/SP) + mỗi lần tạo CV con (độ sâu) → đổi cấu hình ảnh hưởng toàn module; cache invalidate khi Save.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs:21-22` — `[SBAuthorize("CauHinh_WorkFlow_LuuCauHinhInit", ..., "DV-AD-TONG")]` bị comment, thay bằng `[SBAuthorize]` trống → mất giới hạn role cho thao tác lưu cấu hình.
- `IsUseElasticSearch` null → fallback global `Search:CongViec:UseES` (default true) — đổi cấu hình này đổi đường tìm kiếm CongViec toàn đơn vị.
