---
controller: UpdateListDomainCorsController
module: sales
service: Switchboard
man_hinh_ui: "Quản trị hệ thống (QuanTriArea) > Cập nhật domain CORS (nút reset cache CORS)"
muc_dich: "Reset/làm mới danh sách domain CORS đang cache trong bộ nhớ (GlobalConfig.ListDomainCors=null) buộc đọc lại từ DB — service Switchboard. (Trùng tên với bản Contact → node này hậu tố _Switchboard)."
alias: ["cập nhật cors tổng đài", "reset domain cors", "UpdateCors Switchboard"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [IConfiguration, "GlobalConfig.ListDomainCors", "CorsHelper"]
cross_module_in: []
cross_module_out: []
---

# UpdateListDomainCors (Switchboard) — Reset cache domain CORS

> ⚠️ Trùng tên với bản module Contact. Node này = bản Switchboard (`Areas\SandBoxArea\UpdateListDomainCorsController.cs`, route `api/QuanTriArea/UpdateListDomainCors`, `TI_HeThong`) → hậu tố `_Switchboard`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Update lại list domain CORS | Update CORS | POST `UpdateCors` | `DomainCors_UpdateCors` (HT-AD) | internal | màn quản trị hệ thống | Set `GlobalConfig.ListDomainCors = null`, gọi `CorsHelper.IsOriginAllowed` với connection `CoreConnectStr` → cache CORS đọc lại từ DB; trả `true`. | config/DB | — |

## Quan hệ
- **Gọi sang:** `IConfiguration` (lấy `CoreConnectStr`), `CorsHelper`, `GlobalConfig` (cache CORS in-memory).
- **Dữ liệu (entities):** không (chỉ reset cache CORS).
- **Ripple / đồng bộ:** ảnh hưởng cache CORS toàn ứng dụng (reset whitelist).

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs (Switchboard)` — chỉ HT-AD; thao tác reset cache CORS toàn process (`GlobalConfig.ListDomainCors=null`).
- Namespace là `Switchboard.API.Controllers` (không phải Areas) dù file nằm trong `Areas\SandBoxArea` — route vẫn `api/QuanTriArea/...` qua attribute.
