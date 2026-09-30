---
controller: DonHangInvoiceReSendController
module: cua-hang
service: Order
man_hinh_ui: "Cửa hàng / Đơn hàng — màn 'Hóa đơn điện tử cần gửi lại' (FE service `services/order/invoice-resend.service.ts` → `OrderInvoiceReSendService`; component `pages/Order/invoice-resend/invoice-resend.component.ts`, route `invoice-resend`): grid danh sách + chọn dòng → nút Xóa / Gửi lại thủ công."
muc_dich: "Quản lý danh sách hóa đơn điện tử đến hẹn phát hành nhưng gửi lỗi/cần gửi lại: xem, xem chi tiết, xóa, gửi lại thủ công."
alias: ["hóa đơn điện tử gửi lại", "resend hóa đơn", "phát hành lại hóa đơn", "đơn hàng cần gửi lại invoice", "invoice resend"]
auth: public
state_machine: []
deprecated: false
entities: [DonHangTimeToPublishInvoiceReSend]
stored_procs: []
depends: [Invoice service (phát hành hóa đơn điện tử Misa/Viettel)]
cross_module_in: [FE invoice-resend.component gọi qua OrderInvoiceReSendService]
cross_module_out: [ResendManual → trigger phát hành lại hóa đơn sang Invoice service (Misa/Viettel)]
---

# DonHangInvoiceReSendController — Hóa đơn điện tử cần gửi lại

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách hóa đơn cần gửi lại | grid + "Tìm kiếm" | POST `api/DonHangInvoiceReSend/GetList` | — (`[SBAuthorize]` rỗng) | public | màn list | List `DonHangTimeToPublishInvoiceReSend` theo `DonHangTimeToPublishInvoiceReSendFilter` (lọc + phân trang) của CurrentUser | EF (service) | - |
| 2 | Xem chi tiết 1 bản ghi | (mở chi tiết) | POST `api/DonHangInvoiceReSend/GetById/{id:Guid}` | — (`[SBAuthorize]` rỗng) | public | per-row | Trả 1 bản ghi theo id (chi tiết hóa đơn/đơn cần gửi lại) | EF (service) | - |
| 3 | Xóa khỏi danh sách | "Xóa" | POST `api/DonHangInvoiceReSend/Delete` (body: `List<Guid>`) | — (`[SBAuthorize]` rỗng) | public | chọn 1+ dòng | Xóa các bản ghi theo danh sách id (bỏ khỏi hàng đợi gửi lại) | EF (service) | - |
| 4 | Gửi lại thủ công | "Gửi lại" | POST `api/DonHangInvoiceReSend/ResendManual` (body: `List<Guid>`) | — (`[SBAuthorize]` rỗng) | public | chọn 1+ dòng | Trigger phát hành lại hóa đơn điện tử cho các id đã chọn → gọi sang Invoice service (Misa/Viettel) | EF (service) + Invoice service | - |

## Quan hệ
- **Gọi sang:** Invoice service (phát hành hóa đơn điện tử Misa/Viettel) khi `ResendManual`.
- **Dữ liệu (entities):** `DonHangTimeToPublishInvoiceReSend` (hàng đợi hóa đơn đến hẹn phát hành/cần gửi lại).
- **Ripple / đồng bộ:** `ResendManual` đẩy yêu cầu phát hành lại → side-effect sang Invoice service; thành công thì bản ghi được cập nhật/loại khỏi danh sách.

## Lỗi đã biết / lưu ý kỹ thuật
- Tất cả endpoint dùng `[SBAuthorize]` rỗng → chỉ yêu cầu đăng nhập, không gắn permission key (auth: public).
- Controller KHÔNG có `[TienIchAttribute]` ở mức class.
- Code thật: BE `Order/Order.API/Controllers/DonHangInvoiceReSendController.cs`; FE service `services/order/invoice-resend.service.ts`, component `pages/Order/invoice-resend/invoice-resend.component.ts`.
