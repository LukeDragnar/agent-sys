---
controller: UpdateListDomainCorsController
module: cua-hang
service: Branch
man_hinh_ui: "Quản trị hệ thống (HT-AD): nút làm mới danh sách domain CORS được phép. Route api/QuanTriArea/[controller]. Tiện ích hệ thống (MaTienIchSCRM.TI_HeThong)."
muc_dich: "1 endpoint hạ tầng: reset cache danh sách domain CORS (GlobalConfig.ListDomainCors=null) rồi nạp lại từ DB qua CoreConnectStr; chỉ HT-AD dùng."
alias: ["cors", "domain cors", "update cors", "danh sách domain", "cấu hình cors"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [GlobalConfig.ListDomainCors (static cache), CorsHelper.IsOriginAllowed, ConnectionString CoreConnectStr]
cross_module_in: []
cross_module_out: []
---

# UpdateListDomainCorsController (Branch) — Làm mới danh sách domain CORS

> Controller hạ tầng phía service **Branch** (đừng nhầm với `UpdateListDomainCorsController.md` cua-hang của service khác). Class-level `[SBAuthorize]` + endpoint key `DomainCors_UpdateCors` (HT-AD). Route đặc biệt `api/QuanTriArea/[controller]` (Area QuanTri). Tiện ích đánh dấu hệ thống ẩn (`TienIch(..., MaTienIchSCRM.TI_HeThong, true)`). `partial class` (có thể mở rộng thêm action ở file partial khác).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Làm mới danh sách domain CORS | (nút Update CORS) | `POST api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` (HT-AD) | public | màn quản trị HT | Set `GlobalConfig.ListDomainCors = null` (xóa cache static) rồi gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", CoreConnectStr)` để nạp lại list từ DB. Trả `true`. | static cache + DB CoreConnectStr | `UpdateListDomainCorsController.cs:24` |

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed` (đọc DB Core qua `CoreConnectStr`) để re-populate `GlobalConfig.ListDomainCors`.
- **Dữ liệu (entities):** không EF entity; thao tác trên static cache `GlobalConfig.ListDomainCors`.
- **Ripple / đồng bộ:** sau khi gọi, middleware CORS dùng list mới (ảnh hưởng toàn bộ origin được phép gọi API).

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs:28` — dùng origin hardcode `"http://localhost:4200"` chỉ để trigger nạp lại cache (giá trị `rs` không dùng); ý đồ là "warm" lại `ListDomainCors`, không phải kiểm tra origin thật.
- Chỉ làm mới cache trong **process hiện tại**: nếu chạy nhiều instance/pod, cần gọi trên từng instance (cache static per-process).
- `partial class` — kiểm tra file partial khác nếu thấy thiếu action (hiện chỉ 1 file, 1 action).
