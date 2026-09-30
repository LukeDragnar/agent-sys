---
controller: ViettelInvoiceController
module: ke-toan
service: Invoice
man_hinh_ui: "Cấu hình HĐĐT > tab Viettel (SInvoice): CRUD tài khoản Viettel Invoice, lưới mẫu hóa đơn (đồng bộ từ Viettel, đặt mặc định, cập nhật mapping/ký hiệu/seri), Xem trước (preview) hóa đơn SInvoice"
muc_dich: "Quản lý tài khoản kết nối Viettel SInvoice + mẫu hóa đơn của tài khoản (đồng bộ, mặc định, ký hiệu/template code Viettel, mapping field), preview HĐ"
alias: ["Viettel Invoice", "SInvoice", "tài khoản Viettel", "mẫu hóa đơn Viettel", "cấu hình SInvoice"]
auth: mixed
state_machine: []
deprecated: false
entities: [Account, InvoiceTemplate]
stored_procs: []
depends:
  - IViettelInvoiceService   # nghiệp vụ riêng Viettel (account, preview SInvoice, đồng bộ)
  - IAccountService          # Delete (chung)
  - IInvoiceTemplateService  # mẫu (sync, search, mặc định, mapping SInvoice)
  - IInvoiceNoteTempateService
cross_module_in:
  - InvoiceController/Order — gọi gián tiếp khi phát hành/đồng bộ nháp Viettel
cross_module_out:
  - Viettel SInvoice partner API (SyncTemplate, PreviewSInvoice)
---

# ViettelInvoiceController — Cấu hình tài khoản & mẫu hóa đơn Viettel (SInvoice)

> `partial class`, controller-level `[TienIch("AccountController", "Quản lý cấu hình hóa đơn viettel", TI_Invocie_Viettel)]` — thuộc tiện ích Viettel.
> Đối xứng với `MeInvoiceController` nhưng cho NCC Viettel: cùng entity `Account`/`InvoiceTemplate`, khác là mẫu Viettel có thêm `ViettelTemplateCode`/`ViettelInvoiceSeri`/`SInvoiceJsonMappingCustomField`.
> **Bug NCC mặc định:** `InsertOrUpdate` set `TypeInvoice = MeInovice` khi client gửi null — tài khoản tạo qua tab Viettel mà thiếu TypeInvoice sẽ thành **MISA** (xem known-issue).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết tài khoản Viettel | (mở form sửa) | `GET TimTheoId/{id:Guid?}` | ViettelInvoice_TimTheoId (DV-AD-*/DV-KT) | mixed | — | `_service.GetAccount(id, user)` | EF `Account` | — |
| 2 | Thêm/Sửa tài khoản Viettel | Nút "Lưu" tab Viettel | `POST InsertOrUpdate` | ViettelInvoice_InsertOrUpdate (DV-AD-*) | mixed | — | Validate; gán IdDonVi/IdChiNhanh từ user; **nếu TypeInvoice==null → set MeInovice**; `_service.InsertOrUpdate` | EF | TypeInvoice null fallback = MISA (controller:51-54) — sai cho tab Viettel |
| 3 | Xóa tài khoản | Nút "Xóa" | `POST Delete/{id:Guid}` | ViettelInvoice_Xoa (DV-AD-*) | mixed | — | `_account.Delete(id, ViettelInvoice, user)` — ép NCC=Viettel khi xóa | EF | — |
| 4 | Tìm tài khoản theo điều kiện | Lưới tài khoản Viettel | `POST TimTheoDieuKien` | ViettelInvoice_TimTheoDieuKien (+DV-KT) | mixed | tab Viettel | `_service.TimTheoDieuKienAsync(gridInfo, user)` | EF | — |
| 5 | Đồng bộ mẫu HĐ từ Viettel | Nút "Đồng bộ mẫu" | `POST SyncTemplate/{idAccount:Guid}` | ViettelInvoice_SyncTemplate (+DV-KT) | mixed | có tài khoản | `_template.SyncTemplate` — gọi **Viettel API** lấy mẫu, upsert InvoiceTemplate | EF + partner API | — |
| 6 | Tìm mẫu HĐ theo điều kiện | Lưới mẫu hóa đơn | `POST TimKiemTemplateTheoDieuKien` | ViettelInvoice_TimKiemTemplateTheoDieuKien (+DV-KT) | mixed | — | Validate; `_template.TimKiemTemplateTheoDieuKien` | EF | — |
| 7 | Đặt mẫu HĐ làm mặc định | Toggle "Mặc định" | `POST ChangeDefaultTemplate/{id:Guid}` | ViettelInvoice_ChangeDefaultTemplate (+DV-KT) | mixed | — | `_template.ChangeDefault(id, user)` | EF | — |
| 8 | Xem trước HĐ SInvoice | Nút "Xem trước" | `POST PreviewSInvoice` | **không key** (`[SBAuthorize]`) | mixed | — | `_service.PreviewSInvoice(model, user)` — render qua Viettel | partner API | chỉ cần đăng nhập |
| 9 | Cập nhật mapping mẫu SInvoice | (form cấu hình mẫu) | `POST CapNhatThongTinSInvoice` | **không key** | mixed | — | Validate; `_template.CapNhatThongTinSInvoice(payload, user)` — lưu ký hiệu/seri/`SInvoiceJsonMappingCustomField` | EF | key gốc đã comment (controller:111) |
| 10 | Lấy mẫu HĐ theo id | (mở form cấu hình mẫu) | `POST LayMauHoaDonTheoId_SInvoice/{id:Guid}` | **không key** | mixed | — | `_template.LayMauHoaDonTheoId_SInvoice(id, user)` | EF | key gốc đã comment (controller:123) |

