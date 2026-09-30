---
controller: UpdateListDomainCorsController
module: marketing
service: MarketingCampaign
man_hinh_ui: "nội bộ — không có UI (tool vận hành reload list domain CORS)"
muc_dich: "Reload lại danh sách domain CORS in-RAM của service MarketingCampaign (xóa cache GlobalConfig.ListDomainCors, đọc lại từ CoreConnectStr)."
alias: ["update cors marketing", "reload domain cors", "cập nhật danh sách cors"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [GlobalConfig, CorsHelper, CoreConnectStr]
cross_module_in: []
cross_module_out: []
---

# UpdateListDomainCorsController (MarketingCampaign) — Reload domain CORS

> SandBoxArea / route `api/QuanTriArea/UpdateListDomainCorsController`. TienIch = TI_HeThong. Controller `partial`. Tool vận hành, không UI nghiệp vụ.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Reload list domain CORS | (tool/postman) | POST `api/QuanTriArea/UpdateListDomainCorsController/UpdateCors` | `DomainCors_UpdateCors` (HT-AD) | public | - | Set `GlobalConfig.ListDomainCors = null` (xóa cache) → gọi `CorsHelper.IsOriginAllowed(...)` với CoreConnectStr để nạp lại danh sách; trả true | - | Trả về `true` cứng, không phản ánh kết quả nạp lại thực tế |

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed`, đọc connection `CoreConnectStr`.
- **Dữ liệu (entities):** không (chỉ cache RAM `GlobalConfig.ListDomainCors`).
- **Ripple / đồng bộ:** Ảnh hưởng CORS toàn service MarketingCampaign sau khi đổi danh sách domain ở DB Core.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller `partial` (UpdateListDomainCorsController.cs:12) — có thể còn endpoint ở file partial khác; cần xác nhận.
- Luôn trả `ResultWithData(true)` bất kể kết quả nạp lại — không có error path.
- Quyền HT-AD (admin hệ thống) — đúng tính chất tool vận hành.
