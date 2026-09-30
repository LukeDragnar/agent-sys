---
controller: DonHangImportController
module: cua-hang
service: Order
man_hinh_ui: "Cửa hàng — màn 'Import đơn hàng từ Excel' (FE service `services/order/donhang-import.service.ts`; component `pages/Customer/quanly-khachhang/order-import/order-import.component.ts`): nút Tải mẫu DMTHX / Tải mẫu đơn hàng → Upload file → grid staging (GetListDonHangImport) → nút 'Chuyển vào đơn hàng' (InsertDonHangImportToDonHang) + nút Xóa."
muc_dich: "Import đơn hàng hàng loạt từ file Excel: upload → staging DonHangImport (validate địa chỉ tỉnh/huyện/xã) → chuyển sang đơn hàng thật."
alias: ["import đơn hàng excel", "nhập đơn hàng từ file", "tải mẫu import đơn", "đơn hàng import", "staging đơn hàng"]
auth: public
state_machine: []
deprecated: false
entities: [DonHangImport, DonHang]
stored_procs: []
depends: [Order, danh mục địa chỉ Tỉnh/Huyện/Xã (validate địa chỉ), GemBox.Spreadsheet (đọc/ghi Excel)]
cross_module_in: [FE order-import.component (quản lý khách hàng) gọi qua DonHangImportService]
cross_module_out: [Tạo đơn hàng thật trong service Order (InsertDonHangImportToDonHang) → ripple sang luồng xử lý đơn hàng]
---

# DonHangImportController — Import đơn hàng từ Excel

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tải mẫu tỉnh/huyện/xã | "Tải mẫu DMTHX" | GET `api/DonHangImport/DownloadTemplateDMTHXAsync` | — (`[SBAuthorize()]` rỗng) | public | nút tải mẫu | Sinh file Excel `DMTHX.xlsx` chứa danh mục địa chỉ tỉnh/huyện/xã (theo CurrentUser) để user copy mã địa chỉ đúng | EF (service) + GemBox | - |
| 2 | Tải mẫu đơn hàng | "Tải mẫu đơn hàng" | GET `api/DonHangImport/DownloadTemplateOrderAsync` | — (`[SBAuthorize()]` rỗng) | public | nút tải mẫu | Sinh file Excel mẫu `TemplateOrder.xlsx` để nhập đơn hàng | EF (service) + GemBox | - |
| 3 | Import file đơn hàng | "Upload / Chọn file" | POST `api/DonHangImport/ImportFileDonHang` (multipart/form-data) | — (`[SBAuthorize()]` rỗng) | public | sau khi chọn file | Đọc file Excel (`ReqImportDonHang`), parse từng dòng → ghi vào bảng staging `DonHangImport`; validate địa chỉ tỉnh/huyện/xã, đánh dấu dòng lỗi | EF (service) + GemBox | - |
| 4 | Xem danh sách staging | grid kết quả import | POST `api/DonHangImport/GetListDonHangImport` | — (`[SBAuthorize()]` rỗng) | public | màn list | List bản ghi `DonHangImport` đã upload (theo `ReqGetListDonHangImport`: filter/paging/status) để user soát trước khi chuyển | EF (service) | - |
| 5 | Chuyển staging → đơn hàng thật | "Chuyển vào đơn hàng" | POST `api/DonHangImport/InsertDonHangImportToDonHang?boQuaSaiDiaChi=&countImportTrenLan=` | — (`[SBAuthorize()]` rỗng) | public | sau khi soát staging | Chuyển bản ghi `DonHangImport` hợp lệ thành đơn hàng thật; `boQuaSaiDiaChi=true` → bỏ qua dòng sai địa chỉ; `countImportTrenLan` (default 100) = số đơn xử lý mỗi lần (batch) | EF (service) | - |
| 6 | Xóa staging | "Xóa" | POST `api/DonHangImport/XoaDonHangImport?statusImport=` | — (`[SBAuthorize()]` rỗng) | public | per-status | Xóa bản ghi staging theo `statusImport` (vd xóa dòng lỗi / đã xử lý) của CurrentUser | EF (service) | - |
| 7 | Lấy thông tin lần import | (nội bộ màn) | GET `api/DonHangImport/GetInfoImportExcel` | — (`[SBAuthorize()]` rỗng) | public | load màn | Trả info trạng thái/tổng kết lần import gần nhất (số dòng OK/lỗi…) | EF (service) | - |

## Quan hệ
- **Gọi sang:** danh mục địa chỉ tỉnh/huyện/xã để validate khi parse Excel (template DMTHX dùng để map mã địa chỉ).
- **Dữ liệu (entities):** `DonHangImport` (bảng staging trung gian), `DonHang` (đơn hàng thật sau khi chuyển).
- **Ripple / đồng bộ:** `InsertDonHangImportToDonHang` tạo đơn hàng thật → kích hoạt luồng xử lý đơn hàng chuẩn của service Order.

## Luồng xuyên module (import đơn hàng hàng loạt)
```
Tải mẫu (DMTHX + TemplateOrder) → user điền Excel
  → ImportFileDonHang (upload) → parse + validate địa chỉ → ghi staging DonHangImport
  → GetListDonHangImport (soát dòng OK/lỗi)
  → InsertDonHangImportToDonHang (batch countImportTrenLan, boQuaSaiDiaChi tùy chọn) → tạo DonHang thật
```

## Lỗi đã biết / lưu ý kỹ thuật
- Tất cả endpoint dùng `[SBAuthorize()]` rỗng → chỉ yêu cầu đăng nhập, KHÔNG check permission key cụ thể (auth: public).
- Controller gắn `[TienIchAttribute("DonHangImportController", ..., MaTienIchSCRM.TI_Order)]` → vẫn gate theo tiện ích TI_Order ở tầng tiện ích.
- Code thật: BE `Order/Order.API/Controllers/DonHangImportController.cs`; FE service `services/order/donhang-import.service.ts`, component `pages/Customer/quanly-khachhang/order-import/order-import.component.ts`.