## Quy tắc nghiệp vụ
- **Account dùng chung NCC** — Delete ép `ViettelInvoice`; nhưng InsertOrUpdate fallback `MeInovice` khi null (lệch so với tab UI).
- **Mẫu Viettel:** thêm `ViettelTemplateCode` + `ViettelInvoiceSeri` (ký hiệu) + `SInvoiceJsonMappingCustomField` (mapping riêng SInvoice).
- **Phân quyền:** quản trị tài khoản DV-AD-*; xem/tìm/mẫu thêm DV-KT.

## Quan hệ
- **Gọi sang:** `IViettelInvoiceService` (account + preview SInvoice + đồng bộ `DongBoSInvoice` cho luồng nháp), `IInvoiceTemplateService` (mẫu), Viettel partner API.
- **Dữ liệu (entities):** `Account` (TypeInvoice=ViettelInvoice), `InvoiceTemplate` (ViettelTemplateCode/Seri/mapping).
- **Ripple / đồng bộ:** `DongBoSInvoice` được `InvoiceController.DongBoTrangThaiHoaDonNhap` gọi cho nhánh Viettel; account/template mặc định dùng khi phát hành.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- 🆕 `ViettelInvoiceService.cs:~1615,~1646` (dựng `req.buyerInfo` khi phát hành/preview) — map `buyerInfo.buyerCode = DonHang.BuyerInfo?.buyerCode` (mã KH người mua, ngang MISA `AccountObjectCode`); nếu `cauHinh.IsSuDungMaKhachHangMacDinh==true && MaKhachHangMacDinh` có → override `buyerCode = MaKhachHangMacDinh`. Nguồn buyerCode từ OrderLogistic (Config B "1 phần mặc định" chọn `MaKhachHang`). Xem feature `hoa-don-yeu-cau-xuat-logistic`.
- `ViettelInvoiceController.cs:51-54` (`InsertOrUpdate`) — TypeInvoice null fallback `MeInovice`. Tab UI là Viettel nhưng entity có thể bị gán MISA nếu FE không truyền TypeInvoice → tài khoản "lạc NCC".
- `ViettelInvoiceController.cs:104,113,125` — 3 endpoint (Preview/CapNhatThongTin/LayMauHoaDon) bỏ key, chỉ `[SBAuthorize]` trống.
- `ViettelInvoiceController.cs:16` — `[TienIch("AccountController", ...)]` dùng tên alias "AccountController" (giống `QuanLyCauHinhHoaDonController`) — tên tiện ích trùng, dễ nhầm khi map quyền.
