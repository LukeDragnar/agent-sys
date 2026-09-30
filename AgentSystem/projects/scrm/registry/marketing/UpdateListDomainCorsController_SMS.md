---
controller: UpdateListDomainCorsController
module: marketing
service: SMS
man_hinh_ui: "Quản trị hệ thống (Area QuanTri) > nút 'Cập nhật CORS' — admin reset cache danh sách domain CORS sau khi đổi cấu hình. Route api/QuanTriArea/UpdateListDomainCors."
muc_dich: "Reset cache danh sách domain CORS của service SMS: xóa `GlobalConfig.ListDomainCors` để lần sau load lại từ DB; dùng sau khi thêm/sửa domain được phép gọi API. (Trùng tên controller với bản service Promotion — node này là bản SMS.)"
alias: ["UpdateListDomainCors SMS", "cập nhật cors sms", "reset cors sms", "domain cors sms"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "IConfiguration (connection string CoreConnectStr)"
  - "GlobalConfig.ListDomainCors / CorsHelper.IsOriginAllowed (SB.Core)"
  - "MaTienIchSCRM.TI_HeThong — tiện ích gate (Area QuanTri, param4=true)"
cross_module_in: []
cross_module_out:
  - "Ảnh hưởng cache CORS toàn service SMS (GlobalConfig static)"
---

# UpdateListDomainCorsController — Reset cache domain CORS (SMS, Area QuanTri)

> Node này cho **service SMS** (file `Areas/SanboxArea/UpdateListDomainCorsController.cs`, namespace `SMS.API.Controllers`). Tên controller TRÙNG bản service Promotion (`UpdateListDomainCorsController.md`) → file đặt hậu tố `_SMS` để tránh va chạm.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật CORS | Cập nhật CORS | POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` (HT-AD, HT-KT) | public | — | Set `GlobalConfig.ListDomainCors = null` rồi gọi `CorsHelper.IsOriginAllowed` (warm lại từ DB qua CoreConnectStr); trả `true`. | Config DB | Luôn trả `true` cứng, không phản ánh kết quả thực (`cs:28`). |

## Quan hệ
- **Gọi sang:** `IConfiguration`, `CorsHelper` (SB.Core).
- **Dữ liệu:** đọc connection `CoreConnectStr`; ghi `GlobalConfig.ListDomainCors` (static cache).
- **Ripple / đồng bộ:** reset cache CORS toàn service SMS → các origin mới có hiệu lực mà không cần restart.

## Lỗi đã biết / lưu ý kỹ thuật
- Trả `ResultWithData(true)` cứng (`UpdateListDomainCorsController.cs:28`) — không báo lỗi nếu warm cache fail.
- `IsOriginAllowed("http://localhost:4200", ...)` truyền origin hardcode chỉ để trigger reload cache, không phải check thật (`cs:27`).
- Song sinh với bản Promotion cùng tên — sửa nên đồng bộ cả 2 service.
