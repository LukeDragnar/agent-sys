---
controller: MeInvoiceController
module: ke-toan
service: Invoice
man_hinh_ui: "Cấu hình HĐĐT > tab MeInvoice (MISA): CRUD tài khoản MeInvoice, lưới mẫu hóa đơn (đồng bộ từ MISA, đặt mặc định, cập nhật mapping custom field), Xem trước (preview) hóa đơn MeInvoice"
muc_dich: "Quản lý tài khoản kết nối MeInvoice/MISA + danh sách mẫu hóa đơn của tài khoản đó (đồng bộ, mặc định, mapping field), preview HĐ trước khi phát hành"
alias: ["MeInvoice", "MISA", "tài khoản MeInvoice", "mẫu hóa đơn MISA", "cấu hình MISA"]
auth: mixed   # đa số SBAuthorize có key + role; preview/mapping dùng SBAuthorize trống
state_machine: []
deprecated: false
entities: [Account, InvoiceTemplate]
stored_procs: []
depends:
  - IAccountService          # CRUD tài khoản (chung MISA/Viettel, phân biệt bằng TypeInvoice)
  - IMeInvocieService        # nghiệp vụ riêng MISA (tìm account, get account, preview)
  - IInvoiceTemplateService  # mẫu hóa đơn (sync, search, mặc định, mapping)
  - IInvoiceNoteTempateService
cross_module_in:
  - InvoiceController/Order — gọi gián tiếp qua service khi phát hành (account default + template default)
cross_module_out:
  - MeInvoice/MISA partner API (SyncTemplate, PreviewMeInvoice gọi sang MISA)
---

# MeInvoiceController — Cấu hình tài khoản & mẫu hóa đơn MeInvoice (MISA)

> Controller-level `[TienIchAttribute("MeInvoice_Controller", ..., TI_Invocie_Misa)]` — toàn bộ thuộc tiện ích MISA.
> `Account` là entity **dùng chung** MISA + Viettel, phân biệt bằng `TypeInvoice` (`EnumTypeInvoice.MeInovice`). Insert/Update ở đây **ép cứng** `TypeInvoice = MeInovice` trước khi gọi service.
> Một số endpoint mẫu hóa đơn (`CapNhatThongTinMeInvoice`, `LayMauHoaDonTheoId_MeInvoice`) đã **bỏ key permission** (comment), chỉ còn `[SBAuthorize]` trống → mọi user đăng nhập gọi được.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm tài khoản MeInvoice | Nút "Thêm mới" tab MeInvoice | `POST Insert` | MeInvoice_Insert (DV-AD-TONG/CN/MCN) | mixed | — | ModelState invalid → MESS_Model_UnValidate. Ép `TypeInvoice=MeInovice`, `_account.Insert` | EF `Account` | — |
| 2 | Cập nhật tài khoản | Nút "Sửa" | `POST Update` | MeInvoice_Update (DV-AD-TONG/CN/MCN) | mixed | — | Validate; ép `TypeInvoice=MeInovice`; `_account.Update` | EF | — |
| 3 | Tìm tài khoản theo điều kiện | Lưới tài khoản MeInvoice | `POST TimKiemAccountTheoDieuKien` | MeInvoice_TimKiemAccountTheoDieuKien (+DV-KT) | mixed | tab MeInvoice | Validate; `_meinvoice.TimKiemAccountTheoDieuKien` (scope đơn vị) | EF | — |
| 4 | Xóa tài khoản | Nút "Xóa" → confirm | `POST Delete/{id:Guid}` | MeInvoice_Delete (DV-AD-TONG/CN/MCN) | mixed | — | `_account.Delete(id, MeInovice, user)` | EF | — |
| 5 | Lấy chi tiết tài khoản | (mở form sửa) | `POST GetAccountMeInvoice/{id:Guid}` | MeInvoice_GetAccountMeInvoice (+DV-KT) | mixed | — | `_meinvoice.GetAccountMeInvoice(id, user)` | EF | — |
| 6 | Đồng bộ mẫu HĐ từ MISA | Nút "Đồng bộ mẫu" | `POST SyncTemplate/{idAccount:Guid}` | MeInvoice_SyncTemplate (+DV-KT) | mixed | có tài khoản | `_template.SyncTemplate` — gọi **MISA API** lấy danh sách mẫu, upsert `InvoiceTemplate` | EF + partner API | gọi MISA — phụ thuộc kết nối |
| 7 | Tìm mẫu HĐ theo điều kiện | Lưới mẫu hóa đơn | `POST TimKiemTemplateTheoDieuKien` | MeInvoice_TimKiemTemplateTheoDieuKien (+DV-KT) | mixed | — | Validate; `_template.TimKiemTemplateTheoDieuKien` | EF | — |
| 8 | Đặt mẫu HĐ làm mặc định | Toggle "Mặc định" trên lưới mẫu | `POST ChangeDefaultTemplate/{id:Guid}` | MeInvoice_ChangeDefaultTemplate (+DV-KT) | mixed | — | `_template.ChangeDefault(id, user)` — mẫu mặc định dùng khi phát hành | EF | — |
| 9 | Xem trước HĐ MeInvoice | Nút "Xem trước" | `POST PreviewMeInvoice` | **không key** (`[SBAuthorize]`) | mixed | — | `_meinvoice.PreviewMeInvoice(model, user)` — render preview qua MISA | partner API | key gốc đã comment → chỉ cần đăng nhập |
| 10 | Cập nhật mapping custom field mẫu | (form cấu hình mẫu) | `POST CapNhatThongTinMeInvoice` | **không key** | mixed | — | Validate; `_template.CapNhatThongTinMeInvoice(payload, user)` — lưu `MeInvoiceJsonMappingCustomField` | EF | key gốc `MeInvoice_CapNhatThongTinMeInvoice` đã comment (controller:120) |
| 11 | Lấy mẫu HĐ theo id | (mở form cấu hình mẫu) | `POST LayMauHoaDonTheoId_MeInvoice/{id:Guid}` | **không key** | mixed | — | `_template.LayMauHoaDonTheoId_MeInvoice(id, user)` | EF | key gốc đã comment (controller:134) |

