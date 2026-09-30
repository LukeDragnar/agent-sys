---
controller: CustomerUpdateController
module: khach-hang
service: Customer
man_hinh_ui: "nội bộ — không có UI (private service-to-service)"
muc_dich: "Cập nhật dữ liệu khách hàng do các service khác kích hoạt khi thanh toán/chốt đơn: cập nhật chi tiêu, trừ điểm, cộng/rollback điểm tạm trú logistics, sync điểm theo trạng thái giao hàng"
alias: [CustomerUpdate, "Cập nhật KH khi thanh toán", "Chi tiêu logistics", "Điểm tạm trú"]
auth: internal
state_machine: []
deprecated: false
entities: [KhachHang, DonHangLogisticChiTieuLog]
stored_procs: []
depends: [ICustomerUpdateService, "Order (DonHangThanhToanService → CustomerUpdateDonHang/RollBack/TruDiem)", "OrderLogistic (ChiTieuKhachHangService, DonHangService → CapNhatChiTieuLogistic/CustomerChotDonLogistic/...)"]
cross_module_in: ["Order (thanh toán hóa đơn → cập nhật/rollback chi tiêu, trừ điểm online)", "OrderLogistic (chốt/hủy/đổi trạng thái đơn → chi tiêu + điểm tạm trú)"]
cross_module_out: []
---

# CustomerUpdate — Cập nhật KH khi thanh toán / chốt đơn (internal)

> Controller **`internal`** — mọi endpoint `[PrivateSBAuthorize()]` (chỉ gọi service-to-service, không UI). Tiện ích controller-level bị comment (`//[TienIchAttribute(...)]`) → không xuất hiện trong phân quyền tiện ích.
> Nhiều endpoint **idempotent theo `IdTrans`/`IdDonHang`** → an toàn retry (theo comment code).

## Hành động (UI / công khai)

> Controller này không có hành động UI. Bảng dưới mô tả các **endpoint nội bộ** (caller → mục đích → ripple).

| # | Hành động | Caller (service gọi) | Endpoint (HTTP path) | Quyền (key) | Auth | Idempotent | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật KH sau thanh toán đơn | Order (DonHangThanhToanService) | `POST CustomerUpdateDonHang` | `[PrivateSBAuthorize()]` (không key) | internal | — | `CustomerUpdate(model, CurrentUser)` — cập nhật thông tin/chi tiêu KH theo `DanhSachDonHangTheoKhachHang` sau khi thanh toán hóa đơn | EF | Bản cũ nhận `CustomerUpdate` đã bị comment; bản mới đổi DTO |
| 2 | Rollback khi hủy thanh toán đơn | Order/OrderProcess (OrderRollBackService) | `POST RollBackThanhToanDonHang` | `[PrivateSBAuthorize()]` | internal | — | `RollBackThanhToanDonHang(request)` — đảo tác động chi tiêu/điểm khi rollback thanh toán | EF | — |
| 3 | Trừ điểm khi xác nhận thanh toán online | Order (thanh toán online) | `POST TruDiemKhiXacNhanThanhToanOnline` | `[PrivateSBAuthorize()]` | internal | — | `TruDiemKhiXacNhanThanhToanOnline(model, CurrentUser)` — trừ điểm thưởng KH | EF | — |
| 4 | Cập nhật chi tiêu logistics | OrderLogistic (ChiTieuKhachHangService) | `POST CapNhatChiTieuLogistic` | `[PrivateSBAuthorize()]` | internal | (theo log) | 1 endpoint duy nhất xử lý **tăng/giảm/hoàn** chi tiêu logistic (`CapNhatChiTieuLogistic(req)`) | EF (`DonHangLogisticChiTieuLog`) | Gộp 3 hành vi (tăng/giảm/hoàn) vào 1 endpoint — sai `req` dễ tác động ngược dấu |
| 5 | Chốt đơn logistic → cộng điểm tạm trú | OrderLogistic (DonHangService chốt đơn) | `POST CustomerChotDonLogistic` | `[PrivateSBAuthorize()]` | internal | ✅ theo IdTrans | Gom mọi xử lý Customer khi chốt đơn (hiện: **cộng DiemTamTru**); retry an toàn theo IdTrans | EF | — |
| 6 | Rollback chốt đơn logistic | OrderLogistic (hủy chốt) | `POST CustomerRollBackChotDonLogistic` | `[PrivateSBAuthorize()]` | internal | ✅ theo IdTrans | Đảo lại tác động của #5 (trừ DiemTamTru đã cộng) | EF | — |
| 7 | Sync điểm tạm trú theo trạng thái đơn | OrderLogistic (đổi trạng thái giao) | `POST SyncDiemTamTruByTrangThaiDon` | `[PrivateSBAuthorize()]` | internal | ✅ theo IdDonHang (qua lichsu) | Apply DiemTamTru khi vào trạng thái TĂNG, revert khi ra; idempotent theo IdDonHang | EF | — |

