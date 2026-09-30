---
controller: CommonController
module: ke-toan
service: Invoice
man_hinh_ui: "Dropdown/danh sách dùng chung trong cấu hình & phát hành HĐĐT: chọn tài khoản, chọn mẫu hóa đơn; quản lý ghi chú HĐ (CRUD note template) — kế toán/admin chi nhánh dùng"
muc_dich: "Cung cấp danh sách dùng chung (tài khoản phát hành, mẫu hóa đơn) + CRUD ghi chú mẫu hóa đơn (InvoiceNoteTemplate); 1 endpoint internal lấy thông tin HĐ mặc định cho service khác"
alias: ["danh sách tài khoản HĐ", "danh sách mẫu HĐ", "ghi chú hóa đơn", "note hóa đơn", "common invoice"]
auth: mixed   # SBAuthorize (UI) + 1 PrivateSBAuthorize (internal)
state_machine: []
deprecated: false
entities: [Account, InvoiceTemplate, InvoiceNoteTemplate]
stored_procs: []
depends:
  - ICommonService              # GetInvoiceInfoDefault
  - IAccountService             # GetList
  - IInvoiceTemplateService     # GetListTemplate
  - IInvoiceNoteTempateService  # CRUD note
cross_module_in:
  - Order/OrderLogistic (GetInvoiceInfoDefault — lấy thông tin phát hành mặc định theo đơn vị+chi nhánh)
cross_module_out: []
---

# CommonController — Danh sách dùng chung & ghi chú hóa đơn

> Controller "tiện ích chung" của service Invoice: 2 endpoint list (tài khoản, mẫu) đều **gate tiện ích** Misa/Viettel; CRUD ghi chú (`InvoiceNoteTemplate`) gate bằng **role admin chi nhánh** (`CheckUserLoginIsRoleAdminChiNhanh`).
> Không có nghiệp vụ phức tạp — chủ yếu wrap service trả list + CRUD note. 1 endpoint internal `GetInvoiceInfoDefault` cho Order/OrderLogistic.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách tài khoản phát hành | Dropdown "Tài khoản" | `POST GetListAccount` | không key (gate tiện ích) | public | có tiện ích Misa/Viettel | Gate Misa||Viettel; `_account.GetList(user)` | EF `Account` | gate fail → ResponseResult lỗi |
| 2 | Danh sách mẫu hóa đơn | Dropdown "Mẫu hóa đơn" | `POST GetListTemplate` | không key (gate tiện ích) | public | — | Gate; `_template.GetListTemplate(user)` | EF `InvoiceTemplate` | — |
| 3 | Tạo ghi chú mẫu HĐ | Nút "Thêm ghi chú" | `POST CreateNote` | không key (gate role admin-CN) | public | user là admin chi nhánh | Không phải admin-CN → "Bạn không có quyền tạo ghi chú."; Validate; `_note.Create(model, user)` | EF `InvoiceNoteTemplate` | quyền bằng check role trong code, không phải permission key |
| 4 | Cập nhật ghi chú | Nút "Sửa ghi chú" | `POST UpdateNote` | không key (gate role admin-CN) | public | admin-CN | tương tự #3, `_note.Update` | EF | — |
| 5 | Xóa ghi chú | Nút "Xóa ghi chú" | `POST DeleteNote/{id:Guid}` | không key (gate role admin-CN) | public | admin-CN | check admin-CN → `_note.Delete(id, user)` | EF | — |
| 6 | Chi tiết ghi chú | (mở form sửa) | `POST GetDetailNote/{id:Guid}` | không key (`[SBAuthorize]`) | public | — | `_note.GetDetail(id, user)` | EF | không gate role (khác #3-5) |
| 7 | Tìm ghi chú (phân trang) | Lưới ghi chú | `POST SearchNote` | không key | public | — | Validate; `_note.Search(model, user)` | EF | — |
| 8 | Lấy tất cả ghi chú | (dropdown chọn note khi phát hành) | `POST GetAllNote` | không key | public | — | `_note.GetAllNote(user)` | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] **Order/OrderLogistic → `POST GetInvoiceInfoDefault/{idDonVi:Guid}/{idChiNhanh:Guid}`** (`PrivateSBAuthorize`): lấy thông tin HĐ mặc định (tài khoản + mẫu default) theo đơn vị + chi nhánh — dùng khi phát hành tự động.

## Quan hệ
- **Gọi sang:** `IAccountService`, `IInvoiceTemplateService`, `IInvoiceNoteTempateService`, `ICommonService`. Không gọi microservice khác (chỉ phục vụ Order qua endpoint internal).
- **Dữ liệu (entities):** `Account`, `InvoiceTemplate` (list), `InvoiceNoteTemplate` (CRUD note đính kèm hóa đơn).
- **Ripple / đồng bộ:** không có consumer/event; chỉ cung cấp dữ liệu.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `CommonController.cs:77,93,109` — quyền CRUD ghi chú dựa trên `FunctionCommon.CheckUserLoginIsRoleAdminChiNhanh` (hard-code role) chứ không permission key → không xuất hiện trong hệ thống phân quyền chuẩn.
- `CommonController.cs:118-124` (`GetDetailNote`) — KHÔNG gate role admin-CN trong khi Create/Update/Delete có → user thường vẫn xem chi tiết ghi chú được.
- 2 endpoint list (#1,#2) gate tiện ích lặp lại copy-paste (giống pattern InvoiceController).
