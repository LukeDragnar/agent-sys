---
controller: OrderRollBackController
module: he-thong
service: ClearData
man_hinh_ui: "nội bộ — không có UI (service-to-service rollback đơn hàng)"
muc_dich: "Tạo/cập nhật và truy vấn dữ liệu rollback đơn hàng (OrderRollBack) trong ClearData service; toàn bộ endpoint là service-to-service."
alias: ["order rollback", "rollback đơn hàng", "hoàn tác đơn"]
auth: internal
state_machine: []
deprecated: false
entities: [OrderRollBack]
stored_procs: []
depends: [IOrderRollBackService]
cross_module_in: ["Service nội bộ gọi CreateRollBackAll/UpdateRollBack/GetListByIdDonHang* (PrivateSBAuthorize)"]
cross_module_out: []
---

# OrderRollBackController (ClearData) — Rollback đơn hàng

Route `api/OrderRollBack` (không `[TienIch]`). `partial` class. Mọi action `[PrivateSBAuthorize]` → service-to-service, internal.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint UI công khai) | | | | | | | | |

## Endpoint nội bộ (private / service-to-service)
- [internal] POST `api/OrderRollBack/CreateRollBackAll` (`[PrivateSBAuthorize]`) → tạo rollback toàn bộ theo CreateRollBackAllDto + CurrentUser.
- [internal] POST `api/OrderRollBack/UpdateRollBack` (`[PrivateSBAuthorize]`) → cập nhật rollback theo UpdateRollBackDto + CurrentUser.
- [internal] GET `api/OrderRollBack/GetListByIdDonHang?idDonHang=` (`[PrivateSBAuthorize]`) → list OrderRollBack theo idDonHang.
- [internal] GET `api/OrderRollBack/GetListByIdDonHangDoiSoat?idDonHang=` (`[PrivateSBAuthorize]`) → list OrderRollBack đối soát theo idDonHang.

## Quan hệ
- **Gọi sang:** `IOrderRollBackService` (CreateRollBackAll, UpdateRollBack, GetListByIdDonHang, GetListByIdDonHangDoiSoat).
- **Dữ liệu (entities):** `OrderRollBack`.
- **Ripple / đồng bộ:** không (rollback dữ liệu trong service).

## Lỗi đã biết / lưu ý kỹ thuật
- (không phát hiện) — controller mỏng, mọi action delegate service; XML doc trống.
