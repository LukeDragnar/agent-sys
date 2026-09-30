---
controller: TmpDonHangInvoiceController
module: cua-hang
service: Order
man_hinh_ui: "Cửa hàng / Đơn hàng — màn 'Phát hành hóa đơn hàng loạt' (FE service `services/order/invoice-batch.service.ts` → `OrderInvoiceBatchService`; component `pages/invoice/invoice-batch/invoice-batch.component.ts`): thống kê + thêm đơn vào danh sách phát hành + tìm kiếm + nút 'Thực hiện gửi hàng loạt' (DoWorkInvoice) + Xóa."
muc_dich: "Phát hành hóa đơn điện tử HÀNG LOẠT: gom đơn vào danh sách tạm (TmpDonHangInvoice), thống kê, rồi trigger gửi hàng loạt sang Invoice service (Misa/Viettel)."
alias: ["phát hành hóa đơn hàng loạt", "gửi hóa đơn hàng loạt", "invoice batch", "danh sách phát hành hóa đơn", "tmp đơn hàng invoice"]
auth: internal
state_machine: []
deprecated: false
entities: [TmpDonHangInvoice, DonHang]
stored_procs: []
depends: [Invoice service (Misa/Viettel), tiện ích TI_Invocie_Misa / TI_Invocie_Viettel]
cross_module_in: [FE invoice-batch.component gọi qua OrderInvoiceBatchService]
cross_module_out: [DoWorkInvoice → trigger phát hành hóa đơn hàng loạt sang Invoice service (Misa/Viettel)]
---

# TmpDonHangInvoiceController — Phát hành hóa đơn điện tử hàng loạt

> **Mọi endpoint dùng `[SBAuthorize(true)]` (superadmin-style)** + tự kiểm tra tiện ích + role hardcode trong code (xem "Quy tắc nghiệp vụ"). Vì vậy auth = internal.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thống kê đơn phát hành | "Thống kê" | POST `api/TmpDonHangInvoice/Statistical?type=` | `[SBAuthorize(true)]` (key gốc `TmpDonHangInvoice_Statistical` đã comment) | internal | sau gate tiện ích+role | Trả số liệu thống kê đơn trong danh sách phát hành theo `type` (default 1) | EF (service) | permission key bị comment → dùng role hardcode |
| 2 | Thêm đơn vào danh sách phát hành | "Thêm vào danh sách" | POST `api/TmpDonHangInvoice/Create` | `[SBAuthorize(true)]` (key gốc `TmpDonHangInvoice_Create` đã comment) | internal | sau gate; `ModelState` valid | Thêm đơn (`TmpDonHangInvoiceCreateDto`) vào bảng tạm `TmpDonHangInvoice` để chờ phát hành | EF (service) | permission key bị comment |
| 3 | Tìm kiếm danh sách phát hành | "Tìm kiếm" / grid | POST `api/TmpDonHangInvoice/TimKiemTheoDieuKien` | `[SBAuthorize(true)]` (key gốc `TmpDonHangInvoice_TimKiemTheoDieuKien` đã comment) | internal | sau gate; `ModelState` valid | List đơn trong danh sách tạm theo `TmpDonHangInvociePayload` (lọc + phân trang) | EF (service) | permission key bị comment |
| 4 | Xóa theo trạng thái | "Xóa" | POST `api/TmpDonHangInvoice/Delete/{status:int}?type=` | `[SBAuthorize(true)]` (key gốc `TmpDonHangInvoice_Delete` đã comment) | internal | sau gate | Xóa các bản ghi tạm theo `status` (+ `type` default 1) | EF (service) | permission key bị comment |
| 5 | Xóa theo id | "Xóa dòng" | POST `api/TmpDonHangInvoice/DeleteById/{id:Guid}` | `[SBAuthorize(true)]` (key gốc `TmpDonHangInvoice_DeleteById` đã comment) | internal | per-row | Xóa 1 bản ghi tạm theo id | EF (service) | permission key bị comment |
| 6 | Thực hiện gửi hàng loạt | "Thực hiện / Gửi hàng loạt" | POST `api/TmpDonHangInvoice/DoWorkInvoice?idTab=&type=` | `[SBAuthorize(true)]` (key gốc `TmpDonHangInvoice_DoWorkInvoice` đã comment) | internal | sau gate | **Trigger phát hành hóa đơn hàng loạt** cho danh sách tạm → gọi sang Invoice service (Misa/Viettel) phát hành thực tế | EF (service) + Invoice service | permission key bị comment |

