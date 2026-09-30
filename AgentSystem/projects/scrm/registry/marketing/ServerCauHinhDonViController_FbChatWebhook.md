---
controller: ServerCauHinhDonViController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/ServerCauHinhDonVi."
muc_dich: "Quản lý cấu hình 'đơn vị ↔ server webhook' cho service chat (đơn vị bắn vào queue Rabbit nào): AddOrUpdateDonVi, SaveAsync, GetAsync, TimTheoDieuKien. Toàn bộ anonymous."
alias: ["ServerCauHinhDonVi chat", "cấu hình đơn vị server webhook chat", "AddOrUpdateDonVi chat"]
auth: anonymous
state_machine: []
deprecated: false
entities: [ServerDonViCauHinh]
stored_procs: []
depends:
  - "IServerDonViCauHinhService.AddOrUpdateDonvi / SaveAsync / GetAsync / TimTheoDieuKienAsync"
  - "ICommonService, IConfiguration (inject, không dùng)"
cross_module_in: []
cross_module_out: []
---

# ServerCauHinhDonViController — Cấu hình đơn vị ↔ server webhook (chat)

> Route `api/ServerCauHinhDonVi`. Mọi endpoint `[AllowAnonymous]`. Mã giống bản FacebookWebhook.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm/sửa đơn vị | (nội bộ) | POST `api/ServerCauHinhDonVi/AddOrUpdateDonVi` | — | anonymous | — | Upsert cấu hình đơn vị (server/RabbitServerNumber). | EF (ServerDonViCauHinh) | Anonymous ghi (`cs:38-40`). |
| 2 | Lưu cấu hình | (nội bộ) | POST `api/ServerCauHinhDonVi/SaveAsync` | — | anonymous | — | Lưu `ServerDonViCauHinhAddDto`. | EF | Anonymous ghi. |
| 3 | Lấy cấu hình theo đơn vị | (nội bộ) | POST `api/ServerCauHinhDonVi/GetAsync` | — | anonymous | — | Trả cấu hình theo `ServerDonViCauHinhGet`. | EF | — |
| 4 | Tìm theo điều kiện | (grid) | POST `.../TimTheoDieuKien` · `.../TimTheoDieuKienAsync` | — | anonymous | — | Filter theo `ServerCauHinhDonViFilterDto`. | EF | Anonymous đọc toàn bộ. |

## Quan hệ
- **Gọi sang:** `IServerDonViCauHinhService`.
- **Dữ liệu (entities):** `ServerDonViCauHinh` — map đơn vị/chi nhánh → ServerCode + RabbitServerNumber.
- **Ripple:** `FacebookWebhooksController` đọc `GetAsync` (nhánh route-by-config, hiện tắt).

## Lỗi đã biết / lưu ý kỹ thuật
- `ServerCauHinhDonViController.cs:38-91` — **toàn bộ `[AllowAnonymous]`** kể cả ghi → bất kỳ ai đổi định tuyến queue đơn vị. Lỗ hổng; nên siết HT-AD.
- `_commonService`, `_configuration` inject không dùng.
- Trùng tên với bản FbWebhook → node suffix `_FbChatWebhook`.
