---
controller: TmpDonHangInvoiceController
module: kho
service: OrderLogistic
man_hinh_ui: "Giao vận / Đơn hàng logistic — màn 'Phát hành hóa đơn hàng loạt' (đơn logistic): thống kê + thêm đơn vào danh sách phát hành tạm + tìm kiếm + nút 'Thực hiện gửi hàng loạt' (DoWorkInvoice) + Xóa. [File _OrderLogistic do trùng tên với TmpDonHangInvoiceController service Order module cua-hang]."
muc_dich: "Phát hành hóa đơn điện tử HÀNG LOẠT cho đơn logistic: gom đơn vào danh sách tạm (TmpDonHangInvoice), thống kê, tìm kiếm, rồi trigger gửi hàng loạt sang Invoice (Misa/Viettel)."
alias: ["phát hành hóa đơn hàng loạt logistic", "gửi hóa đơn hàng loạt", "invoice batch logistic", "danh sách phát hành hóa đơn", "tmp đơn hàng invoice logistic"]
auth: internal
state_machine: []
deprecated: false
entities: [TmpDonHangInvoice]
stored_procs: []
depends:
  - "ITmpDonHangInvoiceService (Statistical/Create/TimKiemTheoDieuKien/Delete/DoWorkInvoice)"
cross_module_in:
  - "FE đơn logistic (màn phát hành hóa đơn hàng loạt)"
cross_module_out:
  - "Invoice service (Misa/Viettel) — gửi hóa đơn hàng loạt qua DoWorkInvoice"
---

# TmpDonHangInvoiceController (OrderLogistic) — Phát hành hóa đơn hàng loạt (đơn logistic)

> Trùng tên với `cua-hang/TmpDonHangInvoiceController.md` (service Order). Đây là bản **service OrderLogistic** → file suffix `_OrderLogistic`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thống kê đơn phát hành | "Thống kê" (tab) | POST `api/TmpDonHangInvoice/Statistical?type=1` | `[SBAuthorize(true)]` + gate TienIch + gate role (xem Quy tắc) | internal | màn phát hành hàng loạt | Pass gate → `_service.Statistical(CurrentUser, type)`; fail TienIch → lỗi "không có quyền sử dụng tiện ích"; fail role → "Bạn không có quyền." | ITmpDonHangInvoiceService | Permission key SBAuthorize bị comment-out, chỉ dùng `(true)` + check thủ công |
| 2 | Thêm đơn vào danh sách | "Thêm" | POST `api/TmpDonHangInvoice/Create` | `[SBAuthorize(true)]` + gate TienIch + role | internal | màn phát hành | Gate OK + ModelState valid → `_service.Create(payload, CurrentUser)`; model invalid → `MESS_Model_UnValidate` | ITmpDonHangInvoiceService (TmpDonHangInvoice) | - |
| 3 | Tìm kiếm danh sách | "Tìm kiếm" → grid | POST `api/TmpDonHangInvoice/TimKiemTheoDieuKien` | `[SBAuthorize(true)]` + gate TienIch + role | internal | màn phát hành | Gate OK + ModelState valid → `_service.TimKiemTheoDieuKien(payload, CurrentUser)` | ITmpDonHangInvoiceService | - |
| 4 | Xóa theo status (cả danh sách) | "Xóa" | POST `api/TmpDonHangInvoice/Delete/{status:int}?type=1` | `[SBAuthorize(true)]` + gate TienIch + role | internal | màn phát hành | Gate OK → `_service.Delete(status, CurrentUser, type)` (xóa nhóm theo trạng thái) | ITmpDonHangInvoiceService | - |
| 5 | Xóa 1 đơn theo id | "Xóa dòng" | POST `api/TmpDonHangInvoice/DeleteById/{id:Guid}` | `[SBAuthorize(true)]` + gate TienIch + role | internal | per-row | Gate OK → `_service.Delete(id, CurrentUser)` | ITmpDonHangInvoiceService | - |
| 6 | Thực hiện gửi hàng loạt | "Thực hiện gửi hàng loạt" | POST `api/TmpDonHangInvoice/DoWorkInvoice?type=1` (idTab) | `[SBAuthorize(true)]` + gate TienIch + role | internal | sau khi gom đơn | Gate OK → `_service.DoWorkInvoice(CurrentUser, type)` → trigger gửi hóa đơn hàng loạt sang Invoice (Misa/Viettel) | ITmpDonHangInvoiceService → Invoice | Param `idTab` nhận vào nhưng KHÔNG truyền xuống service (DoWorkInvoice chỉ dùng CurrentUser, type) |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
Mọi endpoint áp **cùng 2 lớp gate** (lặp ở từng method):
1. **Gate TienIch**: phải có `TI_Invocie_Misa` HOẶC `TI_Invocie_Viettel` (`CheckTienIch`). Không có cả 2 → "Bạn không có quyền sử dụng tiện ích này." (`CODE_Model_UnValidate`).
2. **Gate role**: `CurrentUser.RoleCodes` phải giao với list `{ DV_AD_CN, DV_AD_TONG, DV_AD_MCN, DV_QLDV, DV_QL_KHO, DV_KT }` (DV_AD_CN bị liệt kê 2 lần — thừa). Không thỏa → "Bạn không có quyền."
3. `type` mặc định = 1 (phân loại tab/loại hóa đơn).

## Quan hệ
- **Gọi sang:** `ITmpDonHangInvoiceService`.
- **Dữ liệu (entities):** `TmpDonHangInvoice` (bảng tạm gom đơn chờ phát hành hóa đơn).
- **Ripple / đồng bộ:** `DoWorkInvoice` → gửi hóa đơn hàng loạt sang Invoice service (Misa/Viettel) → phát hành HĐĐT cho các đơn trong danh sách.

## Luồng xuyên module
- Gom đơn (Create) → thống kê (Statistical) → tìm kiếm/soát (TimKiemTheoDieuKien) → "Thực hiện gửi hàng loạt" (DoWorkInvoice) → Invoice (Misa/Viettel) phát hành HĐĐT.

## Lỗi đã biết / lưu ý kỹ thuật
- Gate TienIch + role bị **copy-paste lặp** ở cả 6 method (TmpDonHangInvoiceController.cs:24-32, 42-50, ...) → nên gom vào filter/attribute; sửa role list phải sửa 6 chỗ.
- `DV_AD_CN` xuất hiện 2 lần trong list role mỗi method → trùng (vô hại nhưng bẩn).
- `DoWorkInvoice` nhận `idTab` (Guid) nhưng không truyền xuống service (TmpDonHangInvoiceController.cs:121-132) → param dead, gửi theo CurrentUser+type.
- Permission key thật trong SBAuthorize đều bị comment-out (vd `TmpDonHangLogicsticInvoice_Statistical`) → chỉ `[SBAuthorize(true)]` + check thủ công; nếu cần map quyền chuẩn phải bật lại.