## Quy tắc nghiệp vụ (điểm tạm trú / chi tiêu)
- **CapNhatChiTieuLogistic (#4):** 1 endpoint thống nhất 3 hành vi tăng / giảm / hoàn chi tiêu — nhánh do nội dung `req` quyết định (ghi `DonHangLogisticChiTieuLog`).
- **DiemTamTru (#5/#6/#7):** cộng khi **chốt đơn** logistic; trừ khi **rollback chốt**; **sync** theo trạng thái giao hàng (apply lúc vào trạng thái tăng, revert lúc ra). Tất cả **idempotent** (theo IdTrans hoặc IdDonHang qua lichsu) → retry không cộng/trừ trùng.

## Quan hệ
- **Gọi sang:** `ICustomerUpdateService` (toàn bộ logic cập nhật KH).
- **Dữ liệu (entities):** `KhachHang` (chi tiêu/điểm thưởng/DiemTamTru), `DonHangLogisticChiTieuLog` (log chi tiêu logistic).
- **Ripple/đồng bộ:** không phát event ra ngoài; là **đích nhận** của Order & OrderLogistic.

## Luồng xuyên module (đích nhận từ Order/OrderLogistic)
1. **Order thanh toán/hủy:** Order → `CustomerUpdateDonHang` (cập nhật chi tiêu) / `RollBackThanhToanDonHang` (đảo) / `TruDiemKhiXacNhanThanhToanOnline` (trừ điểm online).
2. **OrderLogistic chi tiêu:** OrderLogistic → `CapNhatChiTieuLogistic` (tăng/giảm/hoàn).
3. **OrderLogistic chốt đơn + DiemTamTru:** chốt → `CustomerChotDonLogistic` (cộng); hủy chốt → `CustomerRollBackChotDonLogistic` (trừ); đổi trạng thái → `SyncDiemTamTruByTrangThaiDon` (apply/revert). Idempotent theo IdTrans/IdDonHang.

## Lỗi đã biết / lưu ý kỹ thuật
- `CustomerUpdateController.cs:11` — `[TienIchAttribute(...)]` controller-level bị **comment** → không xuất hiện trong cây tiện ích/phân quyền.
- `CustomerUpdateController.cs:21-29` — bản cũ `CustomerUpdateDonHang(CustomerUpdate)` bị comment, thay bằng bản mới nhận `DanhSachDonHangTheoKhachHang` (đổi DTO — caller cũ phải đồng bộ).
- `CustomerUpdateController.cs:65-71` (`CapNhatChiTieuLogistic`) — gộp tăng/giảm/hoàn vào 1 endpoint; logic dấu nằm trong service theo `req` → cần đúng payload, không thấy ràng buộc ở controller.
- Tất cả endpoint `[PrivateSBAuthorize()]` **không key** → bảo vệ bằng cơ chế private token nội bộ, không qua permission key thường.
