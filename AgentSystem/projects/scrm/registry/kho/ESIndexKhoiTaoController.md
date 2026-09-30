---
controller: ESIndexKhoiTaoController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI (endpoint admin/healthcheck gọi trực tiếp)"
muc_dich: "Endpoint nội bộ/admin của service OrderLogistic: healthcheck (Test) và xóa cache menu/permission trong Redis (RemoveCache). KHÔNG liên quan trực tiếp đến ElasticSearch index dù tên gọi 'ESIndexKhoiTao'."
alias: ["healthcheck OrderLogistic", "remove cache menu permission", "ESIndexKhoiTao", "clear cache redis"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "IRedisCacheService (RemoveByPrefix cache menu + permission)"
cross_module_in: []
cross_module_out: []
---

# ESIndexKhoiTaoController — Healthcheck + xóa cache (nội bộ)

> Kế thừa `ControllerBase` (KHÔNG phải BaseController), KHÔNG có `[SBAuthorize]` / `[TienIch]` → 2 endpoint **anonymous**, ai cũng gọi được. Tên controller gợi "khởi tạo ES index" nhưng code thực chỉ làm healthcheck + clear Redis cache.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Healthcheck service | (gọi trực tiếp / monitor) | GET `api/ESIndexKhoiTao/Test` | (không) | anonymous | - | Trả `"Service Ok"` | - | Anonymous |
| 2 | Xóa cache menu + permission | (admin gọi trực tiếp) | GET `api/ESIndexKhoiTao/RemoveCache` | (không) | anonymous | - | `RemoveByPrefix(Cache_MenuPrefix)` + `RemoveByPrefix(Cache_PermissionPrefix)` trong Redis → trả `"Clear Ok"` | Redis | Anonymous — ai cũng xóa được cache menu/permission. Cần xác nhận có chủ ý / chặn ở tầng network |

## Quan hệ
- **Gọi sang:** `IRedisCacheService` (Redis).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** xóa cache menu/permission → lần load sau rebuild cache từ DB.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller anonymous (không SBAuthorize/TienIch) — endpoint `RemoveCache` xóa cache hệ thống mà không xác thực; nên giới hạn ở tầng hạ tầng.
- Tên `ESIndexKhoiTao` không phản ánh chức năng thực (không có code khởi tạo ElasticSearch index).
