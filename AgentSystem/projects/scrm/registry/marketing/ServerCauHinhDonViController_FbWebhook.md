---
controller: ServerCauHinhDonViController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/ServerCauHinhDonVi."
muc_dich: "Quản lý cấu hình 'đơn vị ↔ server webhook' (đơn vị nào bắn vào server/queue Rabbit nào): AddOrUpdateDonVi, SaveAsync, GetAsync, TimTheoDieuKien. Toàn bộ anonymous."
alias: ["ServerCauHinhDonVi", "cấu hình đơn vị server webhook", "đơn vị server rabbit", "AddOrUpdateDonVi"]
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

# ServerCauHinhDonViController — Cấu hình đơn vị ↔ server webhook

> Route `api/ServerCauHinhDonVi`. Mọi endpoint `[AllowAnonymous]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm/sửa đơn vị | (nội bộ) | POST `api/ServerCauHinhDonVi/AddOrUpdateDonVi` | — | anonymous | — | Upsert cấu hình đơn vị (server/RabbitServerNumber) theo `ServerCauHinhAddOrUpdateDto`. | EF (ServerDonViCauHinh) | **Anonymous ghi** cấu hình routing đơn vị (`cs:38-40`). |
| 2 | Lưu cấu hình | (nội bộ) | POST `api/ServerCauHinhDonVi/SaveAsync` | — | anonymous | — | Lưu `ServerDonViCauHinhAddDto`. | EF | Anonymous ghi. |
| 3 | Lấy cấu hình theo đơn vị | (nội bộ) | POST `api/ServerCauHinhDonVi/GetAsync` | — | anonymous | — | Trả cấu hình theo `ServerDonViCauHinhGet` (ServerCode/IdDonVi/IdChiNhanh). | EF | — |
| 4 | Tìm theo điều kiện | (grid) | POST `.../TimTheoDieuKien` · `.../TimTheoDieuKienAsync` | — | anonymous | — | Filter theo `ServerCauHinhDonViFilterDto`. | EF | Anonymous đọc toàn bộ cấu hình. |

## Quan hệ
- **Gọi sang:** `IServerDonViCauHinhService`.
- **Dữ liệu (entities):** `ServerDonViCauHinh` — map đơn vị/chi nhánh → ServerCode + `RabbitServerNumber` (quyết định webhook của đơn vị bắn vào queue nào).
- **Ripple:** `FacebookWebhooksController` đọc cấu hình này (`GetAsync`) trong nhánh route-by-config (hiện tắt).

## Lỗi đã biết / lưu ý kỹ thuật
- `ServerCauHinhDonViController.cs:38-91` — **toàn bộ endpoint `[AllowAnonymous]`** kể cả ghi (AddOrUpdate/Save) → bất kỳ ai có thể đổi định tuyến queue của đơn vị. Lỗ hổng đáng chú ý; nên siết quyền HT-AD.
- `_commonService`, `_configuration` inject nhưng không dùng.
- Trùng tên với bản FbChatWebhook → node suffix `_FbWebhook`.