## Quy tắc nghiệp vụ (gating — MỌI endpoint enforce trước khi xử lý)
Thứ tự kiểm tra (dừng & trả lỗi nếu fail):
1. **Tiện ích**: phải có `TI_Invocie_Misa` HOẶC `TI_Invocie_Viettel` (`CheckTienIch`); nếu không → "Bạn không có quyền sử dụng tiện ích này." (`CODE_Model_UnValidate`).
2. **Role hardcode**: `CurrentUser.RoleCodes` phải chứa ít nhất 1 trong danh sách cứng — `DV_AD_CN`, `DV_AD_TONG`, `DV_AD_MCN`, `DV_QLDV`, `DV_QL_KHO`, `DV_KT` (lưu ý `DV_AD_CN` xuất hiện 2 lần trong list → trùng, vô hại); nếu không → "Bạn không có quyền.".
3. (Create / TimKiemTheoDieuKien) **ModelState**: không valid → trả `MESS_Model_UnValidate`.
4. Qua hết → gọi service tương ứng.
- **Lưu ý**: permission key thật (`TmpDonHangInvoice_Statistical`, `_Create`, …) đều bị **comment** trong code → quyền KHÔNG đi qua hệ thống phân quyền key chuẩn mà phụ thuộc role hardcode + tiện ích. Đổi danh sách role/tiện ích phải sửa code, không cấu hình được.

## Quan hệ
- **Gọi sang:** Invoice service (Misa/Viettel) khi `DoWorkInvoice` phát hành hàng loạt.
- **Dữ liệu (entities):** `TmpDonHangInvoice` (danh sách tạm chờ phát hành), `DonHang` (đơn nguồn).
- **Ripple / đồng bộ:** `DoWorkInvoice` đẩy lệnh phát hành hàng loạt → side-effect lớn sang Invoice service; mỗi đơn phát hành thành công cập nhật trạng thái hóa đơn của đơn hàng.

## Luồng xuyên module (phát hành hóa đơn hàng loạt)
```
Gate tiện ích (Misa|Viettel) + role hardcode
  → Create (gom đơn vào TmpDonHangInvoice) → Statistical / TimKiemTheoDieuKien (soát)
  → DoWorkInvoice (idTab, type) → phát hành hàng loạt sang Invoice service (Misa/Viettel)
      → cập nhật trạng thái hóa đơn từng đơn
```

## Lỗi đã biết / lưu ý kỹ thuật
- **Permission key bị comment ở cả 6 endpoint** → dùng `[SBAuthorize(true)]` (superadmin) + check role hardcode trong code (`TmpDonHangInvoiceController.cs:21-130`). Không quản lý quyền bằng key chuẩn → khó audit/phân quyền linh hoạt.
- **Role list lặp `DV_AD_CN`** trong mỗi method (xuất hiện 2 lần) — thừa, không ảnh hưởng kết quả.
- **Comment tiện ích chỉ check Misa/Viettel** nhưng cùng role list cho cả 6 method → logic gate sao chép lặp lại (duplicate) qua từng endpoint.
- Controller KHÔNG có `[TienIchAttribute]` ở mức class (gate tiện ích thực hiện thủ công trong từng method qua `CheckTienIch`).
- Code thật: BE `Order/Order.API/Controllers/TmpDonHangInvoiceController.cs`; FE service `services/order/invoice-batch.service.ts`, component `pages/invoice/invoice-batch/invoice-batch.component.ts`.
