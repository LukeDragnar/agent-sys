---
controller: ContactUpdateLogErrorController
module: sales
service: Contact
man_hinh_ui: "nội bộ — màn hình Hệ thống (TI_HeThong), danh sách contact cập nhật lỗi cho admin HT-AD"
muc_dich: "Theo dõi danh sách contact cập nhật lỗi (từ đồng bộ đơn hàng) và chạy lại việc cập nhật contact từ đơn hàng"
alias: ["Contact lỗi", "Contact cập nhật lỗi", "Log lỗi contact"]
auth: public
state_machine: []
deprecated: false
entities: [ContactUpdateLogError]
stored_procs: []
depends: [IContactUpdateLogErrorService]
cross_module_in: [OrderLogistic]
cross_module_out: [Contact]
---

# ContactUpdateLogErrorController — Log lỗi cập nhật contact từ đơn hàng

> Controller nhỏ (2 endpoint), thuộc tiện ích Hệ thống (TI_HeThong), chỉ admin HT-AD dùng để theo dõi & retry contact bị lỗi đồng bộ.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách contact lỗi | Lưới "Contact cập nhật lỗi" | `POST TimTheoDieuKien` | ContactUpdateLogErrorController_TimTheoDieuKienAsync (HT-AD) | public | — | `TimTheoDieuKienAsync(gridInfo)` trả list contact lỗi theo filter | EF | — |
| 2 | Chạy lại cập nhật contact từ đơn | Nút "Cập nhật lại" | `POST CapNhatLaiContactFromDonHang` | ContactUpdateLogErrorController_CapNhatLaiContactFromDonHang (HT-AD) | public | — | Retry: quét log lỗi → cập nhật lại contact từ thông tin đơn hàng | EF | Không tham số — chạy toàn bộ bản ghi lỗi 1 lần |

## Quan hệ
- **Gọi sang:** `IContactUpdateLogErrorService`.
- **Dữ liệu (entities):** `ContactUpdateLogError` (bản ghi lỗi khi đồng bộ contact từ đơn hàng).
- **Ripple/đồng bộ:** OrderLogistic cập nhật contact thất bại → ghi log; admin chạy `CapNhatLaiContactFromDonHang` → cập nhật ngược **Contact**.

## Lỗi đã biết / lưu ý kỹ thuật
- `ContactUpdateLogErrorController.cs:41-45` (`CapNhatLaiContactFromDonHang`) — chạy retry toàn bộ không phân trang/giới hạn; nên theo dõi hiệu năng khi log lớn.
