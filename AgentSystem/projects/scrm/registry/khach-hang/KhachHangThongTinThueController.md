---
controller: KhachHangThongTinThueController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/quanly-khachhang-detail — thông tin thuế của khách hàng (form/section thuế khi xuất hóa đơn); ghi (upsert) chủ yếu từ luồng private khi tạo/xuất hóa đơn"
muc_dich: "Quản lý thông tin thuế của khách hàng: upsert (thêm/cập nhật) thông tin thuế (private) và lấy danh sách thông tin thuế theo khách hàng"
alias: [KhachHangThongTinThue, "Thông tin thuế KH", "Tax info", ThongTinThue]
auth: mixed
state_machine: []
deprecated: false
entities: [KhachHangThongTinThue]
stored_procs: []
depends: [IKhachHangThongTinThueService, BaseController]
cross_module_in: ["(private) Invoice/Order gọi UpsertThongTinThue khi lưu/xuất hóa đơn có thông tin thuế KH"]
cross_module_out: []
---

# KhachHangThongTinThue — Thông tin thuế khách hàng

> Controller **GỌN** (2 endpoint), CRUD đơn giản: 1 endpoint ghi (upsert, private) + 1 endpoint đọc theo KH.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy DS thông tin thuế theo KH | Section "Thông tin thuế" | `GET GetListByKhachHang?idKhachHang` | — (`[SBAuthorize]`) | public | — | `GetListByKhachHang(idKhachHang, CurrentUser)` → list thông tin thuế của KH | EF | — |

## Endpoint nội bộ (private / service-to-service)

- [internal] `POST UpsertThongTinThue` (`[PrivateSBAuthorize]`, body `KhachHangThongTinThueDto`) — thêm mới hoặc cập nhật thông tin thuế của KH; gọi từ luồng hóa đơn (Invoice/Order) khi cần lưu thông tin thuế.

## Quan hệ
- **Gọi sang:** `IKhachHangThongTinThueService` (upsert + list).
- **Dữ liệu (entities):** `KhachHangThongTinThue` (gắn theo idKhachHang).
- **Ripple / đồng bộ:** upsert được gọi private từ luồng hóa đơn; không tự đẩy event ra ngoài.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `KhachHangThongTinThueController.cs:10-19` — controller **không có** `[SBAuthorize]` ở mức class; phân quyền theo từng action (1 private + 1 public). `UpsertThongTinThue` chỉ truy cập được qua luồng private (`[PrivateSBAuthorize]`), không có UI ghi trực tiếp (cần xác nhận FE chỉ đọc).
