---
controller: DonHangInvoiceReSendController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > màn Gửi lại hóa đơn (đơn tới hạn phát hành HĐĐT bị fail/treo): list theo filter (chi nhánh / trạng thái / khoảng TimeToPublish / mã đơn), xem chi tiết, xóa, nút 'Gửi lại thủ công'"
muc_dich: "Quản lý hàng đợi đơn tới giờ phát hành hóa đơn điện tử cần gửi lại (DonHangTimeToPublishInvoiceReSend): list/filter, xem chi tiết, xóa bản ghi, và gửi lại thủ công (đặt lại TimeToPublish=now, Status=Pending, bỏ giới hạn số lần resend) để batch phát hành HĐ pick up."
alias: ["gửi lại hóa đơn", "resend hóa đơn", "đơn tới giờ phát hành HĐĐT", "invoice resend", "hàng đợi phát hành hóa đơn", "publish invoice retry"]
auth: public
state_machine: [Pending]
deprecated: false
entities: [DonHangTimeToPublishInvoiceReSend]
stored_procs: []
depends:
  - "IDonHangTimeToPublishInvoiceReSendService (OrderLogistic.Service/Generated/DonHangTimeToPublishInvoiceReSendService.cs) — CRUD trên IRepository<DonHangTimeToPublishInvoiceReSend>"
cross_module_in: []
cross_module_out:
  - "Invoice/HĐĐT batch (ResendManual đặt Status=Pending + TimeToPublish=now → batch phát hành hóa đơn pick up và gửi lại sang dịch vụ HĐĐT)"
---

# DonHangInvoiceReSendController (OrderLogistic) — Gửi lại hóa đơn tới giờ phát hành

> Trùng tên controller `DonHangInvoiceReSendController` với module `cua-hang` → node này dùng suffix `_OrderLogistic`. Đơn giản (CRUD + resend), gate toàn bộ bằng `[SBAuthorize]` mở. Controller này KHÔNG có `[TienIchAttribute]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | List hàng đợi gửi lại HĐ | grid + filter | POST `api/DonHangInvoiceReSend/GetList` | `[SBAuthorize]` (mở) | public | màn list | Lọc theo IdChiNhanh / Status / TuNgay-DenNgay (theo TimeToPublish) / MaDonHang (Contains), scope IdDonVi current user, sort TimeToPublish desc, phân trang | EF | - |
| 2 | Xem chi tiết | mở row | POST `api/DonHangInvoiceReSend/GetById/{id:Guid}` | `[SBAuthorize]` | public | có id | Trả 1 bản ghi theo Id + IdDonVi; không thấy → "Không tìm thấy bản ghi!" | EF | - |
| 3 | Xóa | "Xóa" (multi) | POST `api/DonHangInvoiceReSend/Delete` | `[SBAuthorize]` | public | tick ≥1 | Xóa các bản ghi theo list Ids (lọc IdDonVi). Rỗng → "Danh sách Id không được để trống!"; không khớp → "Không tìm thấy bản ghi nào hợp lệ!" | EF | Hard delete (DeleteAsync) |
| 4 | Gửi lại thủ công | "Gửi lại thủ công" | POST `api/DonHangInvoiceReSend/ResendManual` | `[SBAuthorize]` | public | tick ≥1 | Với mỗi bản ghi: set `TimeToPublish=now`, `Status=Pending`, `IsNotCheckMaxNumberOfResend=true` → batch phát hành HĐ pick up gửi lại (bỏ qua giới hạn số lần resend) | EF | Đặt cờ bỏ giới hạn resend → có thể gửi lại vô hạn nếu HĐ vẫn fail; side-effect phát hành thật ở batch HĐĐT, không ở đây |

## Quan hệ
- **Gọi sang:** `IDonHangTimeToPublishInvoiceReSendService` (CRUD trực tiếp `IRepository<DonHangTimeToPublishInvoiceReSend>`).
- **Dữ liệu (entities):** `DonHangTimeToPublishInvoiceReSend` (hàng đợi: MaDonHang, IdDonVi, IdChiNhanh, TimeToPublish, Status, IsNotCheckMaxNumberOfResend).
- **Ripple / đồng bộ:** không phát hành HĐ trực tiếp — `ResendManual` chỉ RESET trạng thái để job/batch phát hành hóa đơn điện tử (Invoice) phía sau pick up và gửi lại. Triage "gửi lại không chạy" → kiểm tra batch HĐĐT, không phải controller này.

## Lỗi đã biết / lưu ý kỹ thuật
- `DonHangTimeToPublishInvoiceReSendService.cs:99-104` `ResendManual` set `IsNotCheckMaxNumberOfResend=true` → vòng resend bỏ qua ngưỡng số lần tối đa; nếu HĐ vẫn fail có thể gửi lại lặp.
- Controller KHÔNG có `[TienIchAttribute]` (khác các controller khác trong service) và tất cả endpoint `[SBAuthorize]` mở → không phân quyền key.
- `Delete` là hard delete (`DeleteAsync(records, true)`), không soft-delete.
