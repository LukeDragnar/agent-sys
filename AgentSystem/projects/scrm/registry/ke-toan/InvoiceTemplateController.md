---
controller: InvoiceTemplateController
module: ke-toan
service: Invoice
man_hinh_ui: "nội bộ — không có UI riêng (trùng chức năng mẫu hóa đơn của MeInvoice/Viettel controller); endpoint mẫu hóa đơn 'phẳng' không phân NCC"
muc_dich: "CRUD/đồng bộ mẫu hóa đơn chung (sync từ NCC, tìm theo điều kiện, đặt mặc định) — phiên bản không gắn tiện ích, dùng SBAuthorize(true)"
alias: ["mẫu hóa đơn", "invoice template", "đồng bộ mẫu HĐ"]
auth: public   # tất cả [SBAuthorize(true)] — chỉ cần đăng nhập
state_machine: []
deprecated: false
entities: [InvoiceTemplate]
stored_procs: []
depends:
  - IInvoiceTemplateService
cross_module_in: []
cross_module_out:
  - MeInvoice/Viettel partner API (SyncTemplate gọi sang NCC theo account)
---

# InvoiceTemplateController — Mẫu hóa đơn (phiên bản chung, không phân NCC)

> Controller nhỏ (3 endpoint sống), **trùng chức năng** với phần "mẫu hóa đơn" trong `MeInvoiceController` / `ViettelInvoiceController` nhưng KHÔNG gắn tiện ích, KHÔNG phân nhánh NCC ở controller — `SyncTemplate` dựa vào account để biết NCC.
> Tất cả dùng `[SBAuthorize(true)]` (overload bool — chỉ cần đăng nhập, không gắn permission key cụ thể).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đồng bộ mẫu HĐ theo tài khoản | (nút đồng bộ — nếu dùng) | `POST SyncTemplate/{idAccount:Guid}` | không key (`[SBAuthorize(true)]`) | public | có account | `_invoiceTemplate.SyncTemplate(idAccount, user)` — gọi NCC (MISA/Viettel theo account) lấy mẫu, upsert | EF + partner API | NCC suy ra từ account, không từ controller |
| 2 | Tìm mẫu HĐ theo điều kiện | Lưới mẫu | `POST TimTheoDieuKien` | không key | public | — | Validate; `_invoiceTemplate.TimKiemTemplateTheoDieuKien(model, user)` | EF | — |
| 3 | Đặt mẫu HĐ làm mặc định | Toggle "Mặc định" | `POST ChangeDefaultTemplate/{id:Guid}` | không key | public | — | `_invoiceTemplate.ChangeDefault(id, user)` | EF | — |

## Quan hệ
- **Gọi sang:** `IInvoiceTemplateService` (cùng service mà MeInvoice/Viettel controller dùng). `SyncTemplate` → partner API NCC tương ứng account.
- **Dữ liệu (entities):** `InvoiceTemplate`.
- **Ripple / đồng bộ:** SyncTemplate kéo mẫu từ NCC về DB.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- **Trùng lặp chức năng:** SyncTemplate/TimTheoDieuKien/ChangeDefaultTemplate giống hệt các endpoint mẫu trong MeInvoice/Viettel controller (cùng gọi `IInvoiceTemplateService`) → có thể là endpoint "phẳng" cũ/dùng chung; cần xác minh FE gọi controller nào để tránh nhầm.
- `InvoiceTemplateController.cs:54-60` — `TestSyncTemplateViettel` (AllowAnonymous) đã comment.
- Toàn bộ `[SBAuthorize(true)]` không key → không gắn vào phân quyền chi tiết, chỉ chặn anonymous.