## Quy tắc nghiệp vụ
- **`Account` dùng chung MISA/Viettel:** phân biệt bằng `TypeInvoice`. Controller này luôn ép `MeInovice` ở Insert/Update — KHÔNG tin giá trị client gửi.
- **Mẫu hóa đơn (`InvoiceTemplate`):** thuộc 1 `IdAccount`; `IsDefault` quyết định mẫu dùng khi phát hành; `MeInvoiceJsonMappingCustomField` map field tùy biến sang MISA.
- **Phân quyền:** action quản trị (Insert/Update/Delete) chỉ DV-AD-*; xem/tìm thêm DV-KT (kế toán).

## Quan hệ
- **Gọi sang:** `IAccountService` (Account CRUD), `IMeInvocieService` (nghiệp vụ MISA, gọi partner), `IInvoiceTemplateService` (mẫu), MISA partner API (Sync/Preview).
- **Dữ liệu (entities):** `Account` (TypeInvoice=MeInovice), `InvoiceTemplate` (IdAccount, IsDefault, mapping field).
- **Ripple / đồng bộ:** SyncTemplate kéo mẫu từ MISA về DB; account/template mặc định được InvoiceController dùng khi phát hành.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `MeInvoiceController.cs:113,121,135` — 3 endpoint (Preview, CapNhatThongTin, LayMauHoaDon) bỏ key permission (chỉ `[SBAuthorize]` trống), key gốc bị comment → mọi user đăng nhập gọi được.
- `MeInvoiceController.cs:39,51` — ép `TypeInvoice=MeInovice` đúng (chống client gửi sai NCC).
- Tên service `IMeInvocieService` / `MeInvoice_Insert` mô tả "MeInvocie" — **typo "Invocie"** lan khắp service (tiện ích `TI_Invocie_Misa` cũng vậy) — đúng theo code, không sửa.
